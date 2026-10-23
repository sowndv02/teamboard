using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Teamboard.Shared.Http;

// Khung tối thiểu để test biên dịch được (TB-010, test-init). Hành vi thật được viết ở B2.
public static class ProblemFactory
{
    public static ProblemDetails Create(ErrorDefinition error, HttpContext context, string? detail = null)
        => throw new NotImplementedException();
}
