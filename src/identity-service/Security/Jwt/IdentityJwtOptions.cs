namespace IdentityService.Security.Jwt;

public sealed class IdentityJwtOptions
{
    public const string SectionName = "Identity:Jwt";

    public string Issuer { get; set; } = "http://localhost:6061";

    public string Audience { get; set; } = "teamboard";

    public int AccessTokenLifetimeSeconds { get; set; } = 600;
}