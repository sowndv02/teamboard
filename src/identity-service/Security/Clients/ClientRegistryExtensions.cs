using IdentityService.Security.Authorize;
using IdentityService.Security.Token;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Teamboard.Shared.ServiceDefaults;

namespace IdentityService.Security.Clients;

public static class ClientRegistryExtensions
{
    public static WebApplicationBuilder AddIdentityClientRegistry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ClientsOptions>()
            .BindConfiguration(ClientsOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<ClientsOptions>, ClientsOptionsValidator>();

        builder.Services.AddOptions<ClientSecretOptions>()
            .BindConfiguration(ClientSecretOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<RedirectUriMatcher>();
        builder.Services.AddSingleton<ClientSecretHasher>();
        builder.Services.AddScoped<IClientRegistry, ClientRegistry>();
        builder.Services.AddScoped<ClientCredentialsTokenService>();
        builder.Services.AddScoped<AuthorizeRequestValidator>();
        builder.Services.AddHostedService<ClientRegistrySeeder>();

        builder.Services.AddHealthChecks().AddCheck<RegisteredClientsHealthCheck>(
            "registered_clients",
            failureStatus: HealthStatus.Unhealthy,
            tags: [ServiceDefaultsExtensions.ReadyTag]);

        return builder;
    }
}