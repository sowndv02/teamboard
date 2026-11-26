using IdentityService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Security;

public sealed class LoginLockoutService(
    IdentityDbContext db,
    IOptions<LockoutOptions> options,
    TimeProvider timeProvider)
{
    public async Task<LockCheckResult> CheckLockAsync(string emailNorm, string clientIp, CancellationToken cancellationToken)
    {
        var row = await db.LoginAttempts.AsNoTracking()
            .FirstOrDefaultAsync(
                attempt => attempt.EmailNorm == emailNorm && attempt.Ip == clientIp,
                cancellationToken);

        if (row?.LockedUntil is not { } lockedUntil || lockedUntil <= timeProvider.GetUtcNow())
        {
            return LockCheckResult.NotLocked;
        }

        var seconds = Math.Max(1, (int)Math.Ceiling((lockedUntil - timeProvider.GetUtcNow()).TotalSeconds));
        return LockCheckResult.Locked(seconds);
    }

    public async Task RecordSuccessAsync(string emailNorm, string clientIp, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO login_attempt (email_norm, ip, failures, locked_until, updated_at)
            VALUES ({emailNorm}, {clientIp}, 0, NULL, {now})
            ON CONFLICT (email_norm, ip) DO UPDATE SET
              failures = 0,
              locked_until = NULL,
              updated_at = {now}
            """,
            cancellationToken);
    }

    public async Task<FailureRecordResult> RecordFailureAsync(string emailNorm, string clientIp, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var lockUntil = now.AddSeconds(settings.LockoutDurationSeconds);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO login_attempt (email_norm, ip, failures, locked_until, updated_at)
            VALUES ({emailNorm}, {clientIp}, 1, NULL, {now})
            ON CONFLICT (email_norm, ip) DO UPDATE SET
              failures = CASE
                WHEN login_attempt.locked_until IS NOT NULL AND login_attempt.locked_until > {now} THEN login_attempt.failures
                ELSE login_attempt.failures + 1 END,
              locked_until = CASE
                WHEN login_attempt.locked_until IS NOT NULL AND login_attempt.locked_until > {now} THEN login_attempt.locked_until
                WHEN login_attempt.failures + 1 >= {settings.MaxFailures} THEN {lockUntil}
                ELSE NULL END,
              updated_at = {now}
            """,
            cancellationToken);

        var row = await db.LoginAttempts.AsNoTracking()
            .FirstOrDefaultAsync(
                attempt => attempt.EmailNorm == emailNorm && attempt.Ip == clientIp,
                cancellationToken);

        if (row?.LockedUntil is not { } until || until <= now)
        {
            return FailureRecordResult.NotLocked;
        }

        var seconds = Math.Max(1, (int)Math.Ceiling((until - now).TotalSeconds));
        return FailureRecordResult.Locked(seconds);
    }
}

public readonly record struct LockCheckResult(bool IsLocked, int RetryAfterSeconds)
{
    public static LockCheckResult NotLocked => new(false, 0);

    public static LockCheckResult Locked(int retryAfterSeconds) => new(true, retryAfterSeconds);
}

public readonly record struct FailureRecordResult(bool IsLocked, int RetryAfterSeconds)
{
    public static FailureRecordResult NotLocked => new(false, 0);

    public static FailureRecordResult Locked(int retryAfterSeconds) => new(true, retryAfterSeconds);
}