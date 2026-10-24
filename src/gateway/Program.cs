using Gateway;
using Teamboard.Shared.Http;
using Teamboard.Shared.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults("gateway");

// Route và địa chỉ upstream nằm trong cấu hình "ReverseProxy" (NFR-004), không hardcode trong mã.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Thứ tự middleware đích (design 6.4): ForwardedHeaders, Routing, Correlation, ExceptionHandler, CORS, RateLimiter, BodyLimit, YARP.
// Lát này mới có routing, health và fallback; các middleware còn lại thêm ở các lát sau.
app.MapDefaultEndpoints();
app.MapReverseProxy();

// Mặc định của MapFallback có ràng buộc `nonfile` nên path kiểu tệp (/foo.txt) lọt ra ngoài; dùng mẫu catch-all rõ ràng.
app.MapFallback("{**path}", (HttpContext context) =>
    Results.Problem(ProblemFactory.Create(GatewayErrorCodes.RouteNotFound, context)));

app.Run();

// Để test dùng WebApplicationFactory<Program>.
public partial class Program;
