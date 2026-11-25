using System.Net;
using System.Net.Sockets;

namespace Gateway.RateLimiting;

/// <summary>Stable rate-limit partition key for a client address (design D2).</summary>
public static class ClientPartitionKey
{
    public const string UnknownKey = "unknown";

    public static string For(IPAddress? address)
    {
        if (address is null)
        {
            return UnknownKey;
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

        return UnknownKey;
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
