using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Teamboard.Shared.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public const string ReadyTag = "ready";

    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        // Singleton so every probe uses the same check (no new instance and callback each time).
        builder.Services.AddSingleton<StartupHealthCheck>();
        builder.Services
            .AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: [ReadyTag]);

        builder.AddOpenTelemetryDefaults(serviceName);
        return builder;
    }

    /// <summary>`/health/live` does not check dependencies; `/health/ready` runs the checks tagged "ready".</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }

    // Do not record request headers or body in spans or logs (NFR-004, GDPR).
    private static void AddOpenTelemetryDefaults<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // ASP.NET Core's "Request starting" log includes the query string (which may contain tokens), so it is not exported to telemetry.
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

        var telemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        // Export only when an OTLP endpoint is configured; without one the service still runs normally.
        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            telemetry.UseOtlpExporter();
        }
    }
}
