namespace IdentityService.Persistence.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string EmailNorm { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public int PasswordIterations { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}