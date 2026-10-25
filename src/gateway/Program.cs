using Gateway;
using Teamboard.Shared.Http;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("gateway");

// Routes and upstream addresses live in the "ReverseProxy" configuration (NFR-004), not hardcoded.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services
    .AddOptions<UpstreamOptions>()
    .BindConfiguration(UpstreamOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Needed for the route-level Timeout in the YARP configuration: without this middleware a route with Timeout fails with 500.
builder.Services.AddRequestTimeouts();

var app = builder.Build();

// Target middleware order (design 6.4): ForwardedHeaders, Routing, Correlation, ExceptionHandler, RequestTimeouts, CORS, RateLimiter, BodyLimit, YARP.
// This slice has Correlation, ExceptionHandler, routing, health and fallback; the remaining middleware are added in later slices.
// UseRouting is added at the start of the pipeline by minimal hosting, so it is not called explicitly.
app.UseCorrelation();
app.UseTeamboardExceptionHandler();
app.UseRequestTimeouts();
app.UseMiddleware<UpstreamErrorMiddleware>();

app.MapDefaultEndpoints();
app.MapReverseProxy();

// MapFallback's default has a `nonfile` constraint, so file-like paths (/foo.txt) slip through; use an explicit catch-all pattern.
app.MapFallback("{**path}", (HttpContext context) =>
    Results.Problem(ProblemFactory.Create(GatewayErrorCodes.RouteNotFound, context)));

app.Run();

// Exposed so tests can use WebApplicationFactory<Program>.
public partial class Program;
