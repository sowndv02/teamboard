using System.Text.Json;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class DownstreamComposeSteps
{
    private const string MainCompose = "src/docker-compose.yml";
    private const string GatewayUrl = "http://127.0.0.1:8080";

    // Own project name: containers left behind by other Docker tests never mix into this one.
    private readonly DockerCli _docker = new("tb023-tests");
    private string? _overrideDirectory;
    private int _status;
    private string _body = string.Empty;

    [Given("the gateway runs alone in compose with its upstream address pointing at a closed port")]
    public void GivenGatewayWithClosedUpstream()
    {
        // Overriding the address (instead of relying on the name board-service not resolving) keeps the test deterministic
        // on machines whose DNS resolves unknown names.
        _overrideDirectory = Directory.CreateTempSubdirectory("tb023-compose-").FullName;
        var overrideFile = Path.Combine(_overrideDirectory, "closed-upstream.yml");
        File.WriteAllText(
            overrideFile,
            "services:\n  gateway:\n    environment:\n      ReverseProxy__Clusters__board-service__Destinations__d1__Address: http://127.0.0.1:1\n");
        _docker.UseFiles(MainCompose, overrideFile);
        var build = _docker.ComposeBuild("gateway");
        Assert.True(build.ExitCode == 0, $"docker compose build gateway failed: {build.Error}");
        var up = _docker.ComposeUp("gateway");
        Assert.True(up.ExitCode == 0, $"docker compose up gateway failed: {up.Error}");
    }

    [When("I call {string} on host port 8080")]
    public async Task WhenCallHostPort(string request)
    {
        var path = request.Split(' ', 2)[1];
        var ready = await HttpWait.GetStatusUntilAsync($"{GatewayUrl}/health/ready", 200, TimeSpan.FromSeconds(60));
        Assert.True(ready is not null, "The gateway did not become ready within 60 seconds.");

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        using var response = await client.GetAsync($"{GatewayUrl}{path}");
        _status = (int)response.StatusCode;
        _body = await response.Content.ReadAsStringAsync();
    }

    [Then("the gateway answered on host port 8080 with status {int} and problem+json code {string}")]
    public void ThenAnswered(int status, string code)
    {
        Assert.Equal(status, _status);
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
    }

    [AfterScenario("docker")]
    public void Cleanup()
    {
        _docker.ComposeDown();
        if (_overrideDirectory is not null && Directory.Exists(_overrideDirectory))
        {
            Directory.Delete(_overrideDirectory, recursive: true);
        }
    }
}
