using IdentityService.Security.Clients;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizeRequestValidator(IClientRegistry clientRegistry)
{
    public AuthorizeValidationResult Validate(AuthorizeRequest request)
    {
        _ = clientRegistry;
        _ = request;
        throw new NotImplementedException("TB-042 authorize validation is not implemented.");
    }
}

public sealed record AuthorizeRequest(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string State,
    string Scope,
    string CodeChallenge,
    string CodeChallengeMethod);

public readonly record struct AuthorizeValidationResult(bool IsValid, string? ErrorCode);