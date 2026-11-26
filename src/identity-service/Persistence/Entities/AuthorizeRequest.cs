namespace IdentityService.Persistence.Entities;

public sealed class AuthorizeRequest
{
    public Guid Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public string CodeChallengeMethod { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}