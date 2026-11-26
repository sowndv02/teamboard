namespace IdentityService.Tests.Support;

internal static class TestSigningKeyPaths
{
    public static string JsonFile { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "Support", "test-signing-keys.json"));
}