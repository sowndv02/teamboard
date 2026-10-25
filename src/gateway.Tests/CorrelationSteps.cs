using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class CorrelationSteps(GatewayContext context)
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceParentHeader = "traceparent";

    private static readonly string[] AllowedProblemProperties = ["type", "title", "status", "detail", "code", "traceId", "correlationId"];

    private string _clientTraceId = string.Empty;
    private string _clientSpanId = string.Empty;
    private string _sentTraceParent = string.Empty;

    private static void AssertValidTraceParent(string traceParent) =>
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$", traceParent);

    [Given("the fake upstream always returns X-Correlation-Id {string}")]
    public void GivenUpstreamEchoes(string id) => context.Upstream.ResponseCorrelationId = id;

    [Given("the gateway hits an unexpected error while forwarding")]
    public void GivenGatewayFailsUnexpectedly() => context.StartGateway(forceUnhandledError: true);

    [When(@"(?s)^I send ""(.*)"" to ""(.*)"" with X-Correlation-Id (.*)$")]
    public async Task WhenSendWithCorrelationId(string method, string path, string token)
    {
        var value = Decode(token);
        context.SentCorrelationId = value;
        await context.SendAsync(method, path, request =>
        {
            if (value is not null)
            {
                request.Headers.TryAddWithoutValidation(CorrelationHeader, value);
            }
        });
    }

    [When("I send {string} to {string} with two X-Correlation-Id headers {string} and {string}")]
    public async Task WhenSendWithTwoValues(string method, string path, string first, string second)
    {
        context.SentCorrelationId = first;
        await context.SendAsync(method, path, request =>
            request.Headers.TryAddWithoutValidation(CorrelationHeader, [first, second]));
    }

    [When("I send {string} to {string} with a valid traceparent")]
    public async Task WhenSendWithTraceParent(string method, string path)
    {
        _clientTraceId = Guid.NewGuid().ToString("N");
        _clientSpanId = Guid.NewGuid().ToString("N")[..16];
        await context.SendAsync(method, path, request =>
            request.Headers.TryAddWithoutValidation(TraceParentHeader, $"00-{_clientTraceId}-{_clientSpanId}-01"));
    }

    [Then("the fake upstream and the response get exactly the same single correlation id")]
    public void ThenSameSingleId()
    {
        var upstreamId = Assert.Single(context.Upstream.ReceivedHeaders).CorrelationId;
        var responseId = ResponseId();
        Assert.False(string.IsNullOrEmpty(upstreamId));
        Assert.Equal(upstreamId, responseId);
    }

    [Then("that correlation id is {string}")]
    public void ThenIdIs(string expectation)
    {
        var id = ResponseId();
        if (expectation == "preserved")
        {
            Assert.Equal(context.SentCorrelationId, id);
            return;
        }

        Assert.Equal("new GUID", expectation);
        Assert.True(Guid.TryParseExact(id, "D", out _), $"Not a D-format GUID: {id}");
        Assert.NotEqual(context.SentCorrelationId, id);
    }

    [When("I send {string} to {string} with traceparent {string}")]
    public async Task WhenSendWithRawTraceParent(string method, string path, string traceParent)
    {
        _sentTraceParent = traceParent;
        await context.SendAsync(method, path, request =>
            request.Headers.TryAddWithoutValidation(TraceParentHeader, traceParent));
    }

    [Then("the fake upstream receives a valid traceparent")]
    public void ThenUpstreamGotValidTraceParent() => AssertValidTraceParent(Assert.Single(context.Upstream.ReceivedHeaders).TraceParent);

    [Then("the fake upstream receives a valid traceparent different from the value the client sent")]
    public void ThenUpstreamGotNewValidTraceParent()
    {
        var received = Assert.Single(context.Upstream.ReceivedHeaders).TraceParent;
        AssertValidTraceParent(received);
        Assert.NotEqual(_sentTraceParent, received);
    }

    [Then("the fake upstream receives a traceparent with the same trace id as the client but a different span id")]
    public void ThenTraceParentPropagated()
    {
        var traceParent = Assert.Single(context.Upstream.ReceivedHeaders).TraceParent;
        var parts = traceParent.Split('-');
        Assert.Equal(4, parts.Length);
        Assert.Equal(_clientTraceId, parts[1]);
        // The upstream span is the outgoing HttpClient/YARP span, not the client span.
        Assert.NotEqual(_clientSpanId, parts[2]);
    }

    [Then("the correlationId in the body equals the X-Correlation-Id header of the response")]
    public void ThenBodyIdEqualsHeader()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(ResponseId(), document.RootElement.GetProperty("correlationId").GetString());
    }

    [Then("the response has exactly one X-Correlation-Id header that is a GUID")]
    public void ThenHeaderIsGuid() => Assert.True(Guid.TryParseExact(ResponseId(), "D", out _));

    [Then("the response has exactly one X-Correlation-Id header equal to {string}")]
    public void ThenSingleHeaderIs(string expected) => Assert.Equal(expected, ResponseId());

    [Then("the response is 500 problem+json with code {string}")]
    public void ThenProblem500(string code)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, context.Response!.StatusCode);
        Assert.Equal("application/problem+json", context.Response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
    }

    [Then("the response contains no exception name or stack trace")]
    public void ThenNoExceptionDetails()
    {
        // Property-name allowlist: any extra property (stack trace, exception) fails regardless of its name.
        using var document = JsonDocument.Parse(context.Body);
        Assert.All(document.RootElement.EnumerateObject(), property => Assert.Contains(property.Name, AllowedProblemProperties));
        Assert.DoesNotContain("Exception", context.Body);
        Assert.DoesNotContain("StackTrace", context.Body);
        Assert.DoesNotContain("   at ", context.Body);
        Assert.DoesNotContain("secret-forwarder-failure", context.Body);
    }

    /// <summary>Exactly one X-Correlation-Id value in the response (Single throws if there are zero or several).</summary>
    private string ResponseId() => Assert.Single(context.Response!.Headers.GetValues(CorrelationHeader));

    /// <summary>Decodes the Examples table notation: "text" (supports backslash-t and x*N; Gherkin already converts backslash-n and double backslash), (not sent) -> null, (empty) -> "", (arabic-indic digits) -> U+0661 U+0662 U+0663.</summary>
    private static string? Decode(string token)
    {
        if (token == "(not sent)")
        {
            return null;
        }

        // Escapes keep this file ASCII-only; the digits are non-ASCII on purpose (the id charset is ASCII).
        if (token == "(arabic-indic digits)")
        {
            return "\u0661\u0662\u0663";
        }

        if (token == "(empty)")
        {
            return string.Empty;
        }

        var text = token.Trim('"').Replace("\\t", "\t", StringComparison.Ordinal);
        var repeat = Regex.Match(text, @"^(.)\*(\d+)$");
        return repeat.Success ? new string(repeat.Groups[1].Value[0], int.Parse(repeat.Groups[2].Value)) : text;
    }
}
