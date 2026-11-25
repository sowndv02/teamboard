using Microsoft.AspNetCore.Builder;

namespace Gateway;

internal static class GatewayRequestLoggingExtensions
{
    public static IApplicationBuilder UseGatewayRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<GatewayRequestLoggingMiddleware>();
}