using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Security.Signing;

internal static class IdentityJwksSetBuilder
{
    internal static string SerializeActiveKey(ActiveSigningKey activeKey)
    {
        var parameters = activeKey.Rsa.ExportParameters(includePrivateParameters: false);
        var payload = new
        {
            keys = new[]
            {
                new
                {
                    kty = JsonWebAlgorithmsKeyTypes.RSA,
                    kid = activeKey.Kid,
                    alg = SecurityAlgorithms.RsaSha256,
                    use = "sig",
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent),
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }
}