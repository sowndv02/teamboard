using System.Globalization;
using System.Net;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class CorsSteps(GatewayContext context)
{
    private const string Origin = "Origin";
    private const string RequestMethod = "Access-Control-Request-Method";
    private const string RequestHeaders = "Access-Control-Request-Headers";
    private const string AllowOrigin = "Access-Control-Allow-Origin";
    private const string AllowMethods = "Access-Control-Allow-Methods";
    private const string AllowHeaders = "Access-Control-Allow-Headers";
    private const string ExposeHeaders = "Access-Control-Expose-Headers";
    private const string MaxAge = "Access-Control-Max-Age";
    private const string AllowCredentials = "Access-Control-Allow-Credentials";

    [Given("the allowed origin is {string}")]
    public void GivenAllowedOrigin(string origin) => context.Settings["Cors:AllowedOrigins:0"] = origin;

    [When("a browser at origin {string} makes a preflight request for {string} to {string}")]
    public async Task WhenBrowserPreflights(string origin, string method, string path) =>
        await SendPreflightAsync(origin, method, path, client: null);

    [When("a browser at origin {string} makes a preflight request for {string} to {string} from client {string}")]
    public async Task WhenBrowserPreflightsFromClient(string origin, string method, string path, string client) =>
        await SendPreflightAsync(origin, method, path, client);

    [When("a browser at origin {string} makes a simple request for {string} to {string}")]
    public async Task WhenBrowserSendsSimple(string origin, string method, string path) =>
        await SendSimpleAsync(origin, method, path, client: null);

    [When("a browser at origin {string} makes a simple request for {string} to {string} from client {string}")]
    public async Task WhenBrowserSendsSimpleFromClient(string origin, string method, string path, string client) =>
        await SendSimpleAsync(origin, method, path, client);

    [Then("the response allows origin {string}")]
    public void ThenAllowsOrigin(string origin) =>
        Assert.Equal(origin, Assert.Single(context.Response!.Headers.GetValues(AllowOrigin)));

    [Then("the response has no Access-Control-Allow-Origin header")]
    public void ThenNoAllowOrigin() =>
        Assert.False(context.Response!.Headers.Contains(AllowOrigin), "The response carries Access-Control-Allow-Origin.");

    [Then("the origin {string} is {word} by CORS")]
    public void ThenOriginAllowedOrBlocked(string origin, string result)
    {
        switch (result)
        {
            case "allowed":
                ThenAllowsOrigin(origin);
                break;
            case "blocked":
                ThenNoAllowOrigin();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, "Expected allowed or blocked.");
        }
    }

    [Then("the response allows the methods {string}")]
    public void ThenAllowsMethods(string expected) => AssertSameList(expected, AllowMethods);

    [Then("the response allows the request headers {string}")]
    public void ThenAllowsRequestHeaders(string expected) => AssertSameList(expected, AllowHeaders);

    [Then("the response exposes the headers {string}")]
    public void ThenExposesHeaders(string expected) => AssertSameList(expected, ExposeHeaders);

    [Then("the response has a preflight max age of {int} seconds")]
    public void ThenMaxAge(int seconds) =>
        Assert.Equal(seconds.ToString(CultureInfo.InvariantCulture), Assert.Single(context.Response!.Headers.GetValues(MaxAge)));

    [Then("the response has Vary containing {string}")]
    public void ThenVaryContains(string value)
    {
        Assert.True(context.Response!.Headers.TryGetValues("Vary", out var values), "The response has no Vary header.");
        var tokens = values.SelectMany(v => v.Split(',')).Select(t => t.Trim());
        Assert.Contains(value, tokens, StringComparer.OrdinalIgnoreCase);
    }

    [Then("the response has no Access-Control-Allow-Credentials header")]
    public void ThenNoCredentials() =>
        Assert.False(context.Response!.Headers.Contains(AllowCredentials), "The response allows credentials.");

    [Then("the response never allows every origin")]
    public void ThenNeverWildcard()
    {
        if (context.Response!.Headers.TryGetValues(AllowOrigin, out var values))
        {
            Assert.DoesNotContain("*", values);
        }
    }

    private async Task SendPreflightAsync(string origin, string method, string path, string? client) =>
        await context.SendAsync("OPTIONS", path, request =>
        {
            request.Headers.TryAddWithoutValidation(Origin, origin);
            request.Headers.TryAddWithoutValidation(RequestMethod, method);
            request.Headers.TryAddWithoutValidation(RequestHeaders, "content-type");
            if (client is not null)
            {
                request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            }
        });

    private async Task SendSimpleAsync(string origin, string method, string path, string? client) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(Origin, origin);
            if (client is not null)
            {
                request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            }
        });

    // Compares the comma-separated tokens as sets: the order inside the header is not part of the contract.
    private void AssertSameList(string expected, string headerName)
    {
        var actual = string.Join(',', context.Response!.Headers.GetValues(headerName));
        var expectedTokens = expected.Split(',').Select(t => t.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualTokens = actual.Split(',').Select(t => t.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedTokens.OrderBy(t => t, StringComparer.OrdinalIgnoreCase), actualTokens.OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
    }
}
