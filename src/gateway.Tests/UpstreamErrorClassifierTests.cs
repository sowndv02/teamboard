using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Xunit;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

/// <summary>Maps a forwarder failure to what the gateway does (plan TB-023, mapping table; spike on YARP 2.3.0).</summary>
public sealed class UpstreamErrorClassifierTests
{
    private static readonly HttpRequestException ConnectionRefused = new(
        HttpRequestError.ConnectionError, "refused", new SocketException((int)SocketError.ConnectionRefused));

    private static readonly HttpRequestException NameNotResolved = new(
        HttpRequestError.NameResolutionError, "dns", new SocketException((int)SocketError.HostNotFound));

    private static readonly HttpRequestException ConnectionReset = new(
        HttpRequestError.Unknown, "reset", new IOException("connection closed"));

    [Fact]
    public void NoAvailableDestinations_IsUnavailable() =>
        Assert.Equal(UpstreamFailure.Unavailable, Classify(ForwarderError.NoAvailableDestinations));

    [Fact]
    public void Request_ConnectionRefused_IsUnavailable() =>
        Assert.Equal(UpstreamFailure.Unavailable, Classify(ForwarderError.Request, ConnectionRefused));

    [Fact]
    public void Request_NameNotResolved_IsUnavailable() =>
        Assert.Equal(UpstreamFailure.Unavailable, Classify(ForwarderError.Request, NameNotResolved));

    [Fact]
    public void Request_ConnectionResetBeforeHeaders_IsBadGateway() =>
        Assert.Equal(UpstreamFailure.BadGateway, Classify(ForwarderError.Request, ConnectionReset));

    [Fact]
    public void Request_WithoutException_IsBadGateway() =>
        Assert.Equal(UpstreamFailure.BadGateway, Classify(ForwarderError.Request));

    [Theory]
    [MemberData(nameof(TimeoutExceptions))]
    public void RequestTimedOut_IsTimeout(Exception exception) =>
        Assert.Equal(UpstreamFailure.Timeout, Classify(ForwarderError.RequestTimedOut, exception));

    [Theory]
    [InlineData(ForwarderError.RequestCanceled)]
    [InlineData(ForwarderError.RequestBodyCanceled)]
    public void Canceled_ByRouteTimeout_IsTimeout(ForwarderError error) =>
        Assert.Equal(UpstreamFailure.Timeout, Classify(error, routeTimeoutFired: true));

    [Theory]
    [InlineData(ForwarderError.RequestCanceled)]
    [InlineData(ForwarderError.RequestBodyCanceled)]
    [InlineData(ForwarderError.RequestBodyClient)]
    public void Canceled_NotByTimeout_IsClientCancelled(ForwarderError error) =>
        Assert.Equal(UpstreamFailure.ClientCancelled, Classify(error));

    [Theory]
    [InlineData(ForwarderError.ResponseHeaders)]
    [InlineData(ForwarderError.RequestBodyDestination)]
    [InlineData(ForwarderError.UpgradeRequestDestination)]
    public void OtherForwarderErrorsBeforeHeaders_AreBadGateway(ForwarderError error) =>
        Assert.Equal(UpstreamFailure.BadGateway, Classify(error));

    [Theory]
    [InlineData(ForwarderError.ResponseBodyDestination)]
    public void FailureAfterHeaders_IsAborted(ForwarderError error) =>
        Assert.Equal(UpstreamFailure.AbortedAfterHeaders, Classify(error, responseStarted: true));

    [Fact]
    public void ResponseBodyCanceled_ByRouteTimeout_AfterHeaders_IsAborted() =>
        Assert.Equal(UpstreamFailure.AbortedAfterHeaders, Classify(ForwarderError.ResponseBodyCanceled, routeTimeoutFired: true, responseStarted: true));

    [Fact]
    public void ResponseBodyCanceled_NotByTimeout_AfterHeaders_IsClientCancelled() =>
        Assert.Equal(UpstreamFailure.ClientCancelled, Classify(ForwarderError.ResponseBodyCanceled, responseStarted: true));

    [Fact]
    public void None_IsNone() =>
        Assert.Equal(UpstreamFailure.None, Classify(ForwarderError.None));

    public static TheoryData<Exception> TimeoutExceptions() =>
        new() { new TimeoutException("connect"), new TaskCanceledException("activity") };

    private static UpstreamFailure Classify(
        ForwarderError error,
        Exception? exception = null,
        bool routeTimeoutFired = false,
        bool responseStarted = false) =>
        UpstreamErrorClassifier.Classify(error, exception, routeTimeoutFired, responseStarted);
}
