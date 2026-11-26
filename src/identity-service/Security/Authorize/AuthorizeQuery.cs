namespace IdentityService.Security.Authorize;

public sealed record AuthorizeQuery(
    string? ClientId,
    string? RedirectUri,
    string? ResponseType,
    string? State,
    string? Scope,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Nonce);

public static class AuthorizeQueryParser
{
    public static AuthorizeQuery Parse(IQueryCollection query) =>
        new(
            query["client_id"].ToString(),
            query["redirect_uri"].ToString(),
            query["response_type"].ToString(),
            query["state"].ToString(),
            query["scope"].ToString(),
            query["code_challenge"].ToString(),
            query["code_challenge_method"].ToString(),
            query["nonce"].ToString());
}