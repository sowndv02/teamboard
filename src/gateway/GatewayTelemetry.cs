using System.Diagnostics;

namespace Gateway;

internal static class GatewayTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Teamboard.Gateway", "1.0.0");
}