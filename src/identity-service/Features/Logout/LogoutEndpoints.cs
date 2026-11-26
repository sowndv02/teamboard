namespace IdentityService.Features.Logout;

internal static class LogoutEndpoints
{
    public static void MapLogoutEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        _ = enableRateLimiting;
        routes.MapPost("/api/v1/auth/logout", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
    }
}