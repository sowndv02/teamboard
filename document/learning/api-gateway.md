# API Gateway (YARP)

> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: **mới có spec và design, chưa có code** (`src/` chưa có thư mục gateway; lát 1 là [TB-010](../issues/TB-010-gateway-skeleton-routing.md), chờ TB-001) · YARP: `verified: false` trong `config/dependencies.yaml`

## 1. Vấn đề nó giải quyết

Hệ thống có nhiều service (identity, board, task...). Nếu client gọi thẳng từng service thì: client phải biết địa chỉ của mọi service; mỗi service phải tự cài CORS, rate limit, correlation id, định dạng lỗi (lặp lại và dễ lệch nhau); và mọi service đều phải phơi ra internet.

## 2. Là gì (hiểu trong 1 phút)

**API Gateway** là cửa vào duy nhất: client chỉ nói chuyện với nó, nó chuyển request tới đúng service phía sau (**reverse proxy**: proxy đứng trước server, khác forward proxy đứng trước client) và làm các việc chung ở "biên" như giới hạn tốc độ, CORS, gắn id theo dõi.

**YARP** (Yet Another Reverse Proxy) là thư viện reverse proxy của Microsoft chạy ngay trong ASP.NET Core; bạn khai báo route và đích trong cấu hình, còn middleware ASP.NET Core thêm các hành vi riêng.

```
Client ──► [gateway: rate limit, CORS, correlation] ──► board-service
                       │                          └──► identity-service (sau này)
                       └─ /health/live, /health/ready (tự trả lời)
```

## 3. Khái niệm cốt lõi

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

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Một cửa vào: client không biết service nào ở đâu | Thêm một bước nhảy: tăng độ trễ (mục tiêu dự án: thêm p95 < 20 ms) |
| Việc chung (CORS, rate limit, correlation, lỗi) làm một lần | Là điểm lỗi đơn: gateway chết thì mất mọi API |
| Ẩn cấu trúc nội bộ, dễ đổi/tách service phía sau | Dễ phình thành nơi chứa logic nghiệp vụ (phải giữ mỏng) |
| Hợp để gom xác thực, quan sát ở biên | Phải vận hành, nâng cấp, bảo mật thêm một thành phần |
| YARP: một công nghệ (.NET), tuỳ biến bằng middleware, chạy giống nhau local và ECS | Tự lo nhiều thứ mà dịch vụ managed có sẵn (WAF, HA) |

**Nên dùng khi:** nhiều service cần cửa vào chung và chính sách biên thống nhất.
**Không cần khi:** chỉ một service (thêm gateway chỉ tăng độ phức tạp, design phương án C).

**Lựa chọn thay thế:**
- **AWS API Gateway / ALB**: managed, ít vận hành, có sẵn WAF/HA; khó giống hệt local và khó tuỳ biến correlation/`problem+json`, có chi phí. Dự án có thể đặt ALB **phía trước** YARP ở giai đoạn 4.
- **Envoy, Kong, NGINX, Traefik**: gateway/proxy đa ngôn ngữ, nhiều tính năng và plugin; tách khỏi hệ .NET.
- **Ocelot**: gateway .NET đời trước (slide của khoá học dùng); YARP là hướng mới của Microsoft.
- **BFF (Backend for Frontend)**: lớp backend riêng cho một giao diện, có thể tổng hợp dữ liệu; chỉ cần khi có client đặc thù.

## 6. Lỗi hay gặp và cách tránh

1. **Tin mọi `X-Forwarded-For`**: kẻ tấn công đổi IP mỗi request để né rate limit. Chỉ tin proxy đã biết; cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (design D5).
2. **Sai thứ tự middleware**: ví dụ rate limit trước CORS khiến preflight bị tính và `429` không có header CORS. Khoá thứ tự bằng test (design 6.4).
3. **Rate limit đăng ký không phân vùng**: thành một bộ đếm chung cho mọi IP. Dùng policy có khoá phân vùng theo IP (D1).
4. **Lỗi sau khi đã gửi header**: không đổi được status nữa, chỉ ngắt kết nối + log + metric (D4).
5. **Gateway nuốt/đổi lỗi mơ hồ**: client không phân biệt được lỗi do gateway hay do service; cần `code` ổn định và `correlationId` trong `problem+json`.
6. **Nhét logic nghiệp vụ vào gateway**: phá ranh giới; gateway chỉ routing, biên bảo vệ, quan sát.

## 7. Trong thực tế (real-world)

- **Mô hình phổ biến (Chắc, microservices.io):** pattern API Gateway là cửa vào của nhiều hệ microservice; thường làm routing, xác thực, rate limit, và đôi khi tổng hợp response; biến thể BFF cho từng loại client.
- **Tự host hay managed (Vừa):** trên AWS thường dùng API Gateway/ALB hoặc Envoy/Kong/NGINX; với đội .NET, YARP tự host là lựa chọn phổ biến (nhận định trong design D0.1, đã đối chiếu tài liệu YARP của Microsoft).
- **Rate limit (Chắc):** thường đếm cục bộ ở từng instance proxy (xấp xỉ), chỉ dùng kho chung như Redis khi cần chính xác tuyệt đối; nhiều instance thì giới hạn thực ≈ N lần ngưỡng.
- **Dễ sai ở production (Vừa):** gateway thành điểm lỗi đơn không ai theo dõi; không đặt timeout nên một service chậm kéo cạn kết nối; retry ở gateway cộng dồn với retry của service gọi nhau làm nhân tải khi sự cố; đưa ra internet khi chưa có xác thực.
- **Retry/circuit breaker:** dự án chủ ý chưa làm ở gateway (không biết request nào idempotent; xem design D4).

## 8. Trong TeamBoard

| Nguồn | Nội dung |
|---|---|
| [spec/api-gateway.md](../spec/api-gateway.md) | Yêu cầu FR-001..FR-007, NFR (đã `confirmed`) |
| [design/api-gateway.md](../design/api-gateway.md) | Quyết định D0..D5, thứ tự middleware 6.4, chế độ lỗi, STRIDE |
| [plans/api-gateway-slices.md](../plans/api-gateway-slices.md) | 8 lát giao: 1 khung → 2 correlation → 3 lỗi upstream → 4 rate limit → 5 body → 6 CORS → 7 quan sát → 8 smoke thật |
| [TB-007](../issues/TB-007-api-gateway.md), [TB-010](../issues/TB-010-gateway-skeleton-routing.md) | Issue tài liệu / issue lát 1 (todo, chờ TB-001) |
| [ADR 0001](../architecture/adr/0001-service-boundaries.md), [ADR 0002](../architecture/adr/0002-communication.md) | Vì sao có gateway, client ↔ gateway ↔ service dùng REST |
| [patterns-catalog.md](../architecture/patterns-catalog.md) mục 3.5 | API Gateway trong danh mục pattern |

Quyết định đã chốt trong design: tự host YARP (không managed), host mỏng không Clean Architecture, rate limit trong bộ nhớ (sliding window, IPv6 gộp /64), route cấu hình tĩnh, timeout tổng 30 giây, chưa JWT/retry/circuit breaker, **không đưa ra internet trước khi có JWT**.

**Điểm cần lưu ý khi gom:**
1. Gateway **chưa có code**; mọi thứ ở trên là thiết kế, không phải hành vi đã chạy. Ví dụ ở mục 4 là minh hoạ chung.
2. `document/architecture/README.md` mô tả gateway gồm "kiểm tra JWT", nhưng spec/design loại JWT khỏi phạm vi lát này (chờ identity): lệch có chủ ý, đừng đọc README như hiện trạng.
3. Design ghi một số hành vi YARP "chưa kiểm" (ánh xạ lỗi, `X-Forwarded-*` xuống upstream) và sẽ kiểm bằng test ở lát 3 và 2.
4. YARP ghi `verified: false` trong `config/dependencies.yaml`: phải kiểm trang chính thức trước khi dùng (ADR 0007).

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Xác minh |
|---|---|---|---|---|
| 1 | [design/api-gateway.md](../design/api-gateway.md) mục 4–6 | Thiết kế | Kiến trúc, thứ tự middleware, hợp đồng lỗi | Chắc |
| 2 | [spec/api-gateway.md](../spec/api-gateway.md) | Spec | Yêu cầu và tiêu chí chấp nhận | Chắc |
| 3 | [plans/api-gateway-slices.md](../plans/api-gateway-slices.md) | Plan | Thứ tự làm | Chắc |
| 4 | [.agents/rules/api-endpoints.md](../../.agents/rules/api-endpoints.md) | Rule | Quy ước path, lỗi, CORS | Chắc |
| 5 | [YARP overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/yarp-overview) | Chính thức | YARP là gì, cách bắt đầu | Vừa |
| 6 | [YARP config files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/config-files) | Chính thức | Routes/Clusters | Vừa |
| 7 | [YARP rate limiting](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/rate-limiting) | Chính thức | Gắn policy theo route | Vừa |
| 8 | [YARP timeouts](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/timeouts) | Chính thức | Timeout route/cluster | Vừa |
| 9 | [Forwarded headers sau proxy](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer) | Chính thức | IP client, `KnownProxies` | Vừa |
| 10 | [microservices.io: API Gateway](https://microservices.io/patterns/apigateway.html) | Pattern | Ưu/nhược pattern | Vừa |

(Link "Vừa": truy cập được, chưa đọc kỹ nội dung.)

## 10. Tự kiểm tra / thực hành

1. Gateway khác load balancer ở điểm nào? Đúng khi nêu: gateway hiểu ứng dụng (route theo path, rate limit, CORS), load balancer chủ yếu chia tải.
2. Vì sao CORS đứng trước rate limit trong pipeline? Đúng khi nêu: preflight không bị tính và `429` vẫn mang header CORS (design 6.4).
3. Vì sao không tin mọi `X-Forwarded-For`? Đúng khi nêu được cách né rate limit bằng đổi IP.
4. Vì sao khi upstream treo sau khi đã trả header thì không có `504`? Đúng khi nêu: status đã gửi, không đổi được.
5. Việc nhỏ: đọc design mục 6.3 và liệt kê 7 mã lỗi `code` do gateway định nghĩa; đối chiếu với bảng ở đó.
