using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway;

/// <summary>What the gateway does about a forwarder failure.</summary>
public enum UpstreamFailure
{
    None,
    Unavailable,
    BadGateway,
    Timeout,
    ClientCancelled,
    AbortedAfterHeaders,
}

/// <summary>
/// Maps a YARP forwarder failure to what the gateway does (plan TB-023, mapping table). Pure function: no HttpContext, no I/O.
/// </summary>
public static class UpstreamErrorClassifier
{
    public static UpstreamFailure Classify(ForwarderError error, Exception? exception, bool routeTimeoutFired, bool responseStarted)
    {
        if (error == ForwarderError.None)
        {
            return UpstreamFailure.None;
        }

        var name = error.ToString();
        var isClientSide = name.EndsWith("Client", StringComparison.Ordinal);
        var isDestinationSide = name.EndsWith("Destination", StringComparison.Ordinal);

        if (responseStarted)
        {
            // The status is already sent: the only options are to cut the connection (which YARP does) and to record it.
            if (isClientSide)
            {
                return UpstreamFailure.ClientCancelled;
            }

            if (error == ForwarderError.ResponseBodyCanceled && !routeTimeoutFired)
            {
                return UpstreamFailure.ClientCancelled;
            }

            return UpstreamFailure.AbortedAfterHeaders;
        }

        if (isClientSide)
        {
            return UpstreamFailure.ClientCancelled;
        }

        return error switch
        {
            ForwarderError.NoAvailableDestinations => UpstreamFailure.Unavailable,
            ForwarderError.Request when IsConnectionRefusedOrUnresolvable(exception) => UpstreamFailure.Unavailable,
            ForwarderError.RequestTimedOut => UpstreamFailure.Timeout,
            // The route Timeout cancels RequestAborted, so a cancellation only counts as the client leaving when the timeout did not fire.
            ForwarderError.RequestCanceled or ForwarderError.RequestBodyCanceled =>
                routeTimeoutFired ? UpstreamFailure.Timeout : UpstreamFailure.ClientCancelled,
            // Request without a connection failure, ResponseHeaders, *Destination and every value without its own rule (plan P-03).
            _ => UpstreamFailure.BadGateway,
        };
    }

    // A refused connection or an unresolvable name means nothing is listening: 503. A reset in the middle of an exchange is a bad gateway.
    private static bool IsConnectionRefusedOrUnresolvable(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError })
            {
                return true;
            }

            if (current is SocketException socket
                && socket.SocketErrorCode is SocketError.ConnectionRefused or SocketError.HostNotFound or SocketError.TryAgain
                    or SocketError.NoData or SocketError.NetworkUnreachable or SocketError.HostUnreachable)
            {
                return true;
            }
        }

        return false;
    }
}
