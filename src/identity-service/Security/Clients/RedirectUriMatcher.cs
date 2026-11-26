namespace IdentityService.Security.Clients;

public sealed class RedirectUriMatcher
{
    public bool IsAllowed(string requestedUri, IReadOnlyList<string> registeredUris) =>
        throw new NotImplementedException("TB-040 redirect URI matcher");
}