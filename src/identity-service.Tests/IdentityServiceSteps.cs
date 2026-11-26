using System.Diagnostics.Metrics;
using System.Globalization;
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
    private const string LoginPath = "/api/v1/auth/login";
    private const string HashDurationInstrument = "password_hash_duration_seconds";
    private const string LoginFailuresInstrument = "login_failures_total";
    private const string LockoutsInstrument = "lockout_total";
    private const string LockoutMaxFailuresKey = "Identity:Lockout:MaxFailures";
    private const string LockoutDurationSecondsKey = "Identity:Lockout:LockoutDurationSeconds";
    private const string RateLimitEnabledKey = "Identity:RateLimiting:Enabled";
    private const string RateLimitPermitKey = "Identity:RateLimiting:PermitLimit";
    private const string RateLimitWindowKey = "Identity:RateLimiting:WindowSeconds";
    private const string SigningKeysFileKey = "Identity:SigningKeys:JsonFile";
    private const string JwtIssuerKey = "Identity:Jwt:Issuer";
    private const string JwtAudienceKey = "Identity:Jwt:Audience";
    private const string JwtLifetimeKey = "Identity:Jwt:AccessTokenLifetimeSeconds";
    private const string BddJwtIssuer = "https://identity.test.local";
    private const string BddJwtAudience = "teamboard";
    private const int BddJwtLifetimeSeconds = 120;
    private const string AccessTokensIssuedInstrument = "access_tokens_issued_total";
    private const string JwksServedInstrument = "jwks_served_total";

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
    private long _loginFailureMeasurements;
    private long _lockoutMeasurements;
    private long _accessTokenMeasurements;
    private long _jwksServedMeasurements;
    private bool _observeLoginFailuresOnly;
    private bool _observeLockoutsOnly;
    private bool _observeAccessTokensOnly;
    private bool _observeJwksServedOnly;
    private bool _omitSigningKeys;
    private readonly Dictionary<string, string?> _settings = new(StringComparer.Ordinal);
    private readonly List<int> _burstStatusCodes = [];
    private (int First, int Second)? _concurrentStatuses;

    private PostgresDatabase Database => _database ?? throw new InvalidOperationException("No database yet.");

    [BeforeScenario]
    public void ResetScenario()
    {
        _settings.Clear();
        _settings[RateLimitEnabledKey] = "false";
        _burstStatusCodes.Clear();
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = false;
        _observeJwksServedOnly = false;
        _omitSigningKeys = false;
        _loginFailureMeasurements = 0;
        _lockoutMeasurements = 0;
        _hashDurationSamples = 0;
        _jwksServedMeasurements = 0;
    }

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
    public void GivenMetrics()
    {
        _observeLoginFailuresOnly = false;
        StartMeterListener();
    }

    [Given("identity-service metrics are being observed for login failures")]
    public void GivenLoginFailureMetrics()
    {
        _observeLoginFailuresOnly = true;
        _observeLockoutsOnly = false;
        _loginFailureMeasurements = 0;
        StartMeterListener();
    }

    [Given("identity-service metrics are being observed for lockouts")]
    public void GivenLockoutMetrics()
    {
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = true;
        _lockoutMeasurements = 0;
        StartMeterListener();
    }

    [Given("identity-service lockout is configured with max failures {int} and lockout duration {int} seconds")]
    public void GivenLockoutConfigured(int maxFailures, int durationSeconds)
    {
        _settings[LockoutMaxFailuresKey] = maxFailures.ToString(CultureInfo.InvariantCulture);
        _settings[LockoutDurationSecondsKey] = durationSeconds.ToString(CultureInfo.InvariantCulture);
    }

    [Given("identity-service rate limiting is enabled with permit limit {int} per minute")]
    public void GivenRateLimitEnabled(int permitLimit)
    {
        _settings[RateLimitEnabledKey] = "true";
        _settings[RateLimitPermitKey] = permitLimit.ToString(CultureInfo.InvariantCulture);
        _settings[RateLimitWindowKey] = "60";
    }

    [Given("identity-service rate limiting is disabled")]
    public void GivenRateLimitDisabled() => _settings[RateLimitEnabledKey] = "false";

    [Given("identity-service access token issuance is configured for BDD tests")]
    public void GivenAccessTokenIssuanceForBdd()
    {
        _omitSigningKeys = false;
        _settings[SigningKeysFileKey] = TestSigningKeyPaths.JsonFile;
        _settings[JwtIssuerKey] = BddJwtIssuer;
        _settings[JwtAudienceKey] = BddJwtAudience;
        _settings[JwtLifetimeKey] = BddJwtLifetimeSeconds.ToString(CultureInfo.InvariantCulture);
    }

    [Given("identity-service JWKS signing keys are configured for BDD tests")]
    public void GivenJwksSigningKeysForBdd()
    {
        _omitSigningKeys = false;
        _settings[SigningKeysFileKey] = TestSigningKeyPaths.JsonFile;
        _settings[JwtIssuerKey] = "http://localhost:6061";
        _settings[JwtAudienceKey] = BddJwtAudience;
    }

    [Given("identity-service is running for JWKS without a database")]
    public void GivenRunningForJwksWithoutDatabase() =>
        _client = StartService(UnreachableConnection, migrate: false);

    [Given("identity-service metrics are being observed for JWKS served")]
    public void GivenJwksServedMetrics()
    {
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = false;
        _observeJwksServedOnly = true;
        _jwksServedMeasurements = 0;
        StartMeterListener();
    }

    [Given("identity-service is configured without signing keys")]
    public void GivenWithoutSigningKeys() => _omitSigningKeys = true;

    [Given("identity-service metrics are being observed for issued access tokens")]
    public void GivenAccessTokenMetrics()
    {
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = true;
        StartMeterListener();
    }

    [Given("the email and IP pair for {string} is locked from failed logins")]
    public async Task GivenPairLocked(string email) =>
        await SendFailedLoginsUntilLockoutAsync(email, clientIp: null, count: null);

    [Given("client IP {string} has locked the pair for email {string}")]
    public async Task GivenClientIpLockedPair(string clientIp, string email) =>
        await SendFailedLoginsUntilLockoutAsync(email, clientIp, count: null);

    [Given("a user is registered with email {string} and password {string}")]
    public async Task GivenRegisteredUser(string email, string password) => await SendRegisterAsync(email, password, expectedStatus: 201);

    [When("I send a registration request with email {string} and password {string}")]
    public Task WhenRegister(string email, string password) => SendRegisterAsync(email, password);

    [When("I send a login request with email {string} and password {string}")]
    public Task WhenLogin(string email, string password) => SendLoginAsync(email, password);

    [When("I send {int} login requests with email {string} and password {string}")]
    public async Task WhenManyLogins(int count, string email, string password)
    {
        _burstStatusCodes.Clear();
        for (var i = 0; i < count; i++)
        {
            await SendLoginAsync(email, password);
            _burstStatusCodes.Add((int)_response!.StatusCode);
        }
    }

    [When("I send a login request with email {string} and password {string} from client IP {string}")]
    public Task WhenLoginFromIp(string email, string password, string clientIp) =>
        SendLoginAsync(email, password, clientIp);

    [When("I send {int} login requests with email {string} and password {string} from client IP {string}")]
    public async Task WhenManyLoginsFromIp(int count, string email, string password, string clientIp)
    {
        _burstStatusCodes.Clear();
        for (var i = 0; i < count; i++)
        {
            await SendLoginAsync(email, password, clientIp);
            _burstStatusCodes.Add((int)_response!.StatusCode);
        }
    }

    [When("I send {int} registration requests with email {string} and password {string} from client IP {string}")]
    public async Task WhenManyRegistrationsFromIp(int count, string email, string password, string clientIp)
    {
        _burstStatusCodes.Clear();
        for (var i = 0; i < count; i++)
        {
            await SendRegisterAsync(email, password, clientIp: clientIp);
            _burstStatusCodes.Add((int)_response!.StatusCode);
        }
    }

    [When("I retry login with email {string} and password {string} until success within {int} seconds")]
    public async Task WhenRetryLoginUntilSuccess(string email, string password, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            await SendLoginAsync(email, password);
            if ((int)_response!.StatusCode == 200)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }

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

    [Then("the login response body does not contain {string}, {string}, {string}, {string}, or {string}")]
    public void ThenLoginBodyExcludesSecrets(string a, string b, string c, string d, string e)
    {
        Assert.DoesNotContain(a, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(b, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(c, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(d, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(e, _body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the login response body does not contain {string}, {string}, or {string}")]
    public void ThenLoginBodyExcludesThree(string a, string b, string c)
    {
        Assert.DoesNotContain(a, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(b, _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(c, _body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the login response has a non-empty id")]
    public void ThenLoginResponseHasId()
    {
        using var document = JsonDocument.Parse(_body);
        var id = document.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.True(Guid.TryParse(id, out _), "id should be a GUID string.");
    }

    [Then("the login response includes a Bearer access token with expires_in")]
    public void ThenLoginResponseIncludesBearerAccessToken() => JwtBddAssertions.AssertBearerAccessTokenShape(_body);

    [Then("the access token header uses algorithm RS256 and includes kid")]
    public void ThenAccessTokenHeaderRs256()
    {
        var token = JwtBddAssertions.ParseAccessTokenFromLoginBody(_body);
        JwtBddAssertions.AssertRs256HeaderWithKid(token);
    }

    [Then("the access token lifetime is about {int} seconds")]
    public void ThenAccessTokenLifetimeAbout(int expectedSeconds)
    {
        var token = JwtBddAssertions.ParseAccessTokenFromLoginBody(_body);
        JwtBddAssertions.AssertLifetimeAbout(token, expectedSeconds);
    }

    [Then("the access token includes required user claims for audience {string}")]
    public void ThenAccessTokenUserClaims(string audience)
    {
        var userId = JwtBddAssertions.GetUserIdFromLoginBody(_body);
        var token = JwtBddAssertions.ParseAccessTokenFromLoginBody(_body);
        JwtBddAssertions.AssertUserClaims(token, userId, audience);
    }

    [Then("the login response body does not contain a PEM private key")]
    public void ThenLoginBodyNoPem() => JwtBddAssertions.AssertNoPrivateKeyPem(_body);

    [Then("the login response has no access_token property")]
    public void ThenLoginResponseNoAccessToken() => JwtBddAssertions.AssertNoAccessTokenProperty(_body);

    [Then("no login error field contains a three-part JWT")]
    public void ThenLoginErrorFieldsNotJwt() => JwtBddAssertions.AssertNoJwtLikeStringValues(_body);

    [Then("the error title is {string}")]
    public void ThenErrorTitle(string expectedTitle)
    {
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(expectedTitle, document.RootElement.GetProperty("title").GetString());
    }

    [Then("the JWKS document includes public key with kid {string}")]
    public void ThenJwksHasKid(string kid)
    {
        using var document = JsonDocument.Parse(_body);
        var keys = document.RootElement.GetProperty("keys");
        Assert.True(keys.GetArrayLength() > 0, "Expected at least one JWK.");
        var found = keys.EnumerateArray().Any(entry =>
            string.Equals(entry.GetProperty("kid").GetString(), kid, StringComparison.Ordinal));
        Assert.True(found, $"Expected kid {kid} in JWKS.");
    }

    [Then("the JWKS response body does not contain private key material")]
    public void ThenJwksNoPrivateMaterial()
    {
        Assert.DoesNotContain("BEGIN", _body, StringComparison.Ordinal);
        Assert.DoesNotContain("privateKeyPem", _body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"d\":", _body, StringComparison.Ordinal);
    }

    [Then("the JWKS response has cache control public max-age {int}")]
    public void ThenJwksCacheControl(int maxAge)
    {
        Assert.True(_response!.Headers.TryGetValues("Cache-Control", out var values));
        var header = values.First();
        Assert.Contains("public", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"max-age={maxAge}", header, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the identity counter {string} increased by at least {int}")]
    public void ThenIdentityCounter(string instrumentName, int minimumIncrease)
    {
        var observed = instrumentName switch
        {
            LoginFailuresInstrument => _loginFailureMeasurements,
            LockoutsInstrument => _lockoutMeasurements,
            AccessTokensIssuedInstrument => _accessTokenMeasurements,
            JwksServedInstrument => _jwksServedMeasurements,
            _ => throw new ArgumentOutOfRangeException(nameof(instrumentName), instrumentName, "Unknown identity counter."),
        };
        Assert.True(
            observed >= minimumIncrease,
            $"Expected at least {minimumIncrease} measurement(s) for {instrumentName}, got {observed}.");
    }

    [Then("the response has a Retry-After header greater than 0")]
    public void ThenRetryAfterPositive()
    {
        Assert.NotNull(_response);
        Assert.True(_response.Headers.TryGetValues("Retry-After", out var values), "Retry-After header is missing.");
        var header = Assert.Single(values);
        Assert.True(int.TryParse(header, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds));
        Assert.True(seconds > 0, $"Retry-After should be positive, got {header}.");
    }

    [Then("the response has no Retry-After header")]
    public void ThenNoRetryAfter() => Assert.False(_response!.Headers.Contains("Retry-After"));

    [Then("the response body does not contain the word {string}")]
    public void ThenBodyExcludesWord(string word) =>
        Assert.DoesNotContain(word, _body, StringComparison.OrdinalIgnoreCase);

    [Then("no response had status code {int}")]
    public void ThenNoBurstStatus(int forbidden)
    {
        Assert.NotEmpty(_burstStatusCodes);
        Assert.DoesNotContain(forbidden, _burstStatusCodes);
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

    [When("I send GET to {string}")]
    public async Task WhenSendGet(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
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

    private async Task SendRegisterAsync(string email, string password, int? expectedStatus = null, string? clientIp = null)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        using var request = new HttpRequestMessage(HttpMethod.Post, RegisterPath)
        {
            Content = JsonContent.Create(new { email, password }),
        };
        if (clientIp is not null)
        {
            request.Headers.TryAddWithoutValidation(TestClientIpStartupFilter.HeaderName, clientIp);
        }

        _response = await client.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
        if (expectedStatus is not null)
        {
            Assert.Equal(expectedStatus, (int)_response.StatusCode);
        }
    }

    private async Task SendLoginAsync(string email, string password, string? clientIp = null)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        using var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = JsonContent.Create(new { email, password }),
        };
        if (clientIp is not null)
        {
            request.Headers.TryAddWithoutValidation(TestClientIpStartupFilter.HeaderName, clientIp);
        }

        _response = await client.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    private async Task SendFailedLoginsUntilLockoutAsync(string email, string? clientIp, int? count)
    {
        var attempts = count ?? int.Parse(_settings.GetValueOrDefault(LockoutMaxFailuresKey, "5")!, CultureInfo.InvariantCulture);
        _burstStatusCodes.Clear();
        for (var i = 0; i < attempts; i++)
        {
            await SendLoginAsync(email, "WrongPass123456", clientIp);
            _burstStatusCodes.Add((int)_response!.StatusCode);
        }

        // Establish locked state in Given: without lockout this fails here instead of vacuous greens in @S04–@S06.
        await SendLoginAsync(email, "ValidPass123456", clientIp);
        Assert.Equal(401, (int)_response!.StatusCode);
        Assert.True(_response.Headers.Contains("Retry-After"), "Expected Retry-After while asserting the email and IP pair is locked.");
    }

    private void StartMeterListener()
    {
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (!instrument.Meter.Name.Contains("identity", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_observeLoginFailuresOnly)
                {
                    if (instrument.Name == LoginFailuresInstrument)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }

                    return;
                }

                if (_observeLockoutsOnly)
                {
                    if (instrument.Name == LockoutsInstrument)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }

                    return;
                }

                if (_observeAccessTokensOnly)
                {
                    if (instrument.Name == AccessTokensIssuedInstrument)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }

                    return;
                }

                if (_observeJwksServedOnly)
                {
                    if (instrument.Name == JwksServedInstrument)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }

                    return;
                }

                if (instrument.Name == HashDurationInstrument)
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
        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == LoginFailuresInstrument)
            {
                Interlocked.Add(ref _loginFailureMeasurements, measurement);
            }

            if (instrument.Name == LockoutsInstrument)
            {
                Interlocked.Add(ref _lockoutMeasurements, measurement);
            }

            if (instrument.Name == AccessTokensIssuedInstrument)
            {
                Interlocked.Add(ref _accessTokenMeasurements, measurement);
            }

            if (instrument.Name == JwksServedInstrument)
            {
                Interlocked.Add(ref _jwksServedMeasurements, measurement);
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
            foreach (var (key, value) in _settings)
            {
                builder.UseSetting(key, value);
            }

            if (!_omitSigningKeys)
            {
                if (!_settings.ContainsKey(SigningKeysFileKey))
                {
                    builder.UseSetting(SigningKeysFileKey, TestSigningKeyPaths.JsonFile);
                }

                builder.UseSetting(JwtIssuerKey, _settings.GetValueOrDefault(JwtIssuerKey, BddJwtIssuer));
                builder.UseSetting(JwtAudienceKey, _settings.GetValueOrDefault(JwtAudienceKey, BddJwtAudience));
                builder.UseSetting(
                    JwtLifetimeKey,
                    _settings.GetValueOrDefault(JwtLifetimeKey, BddJwtLifetimeSeconds.ToString(CultureInfo.InvariantCulture)));
            }

            if (_logs is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(_logs));
            }

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
                configureServices?.Invoke(services);
            });
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
