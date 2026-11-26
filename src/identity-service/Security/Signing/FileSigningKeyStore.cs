using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Signing;

public sealed class FileSigningKeyStore : ISigningKeyStore, IDisposable
{
    private readonly ILogger<FileSigningKeyStore> _logger;
    private ActiveSigningKey? _active;

    public FileSigningKeyStore(IOptions<SigningKeysOptions> options, ILogger<FileSigningKeyStore> logger)
    {
        _logger = logger;
        var path = options.Value.JsonFile;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("Signing key file was not found.");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var document = JsonSerializer.Deserialize<SigningKeySetFile>(json);
            var entry = document?.Keys.FirstOrDefault(k =>
                string.Equals(k.Status, "active", StringComparison.OrdinalIgnoreCase)
                && string.Equals(k.Algorithm, "RS256", StringComparison.OrdinalIgnoreCase));

            if (entry is null || string.IsNullOrWhiteSpace(entry.PrivateKeyPem) || string.IsNullOrWhiteSpace(entry.Kid))
            {
                _logger.LogWarning("No active RS256 signing key was loaded.");
                return;
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(entry.PrivateKeyPem);
            _active = new ActiveSigningKey(entry.Kid, rsa);
        }
        catch (Exception exception) when (exception is JsonException or CryptographicException or IOException)
        {
            _logger.LogWarning(exception, "Failed to load signing keys from file.");
        }
    }

    public bool HasActiveKey => _active is not null;

    public ActiveSigningKey GetActiveKey() =>
        _active ?? throw new InvalidOperationException("No active signing key is configured.");

    public void Dispose() => _active?.Dispose();
}