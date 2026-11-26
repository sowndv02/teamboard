using IdentityService.Security.Clients;
using Xunit;

namespace IdentityService.Tests.Clients;

public sealed class ClientRegistryTests
{
    [Fact]
    public void TryGet_UnknownClient_NotImplementedYet()
    {
        var registry = new ClientRegistry();

        Assert.Throws<NotImplementedException>(() => registry.TryGet("no-such-client", out _));
    }
}