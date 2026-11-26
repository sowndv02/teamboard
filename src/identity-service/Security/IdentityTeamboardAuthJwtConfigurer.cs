using IdentityService.Security.Jwt;
using IdentityService.Security.Signing;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Auth;

namespace IdentityService.Security;

internal sealed class IdentityTeamboardAuthJwtConfigurer(
    IOptions<IdentityJwtOptions> identityJwt,
    IOptions<SigningKeysOptions> signingKeys) : IConfigureOptions<TeamboardAuthJwtOptions>
{
    public void Configure(TeamboardAuthJwtOptions options)
    {
        options.ValidIssuer = identityJwt.Value.Issuer;
        options.ValidAudience = identityJwt.Value.Audience;
        var jsonFile = signingKeys.Value.JsonFile;
        if (!string.IsNullOrWhiteSpace(jsonFile))
        {
            options.StaticJwksFile = jsonFile;
        }
    }
}