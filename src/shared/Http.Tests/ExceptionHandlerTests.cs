using System.Text.Json;
using Xunit;

namespace Teamboard.Shared.Http.Tests;

public sealed class ExceptionHandlerTests
{
    private static readonly string[] AllowedProperties = ["type", "title", "status", "detail", "code", "traceId", "correlationId"];

    [Fact]
    [Trait("Scenario", "S05")]
    public async Task UnhandledException_Returns500ProblemJsonWithFullContract()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal("INTERNAL_ERROR", root.GetProperty("code").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("https://teamboard/errors/internal-error", root.GetProperty("type").GetString());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("correlationId").GetString()));
    }

    [Fact]
    [Trait("Scenario", "S05")]
    public async Task UnhandledException_BodyLeaksNoInternals()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        // Property-name allowlist: any unknown property (stack trace, exception) turns the test red whatever its name.
        using var document = JsonDocument.Parse(body);
        Assert.All(document.RootElement.EnumerateObject(), property => Assert.Contains(property.Name, AllowedProperties));
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("StackTrace", body);
        Assert.DoesNotContain("   at ", body);
        Assert.DoesNotContain("System.", body);
        Assert.DoesNotContain(TestHostBuilder.SecretDetail, body);
    }

    [Fact]
    [Trait("Scenario", "S05")]
    public async Task UnhandledException_KeepsCorrelationHeaderAndBodyId()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/boom");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, "client-id-500");

        var response = await host.Client.SendAsync(request);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // The header must survive Response.Clear() in UseExceptionHandler (set via OnStarting).
        Assert.Equal("client-id-500", Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
        Assert.Equal("client-id-500", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    [Trait("Scenario", "S05")]
    public async Task UnhandledException_WithoutClientId_BodyIdEqualsGeneratedHeader()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/boom");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var header = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.True(Guid.TryParseExact(header, "D", out _));
        Assert.Equal(header, document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    [Trait("Scenario", "S05")]
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
