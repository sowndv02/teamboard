namespace IdentityService.Cli;

/// <summary>
/// TB-035: CLI entry points for signing key rotation (D7). Implementation follows in coding-start.
/// </summary>
public static class SigningKeyRotationCommands
{
    public static int Start(string keysFilePath, TimeProvider timeProvider) =>
        throw new NotImplementedException("Signing key rotate start is not implemented yet.");

    public static int Activate(string keysFilePath, TimeProvider timeProvider) =>
        throw new NotImplementedException("Signing key rotate activate is not implemented yet.");

    public static int Retire(string keysFilePath, TimeProvider timeProvider) =>
        throw new NotImplementedException("Signing key rotate retire is not implemented yet.");
}