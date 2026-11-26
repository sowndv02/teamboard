using IdentityService.Security.Passwords;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;

namespace IdentityService.Security;

public static class IdentitySecurityExtensions
{
    public static WebApplicationBuilder AddIdentitySecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<PasswordOptions>()
            .BindConfiguration(PasswordOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<PasswordOptions>, PasswordOptionsValidator>();

        builder.Services.AddOptions<LockoutOptions>()
            .BindConfiguration(LockoutOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<LockoutOptions>, LockoutOptionsValidator>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IClientIpResolver, ClientIpResolver>();
        builder.Services.AddScoped<LoginLockoutService>();

        builder.Services.AddSingleton<IdentityMetrics>();
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(IdentityMetrics.MeterName));
        return builder;
    }
}