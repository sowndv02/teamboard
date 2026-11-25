using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Teamboard.Shared.Http;

namespace Gateway.RateLimiting;

public static class RateLimitingExtensions
{
    public const string ReadPolicyName = "read";
    public const string WritePolicyName = "write";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>()
            .BindConfiguration(RateLimitOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        services.AddRateLimiter();
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, RateLimiterOptionsConfigurer>();
        return services;
    }

    private sealed class RateLimiterOptionsConfigurer(IOptions<RateLimitOptions> rateLimitOptions) : IConfigureOptions<RateLimiterOptions>
    {
        public void Configure(RateLimiterOptions options)
        {
            var settings = rateLimitOptions.Value;

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var windowSeconds = WindowSecondsFor(context.HttpContext, settings);
                context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString(CultureInfo.InvariantCulture);
                await Results.Problem(ProblemFactory.Create(GatewayErrorCodes.RateLimitExceeded, context.HttpContext))
                    .ExecuteAsync(context.HttpContext);
            };

            AddSlidingWindowPolicy(options, ReadPolicyName, settings.Read);
            AddSlidingWindowPolicy(options, WritePolicyName, settings.Write);
        }

        private static void AddSlidingWindowPolicy(RateLimiterOptions options, string policyName, RateLimitGroupOptions group) =>
            options.AddPolicy(policyName, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    ClientPartitionKey.For(httpContext.Connection.RemoteIpAddress),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = group.PermitLimit,
                        Window = TimeSpan.FromSeconds(group.WindowSeconds),
                        SegmentsPerWindow = group.SegmentsPerWindow,
                        QueueLimit = 0,
                    }));

        private static int WindowSecondsFor(HttpContext httpContext, RateLimitOptions settings)
        {
            var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
            return string.Equals(policyName, WritePolicyName, StringComparison.Ordinal)
                ? settings.Write.WindowSeconds
                : settings.Read.WindowSeconds;
        }
    }
}
