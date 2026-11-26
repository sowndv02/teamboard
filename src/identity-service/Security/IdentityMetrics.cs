using System.Diagnostics.Metrics;

namespace IdentityService.Security;

public sealed class IdentityMetrics
{
    public const string MeterName = "Teamboard.Identity";

    private readonly Histogram<double> _passwordHashDurationSeconds;
    private readonly Counter<long> _loginFailuresTotal;
    private readonly Counter<long> _lockoutTotal;
    private readonly Counter<long> _accessTokensIssuedTotal;

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
    }

    public void RecordPasswordHashDuration(double seconds) => _passwordHashDurationSeconds.Record(seconds);

    public void RecordLoginFailure() => _loginFailuresTotal.Add(1);

    public void RecordLockout() => _lockoutTotal.Add(1);

    public void RecordAccessTokenIssued() => _accessTokensIssuedTotal.Add(1);
}