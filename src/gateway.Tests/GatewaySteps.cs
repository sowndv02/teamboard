using System.Net;
using System.Text.Json;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class GatewaySteps : IAsyncDisposable
{
    private const string UpstreamAddressKey = "ReverseProxy:Clusters:board-service:Destinations:d1:Address";

    private UpstreamStub? _upstream;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private string _body = string.Empty;

    private UpstreamStub Upstream => _upstream ?? throw new InvalidOperationException("Chưa có upstream giả.");

    [Given("một upstream giả đang chạy")]
    public async Task GivenUpstream() => _upstream = await UpstreamStub.StartAsync();

    [Given("gateway được cấu hình trỏ tới upstream giả")]
    public void GivenGatewayPointsToUpstream()
    {
        // Địa chỉ upstream đi qua cấu hình (NFR-004), không hardcode trong gateway.
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting(UpstreamAddressKey, Upstream.Address));
        _client = _factory.CreateClient();
    }

    [Given("upstream giả đã dừng")]
    public async Task GivenUpstreamStopped() => await Upstream.StopAsync();

    [When("tôi gửi {string} tới {string}")]
    public async Task WhenSend(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [Then("upstream giả nhận đúng method {string} và đường dẫn {string}")]
    public void ThenUpstreamReceived(string method, string path)
    {
        var received = Assert.Single(Upstream.Received);
        Assert.Equal(method, received.Method);
        Assert.Equal(path, received.PathAndQuery);
    }

    [Then("phản hồi của upstream được trả nguyên về client")]
    public void ThenUpstreamResponseReturned()
    {
        Assert.Equal(HttpStatusCode.OK, _response!.StatusCode);
        Assert.True(_response.Headers.Contains(UpstreamStub.MarkerHeader));
    }

    [Then("mã trạng thái là {int}")]
    public void ThenStatus(int status) => Assert.Equal((HttpStatusCode)status, _response!.StatusCode);

    [Then("phản hồi là 404 problem+json với mã {string}")]
    public void ThenProblemNotFound(string code)
    {
        Assert.Equal(HttpStatusCode.NotFound, _response!.StatusCode);
        Assert.Equal("application/problem+json", _response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }

    [Then("phản hồi có traceId và correlationId")]
    public void ThenHasTraceAndCorrelation()
    {
        using var document = JsonDocument.Parse(_body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Then("phản hồi không chứa địa chỉ hay tên nội bộ của upstream")]
    public void ThenNoInternalNames()
    {
        Assert.DoesNotContain(new Uri(Upstream.Address).Authority, _body);
        Assert.DoesNotContain("board-service", _body);
        Assert.DoesNotContain("Exception", _body);
    }

    [Then("upstream giả không nhận request nào")]
    public void ThenUpstreamReceivedNothing() => Assert.Empty(Upstream.Received);

    public async ValueTask DisposeAsync()
    {
        _response?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        if (_upstream is not null)
        {
            await _upstream.DisposeAsync();
        }
    }
}
