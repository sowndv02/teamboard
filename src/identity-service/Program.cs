using IdentityService.Features.ChangePassword;
using IdentityService.Features.Jwks;
using IdentityService.Features.Login;
using IdentityService.Features.Logout;
using IdentityService.Features.Refresh;
using IdentityService.Features.Register;
using IdentityService.Features.Authorize;
using IdentityService.Features.Token;
using IdentityService.Persistence;
using IdentityService.RateLimiting;
using IdentityService.Security;
using IdentityService.Security.Clients;
using Teamboard.Shared.Http;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("identity-service");
builder.AddIdentityPersistence();
builder.AddIdentityClientRegistry();
builder.AddIdentitySecurity();

var rateLimitingEnabled = builder.Configuration.GetValue("Identity:RateLimiting:Enabled", true);
if (rateLimitingEnabled)
{
    builder.AddIdentityRateLimiting();
}

var app = builder.Build();

await app.ApplyMigrationsIfEnabledAsync();

// Target middleware order (backend-engineering.md section 5): correlation, exception handler, authentication, authorization, endpoint.
// Slice 1a only has the exception handler and health; the shared correlation waits for gateway slice 2.
// The handler returns INTERNAL_ERROR as problem+json; the exception goes only to the server log, never to the response.
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Problem(ProblemFactory.Create(SharedErrors.InternalError, context)).ExecuteAsync(context)));

if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapRegisterEndpoints(rateLimitingEnabled);
app.MapLoginEndpoints(rateLimitingEnabled);
app.MapRefreshEndpoints(rateLimitingEnabled);
app.MapLogoutEndpoints(rateLimitingEnabled);
app.MapChangePasswordEndpoints(rateLimitingEnabled);
app.MapJwksEndpoints();
app.MapTokenEndpoints(rateLimitingEnabled);
app.MapAuthorizeEndpoints(rateLimitingEnabled);

app.Run();

// Exposed so tests can use WebApplicationFactory<Program>.
public partial class Program;
