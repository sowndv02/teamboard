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

    /// <summary>Distinct stored password hashes (TB-029 @S04); fails until the users table exists.</summary>
    public async Task<IReadOnlyList<string>> GetDistinctPasswordHashesAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        // Table/column names must match the AddUsers migration in coding-start (plan B1).
        const string sql =
            """
            SELECT DISTINCT password_hash::text AS hash
            FROM users
            WHERE password_hash IS NOT NULL
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    public async Task<bool> OAuthClientExistsAsync(string clientId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = """
            SELECT 1
            FROM client
            WHERE client_id = @client_id
            LIMIT 1
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("client_id", clientId);
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task<string?> GetOAuthClientTypeAsync(string clientId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = """
            SELECT client_type::text
            FROM client
            WHERE client_id = @client_id
            LIMIT 1
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("client_id", clientId);
        return await command.ExecuteScalarAsync() as string;
    }

    public async Task<(string? Hash, string? PlaintextEcho)> GetOAuthClientSecretColumnsAsync(string clientId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        const string sql = """
            SELECT secret_hash::text, secret_plaintext_echo::text
            FROM client
            WHERE client_id = @client_id
            LIMIT 1
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("client_id", clientId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return (null, null);
        }

        return (reader.IsDBNull(0) ? null : reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
