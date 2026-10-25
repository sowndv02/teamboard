using Microsoft.AspNetCore.Builder;

namespace Teamboard.Shared.Http;

public static class CorrelationExtensions
{
    /// <summary>Đặt trước <c>UseTeamboardExceptionHandler</c> để lỗi và log lỗi vẫn mang correlation id (design 6.4).</summary>
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationMiddleware>();
}
