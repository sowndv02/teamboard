namespace IdentityService.Security.Refresh;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Identity:RefreshToken";

    public int LifetimeSeconds { get; init; } = 604_800;

    public string DefaultClientId { get; init; } = "teamboard";
}