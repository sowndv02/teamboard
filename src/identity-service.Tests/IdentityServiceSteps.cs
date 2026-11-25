using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private const string RegisterPath = "/api/v1/auth/register";
    private const string HashDurationInstrument = "password_hash_duration_seconds";

    // Nothing listens on port 1: simulates a stopped DB without needing a container.
    private const string UnreachableConnection =
        "Host=127.0.0.1;Port=1;Database=identity;Username=identity;Password=unused-test-value;Timeout=1";

    private readonly List<WebApplicationFactory<Program>> _factories = [];
    private PostgresDatabase? _database;
    private HttpClient? _client;
    private HttpResponseMessage? _response;
    private Exception? _thrown;
    private string _body = string.Empty;
    private CapturingLoggerProvider? _logs;
    private MeterListener? _meterListener;
    private long _hashDurationSamples;
    private (int First, int Second)? _concurrentStatuses;

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

    [Given("identity-service log capture is enabled")]
    public void GivenLogCapture() => _logs = new CapturingLoggerProvider();

    [Given("identity-service metrics are being observed")]
    public void GivenMetrics() => StartMeterListener();

    [Given("a user is registered with email {string} and password {string}")]
    public async Task GivenRegisteredUser(string email, string password) => await SendRegisterAsync(email, password, expectedStatus: 201);

    [When("I send a registration request with email {string} and password {string}")]
    public Task WhenRegister(string email, string password) => SendRegisterAsync(email, password);

    [When("I send a registration request with email {string} and a password longer than {int} characters")]
    public Task WhenRegisterLongPassword(string email, int minAllowed) =>
        SendRegisterAsync(email, new string('a', minAllowed + 1));

    [When("I register users {string} and {string} with the same password {string}")]
    public async Task WhenRegisterTwoUsers(string emailA, string emailB, string password)
    {
        await SendRegisterAsync(emailA, password, expectedStatus: 201);
        await SendRegisterAsync(emailB, password, expectedStatus: 201);
    }

    [When("two registration requests run concurrently for email {string} and password {string}")]
    public async Task WhenConcurrentRegister(string email, string password)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        var taskA = client.PostAsJsonAsync(RegisterPath, new { email, password });
        var taskB = client.PostAsJsonAsync(RegisterPath, new { email, password });
        await Task.WhenAll(taskA, taskB);
        using var responseA = await taskA;
        using var responseB = await taskB;
        _concurrentStatuses = ((int)responseA.StatusCode, (int)responseB.StatusCode);
    }

    [Then("the registration response body does not contain {string}, {string}, or {string}")]
    public void ThenBodyExcludesSecrets(string a, string b, string c)
    {
        Assert.DoesNotContain(a, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(b, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(c, _body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the registration response has a non-empty id")]
    public void ThenResponseHasId()
    {
        using var document = JsonDocument.Parse(_body);
        var id = document.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.True(Guid.TryParse(id, out _), "id should be a GUID string.");
    }

    [Then("the registration response has a Location header for the created user")]
    public void ThenResponseHasLocation()
    {
        Assert.NotNull(_response);
        Assert.True(_response.Headers.TryGetValues("Location", out var values), "Location header is missing.");
        var location = values.Single();
        Assert.Matches("/api/v1/users/[0-9a-fA-F-]{36}", location);
    }

    [Then("the database stores two different password hashes for those users")]
    public async Task ThenDifferentHashes()
    {
        var hashes = await Database.GetDistinctPasswordHashesAsync();
        Assert.Equal(2, hashes.Count);
        Assert.NotEqual(hashes[0], hashes[1]);
    }

    [Then("no captured log entry contains {string}")]
    public void ThenLogsExclude(string secret)
    {
        Assert.NotNull(_logs);
        foreach (var entry in _logs.Entries)
        {
            Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
            if (entry.Exception is not null)
            {
                Assert.DoesNotContain(secret, entry.Exception.ToString()!, StringComparison.Ordinal);
            }
        }
    }

    [Then("the identity histogram {string} recorded at least one sample")]
    public void ThenHistogram(string instrumentName)
    {
        Assert.Equal(HashDurationInstrument, instrumentName);
        Assert.True(_hashDurationSamples > 0, $"Expected at least one sample for {instrumentName}.");
    }

    [Then("one registration result has status {int} and the other has status {int}")]
    public void ThenConcurrentStatuses(int first, int second)
    {
        Assert.NotNull(_concurrentStatuses);
        var statuses = new[] { _concurrentStatuses.Value.First, _concurrentStatuses.Value.Second };
        Assert.Contains(first, statuses);
        Assert.Contains(second, statuses);
        Assert.NotEqual(statuses[0], statuses[1]);
    }

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

    [Then("the database migration history has exactly {int} migration records")]
    public async Task ThenMigrationRecordCount(int expected)
    {
        Assert.True(await Database.HasMigrationHistoryAsync(), "The migration history table was not created.");
        Assert.Equal(expected, await Database.CountMigrationRecordsAsync());
    }

    [Then("the database has no migration history")]
    public async Task ThenNoMigrationHistory() => Assert.False(await Database.HasMigrationHistoryAsync());

    public async ValueTask DisposeAsync()
    {
        _response?.Dispose();
        _client?.Dispose();
        _meterListener?.Dispose();
        await DisposeFactoriesAsync();
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SendRegisterAsync(string email, string password, int? expectedStatus = null)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        _response = await client.PostAsJsonAsync(RegisterPath, new { email, password });
        _body = await _response.Content.ReadAsStringAsync();
        if (expectedStatus is not null)
        {
            Assert.Equal(expectedStatus, (int)_response.StatusCode);
        }
    }

    private void StartMeterListener()
    {
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name.Contains("identity", StringComparison.OrdinalIgnoreCase)
                    && instrument.Name == HashDurationInstrument)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            if (instrument.Name == HashDurationInstrument)
            {
                Interlocked.Increment(ref _hashDurationSamples);
            }
        });
        _meterListener.Start();
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
            if (_logs is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(_logs));
            }

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
