using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

internal sealed class StaticSigningKeyJwksSource : IJwksKeySetSource
{
    private readonly TeamboardAuthJwtOptions _options;

    public StaticSigningKeyJwksSource(IOptions<TeamboardAuthJwtOptions> options) =>
        _options = options.Value;

    public JsonWebKeySet Load()
    {
        var path = _options.StaticJwksFile
            ?? throw new InvalidOperationException("Auth:Jwt:StaticJwksFile is required when JwksUri is not set.");

        var json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.TryGetProperty("keys", out var keysElement)
            && keysElement.ValueKind == JsonValueKind.Array
            && keysElement.GetArrayLength() > 0
            && keysElement[0].TryGetProperty("kty", out _))
        {
            return new JsonWebKeySet(json);
        }

        if (!document.RootElement.TryGetProperty("keys", out keysElement))
        {
            throw new InvalidOperationException("Static JWKS file must contain a keys array.");
        }

        return LoadFromIdentitySigningFile(keysElement);
    }

    private static JsonWebKeySet LoadFromIdentitySigningFile(JsonElement keysElement)
    {
        var set = new JsonWebKeySet();
        foreach (var entry in keysElement.EnumerateArray())
        {
            if (!string.Equals(entry.GetProperty("status").GetString(), "active", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(entry.GetProperty("algorithm").GetString(), "RS256", StringComparison.Ordinal))
            {
                continue;
            }

            var kid = entry.GetProperty("kid").GetString()
                ?? throw new InvalidOperationException("Signing key entry is missing kid.");
            var pem = entry.GetProperty("privateKeyPem").GetString()
                ?? throw new InvalidOperationException("Signing key entry is missing privateKeyPem.");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            var parameters = rsa.ExportParameters(false);
            set.Keys.Add(new JsonWebKey
            {
                Kty = "RSA",
                Kid = kid,
                Alg = SecurityAlgorithms.RsaSha256,
                Use = "sig",
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                E = Base64UrlEncoder.Encode(parameters.Exponent),
            });
        }

        if (set.Keys.Count == 0)
        {
            throw new InvalidOperationException("No active RS256 signing keys found in static JWKS file.");
        }

        return set;
    }
}