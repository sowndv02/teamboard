using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Teamboard.Shared.Http.Tests;

/// <summary>Minimal in-process host for pipeline unit tests: correlation, exception handler, probe endpoints, and capture of logs, scopes and Activity.</summary>
internal sealed class TestHostBuilder : IAsyncDisposable
{
    public const string SecretDetail = "secret-detail-xyz";

    private readonly ActivityListener _listener;

    private TestHostBuilder()
    {
        // Hosting only creates an Activity when a listener exists, so the test registers one (no ServiceDefaults here).
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>The correlationId tag of the current Activity as seen by the probe endpoint (read during the request, not after the Activity stops).</summary>
    public ConcurrentQueue<string?> SeenTag { get; } = new();

    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>The X-Correlation-Id header as seen by the probe endpoint after the middleware.</summary>
    public ConcurrentQueue<string?> SeenHeader { get; } = new();

    public WebApplication App { get; private set; } = null!;

    public HttpClient Client => App.GetTestClient();

    public static async Task<TestHostBuilder> StartAsync()
    {
        var host = new TestHostBuilder();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(host.Logs);

        host.App = builder.Build();
        // Target order per design 6.4: Correlation outside, ExceptionHandler inside (so the error log stays within the correlationId scope).
        host.App.UseCorrelation();
        host.App.UseTeamboardExceptionHandler();
        host.App.MapGet("/ok", (HttpContext context, ILogger<TestHostBuilder> logger) =>
        {
            host.SeenHeader.Enqueue(context.Request.Headers[CorrelationHeaders.Name].ToString());
            host.SeenTag.Enqueue((string?)Activity.Current?.GetTagItem("correlationId"));
            logger.LogInformation("test endpoint ran");
            return Results.Ok();
        });
        host.App.MapGet("/boom", (ILogger<TestHostBuilder> logger) =>
        {
            logger.LogInformation("about to throw");
            throw new InvalidOperationException(SecretDetail);
        });
        await host.App.StartAsync();
        return host;
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
    }
}

/// <summary>Captures every log record with its active scopes, to check correlation and that invalid ids never leak.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    internal sealed record LogEntry(string Category, LogLevel Level, string Message, string Scopes, Exception? Exception);

    private sealed class CapturingLogger(CapturingLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => owner._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopes = new List<string>();
            owner._scopes.ForEachScope(
                (scope, list) => list.Add(scope is IEnumerable<KeyValuePair<string, object?>> pairs
                    ? string.Join(',', pairs.Select(p => $"{p.Key}={p.Value}"))
                    : scope?.ToString() ?? string.Empty),
                scopes);
            owner.Entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception), string.Join('|', scopes), exception));
        }
    }
}
