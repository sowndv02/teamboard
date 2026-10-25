using System.Globalization;
using System.Net;
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

    [Given("the gateway runs with a cluster that has no destination")]
    public void GivenGatewayWithoutDestinations()
    {
        context.WithoutDestinations = true;
        context.StartGateway();
    }

    [Given("the route timeout is {string}")]
    public void GivenRouteTimeout(string timeout)
    {
        context.Settings["ReverseProxy:Routes:boards-read:Timeout"] = timeout;
        context.Settings["ReverseProxy:Routes:boards-write:Timeout"] = timeout;
    }

    [Given("the cluster activity timeout is {string}")]
    public void GivenActivityTimeout(string timeout) =>
        context.Settings["ReverseProxy:Clusters:board-service:HttpRequest:ActivityTimeout"] = timeout;

    [Given("the Retry-After setting is {string}")]
    public void GivenRetryAfterSetting(string seconds) =>
        context.Settings["Gateway:Upstream:RetryAfterSeconds"] = seconds;

    [When("I send {string} to {string} and read the whole response body")]
    public async Task WhenSendAndRead(string method, string path) => await context.SendAndReadBodyAsync(method, path);

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

    [Then("the response reveals no host, port or internal name of the upstream")]
    public void ThenNoInternalNames()
    {
        AssertNoInternalNames(context.Body);
        AssertNoInternalNames(string.Join(' ', context.Response!.Headers.SelectMany(h => h.Value)));
    }

    [Then("the client gets an error while reading the response body")]
    public void ThenBodyReadFails() => Assert.NotNull(context.BodyReadError);

    [Then("the aborted-upstream counter increased by {int}")]
    public async Task ThenCounter(int expected)
    {
        var reached = await WaitFor.UntilAsync(() => context.AbortedCount >= expected, TimeSpan.FromSeconds(5));
        Assert.True(reached, $"gateway_upstream_aborted is {context.AbortedCount}, expected {expected}.");
        Assert.Equal(expected, context.AbortedCount);
    }

    [Then("a warning with the correlation id of the response was logged")]
    public async Task ThenWarningLogged()
    {
        var id = Assert.Single(context.Response!.Headers.GetValues("X-Correlation-Id"));
        var found = await WaitFor.UntilAsync(
            () => context.Logs.Entries.Any(e => e.Level == LogLevel.Warning && e.Scopes.Contains($"correlationId={id}")),
            TimeSpan.FromSeconds(5));
        Assert.True(found, $"No Warning log in scope correlationId={id}.");
    }

    [Then("the logged warning reveals no host, port or internal name of the upstream")]
    public void ThenWarningRevealsNothing()
    {
        var id = Assert.Single(context.Response!.Headers.GetValues("X-Correlation-Id"));
        var warnings = context.Logs.Entries.Where(e => e.Level == LogLevel.Warning && e.Scopes.Contains($"correlationId={id}")).ToList();
        Assert.NotEmpty(warnings);
        Assert.All(warnings, w => AssertNoInternalNames(w.Message));
    }

    private static void AssertNoInternalNames(string text) =>
        Assert.All(ForbiddenTexts, forbidden => Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase));
}
