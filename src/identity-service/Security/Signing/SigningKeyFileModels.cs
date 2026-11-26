using System.Text.Json.Serialization;

namespace IdentityService.Security.Signing;

internal sealed class SigningKeySetFile
{
    [JsonPropertyName("keys")]
    public List<SigningKeyFileEntry> Keys { get; init; } = [];
}

internal sealed class SigningKeyFileEntry
{
    [JsonPropertyName("kid")]
    public string Kid { get; init; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("privateKeyPem")]
    public string PrivateKeyPem { get; init; } = string.Empty;
}