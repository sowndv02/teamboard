using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Teamboard.Shared.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public const string ReadyTag = "ready";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<ServiceDefaultsOptions>()
            .Configure(options => options.ServiceName = serviceName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: [ReadyTag]);

        return builder;
    }

    /// <summary>`/health/live` không kiểm dependency; `/health/ready` kiểm các check có tag "ready".</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }
}
