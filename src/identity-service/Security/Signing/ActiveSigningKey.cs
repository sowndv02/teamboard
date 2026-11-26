using System.Security.Cryptography;

namespace IdentityService.Security.Signing;

public sealed class ActiveSigningKey : IDisposable
{
    public ActiveSigningKey(string kid, RSA rsa)
    {
        Kid = kid;
        Rsa = rsa;
    }

    public string Kid { get; }

    public RSA Rsa { get; }

    public void Dispose() => Rsa.Dispose();
}