using IdentityService.Persistence;
using IdentityService.Security;
using IdentityService.Security.Clients;
using IdentityService.Security.Jwt;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizationCodeRedemptionService(
    IdentityDbContext db,
    IClientRegistry clientRegistry,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider,
    IdentityMetrics metrics)
{
    public async Task<AuthorizationCodeExchangeResult> TryExchangeAsync(
        string code,
        string codeVerifier,
        string clientId,
        string redirectUri,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(codeVerifier)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(redirectUri))
        {
            return AuthorizationCodeExchangeResult.InvalidRequest(
                "The authorization code request is missing a required parameter.");
        }

        if (!PkceValidator.IsValidS256ChallengeFormat(codeVerifier))
        {
            return AuthorizationCodeExchangeResult.InvalidRequest(
                "The code_verifier is invalid.");
        }

        var hash = AuthorizationCodeHasher.Hash(code);
        var row = await db.AuthorizationCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.CodeHash == hash, cancellationToken);

        if (row is null)
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (!string.Equals(row.ClientId, clientId, StringComparison.Ordinal))
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (!string.Equals(row.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        var now = timeProvider.GetUtcNow();
        if (row.UsedAt is not null)
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (row.ExpiresAt <= now)
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (!PkceValidator.IsChallengeMethodAllowed(row.CodeChallengeMethod))
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (!PkceValidator.VerifyS256(codeVerifier, row.CodeChallenge))
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (!clientRegistry.TryGet(clientId, out var clientSummary))
        {
            return AuthorizationCodeExchangeResult.InvalidGrant();
        }

        if (clientSummary.ClientType == ClientType.Confidential)
        {
            if (string.IsNullOrWhiteSpace(clientSecret)
                || !clientRegistry.ValidateClientSecret(clientId, clientSecret))
            {
                return AuthorizationCodeExchangeResult.InvalidClient();
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var winners = await db.Database.SqlQuery<AuthorizationCodeRedeemRow>($"""
                UPDATE authorization_code
                SET used_at = {now}
                WHERE code_hash = {hash}
                  AND used_at IS NULL
                  AND expires_at > {now}
                  AND client_id = {clientId}
                  AND redirect_uri = {redirectUri}
                RETURNING user_id AS "UserId"
                """).ToListAsync(cancellationToken);

            if (winners.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AuthorizationCodeExchangeResult.InvalidGrant();
            }

            var userId = winners[0].UserId;
            var issued = accessTokenIssuer.IssueForUser(userId);
            await transaction.CommitAsync(cancellationToken);
            metrics.RecordAuthorizationCodeExchange();
            return AuthorizationCodeExchangeResult.Success(issued);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed class AuthorizationCodeRedeemRow
    {
        public Guid UserId { get; init; }
    }
}

public readonly record struct AuthorizationCodeExchangeResult(
    bool IsSuccess,
    IssuedAccessToken? Token,
    string? Error,
    string? ErrorDescription)
{
    public static AuthorizationCodeExchangeResult Success(IssuedAccessToken token) =>
        new(true, token, null, null);

    public static AuthorizationCodeExchangeResult InvalidGrant() =>
        new(false, null, "invalid_grant", "The authorization code is invalid or has expired.");

    public static AuthorizationCodeExchangeResult InvalidClient() =>
        new(false, null, "invalid_client", "Client authentication failed.");

    public static AuthorizationCodeExchangeResult InvalidRequest(string description) =>
        new(false, null, "invalid_request", description);
}