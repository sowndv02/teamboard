using System.Text.RegularExpressions;

namespace BoardService.Tests.Support;

/// <summary>Luật kiểm tra repo dùng cho @S14 (pin phiên bản) và @S15 (không secret viết cứng); có unit test riêng.</summary>
internal static partial class RepositoryPolicies
{
    /// <summary>Phiên bản chính xác: số chấm số, có thể kèm hậu tố tiền phát hành; không dải, không dấu sao.</summary>
    public static bool IsPinnedVersion(string? version) =>
        !string.IsNullOrWhiteSpace(version) && PinnedVersion().IsMatch(version);

    /// <summary>
    /// Dòng có giá trị bí mật viết cứng ở một trong hai dạng:
    /// khoá nhạy cảm có giá trị không phải tham chiếu `${...}` (`MSSQL_SA_PASSWORD: abc`),
    /// hoặc cặp `Password=`/`Pwd=`/`Secret=` nằm trong chuỗi (connection string).
    /// Giá trị mặc định `${VAR:-literal}` cũng bị coi là viết cứng.
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
