using System.Globalization;
using System.Threading.RateLimiting;
using IdentityService.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;

namespace IdentityService.RateLimiting;

public static class IdentityRateLimitingExtensions
{
    public const string AuthPublicPolicyName = "auth-public";

    public static WebApplicationBuilder AddIdentityRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<IdentityRateLimitOptions>()
            .BindConfiguration(IdentityRateLimitOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<IdentityRateLimitOptions>, IdentityRateLimitOptionsValidator>();

        builder.Services.AddRateLimiter();
        builder.Services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterOptionsConfigurer>();
        return builder;
    }

    private sealed class RateLimiterOptionsConfigurer(IOptions<IdentityRateLimitOptions> rateLimitOptions) : IConfigureOptions<RateLimiterOptions>
    {
        public void Configure(RateLimiterOptions options)
        {
            var settings = rateLimitOptions.Value;

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    settings.WindowSeconds.ToString(CultureInfo.InvariantCulture);
                await Results.Problem(ProblemFactory.Create(IdentityErrorCodes.RateLimitExceeded, context.HttpContext))
                    .ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy(AuthPublicPolicyName, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    ClientIpResolver.PartitionKeyFor(httpContext.Connection.RemoteIpAddress),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    }));
        }
    }
}