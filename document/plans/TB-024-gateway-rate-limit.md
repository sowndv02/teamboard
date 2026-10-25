---
title: Gateway lát 4, rate limit theo IP và 429 problem+json
issue: TB-024
status: draft            # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-024 — Gateway lát 4, rate limit theo IP và 429 problem+json

Lát 4 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`; phụ thuộc lát 1, 2 đã merge; lát 3 cũng đã merge). Làn lớn: spec và design `confirmed` ở TB-007; chạm bảo mật (xác định IP client, né giới hạn) và hợp đồng lỗi công khai. Ảnh chụp trạng thái branch: `feature/TB-024-gateway-rate-limit`, tạo từ `main` `feb4278` (đã gồm TB-010, TB-022, TB-023; PR 16 đã merge), working tree chỉ có thay đổi đồng bộ trạng thái lát 3 (issue TB-023, README, bảng lát) chưa commit.

Đã có (đã đọc code): `src/gateway/Program.cs` (pipeline `UseCorrelation` → `UseTeamboardExceptionHandler` → `UseRequestTimeouts` → `UpstreamErrorMiddleware` → `MapDefaultEndpoints` → `MapReverseProxy` → `MapFallback`; chưa có `ForwardedHeaders` và `RateLimiter`), `appsettings.json` (hai route `boards-read` GET/HEAD và `boards-write` POST/PUT/PATCH/DELETE với `Timeout` 30 giây, cluster `board-service`), `GatewayErrorCodes` (chưa có `RATE_LIMIT_EXCEEDED`), `UpstreamOptions` (mẫu Options có `ValidateOnStart`), `src/gateway.Tests` (`GatewayContext` dựng `WebApplicationFactory` (TestServer) với `Settings`, `ConfigureTestServices`, bắt log; `UpstreamStub` Kestrel ghi header nhận được; `DockerCli`; `RepoRoot`). Hiện không có giới hạn nào theo IP.

## 1. Phạm vi của lát

**Làm (FR-004; design D1, D2, D5, 6.4):**
- `ForwardedHeaders` an toàn ở **đầu pipeline** (design D5, 6.4): chỉ tin proxy trong danh sách cấu hình (`KnownProxies`/`KnownNetworks`, mặc định chỉ loopback của middleware), `ForwardLimit` mặc định của middleware; không đặt `ASPNETCORE_FORWARDEDHEADERS_ENABLED`. Client giả `X-Forwarded-For` từ nguồn không tin cậy không đổi được IP dùng để đếm.
- Rate limit `sliding window` theo khoá `IP client + nhóm route` (D2): policy `read` (route `boards-read`, gồm `GET` và `HEAD`) mặc định 120 request/phút/IP và policy `write` (route `boards-write`) mặc định 30 request/phút/IP; ngưỡng, độ dài cửa sổ và số đoạn đọc từ cấu hình có kiểm tra giá trị (NFR-004); gắn vào route bằng `RateLimiterPolicy` của YARP.
- Khoá IP: IPv4 theo địa chỉ đầy đủ; **IPv6 gộp theo /64** (người dùng đã chọn ở design D2); hàm tính khoá là hàm thuần, test riêng.
- `429 problem+json` mã `RATE_LIMIT_EXCEEDED` kèm `Retry-After` (giây, lấy từ metadata của lease nếu có, ngược lại độ dài cửa sổ; design D1) và `X-Correlation-Id`, không lộ IP hay khoá trong thân (`OnRejected` dùng `ProblemFactory`, `RejectionStatusCode = 429`).
- Health (`/health/live`, `/health/ready`) và fallback `404` không bị giới hạn (không gắn policy).
- `X-Forwarded-For/Proto/Host` gửi xuống upstream do gateway xác định, không nối thêm giá trị client gửi (D5; giả định hành vi mặc định của YARP chưa kiểm: spike ở test-init; câu hỏi mở P-03 về phạm vi).
- Test cấu hình: `ASPNETCORE_FORWARDEDHEADERS_ENABLED` không xuất hiện trong các file compose (D5); ngưỡng mặc định 120/30 trong `appsettings.json`.

**Không làm (trong lát này):** CORS và `OPTIONS` (lát 6: preflight không tính vào rate limit, sẽ kiểm ở đó), giới hạn body (lát 5), metric đếm `429` và mọi metric/log retention (lát 7), kho đếm dùng chung nhiều instance (D1: đếm trong bộ nhớ, ghi nợ), rate limit riêng cho `identity-service` (identity lát 3, TB-031), danh sách proxy tin cậy của ALB (giai đoạn 4), kiểm qua compose với IP thật (Docker Desktop NAT; D5 chọn test với `RemoteIpAddress` giả), thay đổi `board-service`.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-004 request ghi thứ 31 trong phút nhận `429` kèm `Retry-After` | D1, D2 | B1 | @S01 | Host test với policy nhỏ ghi đè bằng cấu hình (ví dụ 3/phút): request thứ 4 trả 429, `code` `RATE_LIMIT_EXCEEDED`, header `Retry-After` hợp lệ |
| FR-004 `GET` thứ 121 nhận `429`, thứ 120 vẫn thành công; `HEAD` tính chung với đọc | D2, DA-05 | B2 | @S02 | Host test policy đọc nhỏ: request cuối trong ngưỡng 200, kế tiếp 429; `HEAD` và `GET` dùng chung bộ đếm |
| FR-004 IP khác không bị ảnh hưởng | D2 | B2 | @S03 | Host test với `RemoteIpAddress` giả: IP A hết hạn mức, IP B vẫn qua |
| FR-004 hai địa chỉ IPv6 cùng /64 dùng chung bộ đếm; IPv4 đủ địa chỉ | D2 | B2 | @S04 + unit | Unit hàm khoá (IPv4, IPv6 /64, IPv4-mapped, biên); host test hai địa chỉ cùng /64 chung hạn mức |
| FR-004 đọc và ghi đếm riêng | D2 | B2 | @S05 | Hết hạn mức ghi nhưng đọc vẫn qua và ngược lại |
| FR-004 health và fallback không bị giới hạn | D1, 6.4 | B2 | @S06 | `/health/live`, `/health/ready`, đường dẫn lạ vượt xa ngưỡng vẫn 200/404 |
| FR-004 `429` mang `X-Correlation-Id`, không lộ IP hay khoá | 6.3, D5 | B1 | @S01 (bước chung) | Header `X-Correlation-Id` bằng `correlationId` trong thân; allowlist thuộc tính; thân không chứa IP |
| Spec/D5 giả `X-Forwarded-For` để né rate limit | D5 | B3 | @S07 | Nguồn không tin cậy gửi `X-Forwarded-For` khác nhau mỗi request: vẫn bị đếm chung theo địa chỉ kết nối |
| D5 chỉ tin proxy trong danh sách cấu hình | D5 | B3 | @S08 | Proxy tin cậy (cấu hình) gửi `X-Forwarded-For`: khoá theo địa chỉ client trong header, hai client khác nhau không chung hạn mức |
| D5 `X-Forwarded-*` xuống upstream do gateway xác định | D5 | B3 | @S09 | Upstream giả ghi header nhận được: `X-Forwarded-For` không chứa giá trị giả của client |
| NFR-004 ngưỡng, cửa sổ, proxy trong cấu hình, không hardcode | D2, D5 | B1, B4 | @S10 + unit | Test đọc `appsettings.json` thật: 120 và 30 mỗi 60 giây; giá trị không hợp lệ làm khởi động thất bại |
| D5 cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | D5 | B4 | @S11 | Test đọc các file compose: biến không có mặt |

Kịch bản (ID duy nhất trong file `.feature`; viết ở test-init; viết tiếng Anh, ASCII):
- @S01 Ghi quá hạn mức: `429` `RATE_LIMIT_EXCEEDED` kèm `Retry-After` và `X-Correlation-Id`.
- @S02 Đọc quá hạn mức (`GET` và `HEAD` chung bộ đếm): request cuối trong ngưỡng thành công, kế tiếp `429`.
- @S03 IP khác không bị ảnh hưởng.
- @S04 Hai địa chỉ IPv6 cùng /64 dùng chung hạn mức.
- @S05 Đọc và ghi đếm riêng.
- @S06 Health và fallback không bị giới hạn.
- @S07 `X-Forwarded-For` từ nguồn không tin cậy không né được giới hạn.
- @S08 Proxy tin cậy: IP client lấy từ `X-Forwarded-For`.
- @S09 Upstream nhận `X-Forwarded-For` do gateway xác định.
- @S10 Ngưỡng mặc định trong cấu hình thật (xUnit, không có `.feature`) và giá trị không hợp lệ làm khởi động thất bại.
- @S11 Compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (xUnit, không có `.feature`).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng (stub chỉ chuyển tiếp, không hành vi) chỉ để test biên dịch được. **Spike ở test-init** kiểm các giả định mục 7 trên mã của repo. Test không dùng `Thread.Sleep` hay chờ đồng hồ thật để qua cửa sổ: dùng ngưỡng nhỏ qua cấu hình (design D2) và chỉ kiểm hành vi trong một cửa sổ.

### B1. Policy `write` và `429 problem+json` (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/rate-limit.feature` @S01; unit `RateLimitOptions` (giá trị hợp lệ, mặc định 120/30, cửa sổ 60 giây, số đoạn); `GatewayErrorCodesTests` (mã `RATE_LIMIT_EXCEEDED` 429, message không lộ nội bộ); `GatewayContext` thêm cách đặt IP giả cho test (middleware `IStartupFilter` đặt `RemoteIpAddress` từ header test, chạy trước mọi middleware của gateway). Đỏ vì chưa có rate limiter.
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs` (mã mới), mới `src/gateway/RateLimiting/RateLimitOptions.cs` (`RateLimiting:Read` và `RateLimiting:Write`: `PermitLimit`, `WindowSeconds`, `SegmentsPerWindow`; `ValidateOnStart`), mới `src/gateway/RateLimiting/ClientPartitionKey.cs` (hàm thuần: IP → khoá; IPv6 /64), mới `src/gateway/RateLimiting/RateLimitingExtensions.cs` (`AddGatewayRateLimiting`, `OnRejected` → `ProblemFactory` + `Retry-After`), `src/gateway/appsettings.json` (ngưỡng và `RateLimiterPolicy` ở hai route), `src/gateway/Program.cs` (`UseRateLimiter` sau `UpstreamError`, trước YARP), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `429` `problem+json` công khai (`RATE_LIMIT_EXCEEDED`) kèm `Retry-After` (design 6.3, DA-02).
- **Migration / cấu hình:** khoá `RateLimiting:Read|Write:*` trong `appsettings.json` (mặc định 120 và 30 mỗi 60 giây); không biến môi trường mới.
- **Tài liệu cần cập nhật:** design 6.4 nếu spike cho thấy vị trí `UseRateLimiter` hoặc `ForwardedHeaders` cần ghi khác; không sửa khi chưa được người dùng chốt.
- **Kiểm chứng:** @S01 xanh; test lát 1–3 (route, health, 404, correlation, 500, 503/502/504) vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Policy `read`, nhóm route, khoá IP và health
- **Kịch bản/test sẽ viết (đỏ trước):** @S02, @S03, @S04, @S05, @S06 trong `rate-limit.feature`; unit `ClientPartitionKeyTests` (IPv4 nguyên địa chỉ, IPv6 cùng /64 cùng khoá, khác /64 khác khoá, IPv4-mapped IPv6 → khoá như IPv4 (P-02), địa chỉ không xác định (P-04), biên /64). Đỏ vì chưa có policy `read` và khoá IPv6.
- **File/module dự kiến chạm:** `src/gateway/RateLimiting/*` (policy `read`, gắn nhóm), `src/gateway/appsettings.json` (route `boards-read` dùng policy `read`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không thêm (cùng `429`).
- **Migration / cấu hình:** không thêm.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** @S02–@S06 xanh; unit khoá IP xanh; chạy lặp 10 lần nhóm host test của `gateway.Tests` để bắt flaky (cửa sổ trượt phụ thuộc đồng hồ).
- **Phụ thuộc:** B1.

### B3. `ForwardedHeaders` an toàn và `X-Forwarded-*` xuống upstream
- **Kịch bản/test sẽ viết (đỏ trước):** @S07, @S08, @S09 trong `rate-limit.feature` (hoặc `forwarded-headers.feature` nếu tách capability; quyết ở test-init theo `test-review.md` mục 1); `UpstreamStub` đã ghi header nhận được, thêm `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`. Đỏ vì chưa có `ForwardedHeaders` (khoá IP luôn là địa chỉ kết nối) và hành vi `X-Forwarded-*` mặc định chưa kiểm.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`UseForwardedHeaders` đặt trước các middleware khác; thứ tự với `UseRouting` ngầm kiểm ở spike), mới `src/gateway/ForwardedHeadersOptionsSetup.cs` hoặc cấu hình tương đương (`ForwardedHeaders:KnownProxies`, `KnownNetworks` đọc từ cấu hình; rỗng nghĩa là chỉ loopback), cấu hình transform của YARP trong `appsettings.json` hoặc mã (đặt lại `X-Forwarded-*`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không đổi hợp đồng công khai; `board-service` nhận `X-Forwarded-For` do gateway xác định (thay vì nối thêm).
- **Migration / cấu hình:** khoá `ForwardedHeaders:*` (mặc định rỗng); không biến môi trường mới và không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
- **Tài liệu cần cập nhật:** design 6.4/D5 nếu spike cho thấy hành vi YARP khác điều design giả định; không sửa khi chưa được người dùng chốt.
- **Kiểm chứng:** @S07–@S09 xanh; @S01–@S06 vẫn xanh.
- **Phụ thuộc:** B2.

### B4. Cấu hình thật và kiểm tra compose
- **Kịch bản/test sẽ viết (đỏ trước):** `ConfigurationTests` mở rộng (đọc `appsettings.json` thật: 120 và 30 mỗi 60 giây, `RateLimiterPolicy` ở đúng route) và test chính sách compose (@S11: các file `src/docker-compose*.yml` không chứa `ASPNETCORE_FORWARDEDHEADERS_ENABLED`; có thể xanh sẵn nếu chưa ai đặt biến này: ghi vào TDD log là test hồi quy). `UpstreamOptionsTests`-kiểu cho `RateLimitOptions` (giá trị ngoài khoảng làm khởi động thất bại).
- **File/module dự kiến chạm:** `src/gateway.Tests/`, `COMMANDS.md` (không đổi nếu không có test docker mới).
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** bảng lát và `document/issues/README.md` theo LL-006 (lát 4 → `in-progress` khi coding, `review` khi mở PR, `done` sau merge); kiểm lát trước đã `done` (đã làm ở bước tạo nhánh).
- **Kiểm chứng:** B4 xanh; toàn bộ test không docker của solution xanh; nhóm docker từng assembly vẫn xanh nếu Docker bật; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B1–B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Hàm khoá IP (IPv4, IPv6 /64, IPv4-mapped); `RateLimitOptions`; mã lỗi | xUnit thuần | Không mock |
| Integration (host in-process) | 429, `Retry-After`, nhóm route, health, `ForwardedHeaders`, `X-Forwarded-*` xuống upstream (`gateway.Tests`) | `WebApplicationFactory<Program>` (TestServer) + `UpstreamStub`; ngưỡng nhỏ qua cấu hình; `RemoteIpAddress` giả bằng `IStartupFilter` | Không mock rate limiter hay YARP; không chờ qua cửa sổ thật |
| Test chính sách | Compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`; ngưỡng mặc định | Đọc file trong repo | Không cần Docker |

Không có test Docker mới trong lát này (design D5: không kiểm IP end-to-end qua compose do NAT của Docker Desktop). Kim tự tháp: nhiều unit và host test nhanh. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`). Test BDD viết tiếng Anh, ASCII.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| `RateLimiterPolicy` theo route và `OnRejected` của YARP/ASP.NET Core khác giả định (mã trả về, thứ tự, header) | 429 sai dạng hoặc thiếu `Retry-After` | Spike ở test-init; design D1 đã ghi "kiểm bằng test khi cài" | Revert B1 |
| Cửa sổ trượt phụ thuộc đồng hồ nên test flaky ở ranh giới đoạn | Test đỏ oan | Ngưỡng nhỏ và chỉ kiểm trong một cửa sổ; không chờ qua cửa sổ; chạy lặp 10 lần | Sửa test, không nới điều kiện |
| `ForwardedHeaders` đứng trước hay sau `UseRouting` ngầm làm lệch thứ tự đã khoá bằng test | Test thứ tự đỏ hoặc IP sai | Spike; nếu cần gọi `UseRouting` tường minh thì ghi vào design 6.4 sau khi người dùng chốt | Revert B3 |
| Cấu hình `KnownProxies` sai hoặc bật mọi proxy làm kẻ tấn công né giới hạn | Mất tác dụng rate limit | Mặc định chỉ loopback; test @S07; test compose cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Revert B3 |
| Dưới Docker Desktop mọi client cục bộ có cùng IP cầu nối | Thấy 429 sớm khi thử tay | Ghi chú trong tài liệu chạy local; test dùng `RemoteIpAddress` giả | Không áp dụng |
| Bộ đếm trong bộ nhớ: mất khi khởi động lại, lỏng N lần khi N instance | Giới hạn thực lỏng hơn | Chấp nhận (design D1), ghi vào giới hạn đã biết; xem lại khi >1 instance | Không áp dụng |
| Khoá IPv4-mapped IPv6 sai làm một client có hai bộ đếm | Né giới hạn | Chuẩn hoá về IPv4 (P-02); unit test | Sửa hàm khoá |

Ảnh hưởng consumer (service khác): thêm `429` `problem+json` và `Retry-After` ở contract công khai của gateway (cộng thêm); client Angular chưa tồn tại (ADR 0008); `board-service` nhận `X-Forwarded-For` do gateway xác định (không còn giá trị client gửi). Bảo mật (STRIDE design mục 8): giả `X-Forwarded-For` (chỉ tin proxy trong cấu hình), xoay địa chỉ IPv6 (gộp /64), DoS bằng spam (giới hạn theo IP; DDoS lớn thuộc WAF/ALB, giai đoạn 4); `429` không lộ IP hay khoá; không secret mới. Chi phí: không tài nguyên mới; không test Docker mới.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `security`, `observability`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát và issue) áp dụng ở B4 và đã áp dụng lúc tạo nhánh (đồng bộ lát 3 → `done`); LL-005 (câu hỏi mở kèm lựa chọn và khuyến nghị) áp dụng cho mục 7; từ TB-023: kiểm test log chỉ trên logger của gateway, và bỏ `Response.Clear()` để không xoá header của middleware khác (áp dụng cho `OnRejected`: không xoá header đã đặt, để CORS lát 6 còn nguyên).

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"` rồi nhóm `docker` từng assembly nếu Docker bật; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ).
- Tiêu chí hoàn thành: theo issue TB-024.
- Tài liệu/config cập nhật: như B1–B4; bảng lát và `document/issues/README.md` (LL-006).

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở spike test-init; sai thì dừng và hỏi): A-01 `RateLimiterPolicy` của route YARP đặt `EnableRateLimitingAttribute` vào metadata endpoint nên health và fallback (không gắn policy) không bị giới hạn (chưa kiểm); A-02 lease của `SlidingWindowRateLimiter` mang metadata `RetryAfter` dùng được trong `OnRejected` (chưa kiểm; dự phòng: độ dài cửa sổ 60 giây theo design D1); A-03 mặc định của YARP khi chuyển `X-Forwarded-For` (nối thêm hay thay) chưa kiểm và phải kiểm bằng upstream giả; A-04 `ForwardedHeadersMiddleware` với `RemoteIpAddress` do test đặt (không phải loopback) bỏ qua `X-Forwarded-For` đúng như D5 mô tả (chưa kiểm); A-05 hàng đợi của limiter bằng 0 (từ chối ngay, không xếp hàng) vì spec và design không nêu xếp hàng (giả định của plan, xem P-05); A-06 `UseRateLimiter` đặt sau `UpstreamError` cho kết quả đúng vì `429` do rate limiter ghi trực tiếp, không đi qua forwarder (chưa kiểm).

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-01 | Số đoạn của cửa sổ trượt 60 giây (design D2 chỉ ghi "chia đoạn nhỏ"). Lựa chọn: (a) 6 đoạn (10 giây); (b) 12 đoạn (5 giây); (c) 60 đoạn (1 giây). Khuyến nghị (a): burst rìa cửa sổ giảm còn ~1/6 với chi phí bộ nhớ nhỏ. Hậu quả nếu chưa chốt: B1 không viết được cấu hình mặc định. | B1 | sondv | open |
| P-02 | Địa chỉ IPv4-mapped IPv6 (`::ffff:a.b.c.d`) tính là IPv4 `a.b.c.d` hay là địa chỉ IPv6 thường (gộp /64)? Khuyến nghị: chuẩn hoá về IPv4 (cùng một client không thể có hai bộ đếm). Spec không nêu. | B2 | sondv | open |
| P-03 | Phạm vi `X-Forwarded-*` xuống upstream (D5): làm trong lát này (đặt lại `X-Forwarded-For/Proto/Host` theo giá trị gateway xác định) hay để lát khác? Khuyến nghị: làm ở lát này vì thuộc "ForwardedHeaders an toàn" của hàng lát và design 11 yêu cầu kiểm ở FR-003 nhưng lát 2 chưa kiểm. Nếu YARP mặc định đã đúng thì chỉ thêm test. | B3 | sondv | open |
| P-04 | Khi `RemoteIpAddress` không xác định (null; không xảy ra trên Kestrel thật): dùng một bộ đếm chung "unknown" hay từ chối? Khuyến nghị: bộ đếm chung (an toàn, không chặn oan toàn bộ). | B2 | sondv | open |
| P-05 | Request vượt hạn mức có xếp hàng đợi hay từ chối ngay? Khuyến nghị: từ chối ngay (`QueueLimit` 0), vì spec chỉ nói `429`. | B1 | sondv | open |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-024 (lát 4 của gateway) |
