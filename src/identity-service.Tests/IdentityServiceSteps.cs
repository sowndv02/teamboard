using System.Text.Json;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Reqnroll;
using Xunit;

namespace IdentityService.Tests;

[Binding]
public sealed class IdentityServiceSteps : IAsyncDisposable
{
    private const string ConnectionKey = "ConnectionStrings:IdentityDb";
    private const string MigrateKey = "Database:MigrateOnStartup";
    private const string ThrowPath = "/__test/throw";
    private const string ThrowMessage = "boom: internal detail must not leak";

    // Nothing listens on port 1: simulates a stopped DB without needing a container.
    private const string UnreachableConnection =
        "Host=127.0.0.1;Port=1;Database=identity;Username=identity;Password=unused-test-value;Timeout=1";

    private readonly List<WebApplicationFactory<Program>> _factories = [];
    private PostgresDatabase? _database;
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private Exception? _thrown;
    private string _body = string.Empty;

    private PostgresDatabase Database => _database ?? throw new InvalidOperationException("No database yet.");

    [Given("identity-service is configured to use a database that is not running")]
    public void GivenServiceWithUnreachableDatabase() => _client = StartService(UnreachableConnection, migrate: false);

    [Given("an empty PostgreSQL database is running")]
    public async Task GivenDatabase() => _database = await PostgresDatabase.StartAsync();

    [Given("identity-service is configured to use that database and applies migrations on startup")]
    public void GivenServiceWithDatabase() => _client = StartService(Database.ConnectionString, migrate: true);

    [Given("the database has stopped")]
    public async Task GivenDatabaseStopped() => await Database.StopAsync();

    [Given("identity-service has an operation that causes an unexpected error")]
    public void GivenServiceWithFailingOperation() =>
        _client = StartService(UnreachableConnection, migrate: false, services => services.AddSingleton<IStartupFilter, ThrowingEndpointFilter>());

    [When("I send {string} to {string}")]
    public async Task WhenSend(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [When("I trigger that operation")]
    public async Task WhenTriggerFailure()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ThrowPath);
            _response = await _client!.SendAsync(request);
            _body = await _response.Content.ReadAsStringAsync();
        }
        catch (Exception exception)
        {
            // The test host rethrows the unhandled error instead of returning 500 when no error handler exists; let the Then step fail.
            _thrown = exception;
        }
    }

    [When("identity-service starts twice in a row against that database and applies migrations")]
    public async Task WhenStartTwice()
    {
        for (var run = 0; run < 2; run++)
        {
            using var client = StartService(Database.ConnectionString, migrate: true);
            await client.GetAsync("/health/live");
            await DisposeFactoriesAsync();
        }
    }

    [When("identity-service starts against that database without enabling auto-apply of migrations")]
    public async Task WhenStartWithoutMigration()
    {
        using var client = StartService(Database.ConnectionString, migrate: false);
        _response = await client.GetAsync("/health/live");
        _body = await _response.Content.ReadAsStringAsync();
        await DisposeFactoriesAsync();
    }

    [Then("identity-service still returns status code {int} for the liveness check")]
    public void ThenStillAlive(int expected) => ThenStatus(expected);

    [Then("the service telemetry is named {string}")]
    public void ThenTelemetryServiceName(string serviceName)
    {
        var tracerProvider = _factories[^1].Services.GetService<TracerProvider>();
        Assert.True(tracerProvider is not null, "identity-service has not registered OpenTelemetry (AddServiceDefaults).");
        Assert.Contains(tracerProvider.GetResource().Attributes, attribute => attribute.Key == "service.name" && (string?)attribute.Value == serviceName);
    }

    [Then("the status code is {int}")]
    public void ThenStatus(int expected)
    {
        Assert.True(_thrown is null, $"The request threw an unhandled error instead of returning {expected}: {_thrown?.Message}");
        Assert.Equal(expected, (int)_response!.StatusCode);
    }

    [Then("the response body contains no password or connection string")]
    public void ThenBodyHasNoConnectionDetails()
    {
        Assert.DoesNotContain(PostgresDatabase.Password, _body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", _body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the error follows the standard format with code {string} and a trace id")]
    public void ThenStandardError(string code)
    {
        Assert.Equal("application/problem+json", _response!.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    [Then("the error has a correlation id")]
    public void ThenHasCorrelationId()
    {
        // No correlation middleware yet (gateway slice 2): represented by traceId, only check the field is present (plan TB-021, Confirm log #5).
        using var document = JsonDocument.Parse(_body);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    [Then("the error body contains no stack trace or machine name")]
    public void ThenNoInternalDetails()
    {
        Assert.DoesNotContain("   at ", _body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.MachineName, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ThrowMessage, _body, StringComparison.Ordinal);
    }

    [Then("the database migration history has exactly one baseline record")]
    public async Task ThenOneBaselineRecord()
    {
        Assert.True(await Database.HasMigrationHistoryAsync(), "The migration history table was not created.");
        Assert.Equal(1, await Database.CountMigrationRecordsAsync());
    }

    [Then("the database has no migration history")]
    public async Task ThenNoMigrationHistory() => Assert.False(await Database.HasMigrationHistoryAsync());

    public async ValueTask DisposeAsync()
    {
        _response?.Dispose();
        _client?.Dispose();
        await DisposeFactoriesAsync();
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private HttpClient StartService(string connectionString, bool migrate, Action<IServiceCollection>? configureServices = null)
    {
        // The connection string and migration flag come through configuration (environment variables in a real environment), not hardcoded in the service.
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Production like compose: in Development ASP.NET adds an error page with a stack trace, hiding the behavior under test.
            builder.UseEnvironment("Production");
            builder.UseSetting(ConnectionKey, connectionString);
            builder.UseSetting(MigrateKey, migrate ? "true" : "false");
            if (configureServices is not null)
            {
                builder.ConfigureServices(configureServices);
            }
        });
        _factories.Add(factory);
        return factory.CreateClient();
    }

    private async Task DisposeFactoriesAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        _factories.Clear();
    }

    /// <summary>
    /// Adds a throwing endpoint after the service pipeline: the app pipeline falls through when no route matches,
    /// so the error still goes through the service's error handler (if any).
    /// </summary>
    private sealed class ThrowingEndpointFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map(ThrowPath, branch => branch.Run(_ => throw new InvalidOperationException(ThrowMessage)));
        };
    }
}
