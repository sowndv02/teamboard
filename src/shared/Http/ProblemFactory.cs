using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Teamboard.Shared.Http;

/// <summary>Dựng <c>problem+json</c> theo <c>api-endpoints.md</c> mục 5; không đưa chi tiết nội bộ vào lỗi.</summary>
public static class ProblemFactory
{
    private const string TypePrefix = "https://teamboard/errors/";

    public static ProblemDetails Create(ErrorDefinition error, HttpContext context, string? detail = null)
    {
        // Chưa có middleware correlation (lát 2): correlationId tạm bằng traceId để trường luôn có mặt.
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
        problem.Extensions["correlationId"] = traceId;
        return problem;
    }
}
