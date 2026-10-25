using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Teamboard.Shared.Http;

/// <summary>
/// Attaches a validated correlation id to every request: keeps a valid client id, replaces any other value with a new GUID.
/// Never trusts or logs the raw client value (prevents log injection).
/// </summary>
internal sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = ReadValidId(context) ?? Guid.NewGuid().ToString();

        context.Items[CorrelationHeaders.ItemKey] = id;
        // Overwrite the request header so YARP forwards the validated id to the upstream.
        context.Request.Headers[CorrelationHeaders.Name] = id;
        // OnStarting: UseExceptionHandler calls Response.Clear(), so a header set directly would be lost; this callback runs after the upstream has copied its headers.
        context.Response.OnStarting(static state =>
        {
            var (response, value) = ((HttpResponse, string))state;
            response.Headers[CorrelationHeaders.Name] = value;
            return Task.CompletedTask;
        }, (context.Response, id));

        Activity.Current?.SetTag("correlationId", id);
        using (logger.BeginScope(new Dictionary<string, object?> { ["correlationId"] = id }))
        {
            await next(context);
        }
    }

    // Iterate characters instead of using a regex: .NET's `$` matches before a trailing "\n", so a regex would wrongly accept "abc\n".
    private static string? ReadValidId(HttpContext context)
    {
        var values = context.Request.Headers[CorrelationHeaders.Name];
        if (values.Count != 1)
        {
            return null;
        }

        var value = values[0];
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return null;
        }

        foreach (var c in value)
        {
            var allowed = c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-';
            if (!allowed)
            {
                return null;
            }
        }

        return value;
    }
}
