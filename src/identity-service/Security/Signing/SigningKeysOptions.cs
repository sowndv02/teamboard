namespace IdentityService.Security.Signing;

public sealed class SigningKeysOptions
{
    public const string SectionName = "Identity:SigningKeys";

    public string JsonFile { get; set; } = string.Empty;
}