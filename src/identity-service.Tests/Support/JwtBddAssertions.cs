using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace IdentityService.Tests.Support;

internal static class JwtBddAssertions
{
    private const string TestIssuer = "https://identity.test.local";

    public static void AssertBearerAccessTokenShape(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("access_token", out var tokenElement));
        var token = tokenElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal("Bearer", root.GetProperty("token_type").GetString());
        Assert.True(root.GetProperty("expires_in").GetInt32() > 0);
        Assert.Equal(3, token!.Split('.', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    public static void AssertNoAccessTokenProperty(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        Assert.False(document.RootElement.TryGetProperty("access_token", out _));
    }

    public static void AssertNoJwtLikeStringValues(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = property.Value.GetString() ?? string.Empty;
            Assert.NotEqual(3, value.Split('.', StringSplitOptions.RemoveEmptyEntries).Length);
        }
    }

    public static void AssertNoPrivateKeyPem(string responseBody)
    {
        Assert.DoesNotContain("BEGIN RSA PRIVATE KEY", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", responseBody, StringComparison.Ordinal);
    }

    public static JsonWebToken ParseAccessTokenFromLoginBody(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var token = document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token is missing.");
        var handler = new JsonWebTokenHandler();
        return handler.ReadJsonWebToken(token);
    }

    public static void AssertRs256HeaderWithKid(JsonWebToken token)
    {
        Assert.Equal("RS256", token.Alg);
        Assert.False(string.IsNullOrWhiteSpace(token.Kid));
    }

    public static void AssertAccessTokenKid(JsonWebToken token, string expectedKid) =>
        Assert.Equal(expectedKid, token.Kid);

    public static string GetAccessTokenStringFromLoginBody(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token is missing.");
    }

    public static void AssertLifetimeAbout(JsonWebToken token, int expectedSeconds)
    {
        var issued = token.IssuedAt;
        var expires = token.ValidTo;
        var delta = (expires - issued).TotalSeconds;
        Assert.InRange(delta, expectedSeconds - 5, expectedSeconds + 5);
    }

    public static void AssertUserClaims(JsonWebToken token, string userId, string audience)
    {
        Assert.Equal(userId, token.Subject);
        Assert.Equal(audience, token.GetPayloadValue<string>("aud"));
        Assert.Equal(TestIssuer, token.Issuer);
        Assert.Equal("user", token.GetPayloadValue<string>("token_use"));
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }

    public static string GetUserIdFromLoginBody(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("login id is missing.");
    }

    public static JsonWebToken ParseAccessTokenFromTokenBody(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var token = document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token is missing.");
        return new JsonWebTokenHandler().ReadJsonWebToken(token);
    }

    public static void AssertMachineClaims(JsonWebToken token, string clientId, string scope, string audience)
    {
        Assert.Equal(clientId, token.Subject);
        Assert.Equal(clientId, token.GetPayloadValue<string>("client_id"));
        Assert.Equal("machine", token.GetPayloadValue<string>("token_use"));
        Assert.Equal(audience, token.GetPayloadValue<string>("aud"));
        Assert.Equal(TestIssuer, token.Issuer);
        Assert.Equal(scope, token.GetPayloadValue<string>("scope"));
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }

    public static void AssertNotUserAccessToken(JsonWebToken token)
    {
        Assert.Equal("machine", token.GetPayloadValue<string>("token_use"));
        Assert.NotEqual("user", token.GetPayloadValue<string>("token_use"));
    }
}