using System.ComponentModel.DataAnnotations;

namespace Gateway;

/// <summary>Request body size limit of the gateway (section "Gateway:BodyLimit"; spec FR-007).</summary>
public sealed class BodyLimitOptions
{
    public const string SectionName = "Gateway:BodyLimit";

    /// <summary>Largest request body accepted, in bytes; 1 MiB by default, configurable up to 10 MiB (plan TB-025, Confirm #3).</summary>
    [Range(1, 10_485_760)]
    public int MaxBytes { get; set; } = 1_048_576;
}
