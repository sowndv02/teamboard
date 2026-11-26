namespace IdentityService.Security.Jwt;

public interface IAccessTokenIssuer
{
    IssuedAccessToken IssueForUser(Guid userId);
}