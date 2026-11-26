using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BoardService.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class AuthMeSteps : IDisposable
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private string? _accessToken;
    private string? _body;

    [Given("board-service JWT validation is configured for BDD tests")]
    public void GivenJwtValidationConfigured()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Given("an access token was issued for user {string}")]
    public void GivenAccessTokenForUser(string userId)
    {
        _accessToken = TestAccessTokenFactory.IssueForUser(Guid.Parse(userId));
    }

    [Given("an access token was issued for user {string} with audience {string}")]
    public void GivenAccessTokenForUserWithAudience(string userId, string audience)
    {
        _accessToken = TestAccessTokenFactory.IssueForUser(Guid.Parse(userId), audience: audience);
    }

    [Given("an expired access token was issued for user {string}")]
    public void GivenExpiredAccessTokenForUser(string userId)
    {
        _accessToken = TestAccessTokenFactory.IssueForUser(
            Guid.Parse(userId),
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [When("I send GET to {string} with that Bearer access token")]
    public async Task WhenGetWithBearer(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [When("I send GET to {string} without an Authorization header")]
    public async Task WhenGetWithoutAuth(string path)
    {
        _response = await _client!.SendAsync(new HttpRequestMessage(HttpMethod.Get, path));
        _body = await _response.Content.ReadAsStringAsync();
    }

    [Then("the auth me response status is {int}")]
    public void ThenAuthMeStatus(int status) => Assert.Equal((HttpStatusCode)status, _response!.StatusCode);

    [Then("the response body has sub {string}")]
    public void ThenBodyHasSub(string expectedSub)
    {
        using var document = JsonDocument.Parse(_body!);
        var actual = document.RootElement.GetProperty("sub").GetString();
        Assert.Equal(expectedSub, actual);
    }

    public void Dispose()
    {
        _response?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }
}