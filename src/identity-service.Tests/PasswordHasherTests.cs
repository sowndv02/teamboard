using System.Diagnostics.Metrics;
using IdentityService.Security;
using IdentityService.Security.Passwords;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityService.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashPassword_TwoCallsWithSamePassword_ProduceDifferentHashesAndSalts()
    {
        var hasher = CreateHasher();
        var first = hasher.HashPassword("SharedPass123456");
        var second = hasher.HashPassword("SharedPass123456");

        Assert.NotEqual(first.Hash, second.Hash);
        Assert.NotEqual(first.Salt, second.Salt);
        Assert.Equal(first.Iterations, second.Iterations);
    }

    private static PasswordHasher CreateHasher()
    {
        var options = Options.Create(new PasswordOptions { Iterations = 10_000 });
        var metrics = new IdentityMetrics(new TestMeterFactory());
        return new PasswordHasher(options, metrics);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options.Name, options.Version);

        public Meter Create(string name, string? version = null) => new(name, version);

        public void Dispose()
        {
        }
    }
}