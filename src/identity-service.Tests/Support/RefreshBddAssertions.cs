using System.Text.Json;
using Xunit;

namespace IdentityService.Tests.Support;

internal static class RefreshBddAssertions
{
    public static void AssertLoginRefreshShape(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("refresh_token", out var refreshElement));
        var refreshToken = refreshElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.NotEqual(3, refreshToken!.Split('.', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.True(root.GetProperty("refresh_expires_in").GetInt32() > 0);
    }

    public static void AssertRefreshResponsePair(string responseBody)
    {
        JwtBddAssertions.AssertBearerAccessTokenShape(responseBody);
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("refresh_token", out var refreshElement));
        Assert.False(string.IsNullOrWhiteSpace(refreshElement.GetString()));
        Assert.True(root.GetProperty("refresh_expires_in").GetInt32() > 0);
    }

    public static void AssertRefreshResponseHasNoId(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        Assert.False(document.RootElement.TryGetProperty("id", out _));
    }
}