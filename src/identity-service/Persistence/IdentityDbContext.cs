using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence;

/// <summary>DbContext duy nhất của identity-service; thực thể thêm dần theo từng lát (design 6.2).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options);
