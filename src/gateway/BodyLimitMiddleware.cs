using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;

namespace Gateway;

/// <summary>
/// Rejects request bodies over the limit (FR-007). A declared Content-Length is checked here, before anything is forwarded:
/// Kestrel alone would answer 413 only after the upstream had already received the request (plan TB-025 spike).
/// A body without Content-Length (chunked) is counted while it is read and cut when it passes the limit.
/// </summary>
public sealed class BodyLimitMiddleware(RequestDelegate next, IOptions<BodyLimitOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var maxBytes = options.Value.MaxBytes;

        if (context.Request.ContentLength > maxBytes)
        {
            // No Response.Clear(): headers set by middleware further out (correlation, CORS in slice 6) must survive.
            await Results.Problem(ProblemFactory.Create(GatewayErrorCodes.PayloadTooLarge, context)).ExecuteAsync(context);
            return;
        }

        if (context.Request.ContentLength is null)
        {
            context.Request.Body = new LimitedReadStream(context.Request.Body, maxBytes);
        }

        await next(context);
    }
}
