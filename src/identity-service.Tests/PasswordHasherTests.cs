using IdentityService.Security.Passwords;
using Xunit;

namespace IdentityService.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashPassword_TwoCallsWithSamePassword_ProduceDifferentHashesAndSalts()
    {
        var hasher = new PasswordHasher();
        var first = hasher.HashPassword("SharedPass123456");
        var second = hasher.HashPassword("SharedPass123456");

        Assert.NotEqual(first.Hash, second.Hash);
        Assert.NotEqual(first.Salt, second.Salt);
        Assert.Equal(first.Iterations, second.Iterations);
    }
}