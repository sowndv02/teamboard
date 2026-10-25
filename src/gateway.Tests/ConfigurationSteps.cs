using System.Text.Json;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class ConfigurationSteps
{
    [Then("every route in appsettings.json has a timeout of {string}")]
    public void ThenRoutesHaveTimeout(string expected)
    {
        using var document = LoadReverseProxySection();
        var routes = document.RootElement.GetProperty("Routes").EnumerateObject().ToList();
        Assert.NotEmpty(routes);
        Assert.All(routes, route =>
        {
            Assert.True(route.Value.TryGetProperty("Timeout", out var timeout), $"Route {route.Name} has no Timeout.");
            Assert.Equal(expected, timeout.GetString());
        });
    }

    [Then("the cluster in appsettings.json has an activity timeout of {string}")]
    public void ThenClusterHasActivityTimeout(string expected)
    {
        using var document = LoadReverseProxySection();
        var cluster = document.RootElement.GetProperty("Clusters").GetProperty("board-service");
        Assert.True(cluster.TryGetProperty("HttpRequest", out var httpRequest), "The cluster has no HttpRequest section.");
        Assert.Equal(expected, httpRequest.GetProperty("ActivityTimeout").GetString());
    }

    // Reads the shipped file itself (not the test overrides) so the defaults the gateway ships with are what is checked.
    private static JsonDocument LoadReverseProxySection()
    {
        var path = Path.Combine(RepoRoot.Find(), "src", "gateway", "appsettings.json");
        using var file = JsonDocument.Parse(File.ReadAllText(path));
        return JsonDocument.Parse(file.RootElement.GetProperty("ReverseProxy").GetRawText());
    }
}
