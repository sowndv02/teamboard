using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

internal sealed class HttpJwksKeySetSource : IJwksKeySetSource
{
    private readonly HttpClient _httpClient;
    private readonly TeamboardAuthJwtOptions _options;

    public HttpJwksKeySetSource(HttpClient httpClient, IOptions<TeamboardAuthJwtOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public JsonWebKeySet Load()
    {
        var uri = _options.JwksUri
            ?? throw new InvalidOperationException("Auth:Jwt:JwksUri is not configured.");
        var json = _httpClient.GetStringAsync(uri).GetAwaiter().GetResult();
        return new JsonWebKeySet(json);
    }
}