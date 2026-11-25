---
title: Gateway lát 7, quan sát và vận hành
issue: TB-027
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-11-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-027 — Gateway lát 7, quan sát và vận hành

Lát 7 trong [`api-gateway-slices.md`](api-gateway-slices.md), phụ thuộc lát 2–4 theo bảng lát (TB-022..TB-024 đã `done` trên `main`; lát 5–6 đã merge). Làn vừa–lớn (ước lượng bảng lát 1–2 ngày): thêm access log an toàn, mở rộng metric, cấu hình vận hành compose, graceful shutdown, cảnh báo vận hành; không đổi hợp đồng API công khai.

Ảnh chụp trạng thái branch (2026-11-25): nhánh `feature/TB-027-gateway-observability-ops` tạo từ `origin/main` (`c0caa06`, gồm PR #20 TB-026). Working tree sạch trước khi ghi plan.

Đã có (đã đọc code trên `main` `c0caa06`):
- `src/gateway/Program.cs`: `AddServiceDefaults("gateway")`, OTel metrics với `GatewayMetrics.MeterName`; pipeline đủ lát 1–6 (ForwardedHeaders → … → CORS → RateLimiter → BodyLimit → YARP).
- `src/gateway/GatewayMetrics.cs`: chỉ counter `gateway_upstream_aborted` (TB-023); **chưa có** histogram độ trễ, counter theo route/status/`429`/`502`/`503`/`504`, số kết nối, thời gian tới `ready`.
- `src/gateway/UpstreamErrorMiddleware.cs`: `LoggerMessage` partial cho lỗi upstream; **không** log từng request hoàn tất; log hiện tại **không** chứng minh NFR-006 (có/không `Authorization`/body).
- **Chưa có** middleware access log; **chưa** `HostOptions.ShutdownTimeout` / Kestrel graceful; `src/docker-compose.yml` service `gateway` **không** có `stop_grace_period: 35s`, **không** `logging` driver giới hạn; `src/docker-compose.observability.yml` Jaeger **không** cấu hình TTL 7 ngày.
- Test: `src/gateway.Tests` Reqnroll + `GatewayContext` (upstream giả, `X-Test-Remote-Ip`, `FakeLogCollector` hoặc pattern tương tự TB-023 @S06–@S07 nếu có — hiện dùng `ILogger` provider trong downstream tests). `document/spec/features/gateway/` có các feature lát 1–6; **chưa có** feature quan sát/vận hành.

## 1. Phạm vi của lát

**Làm:**
- **Access log (NFR-006):** mỗi request hoàn tất ghi log có cấu trúc với `traceId`, `correlationId`, method, route/path, status, **IP client** (từ `Connection.RemoteIpAddress` sau `ForwardedHeaders`, design mục 9); **không** ghi `Authorization`, cookie, body (spec NFR-006, design STRIDE).
- **Metric (NFR-001, design mục 9):** bổ sung ít nhất histogram **thời gian xử lý request** (`gateway_request_duration_seconds`) và histogram **độ trễ thêm vào của gateway** (tổng thời gian client thấy trừ thời gian chờ upstream, tên meter theo quy ước dự án); counter `429`, `502`/`503`/`504` (có thể gộp hoặc tách — xem P-02); giữ `gateway_upstream_aborted`. Metric **không** gắn nhãn `correlationId` (TB-023).
- **Kiểm chứng NFR-006 bằng test:** request mang `Authorization` và body → log có IP + `correlationId`, không chứa giá trị header/body.
- **Graceful shutdown (design 6.4, DA-05):** `ShutdownTimeout` ≥ 30 giây; compose `stop_grace_period: 35s` cho service `gateway`.
- **Lưu log 7 ngày (local, NFR-006):** `logging` driver Docker cho `gateway` (`max-size` + `max-file` ước lượng retention ~7 ngày ở tải dev); ghi rõ giới hạn Jaeger local (P-03).
- **Cảnh báo vận hành:** tài liệu nổi bật “không expose gateway ra internet trước khi có kiểm JWT” (spec mục 10, design mục 8).
- **Đồng bộ LL-006:** lát 6 / TB-026 → `done` sau merge PR #20; lát 7 → `planning`; TB-027 → `in-progress`.

**Không làm (trong lát này):**
- Kiểm JWT, WAF, TLS/ALB (giai đoạn 4).
- Terraform CloudWatch retention, cảnh báo Prometheus/Grafana (design mục 9 “đề xuất khi có hạ tầng”).
- Benchmark tải / chứng minh p95 &lt; 20 ms ở production (NFR-001 chỉ yêu cầu **đo được** bằng histogram ở dev).
- Metric/log riêng cho CORS (đã tách sang lát 6).
- Smoke `board-service` thật (lát 8).
- Sửa hành vi routing, rate limit, CORS, body limit.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| NFR-006 log có IP + `correlationId`, không `Authorization`/body | Mục 9, STRIDE | B1 | @S01, @S02 | Host test: gửi `GET`/`POST` với `Authorization` + body; thu log qua test provider; assert fields + redaction |
| NFR-006 retention log local ≤ 7 ngày | Mục 9, spec Q-04 | B3 | @S05 | Đọc `docker-compose.yml`: `logging` options gateway; hoặc test parse YAML (unit) |
| NFR-001 đo độ trễ thêm vào | Mục 9, DA-01 | B2 | @S03 | Host test: `MeterListener` / exporter test nhận histogram `gateway_*`; upstream giả trễ có kiểm soát |
| Metric lỗi gateway đếm được | Mục 9 | B2 | @S04 | Sau request `429` hoặc `503`: counter tăng (không nhãn correlation) |
| Graceful shutdown | 6.4, DA-05 | B3 | @S06 | `ConfigurationTests` hoặc host test: `ShutdownTimeout` ≥ 30; compose có `stop_grace_period: 35s` |
| Cảnh báo không ra internet trước JWT | Mục 8, spec rủi ro | B4 | @S07 | Kiểm file tài liệu (grep/runbook): có câu cảnh báo bắt buộc |
| Hồi quy lát 1–6 | – | B1–B4 | – | `gateway.Tests` (`Category!=docker`) full xanh |

Kịch bản (ID duy nhất trong `observability.feature`, viết ở test-init):
- @S01 Một request API qua gateway tạo access log có `correlationId` và IP, không chứa token.
- @S02 Request có body JSON và `Authorization: Bearer …` không làm lộ nội dung trong log.
- @S03 Histogram độ trễ (và/hoặc overhead) ghi nhận sau request qua upstream giả.
- @S04 Counter `429` hoặc `503` tăng khi tạo lỗi tương ứng (Outline hoặc hai scenario).
- @S05 Compose gateway cấu hình giới hạn log driver (và ghi chú Jaeger nếu P-03).
- @S06 Cấu hình shutdown host/compose khớp design (30s / 35s).
- @S07 Tài liệu vận hành có cảnh báo JWT.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test trước code; commit nhỏ theo bước; không log `Authorization`/body trong production (test là khóa hợp đồng).

### B1. Access log an toàn (NFR-006 đường chính)
- **Kịch bản/test (đỏ trước):** `document/spec/features/gateway/observability.feature` (@S01, @S02); `ObservabilitySteps.cs`; helper thu log (mẫu TB-023: `TestLoggerProvider` / scope `correlationId`). Đỏ vì chưa có log hoàn tất request.
- **File dự kiến:** mới `GatewayRequestLoggingMiddleware.cs` (sau routing, quanh proxy — đặt để có status cuối; tránh log body); `Program.cs`; có thể mở rộng `GatewayContext`.
- **Hợp đồng:** không đổi response HTTP.
- **Cấu hình:** không (hoặc `Logging:LogLevel` mặc định).
- **Tài liệu:** không.
- **Kiểm chứng:** @S01, @S02 xanh; test lát 1–6 vẫn xanh.
- **Phụ thuộc:** không.

### B2. Metric độ trễ và counter lỗi (NFR-001)
- **Kịch bản/test (đỏ trước):** @S03, @S04; mở rộng `GatewayMetrics.cs`; đăng ký trong `Program.cs`. Spike trong test-init nếu đo “overhead” khó (R-01, xem P-01).
- **File dự kiến:** `GatewayMetrics.cs`, middleware hoặc hook sau YARP (timestamp / `Activity` tags), `ObservabilitySteps.cs`, có thể `RateLimiting`/`UpstreamError` gọi counter lỗi.
- **Hợp đồng:** không đổi API; metric OTLP/Prometheus qua OTel hiện có.
- **Kiểm chứng:** `MeterListener` thấy record; @S03/@S04 xanh.
- **Phụ thuộc:** B1 (cùng pipeline, tránh conflict thứ tự).

### B3. Graceful shutdown và retention log compose
- **Kịch bản/test (đỏ trước):** @S05, @S06; `ConfigurationTests` đọc compose + `appsettings`/host options.
- **File dự kiến:** `src/docker-compose.yml` (`gateway.stop_grace_period`, `logging`); `Program.cs` hoặc `appsettings.json` (`ShutdownTimeout`); tuỳ chọn `docker-compose.observability.yml` (ghi chú TTL Jaeger theo P-03).
- **Kiểm chứng:** test cấu hình xanh; `docker compose config -q`; không bắt buộc test shutdown thật (flaky) trừ khi plan-review chốt cách đơn giản (chỉ assert cấu hình).
- **Phụ thuộc:** B1.

### B4. Cảnh báo JWT và đồng bộ tài liệu (LL-006)
- **Kịch bản/test (đỏ trước):** @S07 (đọc file tài liệu đã chốt ở P-04).
- **File dự kiến:** `document/runbooks/gateway-operations.md` (mới) hoặc mở rộng `PROJECT.md` / `COMMANDS.md` theo P-04; `document/issues/TB-026-gateway-cors.md` + README + `api-gateway-slices.md` (lát 6 `done`); `document/plans/README.md`; issue TB-027 tiêu chí hoàn thành.
- **Kiểm chứng:** @S07; grep LL-006 không lệch TB-026/lát 6–7.
- **Phụ thuộc:** B1–B3 (ghi gate đã chạy vào issue).

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường | Ghi chú |
|---|---|---|---|
| Unit | Parse/đọc compose, host options | xUnit | Không mock |
| Integration (host) | Access log, metric, redaction | `WebApplicationFactory` + upstream giả | `MeterListener`, test logger |
| Cấu hình | `docker-compose.yml`, compose observability | Đọc file / `docker compose config` | Không secret |
| Docker | Chỉ nếu plan-review chốt @S05 chạy thật | Nhóm `docker`, project name riêng | Tuỳ chọn, tránh flaky |

Công cụ: xUnit + Reqnroll (`config/tool-versions.yaml`). BDD tiếng Anh, ASCII.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| R-01 Log request lộ body khi đọc sớm | GDPR | Chỉ log metadata sau pipeline; test @S02 | Revert B1 |
| R-02 Thứ tự middleware sai | Thiếu status/IP đúng | Đặt sau routing, trước/ sau proxy theo spike; giữ test lát 1–6 | Revert B1 |
| R-03 Overhead metric sai (double-count) | NFR-001 sai | Spike P-01 ở test-init; một nguồn đo | Tắt histogram |
| R-04 Jaeger không TTL 7 ngày local | Retention không đủ | P-03: document + docker log driver; không hứa Jaeger nếu image không hỗ trợ | Chỉ compose logging |
| R-05 Access log quá nhiều | Chi phí dev | Log Information một dòng/request | Giảm cấp log |

Ảnh hưởng consumer: không. Contract API không đổi.

Bài học: **LL-006** (đồng bộ lát/issue); **LL-005** (câu hỏi mở có lựa chọn); từ TB-023: metric không mang `correlationId`; từ TB-024: không ghi đè `WindowSeconds` &lt; 30 trong test flaky.

## 6. Gate và Definition of Done

- Gate: `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/gateway.Tests/gateway.Tests.csproj --filter "Category!=docker"`; `dotnet format --verify-no-changes`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; `dotnet list ... --vulnerable` (gateway); gitleaks nếu đã cài (ghi rõ nếu chưa).
- Tiêu chí issue: mọi mục checklist trong issue TB-027 (sau khi plan-review cập nhật từ bảng truy vết).
- Tài liệu: runbook/cảnh báo, bảng lát, README issue, TB-026 `done`.

## 7. Câu hỏi mở và giả định

| # | Điều chưa rõ | Ảnh hưởng | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Cách đo **độ trễ thêm vào** với YARP | B2, @S03 | sondv | **Đã chốt (CONFIRM #1):** Activity OTel parent/child; spike ở test-init |
| P-02 | Counter lỗi gateway | B2, @S04 | sondv | **Đã chốt (CONFIRM #2):** bốn counter riêng (`429`, `502`, `503`, `504`), không nhãn correlation/route |
| P-03 | Retention 7 ngày local | B3, @S05 | sondv | **Đã chốt (CONFIRM #3):** logging driver gateway trong compose; Jaeger best-effort + dọn volume |
| P-04 | Cảnh báo JWT | B4, @S07 | sondv | **Đã chốt (CONFIRM #4):** `document/runbooks/gateway-operations.md` + link từ `PROJECT.md` |

Giả định (kiểm khi code): IP lấy sau `UseForwardedHeaders`; test dùng `X-Test-Remote-Ip` như các lát trước.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan:tb-027:p01-overhead-measurement | Đo overhead gateway | Activity OTel parent/child; spike test-init | 1 | 2026-11-25 |
| 2 | plan:tb-027:p02-error-counters | Counter lỗi | Bốn counter riêng 429/502/503/504, không tag | 1 | 2026-11-25 |
| 3 | plan:tb-027:p03-jaeger-retention | Retention 7 ngày | Docker logging driver gateway; Jaeger best-effort | 1 | 2026-11-25 |
| 4 | plan:tb-027:p04-jwt-warning-doc | Cảnh báo JWT | Runbook mới + link PROJECT.md | 1 | 2026-11-25 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-25 | plan-init draft | 0 (4 CONFIRM đã chốt) | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-25 | Tạo bản nháp plan-init | Bắt đầu lát 7 sau merge TB-026 |
| 2026-11-25 | plan-review vòng 1 PASS; `status: approved`; chốt P-01..P-04 | Người dùng chốt qua form |