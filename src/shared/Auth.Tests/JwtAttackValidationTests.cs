using Teamboard.Shared.Auth;
using Teamboard.Shared.Auth.Tests.Support;
using Xunit;

namespace Teamboard.Shared.Auth.Tests;

public sealed class JwtAttackValidationTests
{
  private readonly TeamboardJwtTokenValidator _validator = new();

  [Fact]
  public async Task ValidateAccessTokenAsync_TamperedPayload_Rejects()
  {
    var userId = Guid.Parse("d4444444-4444-4444-4444-444444444444");
    var valid = await _validator.ValidateAccessTokenAsync(TestAccessTokenFactory.IssueForUser(userId));
    Assert.True(valid.IsValid, valid.FailureReason);

    var tampered = TestAccessTokenFactory.IssueTamperedPayloadToken(userId);
    var result = await _validator.ValidateAccessTokenAsync(tampered);

    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task ValidateAccessTokenAsync_WrongAudience_Rejects()
  {
    var userId = Guid.Parse("e5555555-5555-5555-5555-555555555555");
    var valid = await _validator.ValidateAccessTokenAsync(TestAccessTokenFactory.IssueForUser(userId));
    Assert.True(valid.IsValid, valid.FailureReason);

    var wrongAud = TestAccessTokenFactory.IssueForUser(userId, audience: "wrong-aud");
    var result = await _validator.ValidateAccessTokenAsync(wrongAud);

    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task ValidateAccessTokenAsync_ExpiredToken_Rejects()
  {
    var userId = Guid.Parse("f6666666-6666-6666-6666-666666666666");
    var valid = await _validator.ValidateAccessTokenAsync(TestAccessTokenFactory.IssueForUser(userId));
    Assert.True(valid.IsValid, valid.FailureReason);

    var expired = TestAccessTokenFactory.IssueForUser(
      userId,
      expiresAt: DateTimeOffset.UtcNow.AddMinutes(-5));
    var result = await _validator.ValidateAccessTokenAsync(expired);

    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task ValidateAccessTokenAsync_WrongIssuer_Rejects()
  {
    var userId = Guid.Parse("a7777777-7777-7777-7777-777777777777");
    var valid = await _validator.ValidateAccessTokenAsync(TestAccessTokenFactory.IssueForUser(userId));
    Assert.True(valid.IsValid, valid.FailureReason);

    var wrongIssuer = TestAccessTokenFactory.IssueForUser(
      userId,
      issuer: "https://evil.example");
    var result = await _validator.ValidateAccessTokenAsync(wrongIssuer);

    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task ValidateAccessTokenAsync_AlgNone_Rejects()
  {
    var userId = Guid.Parse("b8888888-8888-8888-8888-888888888888");
    var valid = await _validator.ValidateAccessTokenAsync(TestAccessTokenFactory.IssueForUser(userId));
    Assert.True(valid.IsValid, valid.FailureReason);

    var algNone = TestAccessTokenFactory.IssueAlgNoneToken(userId);
    var result = await _validator.ValidateAccessTokenAsync(algNone);

    Assert.False(result.IsValid);
  }
}