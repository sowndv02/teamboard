using System.Text.RegularExpressions;
using Xunit;

namespace Gateway.Tests;

/// <summary>Policy test for the gateway's own error code table (`backend-engineering.md` section 1).</summary>
public sealed partial class GatewayErrorCodesTests
{
    [Fact]
    public void All_ContainsRouteNotFound_As404()
    {
        var error = Assert.Single(GatewayErrorCodes.All, e => e.Code == "ROUTE_NOT_FOUND");

        Assert.Equal(404, error.Status);
    }

    [Theory]
    [InlineData("UPSTREAM_UNAVAILABLE", 503)]
    [InlineData("UPSTREAM_BAD_GATEWAY", 502)]
    [InlineData("UPSTREAM_TIMEOUT", 504)]
    public void All_ContainsUpstreamErrors_WithTheirStatus(string code, int status)
    {
        var error = Assert.Single(GatewayErrorCodes.All, e => e.Code == code);

        Assert.Equal(status, error.Status);
    }

    [Fact]
    public void UpstreamErrors_MessagesRevealNoInternals()
    {
        var upstreamErrors = GatewayErrorCodes.All.Where(e => e.Code.StartsWith("UPSTREAM_", StringComparison.Ordinal)).ToList();

        Assert.Equal(3, upstreamErrors.Count);
        Assert.All(upstreamErrors, e =>
        {
            Assert.DoesNotContain("board-service", e.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Exception", e.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("localhost", e.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(@"\d+\.\d+\.\d+\.\d+|:\d{2,5}\b", e.Message);
        });
    }

    [Fact]
    public void All_Codes_AreUniqueAndHaveMessages()
    {
        Assert.NotEmpty(GatewayErrorCodes.All);
        Assert.Equal(GatewayErrorCodes.All.Count, GatewayErrorCodes.All.Select(e => e.Code).Distinct().Count());
        Assert.All(GatewayErrorCodes.All, e => Assert.Matches(UpperSnake(), e.Code));
        Assert.All(GatewayErrorCodes.All, e =>
        {
            Assert.InRange(e.Status, 400, 599);
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
        });
    }

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$")]
    private static partial Regex UpperSnake();
}
