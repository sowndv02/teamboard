using System.Globalization;
using System.Net;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class RateLimitSteps(GatewayContext context)
{
    private const int DefaultReadLimit = 120;
    private const int DefaultWriteLimit = 30;

    [Given("the write limit is {int} requests per window")]
    public void GivenWriteLimit(int limit) => context.Settings["RateLimiting:Write:PermitLimit"] = limit.ToString(CultureInfo.InvariantCulture);

    [Given("the read limit is {int} requests per window")]
    public void GivenReadLimit(int limit) => context.Settings["RateLimiting:Read:PermitLimit"] = limit.ToString(CultureInfo.InvariantCulture);

    [Given("the gateway trusts the proxy {string}")]
    public void GivenTrustedProxy(string address) => context.Settings["ForwardedHeaders:KnownProxies:0"] = address;

    [Given("a middleware in front of the gateway adds the header {string} with value {string}")]
    public void GivenOuterHeader(string name, string value) => context.OuterResponseHeader = (name, value);

    [Given("client {string} has made {int} {string} requests to {string}")]
    public async Task GivenClientMadeRequests(string client, int count, string method, string path) =>
        await SendManyAsync(client, count, method, path, forwardedFor: null, differentForwardedForEachTime: false);

    [Given("client {string} has used up its {word} allowance")]
    public async Task GivenClientUsedUpAllowance(string client, string group)
    {
        var (method, limit) = group switch
        {
            "write" => ("POST", Allowance("RateLimiting:Write:PermitLimit", DefaultWriteLimit)),
            "read" => ("GET", Allowance("RateLimiting:Read:PermitLimit", DefaultReadLimit)),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, "Expected read or write."),
        };
        await SendManyAsync(client, limit, method, "/api/v1/boards", forwardedFor: null, differentForwardedForEachTime: false);
    }

    [Given("client {string} has made {int} {string} requests to {string} with a different X-Forwarded-For each time")]
    public async Task GivenClientMadeRequestsWithSpoofedHeader(string client, int count, string method, string path) =>
        await SendManyAsync(client, count, method, path, forwardedFor: null, differentForwardedForEachTime: true);

    [Given("the proxy {string} has made {int} {string} requests to {string} on behalf of {string}")]
    public async Task GivenProxyMadeRequests(string proxy, int count, string method, string path, string client) =>
        await SendManyAsync(proxy, count, method, path, forwardedFor: client, differentForwardedForEachTime: false);

    [When("client {string} sends {string} to {string}")]
    public async Task WhenClientSends(string client, string method, string path) =>
        await context.SendAsync(method, path, request => request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client));

    [When("client {string} sends {string} to {string} with X-Forwarded-For {string} and X-Forwarded-Proto {string}")]
    public async Task WhenClientSendsWithForwardedHeaders(string client, string method, string path, string forwardedFor, string proto) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", proto);
        });

    [When("client {string} sends {string} to {string} with a new X-Forwarded-For")]
    public async Task WhenClientSendsWithNewForwardedFor(string client, string method, string path) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", "198.51.100.250");
        });

    [When("the proxy {string} sends {string} to {string} on behalf of {string}")]
    public async Task WhenProxySends(string proxy, string method, string path, string client) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, proxy);
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", client);
        });

    [Then("the response keeps the header {string} with value {string}")]
    public void ThenOuterHeaderKept(string name, string value) =>
        Assert.Equal(value, Assert.Single(context.Response!.Headers.GetValues(name)));

    [Then("the response does not reveal the client address {string}")]
    public void ThenNoClientAddress(string address)
    {
        Assert.DoesNotContain(address, context.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(address, string.Join(' ', context.Response!.Headers.SelectMany(h => h.Value)), StringComparison.Ordinal);
    }

    [Then("the fake upstream received X-Forwarded-For {string} and X-Forwarded-Proto {string}")]
    public void ThenUpstreamGotForwarded(string forwardedFor, string proto)
    {
        var received = Assert.Single(context.Upstream.ReceivedForwarded);
        Assert.Equal(forwardedFor, received.For);
        Assert.Equal(proto, received.Proto);
    }

    private int Allowance(string key, int fallback) =>
        context.Settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    // Requests inside the allowance must all get through, so a Given that runs into a 429 fails here instead of hiding in the When.
    private async Task SendManyAsync(string client, int count, string method, string path, string? forwardedFor, bool differentForwardedForEachTime)
    {
        for (var i = 0; i < count; i++)
        {
            var index = i;
            await context.SendAsync(method, path, request =>
            {
                request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
                if (differentForwardedForEachTime)
                {
                    request.Headers.TryAddWithoutValidation("X-Forwarded-For", $"198.51.100.{index + 1}");
                }
                else if (forwardedFor is not null)
                {
                    request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
                }
            });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, context.Response!.StatusCode);
        }
    }
}
