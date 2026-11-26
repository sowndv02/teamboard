using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Auth;

namespace Teamboard.Shared.Auth.Tests.Support;

internal static class HttpJwksTestValidatorFactory
{
    internal static TeamboardJwtTokenValidator Create(WebApplicationFactory<Program> identityFactory, Uri jwksUri)
    {
        var httpClient = identityFactory.CreateClient();
        var options = Options.Create(new TeamboardAuthJwtOptions
        {
            JwksUri = jwksUri,
            ValidIssuer = "http://localhost:6061",
            ValidAudience = "teamboard",
        });
        var httpSource = new HttpJwksKeySetSource(httpClient, options);
        var cache = new JwksKeySetCache(httpSource, TimeProvider.System, options);
        return new TeamboardJwtTokenValidator(cache, options);
    }
}