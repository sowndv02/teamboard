using System.Diagnostics.Metrics;

namespace IdentityService.Security;

public sealed class IdentityMetrics
{
    public const string MeterName = "Teamboard.Identity";

    private readonly Histogram<double> _passwordHashDurationSeconds;

    public IdentityMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _passwordHashDurationSeconds = meter.CreateHistogram<double>(
            "password_hash_duration_seconds",
            unit: "s",
            description: "Time spent hashing a password with PBKDF2.");
    }

    public void RecordPasswordHashDuration(double seconds) => _passwordHashDurationSeconds.Record(seconds);
}