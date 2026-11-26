using IdentityService.Security.Clients;
using IdentityService.Security.Token;

namespace IdentityService.Security.Authorize;

public sealed class AuthorizeRequestValidator(IClientRegistry clientRegistry)
{
    public AuthorizeValidationResult Validate(AuthorizeQuery query)
    {
        if (!string.Equals(query.ResponseType, "code", StringComparison.Ordinal))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (string.IsNullOrWhiteSpace(query.ClientId))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (string.IsNullOrWhiteSpace(query.State))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (string.IsNullOrWhiteSpace(query.CodeChallenge))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (!PkceValidator.IsChallengeMethodAllowed(query.CodeChallengeMethod))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (!PkceValidator.IsValidS256ChallengeFormat(query.CodeChallenge))
        {
            return AuthorizeValidationResult.Invalid();
        }

        var requestedScopes = ScopeValidator.ParseScopeParameter(query.Scope);
        if (requestedScopes.Length == 0)
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (!clientRegistry.TryGet(query.ClientId, out var client))
        {
            return AuthorizeValidationResult.Invalid();
        }

        var redirectUri = query.RedirectUri ?? string.Empty;
        if (!clientRegistry.ValidateRedirectUri(query.ClientId, redirectUri))
        {
            return AuthorizeValidationResult.Invalid();
        }

        if (!ScopeValidator.IsRequestedScopeAllowed(requestedScopes, client.AllowedScopes))
        {
            return AuthorizeValidationResult.Invalid();
        }

        return AuthorizeValidationResult.Valid();
    }
}

public readonly record struct AuthorizeValidationResult(bool IsValid)
{
    public static AuthorizeValidationResult Valid() => new(true);

    public static AuthorizeValidationResult Invalid() => new(false);
}