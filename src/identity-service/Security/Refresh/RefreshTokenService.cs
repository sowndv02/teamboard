using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Refresh;

public sealed class RefreshTokenService(
    IdentityDbContext db,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider)
{
    public async Task<IssuedRefreshToken> IssueAtLoginAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var plaintext = RefreshTokenGenerator.Generate();
        var hash = RefreshTokenHasher.Hash(plaintext);
        var expiresAt = now.AddSeconds(settings.LifetimeSeconds);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = hash,
            FamilyId = Guid.NewGuid(),
            UserId = userId,
            ClientId = settings.DefaultClientId,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(plaintext, settings.LifetimeSeconds);
    }

    public async Task<RotationWin?> TryRotateAsync(string presentedToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presentedToken))
        {
            return null;
        }

        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var hash = RefreshTokenHasher.Hash(presentedToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var winners = await db.Database.SqlQuery<RefreshRotateRow>($"""
                UPDATE refresh_token
                SET used_at = {now}
                WHERE token_hash = {hash}
                  AND used_at IS NULL
                  AND revoked_at IS NULL
                  AND expires_at > {now}
                RETURNING user_id AS "UserId", family_id AS "FamilyId", client_id AS "ClientId"
                """).ToListAsync(cancellationToken);

            if (winners.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var winner = winners[0];
            var newPlaintext = RefreshTokenGenerator.Generate();
            var newHash = RefreshTokenHasher.Hash(newPlaintext);
            var expiresAt = now.AddSeconds(settings.LifetimeSeconds);

            db.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = newHash,
                FamilyId = winner.FamilyId,
                UserId = winner.UserId,
                ClientId = winner.ClientId,
                ExpiresAt = expiresAt,
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RotationWin(winner.UserId, newPlaintext, settings.LifetimeSeconds);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed class RefreshRotateRow
    {
        public Guid UserId { get; init; }

        public Guid FamilyId { get; init; }

        public string ClientId { get; init; } = string.Empty;
    }
}

public readonly record struct IssuedRefreshToken(string Plaintext, int ExpiresInSeconds);

public readonly record struct RotationWin(Guid UserId, string RefreshPlaintext, int RefreshExpiresInSeconds);