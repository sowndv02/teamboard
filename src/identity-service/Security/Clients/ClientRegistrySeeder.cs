using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using IdentityService.Security.Refresh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Clients;

public sealed class ClientRegistrySeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<ClientsOptions> clientsOptions,
    IOptions<RefreshTokenOptions> refreshOptions,
    IConfiguration configuration,
    ClientSecretHasher secretHasher,
    TimeProvider timeProvider,
    ILogger<ClientRegistrySeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = clientsOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var seeds = BuildSeedList(options, refreshOptions.Value.DefaultClientId);
        var now = timeProvider.GetUtcNow();

        foreach (var seed in seeds)
        {
            await UpsertAsync(db, seed, now, cancellationToken);
        }

        logger.LogInformation("event=client_registry_sync clientCount={ClientCount}", seeds.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IReadOnlyList<ClientSeedDefinition> BuildSeedList(ClientsOptions options, string defaultClientId)
    {
        if (options.Seed.Count > 0)
        {
            return options.Seed;
        }

        return
        [
            new ClientSeedDefinition
            {
                ClientId = defaultClientId,
                ClientType = "public",
                RedirectUris = ["https://localhost/callback"],
                AllowedScopes = ["openid"],
            },
        ];
    }

    private async Task UpsertAsync(
        IdentityDbContext db,
        ClientSeedDefinition seed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedType = NormalizeClientType(seed.ClientType);
        var entity = await db.RegisteredClients.FindAsync([seed.ClientId], cancellationToken);
        if (entity is null)
        {
            entity = new RegisteredClient { ClientId = seed.ClientId };
            db.RegisteredClients.Add(entity);
        }

        entity.ClientType = normalizedType;
        entity.RedirectUris = [.. seed.RedirectUris];
        entity.AllowedScopes = [.. seed.AllowedScopes];
        entity.UpdatedAt = now;

        if (string.Equals(normalizedType, "confidential", StringComparison.Ordinal))
        {
            var plaintext = configuration[$"Identity:Clients:Secrets:{seed.ClientId}"];
            if (string.IsNullOrWhiteSpace(plaintext))
            {
                throw new InvalidOperationException($"Confidential client '{seed.ClientId}' requires Identity:Clients:Secrets:{seed.ClientId}.");
            }

            var hashed = secretHasher.Hash(plaintext);
            entity.SecretHash = hashed.Hash;
            entity.SecretSalt = hashed.Salt;
            entity.SecretIterations = hashed.Iterations;
        }
        else
        {
            entity.SecretHash = null;
            entity.SecretSalt = null;
            entity.SecretIterations = null;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeClientType(string clientType) =>
        string.Equals(clientType, "confidential", StringComparison.OrdinalIgnoreCase) ? "confidential" : "public";
}