using IdentityService.Persistence;
using Teamboard.Shared.Http;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("identity-service");
builder.AddIdentityPersistence();

var app = builder.Build();

await app.ApplyMigrationsIfEnabledAsync();

// Thứ tự middleware đích (backend-engineering.md mục 5): correlation, exception handler, xác thực, phân quyền, endpoint.
// Lát 1a mới có exception handler và health; correlation dùng chung chờ gateway lát 2.
// Handler trả INTERNAL_ERROR dạng problem+json; ngoại lệ chỉ vào log của server, không vào response.
app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    Results.Problem(ProblemFactory.Create(SharedErrors.InternalError, context)).ExecuteAsync(context)));

app.MapDefaultEndpoints();

app.Run();

// Để test dùng WebApplicationFactory<Program>.
public partial class Program;
