using System.Net;
using System.Text.Json;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class GatewaySteps(GatewayContext context)
{
    [Given("một upstream giả đang chạy")]
    public async Task GivenUpstream() => context.UpstreamOrNull = await UpstreamStub.StartAsync();

    [Given("một upstream giả đang chạy và trả 404 problem+json với mã {string} cho đường dẫn {string}")]
    public async Task GivenUpstreamWithProblem(string code, string path) =>
        context.UpstreamOrNull = await UpstreamStub.StartAsync(path, code);

    [Given("gateway được cấu hình trỏ tới upstream giả")]
    public void GivenGatewayPointsToUpstream() => context.StartGateway();

    [Given("upstream giả đã dừng")]
    public async Task GivenUpstreamStopped() => await context.Upstream.StopAsync();

    [When("tôi gửi {string} tới {string}")]
    public async Task WhenSend(string method, string path) => await context.SendAsync(method, path);

    [Then("upstream giả nhận đúng method {string} và đường dẫn {string}")]
    public void ThenUpstreamReceived(string method, string path)
    {
        var received = Assert.Single(context.Upstream.Received);
        Assert.Equal(method, received.Method);
        Assert.Equal(path, received.PathAndQuery);
    }

    [Then("phản hồi của upstream được trả nguyên về client")]
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

    [Then("mã trạng thái là {int}")]
    public void ThenStatus(int status) => Assert.Equal((HttpStatusCode)status, context.Response!.StatusCode);

    [Then("phản hồi là 404 problem+json với mã {string}")]
    public void ThenProblemNotFound(string code)
    {
        Assert.Equal(HttpStatusCode.NotFound, context.Response!.StatusCode);
        Assert.Equal("application/problem+json", context.Response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }

    [Then("phản hồi có type và title theo mã lỗi của gateway")]
    public void ThenTypeAndTitle()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal("https://teamboard/errors/route-not-found", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Not Found", document.RootElement.GetProperty("title").GetString());
    }

    [Then("phản hồi có traceId và correlationId")]
    public void ThenHasTraceAndCorrelation()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Then("phản hồi không chứa địa chỉ hay tên nội bộ của upstream")]
    public void ThenNoInternalNames()
    {
        Assert.DoesNotContain(new Uri(context.Upstream.Address).Authority, context.Body);
        Assert.DoesNotContain("board-service", context.Body);
        Assert.DoesNotContain("Exception", context.Body);
        Assert.DoesNotContain(context.Path, context.Body);
    }

    [Then("phản hồi giữ nguyên nội dung do upstream trả")]
    public void ThenBodyIsUpstreams()
    {
        using var document = JsonDocument.Parse(context.Body);
        Assert.Equal(UpstreamStub.UpstreamTraceId, document.RootElement.GetProperty("traceId").GetString());
        Assert.Single(context.Upstream.Received);
    }

    [Then("upstream giả không nhận request nào")]
    public void ThenUpstreamReceivedNothing() => Assert.Empty(context.Upstream.Received);
}
