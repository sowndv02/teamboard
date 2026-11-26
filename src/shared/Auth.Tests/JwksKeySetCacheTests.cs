using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwksKeySetCacheTests
{
  [Fact]
  public async Task TryRefreshForUnknownKidAsync_UnknownKid_TriggersRefresh()
  {
    var cache = new JwksKeySetCache();

    var refreshed = await cache.TryRefreshForUnknownKidAsync("unknown-kid-33");

    Assert.True(refreshed);
  }
}