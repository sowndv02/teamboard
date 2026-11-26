namespace IdentityService.Security;

public sealed class DataProtectionOptions
{
    public const string SectionName = "Identity:DataProtection";

    public string? KeysDirectory { get; set; }
}