// Khung tối thiểu để test biên dịch được (TB-010, test-init). Routing, health, fallback viết ở B3a, B3b.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();

// Để test dùng WebApplicationFactory<Program>.
public partial class Program;
