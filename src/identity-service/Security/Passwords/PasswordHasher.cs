using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Passwords;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int HashByteLength = 32;
    private const int SaltByteLength = 32;

    private readonly PasswordOptions _options;
    private readonly IdentityMetrics _metrics;

    public PasswordHasher(IOptions<PasswordOptions> options, IdentityMetrics metrics)
    {
        _options = options.Value;
        _metrics = metrics;
    }

    public HashedPassword HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltByteLength);
        var started = Stopwatch.GetTimestamp();
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            _options.Iterations,
            HashAlgorithmName.SHA256,
            HashByteLength);
        var elapsedSeconds = Stopwatch.GetElapsedTime(started).TotalSeconds;
        _metrics.RecordPasswordHashDuration(elapsedSeconds);

        return new HashedPassword(
            Convert.ToBase64String(hashBytes),
            Convert.ToBase64String(saltBytes),
            _options.Iterations);
    }

    public bool VerifyPassword(string password, HashedPassword stored)
    {
        var saltBytes = Convert.FromBase64String(stored.Salt);
        var expected = Convert.FromBase64String(stored.Hash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            stored.Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}