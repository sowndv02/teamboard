using System.Text.Json.Serialization;
using IdentityService.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Refresh;

internal static class RefreshEndpoints
{
    public static void MapRefreshEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/refresh", RefreshAsync)
            .Produces(StatusCodes.Status501NotImplemented)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static IResult RefreshAsync(RefreshRequest request, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context));
        }

        return Results.Problem(
            title: "Not Implemented",
            detail: "Refresh token rotation is not implemented yet.",
            statusCode: StatusCodes.Status501NotImplemented);
    }

    private sealed record RefreshRequest(
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}