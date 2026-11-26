namespace IdentityService.Tests.Support;

internal static class RotationTestKeysFile
{
    internal static string CreateIsolatedCopy()
    {
        var directory = Path.Combine(Path.GetTempPath(), "identity-rotation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "signing-keys.json");
        File.Copy(TestSigningKeyPaths.JsonFile, destination, overwrite: false);
        return destination;
    }
}