using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Teamboard.Shared.ServiceDefaults;

// Khung tối thiểu để test biên dịch được (TB-001, test-init). Hành vi thật được viết ở B3 và B4.
public static class ServiceDefaultsExtensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
        => throw new NotImplementedException();

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
        => throw new NotImplementedException();
}
