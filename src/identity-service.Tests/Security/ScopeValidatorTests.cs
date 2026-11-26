using IdentityService.Security.Token;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class ScopeValidatorTests
{
    [Fact]
    public void IsRequestedScopeAllowed_WhenSubsetOfAllowed_ReturnsTrue()
    {
        Assert.True(ScopeValidator.IsRequestedScopeAllowed(["api.read"], ["api.read", "board.write"]));
    }

    [Fact]
    public void IsRequestedScopeAllowed_WhenNotListed_ReturnsFalse()
    {
        Assert.False(ScopeValidator.IsRequestedScopeAllowed(["admin"], ["api.read"]));
    }

    [Fact]
    public void IsRequestedScopeAllowed_WhenEmpty_ReturnsFalse()
    {
        Assert.False(ScopeValidator.IsRequestedScopeAllowed([], ["api.read"]));
    }
}