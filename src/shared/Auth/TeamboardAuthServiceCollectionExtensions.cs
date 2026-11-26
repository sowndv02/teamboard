using Microsoft.Extensions.DependencyInjection;

namespace Teamboard.Shared.Auth;

public static class TeamboardAuthServiceCollectionExtensions
{
    /// <summary>Registers JWT bearer validation. Implementation follows in coding-start (TB-033).</summary>
    public static IServiceCollection AddTeamboardJwtAuthentication(this IServiceCollection services)
    {
        _ = services;
        return services;
    }
}