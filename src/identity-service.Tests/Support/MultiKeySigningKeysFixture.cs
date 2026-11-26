using System.Text.Json;

namespace IdentityService.Tests.Support;

public static class MultiKeySigningKeysFixture
{
    public static string CreateRetiringAndActiveOnDisk()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(TestSigningKeyPaths.JsonFile));
        var sourceKey = document.RootElement.GetProperty("keys")[0];
        var pem = sourceKey.GetProperty("privateKeyPem").GetString()
            ?? throw new InvalidOperationException("test signing key PEM missing.");

        var directory = Path.Combine(Path.GetTempPath(), "identity-multi-key", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "signing-keys.json");

        var payload = new
        {
            keys = new[]
            {
                new
                {
                    kid = "test-active-1",
                    algorithm = "RS256",
                    status = "retiring",
                    privateKeyPem = pem,
                },
                new
                {
                    kid = "test-rotate-active-2",
                    algorithm = "RS256",
                    status = "active",
                    privateKeyPem = pem,
                },
            },
        };

        File.WriteAllText(path, JsonSerializer.Serialize(payload));
        return path;
    }
}