using System.Text.Json.Serialization;
using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using IdentityService.RateLimiting;
using IdentityService.Security.Passwords;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Teamboard.Shared.Http;

namespace IdentityService.Features.Register;

internal static class RegisterEndpoints
{
    public static void MapRegisterEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost("/api/v1/auth/register", RegisterAsync)
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IdentityDbContext db,
        IPasswordHasher hasher,
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

        var hashed = hasher.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            EmailNorm = emailNorm,
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            PasswordIterations = hashed.Iterations,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            return TypedResults.Problem(ProblemFactory.Create(IdentityErrorCodes.EmailAlreadyExists, context));
        }

        var location = $"/api/v1/users/{user.Id}";
        return TypedResults.Created(location, new RegisterResponse(user.Id));
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }

    private sealed record RegisterRequest(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("password")] string? Password);

    private sealed record RegisterResponse(Guid Id);
}