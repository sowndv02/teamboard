using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Teamboard.Shared.Auth;

public static class TeamboardAuthServiceCollectionExtensions
{
    public static IServiceCollection AddTeamboardJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TeamboardAuthJwtOptions>(configuration.GetSection(TeamboardAuthJwtOptions.SectionName));
        services.AddHttpClient(nameof(JwksKeySetCache), client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<StaticSigningKeyJwksSource>();
        services.AddSingleton<JwksKeySetCache>(sp =>
        {
            var staticSource = sp.GetRequiredService<StaticSigningKeyJwksSource>();
            var options = sp.GetRequiredService<IOptions<TeamboardAuthJwtOptions>>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new JwksKeySetCache(staticSource, options, timeProvider, sp);
        });
        services.AddSingleton<TeamboardJwtTokenValidator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwksKeySetCache, IOptions<TeamboardAuthJwtOptions>>((options, cache, jwtOptions) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters =
                    TeamboardJwtValidationParameters.Create(jwtOptions.Value, cache);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenUse = context.Principal?.FindFirst(TeamboardJwtValidationParameters.TokenUseClaim)?.Value;
                        if (!string.Equals(
                                tokenUse,
                                TeamboardJwtValidationParameters.UserTokenUseValue,
                                StringComparison.Ordinal))
                        {
                            context.Fail("invalid_token_use");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }
}