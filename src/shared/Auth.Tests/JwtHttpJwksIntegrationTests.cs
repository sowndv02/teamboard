using System.Net;
using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Teamboard.Shared.Auth.Tests.Support;
using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwtHttpJwksIntegrationTests : IAsyncDisposable
{
    private WebApplicationFactory<Program>? _identityFactory;

    [Fact]
    public async Task ValidateAccessTokenAsync_UsesHttpJwksFromIdentity_ReturnsValid()
    {
        _identityFactory = new IdentityWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Identity:RateLimiting:Enabled", "false");
            builder.UseSetting(
                "ConnectionStrings:IdentityDb",
                "Host=127.0.0.1;Port=1;Database=identity;Username=identity;Password=unused;Timeout=1");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Identity:SigningKeys:JsonFile", TestSigningKeyPaths.JsonFile);
            builder.UseSetting("Identity:Jwt:Issuer", "http://localhost:6061");
            builder.UseSetting("Identity:Jwt:Audience", "teamboard");
        });

        var identityClient = _identityFactory.CreateClient();
        var jwksUri = new Uri(identityClient.BaseAddress!, ".well-known/jwks.json");

        using var jwksProbe = await identityClient.GetAsync(jwksUri);
        Assert.Equal(HttpStatusCode.OK, jwksProbe.StatusCode);

        var validator = HttpJwksTestValidatorFactory.Create(_identityFactory, jwksUri);
        var token = TestAccessTokenFactory.IssueForUser(Guid.Parse("a1111111-1111-1111-1111-111111111111"));

        var result = await validator.ValidateAccessTokenAsync(token);

        Assert.True(result.IsValid, result.FailureReason);
    }

    public async ValueTask DisposeAsync()
    {
        if (_identityFactory is not null)
        {
            await _identityFactory.DisposeAsync();
        }
    }
}