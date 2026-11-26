using Teamboard.Shared.Auth;
using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwksKeySetCacheTests
{
    [Fact]
    public async Task TryRefreshForUnknownKidAsync_UnknownKid_ReturnsFalseAfterReload()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Support", "test-signing-keys.json");
        var options = Microsoft.Extensions.Options.Options.Create(new TeamboardAuthJwtOptions
        {
            StaticJwksFile = path,
        });
        var source = new StaticSigningKeyJwksSource(options);
        var cache = new JwksKeySetCache(source, TimeProvider.System, options);

        var found = await cache.TryRefreshForUnknownKidAsync("unknown-kid-33");

        Assert.False(found);
    }
}