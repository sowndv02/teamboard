using System.Text.Json;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests;

/// <summary>What the gateway ships with and what it refuses to start with for the body limit (FR-007, NFR-004; plan TB-025 P-04).</summary>
public sealed class BodyLimitConfigurationTests
{
    private const string Key = "Gateway:BodyLimit:MaxBytes";

    [Fact]
    public void ShippedLimit_IsOneMebibyte()
    {
        using var settings = LoadSettings();

        Assert.True(
            settings.RootElement.GetProperty("Gateway").TryGetProperty("BodyLimit", out var section),
            "appsettings.json has no Gateway:BodyLimit section.");
        Assert.Equal(1_048_576, section.GetProperty("MaxBytes").GetInt64());
    }

    // The expected fragment pins the failure to the setting under test: any other startup error would otherwise pass this test.
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("10485761")]
    public void Start_WithInvalidLimit_FailsAndNamesTheSetting(string value)
    {
        using var factory = FactoryWith(value);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(Chain(exception), e => e.Message.Contains("MaxBytes", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1048576")]
    [InlineData("10485760")]
    public void Start_WithValidLimit_Succeeds(string value)
    {
        using var factory = FactoryWith(value);

        using var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private static JsonDocument LoadSettings()
    {
        var path = Path.Combine(RepoRoot.Find(), "src", "gateway", "appsettings.json");
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        return JsonDocument.Parse(File.ReadAllText(path), options);
    }

    private static WebApplicationFactory<Program> FactoryWith(string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(Key, value));
}
