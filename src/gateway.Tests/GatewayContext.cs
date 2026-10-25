using System.Net;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

/// <summary>Trạng thái dùng chung giữa các lớp step của một scenario (Reqnroll tiêm theo scenario).</summary>
public sealed class GatewayContext : IAsyncDisposable
{
    private const string UpstreamAddressKey = "ReverseProxy:Clusters:board-service:Destinations:d1:Address";

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    internal UpstreamStub? UpstreamOrNull { get; set; }

    internal UpstreamStub Upstream => UpstreamOrNull ?? throw new InvalidOperationException("Chưa có upstream giả.");

    public HttpResponseMessage? Response { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string Method { get; private set; } = string.Empty;

    public string Path { get; private set; } = string.Empty;

    /// <summary>Giá trị X-Correlation-Id test đã gửi; null nghĩa là không gửi header.</summary>
    public string? SentCorrelationId { get; set; }

    internal void StartGateway(bool forceUnhandledError = false)
    {
        // Địa chỉ upstream đi qua cấu hình (NFR-004), không hardcode trong gateway.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(UpstreamAddressKey, Upstream.Address);
            if (forceUnhandledError)
            {
                // Ép lỗi chưa lường ở tầng chuyển tiếp để chứng minh ExceptionHandler (plan A-03).
                builder.ConfigureTestServices(services => services.AddSingleton<IHttpForwarder, ThrowingForwarder>());
            }
        });
        _client = _factory.CreateClient();
    }

    public async Task SendAsync(string method, string path, Action<HttpRequestMessage>? configure = null)
    {
        (Method, Path) = (method, path);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        configure?.Invoke(request);
        Response = await _client!.SendAsync(request);
        Body = await Response.Content.ReadAsStringAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Response?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        if (UpstreamOrNull is not null)
        {
            await UpstreamOrNull.DisposeAsync();
        }
    }

    private sealed class ThrowingForwarder : IHttpForwarder
    {
        public ValueTask<ForwarderError> SendAsync(HttpContext context, string destinationPrefix, HttpMessageInvoker httpClient, ForwarderRequestConfig requestConfig, HttpTransformer transformer) =>
            throw new InvalidOperationException("secret-forwarder-failure");

        public ValueTask<ForwarderError> SendAsync(HttpContext context, string destinationPrefix, HttpMessageInvoker httpClient, ForwarderRequestConfig requestConfig, HttpTransformer transformer, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("secret-forwarder-failure");
    }
}
