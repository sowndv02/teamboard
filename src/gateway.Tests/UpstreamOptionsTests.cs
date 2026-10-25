using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
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
    public void Start_WithInvalidRetryAfter_FailsOnOptionsValidation(string value)
    {
        using var factory = FactoryWith(value);

        var exception = Record.Exception(() => factory.CreateClient());

        // Any startup failure would pass a looser check; the cause must be the options validation itself.
        Assert.NotNull(exception);
        Assert.Contains(Chain(exception), e => e is OptionsValidationException || e.Message.Contains("RetryAfterSeconds", StringComparison.Ordinal));
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

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private static WebApplicationFactory<Program> FactoryWith(string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(SettingKey, value));
}
