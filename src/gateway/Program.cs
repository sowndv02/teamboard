using Gateway;
using Gateway.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
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

builder.Services
    .AddOptions<BodyLimitOptions>()
    .BindConfiguration(BodyLimitOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ForwardedHeadersSetup>();
builder.Services.AddGatewayRateLimiting();
builder.Services.AddGatewayCors();

// Needed for the route-level Timeout in the YARP configuration: without this middleware a route with Timeout fails with 500.
builder.Services.AddRequestTimeouts();

builder.Services.AddSingleton<GatewayMetrics>();
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(GatewayMetrics.MeterName));

var app = builder.Build();

// Target middleware order (design 6.4): ForwardedHeaders, Routing, Correlation, ExceptionHandler, RequestTimeouts, UpstreamError, CORS, RateLimiter, BodyLimit, YARP.
app.UseForwardedHeaders();
app.UseRouting();
app.UseCorrelation();
app.UseTeamboardExceptionHandler();
app.UseRequestTimeouts();
app.UseMiddleware<UpstreamErrorMiddleware>();
app.UseGatewayCors();
app.UseRateLimiter();
app.UseMiddleware<BodyLimitMiddleware>();

app.MapDefaultEndpoints();
app.MapReverseProxy();

// MapFallback's default has a `nonfile` constraint, so file-like paths (/foo.txt) slip through; use an explicit catch-all pattern.
app.MapFallback("{**path}", (HttpContext context) =>
    Results.Problem(ProblemFactory.Create(GatewayErrorCodes.RouteNotFound, context)));

app.Run();

// Exposed so tests can use WebApplicationFactory<Program>.
public partial class Program;
