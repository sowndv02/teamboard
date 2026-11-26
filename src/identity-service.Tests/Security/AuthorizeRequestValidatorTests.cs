using IdentityService.Security.Authorize;
using IdentityService.Security.Clients;
using Xunit;

namespace IdentityService.Tests.Security;

public sealed class AuthorizeRequestValidatorTests
{
  [Fact]
  public void Validate_WhenRedirectUriNotRegistered_ReturnsInvalid()
  {
    var registry = new FakeClientRegistry();
    var validator = new AuthorizeRequestValidator(registry);
    var request = new AuthorizeRequest(
        "teamboard",
        "http://evil.example/callback",
        "code",
        "state",
        "openid",
        "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
        "S256");
    var result = validator.Validate(request);
    Assert.False(result.IsValid);
  }

  private sealed class FakeClientRegistry : IClientRegistry
  {
    public bool TryGet(string clientId, out RegisteredClientSummary client)
    {
      if (clientId != "teamboard")
      {
        client = null!;
        return false;
      }

      client = new RegisteredClientSummary(
          "teamboard",
          ClientType.Public,
          ["http://localhost:4200/callback"],
          ["openid"]);
      return true;
    }

    public bool ValidateRedirectUri(string clientId, string redirectUri)
    {
      return TryGet(clientId, out var summary)
          && summary.RedirectUris.Contains(redirectUri, StringComparer.Ordinal);
    }

    public bool ValidateClientSecret(string clientId, string plaintextSecret) => false;
  }
}