namespace IdentityService.Security.Signing;

public interface ISigningKeyStore
{
    bool HasActiveKey { get; }

    ActiveSigningKey GetActiveKey();
}