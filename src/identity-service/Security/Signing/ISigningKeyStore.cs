namespace IdentityService.Security.Signing;

public interface ISigningKeyStore
{
    bool HasActiveKey { get; }

    ActiveSigningKey GetActiveKey();

    IReadOnlyList<ActiveSigningKey> GetPublishedKeysForJwks();
}