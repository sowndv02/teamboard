namespace Teamboard.Shared.Auth;

public sealed class TeamboardAuthJwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string ValidIssuer { get; set; } = "http://localhost:6061";

    public string ValidAudience { get; set; } = "teamboard";

    public string? StaticJwksFile { get; set; }

    public Uri? JwksUri { get; set; }

    public int JwksCacheTtlMinutes { get; set; } = 10;

    public int UnknownKidRefreshThrottleSeconds { get; set; } = 60;
}