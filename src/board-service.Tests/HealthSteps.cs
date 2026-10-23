using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class HealthSteps : IDisposable
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;

    [Given("board-service đã khởi động")]
    public void GivenStarted()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [When("tôi gọi {string}")]
    public async Task WhenCall(string request)
    {
        var parts = request.Split(' ', 2);
        _response = await _client!.SendAsync(new HttpRequestMessage(new HttpMethod(parts[0]), parts[1]));
    }

    [Then("mã trạng thái là {int}")]
    public void ThenStatus(int status) => Assert.Equal((HttpStatusCode)status, _response!.StatusCode);

    public void Dispose()
    {
        _response?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }
}
