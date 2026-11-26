using System.Net;
using System.Net.Sockets;

namespace IdentityService.Security;

public sealed class ClientIpResolver : IClientIpResolver
{
    public const string UnknownClientKey = "unknown";

    public string Resolve(HttpContext context) => PartitionKeyFor(context.Connection.RemoteIpAddress);

    internal static string PartitionKeyFor(IPAddress? address)
    {
        if (address is null)
        {
            return UnknownClientKey;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily is AddressFamily.InterNetwork)
        {
            return $"v4:{address}";
        }

        if (address.AddressFamily is AddressFamily.InterNetworkV6)
        {
            return $"v6:{ToNetworkPrefix64(address)}";
        }

        return UnknownClientKey;
    }

    private static string ToNetworkPrefix64(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        for (var i = 8; i < bytes.Length; i++)
        {
            bytes[i] = 0;
        }

        return new IPAddress(bytes).ToString();
    }
}