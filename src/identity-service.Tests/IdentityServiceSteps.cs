using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Cli;
using IdentityService.Security.Clients;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private const string RefreshPath = "/api/v1/auth/refresh";
    private const string LogoutPath = "/api/v1/auth/logout";
    private const string ChangePasswordPath = "/api/v1/auth/password";
    private const string ConnectTokenPath = "/connect/token";
    private const string ConnectAuthorizePath = "/connect/authorize";
    private const string IdentityLoginPath = "/login";
    private const string ClientCredentialsEnabledKey = "Identity:Features:ClientCredentialsEnabled";
    private const string AuthorizationCodeEnabledKey = "Identity:Features:AuthorizationCodeEnabled";
    private const string RefreshLifetimeKey = "Identity:RefreshToken:LifetimeSeconds";
    private const string RefreshDefaultClientKey = "Identity:RefreshToken:DefaultClientId";
    private const string ClientRegistryEnabledKey = "Identity:Clients:Enabled";
    private const string ClientSecretKeyPrefix = "Identity:Clients:Secrets:";
    private const int BddRefreshLifetimeSeconds = 120;
    private const string HashDurationInstrument = "password_hash_duration_seconds";
    private const string LoginFailuresInstrument = "login_failures_total";
    private const string LockoutsInstrument = "lockout_total";
    private const string LockoutMaxFailuresKey = "Identity:Lockout:MaxFailures";
    private const string LockoutDurationSecondsKey = "Identity:Lockout:LockoutDurationSeconds";
    private const string RateLimitEnabledKey = "Identity:RateLimiting:Enabled";
    private const string RateLimitPermitKey = "Identity:RateLimiting:PermitLimit";
    private const string RateLimitWindowKey = "Identity:RateLimiting:WindowSeconds";
    private const string SigningKeysFileKey = "Identity:SigningKeys:JsonFile";
    private const string AuthJwksFileKey = "Auth:Jwt:StaticJwksFile";
    private const string AuthJwtIssuerKey = "Auth:Jwt:ValidIssuer";
    private const string AuthJwtAudienceKey = "Auth:Jwt:ValidAudience";
    private const string JwtIssuerKey = "Identity:Jwt:Issuer";
    private const string JwtAudienceKey = "Identity:Jwt:Audience";
    private const string JwtLifetimeKey = "Identity:Jwt:AccessTokenLifetimeSeconds";
    private const string BddJwtIssuer = "https://identity.test.local";
    private const string BddJwtAudience = "teamboard";
    private const int BddJwtLifetimeSeconds = 120;
    private const string AccessTokensIssuedInstrument = "access_tokens_issued_total";
    private const string JwksServedInstrument = "jwks_served_total";
    private const string RefreshReuseInstrument = "refresh_reuse_total";

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
    private long _refreshReuseMeasurements;
    private long _refreshReuseBaseline;
    private bool _observeLoginFailuresOnly;
    private bool _observeLockoutsOnly;
    private bool _observeAccessTokensOnly;
    private bool _observeJwksServedOnly;
    private bool _observeRefreshReuseOnly;
    private bool _omitSigningKeys;
    private string? _rotationKeysPath;
    private FakeRotationTimeProvider? _rotationTime;
    private int? _lastRotationExitCode;
    private string? _storedAccessToken;
    private string? _storedRefreshToken;
    private string? _latestRotatedRefreshToken;
    private string? _storedLoginAccessToken;
    private string? _changePasswordAccessToken;
    private string? _firstSessionRefreshToken;
    private string? _secondSessionRefreshToken;
    private int _concurrentRefreshSuccess;
    private int _concurrentRefreshFailure;
    private readonly Dictionary<string, string?> _settings = new(StringComparer.Ordinal);
    private readonly List<int> _burstStatusCodes = [];
    private (int First, int Second)? _concurrentStatuses;
    private bool? _redirectUriMatchResult;
    private readonly List<string> _confidentialClientSecrets = [];
    private bool _preserveHttpCookies;
    private string? _pendingAuthorizeRedirectUri;
    private string? _pendingAuthorizeState;

    private PostgresDatabase Database => _database ?? throw new InvalidOperationException("No database yet.");

    [BeforeScenario]
    public void ResetScenario()
    {
        _settings.Clear();
        _settings[RateLimitEnabledKey] = "false";
        _settings[ClientRegistryEnabledKey] = "false";
        _burstStatusCodes.Clear();
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = false;
        _observeJwksServedOnly = false;
        _observeRefreshReuseOnly = false;
        _omitSigningKeys = false;
        _rotationKeysPath = null;
        _rotationTime = null;
        _lastRotationExitCode = null;
        _storedAccessToken = null;
        _storedRefreshToken = null;
        _latestRotatedRefreshToken = null;
        _storedLoginAccessToken = null;
        _changePasswordAccessToken = null;
        _firstSessionRefreshToken = null;
        _secondSessionRefreshToken = null;
        _refreshReuseMeasurements = 0;
        _refreshReuseBaseline = 0;
        _concurrentRefreshSuccess = 0;
        _concurrentRefreshFailure = 0;
        _loginFailureMeasurements = 0;
        _lockoutMeasurements = 0;
        _hashDurationSamples = 0;
        _jwksServedMeasurements = 0;
        _redirectUriMatchResult = null;
        _confidentialClientSecrets.Clear();
        _preserveHttpCookies = false;
        _pendingAuthorizeRedirectUri = null;
        _pendingAuthorizeState = null;
    }

    [Given("identity-service is configured to use a database that is not running")]
    public void GivenServiceWithUnreachableDatabase() => _client = StartService(UnreachableConnection, migrate: false);

    [Given("identity-service oauth client registry is configured for BDD tests")]
    public void GivenOAuthClientRegistryForBdd()
    {
        _settings[ClientRegistryEnabledKey] = "true";
        _settings["Identity:Clients:Seed:0:ClientId"] = "teamboard";
        _settings["Identity:Clients:Seed:0:ClientType"] = "Public";
        _settings["Identity:Clients:Seed:0:RedirectUris:0"] = "https://localhost/callback";
        _settings["Identity:Clients:Seed:0:AllowedScopes:0"] = "openid";
    }

    [Given("identity-service client credentials grant is enabled for BDD tests")]
    public void GivenClientCredentialsEnabled() => _settings[ClientCredentialsEnabledKey] = "true";

    [Given("identity-service authorization code grant is enabled for BDD tests")]
    public void GivenAuthorizationCodeEnabled() => _settings[AuthorizationCodeEnabledKey] = "true";

    [Given("identity-service http client preserves cookies between requests")]
    public void GivenHttpClientPreservesCookies() => _preserveHttpCookies = true;

    [Given(@"identity-service client {string} has redirect uri {string} and allowed scopes:")]
    public void GivenClientRedirectAndScopes(string clientId, string redirectUri, Table scopesTable)
    {
        var index = FindClientSeedIndex(clientId);
        if (index < 0)
        {
            index = NextClientSeedIndex();
            _settings[$"Identity:Clients:Seed:{index}:ClientId"] = clientId;
            _settings[$"Identity:Clients:Seed:{index}:ClientType"] = "Public";
        }

        _settings[$"Identity:Clients:Seed:{index}:RedirectUris:0"] = redirectUri;
        var scopeIndex = 0;
        foreach (var row in scopesTable.Rows)
        {
            _settings[$"Identity:Clients:Seed:{index}:AllowedScopes:{scopeIndex}"] = row["scope"];
            scopeIndex++;
        }
    }

    [When(@"I GET the authorize endpoint for client {string} with redirect_uri {string} state {string} scope {string} pkce challenge {string} method {string}")]
    public async Task WhenGetAuthorizeWithState(
        string clientId,
        string redirectUri,
        string state,
        string scope,
        string challenge,
        string method) =>
        await SendAuthorizeGetAsync(clientId, redirectUri, state, scope, challenge, method);

    [When(@"I GET the authorize endpoint for client {string} with redirect_uri {string} without state scope {string} pkce challenge {string} method {string}")]
    public async Task WhenGetAuthorizeWithoutState(
        string clientId,
        string redirectUri,
        string scope,
        string challenge,
        string method) =>
        await SendAuthorizeGetAsync(clientId, redirectUri, state: null, scope, challenge, method);

    [When(@"I GET the authorize endpoint for client {string} with redirect_uri {string} state {string} scope {string} without pkce challenge method {string}")]
    public async Task WhenGetAuthorizeWithoutChallenge(
        string clientId,
        string redirectUri,
        string state,
        string scope,
        string method) =>
        await SendAuthorizeGetAsync(clientId, redirectUri, state, scope, challenge: null, method);

    [Then(@"the response has no Location header containing {string}")]
    public void ThenResponseHasNoLocationContaining(string fragment)
    {
        if (_response!.Headers.Location is { } location)
        {
            Assert.DoesNotContain(fragment, location.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [When("I GET the identity login page")]
    public async Task WhenGetIdentityLoginPage()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, IdentityLoginPath);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [When(@"I submit the identity login page for the pending authorize request with email {string} and password {string}")]
    public async Task WhenSubmitIdentityLoginPageForAuthorize(string email, string password)
    {
        await SubmitIdentityLoginPageAsync(email, password, requireAuthorizeRedirect: true);
    }

    [Then(@"the authorize response redirects to {string} with state {string} and an authorization code")]
    public void ThenAuthorizeRedirectWithCode(string redirectUri, string expectedState)
    {
        Assert.Equal(302, (int)_response!.StatusCode);
        var location = _response.Headers.Location?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(location), "Expected Location header with authorization redirect.");
        var absolute = Uri.TryCreate(location, UriKind.Absolute, out var abs)
            ? abs
            : new Uri(new Uri("http://localhost"), location);
        Assert.StartsWith(redirectUri.TrimEnd('/'), absolute.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(absolute.Query);
        Assert.False(string.IsNullOrWhiteSpace(query["code"].ToString()), "Expected code query parameter.");
        Assert.Equal(expectedState, query["state"].ToString());
    }

    [Then("the response has Content-Security-Policy frame-ancestors none")]
    public void ThenCspFrameAncestorsNone()
    {
        Assert.True(
            _response!.Headers.TryGetValues("Content-Security-Policy", out var values),
            "Content-Security-Policy header is missing.");
        Assert.Contains(
            values!,
            value => value.Contains("frame-ancestors 'none'", StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendAuthorizeGetAsync(
        string clientId,
        string redirectUri,
        string? state,
        string scope,
        string? challenge,
        string method)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("client_id", clientId),
            new("redirect_uri", redirectUri),
            new("response_type", "code"),
            new("scope", scope),
            new("code_challenge_method", method),
        };
        if (state is not null)
        {
            query.Add(new KeyValuePair<string, string?>("state", state));
        }

        if (challenge is not null)
        {
            query.Add(new KeyValuePair<string, string?>("code_challenge", challenge));
        }

        var path = QueryString.Create(query).ToUriComponent();
        using var request = new HttpRequestMessage(HttpMethod.Get, ConnectAuthorizePath + path);
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
        _pendingAuthorizeRedirectUri = redirectUri;
        _pendingAuthorizeState = state;
    }

    private async Task SubmitIdentityLoginPageAsync(string email, string password, bool requireAuthorizeRedirect)
    {
        if (requireAuthorizeRedirect)
        {
            var location = _response!.Headers.Location?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(location), "Expected redirect to login after authorize.");
            Assert.Contains("/login", location, StringComparison.OrdinalIgnoreCase);
        }

        var loginGetPath = requireAuthorizeRedirect
            ? (_response!.Headers.Location!.IsAbsoluteUri
                ? _response.Headers.Location.PathAndQuery
                : _response.Headers.Location.ToString())
            : IdentityLoginPath;

        using (var getLogin = new HttpRequestMessage(HttpMethod.Get, loginGetPath))
        {
            _response = await _client!.SendAsync(getLogin);
            _body = await _response.Content.ReadAsStringAsync();
        }

        var antiforgery = ExtractAntiforgeryToken(_body);
        var form = new List<KeyValuePair<string, string>>
        {
            new("Email", email),
            new("Password", password),
        };
        if (!string.IsNullOrEmpty(antiforgery))
        {
            form.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiforgery));
        }

        using var postLogin = new HttpRequestMessage(HttpMethod.Post, IdentityLoginPath)
        {
            Content = new FormUrlEncodedContent(form),
        };
        _response = await _client.SendAsync(postLogin);
        _body = await _response.Content.ReadAsStringAsync();
    }

    private static string? ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""\s+(?:type=""hidden""\s+)?value=""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : null;
    }

    private int FindClientSeedIndex(string clientId)
    {
        for (var idx = 0; idx < 32; idx++)
        {
            var key = $"Identity:Clients:Seed:{idx}:ClientId";
            if (_settings.TryGetValue(key, out var value) && string.Equals(value, clientId, StringComparison.Ordinal))
            {
                return idx;
            }
        }

        return -1;
    }

    [Given("identity-service has confidential oauth client {string} with secret {string}")]
    public void GivenConfidentialOAuthClient(string clientId, string secret)
    {
        _confidentialClientSecrets.Add(secret);
        var index = NextClientSeedIndex();
        _settings[$"Identity:Clients:Seed:{index}:ClientId"] = clientId;
        _settings[$"Identity:Clients:Seed:{index}:ClientType"] = "Confidential";
        _settings[$"Identity:Clients:Seed:{index}:RedirectUris:0"] = "https://localhost/callback";
        _settings[$"Identity:Clients:Seed:{index}:AllowedScopes:0"] = "api.read";
        _settings[$"{ClientSecretKeyPrefix}{clientId}"] = secret;
    }

    [Given("identity-service has confidential oauth client {string} with secret {string} allows scopes:")]
    public void GivenConfidentialOAuthClientWithScopes(string clientId, string secret, Table scopesTable)
    {
        _confidentialClientSecrets.Add(secret);
        var index = NextClientSeedIndex();
        _settings[$"Identity:Clients:Seed:{index}:ClientId"] = clientId;
        _settings[$"Identity:Clients:Seed:{index}:ClientType"] = "Confidential";
        _settings[$"Identity:Clients:Seed:{index}:RedirectUris:0"] = "https://localhost/callback";
        var scopeIndex = 0;
        foreach (var row in scopesTable.Rows)
        {
            _settings[$"Identity:Clients:Seed:{index}:AllowedScopes:{scopeIndex}"] = row["scope"];
            scopeIndex++;
        }

        _settings[$"{ClientSecretKeyPrefix}{clientId}"] = secret;
    }

    [When("I request a client credentials token for client {string} with secret {string} and scope {string}")]
    public async Task WhenRequestClientCredentialsToken(string clientId, string clientSecret, string scope) =>
        await SendClientCredentialsTokenAsync(clientId, clientSecret, includeScope: true, scope);

    [When("I request a client credentials token for client {string} with secret {string} without a scope")]
    public async Task WhenRequestClientCredentialsTokenWithoutScope(string clientId, string clientSecret) =>
        await SendClientCredentialsTokenAsync(clientId, clientSecret, includeScope: false, scope: string.Empty);

    private async Task SendClientCredentialsTokenAsync(
        string clientId,
        string clientSecret,
        bool includeScope,
        string scope)
    {
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        };
        if (includeScope)
        {
            fields["scope"] = scope;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ConnectTokenPath)
        {
            Content = new FormUrlEncodedContent(fields),
        };
        _response = await _client!.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [Then("the status code is not {int}")]
    public void ThenStatusCodeIsNot(int forbidden) => Assert.NotEqual(forbidden, (int)_response!.StatusCode);

    [Then("the token response includes a Bearer access token with expires_in")]
    public void ThenTokenResponseIncludesBearerAccessToken() => JwtBddAssertions.AssertBearerAccessTokenShape(_body);

    [Then("the token response has no access_token")]
    public void ThenTokenResponseHasNoAccessToken() => JwtBddAssertions.AssertNoAccessTokenProperty(_body);

    [Then("the oauth token response error is {string}")]
    public void ThenOAuthTokenError(string expectedError)
    {
        using var document = JsonDocument.Parse(_body);
        var error = document.RootElement.GetProperty("error").GetString();
        Assert.Equal(expectedError, error);
    }

    [Then("the access token is a machine token for client {string} with scope {string} and audience {string}")]
    public void ThenAccessTokenIsMachineToken(string clientId, string scope, string audience)
    {
        var token = JwtBddAssertions.ParseAccessTokenFromTokenBody(_body);
        JwtBddAssertions.AssertMachineClaims(token, clientId, scope, audience);
    }

    [Then("the access token is not a user access token")]
    public void ThenAccessTokenIsNotUserToken()
    {
        var token = JwtBddAssertions.ParseAccessTokenFromTokenBody(_body);
        JwtBddAssertions.AssertNotUserAccessToken(token);
    }

    [When("oauth redirect URI {string} is checked against registered URIs:")]
    public void WhenOAuthRedirectUriChecked(string requestedUri, Table table)
    {
        var registered = table.Rows.Select(row => row["uri"]).ToList();
        _redirectUriMatchResult = new RedirectUriMatcher().IsAllowed(requestedUri, registered);
    }

    [Then("oauth redirect URI match result is {word}")]
    public void ThenOAuthRedirectUriMatchResult(string expected)
    {
        var want = bool.Parse(expected);
        Assert.Equal(want, _redirectUriMatchResult);
    }

    [Then("the database contains registered oauth client {string} with type {string}")]
    public async Task ThenDatabaseContainsOAuthClient(string clientId, string clientType)
    {
        Assert.True(await Database.OAuthClientExistsAsync(clientId), $"Expected oauth client '{clientId}' in database.");
        var storedType = await Database.GetOAuthClientTypeAsync(clientId);
        Assert.Equal(clientType, storedType, StringComparer.OrdinalIgnoreCase);
    }

    [Then("the database does not contain registered oauth client {string}")]
    public async Task ThenDatabaseDoesNotContainOAuthClient(string clientId)
    {
        Assert.False(await Database.OAuthClientExistsAsync(clientId));
    }

    [Then("the database stores a hash for oauth client {string} and not the plaintext secret {string}")]
    public async Task ThenDatabaseStoresHashNotPlaintext(string clientId, string plaintext)
    {
        var hash = await Database.GetOAuthClientSecretHashAsync(clientId);
        Assert.False(string.IsNullOrWhiteSpace(hash), "Expected secret_hash to be set for confidential client.");
        Assert.NotEqual(plaintext, hash);
        Assert.DoesNotContain(plaintext, hash, StringComparison.Ordinal);
    }

    [Then("captured logs contain no oauth client secret")]
    public void ThenCapturedLogsContainNoClientSecret()
    {
        Assert.True(_logs is not null, "Log capture was not enabled.");
        var joined = string.Join('\n', _logs.Entries.Select(entry => entry.Message));
        foreach (var secret in _confidentialClientSecrets)
        {
            Assert.DoesNotContain(secret, joined, StringComparison.Ordinal);
        }
    }

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

    [Given("identity-service refresh token issuance is configured for BDD tests")]
    public void GivenRefreshTokenIssuanceForBdd()
    {
        _settings[RefreshLifetimeKey] = BddRefreshLifetimeSeconds.ToString(CultureInfo.InvariantCulture);
        _settings[RefreshDefaultClientKey] = "teamboard";
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
    public void GivenWithoutSigningKeys()
    {
        _omitSigningKeys = true;
        _settings.Remove(SigningKeysFileKey);
    }

    [Given("identity-service rotation uses an isolated signing keys file")]
    public void GivenRotationIsolatedKeysFile() => _rotationKeysPath = RotationTestKeysFile.CreateIsolatedCopy();

    [Given("identity-service signing keys file points to the rotation test file")]
    public void GivenSigningKeysRotationTestFile()
    {
        var path = _rotationKeysPath ?? throw new InvalidOperationException("Rotation keys file was not created.");
        _settings[SigningKeysFileKey] = path;
    }

    [Given("rotation fake UTC time is {string}")]
    public void GivenRotationFakeTime(string utcTimestamp) =>
        _rotationTime = new FakeRotationTimeProvider(DateTimeOffset.Parse(utcTimestamp, CultureInfo.InvariantCulture));

    [When("I advance rotation fake time by {int} minutes")]
    public void WhenAdvanceRotationFakeTimeMinutes(int minutes)
    {
        var time = _rotationTime ?? throw new InvalidOperationException("Rotation fake time was not set.");
        time.Advance(TimeSpan.FromMinutes(minutes));
    }

    [When("I advance rotation fake time by {int} hours")]
    public void WhenAdvanceRotationFakeTimeHours(int hours)
    {
        var time = _rotationTime ?? throw new InvalidOperationException("Rotation fake time was not set.");
        time.Advance(TimeSpan.FromHours(hours));
    }

    [When("I run signing key rotate start")]
    public void WhenRotateStart() => RunRotationCommand(() =>
        SigningKeyRotationCommands.Start(RequireRotationKeysPath(), RequireRotationTime()));

    [When("I run signing key rotate activate")]
    public void WhenRotateActivate() => RunRotationCommand(() =>
        SigningKeyRotationCommands.Activate(RequireRotationKeysPath(), RequireRotationTime()));

    [When("I run signing key rotate retire")]
    public void WhenRotateRetire() => RunRotationCommand(() =>
        SigningKeyRotationCommands.Retire(RequireRotationKeysPath(), RequireRotationTime()));

    [When("I store the current login access token for rotation checks")]
    public void WhenStoreAccessToken() =>
        _storedAccessToken = JwtBddAssertions.GetAccessTokenStringFromLoginBody(_body);

    [Then("the access token header kid is {string}")]
    public void ThenAccessTokenKid(string expectedKid)
    {
        var token = JwtBddAssertions.ParseAccessTokenFromLoginBody(_body);
        JwtBddAssertions.AssertAccessTokenKid(token, expectedKid);
    }

    [Then("the stored access token header kid is {string}")]
    public void ThenStoredAccessTokenKid(string expectedKid)
    {
        var tokenString = _storedAccessToken ?? throw new InvalidOperationException("No stored access token.");
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(tokenString);
        JwtBddAssertions.AssertAccessTokenKid(token, expectedKid);
    }

    [Then("the JWKS document does not include public key with kid {string}")]
    public void ThenJwksMissingKid(string kid)
    {
        using var document = JsonDocument.Parse(_body);
        var keys = document.RootElement.GetProperty("keys");
        var found = keys.EnumerateArray().Any(entry =>
            string.Equals(entry.GetProperty("kid").GetString(), kid, StringComparison.Ordinal));
        Assert.False(found, $"Did not expect kid {kid} in JWKS.");
    }

    [Then("the JWKS document includes exactly {int} public keys")]
    public void ThenJwksKeyCount(int expectedCount)
    {
        using var document = JsonDocument.Parse(_body);
        var keys = document.RootElement.GetProperty("keys");
        Assert.Equal(expectedCount, keys.GetArrayLength());
    }

    [Then("the rotation command exit code is not {int}")]
    public void ThenRotationExitCodeIsNot(int forbiddenExitCode)
    {
        var exitCode = _lastRotationExitCode ?? throw new InvalidOperationException("No rotation command was run.");
        Assert.NotEqual(forbiddenExitCode, exitCode);
    }

    [Given("identity-service metrics are being observed for issued access tokens")]
    public void GivenAccessTokenMetrics()
    {
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = true;
        StartMeterListener();
    }

    [Given("identity-service metrics are being observed for refresh token reuse")]
    public void GivenRefreshReuseMetrics()
    {
        _observeLoginFailuresOnly = false;
        _observeLockoutsOnly = false;
        _observeAccessTokensOnly = false;
        _observeJwksServedOnly = false;
        _observeRefreshReuseOnly = true;
        StartMeterListener();
        _refreshReuseBaseline = Volatile.Read(ref _refreshReuseMeasurements);
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

    [Then("the login response includes an opaque refresh token with refresh_expires_in")]
    public void ThenLoginResponseIncludesRefresh() => RefreshBddAssertions.AssertLoginRefreshShape(_body);

    [Then("the login refresh_expires_in is about {int} seconds")]
    public void ThenLoginRefreshExpiresAbout(int expectedSeconds)
    {
        using var document = JsonDocument.Parse(_body);
        var actual = document.RootElement.GetProperty("refresh_expires_in").GetInt32();
        Assert.InRange(actual, expectedSeconds - 5, expectedSeconds + 5);
    }

    [When("I store the current login refresh token for rotation checks")]
    public void WhenStoreLoginRefreshToken()
    {
        using var document = JsonDocument.Parse(_body);
        _storedRefreshToken = document.RootElement.GetProperty("refresh_token").GetString()
            ?? throw new InvalidOperationException("refresh_token is missing from login response.");
        _storedLoginAccessToken = document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token is missing from login response.");
    }

    [When("I store the latest refresh token from the last response")]
    public void WhenStoreLatestRefreshFromLastResponse()
    {
        using var document = JsonDocument.Parse(_body);
        _latestRotatedRefreshToken = document.RootElement.GetProperty("refresh_token").GetString()
            ?? throw new InvalidOperationException("refresh_token is missing from the last response.");
    }

    [When("I send a refresh request with the latest rotated refresh token")]
    public Task WhenRefreshWithLatestRotated() =>
        SendRefreshAsync(_latestRotatedRefreshToken ?? throw new InvalidOperationException("No latest rotated refresh token."));

    [When("I send a refresh request with the stored refresh token")]
    public Task WhenRefreshWithStoredToken() =>
        SendRefreshAsync(_storedRefreshToken ?? throw new InvalidOperationException("No stored refresh token."));

    [When("I send a refresh request with refresh token {string}")]
    public Task WhenRefreshWithToken(string refreshToken) => SendRefreshAsync(refreshToken);

    [When("I send a logout request with the stored refresh token")]
    public Task WhenLogoutWithStoredToken() =>
        SendLogoutAsync(_storedRefreshToken ?? throw new InvalidOperationException("No stored refresh token."));

    [When("I send a logout request with refresh token {string}")]
    public Task WhenLogoutWithToken(string refreshToken) => SendLogoutAsync(refreshToken);

    [When("I store the current login refresh token as the first session refresh token")]
    public void WhenStoreFirstSessionRefreshToken()
    {
        using var document = JsonDocument.Parse(_body);
        _firstSessionRefreshToken = document.RootElement.GetProperty("refresh_token").GetString()
            ?? throw new InvalidOperationException("refresh_token is missing from login response.");
    }

    [When("I store the current login refresh token as the second session refresh token")]
    public void WhenStoreSecondSessionRefreshToken()
    {
        using var document = JsonDocument.Parse(_body);
        _secondSessionRefreshToken = document.RootElement.GetProperty("refresh_token").GetString()
            ?? throw new InvalidOperationException("refresh_token is missing from login response.");
    }

    [When("I store the current login access token for change password requests")]
    public void WhenStoreChangePasswordAccessToken()
    {
        using var document = JsonDocument.Parse(_body);
        _changePasswordAccessToken = document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token is missing from login response.");
    }

    [When("I send a change password request with current {string} and new {string} using the stored change password access token")]
    public Task WhenChangePasswordWithStoredToken(string currentPassword, string newPassword) =>
        SendChangePasswordAsync(currentPassword, newPassword, _changePasswordAccessToken);

    [When("I send a change password request with current {string} and new {string} without an access token")]
    public Task WhenChangePasswordWithoutToken(string currentPassword, string newPassword) =>
        SendChangePasswordAsync(currentPassword, newPassword, bearerToken: null);

    [When("I send a change password request with current {string} and new {string} using access token {string}")]
    public Task WhenChangePasswordWithExplicitToken(string currentPassword, string newPassword, string accessToken) =>
        SendChangePasswordAsync(currentPassword, newPassword, accessToken);

    [When("I send {int} change password requests with current {string} and new {string} using the stored change password access token")]
    public async Task WhenManyChangePasswordRequests(int count, string currentPassword, string newPassword)
    {
        for (var i = 0; i < count; i++)
        {
            await SendChangePasswordAsync(currentPassword, newPassword, _changePasswordAccessToken);
        }
    }

    [When("I send a refresh request with the first session refresh token")]
    public Task WhenRefreshFirstSession() =>
        SendRefreshAsync(_firstSessionRefreshToken ?? throw new InvalidOperationException("No first session refresh token."));

    [When("I send a refresh request with the second session refresh token")]
    public Task WhenRefreshSecondSession() =>
        SendRefreshAsync(_secondSessionRefreshToken ?? throw new InvalidOperationException("No second session refresh token."));

    [When("two refresh requests run concurrently with the stored refresh token")]
    public async Task WhenConcurrentRefreshWithStoredToken()
    {
        var refreshToken = _storedRefreshToken ?? throw new InvalidOperationException("No stored refresh token.");
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        var tasks = Enumerable.Range(0, 2)
            .Select(_ => client.PostAsJsonAsync(RefreshPath, new { refresh_token = refreshToken }))
            .ToArray();
        var responses = await Task.WhenAll(tasks);
        try
        {
            _concurrentRefreshSuccess = responses.Count(response => (int)response.StatusCode == 200);
            _concurrentRefreshFailure = responses.Count(response => (int)response.StatusCode == 401);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Then("the refresh response has no id property")]
    public void ThenRefreshResponseHasNoId() => RefreshBddAssertions.AssertRefreshResponseHasNoId(_body);

    [Then("the refresh response includes a new access and refresh token pair")]
    public void ThenRefreshResponseIncludesPair() => RefreshBddAssertions.AssertRefreshResponsePair(_body);

    [Then("the refreshed refresh token differs from the stored refresh token")]
    public void ThenRefreshedRefreshDiffersFromStored()
    {
        using var document = JsonDocument.Parse(_body);
        var refreshed = document.RootElement.GetProperty("refresh_token").GetString();
        var stored = _storedRefreshToken ?? throw new InvalidOperationException("No stored refresh token.");
        Assert.False(string.Equals(refreshed, stored, StringComparison.Ordinal));
    }

    [Then("the refreshed access token differs from the login access token")]
    public void ThenRefreshedAccessDiffersFromLogin()
    {
        using var document = JsonDocument.Parse(_body);
        var refreshed = document.RootElement.GetProperty("access_token").GetString();
        var login = _storedLoginAccessToken ?? throw new InvalidOperationException("No stored login access token.");
        Assert.False(string.Equals(refreshed, login, StringComparison.Ordinal));
    }

    [Then("exactly one refresh response has status {int}")]
    public void ThenExactlyOneRefreshStatus(int statusCode)
    {
        var count = statusCode == 200 ? _concurrentRefreshSuccess : _concurrentRefreshFailure;
        Assert.Equal(1, count);
    }

    [Then("the refresh token reuse total metric increased by {int}")]
    public void ThenRefreshReuseMetricIncreasedBy(int delta)
    {
        var total = Volatile.Read(ref _refreshReuseMeasurements);
        Assert.Equal(_refreshReuseBaseline + delta, total);
    }

    [Then("no login error field contains a three-part JWT")]
    public void ThenLoginErrorFieldsNotJwt() => JwtBddAssertions.AssertNoJwtLikeStringValues(_body);

    [Then("the error title is {string}")]
    public void ThenErrorTitle(string expectedTitle)
    {
        using var document = JsonDocument.Parse(_body);
        Assert.Equal(expectedTitle, document.RootElement.GetProperty("title").GetString());
    }

    [Then("the JWKS public key uses RSA for signature verification")]
    public void ThenJwksRsaForSignature()
    {
        using var document = JsonDocument.Parse(_body);
        var key = document.RootElement.GetProperty("keys")[0];
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("sig", key.GetProperty("use").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
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

    [Then("the response body does not contain {string}")]
    public void ThenBodyExcludes(string value) =>
        Assert.DoesNotContain(value, _body, StringComparison.Ordinal);

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

    private string RequireRotationKeysPath() =>
        _rotationKeysPath ?? throw new InvalidOperationException("Rotation keys file was not created.");

    private TimeProvider RequireRotationTime() => _rotationTime ?? TimeProvider.System;

    private void RunRotationCommand(Func<int> command) => _lastRotationExitCode = command();

    private async Task SendRefreshAsync(string refreshToken)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        _response = await client.PostAsJsonAsync(RefreshPath, new { refresh_token = refreshToken });
        _body = await _response.Content.ReadAsStringAsync();
    }

    private async Task SendLogoutAsync(string refreshToken)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        _response = await client.PostAsJsonAsync(LogoutPath, new { refresh_token = refreshToken });
        _body = await _response.Content.ReadAsStringAsync();
    }

    private async Task SendChangePasswordAsync(string currentPassword, string newPassword, string? bearerToken, string? clientIp = null)
    {
        var client = _client ?? throw new InvalidOperationException("Service not started.");
        _response?.Dispose();
        using var request = new HttpRequestMessage(HttpMethod.Post, ChangePasswordPath)
        {
            Content = JsonContent.Create(new { current_password = currentPassword, new_password = newPassword }),
        };
        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (clientIp is not null)
        {
            request.Headers.TryAddWithoutValidation(TestClientIpStartupFilter.HeaderName, clientIp);
        }

        _response = await client.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
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

                if (_observeRefreshReuseOnly)
                {
                    if (instrument.Name == RefreshReuseInstrument)
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

            if (instrument.Name == RefreshReuseInstrument)
            {
                Interlocked.Add(ref _refreshReuseMeasurements, measurement);
            }
        });
        _meterListener.Start();
    }

    private int NextClientSeedIndex()
    {
        var max = -1;
        foreach (var key in _settings.Keys)
        {
            const string prefix = "Identity:Clients:Seed:";
            const string suffix = ":ClientId";
            if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var middle = key[prefix.Length..^suffix.Length];
            if (int.TryParse(middle, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
            {
                max = Math.Max(max, idx);
            }
        }

        return max + 1;
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

                var signingKeysPath = _settings.GetValueOrDefault(SigningKeysFileKey, TestSigningKeyPaths.JsonFile);
                var jwtIssuer = _settings.GetValueOrDefault(JwtIssuerKey, BddJwtIssuer);
                var jwtAudience = _settings.GetValueOrDefault(JwtAudienceKey, BddJwtAudience);

                builder.UseSetting(JwtIssuerKey, jwtIssuer);
                builder.UseSetting(JwtAudienceKey, jwtAudience);
                builder.UseSetting(
                    JwtLifetimeKey,
                    _settings.GetValueOrDefault(JwtLifetimeKey, BddJwtLifetimeSeconds.ToString(CultureInfo.InvariantCulture)));
                builder.UseSetting(AuthJwksFileKey, signingKeysPath);
                builder.UseSetting(AuthJwtIssuerKey, jwtIssuer);
                builder.UseSetting(AuthJwtAudienceKey, jwtAudience);
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
        if (_preserveHttpCookies)
        {
            return factory.CreateDefaultClient(
                new CookieJarHandler(new CookieContainer(), factory.Server.CreateHandler()));
        }

        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task DisposeFactoriesAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }

        _factories.Clear();
    }

    private sealed class CookieJarHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies;

        public CookieJarHandler(CookieContainer cookies, HttpMessageHandler inner)
            : base(inner) =>
            _cookies = cookies;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { } uri)
            {
                var header = _cookies.GetCookieHeader(uri);
                if (!string.IsNullOrEmpty(header))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", header);
                }
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (request.RequestUri is { } responseUri && response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
            {
                foreach (var value in setCookieValues)
                {
                    _cookies.SetCookies(responseUri, value);
                }
            }

            return response;
        }
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
