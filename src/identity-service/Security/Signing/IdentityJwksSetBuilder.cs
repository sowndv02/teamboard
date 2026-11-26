using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Security.Signing;

internal static class IdentityJwksSetBuilder
{
    internal static string SerializePublishedKeys(IReadOnlyList<ActiveSigningKey> keys)
    {
        var jwks = keys.Select(ToJwk);
        var payload = new { keys = jwks };
        return JsonSerializer.Serialize(payload);
    }

    private static object ToJwk(ActiveSigningKey activeKey)
    {
        var parameters = activeKey.Rsa.ExportParameters(includePrivateParameters: false);
        return new
        {
            kty = JsonWebAlgorithmsKeyTypes.RSA,
            kid = activeKey.Kid,
            alg = SecurityAlgorithms.RsaSha256,
            use = "sig",
            n = Base64UrlEncoder.Encode(parameters.Modulus),
            e = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }
}