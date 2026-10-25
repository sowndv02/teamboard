using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence;

/// <summary>The only DbContext of identity-service; entities are added slice by slice (design 6.2).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options);
