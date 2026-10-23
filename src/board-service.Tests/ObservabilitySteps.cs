using System.Text.Json;
using BoardService.Tests.Support;
using Reqnroll;
using Xunit;

namespace BoardService.Tests;

[Binding]
public sealed class ObservabilitySteps
{
    private const string MainCompose = "src/docker-compose.yml";
    private const string ObservabilityCompose = "src/docker-compose.observability.yml";

    private readonly DockerCli _docker = new();

    [Given("compose chính và compose observability đang chạy với tên dự án riêng")]
    public void GivenComposeRunning()
    {
        var result = _docker.ComposeUp(MainCompose, ObservabilityCompose);
        Assert.True(result.ExitCode == 0, $"docker compose up lỗi: {result.Error}");
    }

    [When("tôi gọi {string} tới cổng host 6060")]
    public async Task WhenCallHost(string request)
    {
        var path = request.Split(' ', 2)[1];
        var status = await HttpWait.GetStatusAsync($"http://127.0.0.1:6060{path}", TimeSpan.FromSeconds(60));
        Assert.Equal(200, status);
    }

    [Then("Jaeger có trace của service {string} trong thời gian chờ")]
    public async Task ThenJaegerHasTrace(string service)
    {
        // Jaeger 2.x chỉ còn API v3 (/api/traces của v1 trả 404) và bắt buộc khoảng thời gian tìm kiếm.
        var from = DateTime.UtcNow.AddMinutes(-15).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var url = $"http://127.0.0.1:16686/api/v3/traces?query.service_name={service}"
            + $"&query.start_time_min={from}&query.start_time_max={to}&query.num_traces=1";
        var body = await HttpWait.GetBodyUntilAsync(url, json => HasTrace(json, service), TimeSpan.FromSeconds(90));
        Assert.True(body is not null, $"Jaeger không có trace của {service}.");
    }

    [Then("Jaeger và cổng OTLP chỉ bind 127.0.0.1")]
    public void ThenBoundToLoopback()
    {
        var result = _docker.Compose(TimeSpan.FromMinutes(1), "config", "--format", "json");
        Assert.True(result.ExitCode == 0, $"docker compose config lỗi: {result.Error}");

        using var document = JsonDocument.Parse(result.Output);
        var ports = document.RootElement.GetProperty("services").GetProperty("jaeger").GetProperty("ports").EnumerateArray().ToList();
        Assert.NotEmpty(ports);
        Assert.All(ports, port => Assert.Equal("127.0.0.1", port.GetProperty("host_ip").GetString()));
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();

    private static bool HasTrace(string json, string service)
    {
        // Jaeger v3 trả JSON OTLP: result.resourceSpans[].resource.attributes có service.name.
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("result", out var result)
            || !result.TryGetProperty("resourceSpans", out var resourceSpans))
        {
            return false;
        }

        return resourceSpans.EnumerateArray().Any(resourceSpan =>
            resourceSpan.TryGetProperty("resource", out var resource)
            && resource.TryGetProperty("attributes", out var attributes)
            && attributes.EnumerateArray().Any(attribute =>
                attribute.TryGetProperty("key", out var key)
                && key.GetString() == "service.name"
                && attribute.TryGetProperty("value", out var value)
                && value.TryGetProperty("stringValue", out var text)
                && text.GetString() == service));
    }
}
