using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using IdentityService.Features.Login;
using IdentityService.Persistence;
using IdentityService.RateLimiting;
using IdentityService.Security;
using IdentityService.Security.Passwords;
using IdentityService.Security.Refresh;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Teamboard.Shared.Http;

namespace IdentityService.Features.ChangePassword;

internal static class ChangePasswordEndpoints
{
    public static void MapChangePasswordEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/password", ChangePasswordAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        IdentityDbContext db,
        IPasswordHasher hasher,
        IdentityMetrics metrics,
        LoginLockoutService lockout,
        RefreshTokenService refreshTokens,
        IClientIpResolver clientIpResolver,
        IOptions<PasswordOptions> passwordOptions,
        ILoggerFactory loggerFactory,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
        {
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        var currentPassword = request.CurrentPassword ?? string.Empty;
        var newPassword = request.NewPassword ?? string.Empty;
        var options = passwordOptions.Value;

        if (currentPassword.Length > options.MaxLength)
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context, "Password length is invalid."));
        }

        if (newPassword.Length < options.MinLength || newPassword.Length > options.MaxLength)
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context, "Password length is invalid."));
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context, "Password length is invalid."));
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            _ = hasher.VerifyPassword(currentPassword, LoginDummyCredentials.Stored);
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        var clientIp = clientIpResolver.Resolve(context);
        var lockCheck = await lockout.CheckLockAsync(user.EmailNorm, clientIp, cancellationToken);
        if (lockCheck.IsLocked)
        {
            SetRetryAfter(context, lockCheck.RetryAfterSeconds);
            _ = VerifyStoredPassword(currentPassword, user, hasher);
            metrics.RecordLockout();
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        if (!VerifyStoredPassword(currentPassword, user, hasher))
        {
            var failure = await lockout.RecordFailureAsync(user.EmailNorm, clientIp, cancellationToken);
            metrics.RecordLoginFailure();
            if (failure.IsLocked)
            {
                SetRetryAfter(context, failure.RetryAfterSeconds);
            }

            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        var hashed = hasher.HashPassword(newPassword);
        user.PasswordHash = hashed.Hash;
        user.PasswordSalt = hashed.Salt;
        user.PasswordIterations = hashed.Iterations;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await refreshTokens.RevokeAllForUserAsync(userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await lockout.RecordSuccessAsync(user.EmailNorm, clientIp, cancellationToken);
        metrics.RecordPasswordChange();
        loggerFactory.CreateLogger("IdentityService.ChangePassword")
            .LogInformation("password_change event=password_change UserId={UserId}", userId);

        return TypedResults.NoContent();
    }

    private static bool VerifyStoredPassword(string password, Persistence.Entities.User user, IPasswordHasher hasher)
    {
        var stored = new HashedPassword(user.PasswordHash, user.PasswordSalt, user.PasswordIterations);
        return hasher.VerifyPassword(password, stored);
    }

    private static void SetRetryAfter(HttpContext context, int seconds) =>
        context.Response.Headers.RetryAfter = Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);

    private sealed record ChangePasswordRequest(
        [property: JsonPropertyName("current_password")] string? CurrentPassword,
        [property: JsonPropertyName("new_password")] string? NewPassword);
}