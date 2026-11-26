using System.Text.RegularExpressions;
using IdentityService.Security.Authorize;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class AuthorizationCodeGeneratorTests
{
    private static readonly Regex Base64UrlPattern = new(@"^[A-Za-z0-9\-._~]+$", RegexOptions.CultureInvariant);

    [Fact]
    public void GeneratePlaintextCode_WhenCalled_ReturnsBase64UrlWithExpectedLength()
    {
        var code = AuthorizationCodeGenerator.GeneratePlaintextCode();
        Assert.InRange(code.Length, 43, 43);
        Assert.Matches(Base64UrlPattern, code);
    }
}