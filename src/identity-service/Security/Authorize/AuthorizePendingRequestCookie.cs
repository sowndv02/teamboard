using Microsoft.AspNetCore.Http;

namespace IdentityService.Security.Authorize;

internal static class AuthorizePendingRequestCookie
{
    internal const string CookieName = "tb.authz.pending";

    internal static void Set(HttpResponse response, Guid requestId) =>
        response.Cookies.Append(
            CookieName,
            requestId.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                MaxAge = AuthorizeRequestService.RequestTtl,
                IsEssential = true,
            });

    internal static Guid? TryRead(HttpRequest request) =>
        Guid.TryParse(request.Cookies[CookieName], out var id) ? id : null;

    internal static void Clear(HttpResponse response) => response.Cookies.Delete(CookieName);
}