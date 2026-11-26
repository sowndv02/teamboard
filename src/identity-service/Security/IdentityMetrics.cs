using System.Diagnostics.Metrics;

namespace IdentityService.Security;

public sealed class IdentityMetrics
{
    public const string MeterName = "Teamboard.Identity";

    private readonly Histogram<double> _passwordHashDurationSeconds;
    private readonly Counter<long> _loginFailuresTotal;
    private readonly Counter<long> _lockoutTotal;
    private readonly Counter<long> _accessTokensIssuedTotal;
    private readonly Counter<long> _refreshTokensIssuedTotal;
    private readonly Counter<long> _refreshRotationsTotal;
    private readonly Counter<long> _refreshReuseTotal;
    private readonly Counter<long> _logoutTotal;
    private readonly Counter<long> _passwordChangeTotal;
    private readonly Counter<long> _jwksServedTotal;
    private readonly Counter<long> _machineAccessTokensIssuedTotal;

    public IdentityMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _passwordHashDurationSeconds = meter.CreateHistogram<double>(
            "password_hash_duration_seconds",
            unit: "s",
            description: "Time spent hashing a password with PBKDF2.");
        _loginFailuresTotal = meter.CreateCounter<long>(
            "login_failures_total",
            description: "Login attempts that failed credential validation.");
        _lockoutTotal = meter.CreateCounter<long>(
            "lockout_total",
            description: "Login responses blocked by an active email and IP lockout.");
        _accessTokensIssuedTotal = meter.CreateCounter<long>(
            "access_tokens_issued_total",
            description: "Access tokens successfully issued after login.");
        _refreshTokensIssuedTotal = meter.CreateCounter<long>(
            "refresh_tokens_issued_total",
            description: "Refresh tokens issued on login or after a successful refresh.");
        _refreshRotationsTotal = meter.CreateCounter<long>(
            "refresh_rotations_total",
            description: "Successful refresh token rotations.");
        _refreshReuseTotal = meter.CreateCounter<long>(
            "refresh_reuse_total",
            description: "Detected refresh token reuse after rotation.");
        _logoutTotal = meter.CreateCounter<long>(
            "logout_total",
            description: "Successful refresh session logouts.");
        _passwordChangeTotal = meter.CreateCounter<long>(
            "password_change_total",
            description: "Successful password changes.");
        _jwksServedTotal = meter.CreateCounter<long>(
            "jwks_served_total",
            description: "JWKS documents successfully served.");
        _machineAccessTokensIssuedTotal = meter.CreateCounter<long>(
            "machine_access_tokens_issued_total",
            description: "Machine access tokens issued via client credentials.");
    }

    public void RecordPasswordHashDuration(double seconds) => _passwordHashDurationSeconds.Record(seconds);

    public void RecordLoginFailure() => _loginFailuresTotal.Add(1);

    public void RecordLockout() => _lockoutTotal.Add(1);

    public void RecordAccessTokenIssued() => _accessTokensIssuedTotal.Add(1);

    public void RecordRefreshTokenIssued() => _refreshTokensIssuedTotal.Add(1);

    public void RecordRefreshRotation() => _refreshRotationsTotal.Add(1);

    public void RecordRefreshReuse() => _refreshReuseTotal.Add(1);

    public void RecordLogout() => _logoutTotal.Add(1);

    public void RecordPasswordChange() => _passwordChangeTotal.Add(1);

    public void RecordJwksServed() => _jwksServedTotal.Add(1);

    public void RecordMachineAccessTokenIssued() => _machineAccessTokensIssuedTotal.Add(1);
}