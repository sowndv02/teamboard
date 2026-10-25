using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Teamboard.Shared.Http;

public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Biến lỗi chưa lường thành 500 problem+json mã INTERNAL_ERROR, không lộ chi tiết nội bộ.
    /// Ghi thẳng response trong <c>app.Run</c> (không dùng ExceptionHandlingPath, vốn chạy lại cả pipeline);
    /// middleware của ASP.NET Core đã log exception, nằm trong scope correlation nếu <c>UseCorrelation</c> đặt trước.
    /// Response đã bắt đầu gửi thì không ghi lại được (thuộc lát 3).
    /// </summary>
    public static IApplicationBuilder UseTeamboardExceptionHandler(this IApplicationBuilder app)
        => app.UseExceptionHandler(errorApp => errorApp.Run(context =>
            Results.Problem(ProblemFactory.Create(SharedErrors.InternalError, context)).ExecuteAsync(context)));
}
