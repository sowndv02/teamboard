namespace IdentityService.RateLimiting;

public sealed class IdentityRateLimitOptions
{
    public const string SectionName = "Identity:RateLimiting";

    public bool Enabled { get; init; } = true;

    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;
}