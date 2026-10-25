using Microsoft.AspNetCore.Builder;

namespace Teamboard.Shared.Http;

public static class CorrelationExtensions
{
    /// <summary>Place before <c>UseTeamboardExceptionHandler</c> so errors and error logs still carry the correlation id (design 6.4).</summary>
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationMiddleware>();
}
