using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using IdentityService.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Refresh;

public sealed class RefreshTokenService(
    IdentityDbContext db,
    IOptions<RefreshTokenOptions> options,
    TimeProvider timeProvider,
    IdentityMetrics metrics,
    ILogger<RefreshTokenService> logger)
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
                await TryHandleReuseAsync(hash, now, cancellationToken);
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

    public async Task<bool> TryLogoutAsync(string presentedToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presentedToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var hash = RefreshTokenHasher.Hash(presentedToken);

        var sessions = await db.Database.SqlQuery<RefreshLogoutRow>($"""
            SELECT user_id AS "UserId", family_id AS "FamilyId"
            FROM refresh_token
            WHERE token_hash = {hash}
              AND used_at IS NULL
              AND revoked_at IS NULL
              AND expires_at > {now}
            """).ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return false;
        }

        var session = sessions[0];
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE refresh_token
            SET revoked_at = {now}
            WHERE family_id = {session.FamilyId}
              AND revoked_at IS NULL
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        metrics.RecordLogout();
        logger.LogInformation(
            "logout event=logout UserId={UserId} FamilyId={FamilyId}",
            session.UserId,
            session.FamilyId);

        return true;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE refresh_token
            SET revoked_at = {now}
            WHERE user_id = {userId}
              AND revoked_at IS NULL
            """,
            cancellationToken);
    }

    private async Task TryHandleReuseAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var reuses = await db.Database.SqlQuery<RefreshReuseRow>($"""
            SELECT user_id AS "UserId", family_id AS "FamilyId"
            FROM refresh_token
            WHERE token_hash = {tokenHash}
              AND used_at IS NOT NULL
            """).ToListAsync(cancellationToken);

        if (reuses.Count == 0)
        {
            return;
        }

        var reuse = reuses[0];
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE refresh_token
            SET revoked_at = {now}
            WHERE family_id = {reuse.FamilyId}
              AND revoked_at IS NULL
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        metrics.RecordRefreshReuse();
        logger.LogWarning(
            "refresh_reuse event=refresh_reuse UserId={UserId} FamilyId={FamilyId}",
            reuse.UserId,
            reuse.FamilyId);
    }

    private sealed class RefreshRotateRow
    {
        public Guid UserId { get; init; }

        public Guid FamilyId { get; init; }

        public string ClientId { get; init; } = string.Empty;
    }

    private sealed class RefreshReuseRow
    {
        public Guid UserId { get; init; }

        public Guid FamilyId { get; init; }
    }

    private sealed class RefreshLogoutRow
    {
        public Guid UserId { get; init; }

        public Guid FamilyId { get; init; }
    }
}

public readonly record struct IssuedRefreshToken(string Plaintext, int ExpiresInSeconds);

public readonly record struct RotationWin(Guid UserId, string RefreshPlaintext, int RefreshExpiresInSeconds);