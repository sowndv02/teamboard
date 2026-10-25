using IdentityService.Persistence;
using Teamboard.Shared.Http;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("identity-service");
builder.AddIdentityPersistence();

var app = builder.Build();

await app.ApplyMigrationsIfEnabledAsync();

// Target middleware order (backend-engineering.md section 5): correlation, exception handler, authentication, authorization, endpoint.
// Slice 1a only has the exception handler and health; the shared correlation waits for gateway slice 2.
// The handler returns INTERNAL_ERROR as problem+json; the exception goes only to the server log, never to the response.
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Problem(ProblemFactory.Create(SharedErrors.InternalError, context)).ExecuteAsync(context)));

app.MapDefaultEndpoints();

app.Run();

// Exposed so tests can use WebApplicationFactory<Program>.
public partial class Program;
