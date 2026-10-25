using Yarp.ReverseProxy.Forwarder;

namespace Gateway;

/// <summary>Việc gateway làm với một lỗi của forwarder.</summary>
public enum UpstreamFailure
{
    None,
    Unavailable,
    BadGateway,
    Timeout,
    ClientCancelled,
    AbortedAfterHeaders,
}

// Khung tối thiểu để test biên dịch được (TB-023, test-init): chưa có hành vi. Ánh xạ thật được viết ở B1/B2.
public static class UpstreamErrorClassifier
{
    public static UpstreamFailure Classify(ForwarderError error, Exception? exception, bool routeTimeoutFired, bool responseStarted)
        => UpstreamFailure.None;
}
