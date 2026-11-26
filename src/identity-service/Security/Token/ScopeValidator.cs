namespace IdentityService.Security.Token;

public static class ScopeValidator
{
    public static bool IsRequestedScopeAllowed(
        IReadOnlyList<string> requestedScopes,
        IReadOnlyList<string> allowedScopes)
    {
        if (requestedScopes.Count == 0 || allowedScopes.Count == 0)
        {
            return false;
        }

        var allowed = new HashSet<string>(allowedScopes, StringComparer.Ordinal);
        foreach (var scope in requestedScopes)
        {
            if (string.IsNullOrWhiteSpace(scope) || !allowed.Contains(scope))
            {
                return false;
            }
        }

        return true;
    }

    public static string[] ParseScopeParameter(string? scopeRaw) =>
        string.IsNullOrWhiteSpace(scopeRaw)
            ? []
            : scopeRaw.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}