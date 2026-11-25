using Microsoft.AspNetCore.Builder;

namespace Gateway;

internal static class GatewayMetricsExtensions
{
    public static IApplicationBuilder UseGatewayRequestMetrics(this IApplicationBuilder app) =>
        app.UseMiddleware<GatewayRequestMetricsMiddleware>();

    public static IApplicationBuilder UseGatewayProxyTiming(this IApplicationBuilder app) =>
        app.UseMiddleware<GatewayProxyTimingMiddleware>();
}