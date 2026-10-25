using System.Text.Json;
using Gateway.Tests.Support;
using Xunit;

namespace Gateway.Tests;

/// <summary>The defaults the gateway ships with (NFR-003): read from appsettings.json itself, not from test overrides.</summary>
public sealed class ConfigurationTests
{
    private const string ExpectedTimeout = "00:00:30";

    [Fact]
    public void ShippedRoutes_AllowThirtySecondsForTheWholeRequest()
    {
        using var proxy = LoadReverseProxySection();

        var routes = proxy.RootElement.GetProperty("Routes").EnumerateObject().ToList();
        Assert.NotEmpty(routes);
        Assert.All(routes, route =>
        {
            Assert.True(route.Value.TryGetProperty("Timeout", out var timeout), $"Route {route.Name} has no Timeout.");
            Assert.Equal(ExpectedTimeout, timeout.GetString());
        });
    }

    [Fact]
    public void ShippedCluster_AllowsThirtySecondsOfUpstreamSilence()
    {
        using var proxy = LoadReverseProxySection();

        var cluster = proxy.RootElement.GetProperty("Clusters").GetProperty("board-service");
        Assert.True(cluster.TryGetProperty("HttpRequest", out var httpRequest), "The cluster has no HttpRequest section.");
        Assert.Equal(ExpectedTimeout, httpRequest.GetProperty("ActivityTimeout").GetString());
    }

    private static JsonDocument LoadReverseProxySection()
    {
        var path = Path.Combine(RepoRoot.Find(), "src", "gateway", "appsettings.json");
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        using var file = JsonDocument.Parse(File.ReadAllText(path), options);
        return JsonDocument.Parse(file.RootElement.GetProperty("ReverseProxy").GetRawText());
    }
}
