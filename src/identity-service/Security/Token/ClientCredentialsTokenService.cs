namespace IdentityService.Security.Token;

public sealed class ClientCredentialsTokenService
{
    public Task<ClientCredentialsTokenResult> ExchangeAsync(
        string clientId,
        string clientSecret,
        string scope,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TB-041: client credentials exchange not implemented.");
    }
}

public readonly record struct ClientCredentialsTokenResult(string AccessToken, int ExpiresInSeconds);