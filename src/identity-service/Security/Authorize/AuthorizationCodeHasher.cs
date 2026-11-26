using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Security.Authorize;

internal static class AuthorizationCodeHasher
{
    internal static string Hash(string authorizationCode) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(authorizationCode)));
}