using System.Security.Cryptography;
using IdentityService.Security.Passwords;

namespace IdentityService.Features.Login;

/// <summary>Fixed hash material used only when no user exists, to approximate verify cost (design 6.4).</summary>
internal static class LoginDummyCredentials
{
    private const int HashByteLength = 32;
    private static readonly int Iterations = new PasswordOptions().Iterations;
    private static readonly byte[] Salt = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

    public static readonly HashedPassword Stored = new(
        Convert.ToBase64String(DeriveHash()),
        Convert.ToBase64String(Salt),
        Iterations);

    private static byte[] DeriveHash() =>
        Rfc2898DeriveBytes.Pbkdf2(
            "login-dummy-timing-placeholder",
            Salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashByteLength);
}