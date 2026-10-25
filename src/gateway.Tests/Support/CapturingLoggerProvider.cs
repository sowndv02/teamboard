using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Gateway.Tests.Support;

/// <summary>Captures log records with their active scopes (copy of the Http.Tests helper; shared test project is tracked debt).</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    internal sealed record LogEntry(LogLevel Level, string Message, string Scopes, Exception? Exception);

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
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
            owner.Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), string.Join('|', scopes), exception));
        }
    }
}
