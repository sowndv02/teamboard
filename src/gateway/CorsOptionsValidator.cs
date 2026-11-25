using Microsoft.Extensions.Options;

namespace Gateway;

/// <summary>
/// Rejects allowlist entries that cannot match a browser Origin header exactly. A wildcard or an entry with a path
/// would silently allow nothing or everything, so the gateway refuses to start instead.
/// </summary>
public sealed class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        var failures = new List<string>();
        foreach (var origin in options.AllowedOrigins)
        {
            if (!IsOrigin(origin))
            {
                failures.Add($"Cors:AllowedOrigins contains '{origin}', which is not an absolute http(s) origin without path, query or trailing slash.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    // A browser sends the origin as scheme://host[:port] with no trailing slash; comparing against the
    // authority form keeps the check and the CORS policy matching the same string.
    private static bool IsOrigin(string? origin) =>
        !string.IsNullOrEmpty(origin)
        && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal);
}
