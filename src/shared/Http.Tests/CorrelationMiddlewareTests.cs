using System.Diagnostics;
using Xunit;

namespace Teamboard.Shared.Http.Tests;

public sealed class CorrelationMiddlewareTests
{
    private static readonly string SixtyFourChars = new('a', 64);

    [Theory]
    [InlineData("abc-123_X.9")]
    [InlineData("a")]
    public async Task Request_ValidId_IsKeptEndToEnd(string id)
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await Send(host, id);

        Assert.Equal(id, Assert.Single(host.SeenHeader));
        Assert.Equal(id, Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
    }

    [Fact]
    public async Task Request_SixtyFourCharId_IsKept()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await Send(host, SixtyFourChars);

        Assert.Equal(SixtyFourChars, Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
    }

    [Fact]
    public async Task Request_NoId_GeneratesGuid()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/ok");

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.True(Guid.TryParse(id, out _), $"Không phải GUID: {id}");
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Theory]
    [InlineData("a b")]
    [InlineData("id;drop")]
    [InlineData("<script>")]
    [InlineData("abc\n")]
    [InlineData("a\tb")]
    [InlineData("١٢٣")]
    [InlineData("")]
    public async Task Request_InvalidId_IsReplacedEverywhere(string bad)
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await Send(host, bad);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.NotEqual(bad, id);
        Assert.True(Guid.TryParse(id, out _));
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Fact]
    public async Task Request_SixtyFiveCharId_IsReplaced()
    {
        await using var host = await TestHostBuilder.StartAsync();
        var tooLong = new string('a', 65);

        var response = await Send(host, tooLong);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.NotEqual(tooLong, id);
    }

    [Fact]
    public async Task Request_TwoHeaderValues_IsReplaced()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, ["abc", "def"]);

        var response = await host.Client.SendAsync(request);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task Request_ValidId_IsTaggedOnActivityAndLogScope()
    {
        await using var host = await TestHostBuilder.StartAsync();

        await Send(host, "trace-me-1");

        Assert.Contains(host.StoppedActivities, a => (string?)a.GetTagItem("correlationId") == "trace-me-1");
        Assert.Contains(host.Logs.Entries, e => e.Message == "endpoint thử đã chạy" && e.Scopes.Contains("correlationId=trace-me-1"));
    }

    [Fact]
    public async Task Request_InvalidId_NeverAppearsInLogsScopesOrTags()
    {
        await using var host = await TestHostBuilder.StartAsync();
        const string bad = "evil;id\tinject";

        await Send(host, bad);

        Assert.DoesNotContain(host.Logs.Entries, e => e.Message.Contains("evil") || e.Scopes.Contains("evil"));
        Assert.DoesNotContain(host.StoppedActivities, a => a.TagObjects.Any(t => $"{t.Value}".Contains("evil")));
    }

    private static Task<HttpResponseMessage> Send(TestHostBuilder host, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, id);
        return host.Client.SendAsync(request);
    }
}
