using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using IdentityService.Security.Signing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityService.Cli;

public static class SigningKeyRotationCommands
{
    public static int Start(string keysFilePath, TimeProvider timeProvider, ILogger? logger = null) =>
        Run(keysFilePath, timeProvider, logger, RotateStart);

    public static int Activate(string keysFilePath, TimeProvider timeProvider, ILogger? logger = null) =>
        Run(keysFilePath, timeProvider, logger, RotateActivate);

    public static int Retire(string keysFilePath, TimeProvider timeProvider, ILogger? logger = null) =>
        Run(keysFilePath, timeProvider, logger, RotateRetire);

    private static int Run(
        string keysFilePath,
        TimeProvider timeProvider,
        ILogger? logger,
        Func<SigningKeySetFile, TimeProvider, ILogger, (bool Success, string Message)> action)
    {
        var log = logger ?? NullLogger.Instance;
        if (string.IsNullOrWhiteSpace(keysFilePath) || !File.Exists(keysFilePath))
        {
            log.LogWarning("Signing keys file was not found: {Path}", keysFilePath);
            return 1;
        }

        try
        {
            var document = SigningKeysFileOperations.Load(keysFilePath);
            var (success, message) = action(document, timeProvider, log);
            if (!success)
            {
                log.LogWarning("Signing key rotation rejected: {Reason}", message);
                return 1;
            }

            SigningKeysFileOperations.SaveAtomic(keysFilePath, document);
            log.LogInformation("key_rotation {Outcome}", message);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or CryptographicException or InvalidDataException or JsonException)
        {
            log.LogError(exception, "Signing key rotation failed.");
            return 1;
        }
    }

    private static (bool Success, string Message) RotateStart(
        SigningKeySetFile document,
        TimeProvider timeProvider,
        ILogger log)
    {
        if (document.Keys.Any(k => string.Equals(k.Status, "pending", StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "event=start rejected reason=pending_key_already_exists");
        }

        if (!document.Keys.Any(k => string.Equals(k.Status, "active", StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "event=start rejected reason=no_active_key");
        }

        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();

        document.Keys.Add(new SigningKeyFileEntry
        {
            Kid = SigningKeyRotationPolicy.PendingKid,
            Algorithm = "RS256",
            Status = "pending",
            PrivateKeyPem = pem,
            PublishedAtUtc = FormatTimestamp(timeProvider.GetUtcNow()),
        });

        log.LogInformation("key_rotation event=start kid={Kid}", SigningKeyRotationPolicy.PendingKid);
        return (true, "event=start");
    }

    private static (bool Success, string Message) RotateActivate(
        SigningKeySetFile document,
        TimeProvider timeProvider,
        ILogger log)
    {
        var pending = document.Keys.FirstOrDefault(k =>
            string.Equals(k.Status, "pending", StringComparison.OrdinalIgnoreCase));
        if (pending is null)
        {
            return (false, "event=activate rejected reason=no_pending_key");
        }

        if (!TryParseTimestamp(pending.PublishedAtUtc, out var publishedAt))
        {
            return (false, "event=activate rejected reason=pending_missing_published_at");
        }

        if (timeProvider.GetUtcNow() - publishedAt < SigningKeyRotationPolicy.JwksPublishWindow)
        {
            return (false, "event=activate rejected reason=publish_window_not_elapsed");
        }

        var now = FormatTimestamp(timeProvider.GetUtcNow());
        var retiring = document.Keys
            .Where(k => string.Equals(k.Status, "active", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in retiring)
        {
            key.Status = "retiring";
            key.RetiringSinceUtc = now;
        }

        pending.Status = "active";
        pending.Kid = SigningKeyRotationPolicy.NextActiveKid;
        pending.PublishedAtUtc = null;

        log.LogInformation(
            "key_rotation event=activate kid={Kid} retired_count={RetiredCount}",
            SigningKeyRotationPolicy.NextActiveKid,
            retiring.Count);
        return (true, "event=activate");
    }

    private static (bool Success, string Message) RotateRetire(
        SigningKeySetFile document,
        TimeProvider timeProvider,
        ILogger log)
    {
        var retiring = document.Keys
            .Where(k => string.Equals(k.Status, "retiring", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (retiring.Count == 0)
        {
            return (false, "event=retire rejected reason=no_retiring_key");
        }

        foreach (var key in retiring)
        {
            if (!TryParseTimestamp(key.RetiringSinceUtc, out var retiringSince))
            {
                return (false, "event=retire rejected reason=retiring_missing_since");
            }

            if (timeProvider.GetUtcNow() - retiringSince < SigningKeyRotationPolicy.MinRetiringDuration)
            {
                return (false, "event=retire rejected reason=min_retiring_duration_not_elapsed");
            }
        }

        document.Keys.RemoveAll(k => string.Equals(k.Status, "retiring", StringComparison.OrdinalIgnoreCase));
        log.LogInformation("key_rotation event=retire removed_count={RemovedCount}", retiring.Count);
        return (true, "event=retire");
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static bool TryParseTimestamp(string? value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed);
}