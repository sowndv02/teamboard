namespace Gateway;

/// <summary>Origins allowed to call the gateway from a browser (section "Cors"). Stub: no behaviour yet (TB-026 test-init).</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
