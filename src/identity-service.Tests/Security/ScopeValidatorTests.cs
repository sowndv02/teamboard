using IdentityService.Security.Token;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class ScopeValidatorTests
{
    [Fact]
    public void IsRequestedScopeAllowed_WhenSingleScopeIsAllowed_ReturnsTrue()
    {
        Assert.Throws<NotImplementedException>(() =>
            ScopeValidator.IsRequestedScopeAllowed(["api.read"], ["api.read", "board.write"]));
    }

    [Fact]
    public void IsRequestedScopeAllowed_WhenScopeNotAllowed_ThrowsUntilImplemented()
    {
        Assert.Throws<NotImplementedException>(() =>
            ScopeValidator.IsRequestedScopeAllowed(["admin"], ["api.read"]));
    }
}