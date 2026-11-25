using System.Text.Json.Serialization;
using IdentityService.Features.Register;
using IdentityService.Persistence;
using IdentityService.Security;
using IdentityService.Security.Passwords;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Login;

internal static class LoginEndpoints
{
    public static void MapLoginEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/v1/auth/login", LoginAsync)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IdentityDbContext db,
        IPasswordHasher hasher,
        IdentityMetrics metrics,
        IOptions<PasswordOptions> passwordOptions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        _ = context.Connection.RemoteIpAddress;

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

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailNorm == emailNorm, cancellationToken);

        if (user is not null)
        {
            var stored = new HashedPassword(user.PasswordHash, user.PasswordSalt, user.PasswordIterations);
            if (hasher.VerifyPassword(password, stored))
            {
                return TypedResults.Ok(new LoginResponse(user.Id));
            }
        }
        else
        {
            _ = hasher.VerifyPassword(password, LoginDummyCredentials.Stored);
        }

        metrics.RecordLoginFailure();
        return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.InvalidCredentials, context));
    }

    private sealed record LoginRequest(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("password")] string? Password);

    private sealed record LoginResponse(Guid Id);
}