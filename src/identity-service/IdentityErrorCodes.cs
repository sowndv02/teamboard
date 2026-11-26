using Teamboard.Shared.Http;

namespace IdentityService;

/// <summary>Error codes owned by identity-service.</summary>
public static class IdentityErrorCodes
{
    public static readonly ErrorDefinition EmailAlreadyExists = new(
        "EMAIL_ALREADY_EXISTS",
        409,
        "An account with this email already exists.");

    public static readonly ErrorDefinition InvalidCredentials = new(
        "INVALID_CREDENTIALS",
        401,
        "Invalid email or password.",
        "Invalid email or password.");

    public static readonly ErrorDefinition RateLimitExceeded = new(
        "RATE_LIMIT_EXCEEDED",
        429,
        "Too many requests.");

    public static readonly ErrorDefinition ServiceUnavailable = new(
        "SERVICE_UNAVAILABLE",
        503,
        "The service is not ready to issue tokens.");
}