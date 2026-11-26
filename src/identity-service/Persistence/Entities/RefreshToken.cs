namespace IdentityService.Persistence.Entities;

public sealed class RefreshToken
{
    public string TokenHash { get; set; } = string.Empty;

    public Guid FamilyId { get; set; }

    public Guid UserId { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}