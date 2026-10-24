using Teamboard.Shared.Http;

namespace Gateway;

/// <summary>Mã lỗi riêng của gateway, cùng kiểu <see cref="ErrorDefinition"/> với bảng dùng chung.</summary>
public static class GatewayErrorCodes
{
    public static readonly ErrorDefinition RouteNotFound = new("ROUTE_NOT_FOUND", 404, "No route matches the request.");

    public static IReadOnlyList<ErrorDefinition> All { get; } = [RouteNotFound];
}
