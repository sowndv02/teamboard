using System.Text.Json;
using Gateway.Tests.Support;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class TracingSteps
{
    private const string MainCompose = "src/docker-compose.yml";
    private const string ObservabilityCompose = "src/docker-compose.observability.yml";
    private const string JaegerUrl = "http://127.0.0.1:16686";

    private readonly DockerCli _docker = new();
    private string _traceId = string.Empty;

    [Given("image của gateway và board-service đã được build bằng compose chính và observability")]
    public void GivenImagesBuilt()
    {
        _docker.UseFiles(MainCompose, ObservabilityCompose);
        var result = _docker.ComposeBuild("gateway", "board-service");
        Assert.True(result.ExitCode == 0, $"docker compose build lỗi: {result.Error}");
    }

    [When("tôi chạy gateway, board-service và jaeger bằng compose với tên dự án riêng")]
    public void WhenUp()
    {
        var result = _docker.ComposeUp("gateway", "board-service", "jaeger");
        Assert.True(result.ExitCode == 0, $"docker compose up lỗi: {result.Error}");
    }

    [When("tôi gọi {string} qua cổng host 8080 với traceparent do test sinh")]
    public async Task WhenCallWithTraceParent(string request)
    {
        var path = request.Split(' ', 2)[1];
        _traceId = Guid.NewGuid().ToString("N");
        var traceParent = $"00-{_traceId}-{Guid.NewGuid().ToString("N")[..16]}-01";

        var ready = await HttpWait.GetStatusUntilAsync("http://127.0.0.1:8080/health/ready", 200, TimeSpan.FromSeconds(60));
        Assert.True(ready is not null, "Gateway không sẵn sàng sau 60 giây.");

        // board-service có thể chưa nhận kết nối ngay sau khi gateway sẵn sàng: gọi lại tới khi gateway chuyển tiếp được (không 502/503/504).
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(60))
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:8080{path}");
            message.Headers.TryAddWithoutValidation("traceparent", traceParent);
            try
            {
                using var response = await client.SendAsync(message);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Chưa sẵn sàng: thử lại.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.Fail("Gateway không chuyển tiếp được tới board-service sau 60 giây.");
    }

    [Then("Jaeger có trace theo trace id đó chứa span của service {string} và {string}")]
    public async Task ThenTraceHasServices(string first, string second)
    {
        // Jaeger v2: GET /api/v3/traces/{traceId} (OTLP JSON); trace id do test sinh nên không khớp nhầm probe health.
        var url = $"{JaegerUrl}/api/v3/traces/{_traceId}";
        var body = await HttpWait.GetBodyUntilAsync(url, json => ServiceNames(json).IsSupersetOf([first, second]), TimeSpan.FromSeconds(90));
        Assert.True(body is not null, $"Trace {_traceId} không có đủ span của {first} và {second} trong Jaeger.");
    }

    [AfterScenario("docker")]
    public void Cleanup() => _docker.ComposeDown();

    private static HashSet<string> ServiceNames(string json)
    {
        // Jaeger v3 trả JSON OTLP: result.resourceSpans[].resource.attributes có service.name.
        var names = new HashSet<string>();
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("result", out var result)
            || !result.TryGetProperty("resourceSpans", out var resourceSpans))
        {
            return names;
        }

        foreach (var resourceSpan in resourceSpans.EnumerateArray())
        {
            if (!resourceSpan.TryGetProperty("resource", out var resource)
                || !resource.TryGetProperty("attributes", out var attributes))
            {
                continue;
            }

            foreach (var attribute in attributes.EnumerateArray())
            {
                if (attribute.TryGetProperty("key", out var key)
                    && key.GetString() == "service.name"
                    && attribute.TryGetProperty("value", out var value)
                    && value.TryGetProperty("stringValue", out var text)
                    && text.GetString() is { } name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }
}
