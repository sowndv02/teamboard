using IdentityService.Security;
using IdentityService.Security.Clients;
using IdentityService.Security.Jwt;

namespace IdentityService.Security.Token;

public sealed class ClientCredentialsTokenService(
    IClientRegistry clientRegistry,
    IAccessTokenIssuer accessTokenIssuer,
    IdentityMetrics metrics)
{
    public ClientCredentialsExchangeResult TryExchange(string clientId, string clientSecret, string? scopeRaw)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return ClientCredentialsExchangeResult.InvalidClient();
        }

        var requestedScopes = ScopeValidator.ParseScopeParameter(scopeRaw);
        if (requestedScopes.Length == 0)
        {
            return ClientCredentialsExchangeResult.InvalidScope();
        }

        if (!clientRegistry.TryGet(clientId, out var client))
        {
            return ClientCredentialsExchangeResult.InvalidClient();
        }

        if (client.ClientType == ClientType.Public)
        {
            return ClientCredentialsExchangeResult.UnauthorizedClient();
        }

        if (!clientRegistry.ValidateClientSecret(clientId, clientSecret))
        {
            return ClientCredentialsExchangeResult.InvalidClient();
        }

        if (!ScopeValidator.IsRequestedScopeAllowed(requestedScopes, client.AllowedScopes))
        {
            return ClientCredentialsExchangeResult.InvalidScope();
        }

        var issued = accessTokenIssuer.IssueForClient(clientId, requestedScopes);
        metrics.RecordMachineAccessTokenIssued();
        return ClientCredentialsExchangeResult.Success(issued);
    }
}

public readonly record struct ClientCredentialsExchangeResult(
    bool IsSuccess,
    IssuedAccessToken? Token,
    string? Error,
    string? ErrorDescription)
{
    public static ClientCredentialsExchangeResult Success(IssuedAccessToken token) =>
        new(true, token, null, null);

    public static ClientCredentialsExchangeResult InvalidClient() =>
        new(false, null, "invalid_client", "Client authentication failed.");

    public static ClientCredentialsExchangeResult UnauthorizedClient() =>
        new(false, null, "unauthorized_client", "The client is not authorized to use this grant type.");

    public static ClientCredentialsExchangeResult InvalidScope() =>
        new(false, null, "invalid_scope", "The requested scope is invalid or missing.");
}