namespace IdentityService.Security.Clients;

public sealed class ClientsOptions
{
    public const string SectionName = "Identity:Clients";

    public bool Enabled { get; init; } = true;

    public List<ClientSeedDefinition> Seed { get; init; } = [];
}

public sealed class ClientSeedDefinition
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientType { get; init; } = "public";

    public List<string> RedirectUris { get; init; } = [];

    public List<string> AllowedScopes { get; init; } = [];
}