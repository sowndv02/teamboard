using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth.Tests.Support;

internal static class TestAccessTokenFactory
{
    private const string DefaultIssuer = "http://localhost:6061";
    private const string DefaultAudience = "teamboard";
    private const string Kid = "test-active-1";

    private static readonly Lazy<JsonWebTokenHandler> Handler = new(() => new JsonWebTokenHandler());

    internal static string IssueForUser(
        Guid userId,
        string? audience = null,
        DateTimeOffset? expiresAt = null,
        string? issuer = null)
    {
        var now = DateTimeOffset.UtcNow;
        var exp = expiresAt ?? now.AddMinutes(10);
        var rsa = LoadActivePrivateKey();

        var key = new RsaSecurityKey(rsa) { KeyId = Kid };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? DefaultIssuer,
            Audience = audience ?? DefaultAudience,
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = exp.UtcDateTime,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("token_use", "user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        return Handler.Value.CreateToken(descriptor);
    }

    internal static string IssueTamperedPayloadToken(Guid userId)
    {
        var token = IssueForUser(userId);
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException("Expected JWT compact serialization.");
        }

        var payloadBytes = Base64UrlEncoder.DecodeBytes(parts[1]);
        var payload = System.Text.Encoding.UTF8.GetString(payloadBytes).Replace(
            userId.ToString(),
            Guid.NewGuid().ToString(),
            StringComparison.Ordinal);
        var tamperedPayload = Base64UrlEncoder.Encode(payload);
        return string.Join('.', parts[0], tamperedPayload, parts[2]);
    }

    private static RSA LoadActivePrivateKey()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Support", "test-signing-keys.json");
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var pem = document.RootElement.GetProperty("keys")[0].GetProperty("privateKeyPem").GetString()
            ?? throw new InvalidOperationException("Missing privateKeyPem in test signing keys.");
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}