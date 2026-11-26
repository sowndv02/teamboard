using IdentityService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence;

/// <summary>The only DbContext of identity-service; entities are added slice by slice (design 6.2).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<RegisteredClient> RegisteredClients => Set<RegisteredClient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_token");
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(88);
            entity.Property(token => token.FamilyId).HasColumnName("family_id");
            entity.Property(token => token.UserId).HasColumnName("user_id");
            entity.Property(token => token.ClientId).HasColumnName("client_id").HasMaxLength(64);
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
            entity.Property(token => token.UsedAt).HasColumnName("used_at");
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
            entity.HasIndex(token => token.FamilyId);
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.ToTable("login_attempt");
            entity.HasKey(attempt => new { attempt.EmailNorm, attempt.Ip });
            entity.Property(attempt => attempt.EmailNorm).HasColumnName("email_norm").HasMaxLength(320);
            entity.Property(attempt => attempt.Ip).HasColumnName("ip").HasMaxLength(64);
            entity.Property(attempt => attempt.Failures).HasColumnName("failures");
            entity.Property(attempt => attempt.LockedUntil).HasColumnName("locked_until");
            entity.Property(attempt => attempt.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.EmailNorm).IsUnique();
            entity.Property(user => user.EmailNorm).HasColumnName("email_norm").HasMaxLength(320);
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(user => user.PasswordSalt).HasColumnName("password_salt").IsRequired();
            entity.Property(user => user.PasswordIterations).HasColumnName("password_iterations");
            entity.Property(user => user.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<RegisteredClient>(entity =>
        {
            entity.ToTable("client");
            entity.HasKey(client => client.ClientId);
            entity.Property(client => client.ClientId).HasColumnName("client_id").HasMaxLength(64);
            entity.Property(client => client.ClientType).HasColumnName("client_type").HasMaxLength(16).IsRequired();
            entity.Property(client => client.SecretHash).HasColumnName("secret_hash");
            entity.Property(client => client.SecretSalt).HasColumnName("secret_salt");
            entity.Property(client => client.SecretIterations).HasColumnName("secret_iterations");
            entity.Property(client => client.RedirectUris).HasColumnName("redirect_uris").HasColumnType("jsonb");
            entity.Property(client => client.AllowedScopes).HasColumnName("allowed_scopes").HasColumnType("jsonb");
            entity.Property(client => client.UpdatedAt).HasColumnName("updated_at");
        });
    }
}