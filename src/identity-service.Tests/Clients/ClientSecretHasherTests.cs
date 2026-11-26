using IdentityService.Security.Clients;
using Xunit;

namespace IdentityService.Tests.Clients;

public sealed class ClientSecretHasherTests
{
    [Fact]
    public void Verify_CorrectSecret_ReturnsTrue()
    {
        var hasher = new ClientSecretHasher();
        const string secret = "MachineSecret-TestValue-12";

        var stored = hasher.Hash(secret);
        var ok = hasher.Verify(secret, stored);

        Assert.True(ok);
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var hasher = new ClientSecretHasher();
        var stored = hasher.Hash("first-secret-value-12");

        var ok = hasher.Verify("other-secret-value-12", stored);

        Assert.False(ok);
    }
}