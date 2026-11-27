using System.Text.Json.Serialization;
using IdentityService.RateLimiting;
using IdentityService.Security.Authorize;
using IdentityService.Security.Token;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityService.Features.Token;

internal static class TokenEndpoints
{
    public const string ConnectTokenPath = "/connect/token";

    private const string ClientCredentialsGrant = "client_credentials";
    private const string AuthorizationCodeGrant = "authorization_code";
    private const string ClientCredentialsFeatureFlagKey = "Identity:Features:ClientCredentialsEnabled";
    private const string AuthorizationCodeFeatureFlagKey = "Identity:Features:AuthorizationCodeEnabled";

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
        ClientCredentialsTokenService clientCredentialsService,
        AuthorizationCodeRedemptionService authorizationCodeService,
        CancellationToken cancellationToken)
    {
        var clientCredentialsEnabled = configuration.GetValue(ClientCredentialsFeatureFlagKey, false);
        var authorizationCodeEnabled = configuration.GetValue(AuthorizationCodeFeatureFlagKey, false);
        if (!clientCredentialsEnabled && !authorizationCodeEnabled)
        {
            return TypedResults.NotFound();
        }

        if (!httpContext.Request.HasFormContentType)
        {
            return OAuthError("invalid_request", "The request must use application/x-www-form-urlencoded.", StatusCodes.Status400BadRequest);
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var grantType = form["grant_type"].ToString();
        if (string.Equals(grantType, ClientCredentialsGrant, StringComparison.Ordinal))
        {
            if (!clientCredentialsEnabled)
            {
                return OAuthError("unsupported_grant_type", "The client credentials grant is not enabled.", StatusCodes.Status400BadRequest);
            }

            return HandleClientCredentials(clientCredentialsService, form);
        }

        if (string.Equals(grantType, AuthorizationCodeGrant, StringComparison.Ordinal))
        {
            if (!authorizationCodeEnabled)
            {
                return OAuthError("unsupported_grant_type", "The authorization code grant is not enabled.", StatusCodes.Status400BadRequest);
            }

            return await HandleAuthorizationCodeAsync(authorizationCodeService, form, cancellationToken);
        }

        return OAuthError("unsupported_grant_type", "The requested grant type is not supported.", StatusCodes.Status400BadRequest);
    }

    private static IResult HandleClientCredentials(ClientCredentialsTokenService tokenService, IFormCollection form)
    {
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

    private static async Task<IResult> HandleAuthorizationCodeAsync(
        AuthorizationCodeRedemptionService service,
        IFormCollection form,
        CancellationToken cancellationToken)
    {
        var exchange = await service.TryExchangeAsync(
            form["code"].ToString(),
            form["code_verifier"].ToString(),
            form["client_id"].ToString(),
            form["redirect_uri"].ToString(),
            form.TryGetValue("client_secret", out var secretValue) ? secretValue.ToString() : null,
            cancellationToken);

        if (exchange.IsSuccess)
        {
            var issued = exchange.Token!;
            return TypedResults.Json(
                new OAuthTokenSuccessResponse(issued.Token, "Bearer", issued.ExpiresInSeconds),
                statusCode: StatusCodes.Status200OK);
        }

        return exchange.Error switch
        {
            "invalid_request" => OAuthError(exchange.Error!, exchange.ErrorDescription!, StatusCodes.Status400BadRequest),
            _ when string.Equals(exchange.Error, "invalid_client", StringComparison.Ordinal) =>
                OAuthError(exchange.Error!, exchange.ErrorDescription!, StatusCodes.Status401Unauthorized),
            _ => OAuthError(exchange.Error ?? "invalid_grant", exchange.ErrorDescription ?? "The authorization code is invalid or has expired.", StatusCodes.Status400BadRequest),
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