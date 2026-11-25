namespace Gateway;

/// <summary>Origins allowed to call the gateway from a browser (section "Cors"); validated at startup by CorsOptionsValidator.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
