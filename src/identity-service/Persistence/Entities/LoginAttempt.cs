namespace IdentityService.Persistence.Entities;

public sealed class LoginAttempt
{
    public required string EmailNorm { get; init; }

    public required string Ip { get; init; }

    public int Failures { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}