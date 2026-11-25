using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Teamboard.Shared.Http;

namespace Gateway;

/// <summary>Access log for completed requests: metadata only, never Authorization or body (NFR-006).</summary>
internal sealed class GatewayRequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLogging> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        LogCompleted(context);
    }

    private void LogCompleted(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue(CorrelationHeaders.ItemKey, out var item) && item is string id
            ? id
            : string.Empty;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var method = context.Request.Method;
        var status = context.Response.StatusCode;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["clientIp"] = clientIp,
            ["method"] = method,
            ["status"] = status,
            ["traceId"] = traceId,
        }))
        {
            logger.LogInformation("Request completed");
        }
    }
}

/// <summary>Logger category <c>Gateway.RequestLogging</c> for access-log tests (TB-027 @S01–@S02).</summary>
internal sealed class RequestLogging;