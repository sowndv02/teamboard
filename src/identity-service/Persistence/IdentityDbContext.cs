using IdentityService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence;

/// <summary>The only DbContext of identity-service; entities are added slice by slice (design 6.2).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}