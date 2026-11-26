namespace IdentityService.Infrastructure;

internal static class ClickjackingProtectionMiddleware
{
    private const string LoginPathPrefix = "/login";
    private const string AuthorizePath = "/connect/authorize";
    private const string CspValue = "frame-ancestors 'none'";

    public static IApplicationBuilder UseIdentityClickjackingProtection(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (ShouldProtect(context.Request.Path))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.ContentSecurityPolicy = CspValue;
                    return Task.CompletedTask;
                });
            }

            await next();
        });

    private static bool ShouldProtect(PathString path) =>
        path.StartsWithSegments(LoginPathPrefix, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(AuthorizePath, StringComparison.OrdinalIgnoreCase);
}