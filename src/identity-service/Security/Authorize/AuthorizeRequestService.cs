using IdentityService.Persistence;
using IdentityService.Persistence.Entities;
using IdentityService.Security.Token;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizeRequestService(IdentityDbContext db, TimeProvider timeProvider)
{
    internal static readonly TimeSpan RequestTtl = TimeSpan.FromMinutes(15);

    public async Task<AuthorizeRequest> CreateAsync(AuthorizeQuery query, CancellationToken cancellationToken)
    {
        var scopes = ScopeValidator.ParseScopeParameter(query.Scope);
        var now = timeProvider.GetUtcNow();
        var entity = new AuthorizeRequest
        {
            Id = Guid.NewGuid(),
            ClientId = query.ClientId ?? string.Empty,
            RedirectUri = query.RedirectUri ?? string.Empty,
            Scope = string.Join(' ', scopes),
            State = query.State ?? string.Empty,
            CodeChallenge = query.CodeChallenge ?? string.Empty,
            CodeChallengeMethod = query.CodeChallengeMethod ?? string.Empty,
            Nonce = query.Nonce ?? string.Empty,
            ExpiresAt = now.Add(RequestTtl),
            CreatedAt = now,
        };

        db.AuthorizeRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<AuthorizeRequest?> TryGetActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await db.AuthorizeRequests.AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == id && request.ExpiresAt > now, cancellationToken);
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken) =>
        db.AuthorizeRequests.Where(request => request.Id == id).ExecuteDeleteAsync(cancellationToken);
}