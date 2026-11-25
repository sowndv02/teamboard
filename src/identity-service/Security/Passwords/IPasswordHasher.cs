namespace IdentityService.Security.Passwords;

/// <summary>Hashes and verifies passwords with PBKDF2 (implementation arrives in coding-start TB-029).</summary>
public interface IPasswordHasher
{
    HashedPassword HashPassword(string password);

    bool VerifyPassword(string password, HashedPassword stored);
}

public sealed record HashedPassword(string Hash, string Salt, int Iterations);