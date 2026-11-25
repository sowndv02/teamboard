using System.Text.Json;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests;

/// <summary>What the gateway ships with and what it refuses to start with (NFR-004; design D2 and D5).</summary>
public sealed class RateLimitConfigurationTests
{
    [Fact]
    public void ShippedLimits_Are120ReadsAnd30WritesPerMinuteInSixSegments()
    {
        using var settings = LoadSettings();

        Assert.True(settings.RootElement.TryGetProperty("RateLimiting", out var rateLimiting), "appsettings.json has no RateLimiting section.");
        foreach (var (group, permits) in new[] { ("Read", 120), ("Write", 30) })
        {
            Assert.True(rateLimiting.TryGetProperty(group, out var section), $"RateLimiting has no {group} section.");
            Assert.Equal(permits, section.GetProperty("PermitLimit").GetInt32());
            Assert.Equal(60, section.GetProperty("WindowSeconds").GetInt32());
            Assert.Equal(6, section.GetProperty("SegmentsPerWindow").GetInt32());
        }
    }

    [Fact]
    public void ShippedRoutes_UseTheMatchingRateLimiterPolicy()
    {
        using var settings = LoadSettings();

        var routes = settings.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");
        foreach (var (route, policy) in new[] { ("boards-read", "read"), ("boards-write", "write") })
        {
            Assert.True(routes.GetProperty(route).TryGetProperty("RateLimiterPolicy", out var value), $"Route {route} has no RateLimiterPolicy.");
            Assert.Equal(policy, value.GetString());
        }
    }

    // The expected fragment pins the failure to the setting under test: any other startup error would otherwise pass this test.
    [Theory]
    [InlineData("RateLimiting:Read:PermitLimit", "0", "PermitLimit")]
    [InlineData("RateLimiting:Write:PermitLimit", "-5", "PermitLimit")]
    [InlineData("RateLimiting:Write:PermitLimit", "abc", "PermitLimit")]
    [InlineData("RateLimiting:Read:WindowSeconds", "0", "WindowSeconds")]
    [InlineData("RateLimiting:Write:SegmentsPerWindow", "0", "SegmentsPerWindow")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-address", "KnownProxies")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "10.0.0.0/33", "KnownIPNetworks")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "garbage", "KnownIPNetworks")]
    public void Start_WithInvalidSetting_FailsAndNamesTheSetting(string key, string value, string expectedFragment)
    {
        using var factory = FactoryWith(key, value);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(Chain(exception), e => e.Message.Contains(expectedFragment, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("RateLimiting:Read:PermitLimit", "500")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "10.0.0.5")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "2001:db8::1")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "10.0.0.0/8")]
    public void Start_WithValidSetting_Succeeds(string key, string value)
    {
        using var factory = FactoryWith(key, value);

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

    private static WebApplicationFactory<Program> FactoryWith(string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(key, value));
}
