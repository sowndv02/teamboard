using Microsoft.AspNetCore.Http;

namespace IdentityService.Security.Authorize;

internal static class AuthorizePendingRequestCookie
{
    internal const string CookieName = "tb.authz.pending";

    internal static void Set(HttpContext context, Guid requestId) =>
        context.Response.Cookies.Append(
            CookieName,
            requestId.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = AuthorizeRequestService.RequestTtl,
                IsEssential = true,
            });

    internal static Guid? TryRead(HttpRequest request) =>
        Guid.TryParse(request.Cookies[CookieName], out var id) ? id : null;

    internal static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
}