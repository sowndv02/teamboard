using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Gateway;

/// <summary>Binds trusted proxy lists from configuration without clearing the middleware loopback defaults (design D5).</summary>
public sealed class ForwardedHeadersSetup(IConfiguration configuration) : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        foreach (var entry in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(entry, out var address))
            {
                throw new InvalidOperationException($"Invalid value in ForwardedHeaders:KnownProxies: '{entry}'.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (var entry in configuration.GetSection("ForwardedHeaders:KnownIPNetworks").Get<string[]>() ?? [])
        {
            if (!System.Net.IPNetwork.TryParse(entry, out var network))
            {
                throw new InvalidOperationException($"Invalid value in ForwardedHeaders:KnownIPNetworks: '{entry}'.");
            }

            options.KnownIPNetworks.Add(network);
        }
    }
}
