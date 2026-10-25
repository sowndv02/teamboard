using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.Tests;

/// <summary>The Retry-After setting is validated when the gateway starts (backend-engineering.md section 7).</summary>
public sealed class UpstreamOptionsTests
{
    private const string SettingKey = "Gateway:Upstream:RetryAfterSeconds";

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("3601")]
    [InlineData("abc")]
    public void Start_WithInvalidRetryAfter_Fails(string value)
    {
        using var factory = FactoryWith(value);

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("5")]
    [InlineData("3600")]
    public void Start_WithValidRetryAfter_Succeeds(string value)
    {
        using var factory = FactoryWith(value);

        using var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    private static WebApplicationFactory<Program> FactoryWith(string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(SettingKey, value));
}
