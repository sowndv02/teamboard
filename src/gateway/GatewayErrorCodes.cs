using Teamboard.Shared.Http;

namespace Gateway;

// Khung tối thiểu để test biên dịch được (TB-010, test-init). Bảng mã riêng của gateway điền ở B3a.
public static class GatewayErrorCodes
{
    public static IReadOnlyList<ErrorDefinition> All => [];
}
