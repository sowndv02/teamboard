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

        var rateLimiting = settings.RootElement.GetProperty("RateLimiting");
        foreach (var (group, permits) in new[] { ("Read", 120), ("Write", 30) })
        {
            var section = rateLimiting.GetProperty(group);
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
        Assert.Equal("read", routes.GetProperty("boards-read").GetProperty("RateLimiterPolicy").GetString());
        Assert.Equal("write", routes.GetProperty("boards-write").GetProperty("RateLimiterPolicy").GetString());
    }

    [Theory]
    [InlineData("RateLimiting:Read:PermitLimit", "0")]
    [InlineData("RateLimiting:Write:PermitLimit", "-5")]
    [InlineData("RateLimiting:Write:PermitLimit", "abc")]
    [InlineData("RateLimiting:Read:WindowSeconds", "0")]
    [InlineData("RateLimiting:Write:SegmentsPerWindow", "0")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-address")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "10.0.0.0/33")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "garbage")]
    public void Start_WithInvalidSetting_Fails(string key, string value)
    {
        using var factory = FactoryWith(key, value);

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
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

    private static JsonDocument LoadSettings()
    {
        var path = Path.Combine(RepoRoot.Find(), "src", "gateway", "appsettings.json");
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        return JsonDocument.Parse(File.ReadAllText(path), options);
    }

    private static WebApplicationFactory<Program> FactoryWith(string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(key, value));
}
