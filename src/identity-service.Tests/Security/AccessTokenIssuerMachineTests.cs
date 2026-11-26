using IdentityService.Security.Jwt;
using IdentityService.Security.Signing;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class AccessTokenIssuerMachineTests
{
    [Fact]
    public void IssueForClient_ThrowsNotImplementedUntilTb041()
    {
        var issuer = new AccessTokenIssuer(
            new NoOpSigningKeyStore(),
            Options.Create(new IdentityJwtOptions
            {
                Issuer = "https://identity.test",
                Audience = "teamboard",
                AccessTokenLifetimeSeconds = 120,
            }),
            TimeProvider.System);

        Assert.Throws<NotImplementedException>(() =>
            issuer.IssueForClient("svc-board", ["api.read"]));
    }

    private sealed class NoOpSigningKeyStore : ISigningKeyStore
    {
        public bool HasActiveKey => true;

        public ActiveSigningKey GetActiveKey() =>
            throw new InvalidOperationException("Not reached in TB-041 red test.");

        public IReadOnlyList<ActiveSigningKey> GetPublishedKeysForJwks() => [];
    }
}