using IdentityService.Security;
using IdentityService.Security.Signing;

namespace IdentityService.Features.Jwks;

internal static class JwksEndpoints
{
    private const string CacheControlValue = "public, max-age=600";

    public static void MapJwksEndpoints(this IEndpointRouteBuilder routes) =>
        routes.MapGet("/.well-known/jwks.json", GetJwksAsync)
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status503ServiceUnavailable);

    private static async Task GetJwksAsync(
        ISigningKeyStore signingKeyStore,
        IdentityMetrics metrics,
        HttpResponse response)
    {
        if (!signingKeyStore.HasActiveKey)
        {
            response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var activeKey = signingKeyStore.GetActiveKey();
        var body = IdentityJwksSetBuilder.SerializeActiveKey(activeKey);
        metrics.RecordJwksServed();

        response.StatusCode = StatusCodes.Status200OK;
        response.Headers.CacheControl = CacheControlValue;
        response.ContentType = "application/json";
        await response.WriteAsync(body);
    }
}