using Yarp.ReverseProxy.Forwarder;

namespace Gateway;

/// <summary>What the gateway does about a forwarder error.</summary>
public enum UpstreamFailure
{
    None,
    Unavailable,
    BadGateway,
    Timeout,
    ClientCancelled,
    AbortedAfterHeaders,
}

// Minimal skeleton so the tests compile (TB-023, test-init): no behavior yet. The real mapping is written in B1/B2.
public static class UpstreamErrorClassifier
{
    public static UpstreamFailure Classify(ForwarderError error, Exception? exception, bool routeTimeoutFired, bool responseStarted)
        => UpstreamFailure.None;
}
