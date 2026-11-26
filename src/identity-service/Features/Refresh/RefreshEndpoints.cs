using System.Text.Json.Serialization;
using IdentityService.RateLimiting;
using IdentityService.Security;
using IdentityService.Security.Jwt;
using IdentityService.Security.Refresh;
using IdentityService.Security.Signing;
using Microsoft.AspNetCore.RateLimiting;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Refresh;

internal static class RefreshEndpoints
{
    public static void MapRefreshEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/refresh", RefreshAsync)
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        RefreshTokenService refreshTokens,
        IAccessTokenIssuer accessTokenIssuer,
        ISigningKeyStore signingKeyStore,
        IdentityMetrics metrics,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context));
        }

        if (!signingKeyStore.HasActiveKey)
        {
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.ServiceUnavailable, context));
        }

        var rotated = await refreshTokens.TryRotateAsync(request.RefreshToken, cancellationToken);
        if (rotated is not { } win)
        {
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        var issued = accessTokenIssuer.IssueForUser(win.UserId);
        metrics.RecordRefreshRotation();
        metrics.RecordRefreshTokenIssued();
        metrics.RecordAccessTokenIssued();

        return TypedResults.Ok(new RefreshResponse(
            issued.Token,
            "Bearer",
            issued.ExpiresInSeconds,
            win.RefreshPlaintext,
            win.RefreshExpiresInSeconds));
    }

    private sealed record RefreshRequest(
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record RefreshResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn);
}