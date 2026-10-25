using System.Net;
using Gateway.RateLimiting;
using Xunit;

namespace Gateway.Tests;

/// <summary>The rate-limit key for a client address (design D2): IPv4 whole, IPv6 grouped by /64, one counter per real client.</summary>
public sealed class ClientPartitionKeyTests
{
    [Fact]
    public void IPv4_UsesTheWholeAddress()
    {
        var first = ClientPartitionKey.For(IPAddress.Parse("203.0.113.9"));
        var second = ClientPartitionKey.For(IPAddress.Parse("203.0.113.10"));

        Assert.False(string.IsNullOrEmpty(first));
        Assert.NotEqual(first, second);
        Assert.Equal(first, ClientPartitionKey.For(IPAddress.Parse("203.0.113.9")));
    }

    [Fact]
    public void IPv6_InTheSame64_ShareAKey()
    {
        var first = ClientPartitionKey.For(IPAddress.Parse("2001:db8:abcd:1::1"));
        var second = ClientPartitionKey.For(IPAddress.Parse("2001:db8:abcd:1:ffff:ffff:ffff:ffff"));

        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("2001:db8:abcd:2::1")]
    [InlineData("2001:db8:abcd:0::1")]
    [InlineData("2001:db8:abcc:1::1")]
    [InlineData("2001:db9:abcd:1::1")]
    public void IPv6_InAnother64_GetsAnotherKey(string other) =>
        Assert.NotEqual(
            ClientPartitionKey.For(IPAddress.Parse("2001:db8:abcd:1::1")),
            ClientPartitionKey.For(IPAddress.Parse(other)));

    [Fact]
    public void IPv4MappedIPv6_IsTreatedAsTheIPv4Address()
    {
        var plain = ClientPartitionKey.For(IPAddress.Parse("203.0.113.9"));

        // A key that is empty on both sides would make the equality below pass without proving anything.
        Assert.False(string.IsNullOrEmpty(plain));
        Assert.Equal(plain, ClientPartitionKey.For(IPAddress.Parse("::ffff:203.0.113.9")));
    }

    [Fact]
    public void IPv6ScopeId_DoesNotLeakIntoTheKey()
    {
        var withoutScope = ClientPartitionKey.For(IPAddress.Parse("fe80::1"));

        Assert.False(string.IsNullOrEmpty(withoutScope));
        Assert.Equal(withoutScope, ClientPartitionKey.For(IPAddress.Parse("fe80::1%5")));
        Assert.DoesNotContain("%", withoutScope, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownAddress_SharesOneCounter()
    {
        var key = ClientPartitionKey.For(null);

        Assert.Equal("unknown", key);
    }
}
