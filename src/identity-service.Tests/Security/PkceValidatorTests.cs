using IdentityService.Security.Authorize;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class PkceValidatorTests
{
    [Fact]
    public void IsChallengeMethodAllowed_WhenS256_ReturnsTrue()
    {
        Assert.True(PkceValidator.IsChallengeMethodAllowed("S256"));
    }

    [Fact]
    public void IsChallengeMethodAllowed_WhenPlain_ReturnsFalse()
    {
        Assert.False(PkceValidator.IsChallengeMethodAllowed("plain"));
    }

    [Fact]
    public void IsValidS256ChallengeFormat_WhenRfc7636Example_ReturnsTrue()
    {
        const string challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
        Assert.True(PkceValidator.IsValidS256ChallengeFormat(challenge));
    }

    [Fact]
    public void VerifyS256_WhenRfc7636Example_ReturnsTrue()
    {
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
        Assert.True(PkceValidator.VerifyS256(verifier, challenge));
    }
}