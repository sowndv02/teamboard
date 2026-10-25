using System.Diagnostics.Metrics;

namespace Gateway;

/// <summary>Custom metrics of the gateway. Counters carry no per-request labels (no correlationId): the id lives in the log.</summary>
public sealed class GatewayMetrics
{
    public const string MeterName = "Teamboard.Gateway";

    private readonly Counter<long> _upstreamAborted;

    public GatewayMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _upstreamAborted = meter.CreateCounter<long>(
            "gateway_upstream_aborted",
            description: "Responses cut after the headers were sent because the upstream failed or stalled.");
    }

    public void UpstreamAborted() => _upstreamAborted.Add(1);
}
