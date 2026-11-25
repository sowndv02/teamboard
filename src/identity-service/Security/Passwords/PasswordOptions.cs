namespace IdentityService.Security.Passwords;

public sealed class PasswordOptions
{
    public const string SectionName = "Password";

    public int Iterations { get; set; } = 600_000;

    public int MinLength { get; set; } = 12;

    public int MaxLength { get; set; } = 128;
}