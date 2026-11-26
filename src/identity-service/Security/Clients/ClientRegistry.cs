using IdentityService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Security.Clients;

public sealed class ClientRegistry(IdentityDbContext db) : IClientRegistry
{
    public bool TryGet(string clientId, out RegisteredClientSummary client)
    {
        var entity = db.RegisteredClients.AsNoTracking().FirstOrDefault(row => row.ClientId == clientId);
        if (entity is null)
        {
            client = null!;
            return false;
        }

        client = new RegisteredClientSummary(
            entity.ClientId,
            ParseClientType(entity.ClientType),
            entity.RedirectUris,
            entity.AllowedScopes);
        return true;
    }

    private static ClientType ParseClientType(string stored) =>
        string.Equals(stored, "confidential", StringComparison.OrdinalIgnoreCase)
            ? ClientType.Confidential
            : ClientType.Public;
}