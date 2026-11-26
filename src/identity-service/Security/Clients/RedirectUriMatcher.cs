namespace IdentityService.Security.Clients;

public sealed class RedirectUriMatcher
{
    public bool IsAllowed(string requestedUri, IReadOnlyList<string> registeredUris)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestedUri);
        foreach (var registered in registeredUris)
        {
            if (string.Equals(registered, requestedUri, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}