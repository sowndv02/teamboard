using Microsoft.AspNetCore.Http;
using Xunit;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway.Tests;

/// <summary>A request body that Kestrel cut because it was too large is a 413 for the client, not a client that left (plan TB-025 B2).</summary>
public sealed class BodyLimitClassifierTests
{
    private static readonly BadHttpRequestException TooLarge = new("Request body too large.", StatusCodes.Status413PayloadTooLarge);

    [Fact]
    public void RequestBodyClient_CausedByTooLargeBody_IsPayloadTooLarge() =>
        Assert.Equal(UpstreamFailure.PayloadTooLarge, Classify(ForwarderError.RequestBodyClient, TooLarge));

    // YARP reports the error wrapped (spike TB-025: AggregateException around the BadHttpRequestException).
    [Fact]
    public void RequestBodyClient_TooLargeBodyWrappedInAggregate_IsPayloadTooLarge() =>
        Assert.Equal(UpstreamFailure.PayloadTooLarge, Classify(ForwarderError.RequestBodyClient, new AggregateException(TooLarge)));

    [Fact]
    public void RequestBodyClient_CausedByOtherBadRequest_StaysClientCancelled()
    {
        var malformed = new BadHttpRequestException("Malformed chunk.", StatusCodes.Status400BadRequest);

        Assert.Equal(UpstreamFailure.ClientCancelled, Classify(ForwarderError.RequestBodyClient, malformed));
    }

    [Fact]
    public void RequestBodyClient_WithoutException_StaysClientCancelled() =>
        Assert.Equal(UpstreamFailure.ClientCancelled, Classify(ForwarderError.RequestBodyClient));

    // Once the response has started the status cannot change: the existing rule (client side error, nothing to answer) stays.
    [Fact]
    public void RequestBodyClient_TooLargeBodyAfterResponseStarted_StaysClientCancelled() =>
        Assert.Equal(UpstreamFailure.ClientCancelled, Classify(ForwarderError.RequestBodyClient, TooLarge, responseStarted: true));

    private static UpstreamFailure Classify(ForwarderError error, Exception? exception = null, bool responseStarted = false) =>
        UpstreamErrorClassifier.Classify(error, exception, routeTimeoutFired: false, responseStarted);
}
