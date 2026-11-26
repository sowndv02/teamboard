namespace IdentityService.Security.Jwt;

public sealed record IssuedAccessToken(string Token, int ExpiresInSeconds);