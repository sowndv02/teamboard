---
title: Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json
issue: TB-022
status: draft            # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway, shared/Http]
---

# Plan: TB-022 — Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json

Lát 2 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`). Làn lớn: spec và design `confirmed` ở TB-007. Ảnh chụp trạng thái branch: `feature/TB-022-gateway-correlation-error-handling`, tạo từ `origin/main` `8716854` (đã gồm TB-010 và TB-021), working tree sạch trước khi tạo issue. Đã có: `src/gateway` (YARP, route boards, health, `404 problem+json`), `src/shared/Http` (`ErrorDefinition`, `ProblemFactory` với `correlationId` tạm bằng `traceId`, `SharedErrors` gồm `INTERNAL_ERROR`), `src/shared/ServiceDefaults` (OTel, health), `gateway.Tests` (upstream giả bằng Kestrel, `WebApplicationFactory<Program>`, nhóm `docker`), `Http.Tests` (unit), `docker-compose.observability.yml` (OTLP mới chỉ nối cho `board-service`). Compose chính đòi biến môi trường; các `DockerCli` đã dùng `--env-file src/.env.example` (TB-021).

## 1. Phạm vi của lát

**Làm:**
- `src/shared/Http`: middleware correlation: nhận `X-Correlation-Id` hợp lệ (`A-Z a-z 0-9 . _ -`, 1–64 ký tự) thì giữ nguyên; thiếu, rỗng, nhiều giá trị, ký tự lạ hoặc dài hơn 64 thì tạo mới (GUID dạng chuỗi); ghi đè header của request (để YARP chuyển giá trị đã kiểm xuống upstream); đặt vào response (`Response.OnStarting`, để lỗi vẫn có); gắn tag `correlationId` cho `Activity` và log scope (design D5, FR-003).
- `ProblemFactory` lấy `correlationId` từ middleware (fallback `traceId` khi không có, để thư viện dùng được ngoài pipeline).
- ExceptionHandler dùng chung: lỗi chưa lường trả `500 problem+json` mã `INTERNAL_ERROR` (đã có trong `SharedErrors`), không lộ chi tiết nội bộ, vẫn có `X-Correlation-Id`, ghi log lỗi kèm correlation (design 6.3, 6.4; người dùng chốt gán vào lát 2).
- `src/gateway`: thứ tự middleware `Correlation` → `ExceptionHandler` → (health, YARP, fallback) theo design 6.4.
- `traceparent`: gateway chuyển xuống upstream, span của gateway nối vào trace của client (FR-003, NFR-002).
- `docker-compose.observability.yml`: nối OTLP cho `gateway`; kiểm trace gateway → board-service trong Jaeger.
- Test: unit (`Http.Tests`), host test và test Docker (`gateway.Tests`); `.feature` ở `document/spec/features/gateway/`.

**Không làm (trong lát này):** timeout, `502/503/504` (lát 3), rate limit, `ForwardedHeaders` (lát 4), body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention, graceful shutdown (lát 7), correlation ở phía `board-service` (service tự thêm sau), smoke với endpoint boards thật (lát 8).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-003 tạo/giữ `X-Correlation-Id`, trả trong response, ghi đè xuống upstream | D5, 6.4 | B1, B2 | @S01, @S02 | Unit middleware; host test với upstream giả ghi header nhận được |
| FR-003 `traceparent` truyền xuống, span nối cùng trace | 6.4, 9 | B2 | @S03 | Upstream giả ghi `traceparent`; so trace id với client |
| FR-001 `404 problem+json` mang `correlationId` thật | 6.3, D5 | B1, B2 | @S04 | Host test: `correlationId` trong body bằng header response |
| Lỗi chưa lường → `500 INTERNAL_ERROR` (design 6.3; gán lát 2) | 6.3, 6.4 | B1, B2 | @S05 | Unit handler; host test thay `IHttpForwarder` bằng bản ném lỗi |
| NFR-002 trace xuyên gateway → service trong Jaeger | 9 | B3 | @S06 | Test Docker: một trace có span `gateway` và `board-service` |
| NFR-004 cấu hình, không hardcode | D3 | B3 | @S06 | Biến OTLP qua env ở compose; không secret |
| NFR-006 không log token/body, không lộ nội bộ | D5, 8 | B1 | @S05 | Handler không đưa exception vào body; log chỉ kèm correlation |
| Gate xanh (tiêu chí issue) | – | B1–B3 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất trong từng file, tag `@TB-022 @gateway`):
- @S01 (Outline) Request không có `X-Correlation-Id`, hoặc có id hợp lệ (gồm độ dài 64): upstream và response cùng nhận một id; id hợp lệ giữ nguyên, thiếu thì là id mới.
- @S02 (Outline) Id không hợp lệ (khoảng trắng, `;`, dấu `<`, dài 65 ký tự, rỗng, hai giá trị): gateway thay bằng id mới; upstream và response nhận id mới, không phải giá trị gốc.
- @S03 Request có `traceparent`: upstream nhận `traceparent` cùng trace id với client, span id khác (span của gateway).
- @S04 Request tới path lạ nhận `404 problem+json` có `correlationId` bằng `X-Correlation-Id` của response (cả khi client tự gửi id); `/health/live` cũng trả header này.
- @S05 Lỗi chưa lường trong pipeline trả `500 problem+json` mã `INTERNAL_ERROR`, có `X-Correlation-Id` và `correlationId`, body không chứa tên exception hay stack trace.
- @S06 (`@docker`) Compose chạy `gateway`, `board-service` và `jaeger`: một request qua gateway tạo một trace trong Jaeger có span của `gateway` và `board-service`.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff).

### B1. Thư viện: correlation, `ProblemFactory` và ExceptionHandler
- **Kịch bản/test sẽ viết (đỏ trước):** unit test trong `Http.Tests` (host in-process bằng `TestServer`): luật id (giữ nếu hợp lệ; thay nếu thiếu, rỗng, nhiều giá trị, ký tự lạ, 65 ký tự; id mới là GUID hợp lệ theo luật), header response có mặt cả với lỗi, `Activity` có tag `correlationId`, log scope có `correlationId`; `ProblemFactory` dùng `correlationId` của context; ExceptionHandler trả `500` `INTERNAL_ERROR` không lộ exception và vẫn có header. Đỏ vì chưa có middleware (khung ném `NotImplementedException`).
- **File/module dự kiến chạm:** `src/shared/Http/` (`CorrelationMiddleware`, `CorrelationExtensions` (`UseCorrelation`), `CorrelationHeaders`/`CorrelationId`, `ErrorHandlingExtensions` (`UseTeamboardExceptionHandler`)), `ProblemFactory.cs`, `src/shared/Http.Tests/` (thêm `Microsoft.AspNetCore.TestHost` 10.0.13, T0, ghim chính xác), `config/dependencies.yaml`.
- **Hợp đồng thay đổi:** header `X-Correlation-Id` theo `api-endpoints.md` mục 6; `problem+json` giữ dạng cũ, `correlationId` đổi nguồn; mã `INTERNAL_ERROR` đã có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (hợp đồng đã có ở `api-endpoints.md`).
- **Kiểm chứng:** test thư viện xanh; `dotnet build -warnaserror`; `git diff` không chứa business logic trong `src/shared` (`PROJECT.md`).
- **Phụ thuộc:** không. Câu hỏi mở P-01 (nhiều giá trị header) phải chốt trước.

### B2. Nối vào gateway và kiểm bằng host test (@S01–@S05)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/correlation.feature` (@S01, @S02, @S03, @S04) và `error-handling.feature` (@S05); `UpstreamStub` ghi thêm header nhận được (`X-Correlation-Id`, `traceparent`); @S05 thay `IHttpForwarder` bằng bản ném lỗi qua `ConfigureTestServices` (giả định A-03). Đỏ vì gateway chưa gọi middleware.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`UseCorrelation`, `UseTeamboardExceptionHandler`, ghi chú thứ tự đích cập nhật), `src/gateway.Tests/` (step, `UpstreamStub`, `GatewayErrorCodes` không đổi), `document/spec/features/gateway/`.
- **Hợp đồng thay đổi:** mọi response của gateway có `X-Correlation-Id`; request tới upstream mang id đã kiểm (không bao giờ là giá trị thô của client); `problem+json` do gateway tạo có `correlationId` khớp header.
- **Migration / cấu hình / biến môi trường:** không có.
- **Tài liệu cần cập nhật:** chuyển sang B3.
- **Kiểm chứng:** @S01–@S05 xanh; test lát 1 (route, health, 404) vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`; chạy lặp 10 lần nhóm test nhanh.
- **Phụ thuộc:** B1.

### B3. Trace xuyên service trong Jaeger (@S06)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/tracing.feature` (@S06, `@docker`): chạy compose chính + observability với `gateway`, `board-service`, `jaeger` (tên dự án riêng, `down` không `-v`), gọi `GET /api/v1/boards` ở cổng 8080 (board-service trả 404 vì chưa có endpoint, vẫn tạo span), rồi truy vấn API v3 của Jaeger tìm trace có cả hai service. Đỏ vì gateway chưa nối OTLP.
- **File/module dự kiến chạm:** `src/docker-compose.observability.yml` (env OTLP cho `gateway`), `src/gateway.Tests/` (step, helper Jaeger dùng lại mẫu của TB-001), `COMMANDS.md`, `PROJECT.md` nếu cần.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình / biến môi trường:** `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` cho `gateway` ở file observability; không secret.
- **Tài liệu cần cập nhật:** `COMMANDS.md`, bảng lát (lát 2 → `done` khi merge), `document/issues/README.md`.
- **Kiểm chứng:** @S06 xanh (cần Docker Desktop, cổng 6060, 8080, 16686 trống); các test Docker khác (TB-001, TB-010, TB-021) vẫn xanh; `docker compose -f src/docker-compose.yml -f src/docker-compose.observability.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B2.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit (host in-process nhỏ) | Luật id, middleware, handler, `ProblemFactory` (`Http.Tests`) | `TestServer` với pipeline tối thiểu | Không mock ASP.NET Core |
| Integration (host in-process) | Gateway: correlation, `traceparent`, 404, 500 (`gateway.Tests`) | `WebApplicationFactory<Program>` + upstream giả Kestrel cổng ngẫu nhiên; @S05 thay `IHttpForwarder` | Không mock YARP ngoài điểm thay để ép lỗi chưa lường |
| Integration Docker (nhóm `docker`) | Trace xuyên `gateway` → `board-service` trong Jaeger (@S06) | Docker local, tên dự án riêng | Gọi `docker compose` qua Process; tự bỏ qua khi không có Docker |

Kim tự tháp: nhiều unit/host test nhanh; một test Docker. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Thay `IHttpForwarder` không ép được lỗi chưa lường (A-03) | @S05 không viết được | Spike nhỏ ở bước test-init; phương án thay: dựng host riêng trong `Http.Tests` và kiểm thứ tự middleware ở gateway; hỏi người dùng nếu phải đổi | Revert B2 |
| `ExceptionHandler` xoá header đã đặt (đã nêu ở design 6.4) | Lỗi thiếu `X-Correlation-Id` | Đặt header bằng `OnStarting` và kiểm @S05 | Sửa thứ tự middleware |
| YARP không truyền `traceparent` như tài liệu (A-02) | @S03 và @S06 đỏ | Kiểm ở test-init; không tự viết propagator nếu YARP đã có, hỏi người dùng nếu lệch | Revert B2 |
| Trace trong Jaeger chậm xuất hiện làm @S06 flaky | Test đỏ oan | Chờ có điều kiện, hạn 90 giây (như TB-001) | Sửa test, không nới điều kiện |
| Ghi đè header request làm mất header gốc cần cho debug | Khó điều tra id lạ của client | Chấp nhận (design D5); log ghi cảnh báo mức thấp khi thay id, không ghi giá trị gốc (tránh log injection) | Không áp dụng |

Ảnh hưởng consumer (service khác): `board-service` nhận thêm `X-Correlation-Id` và `traceparent` (header mới, tương thích ngược); contract công khai thêm header `X-Correlation-Id` ở mọi response gateway. Bảo mật: luật id chặn log injection và id quá dài (STRIDE design mục 8); lỗi 500 không lộ nội bộ; không secret. Chi phí: chỉ local.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `security`, `workflow`): bài học từ review TB-001 (query string lọt vào log) áp dụng: không ghi giá trị gốc của id không hợp lệ vào log; LL-005 áp dụng cho câu hỏi mở.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx` (gồm nhóm `docker`); `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài); `git status --short && git diff --stat`.
- Tiêu chí hoàn thành: theo issue TB-022.
- Tài liệu/config cập nhật: như B3; bảng lát (lát 2 → `done`), `document/issues/README.md`.

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở bước tương ứng; sai thì dừng và hỏi): A-01 `Activity.Current` do ServiceDefaults/ASP.NET Core có trong pipeline gateway nên tag `correlationId` gắn được (B1, B2); A-02 YARP mặc định truyền `traceparent` qua `DistributedContextPropagator` (đã nêu ở design 6.3, kiểm ở @S03); A-03 `ConfigureTestServices` thay được `IHttpForwarder` để ép lỗi chưa lường (B2).

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Header `X-Correlation-Id` xuất hiện nhiều lần trong một request | B1, @S02 | sondv | open |

### P-01 Nhiều giá trị `X-Correlation-Id`
- **Lựa chọn:** A) coi là không hợp lệ và tạo id mới (một request chỉ có đúng một id; không chọn thay client); B) lấy giá trị đầu tiên nếu hợp lệ; C) ghép các giá trị.
- **Khuyến nghị:** A, vì spec chỉ quy định id hợp lệ thì giữ, còn nhiều giá trị là đầu vào mơ hồ, tạo mới là cách an toàn nhất và dễ kiểm. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** luật id ở B1 và hàng "hai giá trị" của @S02 chưa viết được.
- **Mặc định nếu quá hạn:** không dùng mặc định (hành vi của hợp đồng công khai).

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-022 (lát 2 của gateway) |
