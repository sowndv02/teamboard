using System.Globalization;
using System.Net;
using System.Text;
using Reqnroll;
using Xunit;

namespace Gateway.Tests;

[Binding]
public sealed class BodyLimitSteps(GatewayContext context)
{
    [Given("the body limit is {int} bytes")]
    public void GivenBodyLimit(int bytes) => context.Settings["Gateway:BodyLimit:MaxBytes"] = bytes.ToString(CultureInfo.InvariantCulture);

    [Given("the gateway runs on a real server socket")]
    public void GivenRealSocket() => context.UseRealKestrel = true;

    [When("I send {string} to {string} with a body of {int} bytes")]
    public async Task WhenSendBody(string method, string path, int size) =>
        await context.SendAsync(method, path, request => request.Content = new ByteArrayContent(Filler(size, "a")));

    [When("I send {string} to {string} with a body of {int} bytes made of {string}")]
    public async Task WhenSendBodyMadeOf(string method, string path, int size, string phrase) =>
        await context.SendAsync(method, path, request => request.Content = new ByteArrayContent(Filler(size, phrase)));

    [When("I send {string} to {string} with a chunked body of {int} bytes")]
    public async Task WhenSendChunkedBody(string method, string path, int size) =>
        await context.SendAsync(method, path, request =>
        {
            request.Content = new ChunkedContent(Filler(size, "a"));
            request.Headers.TransferEncodingChunked = true;
        });

    [When("client {string} sends {string} to {string} with a body of {int} bytes")]
    public async Task WhenClientSendsBody(string client, string method, string path, int size) =>
        await context.SendAsync(method, path, request =>
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpStartupFilter.HeaderName, client);
            request.Content = new ByteArrayContent(Filler(size, "a"));
        });

    [Then("the fake upstream received a request body of {int} bytes")]
    public void ThenUpstreamBody(int bytes) => Assert.Equal(bytes, Assert.Single(context.Upstream.ReceivedBodyBytes));

    [Then("the number of requests the fake upstream received is {int}")]
    public void ThenUpstreamCount(int count) => Assert.Equal(count, context.Upstream.Received.Count);

    // Every category counts: Kestrel and YARP write their own lines when a body is cut, and none may carry the content.
    [Then("no log line contains {string}")]
    public void ThenNoLogContains(string text)
    {
        Assert.NotEmpty(context.Logs.Entries);
        Assert.All(context.Logs.Entries, entry =>
            Assert.DoesNotContain(text, $"{entry.Message} {entry.Scopes} {entry.Exception}", StringComparison.Ordinal));
    }

    private static byte[] Filler(int size, string phrase)
    {
        var text = new StringBuilder(size + phrase.Length);
        while (text.Length < size)
        {
            text.Append(phrase);
        }

        return Encoding.ASCII.GetBytes(text.ToString(0, size));
    }

    /// <summary>Content whose length is unknown up front, so the client sends it with Transfer-Encoding: chunked.</summary>
    private sealed class ChunkedContent(byte[] data) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            const int pieceSize = 64 * 1024;
            for (var offset = 0; offset < data.Length; offset += pieceSize)
            {
                await stream.WriteAsync(data.AsMemory(offset, Math.Min(pieceSize, data.Length - offset)));
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
