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

    [Given("Docker khả dụng")]
    public void GivenDocker() => DockerCli.RequireDocker();

    [Given("image của gateway đã được build bằng compose chính")]
    public void GivenImageBuilt()
    {
        _docker.UseFiles(MainCompose);
        var result = _docker.ComposeBuild("gateway");
        Assert.True(result.ExitCode == 0, $"docker compose build gateway lỗi: {result.Error}");
    }

    [When("tôi chạy riêng service gateway bằng compose với tên dự án riêng")]
    public void WhenUpGateway()
    {
        _sinceUp.Restart();
        var result = _docker.ComposeUp("gateway");
        Assert.True(result.ExitCode == 0, $"docker compose up gateway lỗi: {result.Error}");
    }

    [Then("cổng host 8080 trả 200 cho {string}")]
    public async Task ThenPortOk(string path)
    {
        var status = await HttpWait.GetStatusAsync($"{GatewayUrl}{path}", TimeSpan.FromSeconds(30));
        Assert.Equal(200, status);
    }

    [Then("{string} trả 200 trong vòng 10 giây kể từ lệnh chạy")]
    public async Task ThenReadyWithinTenSeconds(string path)
    {
        var body = await HttpWait.GetBodyUntilAsync($"{GatewayUrl}{path}", _ => true, TimeSpan.FromSeconds(10) - _sinceUp.Elapsed);
        Assert.True(body is not null, $"{path} chưa sẵn sàng sau 10 giây kể từ lệnh chạy.");
        Assert.True(_sinceUp.Elapsed < TimeSpan.FromSeconds(10), $"Mất {_sinceUp.Elapsed.TotalSeconds:F1} giây.");
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();
}
