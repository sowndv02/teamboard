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
    private const string LoginPendingHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Sign-in pending</title></head>
        <body><p>Sign-in for this authorization request is not available yet.</p></body>
        </html>
        """;

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

    private static Task<IResult> HandleConnectAuthorizeAsync(
        HttpContext httpContext,
        IConfiguration configuration,
        AuthorizeRequestValidator validator)
    {
        if (!configuration.GetValue(FeatureFlagKey, false))
        {
            return Task.FromResult(Results.NotFound());
        }

        var query = AuthorizeQueryParser.Parse(httpContext.Request.Query);
        var result = validator.Validate(query);
        if (!result.IsValid)
        {
            return Task.FromResult<IResult>(Results.Content(
                InvalidAuthorizeHtml,
                contentType: "text/html",
                statusCode: StatusCodes.Status400BadRequest));
        }

        return Task.FromResult<IResult>(Results.Content(
            LoginPendingHtml,
            contentType: "text/html",
            statusCode: StatusCodes.Status501NotImplemented));
    }
}