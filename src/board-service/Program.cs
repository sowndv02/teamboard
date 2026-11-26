using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Teamboard.Shared.Auth;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("board-service");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddTeamboardJwtAuthentication(builder.Configuration);

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/v1/auth/me", (ClaimsPrincipal user) =>
{
    var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Results.Json(new { sub });
}).RequireAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;