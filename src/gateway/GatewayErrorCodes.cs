using Teamboard.Shared.Http;

namespace Gateway;

/// <summary>Error codes owned by the gateway, same <see cref="ErrorDefinition"/> type as the shared table.</summary>
public static class GatewayErrorCodes
{
    public static readonly ErrorDefinition RouteNotFound = new("ROUTE_NOT_FOUND", 404, "No route matches the request.");

    public static readonly ErrorDefinition UpstreamUnavailable = new("UPSTREAM_UNAVAILABLE", 503, "The upstream service is unavailable.");

    public static readonly ErrorDefinition UpstreamBadGateway = new("UPSTREAM_BAD_GATEWAY", 502, "The upstream service returned an invalid response.");

    public static readonly ErrorDefinition UpstreamTimeout = new("UPSTREAM_TIMEOUT", 504, "The upstream service did not respond in time.");

    public static IReadOnlyList<ErrorDefinition> All { get; } = [RouteNotFound, UpstreamUnavailable, UpstreamBadGateway, UpstreamTimeout];
}
