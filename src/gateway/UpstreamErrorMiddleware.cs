using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;
using Yarp.ReverseProxy.Forwarder;

namespace Gateway;

/// <summary>
/// Turns forwarder failures that happen before the response starts into problem+json (design D4).
/// It sits in front of YARP and reads <see cref="IForwarderErrorFeature"/> once the forwarder has returned.
/// </summary>
public sealed partial class UpstreamErrorMiddleware(RequestDelegate next, ILogger<UpstreamErrorMiddleware> logger, IOptions<UpstreamOptions> options, GatewayMetrics metrics)
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
                // 503 and 504 are routine while an upstream is down and YARP already logs the exception: only the type is kept here.
                LogUnavailable(feature.Error, feature.Exception?.GetType().Name);
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamUnavailable, retryAfter: true);
                break;
            case UpstreamFailure.Timeout:
                LogTimeout(feature.Error, feature.Exception?.GetType().Name);
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamTimeout, retryAfter: false);
                break;
            case UpstreamFailure.BadGateway:
                // The exception goes to the log only: it can name internal hosts and ports, which the response must never carry.
                LogBadGateway(feature.Exception, feature.Error);
                await WriteProblemAsync(context, GatewayErrorCodes.UpstreamBadGateway, retryAfter: false);
                break;
            case UpstreamFailure.AbortedAfterHeaders:
                // Too late to change the status: YARP already cut the connection, so record it for the log and the counter.
                LogAborted(feature.Exception, feature.Error);
                metrics.UpstreamAborted();
                break;
            case UpstreamFailure.ClientCancelled:
                // The client is gone: nothing to answer and not an upstream failure.
                LogClientCancelled(feature.Error);
                break;
        }
    }

    private async Task WriteProblemAsync(HttpContext context, ErrorDefinition error, bool retryAfter)
    {
        // No Response.Clear(): headers set by other middleware in the pipeline (CORS, slice 6) must survive the error.
        // YARP sets only the status before a failure, and Results.Problem overwrites it together with the content type.
        if (retryAfter)
        {
            context.Response.Headers.RetryAfter = options.Value.RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await Results.Problem(ProblemFactory.Create(error, context)).ExecuteAsync(context);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream unavailable, answered 503 ({ForwarderError}, {ExceptionType}).")]
    private partial void LogUnavailable(ForwarderError forwarderError, string? exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream timed out, answered 504 ({ForwarderError}, {ExceptionType}).")]
    private partial void LogTimeout(ForwarderError forwarderError, string? exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream failure answered with 502 ({ForwarderError}).")]
    private partial void LogBadGateway(Exception? exception, ForwarderError forwarderError);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream failed after the response started ({ForwarderError}); the connection was cut.")]
    private partial void LogAborted(Exception? exception, ForwarderError forwarderError);

    [LoggerMessage(Level = LogLevel.Information, Message = "The client cancelled the request ({ForwarderError}).")]
    private partial void LogClientCancelled(ForwarderError forwarderError);
}
