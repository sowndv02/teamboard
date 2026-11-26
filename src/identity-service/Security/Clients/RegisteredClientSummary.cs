namespace IdentityService.Security.Clients;

public sealed record RegisteredClientSummary(string ClientId, ClientType ClientType, IReadOnlyList<string> RedirectUris, IReadOnlyList<string> AllowedScopes);