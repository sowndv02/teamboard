using Npgsql;
using Testcontainers.PostgreSql;

namespace IdentityService.Tests.Support;

/// <summary>PostgreSQL thật trong container cho test tích hợp (không mock DB; Testcontainers đã duyệt ở Confirm log #2 của plan TB-021).</summary>
internal sealed class PostgresDatabase : IAsyncDisposable
{
    public const string Password = "test-only-pw-9f3a";

    // Ghim theo config/tool-versions.yaml (người dùng chọn 2026-10-25); compose dùng cùng tag ở B4.
    private const string Image = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container;

    private PostgresDatabase(PostgreSqlContainer container) => _container = container;

    public string ConnectionString => _container.GetConnectionString();

    public static async Task<PostgresDatabase> StartAsync()
    {
        DockerCli.RequireDocker();
        var container = new PostgreSqlBuilder(Image).WithPassword(Password).Build();
        await container.StartAsync();
        return new PostgresDatabase(container);
    }

    public Task StopAsync() => _container.StopAsync();

    public async Task<bool> HasMigrationHistoryAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL", connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    public async Task<long> CountMigrationRecordsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM \"__EFMigrationsHistory\"", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
