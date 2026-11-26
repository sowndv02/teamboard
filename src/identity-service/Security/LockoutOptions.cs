namespace IdentityService.Security;

public sealed class LockoutOptions
{
    public const string SectionName = "Identity:Lockout";

    public int MaxFailures { get; init; } = 5;

    public int LockoutDurationSeconds { get; init; } = 900;
}