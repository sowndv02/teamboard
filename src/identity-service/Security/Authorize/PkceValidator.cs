using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityService.Security.Authorize;

public static class PkceValidator
{
    private const int MinChallengeLength = 43;
    private const int MaxChallengeLength = 128;
    private static readonly Regex Base64UrlPattern = new(@"^[A-Za-z0-9\-._~]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsChallengeMethodAllowed(string? method) =>
        string.Equals(method, "S256", StringComparison.Ordinal);

    public static bool IsValidS256ChallengeFormat(string? codeChallenge) =>
        !string.IsNullOrWhiteSpace(codeChallenge)
        && codeChallenge.Length is >= MinChallengeLength and <= MaxChallengeLength
        && Base64UrlPattern.IsMatch(codeChallenge);

    public static byte[] ComputeS256ChallengeBytes(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return bytes;
    }

    public static bool VerifyS256(string codeVerifier, string codeChallenge) =>
        throw new NotImplementedException("TB-043 coding-start");
}