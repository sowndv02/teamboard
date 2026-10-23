using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reqnroll;
using Xunit;

namespace Teamboard.Shared.ServiceDefaults.Tests;

[Binding]
public sealed class ServiceDefaultsSteps : IAsyncDisposable
{
    private TestHostFactory? _host;
    private HealthReport? _report;
    private readonly List<HttpResponseMessage> _responses = [];

    private TestHostFactory Host => _host ?? throw new InvalidOperationException("Chưa dựng host.");

    [Given("một host dùng AddServiceDefaults chưa khởi động")]
    public void GivenHostNotStarted() => _host = TestHostFactory.Build("service-defaults-tests");

    [Given("một host tối thiểu gọi AddServiceDefaults và MapDefaultEndpoints đã khởi động")]
    public async Task GivenMinimalHostStarted()
    {
        _host = TestHostFactory.Build("service-defaults-tests");
        await _host.StartAsync();
    }

    [Given("một host dùng AddServiceDefaults với tên service {string} đã khởi động")]
    public async Task GivenHostWithServiceNameStarted(string serviceName)
    {
        _host = TestHostFactory.Build(serviceName);
        await _host.StartAsync();
    }

    [Given("biến môi trường endpoint OTLP không được đặt")]
    public void GivenOtlpEndpointUnset() => Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

    [When("tôi chạy các health check có tag {string}")]
    public async Task WhenRunHealthChecks(string tag)
    {
        var service = Host.App.Services.GetRequiredService<HealthCheckService>();
        _report = await service.CheckHealthAsync(registration => registration.Tags.Contains(tag));
    }

    [When("tôi gọi {string}")]
    public async Task WhenCall(string request) => _responses.Add(await Send(request));

    [When("tôi gọi cả {string} và {string}")]
    public async Task WhenCallBoth(string first, string second)
    {
        _responses.Add(await Send(first));
        _responses.Add(await Send(second));
    }

    [When("tôi gửi một yêu cầu ghi dữ liệu với header Authorization {string} và body {string}")]
    public async Task WhenPostWithSecrets(string authorization, string body)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/echo")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        message.Headers.TryAddWithoutValidation("Authorization", authorization);
        _responses.Add(await Host.Client.SendAsync(message));
    }

    [Then("kết quả là Unhealthy")]
    public void ThenUnhealthy()
    {
        Assert.NotNull(_report);
        Assert.Equal(HealthStatus.Unhealthy, _report!.Status);
    }

    [Then("cả hai phản hồi có mã {int}")]
    public void ThenBothStatus(int status)
    {
        Assert.Equal(2, _responses.Count);
        Assert.All(_responses, response => Assert.Equal((HttpStatusCode)status, response.StatusCode));
    }

    [Then("mã trạng thái là {int}")]
    public void ThenStatus(int status)
    {
        var response = Assert.Single(_responses);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
    }

    [Then("có một span của request với service.name {string}")]
    public void ThenSpanHasServiceName(string serviceName)
    {
        var tracerProvider = Host.App.Services.GetRequiredService<TracerProvider>();
        tracerProvider.ForceFlush();

        Assert.Contains(Host.Spans, span => span.Kind == ActivityKind.Server);
        var attributes = tracerProvider.GetResource().Attributes;
        Assert.Contains(attributes, attribute => attribute.Key == "service.name" && (string?)attribute.Value == serviceName);
    }

    [Then("telemetry đã ghi nhận ít nhất một span")]
    public void ThenSpanRecorded()
    {
        Host.App.Services.GetRequiredService<TracerProvider>().ForceFlush();
        Assert.NotEmpty(Host.Spans);
    }

    [Then("telemetry đã ghi nhận ít nhất một metric")]
    public void ThenMetricRecorded()
    {
        Host.App.Services.GetRequiredService<MeterProvider>().ForceFlush();
        Assert.NotEmpty(Host.Metrics);
    }

    [Then("telemetry đã ghi nhận ít nhất một bản ghi log")]
    public void ThenLogRecorded()
    {
        Host.App.Services.GetRequiredService<LoggerProvider>().ForceFlush();
        Assert.NotEmpty(Host.Logs);
    }

    [Then("không span hay bản ghi log nào chứa {string}")]
    public void ThenNoTelemetryContains(string secret)
    {
        Host.App.Services.GetRequiredService<TracerProvider>().ForceFlush();
        Host.App.Services.GetRequiredService<LoggerProvider>().ForceFlush();

        foreach (var span in Host.Spans)
        {
            Assert.DoesNotContain(secret, string.Join('|', SpanTexts(span)));
        }

        foreach (var log in Host.Logs)
        {
            Assert.DoesNotContain(secret, string.Join('|', LogTexts(log)));
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var response in _responses)
        {
            response.Dispose();
        }

        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }

    private async Task<HttpResponseMessage> Send(string request)
    {
        var (method, path) = Split(request);
        return await Host.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
    }

    private static (string Method, string Path) Split(string request)
    {
        var parts = request.Split(' ', 2);
        return (parts[0], parts[1]);
    }

    private static IEnumerable<string> SpanTexts(Activity span)
    {
        yield return span.DisplayName;
        foreach (var tag in span.TagObjects)
        {
            yield return $"{tag.Key}={tag.Value}";
        }

        foreach (var evt in span.Events)
        {
            yield return evt.Name;
            foreach (var tag in evt.Tags)
            {
                yield return $"{tag.Key}={tag.Value}";
            }
        }

        foreach (var baggage in span.Baggage)
        {
            yield return $"{baggage.Key}={baggage.Value}";
        }
    }

    private static IEnumerable<string> LogTexts(LogRecord log)
    {
        yield return log.Body ?? string.Empty;
        yield return log.FormattedMessage ?? string.Empty;
        yield return log.Exception?.ToString() ?? string.Empty;
        foreach (var attribute in log.Attributes ?? [])
        {
            yield return $"{attribute.Key}={attribute.Value}";
        }
    }
}
