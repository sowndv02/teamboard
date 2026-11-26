using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

internal static class TeamboardJwtValidationParameters
{
    internal const string TokenUseClaim = "token_use";
    internal const string UserTokenUseValue = "user";

    public static TokenValidationParameters Create(
        TeamboardAuthJwtOptions options,
        JwksKeySetCache keySetCache)
    {
        return new TokenValidationParameters
        {
            ValidIssuer = options.ValidIssuer,
            ValidAudience = options.ValidAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = JwtRegisteredClaimNames.Sub,
            IssuerSigningKeyResolver = (_, _, kid, _) => keySetCache.GetSigningKeys(kid),
        };
    }
}