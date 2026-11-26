using IdentityService.Persistence;
using IdentityService.Security.Passwords;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Features.Login;

internal static class LoginCredentialVerifier
{
    internal static async Task<Guid?> TryVerifyUserIdAsync(
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
}