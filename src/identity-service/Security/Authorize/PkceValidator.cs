namespace IdentityService.Security.Authorize;

public static class PkceValidator
{
    public static bool IsChallengeMethodAllowed(string? method) =>
        throw new NotImplementedException("TB-042 PKCE method validation is not implemented.");

    public static bool IsValidS256ChallengeFormat(string? codeChallenge) =>
        throw new NotImplementedException("TB-042 PKCE challenge format validation is not implemented.");

    public static byte[] ComputeS256ChallengeBytes(string codeVerifier) =>
        throw new NotImplementedException("TB-042 PKCE S256 computation is not implemented.");
}