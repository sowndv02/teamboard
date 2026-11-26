using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Teamboard.Shared.Auth;

public sealed class TeamboardJwtTokenValidator
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly TokenValidationParameters _parameters;

    public TeamboardJwtTokenValidator(JwksKeySetCache keySetCache, IOptions<TeamboardAuthJwtOptions> options)
    {
        _parameters = TeamboardJwtValidationParameters.Create(options.Value, keySetCache);
    }

    private TeamboardJwtTokenValidator(TokenValidationParameters parameters) => _parameters = parameters;

    public static TeamboardJwtTokenValidator CreateForTesting(string staticSigningKeysFile)
    {
        var options = Options.Create(new TeamboardAuthJwtOptions
        {
            StaticJwksFile = staticSigningKeysFile,
        });
        var source = new StaticSigningKeyJwksSource(options);
        var cache = new JwksKeySetCache(source, TimeProvider.System, options);
        return new TeamboardJwtTokenValidator(
            TeamboardJwtValidationParameters.Create(options.Value, cache));
    }

    public async Task<TeamboardJwtValidationResult> ValidateAccessTokenAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.ValidateTokenAsync(bearerToken, _parameters);
        if (!result.IsValid)
        {
            return new TeamboardJwtValidationResult(false, result.Exception?.Message ?? "invalid_token");
        }

        var tokenUse = result.ClaimsIdentity?.FindFirst(TeamboardJwtValidationParameters.TokenUseClaim)?.Value;
        if (!string.Equals(tokenUse, TeamboardJwtValidationParameters.UserTokenUseValue, StringComparison.Ordinal))
        {
            return new TeamboardJwtValidationResult(false, "invalid_token_use");
        }

        return new TeamboardJwtValidationResult(true, null);
    }
}