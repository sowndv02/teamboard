using IdentityService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence;

/// <summary>The only DbContext of identity-service; entities are added slice by slice (design 6.2).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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