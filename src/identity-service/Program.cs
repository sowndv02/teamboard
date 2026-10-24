// Khung tối thiểu để test biên dịch (test-init, TB-021); hành vi thật thêm ở B1 trở đi.
var app = WebApplication.CreateBuilder(args).Build();

app.Run();

// Để test dùng WebApplicationFactory<Program>.
public partial class Program;
