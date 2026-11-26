namespace IdentityService.Persistence.Entities;

public sealed class RegisteredClient
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientType { get; set; } = "public";

    public string? SecretHash { get; set; }

    public string? SecretSalt { get; set; }

    public int? SecretIterations { get; set; }

    public List<string> RedirectUris { get; set; } = [];

    public List<string> AllowedScopes { get; set; } = [];

    public DateTimeOffset UpdatedAt { get; set; }
}