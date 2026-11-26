namespace IdentityService.Security.Clients;

public interface IClientRegistry
{
    bool TryGet(string clientId, out RegisteredClientSummary client);

    bool ValidateRedirectUri(string clientId, string redirectUri);

    bool ValidateClientSecret(string clientId, string plaintextSecret);
}