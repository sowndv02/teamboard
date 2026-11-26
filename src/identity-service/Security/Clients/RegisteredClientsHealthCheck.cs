using IdentityService.Persistence;
using Microsoft.EntityFrameworkCore;
using IdentityService.Security.Refresh;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace IdentityService.Security.Clients;

public sealed class RegisteredClientsHealthCheck(
    IServiceScopeFactory scopeFactory,
    IOptions<ClientsOptions> clientsOptions,
    IOptions<RefreshTokenOptions> refreshOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!clientsOptions.Value.Enabled)
        {
            return HealthCheckResult.Healthy();
        }

        var defaultId = refreshOptions.Value.DefaultClientId;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var exists = await db.RegisteredClients.AsNoTracking().AnyAsync(c => c.ClientId == defaultId, cancellationToken);
        return exists
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"OAuth client '{defaultId}' is not registered.");
    }
}