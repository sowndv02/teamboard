using Xunit;

namespace Teamboard.Shared.Http.Tests;

public sealed class CorrelationMiddlewareTests
{
    [Theory]
    [Trait("Scenario", "S01")]
    [InlineData("abc-123_X.9")]
    [InlineData("a")]
    [InlineData("Zz09._-")]
    public async Task Request_ValidId_IsKeptEndToEnd(string id)
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await Send(host, id);

        Assert.Equal(id, Assert.Single(host.SeenHeader));
        Assert.Equal(id, Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
    }

    [Fact]
    [Trait("Scenario", "S01")]
    public async Task Request_SixtyFourCharId_IsKept()
    {
        await using var host = await TestHostBuilder.StartAsync();
        var id = new string('a', 64);

        var response = await Send(host, id);

        Assert.Equal(id, Assert.Single(host.SeenHeader));
        Assert.Equal(id, Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
    }

    [Fact]
    [Trait("Scenario", "S01")]
    public async Task Request_LowercaseHeaderName_IsRecognised()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation("x-correlation-id", "lower-name-1");

        var response = await host.Client.SendAsync(request);

        Assert.Equal("lower-name-1", Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name)));
    }

    [Fact]
    [Trait("Scenario", "S01")]
    public async Task Request_NoId_GeneratesGuid()
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await host.Client.GetAsync("/ok");

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.True(IsGuid(id), $"Not a D-format GUID: {id}");
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Theory]
    [Trait("Scenario", "S02")]
    [InlineData("a b")]
    [InlineData("id;drop")]
    [InlineData("<script>")]
    [InlineData("abc\n")]
    [InlineData("a\tb")]
    [InlineData("\u0661\u0662\u0663")]
    [InlineData("")]
    // Characters adjacent to the A-Z, a-z, 0-9 ranges: catch an implementation using a wrong range such as A-z or a missing anchor.
    [InlineData("a[b")]
    [InlineData("a\\b")]
    [InlineData("a]b")]
    [InlineData("a^b")]
    [InlineData("a`b")]
    [InlineData("a/b")]
    [InlineData("a:b")]
    [InlineData("a@b")]
    [InlineData("a{b")]
    [InlineData("a~b")]
    public async Task Request_InvalidId_IsReplacedEverywhere(string bad)
    {
        await using var host = await TestHostBuilder.StartAsync();

        var response = await Send(host, bad);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.NotEqual(bad, id);
        Assert.True(IsGuid(id));
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Fact]
    [Trait("Scenario", "S02")]
    public async Task Request_SixtyFiveCharId_IsReplaced()
    {
        await using var host = await TestHostBuilder.StartAsync();
        var tooLong = new string('a', 65);

        var response = await Send(host, tooLong);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.NotEqual(tooLong, id);
        Assert.True(IsGuid(id));
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Fact]
    [Trait("Scenario", "S08")]
    public async Task Request_TwoHeaderValues_IsReplaced()
    {
        await using var host = await TestHostBuilder.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, ["abc", "def"]);

        var response = await host.Client.SendAsync(request);

        var id = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.True(IsGuid(id));
        Assert.Equal(id, Assert.Single(host.SeenHeader));
    }

    [Fact]
    [Trait("Scenario", "S01")]
    public async Task Request_ValidId_IsTaggedOnActivityAndLogScope()
    {
        await using var host = await TestHostBuilder.StartAsync();

        await Send(host, "trace-me-1");

        Assert.Equal("trace-me-1", Assert.Single(host.SeenTag));
        Assert.Contains(host.Logs.Entries, e => e.Message == "test endpoint ran" && e.Scopes.Contains("correlationId=trace-me-1"));
    }

    [Fact]
    [Trait("Scenario", "S02")]
    public async Task Request_InvalidId_NeverAppearsInLogsScopesOrTags()
    {
        await using var host = await TestHostBuilder.StartAsync();
        const string bad = "evil;id\tinject";

        var response = await Send(host, bad);

        // Positive control: the replacement id must appear in tag and scope, otherwise the negative checks below prove nothing.
        var replacement = Assert.Single(response.Headers.GetValues(CorrelationHeaders.Name));
        Assert.Equal(replacement, Assert.Single(host.SeenTag));
        Assert.Contains(host.Logs.Entries, e => e.Message == "test endpoint ran" && e.Scopes.Contains($"correlationId={replacement}"));
        Assert.DoesNotContain(host.Logs.Entries, e => e.Message.Contains("evil") || e.Scopes.Contains("evil"));
        Assert.DoesNotContain(host.SeenTag, tag => tag is not null && tag.Contains("evil"));
    }

    // The plan fixes the D format (36 chars with hyphens); a generic TryParse would also accept N, B, P and X.
    private static bool IsGuid(string id) => Guid.TryParseExact(id, "D", out _);

    private static Task<HttpResponseMessage> Send(TestHostBuilder host, string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(CorrelationHeaders.Name, id);
        return host.Client.SendAsync(request);
    }
}
