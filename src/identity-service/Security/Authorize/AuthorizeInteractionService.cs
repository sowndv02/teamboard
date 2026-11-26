using System.Security.Claims;
using IdentityService.Persistence.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizeInteractionService(
    AuthorizeRequestService authorizeRequests,
    AuthorizationCodeIssuer codeIssuer)
{
    public async Task<string> CompleteAndBuildRedirectAsync(
        AuthorizeRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var code = await codeIssuer.IssueAsync(request, userId, cancellationToken);
        await authorizeRequests.RemoveAsync(request.Id, cancellationToken);
        return AuthorizeRedirectBuilder.BuildAuthorizationRedirect(request.RedirectUri, request.State, code);
    }

    public static Guid? TryGetIdpUserId(ClaimsPrincipal? principal)
    {
        var raw = principal?.FindFirst(IdentityIdpAuthentication.UserIdClaimType)?.Value;
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    public static async Task<Guid?> TryGetIdpUserIdAsync(HttpContext httpContext)
    {
        var auth = await httpContext.AuthenticateAsync(IdentityIdpAuthentication.AuthenticationScheme);
        if (!auth.Succeeded)
        {
            return null;
        }

        return TryGetIdpUserId(auth.Principal);
    }
}