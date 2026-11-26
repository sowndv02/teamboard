namespace IdentityService.Persistence.Entities;

public sealed class AuthorizationCode
{
    public string CodeHash { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public string CodeChallengeMethod { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}