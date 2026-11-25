using System.ComponentModel.DataAnnotations;

namespace Gateway.RateLimiting;

/// <summary>Sliding-window limits for read and write route groups (section "RateLimiting").</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitGroupOptions Read { get; set; } = new() { PermitLimit = 120 };

    public RateLimitGroupOptions Write { get; set; } = new() { PermitLimit = 30 };
}

/// <summary>One route group's sliding-window parameters.</summary>
public sealed class RateLimitGroupOptions
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; }

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    [Range(1, 100)]
    public int SegmentsPerWindow { get; set; } = 6;
}
