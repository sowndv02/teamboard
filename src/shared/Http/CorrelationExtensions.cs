using Microsoft.AspNetCore.Builder;

namespace Teamboard.Shared.Http;

// Khung tối thiểu để test biên dịch được (TB-022, test-init): chỉ chuyển tiếp, không có hành vi. Hành vi thật được viết ở B1a.
public static class CorrelationExtensions
{
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app)
        => app.Use((context, next) => next(context));
}
