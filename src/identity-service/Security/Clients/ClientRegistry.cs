namespace IdentityService.Security.Clients;

public sealed class ClientRegistry : IClientRegistry
{
    public bool TryGet(string clientId, out RegisteredClientSummary client) =>
        throw new NotImplementedException("TB-040 client registry");
}