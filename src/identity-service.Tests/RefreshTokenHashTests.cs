using System.Security.Cryptography;
using System.Text;
using IdentityService.Security.Refresh;
using Xunit;

namespace IdentityService.Tests;

public sealed class RefreshTokenHashTests
{
    [Fact]
    public void HashToken_ReturnsSha256Base64OfUtf8()
    {
        const string token = "sample-refresh-token-value";
        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        var actual = RefreshTokenHasher.Hash(token);

        Assert.Equal(expected, actual);
    }
}