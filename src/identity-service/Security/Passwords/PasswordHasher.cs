namespace IdentityService.Security.Passwords;

/// <summary>Stub for test-init; real PBKDF2 logic is implemented in coding-start (TB-029 B2).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public HashedPassword HashPassword(string password) =>
        throw new NotImplementedException("TB-029 B2: PBKDF2 password hashing is not implemented yet.");

    public bool VerifyPassword(string password, HashedPassword stored) =>
        throw new NotImplementedException("TB-029 B2: PBKDF2 password verification is not implemented yet.");
}