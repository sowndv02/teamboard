using IdentityService.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.Token;

internal static class TokenEndpoints
{
    public const string ConnectTokenPath = "/connect/token";

    public static void MapTokenEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost(ConnectTokenPath, HandleConnectTokenAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static IResult HandleConnectTokenAsync(IConfiguration configuration)
    {
        if (!configuration.GetValue("Identity:Features:ClientCredentialsEnabled", false))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Json(
            new OAuthTokenErrorResponse("server_error", "Client credentials grant is not implemented."),
            statusCode: StatusCodes.Status501NotImplemented);
    }

    private sealed record OAuthTokenErrorResponse(string Error, string ErrorDescription);
}