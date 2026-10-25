using System.Net;
using Gateway.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

/// <summary>State shared between the step classes of one scenario (Reqnroll injects it per scenario).</summary>
public sealed class GatewayContext : IAsyncDisposable
{
    private const string UpstreamAddressKey = "ReverseProxy:Clusters:board-service:Destinations:d1:Address";

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    internal UpstreamStub? UpstreamOrNull { get; set; }

    internal UpstreamStub Upstream => UpstreamOrNull ?? throw new InvalidOperationException("No fake upstream yet.");

    public HttpResponseMessage? Response { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string Method { get; private set; } = string.Empty;

    public string Path { get; private set; } = string.Empty;

    /// <summary>The X-Correlation-Id value the test sent; null means the header was not sent.</summary>
    public string? SentCorrelationId { get; set; }

    internal void StartGateway(bool forceUnhandledError = false)
    {
        // The upstream address comes from configuration (NFR-004), not hardcoded in the gateway.
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(UpstreamAddressKey, Upstream.Address);
            if (forceUnhandledError)
            {
                // Force an unexpected error in the forwarding layer to exercise the ExceptionHandler (plan A-03).
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
