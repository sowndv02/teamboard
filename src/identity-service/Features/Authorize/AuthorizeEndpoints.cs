using IdentityService.RateLimiting;
using IdentityService.Security.Authorize;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.Authorize;

internal static class AuthorizeEndpoints
{
    public const string ConnectAuthorizePath = "/connect/authorize";

    private const string FeatureFlagKey = "Identity:Features:AuthorizationCodeEnabled";
    private const string InvalidAuthorizeHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Authorization failed</title></head>
        <body><p>The authorization request could not be processed.</p></body>
        </html>
        """;

    public static void MapAuthorizeEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapGet(ConnectAuthorizePath, HandleConnectAuthorizeAsync)
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> HandleConnectAuthorizeAsync(
        HttpContext httpContext,
        IConfiguration configuration,
        AuthorizeRequestValidator validator,
        AuthorizeRequestService authorizeRequests,
        AuthorizeInteractionService authorizeInteraction,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue(FeatureFlagKey, false))
        {
            return Results.NotFound();
        }

        var query = AuthorizeQueryParser.Parse(httpContext.Request.Query);
        var result = validator.Validate(query);
        if (!result.IsValid)
        {
            return Results.Content(
                InvalidAuthorizeHtml,
                contentType: "text/html",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var userId = await AuthorizeInteractionService.TryGetIdpUserIdAsync(httpContext);
        if (userId is { } authenticatedUserId)
        {
            var pending = await authorizeRequests.CreateAsync(query, cancellationToken);
            var location = await authorizeInteraction.CompleteAndBuildRedirectAsync(
                pending,
                authenticatedUserId,
                cancellationToken);
            return Results.Redirect(location);
        }

        var authorizeRequest = await authorizeRequests.CreateAsync(query, cancellationToken);
        var loginLocation = $"/login?requestId={authorizeRequest.Id}";
        return Results.Redirect(loginLocation);
    }
}