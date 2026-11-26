using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizationCodeIssuer(
    IdentityDbContext db,
    TimeProvider timeProvider,
    IdentityMetrics metrics)
{
    internal static readonly TimeSpan CodeLifetime = TimeSpan.FromSeconds(60);

    public async Task<string> IssueAsync(AuthorizeRequest request, Guid userId, CancellationToken cancellationToken)
    {
        var plaintext = AuthorizationCodeGenerator.GeneratePlaintextCode();
        var hash = AuthorizationCodeHasher.Hash(plaintext);
        var now = timeProvider.GetUtcNow();

        db.AuthorizationCodes.Add(new AuthorizationCode
        {
            CodeHash = hash,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Scope = request.Scope,
            Nonce = request.Nonce,
            UserId = userId,
            ExpiresAt = now.Add(CodeLifetime),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        metrics.RecordAuthorizationCodeIssued();
        return plaintext;
    }
}