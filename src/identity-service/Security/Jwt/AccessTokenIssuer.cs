using System.Security.Claims;
using IdentityService.Security.Signing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Security.Jwt;

public sealed class AccessTokenIssuer : IAccessTokenIssuer
{
    private readonly ISigningKeyStore _signingKeyStore;
    private readonly IdentityJwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessTokenIssuer(
        ISigningKeyStore signingKeyStore,
        IOptions<IdentityJwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _signingKeyStore = signingKeyStore;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public IssuedAccessToken IssueForUser(Guid userId)
    {
        var signingKey = _signingKeyStore.GetActiveKey();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromSeconds(_jwtOptions.AccessTokenLifetimeSeconds);
        var expires = now.Add(lifetime);

        var rsaKey = new RsaSecurityKey(signingKey.Rsa) { KeyId = signingKey.Kid };
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            NotBefore = now,
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("token_use", "user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return new IssuedAccessToken(token, _jwtOptions.AccessTokenLifetimeSeconds);
    }
}