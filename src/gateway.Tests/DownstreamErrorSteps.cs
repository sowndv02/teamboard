using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Gateway.Tests.Support;
using Microsoft.Extensions.Logging;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class DownstreamErrorSteps(GatewayContext context)
{
    private static readonly string[] AllowedProblemProperties = ["type", "title", "status", "detail", "code", "traceId", "correlationId"];
    private static readonly string[] ForbiddenTexts = ["board-service", "Exception", "StackTrace", "   at ", "127.0.0.1", "localhost"];

    [Given("a fake upstream is running that closes the connection before sending headers")]
    public async Task GivenUpstreamClosesBeforeHeaders() =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(behavior: UpstreamBehavior.CloseBeforeHeaders);

    [Given("a fake upstream is running that hangs without sending headers")]
    public async Task GivenUpstreamHangsBeforeHeaders() =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(behavior: UpstreamBehavior.HangBeforeHeaders);

    [Given("a fake upstream is running that sends headers and then closes the connection")]
    public async Task GivenUpstreamHeadersThenClose() =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(behavior: UpstreamBehavior.HeadersThenClose);

    [Given("a fake upstream is running that sends headers and then hangs")]
    public async Task GivenUpstreamHeadersThenHang() =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(behavior: UpstreamBehavior.HeadersThenHang);

    [Given("the gateway runs with no upstream instance available")]
    public void GivenGatewayWithoutDestinations()
    {
        context.WithoutDestinations = true;
        context.StartGateway();
    }

    [Given("the time allowed for the whole request is {string}")]
    public void GivenWholeRequestTime(string timeout)
    {
        context.Settings["ReverseProxy:Routes:boards-read:Timeout"] = timeout;
        context.Settings["ReverseProxy:Routes:boards-write:Timeout"] = timeout;
    }

    [Given("the time allowed for upstream silence is {string}")]
    public void GivenSilenceTime(string timeout) =>
        context.Settings["ReverseProxy:Clusters:board-service:HttpRequest:ActivityTimeout"] = timeout;

    [Given("the Retry-After setting is {string}")]
    public void GivenRetryAfterSetting(string seconds) =>
        context.Settings["Gateway:Upstream:RetryAfterSeconds"] = seconds;

    [When("I send {string} to {string} and read the whole response body")]
    public async Task WhenSendAndRead(string method, string path) => await context.SendAndReadBodyAsync(method, path);

    [When("I send {string} to {string} with Authorization {string} and body {string}")]
    public async Task WhenSendWithCredentials(string method, string path, string authorization, string body) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
            request.Content = new StringContent(body);
        });

    [Then("the gateway answers with status {int} and problem+json code {string} for {string}")]
    public void ThenGatewayAnswers(int status, string code, string method)
    {
        Assert.Equal((HttpStatusCode)status, context.Response!.StatusCode);
        if (method == "HEAD")
        {
            // A HEAD response has no body: status and headers only.
            return;
        }

        Assert.Equal("application/problem+json", context.Response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(context.Body);
        var root = document.RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.Equal(
            Assert.Single(context.Response.Headers.GetValues("X-Correlation-Id")),
            root.GetProperty("correlationId").GetString());
        Assert.All(root.EnumerateObject(), property => Assert.Contains(property.Name, AllowedProblemProperties));
    }

    [Then("the response has a Retry-After header of {int} seconds")]
    public void ThenRetryAfter(int seconds) =>
        Assert.Equal(seconds.ToString(CultureInfo.InvariantCulture), Assert.Single(context.Response!.Headers.GetValues("Retry-After")));

    [Then("the response has no Retry-After header")]
    public void ThenNoRetryAfter() => Assert.False(context.Response!.Headers.Contains("Retry-After"));

    [Then("the response reveals no host, port or internal name of the upstream")]
    public void ThenNoInternalNames()
    {
        AssertNoInternalNames(context.Body);
        AssertNoInternalNames(string.Join(' ', context.Response!.Headers.SelectMany(h => h.Value)));
    }

    [Then("the client gets an error while reading the response body")]
    public void ThenBodyReadFails() => Assert.NotNull(context.BodyReadError);

    [Then("the aborted-upstream counter is exactly {int}")]
    public async Task ThenCounter(int expected)
    {
        var reached = await WaitFor.UntilAsync(() => context.AbortedCount >= expected, TimeSpan.FromSeconds(5));
        Assert.True(reached, $"gateway_upstream_aborted is {context.AbortedCount}, expected {expected}.");
        // The gateway warning (waited for in the previous step) is written by the same code path, so by now a double count would be visible.
        Assert.Equal(expected, context.AbortedCount);
    }

    [Then("the gateway logged a warning in the correlation scope of the response")]
    public async Task ThenWarningLogged()
    {
        var id = Assert.Single(context.Response!.Headers.GetValues("X-Correlation-Id"));
        var found = await WaitFor.UntilAsync(() => GatewayWarnings(id).Any(), TimeSpan.FromSeconds(5));
        Assert.True(found, $"No Warning from a Gateway logger in scope correlationId={id} (YARP's own warnings do not count).");
    }

    [Then("the logged gateway warning reveals no host, port or internal name of the upstream")]
    public void ThenWarningRevealsNothing()
    {
        var id = Assert.Single(context.Response!.Headers.GetValues("X-Correlation-Id"));
        var warnings = GatewayWarnings(id).ToList();
        Assert.NotEmpty(warnings);
        Assert.All(warnings, w => AssertNoInternalNames(w.Message));
    }

    [Then("no log written by the gateway itself contains {string}, {string} or {string}")]
    public void ThenNoSecretsInLogs(string first, string second, string third)
    {
        var gatewayEntries = context.Logs.Entries.Where(e => IsGatewayCategory(e.Category)).ToList();
        Assert.NotEmpty(gatewayEntries);
        Assert.All(gatewayEntries, entry =>
        {
            var text = $"{entry.Message} {entry.Scopes} {entry.Exception}";
            Assert.DoesNotContain(first, text, StringComparison.Ordinal);
            Assert.DoesNotContain(second, text, StringComparison.Ordinal);
            Assert.DoesNotContain(third, text, StringComparison.Ordinal);
        });
    }

    private IEnumerable<CapturingLoggerProvider.LogEntry> GatewayWarnings(string correlationId) =>
        context.Logs.Entries.Where(e =>
            e.Level == LogLevel.Warning && IsGatewayCategory(e.Category) && e.Scopes.Contains($"correlationId={correlationId}"));

    // Loggers created by the gateway's own types (namespace Gateway), as opposed to YARP or ASP.NET Core loggers,
    // which already write warnings of their own in the same scope.
    private static bool IsGatewayCategory(string category) => category.StartsWith("Gateway.", StringComparison.Ordinal);

    private void AssertNoInternalNames(string text)
    {
        Assert.All(ForbiddenTexts, forbidden => Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase));
        if (context.UpstreamOrNull is not null)
        {
            Assert.DoesNotContain(new Uri(context.UpstreamOrNull.Address).Authority, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
