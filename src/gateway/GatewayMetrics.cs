using System.Diagnostics.Metrics;

namespace Gateway;

/// <summary>Custom metrics of the gateway. Counters carry no per-request labels (no correlationId): the id lives in the log.</summary>
public sealed class GatewayMetrics
{
    public const string MeterName = "Teamboard.Gateway";

    private readonly Counter<long> _upstreamAborted;
    private readonly Counter<long> _http429;
    private readonly Counter<long> _http502;
    private readonly Counter<long> _http503;
    private readonly Counter<long> _http504;
    private readonly Histogram<double> _requestDurationSeconds;
    private readonly Histogram<double> _overheadDurationSeconds;

    public GatewayMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _upstreamAborted = meter.CreateCounter<long>(
            "gateway_upstream_aborted",
            description: "Responses cut after the headers were sent because the upstream failed or stalled.");
        _http429 = meter.CreateCounter<long>("gateway_http_429", description: "Responses answered with HTTP 429.");
        _http502 = meter.CreateCounter<long>("gateway_http_502", description: "Responses answered with HTTP 502.");
        _http503 = meter.CreateCounter<long>("gateway_http_503", description: "Responses answered with HTTP 503.");
        _http504 = meter.CreateCounter<long>("gateway_http_504", description: "Responses answered with HTTP 504.");
        _requestDurationSeconds = meter.CreateHistogram<double>(
            "gateway_request_duration_seconds",
            unit: "s",
            description: "Wall time from gateway entry through response completion.");
        _overheadDurationSeconds = meter.CreateHistogram<double>(
            "gateway_overhead_duration_seconds",
            unit: "s",
            description: "Request duration minus time spent waiting on the upstream forwarder.");
    }

    public void UpstreamAborted() => _upstreamAborted.Add(1);

    public void Http429() => _http429.Add(1);

    public void Http502() => _http502.Add(1);

    public void Http503() => _http503.Add(1);

    public void Http504() => _http504.Add(1);

    public void RecordTimings(double requestSeconds, double upstreamSeconds)
    {
        _requestDurationSeconds.Record(requestSeconds);
        var overhead = Math.Max(0, requestSeconds - upstreamSeconds);
        _overheadDurationSeconds.Record(overhead);
    }
}