using Npgsql;
using Testcontainers.PostgreSql;

namespace IdentityService.Tests.Support;

/// <summary>Real PostgreSQL in a container for integration tests (no DB mock; Testcontainers approved in Confirm log #2 of plan TB-021).</summary>
internal sealed class PostgresDatabase : IAsyncDisposable
{
    public const string Password = "test-only-pw-9f3a";

    // Pinned per config/tool-versions.yaml (chosen by the user 2026-10-25); compose uses the same tag in B4.
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
