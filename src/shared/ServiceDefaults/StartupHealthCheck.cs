using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Teamboard.Shared.ServiceDefaults;

/// <summary>
/// Check "ứng dụng đã sẵn sàng": không khoẻ cho tới khi host báo đã khởi động xong.
/// Service tự thêm check phụ thuộc (DB...) cùng tag "ready" khi thật sự dùng.
/// </summary>
internal sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _started;

    public StartupHealthCheck(IHostApplicationLifetime lifetime) =>
        lifetime.ApplicationStarted.Register(() => _started = true);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(_started
            ? HealthCheckResult.Healthy("Ứng dụng đã sẵn sàng.")
            : HealthCheckResult.Unhealthy("Ứng dụng chưa khởi động xong."));
}
