using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
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
        builder.Services
            .AddOptions<ServiceDefaultsOptions>()
            .Configure(options => options.ServiceName = serviceName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Singleton để mọi lần probe dùng cùng một check (không tạo instance và callback mới mỗi lần).
        builder.Services.AddSingleton<StartupHealthCheck>();
        builder.Services
            .AddHealthChecks()
            .AddCheck<StartupHealthCheck>("startup", tags: [ReadyTag]);

        builder.AddOpenTelemetryDefaults(serviceName);
        return builder;
    }

    /// <summary>`/health/live` không kiểm dependency; `/health/ready` kiểm các check có tag "ready".</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }

    // Không bật ghi header/body của request vào span hay log (NFR-004, GDPR).
    private static void AddOpenTelemetryDefaults<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var telemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        // Chỉ xuất ra ngoài khi có endpoint OTLP; thiếu thì service vẫn chạy bình thường.
        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            telemetry.UseOtlpExporter();
        }
    }
}
