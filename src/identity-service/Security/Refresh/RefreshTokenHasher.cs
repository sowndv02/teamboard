using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Security.Refresh;

internal static class RefreshTokenHasher
{
    internal static string Hash(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}