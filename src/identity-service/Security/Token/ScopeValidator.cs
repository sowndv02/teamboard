namespace IdentityService.Security.Token;

public static class ScopeValidator
{
    public static bool IsRequestedScopeAllowed(
        IReadOnlyList<string> requestedScopes,
        IReadOnlyList<string> allowedScopes)
    {
        throw new NotImplementedException("TB-041: scope validation not implemented.");
    }
}