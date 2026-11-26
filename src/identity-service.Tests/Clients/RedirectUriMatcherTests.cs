using IdentityService.Security.Clients;
using Xunit;

namespace IdentityService.Tests.Clients;

public sealed class RedirectUriMatcherTests
{
    [Fact]
    public void IsAllowed_ExactMatch_ReturnsTrue()
    {
        var matcher = new RedirectUriMatcher();
        var registered = new[] { "https://app.example/callback" };

        var allowed = matcher.IsAllowed("https://app.example/callback", registered);

        Assert.True(allowed);
    }
}