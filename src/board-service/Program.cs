using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("board-service");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "TeamBoard API v1");
    });
}

app.UseAuthorization();

// TB-033 stub: protected route wired in coding-start (Teamboard.Auth + real sub claim).
app.MapGet("/api/v1/auth/me", () => Results.Json(new { sub = "not-implemented" }));

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

// Exposed so tests can use WebApplicationFactory<Program>.
public partial class Program;
