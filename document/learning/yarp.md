# YARP (reverse proxy cho .NET)

> Loại: Công cụ · Một trong các lựa chọn của [API Gateway](api-gateway.md#bản-đồ-giải-pháp) (xem phần "Phạm vi" ở mục 5). Đọc [api-gateway.md](api-gateway.md) trước để hiểu khái niệm; ghi chú này chỉ nói riêng về YARP và cách dự án dùng
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: **mới có spec và design, chưa có code** (`src/` chưa có thư mục gateway; lát 1 là [TB-010](../issues/TB-010-gateway-skeleton-routing.md), chờ TB-001) · YARP: `verified: false` trong `config/dependencies.yaml`

## 1. Vì sao YARP (so với các cách làm gateway khác)

Khái niệm API Gateway và các lựa chọn (Envoy, NGINX, Kong, Traefik, Ocelot, dịch vụ managed...) nằm ở [api-gateway.md](api-gateway.md). Với một đội .NET có một cửa vào mỏng và muốn tuỳ biến bằng C# (correlation id, `problem+json`, rate limit riêng), một **thư viện reverse proxy chạy trong ASP.NET Core** có hai lợi thế: cùng công nghệ với các service, và chạy giống hệt trên máy dev lẫn ECS. Đó là lý do design chọn YARP, đánh đổi bằng việc tự lo những thứ dịch vụ managed có sẵn (WAF, HA).

## 2. Là gì (hiểu trong 1 phút)

**YARP** (Yet Another Reverse Proxy) là thư viện reverse proxy do Microsoft duy trì, dựng trên ASP.NET Core, mã nguồn mở (giấy phép MIT theo README repo dotnet/yarp, agent đã đọc). Bạn khai báo **route** và **đích** trong cấu hình; middleware ASP.NET Core thêm các hành vi riêng. Nó là "bộ dụng cụ để tự code gateway", **không** có sẵn portal, API key, quota.

```
Client ──► [gateway: rate limit, CORS, correlation] ──► board-service
                       │                          └──► identity-service (sau này)
                       └─ /health/live, /health/ready (tự trả lời)
```

## 3. Khái niệm cốt lõi (của YARP và thiết kế dự án)

| Khái niệm | Nghĩa | Ví dụ trong thiết kế |
|---|---|---|
| Route | Quy tắc khớp request (path, method) | `/api/v1/boards/{**catch-all}` |
| Cluster / destination | Nhóm đích mà route chuyển tới | board-service |
| Middleware | Bước xử lý theo thứ tự trên đường đi của request | ForwardedHeaders → Routing → Correlation → ExceptionHandler → CORS → RateLimiter → BodyLimit → YARP |
| Rate limit | Giới hạn số request theo khoá trong một khoảng thời gian | 120/phút đọc, 30/phút ghi, theo IP |
| Sliding window | Cửa sổ thời gian trượt, tránh dồn gấp đôi ở rìa cửa sổ | thuật toán đã chọn (D2) |
| CORS + preflight | Trình duyệt hỏi bằng `OPTIONS` trước khi cho gọi chéo origin | allowlist `http://localhost:4200` |
| Correlation id | Một id đi xuyên mọi service để lần ra cả chuỗi request | `X-Correlation-Id` + `traceparent` |
| `X-Forwarded-For` | Header ghi IP thật của client khi qua proxy | tin sai → né rate limit |
| `problem+json` | Định dạng lỗi JSON chuẩn (RFC 9457/7807) | `{code: "ROUTE_NOT_FOUND", traceId, ...}` |
| 502 / 503 / 504 | Gateway lỗi giao thức / không có đích / quá timeout | ánh xạ lỗi upstream |

## 4. Cách dùng

Khung tối thiểu của một gateway YARP (minh hoạ chung, **chưa phải code của dự án**; kiểm lại với tài liệu YARP phiên bản đang dùng):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

```jsonc
// appsettings.json
"ReverseProxy": {
  "Routes": {
    "boards-read": {
      "ClusterId": "board",
      "Match": { "Path": "/api/v1/boards/{**catch-all}", "Methods": ["GET", "HEAD"] }
    }
  },
  "Clusters": {
    "board": { "Destinations": { "d1": { "Address": "http://board-service:7070/" } } }
  }
}
```

Mong đợi: `GET http://localhost:<cổng gateway>/api/v1/boards` được chuyển nguyên path tới board-service. Route, đích, ngưỡng, origin nằm trong cấu hình (không secret), đổi file thì YARP nạp lại (design D3).

**Rate limit trong YARP** (agent đã đọc [trang rate limiting của YARP](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/rate-limiting), tôi chưa mở lại): YARP dùng middleware `RateLimiter` của ASP.NET Core; gán policy cho từng route qua `RateLimiterPolicy`; cần gọi `app.UseRateLimiter()` trước `app.MapReverseProxy()`.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Cùng công nghệ (.NET), tuỳ biến bằng middleware C# | Tự lo nhiều thứ mà dịch vụ managed có sẵn (WAF, HA, portal, API key, quota) |
| Chạy giống nhau local và ECS | Là điểm lỗi đơn và thêm một hop (mục tiêu dự án: thêm p95 < 20 ms) |
| Cấu hình nạp lại không cần restart | Dễ phình thành nơi chứa logic nghiệp vụ (phải giữ mỏng) |
| MIT, Microsoft duy trì | `verified: false` trong `config/dependencies.yaml` cho tới khi kiểm trang chính thức |

**Nên dùng khi:** stack .NET, cửa vào mỏng, cần tuỳ biến bằng code.
**Không cần khi:** chỉ một service (thêm gateway chỉ tăng độ phức tạp, design phương án C).

### Phạm vi và những gì công cụ này KHÔNG đại diện

YARP chỉ là **một lựa chọn** trong [bản đồ giải pháp API Gateway](api-gateway.md#bản-đồ-giải-pháp), thuộc họ "thư viện proxy tự code trong ngôn ngữ của bạn". Nó **không** đại diện cho cả khái niệm:

- **Không phải sản phẩm quản lý API.** Không có sẵn portal cho developer, API key, quota, analytics; Azure API Management hay AWS API Gateway có.
- **Không phải load balancer/WAF.** Dự án có thể đặt ALB phía trước ở giai đoạn 4.
- **Không phải service mesh.** Gateway lo lưu lượng bắc-nam (client vào); mesh (Istio, Linkerd) lo đông-tây giữa các service.
- **Khác Ocelot** (gateway .NET đời trước, cấu hình aggregation/rate limit sẵn, được Microsoft dùng cho eShopOnContainers 2.0): YARP là bộ dụng cụ cấp thấp hơn, tuỳ biến nhiều hơn (nhận định, Vừa).
- **Khác Envoy/NGINX/Kong/Traefik:** các proxy đa ngôn ngữ chạy như tiến trình riêng, nhiều tính năng và plugin; tách khỏi hệ .NET.
- **Không phải BFF.** BFF là mô hình (một backend riêng cho mỗi loại client); YARP có thể là nền để dựng nó, chỉ cần khi có client đặc thù.

## 6. Lỗi hay gặp và cách tránh

1. **Tin mọi `X-Forwarded-For`**: kẻ tấn công đổi IP mỗi request để né rate limit. Chỉ tin proxy đã biết; cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (design D5).
2. **Sai thứ tự middleware**: ví dụ rate limit trước CORS khiến preflight bị tính và `429` không có header CORS. Khoá thứ tự bằng test (design 6.4).
3. **Rate limit đăng ký không phân vùng**: thành một bộ đếm chung cho mọi IP. Dùng policy có khoá phân vùng theo IP (D1).
4. **Lỗi sau khi đã gửi header**: không đổi được status nữa, chỉ ngắt kết nối + log + metric (D4).
5. **Gateway nuốt/đổi lỗi mơ hồ**: client không phân biệt được lỗi do gateway hay do service; cần `code` ổn định và `correlationId` trong `problem+json`.
6. **Nhét logic nghiệp vụ vào gateway**: phá ranh giới; gateway chỉ routing, biên bảo vệ, quan sát.

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **Microsoft: YARP 1.0** (09/11/2021) | Nhiều đội Microsoft tự dựng reverse proxy và hỏi cùng một loại câu hỏi | Gom thành một thư viện reverse proxy trên ASP.NET Core; cấu hình route/cluster; mở rộng bằng middleware | Bài nêu Azure App Service dùng trực tiếp request forwarder của YARP để route tới instance khởi động theo yêu cầu | [Announcing YARP 1.0](https://devblogs.microsoft.com/dotnet/announcing-yarp-1-0-release/) | Chắc (đã mở) |
| **Microsoft: eShopOnContainers 2.0 (Ocelot)** | Ứng dụng tham chiếu microservice cần gateway .NET nhẹ, triển khai cùng chỗ với các container | Chọn Ocelot (gateway .NET mã nguồn mở, nhẹ) làm gateway cho bản 2.0; trang không nhắc YARP | Cho thấy trước YARP, Ocelot là lựa chọn .NET phổ biến trong tài liệu Microsoft | [Gateway pattern, .NET Microservices guide](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/direct-client-to-microservice-communication-versus-the-api-gateway-pattern) | Chắc (đã mở) |
| **ASP.NET Core: forwarded headers** | Sau proxy, IP client nằm trong `X-Forwarded-For` có thể bị giả | Middleware chỉ nên nhận header từ `KnownProxies`/`KnownNetworks`; `ForwardLimit` mặc định 1; đặt `null` mà thiếu hai danh sách đó thì cho phép giả mạo IP | Gốc của luật D5 ở mục 6 | [proxy-load-balancer](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer) | Vừa (agent đã đọc, tôi chưa mở lại) |
| **YARP + RateLimiter** | Cần giới hạn tốc độ theo route | Policy RateLimiter của ASP.NET Core gán theo route, đổi được khi chạy; cần .NET 7 trở lên | Là cách dự án định làm ở lát 4 | [YARP rate limiting](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/rate-limiting) | Vừa (như trên) |

Các case gateway nói chung (Zuul, Amazon API Gateway, Stripe, Uber, Kubernetes Gateway API) nằm ở [api-gateway.md](api-gateway.md) mục 7.

**Mô hình phổ biến (Chắc, theo Microsoft):** với đội .NET, YARP tự host hoặc Azure API Management (managed); Ocelot ở các tài liệu cũ hơn.

**Khi YARP/gateway là nguyên nhân sự cố:** retry khuếch đại và tin `X-Forwarded-For` sai (chi tiết, kèm nguồn, ở [api-gateway.md](api-gateway.md) mục 7). Dự án chủ ý chưa làm retry/circuit breaker ở gateway (design D4: không biết request nào idempotent).

## 8. Trong TeamBoard

| Nguồn | Nội dung |
|---|---|
| [spec/api-gateway.md](../spec/api-gateway.md) | Yêu cầu FR-001..FR-007, NFR (đã `confirmed`) |
| [design/api-gateway.md](../design/api-gateway.md) | Quyết định D0..D5, thứ tự middleware 6.4, chế độ lỗi, STRIDE |
| [plans/api-gateway-slices.md](../plans/api-gateway-slices.md) | 8 lát giao: 1 khung → 2 correlation → 3 lỗi upstream → 4 rate limit → 5 body → 6 CORS → 7 quan sát → 8 smoke thật |
| [TB-007](../issues/TB-007-api-gateway.md), [TB-010](../issues/TB-010-gateway-skeleton-routing.md) | Issue tài liệu / issue lát 1 (todo, chờ TB-001) |
| [ADR 0001](../architecture/adr/0001-service-boundaries.md), [ADR 0002](../architecture/adr/0002-communication.md) | Vì sao có gateway, client ↔ gateway ↔ service dùng REST |
| [patterns-catalog.md](../architecture/patterns-catalog.md) mục 3.5 | API Gateway trong danh mục pattern |

Quyết định đã chốt trong design: tự host YARP (không managed), host mỏng không Clean Architecture, rate limit trong bộ nhớ (sliding window, IPv6 gộp /64), route cấu hình tĩnh, timeout tổng 30 giây, chưa JWT/retry/circuit breaker, **không đưa ra internet trước khi có JWT**. Kho dùng chung (Redis/Valkey) được cân nhắc và loại vì chỉ 1 instance (`document/design/api-gateway.md:153`, xem [Redis / Valkey](valkey/README.md)).

**Điểm cần lưu ý khi gom:**
1. Gateway **chưa có code**; mọi thứ ở trên là thiết kế, không phải hành vi đã chạy. Ví dụ ở mục 4 là minh hoạ chung.
2. `document/architecture/README.md` mô tả gateway gồm "kiểm tra JWT", nhưng spec/design loại JWT khỏi phạm vi lát này (chờ identity): lệch có chủ ý, đừng đọc README như hiện trạng.
3. Design ghi một số hành vi YARP "chưa kiểm" (ánh xạ lỗi, `X-Forwarded-*` xuống upstream) và sẽ kiểm bằng test ở lát 3 và 2.
4. YARP ghi `verified: false` trong `config/dependencies.yaml`: phải kiểm trang chính thức trước khi dùng (ADR 0007). Giấy phép MIT đã thấy ở README repo (agent đã đọc).

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Xác minh |
|---|---|---|---|---|
| 1 | [api-gateway.md](api-gateway.md) | Nội bộ | khái niệm và bản đồ giải pháp | Chắc |
| 2 | [design/api-gateway.md](../design/api-gateway.md) mục 4–6 | Thiết kế | Kiến trúc, thứ tự middleware, hợp đồng lỗi | Chắc |
| 3 | [spec/api-gateway.md](../spec/api-gateway.md) | Spec | Yêu cầu và tiêu chí chấp nhận | Chắc |
| 4 | [plans/api-gateway-slices.md](../plans/api-gateway-slices.md) | Plan | Thứ tự làm | Chắc |
| 5 | [.agents/rules/api-endpoints.md](../../.agents/rules/api-endpoints.md) | Rule | Quy ước path, lỗi, CORS | Chắc |
| 6 | [YARP overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/yarp-overview) | Chính thức | YARP là gì, cách bắt đầu | Vừa |
| 7 | [YARP config files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/config-files) | Chính thức | Routes/Clusters | Vừa |
| 8 | [YARP rate limiting](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/rate-limiting) | Chính thức | Gắn policy theo route | Vừa |
| 9 | [YARP timeouts](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/timeouts) | Chính thức | Timeout route/cluster | Vừa |
| 10 | [Forwarded headers sau proxy](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer) | Chính thức | IP client, `KnownProxies` | Vừa |

(Link "Vừa": truy cập được, chưa đọc kỹ nội dung.)

## 10. Tự kiểm tra / thực hành

1. Vì sao CORS đứng trước rate limit trong pipeline? Đúng khi nêu: preflight không bị tính và `429` vẫn mang header CORS (design 6.4).
2. Vì sao không tin mọi `X-Forwarded-For`? Đúng khi nêu được cách né rate limit bằng đổi IP.
3. Vì sao khi upstream treo sau khi đã trả header thì không có `504`? Đúng khi nêu: status đã gửi, không đổi được.
4. Nêu hai việc một gateway managed (API Management, AWS API Gateway) làm được mà YARP không làm sẵn, và nói dự án xử lý chúng ra sao (hoặc chưa xử lý).
5. Việc nhỏ: đọc design mục 6.3 và liệt kê 7 mã lỗi `code` do gateway định nghĩa; đối chiếu với bảng ở đó.
