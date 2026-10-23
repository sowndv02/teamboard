using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Tests.Support;

/// <summary>
/// Upstream giả: Kestrel thật ở cổng ngẫu nhiên để YARP gọi qua socket thật; ghi lại method và đường dẫn (kèm query) nhận được.
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

    public ConcurrentQueue<(string Method, string PathAndQuery)> Received { get; } = new();

    public static async Task<UpstreamStub> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        UpstreamStub? stub = null;
        app.Run(async context =>
        {
            stub!.Received.Enqueue((context.Request.Method, context.Request.Path + context.Request.QueryString));
            context.Response.Headers[MarkerHeader] = "1";
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync($"upstream:{context.Request.Method}:{context.Request.Path}");
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
