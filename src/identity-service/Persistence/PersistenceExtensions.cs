using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Teamboard.Shared.ServiceDefaults;

namespace IdentityService.Persistence;

public static class PersistenceExtensions
{
    public const string ConnectionStringName = "IdentityDb";

    public static WebApplicationBuilder AddIdentityPersistence(this WebApplicationBuilder builder)
    {
        // Thiếu chuỗi kết nối thì dừng ngay lúc khởi động; thông báo không chứa giá trị cấu hình.
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string 'ConnectionStrings:{ConnectionStringName}' is required.");
        }

        builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName).ValidateOnStart();
        builder.Services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

        // Ready phụ thuộc DB (design 6.3): trả 503 khi không kết nối được, body không có chi tiết.
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
        await database.MigrateAsync();
    }
}
