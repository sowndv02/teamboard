using System.Text.Json.Serialization;
using IdentityService.RateLimiting;
using IdentityService.Security.Refresh;
using Microsoft.AspNetCore.RateLimiting;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Logout;

internal static class LogoutEndpoints
{
    public static void MapLogoutEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/logout", LogoutAsync)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        RefreshTokenService refreshTokens,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context));
        }

        var revoked = await refreshTokens.TryLogoutAsync(request.RefreshToken, cancellationToken);
        if (!revoked)
        {
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        return TypedResults.NoContent();
    }

    private sealed record LogoutRequest(
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}