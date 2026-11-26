namespace Teamboard.Shared.Auth;

/// <summary>JWKS cache with unknown-kid refresh throttle (stub until TB-033 coding-start).</summary>
public sealed class JwksKeySetCache
{
    public Task<bool> TryRefreshForUnknownKidAsync(string kid, CancellationToken cancellationToken = default)
    {
        _ = kid;
        _ = cancellationToken;
        return Task.FromResult(false);
    }
}