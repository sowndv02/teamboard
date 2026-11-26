using System.Text.Json.Serialization;
using IdentityService.RateLimiting;
using IdentityService.Security.Token;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.Token;

internal static class TokenEndpoints
{
    public const string ConnectTokenPath = "/connect/token";

    private const string ClientCredentialsGrant = "client_credentials";
    private const string FeatureFlagKey = "Identity:Features:ClientCredentialsEnabled";

    public static void MapTokenEndpoints(this IEndpointRouteBuilder routes, bool enableRateLimiting = true)
    {
        var endpoint = routes.MapPost(ConnectTokenPath, HandleConnectTokenAsync)
            .Produces<OAuthTokenSuccessResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        if (enableRateLimiting)
        {
            endpoint.RequireRateLimiting(IdentityRateLimitingExtensions.AuthPublicPolicyName);
        }
    }

    private static async Task<IResult> HandleConnectTokenAsync(
        HttpContext httpContext,
        IConfiguration configuration,
        ClientCredentialsTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue(FeatureFlagKey, false))
        {
            return TypedResults.NotFound();
        }

        if (!httpContext.Request.HasFormContentType)
        {
            return OAuthError("invalid_request", "The request must use application/x-www-form-urlencoded.", StatusCodes.Status400BadRequest);
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var grantType = form["grant_type"].ToString();
        if (!string.Equals(grantType, ClientCredentialsGrant, StringComparison.Ordinal))
        {
            return OAuthError("unsupported_grant_type", "Only client_credentials is supported.", StatusCodes.Status400BadRequest);
        }

        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();
        var scope = form["scope"].ToString();

        var exchange = tokenService.TryExchange(clientId, clientSecret, scope);
        if (exchange.IsSuccess)
        {
            var issued = exchange.Token!;
            return TypedResults.Json(
                new OAuthTokenSuccessResponse(issued.Token, "Bearer", issued.ExpiresInSeconds),
                statusCode: StatusCodes.Status200OK);
        }

        return exchange.Error switch
        {
            "invalid_scope" => OAuthError(exchange.Error!, exchange.ErrorDescription!, StatusCodes.Status400BadRequest),
            "unauthorized_client" => OAuthError(exchange.Error!, exchange.ErrorDescription!, StatusCodes.Status401Unauthorized),
            _ => OAuthError(exchange.Error ?? "invalid_client", exchange.ErrorDescription ?? "Client authentication failed.", StatusCodes.Status401Unauthorized),
        };
    }

    private static IResult OAuthError(string error, string description, int statusCode) =>
        TypedResults.Json(new OAuthTokenErrorResponse(error, description), statusCode: statusCode);

    private sealed record OAuthTokenSuccessResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresInSeconds);

    private sealed record OAuthTokenErrorResponse(
        [property: JsonPropertyName("error")] string Error,
        [property: JsonPropertyName("error_description")] string ErrorDescription);
}