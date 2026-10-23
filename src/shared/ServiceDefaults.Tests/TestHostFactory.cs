using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Teamboard.Shared.ServiceDefaults.Tests;

/// <summary>Host in-process dùng ServiceDefaults, thu span/log vào bộ nhớ để test kiểm chứng.</summary>
internal sealed class TestHostFactory : IAsyncDisposable
{
    public List<Activity> Spans { get; } = [];

    public List<LogRecord> Logs { get; } = [];

    public WebApplication App { get; private set; } = null!;

    public HttpClient Client => App.GetTestClient();

    public static TestHostFactory Build(string serviceName)
    {
        var factory = new TestHostFactory();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults(serviceName);
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(factory.Spans));
        builder.Services.ConfigureOpenTelemetryLoggerProvider(logging => logging.AddInMemoryExporter(factory.Logs));

        factory.App = builder.Build();
        factory.App.MapDefaultEndpoints();
        // Endpoint chỉ có trong test để gửi body; không phải hành vi của ServiceDefaults.
        factory.App.MapPost("/echo", async (HttpContext context) =>
        {
            await context.Request.Body.CopyToAsync(Stream.Null);
            return Results.Ok();
        });
        return factory;
    }

    public Task StartAsync() => App.StartAsync();

    public async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}
