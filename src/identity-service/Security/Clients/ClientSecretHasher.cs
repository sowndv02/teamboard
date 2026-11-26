namespace IdentityService.Security.Clients;

public sealed class ClientSecretHasher
{
    public ClientSecretHashResult Hash(string plaintextSecret) =>
        throw new NotImplementedException("TB-040 client secret hasher");

    public bool Verify(string plaintextSecret, ClientSecretHashResult stored) =>
        throw new NotImplementedException("TB-040 client secret hasher");
}

public sealed record ClientSecretHashResult(byte[] Hash, byte[] Salt, int Iterations);