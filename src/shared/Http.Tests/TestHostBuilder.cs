using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Teamboard.Shared.Http.Tests;

/// <summary>Host in-process tối thiểu cho unit test pipeline: correlation, ExceptionHandler, endpoint thử, thu log/scope/Activity.</summary>
internal sealed class TestHostBuilder : IAsyncDisposable
{
    public const string SecretDetail = "secret-detail-xyz";

    private readonly ActivityListener _listener;

    private TestHostBuilder()
    {
        // Hosting chỉ tạo Activity khi có listener nên test tự đăng ký (không có ServiceDefaults ở đây).
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => StoppedActivities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public ConcurrentBag<Activity> StoppedActivities { get; } = [];

    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>Header X-Correlation-Id mà endpoint thử nhìn thấy sau middleware.</summary>
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
        host.App.UseTeamboardExceptionHandler();
        host.App.UseCorrelation();
        host.App.MapGet("/ok", (HttpContext context, ILogger<TestHostBuilder> logger) =>
        {
            host.SeenHeader.Enqueue(context.Request.Headers[CorrelationHeaders.Name].ToString());
            logger.LogInformation("endpoint thử đã chạy");
            return Results.Ok();
        });
        host.App.MapGet("/boom", (ILogger<TestHostBuilder> logger) =>
        {
            logger.LogInformation("sắp ném lỗi");
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

/// <summary>Bắt mọi bản ghi log kèm các scope đang hiệu lực, để kiểm correlation và không lộ id sai.</summary>
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
