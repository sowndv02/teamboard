using System.Globalization;
using System.Text.Json.Serialization;
using IdentityService.Features.Register;
using IdentityService.Persistence;
using IdentityService.RateLimiting;
using IdentityService.Security;
using IdentityService.Security.Passwords;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Login;

internal static class LoginEndpoints
{
    public static void MapLoginEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/login", LoginAsync)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IdentityDbContext db,
        IPasswordHasher hasher,
        IdentityMetrics metrics,
        LoginLockoutService lockout,
        IClientIpResolver clientIpResolver,
        IOptions<PasswordOptions> passwordOptions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryNormalize(request.Email ?? string.Empty, out var emailNorm))
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context, "Email is invalid."));
        }

        var password = request.Password ?? string.Empty;
        var options = passwordOptions.Value;
        if (password.Length < options.MinLength || password.Length > options.MaxLength)
        {
            return TypedResults.Problem(ProblemFactory.Create(SharedErrors.ValidationFailed, context, "Password length is invalid."));
        }

        var clientIp = clientIpResolver.Resolve(context);
        var lockCheck = await lockout.CheckLockAsync(emailNorm, clientIp, cancellationToken);
        if (lockCheck.IsLocked)
        {
            SetRetryAfter(context, lockCheck.RetryAfterSeconds);
            await VerifyCredentialsAsync(password, emailNorm, db, hasher, cancellationToken);
            metrics.RecordLockout();
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
        }

        var credentialValid = await VerifyCredentialsAsync(password, emailNorm, db, hasher, cancellationToken);
        if (credentialValid is { } userId)
        {
            await lockout.RecordSuccessAsync(emailNorm, clientIp, cancellationToken);
            return TypedResults.Ok(new LoginResponse(userId));
        }

        var failure = await lockout.RecordFailureAsync(emailNorm, clientIp, cancellationToken);
        metrics.RecordLoginFailure();
        if (failure.IsLocked)
        {
            SetRetryAfter(context, failure.RetryAfterSeconds);
        }

        return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
    }

    private static async Task<Guid?> VerifyCredentialsAsync(
        string password,
        string emailNorm,
        IdentityDbContext db,
        IPasswordHasher hasher,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailNorm == emailNorm, cancellationToken);

        if (user is not null)
        {
            var stored = new HashedPassword(user.PasswordHash, user.PasswordSalt, user.PasswordIterations);
            if (hasher.VerifyPassword(password, stored))
            {
                return user.Id;
            }

            return null;
        }

        _ = hasher.VerifyPassword(password, LoginDummyCredentials.Stored);
        return null;
    }

    private static void SetRetryAfter(HttpContext context, int seconds) =>
        context.Response.Headers.RetryAfter = Math.Max(1, seconds).ToString(CultureInfo.InvariantCulture);

    private sealed record LoginRequest(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("password")] string? Password);

    private sealed record LoginResponse(Guid Id);
}