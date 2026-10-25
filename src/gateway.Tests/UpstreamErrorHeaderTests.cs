using Gateway.Tests.Support;
using Xunit;

namespace Gateway.Tests;

/// <summary>
/// The error middleware must not wipe headers that other middleware set (CORS arrives in slice 6, design 6.4):
/// a browser can only read a 5xx from another origin if it still carries the CORS headers.
/// </summary>
public sealed class UpstreamErrorHeaderTests
{
    [Fact]
    public async Task UnavailableUpstream_KeepsHeadersSetByOuterMiddleware()
    {
        await using var context = new GatewayContext { OuterResponseHeader = ("X-Outer-Middleware", "kept") };
        context.UpstreamOrNull = await UpstreamStub.StartAsync();
        context.StartGateway();
        await context.Upstream.StopAsync();

        await context.SendAsync("GET", "/api/v1/boards");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, context.Response!.StatusCode);
        Assert.Equal("kept", Assert.Single(context.Response.Headers.GetValues("X-Outer-Middleware")));
    }
}
