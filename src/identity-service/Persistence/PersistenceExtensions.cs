using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Teamboard.Shared.ServiceDefaults;

namespace IdentityService.Persistence;

public static class PersistenceExtensions
{
    public const string ConnectionStringName = "IdentityDb";

    public static WebApplicationBuilder AddIdentityPersistence(this WebApplicationBuilder builder)
    {
        // Fail fast at startup when the connection string is missing; the message does not contain configuration values.
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string 'ConnectionStrings:{ConnectionStringName}' is required.");
        }

        builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName).ValidateOnStart();
        builder.Services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

        // Ready depends on the DB (design 6.3): returns 503 when it cannot connect, with no detail in the body.
        builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("database", tags: [ServiceDefaultsExtensions.ReadyTag]);
        return builder;
    }

    public static async Task ApplyMigrationsIfEnabledAsync(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.MigrateOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database;
        await database.MigrateAsync(app.Lifetime.ApplicationStopping);
    }
}
