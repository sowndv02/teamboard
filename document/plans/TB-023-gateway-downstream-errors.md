---
title: Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)
issue: TB-023
status: draft            # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-023 — Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)

Lát 3 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`). Làn lớn: spec và design `confirmed` ở TB-007; hợp đồng lỗi công khai và hành vi YARP chưa kiểm (design mục 16). Ảnh chụp trạng thái branch: `feature/TB-023-gateway-downstream-errors`, tạo từ `main` `5b7266a` (đã gồm TB-010, TB-021, TB-022; PR 15 đã merge), working tree sạch trước khi soạn plan.

Đã có (đã đọc code): `src/gateway/Program.cs` (`AddServiceDefaults`, `AddReverseProxy().LoadFromConfig`, `UseCorrelation`, `UseTeamboardExceptionHandler`, `MapDefaultEndpoints`, `MapReverseProxy()` không callback, `MapFallback`), `GatewayErrorCodes` (chỉ `ROUTE_NOT_FOUND`), `appsettings.json` (hai route `boards-read` và `boards-write`, cluster `board-service` một đích `d1`, chưa có `Timeout`/`ActivityTimeout`), `src/shared/Http` (`ErrorDefinition`, `ProblemFactory`, `SharedErrors`, correlation, handler `500`), `src/gateway.Tests` (`UpstreamStub` Kestrel cổng ngẫu nhiên, `GatewayContext` với `StartGateway`, `DockerCli`, `ComposeSteps`). Hiện lỗi kết nối tới upstream do YARP tự xử lý (status mặc định, thân rỗng, không `problem+json`).

## 1. Phạm vi của lát

**Làm:**
- Mã lỗi mới trong `GatewayErrorCodes` (hợp đồng công khai, DA-02): `UPSTREAM_UNAVAILABLE` (503), `UPSTREAM_BAD_GATEWAY` (502), `UPSTREAM_TIMEOUT` (504); mở rộng `GatewayErrorCodesTests` (không trùng mã, status, message không lộ nội bộ).
- Middleware trong pipeline proxy của gateway (`MapReverseProxy(pipeline => ...)`, tự thêm lại các middleware mặc định của YARP) đọc `IForwarderErrorFeature` sau khi forwarder chạy và, **khi response chưa bắt đầu**, ghi `problem+json` theo bảng ánh xạ (mục 3, B1): không có đích khả dụng hoặc không kết nối được → `503` kèm `Retry-After`; lỗi giao thức hoặc upstream ngắt trước khi trả header → `502`; quá timeout trước header → `504`. Phản hồi không lộ host, cổng, tên nội bộ hay stack trace; có `code`, `traceId`, `correlationId`.
- Timeout cấu hình được (NFR-003): `Timeout` tổng 30 giây ở mỗi route và `HttpRequest.ActivityTimeout` ≤ 30 giây ở cluster trong `appsettings.json`; bật `AddRequestTimeouts()` và `UseRequestTimeouts()` (cần cho `Timeout` của route). Giá trị đọc từ cấu hình, test ghi đè bằng giá trị ngắn.
- `Retry-After` trên `503`: mặc định 5 giây, cấu hình được qua Options có kiểm tra giá trị (`api-endpoints.md` §6; giả định DA-05).
- Lỗi **sau khi đã gửi header**: gateway ngắt kết nối với client (`HttpContext.Abort()`), ghi log mức Warning trong scope correlation và tăng counter `gateway_upstream_aborted` (design D4, mục 9); không đổi status đã gửi.
- Test: host test in-process với upstream giả (`gateway.Tests`), một test Docker (gateway chạy riêng trong compose, `board-service` không tồn tại, DNS không phân giải được); `.feature` ở `document/spec/features/gateway/`.

**Không làm (trong lát này):** retry và circuit breaker (spec mục 4; ghi nợ ở design D4), `ForwardedHeaders` và rate limit (lát 4), body 1 MiB (lát 5), CORS (lát 6), metric độ trễ thêm vào, metric theo route/status, retention log, graceful shutdown (lát 7), smoke với `board-service` thật (lát 8), `identity-service` hay `board-service`, health `ready` phụ thuộc upstream (spec: không phụ thuộc).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-006 không kết nối được → `503` `problem+json` kèm `Retry-After` | D4, bảng mã mục 6 | B1 | @S01 | Host test: upstream giả đã dừng (cổng đóng) → 503, header `Retry-After`, `code`, `traceId`, `correlationId` |
| FR-006 không có đích khả dụng → `503` | D4 | B1 | @S02 | Host test: cluster không có destination → 503 cùng dạng |
| FR-006 lỗi giao thức / ngắt trước header → `502` | D4 | B1 | @S03 | Host test: upstream giả đóng socket trước khi trả header → 502 |
| FR-006, NFR-003 quá timeout trước header → `504` | D4 | B2 | @S04, @S05 | Host test: upstream treo; ghi đè `Timeout` ngắn (S04) và `ActivityTimeout` ngắn (S05) → 504 |
| FR-006 không lộ host nội bộ hay stack trace | D4, mục 8 | B1, B2 | @S01–@S05 (bước chung) | Mọi phản hồi lỗi: không chứa địa chỉ upstream, tên service, tên exception; allowlist thuộc tính |
| FR-006 header đã gửi rồi mới ngắt/treo → ngắt kết nối + log + metric | D4, mục 9 | B3 | @S06, @S07 | Host test: upstream trả header rồi đóng hoặc treo; client nhận lỗi đọc thân; `MeterListener` thấy `gateway_upstream_aborted`; log Warning có `correlationId` |
| NFR-003 timeout cấu hình được, không hardcode | D4 | B2, B4 | @S04, @S05, @S08 | Test dùng giá trị ghi đè ngắn qua cấu hình; test đọc `appsettings.json` thật: 30 giây |
| `Retry-After` cấu hình được | D4, `api-endpoints.md` §6 | B1 | @S01, @S08 | Ghi đè 7 giây → header `7`; giá trị không hợp lệ làm khởi động thất bại |
| Gateway sống khi upstream chết (spec FR-002) | spec | B4 | @S09 | `/health/live` và `/health/ready` 200 khi upstream đã dừng |
| FR-006 không kết nối được trong môi trường thật | D4 | B4 | @S10 | `@docker`: gateway chạy riêng, `GET /api/v1/boards` → 503 `UPSTREAM_UNAVAILABLE` (tên `board-service` không phân giải được) |

Kịch bản (ID duy nhất trong file `.feature`; viết ở test-init):
- @S01 (Outline) Upstream đã dừng: `GET`, `HEAD`, `POST` đều nhận `503` `UPSTREAM_UNAVAILABLE` kèm `Retry-After` mặc định 5.
- @S02 Cluster không có đích khả dụng: `503` `UPSTREAM_UNAVAILABLE`.
- @S03 Upstream đóng kết nối trước khi trả header: `502` `UPSTREAM_BAD_GATEWAY`.
- @S04 Upstream treo vượt `Timeout` của route: `504` `UPSTREAM_TIMEOUT`.
- @S05 Upstream im lặng vượt `ActivityTimeout` của cluster (ngắn hơn `Timeout`): `504` `UPSTREAM_TIMEOUT`.
- @S06 Upstream trả header rồi đóng kết nối giữa chừng: client bị ngắt, metric `gateway_upstream_aborted` tăng 1, có log Warning mang `correlationId`.
- @S07 Upstream trả header rồi treo vượt timeout: client bị ngắt, metric tăng.
- @S08 Cấu hình: timeout mặc định trong `appsettings.json` là 30 giây (route và cluster); `Retry-After` đọc từ cấu hình.
- @S09 `/health/live` và `/health/ready` trả 200 khi upstream đã dừng.
- @S10 (`@docker`) Gateway chạy riêng trong compose, `GET /api/v1/boards` trả `503` `UPSTREAM_UNAVAILABLE`.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng (stub chỉ chuyển tiếp, không hành vi) chỉ để test biên dịch được. **Spike ở test-init** (kiểm các giả định A-01..A-04, mục 7) trước khi viết test cuối; giả định sai thì dừng và hỏi.

**Bảng ánh xạ dự kiến (kiểm ở spike, không ghi như đã chốt):** `NoAvailableDestinations` → 503; `Request` mà ngoại lệ gốc là lỗi kết nối (`SocketException` từ chối kết nối, không phân giải tên; `HttpRequestError.ConnectionError`/`NameResolutionError`) → 503; `Request` khác (lỗi giao thức, đóng trước header), `ResponseHeaders` → 502; `RequestTimedOut` hoặc huỷ do `Timeout` của route → 504; lỗi sau khi response đã bắt đầu (`ResponseBody*`, `ResponseHeaders` sau khi bắt đầu) → ngắt kết nối; `RequestCanceled`/`*Client` do client huỷ → không ghi response (câu hỏi mở P-02).

### B1. Hợp đồng lỗi và ánh xạ 503 / 502 (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/downstream-errors.feature` (@S01 Outline, @S02, @S03) cộng unit test `GatewayErrorCodesTests` (ba mã mới: mã, status, message không lộ nội bộ, không trùng); `UpstreamStub` thêm chế độ đóng socket trước header (S03) và `GatewayContext` hỗ trợ cluster không có đích (S02). Đỏ vì chưa có mã và chưa có middleware (hiện YARP trả status mặc định, thân rỗng).
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs`; mới `src/gateway/UpstreamErrorMiddleware.cs` (đọc `IForwarderErrorFeature`, phân loại, ghi `problem+json`); mới `src/gateway/UpstreamOptions.cs` (`Gateway:Upstream:RetryAfterSeconds`, kiểm tra khoảng giá trị khi khởi động); `src/gateway/Program.cs` (`MapReverseProxy(pipeline => ...)` kèm lại middleware mặc định của YARP); `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `503`/`502` `problem+json` công khai; header `Retry-After` trên `503`.
- **Migration / cấu hình:** khóa `Gateway:Upstream:RetryAfterSeconds` (mặc định 5) trong `appsettings.json`.
- **Tài liệu cần cập nhật:** `api-endpoints.md` nếu bảng mã lỗi chung cần dòng mã mới của gateway (kiểm ở plan-review; design mục 6 đã có bảng).
- **Kiểm chứng:** @S01–@S03 xanh; test lát 1–2 (route, health, 404, correlation, 500) vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Timeout và ánh xạ 504
- **Kịch bản/test sẽ viết (đỏ trước):** @S04 và @S05 trong `downstream-errors.feature`; `UpstreamStub` thêm chế độ treo (không trả header) có thể huỷ để dọn; test ghi đè `Timeout` (route) hoặc `ActivityTimeout` (cluster) bằng giá trị ngắn qua cấu hình. Đỏ vì chưa có cấu hình timeout và chưa ánh xạ `504`.
- **File/module dự kiến chạm:** `src/gateway/appsettings.json` (`Timeout` mỗi route `00:00:30`, `HttpRequest.ActivityTimeout` của cluster `00:00:30`), `src/gateway/Program.cs` (`AddRequestTimeouts`, `UseRequestTimeouts` đặt sau handler và trước YARP; thứ tự đích cập nhật trong comment, câu hỏi mở P-01), `src/gateway/UpstreamErrorMiddleware.cs` (nhánh `504`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `504` `problem+json` công khai.
- **Migration / cấu hình:** hai giá trị timeout trong cấu hình (NFR-003); không biến môi trường mới.
- **Tài liệu cần cập nhật:** design mục 6.4 (thứ tự middleware thêm `RequestTimeouts`) nếu P-01 chốt vị trí mới; không sửa design khi chưa được người dùng chốt.
- **Kiểm chứng:** @S04, @S05 xanh (mỗi test dưới ~5 giây nhờ giá trị ngắn); không `sleep` cố định; chạy lặp 10 lần nhóm host test của `gateway.Tests` để bắt flaky.
- **Phụ thuộc:** B1.

### B3. Lỗi sau khi đã gửi header: ngắt kết nối, log và metric
- **Kịch bản/test sẽ viết (đỏ trước):** @S06, @S07; `UpstreamStub` thêm chế độ trả header rồi đóng hoặc treo; test đọc thân từ client và kỳ vọng lỗi đọc/ngắt; `MeterListener` bắt `gateway_upstream_aborted`; `ILoggerProvider` bắt log Warning kèm scope `correlationId`. Đỏ vì chưa có counter và chưa ngắt kết nối có chủ đích.
- **File/module dự kiến chạm:** mới `src/gateway/GatewayMetrics.cs` (`Meter` và counter `gateway_upstream_aborted`), `src/gateway/UpstreamErrorMiddleware.cs` (nhánh response đã bắt đầu: log, tăng counter, `Abort()`), `src/gateway/Program.cs` (đăng ký meter vào OpenTelemetry bằng `AddOpenTelemetry().WithMetrics(m => m.AddMeter(...))`; không sửa `ServiceDefaults`), `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** không đổi HTTP; thêm metric nội bộ (không phải hợp đồng công khai).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (design mục 9 đã mô tả metric).
- **Kiểm chứng:** @S06, @S07 xanh; log không chứa `Authorization`, body hay địa chỉ upstream.
- **Phụ thuộc:** B1 (middleware), B2 (timeout cho S07).

### B4. Cấu hình, sức khoẻ khi upstream chết và test Docker
- **Kịch bản/test sẽ viết (đỏ trước):** @S08 (đọc `appsettings.json` thật), @S09, @S10 (`@docker`, tái dùng `ComposeSteps`/`DockerCli` với tên dự án riêng `tb023-tests`, chạy riêng service `gateway`). Có thể xanh sẵn (S09) nếu hành vi đã đúng từ lát 1: ghi vào TDD log là test hồi quy.
- **File/module dự kiến chạm:** `src/gateway.Tests/` (step, feature), `document/spec/features/gateway/`, `COMMANDS.md` (nhắc nhóm docker mỗi lần một assembly; đã có từ TB-022, chỉ kiểm).
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** bảng lát (lát 3 → `done` khi merge), `document/issues/README.md`.
- **Kiểm chứng:** @S08–@S10 xanh (S10 cần Docker Desktop, cổng 8080 trống); các test Docker khác (TB-001, TB-010, TB-021, TB-022) vẫn xanh khi chạy từng assembly; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B1–B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Bảng mã lỗi gateway; kiểm tra `UpstreamOptions` | xUnit thuần | Không mock |
| Integration (host in-process) | Ánh xạ 503/502/504, `Retry-After`, ngắt sau header, metric, log (`gateway.Tests`) | `WebApplicationFactory<Program>` + `UpstreamStub` Kestrel cổng ngẫu nhiên có chế độ lỗi (dừng, đóng socket, treo, header rồi đóng/treo); cấu hình ghi đè bằng `UseSetting` | Không mock YARP; upstream giả là socket thật để lỗi mạng thật |
| Integration Docker (nhóm `docker`) | `503` trong môi trường thật (DNS không phân giải) @S10 | Compose chính, chỉ chạy `gateway`, tên dự án riêng | Gọi `docker compose` qua Process; tự bỏ qua khi không có Docker |

Kim tự tháp: nhiều host test nhanh, một test Docker. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`). Test BDD viết tiếng Anh, ASCII (`test-review.md` mục 1).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Mã trạng thái và `IForwarderErrorFeature` của YARP khác giả định (design mục 16 đã cảnh báo) | Ánh xạ sai hoặc không phân biệt được 503/502 | Spike ở test-init cho từng loại lỗi; bảng ánh xạ dự kiến ở B1 chỉ chốt sau spike; sai thì dừng và hỏi | Revert B1 |
| `MapReverseProxy(callback)` bỏ các middleware mặc định (load balancing, affinity, health) | Mất cân bằng tải/`NoAvailableDestinations` | Thêm lại đúng thứ tự mặc định trong callback; test @S02; đọc mã nguồn YARP 2.3.0 khi viết | Revert B1 |
| `Timeout` theo route cần `UseRequestTimeouts` và ngoại lệ huỷ không ra `RequestTimedOut` | `504` không xuất hiện | Spike @S04; phương án dự phòng: phân loại theo `IHttpRequestTimeoutFeature`/trạng thái huỷ trong middleware | Revert B2 |
| Test timeout/treo flaky hoặc chậm | CI chậm, đỏ oan | Giá trị timeout ngắn (≈1 giây) qua cấu hình, chờ có điều kiện, không `sleep`; chạy lặp 10 lần | Sửa test, không nới điều kiện |
| `Abort()` sau header khác nhau giữa TestServer, Kestrel thật | S06/S07 xanh giả ở TestServer | Upstream giả và gateway chạy Kestrel thật trong host test (đã là cách của `UpstreamStub`); client đọc thân bằng `HttpClient` thật nếu `TestServer` che lỗi | Sửa test |
| Test Docker chồng cổng với assembly khác | Đỏ oan | Chạy nhóm docker mỗi lần một assembly (`COMMANDS.md`) | Không áp dụng |
| Counter không có `correlationId` (metric không mang cardinality cao) | Khó lần theo từng lần ngắt | Id nằm ở log Warning, metric chỉ đếm (đúng thực hành OTel: tránh nhãn có cardinality cao) | Không áp dụng |

Ảnh hưởng consumer (service khác): thêm `503`/`502`/`504` `problem+json` và `Retry-After` ở contract công khai của gateway (cộng thêm, trước đây là thân rỗng); client Angular chưa tồn tại (ADR 0008); `board-service` không đổi. Bảo mật: lỗi không lộ host/cổng/tên nội bộ hay stack trace (STRIDE design mục 8); log không ghi header `Authorization`, cookie hay body; không secret mới. Chi phí: không tài nguyên mới; một test Docker (build image gateway, vài chục giây).

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `security`, `workflow`, `testing`): áp dụng bài học query string lọt vào log (không log URL đầy đủ ở dòng lỗi) và LL-005 (câu hỏi mở kèm lựa chọn và khuyến nghị).

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"` rồi nhóm `docker` mỗi lần một assembly; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ).
- Tiêu chí hoàn thành: theo issue TB-023.
- Tài liệu/config cập nhật: như B1–B4; bảng lát (lát 3 → `done` khi merge), `document/issues/README.md`.

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở spike test-init; sai thì dừng và hỏi): A-01 forwarder của YARP 2.3.0 đặt `IForwarderErrorFeature` cho lỗi kết nối và timeout, và middleware tuỳ chỉnh trong pipeline proxy đọc được sau `next()` khi response chưa bắt đầu (chưa kiểm); A-02 `NoAvailableDestinations` trả `503` ở middleware load balancing, có thể bắt bằng status hoặc feature (chưa kiểm); A-03 `Timeout` của route (cần `UseRequestTimeouts`) và `ActivityTimeout` đều cho `RequestTimedOut`/trạng thái huỷ phân biệt được để ra `504` (chưa kiểm); A-04 `HttpContext.Abort()` sau khi header đã gửi làm client thấy lỗi đọc thân trên Kestrel thật (chưa kiểm); A-05 `HttpRequestException` từ chối kết nối/không phân giải tên có thể phân biệt với lỗi giao thức qua `HttpRequestError` hoặc `SocketException` (chưa kiểm). Nguồn đã đọc: tài liệu YARP về timeouts (`Timeout`, `ActivityTimeout` mặc định 100 giây, cần `UseRequestTimeouts`) và direct forwarding (`IForwarderErrorFeature`, forwarder đổi lỗi thành 5xx hoặc ngắt response, không có retry).

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-01 | Vị trí `UseRequestTimeouts` trong thứ tự middleware (design 6.4 chưa liệt kê). Lựa chọn: (a) sau `ExceptionHandler`, trước YARP, ghi vào design 6.4; (b) trong pipeline proxy ngay trước forwarder. Khuyến nghị (a): cùng chỗ mọi middleware có policy theo route, dễ đọc. Hậu quả nếu chưa chốt: B2 không viết được. | B2, design 6.4 | sondv | open |
| P-02 | Client huỷ request (đóng kết nối) khi upstream đang xử lý: không ghi response và chỉ log mức Debug/Information, hay trả mã riêng (499 không chuẩn)? Khuyến nghị: không ghi response, log Information, không tính vào metric lỗi. Spec không nêu. | B1 | sondv | open |
| P-03 | Lỗi forwarder không thuộc bảng (ví dụ `RequestBodyDestination`, `UpgradeRequest*`) trước header: `502` hay `500 INTERNAL_ERROR`? Khuyến nghị `502` (lỗi do phía upstream/giao thức) kèm log chi tiết. | B1 | sondv | open |
| P-04 | Tên và nhãn metric: design ghi `gateway_upstream_aborted`; chuẩn OTel thường dùng dấu chấm. Giữ nguyên tên của design, nhãn có hay không (route, loại lỗi)? Khuyến nghị giữ tên của design, không nhãn ở lát này (nhãn thêm ở lát 7). | B3 | sondv | open |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-023 (lát 3 của gateway) |
