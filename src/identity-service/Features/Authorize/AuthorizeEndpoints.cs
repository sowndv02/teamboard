using IdentityService.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.Authorize;

internal static class AuthorizeEndpoints
{
    public const string ConnectAuthorizePath = "/connect/authorize";

    private const string FeatureFlagKey = "Identity:Features:AuthorizationCodeEnabled";

    public static void MapAuthorizeEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapGet(ConnectAuthorizePath, HandleConnectAuthorizeAsync)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status501NotImplemented);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static Task<IResult> HandleConnectAuthorizeAsync(HttpContext _, IConfiguration configuration) =>
        Task.FromResult(configuration.GetValue(FeatureFlagKey, false)
            ? Results.StatusCode(StatusCodes.Status501NotImplemented)
            : Results.NotFound());
}