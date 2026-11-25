using Teamboard.Shared.Http;

namespace IdentityService;

/// <summary>Error codes owned by identity-service.</summary>
public static class IdentityErrorCodes
{
    public static readonly ErrorDefinition EmailAlreadyExists = new(
        "EMAIL_ALREADY_EXISTS",
        409,
        "An account with this email already exists.");
}