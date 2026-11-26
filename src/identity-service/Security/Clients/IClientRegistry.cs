namespace IdentityService.Security.Clients;

public interface IClientRegistry
{
    bool TryGet(string clientId, out RegisteredClientSummary client);
}