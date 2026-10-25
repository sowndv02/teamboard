using System.Text.Json;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Reqnroll;
using Xunit;

namespace IdentityService.Tests;

[Binding]
public sealed class IdentityServiceSteps : IAsyncDisposable
{
    private const string ConnectionKey = "ConnectionStrings:IdentityDb";
    private const string MigrateKey = "Database:MigrateOnStartup";
    private const string ThrowPath = "/__test/throw";
    private const string ThrowMessage = "boom: chi tiết nội bộ không được lộ";

    // Cổng 1 không có ai lắng nghe: mô phỏng DB không chạy mà không cần container.
    private const string UnreachableConnection =
        "Host=127.0.0.1;Port=1;Database=identity;Username=identity;Password=unused-test-value;Timeout=1";

    private readonly List<WebApplicationFactory<Program>> _factories = [];
    private PostgresDatabase? _database;
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private Exception? _thrown;
    private string _body = string.Empty;

    private PostgresDatabase Database => _database ?? throw new InvalidOperationException("Chưa có cơ sở dữ liệu.");

    [Given("identity-service được cấu hình trỏ tới một cơ sở dữ liệu không chạy")]
    public void GivenServiceWithUnreachableDatabase() => _client = StartService(UnreachableConnection, migrate: false);

    [Given("một cơ sở dữ liệu PostgreSQL trống đang chạy")]
    public async Task GivenDatabase() => _database = await PostgresDatabase.StartAsync();

    [Given("identity-service được cấu hình trỏ tới cơ sở dữ liệu đó và tự áp migration khi khởi động")]
    public void GivenServiceWithDatabase() => _client = StartService(Database.ConnectionString, migrate: true);

    [Given("cơ sở dữ liệu đã dừng")]
    public async Task GivenDatabaseStopped() => await Database.StopAsync();

    [Given("identity-service có một thao tác gây lỗi không lường trước")]
    public void GivenServiceWithFailingOperation() =>
        _client = StartService(UnreachableConnection, migrate: false, services => services.AddSingleton<IStartupFilter, ThrowingEndpointFilter>());

    [When("tôi gửi {string} tới {string}")]
    public async Task WhenSend(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [When("tôi kích hoạt thao tác gây lỗi đó")]
    public async Task WhenTriggerFailure()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ThrowPath);
            _response = await _client!.SendAsync(request);
            _body = await _response.Content.ReadAsStringAsync();
        }
        catch (Exception exception)
        {
            // Host test ném lại lỗi chưa xử lý thay vì trả 500: chưa có bộ xử lý lỗi, để bước Then báo thất bại.
            _thrown = exception;
        }
    }

    [When("identity-service khởi động hai lần liên tiếp với cơ sở dữ liệu đó và tự áp migration")]
    public async Task WhenStartTwice()
    {
        for (var run = 0; run < 2; run++)
        {
            using var client = StartService(Database.ConnectionString, migrate: true);
            await client.GetAsync("/health/live");
            await DisposeFactoriesAsync();
        }
    }

    [When("identity-service khởi động với cơ sở dữ liệu đó và không bật tự áp migration")]
    public async Task WhenStartWithoutMigration()
    {
        using var client = StartService(Database.ConnectionString, migrate: false);
        _response = await client.GetAsync("/health/live");
        _body = await _response.Content.ReadAsStringAsync();
        await DisposeFactoriesAsync();
    }

    [Then("identity-service vẫn trả mã trạng thái {int} cho kiểm tra còn sống")]
    public void ThenStillAlive(int expected) => ThenStatus(expected);

    [Then("telemetry của service mang tên {string}")]
    public void ThenTelemetryServiceName(string serviceName)
    {
        var tracerProvider = _factories[^1].Services.GetService<TracerProvider>();
        Assert.True(tracerProvider is not null, "identity-service chưa đăng ký OpenTelemetry (AddServiceDefaults).");
        Assert.Contains(tracerProvider.GetResource().Attributes, attribute => attribute.Key == "service.name" && (string?)attribute.Value == serviceName);
    }

    [Then("mã trạng thái là {int}")]
    public void ThenStatus(int expected)
    {
        Assert.True(_thrown is null, $"Yêu cầu ném lỗi chưa xử lý thay vì trả {expected}: {_thrown?.Message}");
        Assert.Equal(expected, (int)_response!.StatusCode);
    }

    [Then("nội dung phản hồi không chứa mật khẩu hay chuỗi kết nối")]
    public void ThenBodyHasNoConnectionDetails()
    {
        Assert.DoesNotContain(PostgresDatabase.Password, _body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", _body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("lỗi theo định dạng chuẩn có mã {string} và mã vết")]
    public void ThenStandardError(string code)
    {
        Assert.Equal("application/problem+json", _response!.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    [Then("lỗi có mã tương quan")]
    public void ThenHasCorrelationId()
    {
        // Chưa có middleware correlation (gateway lát 2): hiện bằng traceId, chỉ kiểm trường có mặt (plan TB-021, Confirm log #5).
        using var document = JsonDocument.Parse(_body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Then("nội dung lỗi không chứa dấu vết ngăn xếp hay tên máy")]
    public void ThenNoInternalDetails()
    {
        Assert.DoesNotContain("   at ", _body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.MachineName, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ThrowMessage, _body, StringComparison.Ordinal);
    }

    [Then("lịch sử migration của cơ sở dữ liệu có đúng một bản ghi nền")]
    public async Task ThenOneBaselineRecord()
    {
        Assert.True(await Database.HasMigrationHistoryAsync(), "Bảng lịch sử migration chưa được tạo.");
        Assert.Equal(1, await Database.CountMigrationRecordsAsync());
    }

    [Then("cơ sở dữ liệu chưa có lịch sử migration")]
    public async Task ThenNoMigrationHistory() => Assert.False(await Database.HasMigrationHistoryAsync());

    public async ValueTask DisposeAsync()
    {
        _response?.Dispose();
        _client?.Dispose();
        await DisposeFactoriesAsync();
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private HttpClient StartService(string connectionString, bool migrate, Action<IServiceCollection>? configureServices = null)
    {
        // Chuỗi kết nối và cờ migration đi qua cấu hình (biến môi trường ở môi trường thật), không hardcode trong service.
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Production như compose: ở Development ASP.NET tự thêm trang lỗi có dấu vết ngăn xếp, che mất hành vi cần kiểm.
            builder.UseEnvironment("Production");
            builder.UseSetting(ConnectionKey, connectionString);
            builder.UseSetting(MigrateKey, migrate ? "true" : "false");
            if (configureServices is not null)
            {
                builder.ConfigureServices(configureServices);
            }
        });
        _factories.Add(factory);
        return factory.CreateClient();
    }

    private async Task DisposeFactoriesAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        _factories.Clear();
    }

    /// <summary>
    /// Thêm điểm cuối ném lỗi sau pipeline của service: pipeline của app gọi tiếp khi không khớp route,
    /// nên lỗi vẫn đi qua bộ xử lý lỗi của service (nếu có).
    /// </summary>
    private sealed class ThrowingEndpointFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map(ThrowPath, branch => branch.Run(_ => throw new InvalidOperationException(ThrowMessage)));
        };
    }
}
