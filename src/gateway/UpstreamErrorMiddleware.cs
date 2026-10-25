using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway;

/// <summary>
/// Turns forwarder failures that happen before the response starts into problem+json (design D4).
/// It sits in front of YARP and reads <see cref="IForwarderErrorFeature"/> once the forwarder has returned.
/// </summary>
public sealed class UpstreamErrorMiddleware(RequestDelegate next, ILogger<UpstreamErrorMiddleware> logger, IOptions<UpstreamOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var feature = context.GetForwarderErrorFeature();
        if (feature is null)
        {
            return;
        }

        // The route Timeout also cancels RequestAborted, so only this feature tells a timeout apart from a client that left.
        var routeTimeoutFired = context.Features.Get<IHttpRequestTimeoutFeature>()?.RequestTimeoutToken.IsCancellationRequested == true;
        var failure = UpstreamErrorClassifier.Classify(feature.Error, feature.Exception, routeTimeoutFired, context.Response.HasStarted);
        switch (failure)
        {
            case UpstreamFailure.Unavailable:
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamUnavailable, feature, retryAfter: true);
                break;
            case UpstreamFailure.BadGateway:
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamBadGateway, feature, retryAfter: false);
                break;
            case UpstreamFailure.Timeout:
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamTimeout, feature, retryAfter: false);
                break;
            case UpstreamFailure.ClientCancelled:
                // The client is gone: nothing to answer and not an upstream failure.
                logger.LogInformation("The client cancelled the request ({ForwarderError}).", feature.Error);
                break;
        }
    }

    private async Task WriteProblemAsync(HttpContext context, ErrorDefinition error, IForwarderErrorFeature feature, bool retryAfter)
    {
        // The exception goes to the log only: it can name internal hosts and ports, which the response must never carry.
        logger.LogWarning(feature.Exception, "Upstream failure answered with {Code} ({ForwarderError}).", error.Code, feature.Error);

        context.Response.Clear();
        if (retryAfter)
        {
            context.Response.Headers.RetryAfter = options.Value.RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await Results.Problem(ProblemFactory.Create(error, context)).ExecuteAsync(context);
    }
}
