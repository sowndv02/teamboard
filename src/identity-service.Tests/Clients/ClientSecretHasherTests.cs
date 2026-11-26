using IdentityService.Security.Clients;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityService.Tests.Clients;

public sealed class ClientSecretHasherTests
{
    private static ClientSecretHasher CreateHasher() =>
        new(Options.Create(new ClientSecretOptions { Iterations = 4_000 }));

    [Fact]
    public void Verify_CorrectSecret_ReturnsTrue()
    {
        var hasher = CreateHasher();
        const string secret = "MachineSecret-TestValue-12";

        var stored = hasher.Hash(secret);
        var ok = hasher.Verify(secret, stored);

        Assert.True(ok);
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var hasher = CreateHasher();
        var stored = hasher.Hash("first-secret-value-12");

        var ok = hasher.Verify("other-secret-value-12", stored);

        Assert.False(ok);
    }
}