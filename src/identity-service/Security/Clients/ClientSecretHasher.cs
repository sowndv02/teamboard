using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Clients;

public sealed class ClientSecretHasher(IOptions<ClientSecretOptions> options)
{
    private const int HashByteLength = 32;
    private const int SaltByteLength = 32;

    private readonly ClientSecretOptions _options = options.Value;

    public ClientSecretHashResult Hash(string plaintextSecret)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltByteLength);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            plaintextSecret,
            saltBytes,
            _options.Iterations,
            HashAlgorithmName.SHA256,
            HashByteLength);

        return new ClientSecretHashResult(
            Convert.ToBase64String(hashBytes),
            Convert.ToBase64String(saltBytes),
            _options.Iterations);
    }

    public bool Verify(string plaintextSecret, ClientSecretHashResult stored)
    {
        var saltBytes = Convert.FromBase64String(stored.Salt);
        var expected = Convert.FromBase64String(stored.Hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            plaintextSecret,
            saltBytes,
            stored.Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public bool Verify(string plaintextSecret, string hashBase64, string saltBase64, int iterations)
    {
        var stored = new ClientSecretHashResult(hashBase64, saltBase64, iterations);
        return Verify(plaintextSecret, stored);
    }
}

public sealed record ClientSecretHashResult(string Hash, string Salt, int Iterations);