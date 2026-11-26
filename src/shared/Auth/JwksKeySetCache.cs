using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

public sealed class JwksKeySetCache
{
    private readonly IJwksKeySetSource _primarySource;
    private readonly IJwksKeySetSource? _fallbackSource;
    private readonly TimeProvider _timeProvider;
    private readonly TeamboardAuthJwtOptions _options;
    private readonly object _gate = new();
    private JsonWebKeySet? _cached;
    private DateTimeOffset _cachedAt;
    private DateTimeOffset _lastUnknownKidRefreshAt;

    internal JwksKeySetCache(
        StaticSigningKeyJwksSource staticSource,
        IOptions<TeamboardAuthJwtOptions> options,
        TimeProvider timeProvider,
        IServiceProvider services)
    {
        _primarySource = staticSource;
        _options = options.Value;
        _timeProvider = timeProvider;

        if (_options.JwksUri is not null)
        {
            var factory = services.GetRequiredService<IHttpClientFactory>();
            _fallbackSource = new HttpJwksKeySetSource(factory.CreateClient(nameof(JwksKeySetCache)), options);
        }
    }

    internal JwksKeySetCache(IJwksKeySetSource source, TimeProvider timeProvider, IOptions<TeamboardAuthJwtOptions> options)
    {
        _primarySource = source;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<SecurityKey> GetSigningKeys(string? kid)
    {
        var keys = GetOrLoadKeys();
        if (kid is not null && keys.All(k => !string.Equals(k.KeyId, kid, StringComparison.Ordinal)))
        {
            _ = TryRefreshForUnknownKid(kid);
            keys = GetOrLoadKeys();
        }

        return keys;
    }

    public Task<bool> TryRefreshForUnknownKidAsync(string kid, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(TryRefreshForUnknownKid(kid));
    }

    private bool TryRefreshForUnknownKid(string kid)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_gate)
        {
            if (now - _lastUnknownKidRefreshAt < TimeSpan.FromSeconds(_options.UnknownKidRefreshThrottleSeconds))
            {
                return KeySetContainsKid(GetOrLoadKeysUnsafe(), kid);
            }

            _lastUnknownKidRefreshAt = now;
            ReloadKeysUnsafe();
            return KeySetContainsKid(GetOrLoadKeysUnsafe(), kid);
        }
    }

    private IReadOnlyList<SecurityKey> GetOrLoadKeys()
    {
        lock (_gate)
        {
            return GetOrLoadKeysUnsafe();
        }
    }

    private IReadOnlyList<SecurityKey> GetOrLoadKeysUnsafe()
    {
        var now = _timeProvider.GetUtcNow();
        var ttl = TimeSpan.FromMinutes(_options.JwksCacheTtlMinutes);
        if (_cached is null || now - _cachedAt >= ttl)
        {
            ReloadKeysUnsafe();
        }

        return _cached!.GetSigningKeys().ToList();
    }

    private void ReloadKeysUnsafe()
    {
        try
        {
            _cached = _primarySource.Load();
            _cachedAt = _timeProvider.GetUtcNow();
        }
        catch when (_fallbackSource is not null)
        {
            _cached = _fallbackSource.Load();
            _cachedAt = _timeProvider.GetUtcNow();
        }
    }

    private static bool KeySetContainsKid(IReadOnlyList<SecurityKey> keys, string kid) =>
        keys.Any(k => string.Equals(k.KeyId, kid, StringComparison.Ordinal));
}