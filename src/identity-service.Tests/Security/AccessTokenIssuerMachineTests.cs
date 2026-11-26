using IdentityService.Security.Jwt;
using IdentityService.Security.Signing;
using IdentityService.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class AccessTokenIssuerMachineTests
{
    private const string TestIssuer = "https://identity.test.local";

    [Fact]
    public void IssueForClient_EmitsMachineClaimsWithRequestedScope()
    {
        var options = Options.Create(new SigningKeysOptions { JsonFile = TestSigningKeyPaths.JsonFile });
        var store = new FileSigningKeyStore(options, NullLogger<FileSigningKeyStore>.Instance);
        var issuer = new AccessTokenIssuer(
            store,
            Options.Create(new IdentityJwtOptions
            {
                Issuer = TestIssuer,
                Audience = "teamboard",
                AccessTokenLifetimeSeconds = 120,
            }),
            TimeProvider.System);

        var issued = issuer.IssueForClient("svc-board", ["api.read"]);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.Token);

        Assert.Equal("svc-board", token.Subject);
        Assert.Equal("machine", token.GetPayloadValue<string>("token_use"));
        Assert.Equal("api.read", token.GetPayloadValue<string>("scope"));
        Assert.Equal("svc-board", token.GetPayloadValue<string>("client_id"));
    }
}