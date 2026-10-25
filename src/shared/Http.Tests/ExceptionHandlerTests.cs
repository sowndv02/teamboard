using System.Text.Json;
using Xunit;

namespace Teamboard.Shared.Http.Tests;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task UnhandledException_Returns500ProblemJsonWithoutInternals()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", document.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain(TestHostBuilder.SecretDetail, body);
        Assert.DoesNotContain(" at ", body);
    }

    [Fact]
    public async Task UnhandledException_KeepsCorrelationHeaderAndBodyId()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, "client-id-500");

        var response = await host.Client.SendAsync(request);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Header phải sống sót qua Response.Clear() của UseExceptionHandler (đặt bằng OnStarting).
        Assert.Equal("client-id-500", Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
        Assert.Equal("client-id-500", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnhandledException_IsLoggedWithCorrelationScope()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, "client-id-501");

        await host.Client.SendAsync(request);

        Assert.Contains(
            host.Logs.Entries,
            e => e.Level >= Microsoft.Extensions.Logging.LogLevel.Error && e.Exception is InvalidOperationException && e.Scopes.Contains("correlationId=client-id-501"));
    }
}
