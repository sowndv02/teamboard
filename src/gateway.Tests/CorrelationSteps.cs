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

    private string _clientTraceId = string.Empty;
    private string _clientSpanId = string.Empty;

    [Given("upstream giả luôn trả X-Correlation-Id {string}")]
    public void GivenUpstreamEchoes(string id) => context.Upstream.ResponseCorrelationId = id;

    [Given("gateway có lỗi chưa lường khi chuyển tiếp")]
    public void GivenGatewayFailsUnexpectedly() => context.StartGateway(forceUnhandledError: true);

    [When(@"(?s)^tôi gửi ""(.*)"" tới ""(.*)"" với X-Correlation-Id (.*)$")]
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

    [When("tôi gửi {string} tới {string} với hai header X-Correlation-Id {string} và {string}")]
    public async Task WhenSendWithTwoValues(string method, string path, string first, string second)
    {
        context.SentCorrelationId = first;
        await context.SendAsync(method, path, request =>
            request.Headers.TryAddWithoutValidation(CorrelationHeader, [first, second]));
    }

    [When("tôi gửi {string} tới {string} với traceparent hợp lệ")]
    public async Task WhenSendWithTraceParent(string method, string path)
    {
        _clientTraceId = Guid.NewGuid().ToString("N");
        _clientSpanId = Guid.NewGuid().ToString("N")[..16];
        await context.SendAsync(method, path, request =>
            request.Headers.TryAddWithoutValidation(TraceParentHeader, $"00-{_clientTraceId}-{_clientSpanId}-01"));
    }

    [Then("upstream giả và phản hồi nhận cùng đúng một correlation id")]
    public void ThenSameSingleId()
    {
        var upstreamId = Assert.Single(context.Upstream.ReceivedHeaders).CorrelationId;
        var responseId = ResponseId();
        Assert.False(string.IsNullOrEmpty(upstreamId));
        Assert.Equal(upstreamId, responseId);
    }

    [Then("correlation id đó là {string}")]
    public void ThenIdIs(string expectation)
    {
        var id = ResponseId();
        if (expectation == "giữ nguyên")
        {
            Assert.Equal(context.SentCorrelationId, id);
            return;
        }

        Assert.Equal("GUID mới", expectation);
        Assert.True(Guid.TryParse(id, out _), $"Không phải GUID: {id}");
        Assert.NotEqual(context.SentCorrelationId, id);
    }

    [Then("upstream giả nhận traceparent cùng trace id với client nhưng span id khác")]
    public void ThenTraceParentPropagated()
    {
        var traceParent = Assert.Single(context.Upstream.ReceivedHeaders).TraceParent;
        var parts = traceParent.Split('-');
        Assert.Equal(4, parts.Length);
        Assert.Equal(_clientTraceId, parts[1]);
        // Span ở upstream là span đi ra của HttpClient/YARP, không phải span của client.
        Assert.NotEqual(_clientSpanId, parts[2]);
    }

    [Then("correlationId trong body bằng header X-Correlation-Id của phản hồi")]
    public void ThenBodyIdEqualsHeader()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(ResponseId(), document.RootElement.GetProperty("correlationId").GetString());
    }

    [Then("header X-Correlation-Id của phản hồi là {string}")]
    public void ThenHeaderIs(string expected) => Assert.Equal(expected, ResponseId());

    [Then("phản hồi có đúng một header X-Correlation-Id là GUID")]
    public void ThenHeaderIsGuid() => Assert.True(Guid.TryParse(ResponseId(), out _));

    [Then("phản hồi có đúng một header X-Correlation-Id là {string}")]
    public void ThenSingleHeaderIs(string expected) => Assert.Equal(expected, ResponseId());

    [Then("phản hồi là 500 problem+json với mã {string}")]
    public void ThenProblem500(string code)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, context.Response!.StatusCode);
        Assert.Equal("application/problem+json", context.Response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
    }

    [Then("phản hồi không chứa tên exception hay stack trace")]
    public void ThenNoExceptionDetails()
    {
        Assert.DoesNotContain("Exception", context.Body);
        Assert.DoesNotContain("secret-forwarder-failure", context.Body);
        Assert.DoesNotContain(" at ", context.Body);
    }

    /// <summary>Đúng một giá trị X-Correlation-Id trong phản hồi (Single ném lỗi nếu 0 hoặc nhiều hơn một).</summary>
    private string ResponseId() => Assert.Single(context.Response!.Headers.GetValues(CorrelationHeader));

    /// <summary>Giải mã ký hiệu trong bảng Examples: "chuỗi" (hỗ trợ \t, \n, x*N), (không gửi) → null, (rỗng) → "".</summary>
    private static string? Decode(string token)
    {
        if (token == "(không gửi)")
        {
            return null;
        }

        if (token == "(rỗng)")
        {
            return string.Empty;
        }

        var text = token.Trim('"').Replace("\\t", "\t", StringComparison.Ordinal).Replace("\\n", "\n", StringComparison.Ordinal);
        var repeat = Regex.Match(text, @"^(.)\*(\d+)$");
        return repeat.Success ? new string(repeat.Groups[1].Value[0], int.Parse(repeat.Groups[2].Value)) : text;
    }
}
