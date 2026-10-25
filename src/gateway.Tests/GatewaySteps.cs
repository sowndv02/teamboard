using System.Net;
using System.Text.Json;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class GatewaySteps(GatewayContext context)
{
    [Given("a fake upstream is running")]
    public async Task GivenUpstream() => context.UpstreamOrNull = await UpstreamStub.StartAsync();

    [Given("a fake upstream is running and returns 404 problem+json with code {string} for path {string}")]
    public async Task GivenUpstreamWithProblem(string code, string path) =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(path, code);

    [Given("the gateway is configured to point at the fake upstream")]
    public void GivenGatewayPointsToUpstream() => context.StartGateway();

    [Given("the fake upstream has stopped")]
    public async Task GivenUpstreamStopped() => await context.Upstream.StopAsync();

    [When("I send {string} to {string}")]
    public async Task WhenSend(string method, string path) => await context.SendAsync(method, path);

    [Then("the fake upstream receives exactly method {string} and path {string}")]
    public void ThenUpstreamReceived(string method, string path)
    {
        var received = Assert.Single(context.Upstream.Received);
        Assert.Equal(method, received.Method);
        Assert.Equal(path, received.PathAndQuery);
    }

    [Then("the upstream response is returned unchanged to the client")]
    public void ThenUpstreamResponseReturned()
    {
        Assert.Equal((HttpStatusCode)UpstreamStub.ExpectedStatus(context.Method), context.Response!.StatusCode);
        Assert.True(context.Response.Headers.Contains(UpstreamStub.MarkerHeader));
        if (context.Method != "HEAD" && context.Response.StatusCode != HttpStatusCode.NoContent)
        {
            var pathOnly = context.Path.Split('?')[0];
            Assert.Equal(UpstreamStub.ExpectedBody(context.Method, pathOnly), context.Body);
        }
    }

    [Then("the status code is {int}")]
    public void ThenStatus(int status) => Assert.Equal((HttpStatusCode)status, context.Response!.StatusCode);

    [Then("the response is 404 problem+json with code {string}")]
    public void ThenProblemNotFound(string code)
    {
        Assert.Equal(HttpStatusCode.NotFound, context.Response!.StatusCode);
        Assert.Equal("application/problem+json", context.Response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }

    [Then("the response has type and title matching the gateway error code")]
    public void ThenTypeAndTitle()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal("https://teamboard/errors/route-not-found", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Not Found", document.RootElement.GetProperty("title").GetString());
    }

    [Then("the response has traceId and correlationId")]
    public void ThenHasTraceAndCorrelation()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Then("the response contains no internal address or name of the upstream")]
    public void ThenNoInternalNames()
    {
        Assert.DoesNotContain(new Uri(context.Upstream.Address).Authority, context.Body);
        Assert.DoesNotContain("board-service", context.Body);
        Assert.DoesNotContain("Exception", context.Body);
        Assert.DoesNotContain(context.Path, context.Body);
    }

    [Then("the response keeps the content returned by the upstream")]
    public void ThenBodyIsUpstreams()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(UpstreamStub.UpstreamTraceId, document.RootElement.GetProperty("traceId").GetString());
        Assert.Single(context.Upstream.Received);
    }

    [Then("the fake upstream receives no request")]
    public void ThenUpstreamReceivedNothing() => Assert.Empty(context.Upstream.Received);
}
