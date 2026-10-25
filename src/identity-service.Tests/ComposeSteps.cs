using System.Diagnostics;
using IdentityService.Tests.Support;
using Reqnroll;
using Xunit;

namespace IdentityService.Tests;

[Binding]
public sealed class ComposeSteps
{
    private const string MainCompose = "src/docker-compose.yml";
    private const string EnvFile = "src/.env.example";
    private const string IdentityUrl = "http://127.0.0.1:6061";

    private readonly DockerCli _docker = new();
    private readonly Stopwatch _sinceUp = new();

    [Given("Docker is available")]
    public void GivenDocker() => DockerCli.RequireDocker();

    [Given("the identity-service image is built with the main compose file")]
    public void GivenImageBuilt()
    {
        _docker.UseFiles(MainCompose);
        _docker.UseEnvFile(EnvFile);
        var result = _docker.ComposeBuild("identity-service");
        Assert.True(result.ExitCode == 0, $"docker compose build identity-service failed: {result.Error}");
    }

    [When("I run identity-service with PostgreSQL using compose with a dedicated project name")]
    public void WhenUp()
    {
        _sinceUp.Restart();
        var result = _docker.ComposeUp("identity-service");
        Assert.True(result.ExitCode == 0, $"docker compose up identity-service failed: {result.Error}");
    }

    [Then("host port 6061 returns 200 for {string} within 30 seconds of the start command")]
    public async Task ThenReadyWithinThirtySeconds(string path)
    {
        var status = await HttpWait.GetStatusUntilAsync($"{IdentityUrl}{path}", 200, TimeSpan.FromSeconds(30) - _sinceUp.Elapsed);
        Assert.True(status is not null, $"{path} was not ready 30 seconds after the start command.");
        Assert.True(_sinceUp.Elapsed < TimeSpan.FromSeconds(30), $"Took {_sinceUp.Elapsed.TotalSeconds:F1} seconds.");
    }

    [Then("the identity database user has no admin privileges")]
    public void ThenDatabaseUserIsNotAdmin()
    {
        // Connect as the identity user to the identity database over the in-container socket (the DB exposes no host port).
        var result = _docker.ComposeExec(
            "postgres", "psql", "-U", "identity", "-d", "identity", "-tAc",
            "SELECT rolsuper OR rolcreaterole OR rolcreatedb FROM pg_roles WHERE rolname = current_user");
        Assert.True(result.ExitCode == 0, $"Could not connect as the identity user: {result.Error}");
        Assert.Equal("f", result.Output.Trim());
    }

    [Then("another database user on the same instance cannot connect to the identity database")]
    public void ThenOtherDatabaseUserCannotConnect()
    {
        // A second role simulates another service's user sharing the instance (task-service later).
        var created = _docker.ComposeExec(
            "postgres", "psql", "-U", "postgres", "-d", "postgres", "-tAc",
            "CREATE ROLE other_service LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE");
        Assert.True(created.ExitCode == 0, $"Could not create the test role: {created.Error}");

        // The CONNECT privilege is checked regardless of authentication method, so the in-container socket is enough to prove it.
        var attempt = _docker.ComposeExec("postgres", "psql", "-U", "other_service", "-d", "identity", "-tAc", "SELECT 1");
        Assert.True(attempt.ExitCode != 0, "The other role can still connect to the identity database.");
        Assert.Contains("permission denied", attempt.Error, StringComparison.OrdinalIgnoreCase);
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();
}
