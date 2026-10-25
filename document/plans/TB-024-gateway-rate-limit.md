---
title: Gateway lát 4, rate limit theo IP và 429 problem+json
issue: TB-024
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-024 — Gateway lát 4, rate limit theo IP và 429 problem+json

Lát 4 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`; phụ thuộc lát 1, 2 đã merge; lát 3 cũng đã merge). Làn lớn: spec và design `confirmed` ở TB-007; chạm bảo mật (xác định IP client, né giới hạn) và hợp đồng lỗi công khai. Ảnh chụp trạng thái branch: `feature/TB-024-gateway-rate-limit`, tạo từ `main` `feb4278` (đã gồm TB-010, TB-022, TB-023; PR 16 đã merge); HEAD lúc plan-review `6c90fe0` (đã chứa plan draft và đồng bộ lát 3), working tree sạch.

Đã có (đã đọc code): `src/gateway/Program.cs` (pipeline `UseCorrelation` → `UseTeamboardExceptionHandler` → `UseRequestTimeouts` → `UpstreamErrorMiddleware` → `MapDefaultEndpoints` → `MapReverseProxy` → `MapFallback`; chưa có `ForwardedHeaders` và `RateLimiter`), `appsettings.json` (hai route `boards-read` GET/HEAD và `boards-write` POST/PUT/PATCH/DELETE với `Timeout` 30 giây, cluster `board-service`), `GatewayErrorCodes` (chưa có `RATE_LIMIT_EXCEEDED`), `UpstreamOptions` (mẫu Options có `ValidateOnStart`), `src/gateway.Tests` (`GatewayContext` dựng `WebApplicationFactory` (TestServer) với `Settings`, `ConfigureTestServices`, bắt log và `OuterResponseHeader`; `UpstreamStub` Kestrel ghi header nhận được; `DockerCli`; `RepoRoot`). Hiện không có giới hạn nào theo IP.

**Kết quả spike của reviewer độc lập (Yarp 2.3.0, .NET 10.0.402, Mvc.Testing 10.0.13; thực nghiệm trên bản sao gateway; kiểm lại ở test-init trên mã repo):**

| Điều đã thử | Kết quả |
|---|---|
| `RateLimiterPolicy` ở route YARP | Health và fallback (không gắn policy) không bị giới hạn |
| `MetadataName.RetryAfter` của lease `SlidingWindow` | **Không có** (`TryGetMetadata` trả `false`); `FixedWindow` có. `Retry-After` phải lấy từ độ dài cửa sổ |
| Số đoạn `SegmentsPerWindow` 1, 2, 6 với ngưỡng 3 | Cùng kết quả 200, 200, 200, 429: số đoạn không đổi ngữ nghĩa "N/phút", chỉ ảnh hưởng burst ở rìa cửa sổ; trong một cửa sổ 60 giây kết quả xác định |
| `GET` và `HEAD` | Dùng chung bộ đếm đọc |
| YARP mặc định với `X-Forwarded-For/Proto` | **Thay (Set)** theo giá trị gateway thấy; `Forwarded` và `X-Forwarded-Prefix` bị bỏ; **ngoại lệ**: `X-Forwarded-Host` bằng đúng `Host` client gửi |
| `UseForwardedHeaders()` không cấu hình | No-op vì `ForwardedHeaders` mặc định là `None`; phải bật cờ |
| Nguồn không tin cậy gửi `X-Forwarded-For` | Bị bỏ qua, IP giữ là địa chỉ kết nối; đổi `X-Forwarded-For` mỗi request vẫn bị đếm chung |
| Nguồn loopback (`127.0.0.1`, `::1`, `::ffff:127.0.0.1`) hoặc `RemoteIpAddress` null (mặc định TestServer) | Được coi là tin cậy: IP lấy từ `X-Forwarded-For` |
| `ForwardedHeadersOptions.KnownNetworks` | Obsolete (`ASPDEPR005`) trên .NET 10: dùng `KnownIPNetworks`; bind từ cấu hình phải tự làm |
| `UseRouting` ngầm của minimal hosting | Chạy **trước** mọi middleware (lệch design 6.4); gọi tường minh `UseForwardedHeaders()` rồi `UseRouting()` thì bản ngầm bị bỏ qua và route vẫn đúng |
| Thứ tự `UseRateLimiter` sau `UpstreamError`, trước YARP | `429` do limiter ghi trực tiếp, có `problem+json`, `X-Correlation-Id` bằng `correlationId` trong thân và header của middleware ngoài |

## 1. Phạm vi của lát

**Làm (FR-004; design D1, D2, D5, 6.4):**
- `ForwardedHeaders` an toàn ở **đầu pipeline** (design D5, 6.4): gọi tường minh `app.UseForwardedHeaders()` rồi `app.UseRouting()` để giữ thứ tự design (người dùng chốt); chỉ xử lý cờ `XForwardedFor | XForwardedProto` (người dùng chốt); chỉ tin proxy trong danh sách cấu hình (`KnownProxies` và `KnownIPNetworks`, bind từ cấu hình; mặc định giữ loopback của middleware, không `Clear()`); `ForwardLimit` mặc định của middleware; không đặt `ASPNETCORE_FORWARDEDHEADERS_ENABLED`. Địa chỉ proxy hoặc mạng sai làm khởi động thất bại.
- Rate limit `sliding window` theo khoá `IP client + nhóm route` (D2): policy `read` (route `boards-read`, gồm `GET` và `HEAD`) mặc định 120 request/phút/IP và policy `write` (route `boards-write`) mặc định 30 request/phút/IP; cửa sổ 60 giây, **6 đoạn** (người dùng chốt), hàng đợi bằng 0 (từ chối ngay; FR-004 "request thứ 31 nhận 429"); ngưỡng, cửa sổ và số đoạn đọc từ cấu hình có kiểm tra giá trị (NFR-004); gắn vào route bằng `RateLimiterPolicy` của YARP.
- Khoá IP: IPv4 theo địa chỉ đầy đủ; **IPv6 gộp theo /64** (design D2); địa chỉ IPv4-mapped IPv6 chuẩn hoá về IPv4 (một client không có hai bộ đếm; FR-004 và D2 đã đủ đáp án); `ScopeId` không lọt vào khoá; `RemoteIpAddress` null dùng **một bộ đếm chung `unknown`** (người dùng chốt; không xảy ra trên Kestrel thật, là mặc định của TestServer); hàm tính khoá là hàm thuần, test riêng.
- `429 problem+json` mã `RATE_LIMIT_EXCEEDED` kèm `Retry-After` bằng đúng độ dài cửa sổ cấu hình (giây; spike: metadata của lease `SlidingWindow` không có `RetryAfter`, nên không có nhánh đọc metadata; giá trị bảo thủ vì quota thực tế hồi sớm hơn) và `X-Correlation-Id`, không lộ IP hay khoá trong thân (`OnRejected` dùng `ProblemFactory`, `RejectionStatusCode = 429`); không xoá header do middleware khác đặt.
- Health (`/health/live`, `/health/ready`) và fallback `404` không bị giới hạn (không gắn policy; spike xác nhận).
- `X-Forwarded-For/Proto` xuống upstream do gateway xác định: spike cho thấy mặc định của YARP đã **Set** theo giá trị gateway thấy (không nối giá trị giả của client), nên lát này chỉ thêm test đặc trưng, không thêm transform. `X-Forwarded-Host` bằng `Host` client gửi (`AllowedHosts` là `*`): chấp nhận, ghi giới hạn đã biết (người dùng chốt).
- Test cấu hình: ngưỡng mặc định 120/30 trong cấu hình thật qua một kịch bản dùng đúng cấu hình đóng gói (không ghi đè ngưỡng); `ASPNETCORE_FORWARDEDHEADERS_ENABLED` không xuất hiện trong các file compose (D5).

**Không làm (trong lát này):** CORS và `OPTIONS` (lát 6: preflight không tính vào rate limit, sẽ kiểm ở đó), giới hạn body (lát 5), metric đếm `429` và mọi metric/log retention (lát 7), kho đếm dùng chung nhiều instance (D1: đếm trong bộ nhớ, ghi nợ), rate limit riêng cho `identity-service` (identity lát 3, TB-031), danh sách proxy tin cậy của ALB (giai đoạn 4), kiểm qua compose với IP thật (Docker Desktop NAT; D5 chọn test với `RemoteIpAddress` giả), chặn `X-Forwarded-Host` hay đặt `AllowedHosts` cụ thể, thay đổi `board-service`.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-004 request ghi thứ 31 trong phút nhận `429` kèm `Retry-After` | D1, D2 | B1 | @S01 | Host test với ngưỡng nhỏ ghi đè bằng cấu hình (ví dụ 3/phút): request thứ 4 trả 429, `code` `RATE_LIMIT_EXCEEDED`, `Retry-After` bằng độ dài cửa sổ đã cấu hình, `X-Correlation-Id` bằng `correlationId` trong thân, header của middleware ngoài còn nguyên, không lộ IP |
| FR-004 `GET` thứ 121 nhận `429`, thứ 120 vẫn thành công; `HEAD` tính chung với đọc | D2, DA-05 | B2 | @S02 | Host test ngưỡng đọc nhỏ: request cuối trong ngưỡng 200, kế tiếp 429; `GET` và `HEAD` dùng chung bộ đếm |
| FR-004 IP khác, nhóm khác, health, fallback không bị ảnh hưởng | D2, D1, 6.4 | B2 | @S03 (Outline) | Sau khi hết hạn mức một IP và một nhóm: IP khác vẫn qua, nhóm kia (đọc ↔ ghi) vẫn qua, `/health/live`, `/health/ready`, đường dẫn lạ vẫn 200/404 |
| FR-004 hai địa chỉ IPv6 cùng /64 dùng chung bộ đếm; IPv4 đủ địa chỉ | D2 | B2 | @S04 + unit | Unit hàm khoá (IPv4, IPv6 /64, IPv4-mapped, `ScopeId`, null → `unknown`, biên /64); host test hai địa chỉ cùng /64 chung hạn mức, hai /64 khác nhau tách |
| Spec/D5 giả `X-Forwarded-For` để né rate limit | D5 | B3 | @S05 | Nguồn không tin cậy gửi `X-Forwarded-For` khác nhau mỗi request: vẫn bị đếm chung theo địa chỉ kết nối |
| D5 chỉ tin proxy trong danh sách cấu hình | D5 | B3 | @S06 | Proxy tin cậy (cấu hình): khoá theo IP client trong header, hai client khác nhau không chung hạn mức |
| D5 `X-Forwarded-For/Proto` xuống upstream do gateway xác định | D5 | B3 | @S07 | Upstream giả ghi header nhận được: không chứa giá trị giả của client (test đặc trưng, có thể xanh sẵn) |
| FR-004 ngưỡng mặc định 120/30, NFR-004 trong cấu hình | D2 | B4 | @S08 | Kịch bản dùng đúng cấu hình đóng gói (không ghi đè ngưỡng): ghi thứ 30 qua, thứ 31 là 429; đọc thứ 120 qua, thứ 121 là 429 |
| NFR-004 giá trị cấu hình hợp lệ, proxy tin cậy hợp lệ | D2, D5 | B1, B3, B4 | @S09 (xUnit) | `RateLimitOptions` và cấu hình proxy: giá trị ngoài khoảng, địa chỉ hay mạng sai làm khởi động thất bại; ngưỡng mặc định trong `appsettings.json` |
| D5 cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | D5 | B4 | @S10 (xUnit) | Test đọc các file compose (`src/docker-compose*.yml`), `.env.example`: biến không có mặt |
| Thứ tự middleware khoá bằng test (design 6.4) | 6.4 | B3 | @S05, @S06, @S01 (kèm) | `ForwardedHeaders` chạy trước limiter (S05, S06); `Correlation` bao ngoài (S01) |

Kịch bản (ID duy nhất trong file `.feature`; viết ở test-init; viết tiếng Anh, ASCII):
- @S01 Ghi quá hạn mức: `429` `RATE_LIMIT_EXCEEDED` kèm `Retry-After` và `X-Correlation-Id`.
- @S02 Đọc quá hạn mức (`GET` và `HEAD` chung bộ đếm): request cuối trong ngưỡng thành công, kế tiếp `429`.
- @S03 (Outline) IP khác, nhóm route khác, health và fallback không bị ảnh hưởng khi một IP hết hạn mức.
- @S04 Hai địa chỉ IPv6 cùng /64 dùng chung hạn mức.
- @S05 `X-Forwarded-For` từ nguồn không tin cậy không né được giới hạn.
- @S06 Proxy tin cậy: IP client lấy từ `X-Forwarded-For`.
- @S07 Upstream nhận `X-Forwarded-For` và `X-Forwarded-Proto` do gateway xác định.
- @S08 Ngưỡng mặc định đóng gói: ghi 30 qua 31 chặn, đọc 120 qua 121 chặn.
- @S09 (xUnit, không có `.feature`) Cấu hình hợp lệ: khoảng giá trị, proxy và mạng tin cậy, mặc định trong `appsettings.json`.
- @S10 (xUnit, không có `.feature`) Compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng (stub chỉ chuyển tiếp, không hành vi) chỉ để test biên dịch được. **Spike ở test-init** kiểm lại kết quả của reviewer trên mã repo. Test không dùng `Thread.Sleep` hay chờ đồng hồ thật để qua cửa sổ: dùng ngưỡng nhỏ qua cấu hình (design D2) và chỉ kiểm hành vi trong một cửa sổ; không ghi đè `WindowSeconds` xuống dưới 30 giây (cửa sổ ngắn mới làm test flaky).

### B1. Policy `write` và `429 problem+json` (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/rate-limit.feature` @S01; unit `RateLimitOptions` (mặc định 120/30, cửa sổ 60 giây, 6 đoạn, giá trị ngoài khoảng làm khởi động thất bại); `GatewayErrorCodesTests` (mã `RATE_LIMIT_EXCEEDED` 429, message không lộ nội bộ); `GatewayContext` thêm cách đặt IP giả cho test (middleware `IStartupFilter` đặt `RemoteIpAddress` từ header test, chạy trước mọi middleware của gateway). Đỏ vì chưa có rate limiter.
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs` (mã mới), mới `src/gateway/RateLimiting/RateLimitOptions.cs` (`RateLimiting:Read` và `RateLimiting:Write`: `PermitLimit`, `WindowSeconds`, `SegmentsPerWindow`; `ValidateOnStart`), mới `src/gateway/RateLimiting/RateLimitingExtensions.cs` (`AddGatewayRateLimiting`, policy `write`, `OnRejected` → `ProblemFactory` + `Retry-After` = `WindowSeconds`, không `Response.Clear()`), `src/gateway/appsettings.json` (ngưỡng và `RateLimiterPolicy` ở route `boards-write`), `src/gateway/Program.cs` (`UseRateLimiter` sau `UpstreamError`, trước YARP), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `429` `problem+json` công khai (`RATE_LIMIT_EXCEEDED`) kèm `Retry-After` (design 6.3, DA-02).
- **Migration / cấu hình:** khoá `RateLimiting:Read|Write:*` trong `appsettings.json` (mặc định 120 và 30 mỗi 60 giây, 6 đoạn); không biến môi trường mới.
- **Tài liệu cần cập nhật:** không thêm.
- **Kiểm chứng:** @S01 xanh (gồm assertion header của middleware ngoài còn nguyên); test lát 1–3 vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Policy `read`, hàm khoá IP, nhóm route và health
- **Kịch bản/test sẽ viết (đỏ trước):** @S02, @S03, @S04 trong `rate-limit.feature`; unit `ClientPartitionKeyTests` (IPv4 nguyên địa chỉ, IPv6 cùng /64 cùng khoá, khác /64 khác khoá, IPv4-mapped IPv6 → khoá như IPv4, `ScopeId` không lọt vào khoá, null → `unknown`, biên /64). Đỏ vì chưa có policy `read` và khoá IPv6.
- **File/module dự kiến chạm:** mới `src/gateway/RateLimiting/ClientPartitionKey.cs` (hàm thuần: IP → khoá), `src/gateway/RateLimiting/RateLimitingExtensions.cs` (policy `read`, dùng hàm khoá cho cả hai policy), `src/gateway/appsettings.json` (route `boards-read` dùng policy `read`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không thêm (cùng `429`).
- **Migration / cấu hình:** không thêm.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** @S02–@S04 xanh; unit khoá IP xanh; chạy lặp 10 lần nhóm host test của `gateway.Tests` (kiểm không flaky).
- **Phụ thuộc:** B1.

### B3. `ForwardedHeaders` an toàn và kiểm `X-Forwarded-*` xuống upstream
- **Kịch bản/test sẽ viết (đỏ trước):** @S05, @S06, @S07 trong `rate-limit.feature` (hoặc `forwarded-headers.feature` nếu tách capability; quyết ở test-init theo `test-review.md` mục 1); `UpstreamStub` ghi thêm `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`; test cấu hình proxy hợp lệ/sai. Đỏ thật: @S06 (cần cờ và danh sách proxy). @S05 có thể xanh sẵn khi `ForwardedHeaders` chưa bật (IP luôn là địa chỉ kết nối) và @S07 xanh sẵn nhờ mặc định của YARP: ghi vào TDD log là test đặc trưng/hồi quy, không bịa lý do đỏ.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`UseForwardedHeaders()` rồi `UseRouting()` tường minh, comment cập nhật), mới `src/gateway/ForwardedHeadersSetup.cs` (hoặc tương đương: bật `XForwardedFor | XForwardedProto`; bind `ForwardedHeaders:KnownProxies` bằng `IPAddress.Parse` và `ForwardedHeaders:KnownIPNetworks` bằng `System.Net.IPNetwork.Parse`; không dùng `KnownNetworks` obsolete; giữ loopback mặc định), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không đổi hợp đồng công khai.
- **Migration / cấu hình:** khoá `ForwardedHeaders:KnownProxies` và `ForwardedHeaders:KnownIPNetworks` (mặc định rỗng: chỉ loopback); không biến môi trường mới và không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
- **Tài liệu cần cập nhật:** comment "Target middleware order" trong `Program.cs`; design 6.4 không đổi (giữ ForwardedHeaders đầu tiên).
- **Kiểm chứng:** @S05–@S07 xanh; @S01–@S04 vẫn xanh.
- **Phụ thuộc:** B2.

### B4. Cấu hình thật, chính sách compose và đóng lát
- **Kịch bản/test sẽ viết (đỏ trước):** @S08 trong `rate-limit.feature` (dùng đúng cấu hình đóng gói, không ghi đè ngưỡng); `ConfigurationTests` mở rộng (@S09: ngưỡng mặc định và `RateLimiterPolicy` ở đúng route); test chính sách compose (@S10: các file `src/docker-compose*.yml` và `.env.example` không chứa `ASPNETCORE_FORWARDEDHEADERS_ENABLED`; có thể xanh sẵn: ghi vào TDD log là test hồi quy).
- **File/module dự kiến chạm:** `src/gateway.Tests/`, `COMMANDS.md` (không đổi nếu không có test docker mới).
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** bảng lát và `document/issues/README.md` theo LL-006 (bảng lát có trạng thái chi tiết hơn issue: lát 4 là `planning` ở plan-init → `in-progress` khi bắt đầu coding → `review` khi mở PR → `done` sau merge; issue và README issue là `in-progress` từ lúc bắt đầu rồi `review` và `done` cùng thời điểm với bảng lát); giới hạn đã biết ghi ở issue (đếm trong bộ nhớ, `X-Forwarded-Host`).
- **Kiểm chứng:** B4 xanh; toàn bộ test không docker của solution xanh; nhóm docker từng assembly vẫn xanh nếu Docker bật; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B1–B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Hàm khoá IP (IPv4, IPv6 /64, IPv4-mapped, `ScopeId`, null); `RateLimitOptions`; cấu hình proxy; mã lỗi | xUnit thuần | Không mock |
| Integration (host in-process) | 429, `Retry-After`, nhóm route, health, `ForwardedHeaders`, `X-Forwarded-*` xuống upstream (`gateway.Tests`) | `WebApplicationFactory<Program>` (TestServer) + `UpstreamStub`; ngưỡng nhỏ qua cấu hình, cửa sổ không dưới 30 giây; `RemoteIpAddress` giả bằng `IStartupFilter`; một kịch bản dùng cấu hình đóng gói (TestServer chạy vài trăm request trong mili giây) | Không mock rate limiter hay YARP; không chờ qua cửa sổ thật |
| Test chính sách | Compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`; ngưỡng mặc định | Đọc file trong repo | Không cần Docker |

Không có test Docker mới trong lát này (design D5: không kiểm IP end-to-end qua compose do NAT của Docker Desktop). Kim tự tháp: nhiều unit và host test nhanh. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`). Test BDD viết tiếng Anh, ASCII.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| `OnRejected` và `RateLimiterPolicy` khác giả định ở phần chưa spike (ví dụ đọc sau khi ghi bão hoà, đồng thời) | 429 sai dạng | Spike của reviewer đã kiểm phần chính; test-init kiểm lại; design D1 đã ghi "kiểm bằng test khi cài" | Revert B1 |
| Test flaky khi dùng cửa sổ ngắn | Test đỏ oan | Không ghi đè `WindowSeconds` dưới 30 giây; ngưỡng nhỏ thay vì chờ; chạy lặp 10 lần | Sửa test, không nới điều kiện |
| `UseRouting` tường minh đổi hành vi endpoint của các lát trước | Hồi quy route | Gọi `UseForwardedHeaders()` rồi `UseRouting()` (spike: route vẫn đúng); test lát 1–3 phải xanh | Revert B3 |
| Cấu hình `KnownProxies`/`KnownIPNetworks` sai hoặc bật mọi proxy làm kẻ tấn công né giới hạn | Mất tác dụng rate limit | Mặc định chỉ loopback, không `Clear()`; test @S05; test compose cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED`; địa chỉ sai làm khởi động thất bại | Revert B3 |
| Nguồn loopback hoặc null được tin cậy mặc định của `ForwardedHeaders` | Client cục bộ (cùng host) giả IP | Chấp nhận (nguồn cục bộ là chính proxy cùng máy); test S05 đặt remote không phải loopback tường minh; không viết "null thì bỏ qua XFF" | Không áp dụng |
| Dưới Docker Desktop mọi client cục bộ có cùng IP cầu nối | Thấy 429 sớm khi thử tay | Ghi chú trong tài liệu chạy local; test dùng `RemoteIpAddress` giả | Không áp dụng |
| Bộ đếm trong bộ nhớ: mất khi khởi động lại, lỏng N lần khi N instance | Giới hạn thực lỏng hơn | Chấp nhận (design D1), ghi vào giới hạn đã biết; xem lại khi >1 instance | Không áp dụng |
| `X-Forwarded-Host` bằng `Host` client gửi (`AllowedHosts` là `*`) | Upstream có thể thấy host giả | Chấp nhận, ghi giới hạn đã biết (người dùng chốt) | Không áp dụng |

Ảnh hưởng consumer (service khác): thêm `429` `problem+json` và `Retry-After` ở contract công khai của gateway (cộng thêm); client Angular chưa tồn tại (ADR 0008); `board-service` nhận `X-Forwarded-For/Proto` do gateway xác định (YARP đã làm vậy mặc định; thêm test). Bảo mật (STRIDE design mục 8): giả `X-Forwarded-For` (chỉ tin proxy trong cấu hình), xoay địa chỉ IPv6 (gộp /64), DoS bằng spam (giới hạn theo IP; DDoS lớn thuộc WAF/ALB, giai đoạn 4); `429` không lộ IP hay khoá; không secret mới. Chi phí: không tài nguyên mới; không test Docker mới.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `security`, `observability`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát và issue) áp dụng ở B4 và đã áp dụng lúc tạo nhánh (đồng bộ lát 3 → `done`); LL-005 (câu hỏi mở kèm lựa chọn và khuyến nghị) áp dụng cho mục 7; từ TB-023: kiểm test log chỉ trên logger của gateway, và bỏ `Response.Clear()` để không xoá header của middleware khác (áp dụng cho `OnRejected`).

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"` rồi nhóm `docker` từng assembly nếu Docker bật; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ).
- Tiêu chí hoàn thành: theo issue TB-024.
- Tài liệu/config cập nhật: như B1–B4; bảng lát và `document/issues/README.md` (LL-006).

## 7. Câu hỏi mở và giả định

Giả định đã kiểm bằng spike hộp đen của reviewer (xem bảng đầu plan; kiểm lại ở test-init trên mã repo, sai thì dừng và hỏi): A-01 health và fallback không gắn policy nên không bị giới hạn (đúng); A-02 metadata `RetryAfter` của lease `SlidingWindow` (**sai**: dùng `WindowSeconds`); A-03 YARP mặc định Set `X-Forwarded-For/Proto` (đúng; ngoại lệ `X-Forwarded-Host`); A-04 `ForwardedHeaders` bỏ qua `X-Forwarded-For` từ nguồn không tin cậy (đúng nhưng phải bật cờ; null và loopback được tin cậy); A-05 hàng đợi 0 từ chối ngay (đúng); A-06 `UseRateLimiter` sau `UpstreamError`, trước YARP (đúng). Chưa kiểm: Kestrel dual-stack có trả IPv4-mapped IPv6 hay không (test dùng `RemoteIpAddress` giả), đồng thời nhiều request vào cùng một khoá, hồi quota theo thời gian thực, `X-Real-IP` xuống upstream.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Số đoạn của cửa sổ trượt | B1 | sondv | answered: 6 đoạn (10 giây) (2026-10-25) |
| P-02 | Địa chỉ IPv4-mapped IPv6 | B2 | – | answered bởi FR-004 và D2 (một client không có hai bộ đếm): chuẩn hoá về IPv4 |
| P-03 | Phạm vi `X-Forwarded-*` xuống upstream | B3 | sondv | answered: nằm trong lát; YARP mặc định đã Set, chỉ thêm test đặc trưng; `X-Forwarded-Host` chấp nhận (2026-10-25) |
| P-04 | `RemoteIpAddress` null | B2 | sondv | answered: bộ đếm chung `unknown` (2026-10-25) |
| P-05 | Xếp hàng hay từ chối ngay | B1 | – | answered bởi FR-004 ("request thứ 31 nhận 429"): từ chối ngay, hàng đợi 0 |
| P-06 | Cờ `ForwardedHeaders` được xử lý | B3 | sondv | answered: `XForwardedFor` và `XForwardedProto` (2026-10-25) |
| P-07 | Thứ tự `UseRouting` và `ForwardedHeaders` | B3, design 6.4 | sondv | answered: gọi tường minh `UseForwardedHeaders()` rồi `UseRouting()`, giữ design (2026-10-25) |
| P-08 | Cỡ lát (8 kịch bản `.feature` và 2 xUnit, 4 bước) | cả plan | sondv | answered: giữ một lát, đã gộp kịch bản (2026-10-25) |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-024:forwarded-flags | Cờ `ForwardedHeaders` được xử lý | `XForwardedFor` và `XForwardedProto` | 1 | 2026-10-25 |
| 2 | plan-review:TB-024:routing-order | `UseRouting` ngầm chạy trước `ForwardedHeaders` | Gọi tường minh `UseForwardedHeaders()` rồi `UseRouting()`, giữ design 6.4 | 1 | 2026-10-25 |
| 3 | plan-review:TB-024:slice-size | Giữ hay chia lát | Giữ một lát, gộp kịch bản | 1 | 2026-10-25 |
| 4 | plan-review:TB-024:forwarded-host | `X-Forwarded-Host` bằng `Host` client gửi | Chấp nhận, ghi giới hạn đã biết | 1 | 2026-10-25 |
| 5 | plan-review:TB-024:null-remote | `RemoteIpAddress` null | Bộ đếm chung `unknown` | 1 | 2026-10-25 |
| 6 | plan-review:TB-024:segments-default | Số đoạn của cửa sổ trượt | 6 đoạn (10 giây) | 1 | 2026-10-25 |

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-25 | Toàn plan, đủ A0, A–H (reviewer độc lập kèm spike YARP và ASP.NET Core) | 6 → đã chốt (+2 đã có đáp án trong spec/design) | 8 → đã sửa | NEEDS_CONFIRM → PASS sau khi chốt và sửa |

### Fix log vòng 1

| Fingerprint | Vị trí | Vấn đề | Đã sửa thế nào |
|---|---|---|---|
| knownnetworks-obsolete | B3, mục 1 | `KnownNetworks` obsolete (`ASPDEPR005`) làm build đỏ với `-warnaserror` | Dùng `KnownIPNetworks`, bind tự làm từ cấu hình, địa chỉ hay mạng sai làm khởi động thất bại, không `Clear()` loopback |
| retry-after-metadata | A-02, B1, mục 1 | Lease `SlidingWindow` không có `RetryAfter` | `Retry-After` = `WindowSeconds`, bỏ nhánh đọc metadata; S01 so với giá trị cấu hình |
| xff-default-set | A-03, B3, P-03, S07 | YARP mặc định đã Set `X-Forwarded-For/Proto` | Không thêm transform; S07 là test đặc trưng xanh sẵn; phần đỏ thật của B3 là @S06 |
| p02-p05-answered | P-02, P-05 | Đã có đáp án trong FR-004 và D2 | Đóng, ghi vào mục 1; thêm `ScopeId` vào test biên hàm khoá |
| literal-fr004-scenario | S08, truy vết FR-004 | Test đọc JSON không chứng minh binding cấu hình | @S08: kịch bản dùng đúng cấu hình đóng gói, ghi 30/31 và đọc 120/121 |
| outer-header-test | B1, S01 | Không kiểm `OnRejected` giữ header của middleware ngoài | Thêm assertion với `OuterResponseHeader` ở @S01 |
| flaky-claim | mục 5, B2 | Nêu sai nguyên nhân flaky (ranh giới đoạn) | Nêu đúng: chỉ khi cửa sổ ngắn; cấm ghi đè `WindowSeconds` dưới 30 giây |
| formal-sync | đầu plan, bảng lát, issue, README | Mô tả trạng thái sai; lát 4 `planning` còn issue `in-progress` | Sửa mô tả; làm rõ quan hệ trạng thái bảng lát (chi tiết hơn) với issue/README theo LL-006; `status: approved` |

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-024 (lát 4 của gateway) |
| 2026-10-25 | plan-review vòng 1: ghi 6 quyết định, sửa 8 FIX theo kết quả spike, gộp kịch bản (11 → 10), plan `approved` | plan-review TB-024 |
