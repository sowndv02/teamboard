using System.Net;
using System.Text.Json;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwtMultiKidJwksIntegrationTests : IAsyncDisposable
{
    private WebApplicationFactory<Program>? _identityFactory;

    [Fact]
    public async Task Jwks_IncludesRetiringAndActivePublicKeys_WhenBothArePublished()
    {
        var keysFile = MultiKeySigningKeysFixture.CreateRetiringAndActiveOnDisk();

        _identityFactory = new IdentityWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Identity:RateLimiting:Enabled", "false");
            builder.UseSetting(
                "ConnectionStrings:IdentityDb",
                "Host=127.0.0.1;Port=1;Database=identity;Username=identity;Password=unused;Timeout=1");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Identity:SigningKeys:JsonFile", keysFile);
            builder.UseSetting("Identity:Jwt:Issuer", "http://localhost:6061");
            builder.UseSetting("Identity:Jwt:Audience", "teamboard");
        });

        var client = _identityFactory.CreateClient();
        using var response = await client.GetAsync(new Uri(client.BaseAddress!, ".well-known/jwks.json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var keys = document.RootElement.GetProperty("keys");
        var kids = keys.EnumerateArray()
            .Select(entry => entry.GetProperty("kid").GetString())
            .Where(kid => kid is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("test-active-1", kids);
        Assert.Contains("test-rotate-active-2", kids);
        Assert.True(keys.GetArrayLength() >= 2, "JWKS should publish multiple keys during rotation.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_identityFactory is not null)
        {
            await _identityFactory.DisposeAsync();
        }
    }
}