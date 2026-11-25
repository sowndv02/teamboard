using System.Diagnostics;

namespace Gateway;

/// <summary>Measures time spent inside the reverse proxy (YARP); child Activity under the gateway.request span (P-01).</summary>
internal sealed class GatewayProxyTimingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = GatewayTelemetry.ActivitySource.StartActivity("gateway.upstream", ActivityKind.Client);
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            context.Items[GatewayMetricKeys.UpstreamDurationSeconds] = stopwatch.Elapsed.TotalSeconds;
        }
    }
}