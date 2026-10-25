using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Teamboard.Shared.Http;

public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Turns an unexpected error into a 500 problem+json with code INTERNAL_ERROR, leaking no internal details.
    /// Writes the response directly in <c>app.Run</c> (not ExceptionHandlingPath, which re-runs the whole pipeline);
    /// the ASP.NET Core middleware already logs the exception, inside the correlation scope if <c>UseCorrelation</c> is placed first.
    /// Once the response has started it cannot be rewritten (handled in slice 3).
    /// </summary>
    public static IApplicationBuilder UseTeamboardExceptionHandler(this IApplicationBuilder app)
        => app.UseExceptionHandler(errorApp => errorApp.Run(context =>
            Results.Problem(ProblemFactory.Create(SharedErrors.InternalError, context)).ExecuteAsync(context)));
}
