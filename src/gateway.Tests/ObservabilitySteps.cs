using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gateway.Tests.Support;
using Microsoft.Extensions.Logging;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class ObservabilitySteps(GatewayContext context)
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string AccessLogCategory = "Gateway.RequestLogging";

    private string? _composeGatewayBlock;
    private string? _programSource;
    private string? _runbookText;

    [When(@"client ""(.*)"" sends ""(.*)"" to ""(.*)"" with X-Correlation-Id ""(.*)""")]
    public async Task WhenClientSendsWithCorrelation(string client, string method, string path, string correlationId) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            request.Headers.TryAddWithoutValidation(CorrelationHeader, correlationId);
        });

    [When(@"client ""(.*)"" sends ""(.*)"" to ""(.*)"" with Authorization ""(.*)"" and body ""(.*)""")]
    public async Task WhenClientSendsWithAuthAndBody(string client, string method, string path, string authorization, string jsonBody)
    {
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        });
    }

    private CapturingLoggerProvider.LogEntry? _lastAccessLog;

    [Then(@"the gateway wrote an access log entry with correlationId ""(.*)"" and client IP ""(.*)""")]
    public void ThenAccessLogWithCorrelationAndIp(string correlationId, string clientIp)
    {
        _lastAccessLog = FindAccessLog(correlationId, clientIp);
        Assert.NotNull(_lastAccessLog);
    }

    [Then(@"that access log entry includes method ""(.*)"" and status {int}")]
    public void ThenAccessLogMethodAndStatus(string method, int status)
    {
        var entry = _lastAccessLog ?? throw new InvalidOperationException("No access log entry from the previous step.");
        var text = $"{entry.Message} {entry.Scopes}";
        Assert.Contains($"method={method}", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"status={status}", text, StringComparison.Ordinal);
    }

    [Then("that access log entry includes a non-empty trace id")]
    public void ThenAccessLogTraceId()
    {
        var entry = _lastAccessLog ?? throw new InvalidOperationException("No access log entry from the previous step.");
        var text = $"{entry.Message} {entry.Scopes}";
        Assert.Matches("traceId=[^|,}\\s]+", text);
    }

    [Then(@"the gateway wrote an access log entry with correlationId from the response and client IP ""(.*)""")]
    public void ThenAccessLogWithResponseCorrelationAndIp(string clientIp)
    {
        var correlationId = AssertResponseCorrelationId();
        _lastAccessLog = FindAccessLog(correlationId, clientIp);
        Assert.NotNull(_lastAccessLog);
    }

    [Then(@"no gateway log entry contains ""(.*)"", ""(.*)"" or the request body")]
    public void ThenNoSecretsInLogs(string first, string second)
    {
        var gatewayEntries = context.Logs.Entries.Where(e => IsGatewayCategory(e.Category)).ToList();
        Assert.NotEmpty(gatewayEntries);
        Assert.All(gatewayEntries, entry =>
        {
            var text = $"{entry.Message} {entry.Scopes} {entry.Exception}";
            Assert.DoesNotContain(first, text, StringComparison.Ordinal);
            Assert.DoesNotContain(second, text, StringComparison.Ordinal);
            Assert.DoesNotContain("private-body", text, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Then(@"the gateway histogram ""([^""]+)"" recorded at least one sample")]
    public void ThenHistogramRecorded(string instrumentName)
    {
        var samples = instrumentName switch
        {
            "gateway_request_duration_seconds" => context.RequestDurationSamples,
            "gateway_overhead_duration_seconds" => context.OverheadDurationSamples,
            _ => throw new ArgumentOutOfRangeException(nameof(instrumentName), instrumentName, "Unknown gateway histogram."),
        };
        Assert.True(samples >= 1, $"Expected at least one sample on {instrumentName}, saw {samples}.");
    }

    [Then(@"the gateway counter ""([^""]+)"" is at least (\d+)")]
    public void ThenCounterAtLeast(string instrumentName, int minimum) =>
        Assert.True(context.CounterTotal(instrumentName) >= minimum, $"Expected {instrumentName} >= {minimum}, saw {context.CounterTotal(instrumentName)}.");

    [When("I inspect the shipped gateway compose settings")]
    public void WhenInspectCompose() => _composeGatewayBlock = ExtractGatewayServiceBlock();

    [Then("the gateway service uses the json-file log driver with size limits")]
    public void ThenComposeLoggingConfigured()
    {
        var block = _composeGatewayBlock ?? ExtractGatewayServiceBlock();
        Assert.Contains("logging:", block, StringComparison.Ordinal);
        Assert.Contains("driver:", block, StringComparison.Ordinal);
        Assert.Contains("json-file", block, StringComparison.Ordinal);
        Assert.Contains("max-size", block, StringComparison.Ordinal);
        Assert.Contains("max-file", block, StringComparison.Ordinal);
    }

    [When("I inspect the shipped gateway host and compose settings")]
    public void WhenInspectHostAndCompose()
    {
        _composeGatewayBlock = ExtractGatewayServiceBlock();
        var programPath = Path.Combine(RepoRoot.Find(), "src", "gateway", "Program.cs");
        _programSource = File.ReadAllText(programPath);
    }

    [Then("the gateway host shutdown timeout is at least {int} seconds")]
    public void ThenHostShutdownTimeout(int minimumSeconds)
    {
        var seconds = TryReadShutdownTimeoutSeconds();
        Assert.True(seconds is not null, "Neither Program.cs nor appsettings.json sets Host shutdown timeout.");
        Assert.True(seconds >= minimumSeconds, $"ShutdownTimeout is {seconds}s, expected >= {minimumSeconds}s.");
    }

    private int? TryReadShutdownTimeoutSeconds()
    {
        var programPath = Path.Combine(RepoRoot.Find(), "src", "gateway", "Program.cs");
        var source = _programSource ?? File.ReadAllText(programPath);
        var codeMatch = Regex.Match(source, @"ShutdownTimeout\s*=\s*TimeSpan\.FromSeconds\((\d+)\)");
        if (codeMatch.Success)
        {
            return int.Parse(codeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var settingsPath = Path.Combine(RepoRoot.Find(), "src", "gateway", "appsettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        if (document.RootElement.TryGetProperty("Host", out var host)
            && host.TryGetProperty("ShutdownTimeout", out var timeoutToken))
        {
            var raw = timeoutToken.GetString();
            if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var timeout))
            {
                return (int)timeout.TotalSeconds;
            }
        }

        return null;
    }

    [Then("the gateway compose service allows {int} seconds to stop")]
    public void ThenComposeStopGrace(int seconds)
    {
        var block = _composeGatewayBlock ?? ExtractGatewayServiceBlock();
        Assert.Contains($"stop_grace_period: {seconds}s", block, StringComparison.Ordinal);
    }

    [When("I inspect the gateway operations runbook")]
    public void WhenInspectRunbook()
    {
        var path = Path.Combine(RepoRoot.Find(), "document", "runbooks", "gateway-operations.md");
        Assert.True(File.Exists(path), $"Missing runbook at {path}.");
        _runbookText = File.ReadAllText(path);
    }

    [Then("the runbook warns not to expose the gateway on the internet before JWT validation is in place")]
    public void ThenRunbookJwtWarning()
    {
        var text = _runbookText ?? File.ReadAllText(Path.Combine(RepoRoot.Find(), "document", "runbooks", "gateway-operations.md"));
        Assert.Contains("internet", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JWT", text, StringComparison.Ordinal);
        Assert.True(
            text.Contains("before", StringComparison.OrdinalIgnoreCase),
            "Runbook must state the JWT warning explicitly.");
    }

    private CapturingLoggerProvider.LogEntry? FindAccessLog(string correlationId, string clientIp)
    {
        var entries = context.Logs.Entries
            .Where(e => e.Category == AccessLogCategory && e.Level == LogLevel.Information)
            .ToList();
        return entries.FirstOrDefault(e =>
            e.Scopes.Contains($"correlationId={correlationId}", StringComparison.Ordinal)
            && (e.Message.Contains(clientIp, StringComparison.Ordinal) || e.Scopes.Contains($"clientIp={clientIp}", StringComparison.Ordinal)));
    }

    private string AssertResponseCorrelationId()
    {
        Assert.True(context.Response!.Headers.TryGetValues(CorrelationHeader, out var values), "Response has no X-Correlation-Id.");
        var headerValue = Assert.Single(values);
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue;
        }

        using var document = JsonDocument.Parse(context.Body);
        var fromBody = document.RootElement.GetProperty("correlationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(fromBody));
        return fromBody!;
    }

    private static string ExtractGatewayServiceBlock()
    {
        var path = Path.Combine(RepoRoot.Find(), "src", "docker-compose.yml");
        var lines = File.ReadAllLines(path);
        var start = Array.FindIndex(lines, line => line.TrimStart().StartsWith("gateway:", StringComparison.Ordinal));
        Assert.True(start >= 0, "docker-compose.yml has no gateway service.");
        var end = start + 1;
        while (end < lines.Length && (lines[end].StartsWith(' ') || lines[end].Length == 0))
        {
            end++;
        }

        return string.Join('\n', lines[start..end]);
    }

    private static bool IsGatewayCategory(string category) => category.StartsWith("Gateway.", StringComparison.Ordinal);
}