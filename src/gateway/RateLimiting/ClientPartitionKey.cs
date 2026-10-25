using System.Net;

namespace Gateway.RateLimiting;

// Khung tối thiểu để test biên dịch được (TB-024, test-init): chưa có hành vi. Hàm khoá thật được viết ở B2.
public static class ClientPartitionKey
{
    public static string For(IPAddress? address) => string.Empty;
}
