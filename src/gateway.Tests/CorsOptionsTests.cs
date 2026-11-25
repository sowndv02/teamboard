using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.Tests;

public sealed class CorsOptionsTests
{
    private static readonly CorsOptionsValidator Validator = new();

    [Fact]
    public void Validate_AbsoluteOrigin_Succeeds()
    {
        var result = Validate("http://localhost:4200");

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptyList_Succeeds()
    {
        var result = Validator.Validate(null, new CorsOptions { AllowedOrigins = [] });

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("http://localhost:4200/app")]
    [InlineData("http://localhost:4200?x=1")]
    [InlineData("localhost:4200")]
    [InlineData("")]
    public void Validate_NotAnAbsoluteOrigin_Fails(string origin)
    {
        var result = Validate(origin);

        Assert.True(result.Failed, $"Origin '{origin}' should be rejected at startup.");
    }

    private static ValidateOptionsResult Validate(string origin) =>
        Validator.Validate(null, new CorsOptions { AllowedOrigins = [origin] });
}
