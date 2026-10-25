using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Teamboard.Shared.ServiceDefaults;

/// <summary>
/// Check "application is ready": unhealthy until the host reports startup has completed.
/// A service adds its own dependency checks (DB...) with the "ready" tag when it actually uses them.
/// </summary>
internal sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _started;

    public StartupHealthCheck(IHostApplicationLifetime lifetime) =>
        lifetime.ApplicationStarted.Register(() => _started = true);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(_started
            ? HealthCheckResult.Healthy("Application is ready.")
            : HealthCheckResult.Unhealthy("Application has not finished starting."));
}
