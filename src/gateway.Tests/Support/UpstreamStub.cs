using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Tests.Support;

/// <summary>
/// Fake upstream: a real Kestrel on a random port so YARP calls it over a real socket; records the method and path (with query) it receives.
/// </summary>
internal sealed class UpstreamStub : IAsyncDisposable
{
    public const string MarkerHeader = "X-Upstream-Stub";

    private readonly WebApplication _app;

    private UpstreamStub(WebApplication app, string address)
    {
        _app = app;
        Address = address;
    }

    public string Address { get; }

    /// <summary>Status the fake upstream returns per method, like the real API: create 201, delete 204, otherwise 200.</summary>
    public static int ExpectedStatus(string method) => method switch
    {
        "POST" => StatusCodes.Status201Created,
        "DELETE" => StatusCodes.Status204NoContent,
        _ => StatusCodes.Status200OK,
    };

    public static string ExpectedBody(string method, string path) => $"upstream:{method}:{path}";

    public ConcurrentQueue<(string Method, string PathAndQuery)> Received { get; } = new();

    /// <summary>X-Correlation-Id and traceparent headers each request received (empty string if absent; multiple values joined by commas).</summary>
    public ConcurrentQueue<(string CorrelationId, string TraceParent)> ReceivedHeaders { get; } = new();

    /// <summary>If set, the upstream returns this X-Correlation-Id (checks the gateway does not leave two values in the response).</summary>
    public string? ResponseCorrelationId { get; set; }

    public const string UpstreamTraceId = "upstream-trace-id";

    public static async Task<UpstreamStub> StartAsync(string? problemPath = null, string? problemCode = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        UpstreamStub? stub = null;
        app.Run(async context =>
        {
            stub!.Received.Enqueue((context.Request.Method, context.Request.Path + context.Request.QueryString));
            stub.ReceivedHeaders.Enqueue((
                context.Request.Headers["X-Correlation-Id"].ToString(),
                context.Request.Headers["traceparent"].ToString()));
            if (stub.ResponseCorrelationId is not null)
            {
                context.Response.Headers["X-Correlation-Id"] = stub.ResponseCorrelationId;
            }

            if (problemPath is not null && context.Request.Path == problemPath)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    $"{{\"type\":\"https://teamboard/errors/board-not-found\",\"title\":\"Board not found\",\"status\":404,\"code\":\"{problemCode}\",\"traceId\":\"{UpstreamTraceId}\",\"correlationId\":\"{UpstreamTraceId}\"}}");
                return;
            }

            context.Response.Headers[MarkerHeader] = "1";
            context.Response.StatusCode = ExpectedStatus(context.Request.Method);
            if (context.Response.StatusCode != StatusCodes.Status204NoContent)
            {
                await context.Response.WriteAsync(ExpectedBody(context.Request.Method, context.Request.Path));
            }
        });

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        stub = new UpstreamStub(app, address);
        return stub;
    }

    public Task StopAsync() => _app.StopAsync();

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
