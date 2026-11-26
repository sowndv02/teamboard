using System.Globalization;
using System.Security.Claims;
using IdentityService.Features.Login;
using IdentityService.Features.Register;
using IdentityService.Persistence;
using IdentityService.Security;
using IdentityService.Security.Authorize;
using IdentityService.Security.Passwords;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IdentityService.Pages;

public sealed class LoginModel(
    AuthorizeRequestService authorizeRequests,
    AuthorizeInteractionService authorizeInteraction,
    LoginLockoutService lockout,
    IPasswordHasher hasher,
    IdentityDbContext db,
    IdentityMetrics metrics,
    IClientIpResolver clientIpResolver,
    IOptions<PasswordOptions> passwordOptions) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public Guid? AuthorizeRequestId { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet([FromQuery(Name = "requestId")] Guid? requestId)
    {
        if (requestId is { } id)
        {
            AuthorizeRequestId = id;
            AuthorizePendingRequestCookie.Set(HttpContext, id);
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        AuthorizeRequestId ??= AuthorizePendingRequestCookie.TryRead(Request);
        if (!EmailAddress.TryNormalize(Email, out var emailNorm))
        {
            ErrorMessage = "Sign-in failed. Check your email and password.";
            return Page();
        }

        var password = Password ?? string.Empty;
        var passwordPolicy = passwordOptions.Value;
        if (password.Length < passwordPolicy.MinLength || password.Length > passwordPolicy.MaxLength)
        {
            ErrorMessage = "Sign-in failed. Check your email and password.";
            return Page();
        }

        var clientIp = clientIpResolver.Resolve(HttpContext);
        var lockCheck = await lockout.CheckLockAsync(emailNorm, clientIp, cancellationToken);
        if (lockCheck.IsLocked)
        {
            SetRetryAfter(lockCheck.RetryAfterSeconds);
            await LoginCredentialVerifier.TryVerifyUserIdAsync(password, emailNorm, db, hasher, cancellationToken);
            metrics.RecordLockout();
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        var userId = await LoginCredentialVerifier.TryVerifyUserIdAsync(password, emailNorm, db, hasher, cancellationToken);
        if (userId is not { } authenticatedUserId)
        {
            var failure = await lockout.RecordFailureAsync(emailNorm, clientIp, cancellationToken);
            metrics.RecordLoginFailure();
            if (failure.IsLocked)
            {
                SetRetryAfter(failure.RetryAfterSeconds);
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            ErrorMessage = "Sign-in failed. Check your email and password.";
            return Page();
        }

        await lockout.RecordSuccessAsync(emailNorm, clientIp, cancellationToken);
        await SignInIdpAsync(authenticatedUserId);

        if (AuthorizeRequestId is not { } requestId)
        {
            return RedirectToPage();
        }

        var request = await authorizeRequests.TryGetActiveAsync(requestId, cancellationToken);
        if (request is null)
        {
            ErrorMessage = "Your sign-in request has expired. Start again from the application.";
            return Page();
        }

        var redirect = await authorizeInteraction.CompleteAndBuildRedirectAsync(
            request,
            authenticatedUserId,
            cancellationToken);
        AuthorizePendingRequestCookie.Clear(HttpContext);
        return Redirect(redirect);
    }

    private async Task SignInIdpAsync(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(IdentityIdpAuthentication.UserIdClaimType, userId.ToString())],
            IdentityIdpAuthentication.AuthenticationScheme);
        await HttpContext.SignInAsync(
            IdentityIdpAuthentication.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    private void SetRetryAfter(int seconds) =>
        Response.Headers.RetryAfter = Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);
}