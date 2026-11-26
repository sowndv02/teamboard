using IdentityService.Security.Jwt;
using IdentityService.Security.Passwords;
using IdentityService.Security.Signing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Teamboard.Shared.ServiceDefaults;

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

        builder.Services.AddOptions<SigningKeysOptions>().BindConfiguration(SigningKeysOptions.SectionName).ValidateOnStart();
        builder.Services.AddOptions<IdentityJwtOptions>()
            .BindConfiguration(IdentityJwtOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<IdentityJwtOptions>, IdentityJwtOptionsValidator>();

        builder.Services.AddSingleton<ISigningKeyStore, FileSigningKeyStore>();
        builder.Services.AddSingleton<IAccessTokenIssuer, AccessTokenIssuer>();

        builder.Services.AddHealthChecks().AddCheck<SigningKeysHealthCheck>(
            "signing_keys",
            failureStatus: HealthStatus.Unhealthy,
            tags: [ServiceDefaultsExtensions.ReadyTag]);

        builder.Services.AddSingleton<IdentityMetrics>();
        builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
        builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(IdentityMetrics.MeterName));
        return builder;
    }
}