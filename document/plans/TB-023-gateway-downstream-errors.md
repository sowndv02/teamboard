---
title: Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)
issue: TB-023
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-023 — Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)

Lát 3 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`). Làn lớn: spec và design `confirmed` ở TB-007; hợp đồng lỗi công khai và hành vi YARP (design mục 16). Ảnh chụp trạng thái branch: `feature/TB-023-gateway-downstream-errors`, tạo từ `main` `5b7266a` (đã gồm TB-010, TB-021, TB-022; PR 15 đã merge), working tree sạch trước khi soạn plan.

Đã có (đã đọc code): `src/gateway/Program.cs` (`AddServiceDefaults`, `AddReverseProxy().LoadFromConfig`, `UseCorrelation`, `UseTeamboardExceptionHandler`, `MapDefaultEndpoints`, `MapReverseProxy()` không callback, `MapFallback`), `GatewayErrorCodes` (chỉ `ROUTE_NOT_FOUND`), `appsettings.json` (hai route `boards-read` và `boards-write`, cluster `board-service` một đích `d1`, chưa có `Timeout`/`ActivityTimeout`), `src/shared/Http` (`ErrorDefinition`, `ProblemFactory`, `SharedErrors`, correlation, handler `500`), `src/gateway.Tests` (`UpstreamStub` Kestrel cổng ngẫu nhiên, `GatewayContext` dùng `WebApplicationFactory` (TestServer) và `SendAsync` đọc cả thân, `DockerCli`, `ComposeSteps`). Hiện lỗi kết nối tới upstream do YARP tự xử lý (status mặc định, thân rỗng, không `problem+json`).

**Kết quả spike của reviewer độc lập (YARP 2.3.0, .NET 10, Kestrel, Windows; thực nghiệm hộp đen, chưa đọc source; kiểm lại ở test-init):**

| Tình huống | `IForwarderErrorFeature` | Status YARP tự đặt |
|---|---|---|
| Cổng đóng / DNS không phân giải / upstream ngắt trước header | `Request` (ngoại lệ gốc: `ConnectionError`, `NameResolutionError`, `IOException`) | 502 |
| Cluster không có destination | `NoAvailableDestinations` | 503 |
| `ActivityTimeout` ngắn, upstream treo | `RequestTimedOut` | 504 |
| Route `Timeout` + `UseRequestTimeouts`, treo trước header | `RequestCanceled` (`RequestTimeoutToken` đã cancel) | 400 |
| Route `Timeout` mà thiếu `UseRequestTimeouts` | không có | 500 |
| Header đã gửi rồi upstream ngắt / treo (`ActivityTimeout`) | `ResponseBodyDestination` | 200 (đã gửi) |
| Header đã gửi rồi treo, route `Timeout` | `ResponseBodyCanceled` (timeout token đã cancel) | 200 (đã gửi) |
| Connect tới IP không trả lời (`ConnectTimeout` mặc định 15 giây) | `RequestTimedOut` | 504 |

Sau header, YARP tự ngắt kết nối với client; client thấy lỗi đọc thân.

## 1. Phạm vi của lát

**Làm:**
- Mã lỗi mới trong `GatewayErrorCodes` (hợp đồng công khai, DA-02): `UPSTREAM_UNAVAILABLE` (503), `UPSTREAM_BAD_GATEWAY` (502), `UPSTREAM_TIMEOUT` (504); mở rộng `GatewayErrorCodesTests` (không trùng mã, status, message không lộ nội bộ).
- Middleware của gateway đọc `IForwarderErrorFeature` sau khi forwarder chạy và, **khi response chưa bắt đầu**, ghi `problem+json` theo bảng ánh xạ (mục 3): `NoAvailableDestinations` hoặc `Request` mà ngoại lệ gốc là lỗi kết nối bị từ chối/không phân giải tên → `503` kèm `Retry-After`; `Request` khác (lỗi giao thức, ngắt trước header) và lỗi forwarder chưa thuộc bảng trước header → `502` kèm log chi tiết (người dùng chốt P-03); `RequestTimedOut` (gồm timeout kết nối) → `504` (người dùng chốt, ghi vào design D4 và spec FR-006). Timeout của route (feature `RequestCanceled`/`RequestBodyCanceled` mà `IHttpRequestTimeoutFeature.RequestTimeoutToken` đã cancel) cũng → `504`. Phản hồi không lộ host, cổng, tên nội bộ hay stack trace; có `code`, `traceId`, `correlationId` trong body và `X-Correlation-Id` ở header.
- Client huỷ request (cancel không do timeout, không do `*Destination`): không ghi response, log mức Information, không tính vào metric lỗi (người dùng chốt P-02). Phân biệt với timeout bằng `IHttpRequestTimeoutFeature`, không bằng `RequestAborted` (route `Timeout` cũng cancel `RequestAborted`).
- Timeout cấu hình được (NFR-003): `Timeout` tổng 30 giây ở mỗi route và `HttpRequest.ActivityTimeout` ≤ 30 giây ở cluster trong `appsettings.json`; `AddRequestTimeouts()` và `UseRequestTimeouts()` đặt **ngay sau `ExceptionHandler`** (người dùng chốt P-01; cập nhật design 6.4 cùng commit; thiếu middleware này thì route `Timeout` làm request 500).
- `Retry-After` trên `503`: số nguyên giây từ 1 đến 3600, mặc định 5 (người dùng chốt), qua Options có `ValidateOnStart` (`backend-engineering.md` mục 7); giá trị ngoài khoảng làm khởi động thất bại.
- Lỗi **sau khi đã gửi header** (`ResponseBodyDestination`, `ResponseBodyCanceled` do timeout, ...): YARP đã tự ngắt kết nối; gateway thêm log mức Warning trong scope correlation và tăng counter `gateway_upstream_aborted` (không nhãn `correlationId`: người dùng chốt, id nằm ở log; design D4 và mục 9 sửa chữ cho khớp). Không đổi status đã gửi.
- Quan sát lỗi trước header: instrumentation ASP.NET Core đánh dấu span lỗi với 5xx và đếm theo status trong `http.server.request.duration`; middleware ghi thêm log Warning (không URL, query, host, body) trong scope correlation. Đặt tên `Meter` tường minh (ví dụ `Teamboard.Gateway`) và đăng ký bằng `AddOpenTelemetry().WithMetrics(m => m.AddMeter(...))` trong gateway (không sửa `ServiceDefaults`).
- Test: host test in-process với upstream giả (`gateway.Tests`), một test Docker; `.feature` ở `document/spec/features/gateway/`.

**Không làm (trong lát này):** retry và circuit breaker (spec mục 4; ghi nợ ở design D4), `ForwardedHeaders` và rate limit (lát 4), body 1 MiB và kiểm `BadHttpRequestException` bị `ExceptionHandler` đổi thành 500 (lát 5, TB-025; việc TB-022 để lại "lát 3 và 5" được dồn về lát 5 vì chỉ liên quan 413), CORS (lát 6), metric độ trễ thêm vào, metric theo route/status, retention log, graceful shutdown (lát 7), smoke với `board-service` thật (lát 8), `identity-service` hay `board-service`, health `ready` phụ thuộc upstream (spec: không phụ thuộc).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-006 không kết nối được → `503` `problem+json` kèm `Retry-After` | D4, bảng mã mục 6 | B1 | @S01 | Host test: upstream giả đã dừng (cổng đóng): `GET` → 503 với `code`, `traceId`, `correlationId` trong body; `HEAD` và `POST` chỉ kiểm status, `Retry-After`, header `X-Correlation-Id` (HEAD không có body) |
| FR-006 không có đích khả dụng → `503` | D4 | B1 | @S02 | Host test: cluster có thuộc tính nhưng không có destination → 503 cùng dạng |
| FR-006 lỗi giao thức / ngắt trước header → `502` | D4 | B1 | @S03 | Host test: upstream giả đóng socket trước khi trả header → 502 |
| FR-006 lỗi forwarder chưa thuộc bảng trước header → `502` | D4 | B1 | (unit của bộ phân loại) | Unit: bảng `ForwarderError` + ngoại lệ → mã; cancel không timeout → không ghi response |
| FR-006, NFR-003 quá timeout trước header → `504` | D4 | B2 | @S04, @S05 | Host test: upstream treo; ghi đè `Timeout` ngắn (S04), `ActivityTimeout` ngắn (S05) → 504 |
| FR-006 timeout kết nối → `504` | D4 (cập nhật) | B2 | (unit của bộ phân loại) | Unit: `RequestTimedOut` với ngoại lệ `TimeoutException` → 504; `ConnectTimeout` thật (15 giây) không chạy trong CI |
| FR-006 không lộ host nội bộ hay stack trace; 5xx mang `X-Correlation-Id` | D4, mục 8, 11 | B1, B2 | @S01–@S05 (bước chung) | Mọi phản hồi lỗi: không chứa địa chỉ upstream, tên service, tên exception; allowlist thuộc tính; header `X-Correlation-Id` bằng `correlationId` của body (HEAD: chỉ header) |
| FR-006 header đã gửi rồi mới ngắt/treo → ngắt kết nối + log + metric | D4, mục 9 | B3 | @S06, @S07 | Host test (Outline S07: treo vượt `Timeout` của route / vượt `ActivityTimeout`): client lỗi đọc thân; `MeterListener` thấy `gateway_upstream_aborted`; log Warning có `correlationId` |
| NFR-003 timeout cấu hình được, không hardcode | D4 | B2, B4 | @S04, @S05, @S08 | Test dùng giá trị ghi đè ngắn qua cấu hình; test đọc `appsettings.json` thật: 30 giây |
| `Retry-After` cấu hình được, có khoảng hợp lệ | D4, `api-endpoints.md` §6 | B1 | @S01, @S11 | Ghi đè 7 giây → header `7`; giá trị 0 hoặc 3601 làm khởi động thất bại (unit `UpstreamOptions`) |
| Gateway sống khi upstream chết (spec FR-002) | spec | B4 | @S09 | `/health/live` và `/health/ready` 200 khi upstream đã dừng |
| FR-006 không kết nối được trong môi trường thật | D4 | B4 | @S10 | `@docker`: gateway chạy riêng, `GET /api/v1/boards` → 503 `UPSTREAM_UNAVAILABLE` |
| Thứ tự middleware khoá bằng test (slice mục đầu, design 6.4) | 6.4 (cập nhật) | B2 | @S04 (kèm) | Test khẳng định 504 mang `X-Correlation-Id` (Correlation ngoài) và route `Timeout` có hiệu lực (RequestTimeouts đứng trước YARP) |

Kịch bản (ID duy nhất trong file `.feature`; viết ở test-init):
- @S01 (Outline) Upstream đã dừng: `GET`, `HEAD`, `POST` đều nhận `503` `UPSTREAM_UNAVAILABLE` kèm `Retry-After` mặc định 5.
- @S02 Cluster không có đích khả dụng: `503` `UPSTREAM_UNAVAILABLE`.
- @S03 Upstream đóng kết nối trước khi trả header: `502` `UPSTREAM_BAD_GATEWAY`.
- @S04 Upstream treo vượt `Timeout` của route: `504` `UPSTREAM_TIMEOUT`.
- @S05 Upstream im lặng vượt `ActivityTimeout` của cluster (ngắn hơn `Timeout`): `504` `UPSTREAM_TIMEOUT`.
- @S06 Upstream trả header rồi đóng kết nối giữa chừng: client bị ngắt, metric `gateway_upstream_aborted` tăng 1, có log Warning mang `correlationId`.
- @S07 (Outline) Upstream trả header rồi treo vượt `Timeout` của route / vượt `ActivityTimeout`: client bị ngắt, metric tăng.
- @S08 Cấu hình: timeout mặc định trong `appsettings.json` là 30 giây (route và cluster).
- @S09 `/health/live` và `/health/ready` trả 200 khi upstream đã dừng.
- @S10 (`@docker`) Gateway chạy riêng trong compose, `GET /api/v1/boards` trả `503` `UPSTREAM_UNAVAILABLE`.
- @S11 `Retry-After` đọc từ cấu hình (ghi đè 7 giây → header `7`); tách khỏi @S08 ở test-init để ID kịch bản duy nhất và mỗi kịch bản một hành vi.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng (stub chỉ chuyển tiếp, không hành vi) chỉ để test biên dịch được. **Spike ở test-init** kiểm lại các giả định mục 7 trên mã của repo (reviewer đã spike hộp đen ở plan-review). Đỏ có thể không đạt cho phần "client bị ngắt" của S06/S07 vì YARP đã tự ngắt: ghi vào TDD log là xanh sẵn, chỉ log và metric đỏ.

**Bảng ánh xạ (sau spike và quyết định của người dùng):**

| Điều kiện (response chưa bắt đầu) | Kết quả |
|---|---|
| `NoAvailableDestinations` | 503 `UPSTREAM_UNAVAILABLE` + `Retry-After` |
| `Request` với ngoại lệ gốc `ConnectionError` / `NameResolutionError` / `SocketException` (từ chối kết nối, không phân giải tên) | 503 `UPSTREAM_UNAVAILABLE` + `Retry-After` |
| `RequestTimedOut` (gồm timeout kết nối) | 504 `UPSTREAM_TIMEOUT` |
| `RequestCanceled` / `RequestBodyCanceled` mà `RequestTimeoutToken` đã cancel (timeout theo route) | 504 `UPSTREAM_TIMEOUT` |
| `RequestCanceled` / `RequestBodyCanceled` / `*Client` không do timeout (client huỷ) | không ghi response; log Information |
| `Request` khác; `ResponseHeaders`; mọi `ForwarderError` khác | 502 `UPSTREAM_BAD_GATEWAY` + log chi tiết |
| Response đã bắt đầu (`ResponseBodyDestination`, `ResponseBodyCanceled` do timeout, ...) | YARP đã ngắt; log Warning + `gateway_upstream_aborted` |

### B1. Hợp đồng lỗi và ánh xạ 503 / 502 (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/downstream-errors.feature` (@S01 Outline, @S02, @S03) cộng unit test `GatewayErrorCodesTests` (ba mã mới), `UpstreamOptions` (khoảng 1..3600, mặc định 5) và bộ phân loại lỗi (bảng ánh xạ trên); `UpstreamStub` thêm chế độ đóng socket trước header (S03); `GatewayContext` hỗ trợ cluster không có đích và cấu hình ghi đè (S02: cluster phải có ít nhất một thuộc tính, ví dụ `LoadBalancingPolicy`, vì cluster vắng hẳn làm pipeline trả 503 mà không chạy middleware tuỳ chỉnh). Đỏ vì chưa có mã và chưa có middleware.
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs`; mới `src/gateway/UpstreamErrorMiddleware.cs` (đọc `IForwarderErrorFeature` sau `next()`, phân loại, ghi `problem+json`); mới `src/gateway/UpstreamOptions.cs` (`Gateway:Upstream:RetryAfterSeconds`, `ValidateOnStart`); `src/gateway/Program.cs` (đặt middleware; nếu dùng `MapReverseProxy(pipeline => ...)` thì thêm lại `UseSessionAffinity` → `UseLoadBalancing` → `UsePassiveHealthChecks`; spike cho thấy middleware đặt ngoài callback cũng đọc được feature, chọn cách ít thay đổi nhất sau khi kiểm); `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `503`/`502` `problem+json` công khai; header `Retry-After` trên `503`. `api-endpoints.md` không cần sửa (đã kiểm: §2, §5, §6 đã có `Retry-After` kèm `503`, không có bảng mã riêng từng service).
- **Migration / cấu hình:** khóa `Gateway:Upstream:RetryAfterSeconds` (mặc định 5) trong `appsettings.json`.
- **Tài liệu cần cập nhật:** không thêm (design D4 đã sửa ở plan-review).
- **Kiểm chứng:** @S01–@S03 xanh; test lát 1–2 (route, health, 404, correlation, 500) vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Timeout và ánh xạ 504
- **Kịch bản/test sẽ viết (đỏ trước):** @S04 và @S05 trong `downstream-errors.feature`; `UpstreamStub` thêm chế độ treo (không trả header, có thể huỷ để dọn); test ghi đè `Timeout` (route) hoặc `ActivityTimeout` (cluster) bằng giá trị ngắn qua cấu hình; khẳng định 504 mang `X-Correlation-Id`. Đỏ vì chưa có cấu hình timeout và chưa ánh xạ `504`.
- **File/module dự kiến chạm:** `src/gateway/appsettings.json` (`Timeout` mỗi route `00:00:30`, `HttpRequest.ActivityTimeout` của cluster `00:00:30`), `src/gateway/Program.cs` (`AddRequestTimeouts`, `UseRequestTimeouts` ngay sau handler; comment thứ tự cập nhật), `src/gateway/UpstreamErrorMiddleware.cs` (nhánh `504` gồm timeout theo route qua `IHttpRequestTimeoutFeature`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `504` `problem+json` công khai.
- **Migration / cấu hình:** hai giá trị timeout trong cấu hình (NFR-003); không biến môi trường mới.
- **Tài liệu cần cập nhật:** design 6.4 và D4 đã sửa ở plan-review.
- **Kiểm chứng:** @S04, @S05 xanh (mỗi test dưới ~5 giây nhờ giá trị ngắn); không `sleep` cố định; chạy lặp 10 lần nhóm host test của `gateway.Tests` để bắt flaky.
- **Phụ thuộc:** B1.

### B3. Lỗi sau khi đã gửi header: log và metric
- **Kịch bản/test sẽ viết (đỏ trước):** @S06, @S07 (Outline hai dòng); `UpstreamStub` thêm chế độ trả header rồi đóng hoặc treo; `GatewayContext` thêm đường gửi `HttpCompletionOption.ResponseHeadersRead` (đường hiện tại đọc cả thân nên ném ở giữa chừng); test đọc thân và kỳ vọng lỗi đọc; `MeterListener` bắt `gateway_upstream_aborted`; `ILoggerProvider` bắt log Warning kèm scope `correlationId`. Đỏ ở log và metric; phần client bị ngắt xanh sẵn vì YARP tự ngắt.
- **File/module dự kiến chạm:** mới `src/gateway/GatewayMetrics.cs` (`Meter` tên tường minh và counter), `src/gateway/UpstreamErrorMiddleware.cs` (nhánh response đã bắt đầu: log, tăng counter; không gọi `Abort()` nếu YARP đã ngắt, chỉ gọi khi spike cho thấy cần), `src/gateway/Program.cs` (đăng ký meter), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không đổi HTTP; thêm metric nội bộ (không phải hợp đồng công khai).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (design mục 9 đã sửa chữ ở plan-review).
- **Kiểm chứng:** @S06, @S07 xanh; log không chứa `Authorization`, body hay địa chỉ upstream.
- **Phụ thuộc:** B1 (middleware), B2 (timeout cho S07).

### B4. Cấu hình, sức khoẻ khi upstream chết và test Docker
- **Kịch bản/test sẽ viết (đỏ trước):** @S08 (đọc `appsettings.json` thật), @S09, @S10 (`@docker`, tái dùng `ComposeSteps`/`DockerCli` với tên dự án riêng `tb023-tests`, chạy riêng service `gateway`; để xác định, ghi đè địa chỉ cluster về cổng đóng trong container (ví dụ `http://127.0.0.1:1`) thay vì dựa vào việc `board-service` không phân giải được: DNS wildcard hoặc search domain của máy có thể làm tên đó phân giải được). Có thể xanh sẵn (S09) nếu hành vi đã đúng từ lát 1: ghi vào TDD log là test hồi quy.
- **File/module dự kiến chạm:** `src/gateway.Tests/` (step, feature), `document/spec/features/gateway/`, `COMMANDS.md` (kiểm nhắc nhóm docker mỗi lần một assembly; đã có từ TB-022).
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** bảng lát và `document/issues/README.md` theo LL-006: lát 3 → `in-progress` khi bắt đầu coding, `review` khi mở PR, `done` sau merge; sửa luôn hàng lát 2 và TB-022 (đã merge) thành `done` nếu chưa đồng bộ (cùng thay đổi, vì thuộc cùng bảng).
- **Kiểm chứng:** @S08–@S10 xanh (S10 cần Docker Desktop, cổng 8080 trống); các test Docker khác (TB-001, TB-010, TB-021, TB-022) vẫn xanh khi chạy từng assembly; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B1–B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Bảng mã lỗi gateway; `UpstreamOptions`; bộ phân loại `ForwarderError` + ngoại lệ → mã | xUnit thuần | Không mock |
| Integration (host in-process) | Ánh xạ 503/502/504, `Retry-After`, header `X-Correlation-Id`, log và metric sau header (`gateway.Tests`) | `WebApplicationFactory<Program>` (TestServer) + `UpstreamStub` Kestrel cổng ngẫu nhiên có chế độ lỗi (dừng, đóng socket, treo, header rồi đóng/treo); cấu hình ghi đè bằng `UseSetting` | Không mock YARP; upstream giả là socket thật để lỗi mạng thật; spike cho thấy TestServer cũng cho client lỗi đọc thân khi upstream ngắt |
| Integration Docker (nhóm `docker`) | `503` trong compose thật @S10 | Compose chính, chỉ chạy `gateway`, tên dự án riêng | Gọi `docker compose` qua Process; tự bỏ qua khi không có Docker |

Kim tự tháp: nhiều unit/host test nhanh, một test Docker. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`). Test BDD viết tiếng Anh, ASCII (`test-review.md` mục 1).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Mã trạng thái và `IForwarderErrorFeature` của YARP khác giả định (design mục 16) | Ánh xạ sai | Bảng ánh xạ đã dựa trên spike của reviewer (hộp đen); kiểm lại ở test-init; sai thì dừng và hỏi | Revert B1 |
| Thêm `UseRequestTimeouts` làm lệch thứ tự middleware đã khoá bằng test | Test thứ tự đỏ hoặc 400/500 do route `Timeout` | Người dùng đã chốt vị trí; sửa design 6.4 và test thứ tự cùng B2 | Revert B2 |
| Route `Timeout` ra `RequestCanceled` (YARP đặt 400) nên dễ nhầm với client huỷ | Trả 400 thay vì 504 hoặc ngược lại | Phân biệt bằng `IHttpRequestTimeoutFeature.RequestTimeoutToken`; test S04, S07 | Revert B2 |
| Test timeout/treo flaky hoặc chậm | CI chậm, đỏ oan | Giá trị timeout ngắn (≈1 giây) qua cấu hình, chờ có điều kiện, không `sleep`; chạy lặp 10 lần | Sửa test, không nới điều kiện |
| Phần "client bị ngắt" của S06/S07 xanh sẵn (YARP tự ngắt) | Test không dẫn dắt code ở phần đó | Ghi vào TDD log; log và metric vẫn đỏ | Không áp dụng |
| `GatewayContext.SendAsync` đọc cả thân nên ném giữa chừng | Test không viết được | Thêm đường `ResponseHeadersRead` ở B3 | Không áp dụng |
| Test Docker S10 phụ thuộc DNS của Docker/máy | Flaky | Ghi đè địa chỉ về cổng đóng trong container | Sửa test |
| Test Docker chồng cổng với assembly khác | Đỏ oan | Chạy nhóm docker mỗi lần một assembly (`COMMANDS.md`) | Không áp dụng |

Ảnh hưởng consumer (service khác): thêm `503`/`502`/`504` `problem+json` và `Retry-After` ở contract công khai của gateway (cộng thêm, trước đây là thân rỗng); client Angular chưa tồn tại (ADR 0008); `board-service` không đổi. Bảo mật: lỗi không lộ host/cổng/tên nội bộ hay stack trace (STRIDE design mục 8); log không ghi header `Authorization`, cookie, body, URL đầy đủ; không secret mới. Chi phí: không tài nguyên mới; một test Docker (build image gateway, vài chục giây).

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `security`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát với issue) áp dụng ở B4; LL-005 (câu hỏi mở kèm lựa chọn và khuyến nghị) áp dụng cho mục 7; quy tắc không ghi query string vào log lấy từ chú thích ở `src/shared/ServiceDefaults/ServiceDefaultsExtensions.cs` (không phải một lesson).

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"` rồi nhóm `docker` mỗi lần một assembly; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ).
- Tiêu chí hoàn thành: theo issue TB-023.
- Tài liệu/config cập nhật: như B1–B4; bảng lát và `document/issues/README.md` (LL-006).

## 7. Câu hỏi mở và giả định

Giả định đã kiểm bằng spike hộp đen của reviewer (YARP 2.3.0, .NET 10, Windows; kiểm lại ở test-init trên mã repo): A-01 forwarder đặt `IForwarderErrorFeature` và middleware đọc được sau `next()` khi response chưa bắt đầu (đúng, cả trong và ngoài callback `MapReverseProxy`); A-02 `NoAvailableDestinations` → 503 (đúng); A-03 `ActivityTimeout` → `RequestTimedOut`/504, route `Timeout` → `RequestCanceled`/400 (cần `UseRequestTimeouts`, thiếu thì 500; phân biệt bằng `IHttpRequestTimeoutFeature`); A-04 sau header YARP tự ngắt kết nối, client thấy lỗi đọc thân (đúng, cả TestServer); A-05 lỗi kết nối phân biệt với lỗi giao thức qua `HttpRequestError` hoặc `SocketException` (đúng: `ConnectionError`, `NameResolutionError`). Chưa kiểm: `ForwarderError.ResponseHeaders` xảy ra khi nào, timeout kết nối với giá trị ngắn (YARP không có khoá cấu hình, chỉ mặc định 15 giây), `MeterListener` và bắt log scope trong test, hành vi trên Linux container. Nguồn đã đọc: tài liệu YARP về timeouts và direct forwarding.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Vị trí `UseRequestTimeouts` | B2, design 6.4 | sondv | answered: ngay sau `ExceptionHandler`, sửa design 6.4 (2026-10-25) |
| P-02 | Client huỷ request | B1 | sondv | answered: không ghi response, log Information, không tính metric lỗi (2026-10-25) |
| P-03 | Lỗi forwarder ngoài bảng trước header | B1 | sondv | answered: `502` kèm log chi tiết (2026-10-25) |
| P-04 | Tên và nhãn metric | B3 | sondv | answered: tên `gateway_upstream_aborted` theo design, không nhãn, `correlationId` chỉ ở log; sửa spec/design/issue (2026-10-25) |
| P-05 | Timeout kết nối: 503 hay 504 | B2 | sondv | answered: `504`, ghi vào design D4 và spec FR-006 (2026-10-25) |
| P-06 | Cỡ lát (10 kịch bản, 4 bước) | cả plan | sondv | answered: giữ nguyên (2026-10-25) |
| P-07 | Khoảng giá trị `Retry-After` | B1 | sondv | answered: số nguyên 1..3600, mặc định 5 (2026-10-25) |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-023:p01-request-timeouts-position | Vị trí `UseRequestTimeouts` trong thứ tự middleware | Ngay sau `ExceptionHandler`, sửa design 6.4 | 1 | 2026-10-25 |
| 2 | plan-review:TB-023:metric-correlationid | Metric `gateway_upstream_aborted` có `correlationId`? | Chỉ ở log; metric chỉ đếm; sửa chữ spec, design và issue | 1 | 2026-10-25 |
| 3 | plan-review:TB-023:connect-timeout-mapping | Timeout kết nối là 503 hay 504 | `504`, ghi vào design D4 và spec FR-006 | 1 | 2026-10-25 |
| 4 | plan-review:TB-023:p03-unmapped-forwarder-errors | Lỗi forwarder ngoài bảng trước header | `502` kèm log chi tiết | 1 | 2026-10-25 |
| 5 | plan-review:TB-023:p02-client-cancel | Client huỷ request | Không ghi response, log Information, không tính metric lỗi | 1 | 2026-10-25 |
| 6 | plan-review:TB-023:slice-size | Giữ hay chia lát | Giữ nguyên | 1 | 2026-10-25 |
| 7 | plan-review:TB-023:retry-after-range | Khoảng giá trị `Retry-After` | Số nguyên 1..3600, mặc định 5 | 1 | 2026-10-25 |

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-25 | Toàn plan, đủ A0, A–H (reviewer độc lập kèm spike YARP) | 7 → đã chốt | 11 → đã sửa | NEEDS_CONFIRM → PASS sau khi chốt và sửa |

### Fix log vòng 1

| Fingerprint | Vị trí | Vấn đề | Đã sửa thế nào |
|---|---|---|---|
| mapping-after-header-and-timeout-feature | mục 3 bảng ánh xạ, mục 7, S07 | Route `Timeout` ra `RequestCanceled`/`ResponseBodyCanceled`, bảng thiếu | Bảng ánh xạ mới có timeout theo route qua `IHttpRequestTimeoutFeature`; S07 thành Outline hai dòng |
| correlation-header-on-5xx | bảng truy vết | Chưa kiểm `X-Correlation-Id` ở 5xx | Thêm bước chung và dòng truy vết thứ tự middleware |
| testserver-vs-kestrel-claim | mục 4, 5, B3 | Nói gateway chạy Kestrel thật; `SendAsync` đọc cả thân | Sửa mô tả; thêm đường `ResponseHeadersRead`; ghi S06/S07 xanh sẵn phần ngắt |
| head-no-body-assert | S01 | HEAD không có thân | HEAD chỉ kiểm status và header |
| s10-dns-determinism | B4, mục 5 | S10 phụ thuộc DNS | Ghi đè địa chỉ về cổng đóng trong container; thêm rủi ro |
| lesson-citation-invalid | mục 5 | Viện lesson không tồn tại | Trích đúng nguồn (chú thích ServiceDefaults) và LL-005, LL-006 |
| slice-table-sync-ll006 | B4 | Chưa nêu cập nhật bảng lát | Liệt kê cập nhật bảng lát và README, gồm hàng lát 2 |
| api-endpoints-no-change | B1 | Mục "kiểm ở plan-review" còn mở | Đã kiểm: không cần sửa |
| log-span-5xx-design65 | mục 1 | Thiếu log/span cho 5xx trước header | Nêu span tự đánh dấu, log Warning, đặt tên `Meter` |
| carryover-from-tb022 | mục 1 không làm | Việc TB-022 để lại cho lát 3 | Dồn kiểm `BadHttpRequestException` về lát 5 (TB-025) |
| default-middleware-wording | B1, mục 5 | Mô tả sai về `NoAvailableDestinations`; thiếu thứ tự | Ghi thứ tự middleware mặc định; chỉ thêm lại khi dùng callback; ghi chú S02 |

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-023 (lát 3 của gateway) |
| 2026-10-25 | plan-review vòng 1: ghi 7 quyết định, sửa 11 FIX, sửa spec FR-006 và design D4, 6.4, 9; plan `approved` | plan-review TB-023 |
| 2026-10-25 | test-init: tách @S11 (Retry-After từ cấu hình) khỏi @S08; không đổi hành vi | test-init TB-023 |
