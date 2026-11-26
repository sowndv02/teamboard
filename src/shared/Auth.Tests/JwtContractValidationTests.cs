using Teamboard.Shared.Auth;
using Teamboard.Shared.Auth.Tests.Support;
using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwtContractValidationTests
{
    private readonly TeamboardJwtTokenValidator _validator = TeamboardJwtTokenValidator.CreateForTesting(
        Path.Combine(AppContext.BaseDirectory, "Support", "test-signing-keys.json"));

    [Fact]
    public async Task ValidateAccessTokenAsync_ValidRs256Token_ReturnsValid()
    {
        var token = TestAccessTokenFactory.IssueForUser(Guid.Parse("a1111111-1111-1111-1111-111111111111"));

        var result = await _validator.ValidateAccessTokenAsync(token);

        Assert.True(result.IsValid, result.FailureReason);
    }
}