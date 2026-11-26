using IdentityService.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IdentityService.Tests.Health;

public sealed class SigningKeyReadyTests : IAsyncDisposable
{
    private WebApplicationFactory<Program>? _factory;

    [Fact]
    public async Task Ready_Returns503_WhenSigningKeysAreMissing()
    {
        await using var database = await PostgresDatabase.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:IdentityDb", database.ConnectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("Identity:RateLimiting:Enabled", "false");
            builder.UseSetting("Identity:SigningKeys:JsonFile", string.Empty);
        });

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        Assert.Equal(503, (int)response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}