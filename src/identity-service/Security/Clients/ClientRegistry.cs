using IdentityService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Security.Clients;

public sealed class ClientRegistry(
    IdentityDbContext db,
    RedirectUriMatcher redirectMatcher,
    ClientSecretHasher secretHasher) : IClientRegistry
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

    public bool ValidateRedirectUri(string clientId, string redirectUri)
    {
        if (!TryGet(clientId, out var summary))
        {
            return false;
        }

        return redirectMatcher.IsAllowed(redirectUri, summary.RedirectUris);
    }

    public bool ValidateClientSecret(string clientId, string plaintextSecret)
    {
        var entity = db.RegisteredClients.AsNoTracking().FirstOrDefault(row => row.ClientId == clientId);
        if (entity is null
            || !string.Equals(entity.ClientType, "confidential", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(entity.SecretHash)
            || string.IsNullOrWhiteSpace(entity.SecretSalt)
            || entity.SecretIterations is null)
        {
            return false;
        }

        return secretHasher.Verify(
            plaintextSecret,
            entity.SecretHash,
            entity.SecretSalt,
            entity.SecretIterations.Value);
    }

    private static ClientType ParseClientType(string stored) =>
        string.Equals(stored, "confidential", StringComparison.OrdinalIgnoreCase)
            ? ClientType.Confidential
            : ClientType.Public;
}