using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdentityService.Security.Signing;

public sealed class SigningKeysHealthCheck : IHealthCheck
{
    private readonly ISigningKeyStore _store;

    public SigningKeysHealthCheck(ISigningKeyStore store) => _store = store;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _store.HasActiveKey
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("No active signing key is configured."));
}