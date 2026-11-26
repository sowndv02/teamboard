namespace IdentityService.Security.Jwt;

public interface IAccessTokenIssuer
{
    IssuedAccessToken IssueForUser(Guid userId);

    IssuedAccessToken IssueForClient(string clientId, IReadOnlyList<string> scopes);
}