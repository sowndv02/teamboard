using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Teamboard.Shared.Http;

/// <summary>Builds <c>problem+json</c> per <c>api-endpoints.md</c> section 5; puts no internal details in the error.</summary>
public static class ProblemFactory
{
    private const string TypePrefix = "https://teamboard/errors/";

    public static ProblemDetails Create(ErrorDefinition error, HttpContext context, string? detail = null)
    {
        // correlationId is set by CorrelationMiddleware; when the middleware has not run (e.g. unit tests) fall back to traceId so the field is always present.
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problem = new ProblemDetails
        {
            Type = TypePrefix + error.Code.ToLowerInvariant().Replace('_', '-'),
            Title = ReasonPhrases.GetReasonPhrase(error.Status),
            Status = error.Status,
            Detail = detail ?? error.Message,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["correlationId"] = context.Items[CorrelationHeaders.ItemKey] as string ?? traceId;
        return problem;
    }
}
