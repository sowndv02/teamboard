using IdentityService.Cli;
using IdentityService.Tests.Support;
using Xunit;

namespace IdentityService.Tests;

public sealed class SigningKeyRotationCliTests
{
    [Fact]
    public void RotateStart_AddsPendingKeyToSigningKeysFile()
    {
        var path = RotationTestKeysFile.CreateIsolatedCopy();

        var exitCode = SigningKeyRotationCommands.Start(path, TimeProvider.System);

        Assert.Equal(0, exitCode);
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var keys = document.RootElement.GetProperty("keys");
        Assert.True(
            keys.EnumerateArray().Any(entry =>
                string.Equals(entry.GetProperty("kid").GetString(), "test-rotate-pending-1", StringComparison.Ordinal)
                && string.Equals(entry.GetProperty("status").GetString(), "pending", StringComparison.OrdinalIgnoreCase)),
            "Expected pending rotation key in signing keys file.");
    }

    [Fact]
    public void RotateStart_ReturnsNonZeroWhenSigningKeysFileIsInvalidJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"signing-keys-invalid-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            var exitCode = SigningKeyRotationCommands.Start(path, TimeProvider.System);

            Assert.NotEqual(0, exitCode);
        }
        finally
        {
            File.Delete(path);
        }
    }
}