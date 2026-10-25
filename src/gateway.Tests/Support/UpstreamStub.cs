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

    public static async Task<UpstreamStub> StartAsync(string? problemPath = null, string? problemCode = null, UpstreamBehavior behavior = UpstreamBehavior.Normal)
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

            switch (behavior)
            {
                case UpstreamBehavior.CloseBeforeHeaders:
                    // Reset the connection before any response bytes: the gateway sees a transport error, not a response.
                    context.Abort();
                    return;
                case UpstreamBehavior.HangBeforeHeaders:
                    await WaitUntilClientGoesAway(context);
                    return;
                case UpstreamBehavior.HeadersThenClose:
                    await SendHeadersAndPartialBody(context);
                    // Returning with fewer bytes than Content-Length makes Kestrel flush what was written, then drop the connection;
                    // unlike Abort() this keeps the byte order, so the headers always reach the gateway first.
                    return;
                case UpstreamBehavior.HeadersThenHang:
                    await SendHeadersAndPartialBody(context);
                    await WaitUntilClientGoesAway(context);
                    return;
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

    // The gateway drops its connection when it gives up (timeout), which cancels RequestAborted and frees this request.
    private static async Task WaitUntilClientGoesAway(HttpContext context)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Expected: the client (gateway) went away.
        }
    }

    private static async Task SendHeadersAndPartialBody(HttpContext context)
    {
        context.Response.Headers[MarkerHeader] = "1";
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentLength = 1024;
        await context.Response.WriteAsync("partial");
        await context.Response.Body.FlushAsync();
    }

    public Task StopAsync() => _app.StopAsync();

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

/// <summary>How the fake upstream misbehaves (failure injection for the downstream-error scenarios).</summary>
internal enum UpstreamBehavior
{
    Normal,
    CloseBeforeHeaders,
    HangBeforeHeaders,
    HeadersThenClose,
    HeadersThenHang,
}
