using Microsoft.AspNetCore.WebUtilities;

namespace IdentityService.Security.Authorize;

internal static class AuthorizeRedirectBuilder
{
    internal static string BuildAuthorizationRedirect(string redirectUri, string state, string code) =>
        QueryHelpers.AddQueryString(
            redirectUri,
            new Dictionary<string, string?>
            {
                ["code"] = code,
                ["state"] = state,
            });
}