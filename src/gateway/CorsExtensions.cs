using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace Gateway;

public static class CorsExtensions
{
    private static readonly string[] AllowedMethods = ["GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    private static readonly string[] AllowedHeaders = ["Authorization", "Content-Type", "X-Correlation-Id", "Idempotency-Key", "If-Match"];
    private static readonly string[] ExposedHeaders = ["X-Correlation-Id", "Retry-After", "ETag", "Location"];
    private static readonly TimeSpan PreflightMaxAge = TimeSpan.FromSeconds(600);

    /// <summary>Registers the origin allowlist (section "Cors") and the default CORS policy the gateway applies with UseCors().</summary>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services)
    {
        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsOptions>, CorsOptionsValidator>();

        services.AddCors();
        services.AddSingleton<IConfigureOptions<AspNetCorsOptions>, CorsPolicyConfigurer>();
        return services;
    }

    /// <summary>
    /// Applies the CORS policy and marks responses to requests with an Origin header as varying by Origin, so a cache
    /// does not hand one origin's Access-Control-Allow-Origin to another origin (plan TB-026, P-01).
    /// </summary>
    public static IApplicationBuilder UseGatewayCors(this IApplicationBuilder app)
    {
        app.Use((context, next) =>
        {
            if (context.Request.Headers.ContainsKey(HeaderNames.Origin))
            {
                context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Origin);
            }

            return next(context);
        });
        return app.UseCors();
    }

    // Policy configuration reads the allowlist from Options so the origins come from configuration (NFR-004), not code.
    private sealed class CorsPolicyConfigurer(IOptions<CorsOptions> corsOptions) : IConfigureOptions<AspNetCorsOptions>
    {
        public void Configure(AspNetCorsOptions options) =>
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(corsOptions.Value.AllowedOrigins)
                .WithMethods(AllowedMethods)
                .WithHeaders(AllowedHeaders)
                .WithExposedHeaders(ExposedHeaders)
                .SetPreflightMaxAge(PreflightMaxAge));
    }
}
