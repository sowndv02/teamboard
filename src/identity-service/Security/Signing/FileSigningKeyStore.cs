using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Signing;

public sealed class FileSigningKeyStore : ISigningKeyStore, IDisposable
{
    private static readonly string[] PublishedStatuses = ["pending", "active", "retiring"];

    private readonly ILogger<FileSigningKeyStore> _logger;
    private readonly string? _path;
    private readonly object _gate = new();
    private ActiveSigningKey? _active;
    private IReadOnlyList<ActiveSigningKey> _published = [];

    public FileSigningKeyStore(IOptions<SigningKeysOptions> options, ILogger<FileSigningKeyStore> logger)
    {
        _logger = logger;
        _path = string.IsNullOrWhiteSpace(options.Value.JsonFile) ? null : options.Value.JsonFile;
        if (_path is not null)
        {
            ReloadFromDisk();
        }
    }

    public bool HasActiveKey
    {
        get
        {
            lock (_gate)
            {
                ReloadFromDisk();
                return _active is not null;
            }
        }
    }

    public ActiveSigningKey GetActiveKey()
    {
        lock (_gate)
        {
            ReloadFromDisk();
            return _active ?? throw new InvalidOperationException("No active signing key is configured.");
        }
    }

    public IReadOnlyList<ActiveSigningKey> GetPublishedKeysForJwks()
    {
        lock (_gate)
        {
            ReloadFromDisk();
            return _published;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            DisposeLoadedKeys();
        }
    }

    private void ReloadFromDisk()
    {
        if (_path is null || !File.Exists(_path))
        {
            DisposeLoadedKeys();
            return;
        }

        try
        {
            var document = SigningKeysFileOperations.Load(_path);
            var activeEntry = document.Keys.FirstOrDefault(k =>
                string.Equals(k.Status, "active", StringComparison.OrdinalIgnoreCase)
                && string.Equals(k.Algorithm, "RS256", StringComparison.OrdinalIgnoreCase));

            var publishedEntries = document.Keys.Where(k =>
                    PublishedStatuses.Contains(k.Status, StringComparer.OrdinalIgnoreCase)
                    && string.Equals(k.Algorithm, "RS256", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(k.Kid)
                    && !string.IsNullOrWhiteSpace(k.PrivateKeyPem))
                .ToList();

            DisposeLoadedKeys();

            if (activeEntry is not null)
            {
                _active = CreateActiveKey(activeEntry);
            }

            _published = publishedEntries
                .Select(CreateActiveKey)
                .ToList();
        }
        catch (Exception exception) when (exception is JsonException or CryptographicException or IOException or InvalidDataException)
        {
            _logger.LogWarning(exception, "Failed to load signing keys from file.");
            DisposeLoadedKeys();
        }
    }

    private static ActiveSigningKey CreateActiveKey(SigningKeyFileEntry entry)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(entry.PrivateKeyPem);
        return new ActiveSigningKey(entry.Kid, rsa);
    }

    private void DisposeLoadedKeys()
    {
        _active?.Dispose();
        _active = null;
        foreach (var key in _published)
        {
            key.Dispose();
        }

        _published = [];
    }
}