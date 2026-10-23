using Xunit;

namespace Gateway.Tests;

/// <summary>Test chính sách cho bảng mã lỗi riêng của gateway (`backend-engineering.md` mục 1).</summary>
public sealed class GatewayErrorCodesTests
{
    [Fact]
    public void All_ContainsRouteNotFound_As404()
    {
        var error = Assert.Single(GatewayErrorCodes.All, e => e.Code == "ROUTE_NOT_FOUND");

        Assert.Equal(404, error.Status);
    }

    [Fact]
    public void All_Codes_AreUniqueAndHaveMessages()
    {
        Assert.NotEmpty(GatewayErrorCodes.All);
        Assert.Equal(GatewayErrorCodes.All.Count, GatewayErrorCodes.All.Select(e => e.Code).Distinct().Count());
        Assert.All(GatewayErrorCodes.All, e =>
        {
            Assert.InRange(e.Status, 400, 599);
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
        });
    }
}
