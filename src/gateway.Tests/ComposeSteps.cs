using System.Diagnostics;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class ComposeSteps
{
    private const string MainCompose = "src/docker-compose.yml";
    private const string GatewayUrl = "http://127.0.0.1:8080";

    private readonly DockerCli _docker = new();
    private readonly Stopwatch _sinceUp = new();

    [Given("Docker is available")]
    public void GivenDocker() => DockerCli.RequireDocker();

    [Given("the gateway image has been built with the main compose file")]
    public void GivenImageBuilt()
    {
        _docker.UseFiles(MainCompose);
        var result = _docker.ComposeBuild("gateway");
        Assert.True(result.ExitCode == 0, $"docker compose build gateway failed: {result.Error}");
    }

    [When("I start only the gateway service with compose under a dedicated project name")]
    public void WhenUpGateway()
    {
        _sinceUp.Restart();
        var result = _docker.ComposeUp("gateway");
        Assert.True(result.ExitCode == 0, $"docker compose up gateway failed: {result.Error}");
    }

    [Then("host port 8080 returns 200 for {string}")]
    public async Task ThenPortOk(string path)
    {
        var status = await HttpWait.GetStatusUntilAsync($"{GatewayUrl}{path}", 200, TimeSpan.FromSeconds(30));
        Assert.Equal(200, status);
    }

    [Then("{string} returns 200 within 10 seconds of the start command")]
    public async Task ThenReadyWithinTenSeconds(string path)
    {
        var status = await HttpWait.GetStatusUntilAsync($"{GatewayUrl}{path}", 200, TimeSpan.FromSeconds(10) - _sinceUp.Elapsed);
        Assert.True(status is not null, $"{path} was not ready within 10 seconds of the start command.");
        Assert.True(_sinceUp.Elapsed < TimeSpan.FromSeconds(10), $"Took {_sinceUp.Elapsed.TotalSeconds:F1} seconds.");
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();
}
