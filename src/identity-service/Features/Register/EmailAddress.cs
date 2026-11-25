using System.Text.RegularExpressions;

namespace IdentityService.Features.Register;

internal static partial class EmailAddress
{
    private static readonly Regex ValidFormat =
        ValidFormatRegex();

    public static bool TryNormalize(string email, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        if (!ValidFormat.IsMatch(trimmed))
        {
            return false;
        }

        normalized = trimmed.ToLowerInvariant();
        return true;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex ValidFormatRegex();
}