using System.ComponentModel.DataAnnotations;

namespace Gateway;

/// <summary>Settings for how the gateway reports an unavailable upstream (section "Gateway:Upstream").</summary>
public sealed class UpstreamOptions
{
    public const string SectionName = "Gateway:Upstream";

    /// <summary>Seconds sent in Retry-After with a 503; the default comes from design D4 (assumption DA-05).</summary>
    [Range(1, 3600)]
    public int RetryAfterSeconds { get; set; } = 5;
}
