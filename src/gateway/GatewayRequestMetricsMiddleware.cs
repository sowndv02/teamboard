using System.Diagnostics;

namespace Gateway;

/// <summary>Records per-request latency histograms (NFR-001); uses Activity parent while the upstream child runs in <see cref="GatewayProxyTimingMiddleware"/>.</summary>
internal sealed class GatewayRequestMetricsMiddleware(RequestDelegate next, GatewayMetrics metrics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = GatewayTelemetry.ActivitySource.StartActivity("gateway.request", ActivityKind.Server);
        var stopwatch = Stopwatch.StartNew();
        await next(context);
        stopwatch.Stop();

        var upstreamSeconds = context.Items.TryGetValue(GatewayMetricKeys.UpstreamDurationSeconds, out var value) && value is double seconds
            ? seconds
            : 0;
        metrics.RecordTimings(stopwatch.Elapsed.TotalSeconds, upstreamSeconds);
    }
}