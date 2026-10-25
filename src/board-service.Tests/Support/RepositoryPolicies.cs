using System.Text.RegularExpressions;

namespace BoardService.Tests.Support;

/// <summary>Repo check rules used by @S14 (version pinning) and @S15 (no hard-coded secrets); covered by their own unit tests.</summary>
internal static partial class RepositoryPolicies
{
    /// <summary>Exact version: dotted numbers, optionally with a prerelease suffix; no ranges, no wildcards.</summary>
    public static bool IsPinnedVersion(string? version) =>
        !string.IsNullOrWhiteSpace(version) && PinnedVersion().IsMatch(version);

    /// <summary>
    /// A line with a hard-coded secret value in one of two forms:
    /// a sensitive key whose value is not a `${...}` (`MSSQL_SA_PASSWORD: abc`),
    /// or a `Password=`/`Pwd=`/`Secret=` pair inside a string (connection string).
    /// A default value `${VAR:-literal}` also counts as hard-coded.
    /// </summary>
    public static bool IsHardcodedSecret(string line) =>
        HasHardcodedKeyValue(line) || HasHardcodedPair(line) || DefaultLiteral().IsMatch(line) && SensitiveWord().IsMatch(line);

    private static bool HasHardcodedKeyValue(string line)
    {
        var match = SecretAssignment().Match(line);
        if (!match.Success)
        {
            return false;
        }

        var value = match.Groups["value"].Value.Trim().Trim('"', '\'');
        return value.Length > 0 && !value.Contains("${", StringComparison.Ordinal);
    }

    private static bool HasHardcodedPair(string line) =>
        SecretPair().Matches(line).Any(pair => !pair.Groups["value"].Value.StartsWith("${", StringComparison.Ordinal));

    [GeneratedRegex(@"^\d+(\.\d+){1,3}(-[0-9A-Za-z.\-]+)?$")]
    private static partial Regex PinnedVersion();

    [GeneratedRegex(@"^\s*-?\s*(?<key>[A-Za-z0-9_]*(password|secret|api[_-]?key|token)[A-Za-z0-9_]*)\s*[:=]\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex SecretAssignment();

    [GeneratedRegex(@"\b(password|pwd|secret|api[_-]?key|token)\s*=\s*(?<value>[^;""'\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPair();

    [GeneratedRegex(@"\$\{[^}]*:-[^}]+\}")]
    private static partial Regex DefaultLiteral();

    [GeneratedRegex(@"password|pwd|secret|api[_-]?key|token", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveWord();
}
