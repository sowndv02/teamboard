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

    [Given("Docker khả dụng")]
    public void GivenDocker() => DockerCli.RequireDocker();

    [Given("image của identity-service đã được build bằng compose chính")]
    public void GivenImageBuilt()
    {
        _docker.UseFiles(MainCompose);
        _docker.UseEnvFile(EnvFile);
        var result = _docker.ComposeBuild("identity-service");
        Assert.True(result.ExitCode == 0, $"docker compose build identity-service lỗi: {result.Error}");
    }

    [When("tôi chạy identity-service cùng PostgreSQL bằng compose với tên dự án riêng")]
    public void WhenUp()
    {
        _sinceUp.Restart();
        var result = _docker.ComposeUp("identity-service");
        Assert.True(result.ExitCode == 0, $"docker compose up identity-service lỗi: {result.Error}");
    }

    [Then("cổng host 6061 trả 200 cho {string} trong vòng 30 giây kể từ lệnh chạy")]
    public async Task ThenReadyWithinThirtySeconds(string path)
    {
        var status = await HttpWait.GetStatusUntilAsync($"{IdentityUrl}{path}", 200, TimeSpan.FromSeconds(30) - _sinceUp.Elapsed);
        Assert.True(status is not null, $"{path} chưa sẵn sàng sau 30 giây kể từ lệnh chạy.");
        Assert.True(_sinceUp.Elapsed < TimeSpan.FromSeconds(30), $"Mất {_sinceUp.Elapsed.TotalSeconds:F1} giây.");
    }

    [Then("người dùng cơ sở dữ liệu của identity không có quyền quản trị")]
    public void ThenDatabaseUserIsNotAdmin()
    {
        // Kết nối bằng chính user và database của identity qua socket trong container (DB không mở cổng ra host).
        var result = _docker.ComposeExec(
            "postgres", "psql", "-U", "identity", "-d", "identity", "-tAc",
            "SELECT rolsuper OR rolcreaterole OR rolcreatedb FROM pg_roles WHERE rolname = current_user");
        Assert.True(result.ExitCode == 0, $"Không kết nối được bằng user identity: {result.Error}");
        Assert.Equal("f", result.Output.Trim());
    }

    [Then("một người dùng cơ sở dữ liệu khác trong cùng instance không kết nối được vào database của identity")]
    public void ThenOtherDatabaseUserCannotConnect()
    {
        // Role thứ hai mô phỏng user của service khác dùng chung instance (task-service sau này).
        var created = _docker.ComposeExec(
            "postgres", "psql", "-U", "postgres", "-d", "postgres", "-tAc",
            "CREATE ROLE other_service LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE");
        Assert.True(created.ExitCode == 0, $"Không tạo được role thử nghiệm: {created.Error}");

        // Quyền CONNECT được kiểm bất kể cách xác thực nên socket trong container đủ để chứng minh.
        var attempt = _docker.ComposeExec("postgres", "psql", "-U", "other_service", "-d", "identity", "-tAc", "SELECT 1");
        Assert.True(attempt.ExitCode != 0, "Role khác vẫn kết nối được vào database identity.");
        Assert.Contains("permission denied", attempt.Error, StringComparison.OrdinalIgnoreCase);
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();
}
