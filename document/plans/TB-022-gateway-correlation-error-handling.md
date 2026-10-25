---
title: Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json
issue: TB-022
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway, shared/Http]
---

# Plan: TB-022 — Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json

Lát 2 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`). Làn lớn: spec và design `confirmed` ở TB-007. Ảnh chụp trạng thái branch: `feature/TB-022-gateway-correlation-error-handling`, tạo từ `origin/main` `8716854` (đã gồm TB-010 và TB-021), working tree sạch trước khi tạo issue. Đã có: `src/gateway` (YARP, route boards, health, `404 problem+json`), `src/shared/Http` (`ErrorDefinition`, `ProblemFactory` với `correlationId` tạm bằng `traceId`, `SharedErrors` gồm `INTERNAL_ERROR`), `src/shared/ServiceDefaults` (OTel, health), `gateway.Tests` (upstream giả bằng Kestrel, `WebApplicationFactory<Program>`, nhóm `docker`), `Http.Tests` (unit, chưa có TestHost), `docker-compose.observability.yml` (OTLP mới nối cho `board-service`). Compose chính đòi biến môi trường; các `DockerCli` đã dùng `--env-file src/.env.example` (TB-021).

## 1. Phạm vi của lát

**Làm:**
- `src/shared/Http`: middleware correlation: nhận `X-Correlation-Id` hợp lệ thì giữ nguyên; thiếu, rỗng, **nhiều giá trị** (người dùng chốt P-01), ký tự lạ hoặc dài hơn 64 thì tạo mới; ghi đè header của request (YARP chuyển giá trị đã kiểm xuống upstream); đặt vào response bằng `Response.OnStarting` (header đặt trực tiếp sẽ bị `UseExceptionHandler` xoá; đã kiểm trên mã nguồn ASP.NET Core); gắn tag `correlationId` cho `Activity` (nếu có) và log scope (design D5, FR-003).
  - **Luật id:** 1–64 ký tự (đếm theo `char`), chỉ `A-Z a-z 0-9 . _ -` ASCII, kiểm bằng vòng lặp ký tự hoặc regex neo `\z` (không dùng `$` vì .NET cho `$` khớp trước `\n` cuối nên `"abc\n"` lọt qua).
  - **Id mới:** `Guid.NewGuid().ToString()` (dạng `D`, 36 ký tự, nằm trong luật id).
  - **Không log khi thay id** (người dùng chốt): tránh phình log do kẻ tấn công gửi id sai mỗi request và tránh ghi giá trị gốc (log injection); đếm bằng metric ở lát 7 nếu cần.
- `ProblemFactory` lấy `correlationId` từ middleware (fallback `traceId` khi không có, để thư viện dùng được ngoài pipeline). `traceId` giữ nguyên nguồn `Activity.Current?.Id` (dạng W3C `00-…-…-01`, khớp ví dụ `api-endpoints.md` mục 5).
- ExceptionHandler dùng chung: lỗi chưa lường trả `500 problem+json` mã `INTERNAL_ERROR` (đã có trong `SharedErrors`), không lộ chi tiết nội bộ, vẫn có `X-Correlation-Id`, ghi log lỗi trong scope correlation (design 6.3, 6.4; người dùng chốt gán vào lát 2). Cơ chế: `UseExceptionHandler(app => app.Run(...))` ghi `problem+json` trực tiếp, không dùng `ExceptionHandlingPath` (sẽ chạy lại pipeline). Response đã bắt đầu thì không ghi lại được: thuộc lát 3, ngoài phạm vi.
- `src/gateway`: thứ tự `Correlation` → `ExceptionHandler` → (health, YARP, fallback) theo design 6.4; `UseRouting` do minimal hosting tự thêm nên không gọi tường minh, ghi chú trong `Program.cs`.
- `traceparent`: YARP xoá `traceparent`/`tracestate` của client rồi inject từ `Activity` (đã đọc `ReverseProxyPropagator`), nên upstream nhận `traceparent` cùng trace id với client và span id khác.
- `docker-compose.observability.yml`: nối OTLP cho `gateway` (kèm `depends_on: jaeger` như `board-service`; chỉ phụ thuộc Jaeger, không phụ thuộc upstream); kiểm trace gateway → board-service trong Jaeger.
- Test: unit (`Http.Tests`), host test và test Docker (`gateway.Tests`); `.feature` ở `document/spec/features/gateway/`.

**Không làm (trong lát này):** timeout, `502/503/504`, xử lý lỗi khi response đã bắt đầu (lát 3), rate limit, `ForwardedHeaders` (lát 4), body (lát 5), CORS (lát 6), metric độ trễ thêm vào và metric đếm id bị thay, log retention, graceful shutdown (lát 7), correlation ở phía `board-service`, smoke với endpoint boards thật (lát 8).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-003 tạo/giữ `X-Correlation-Id`, trả trong response, ghi đè xuống upstream | D5, 6.4 | B1a, B2 | @S01, @S02 | Unit middleware; host test với upstream giả ghi header nhận được |
| FR-003 id từ upstream không làm response có hai giá trị | 6.4 | B2 | @S07 | Upstream giả trả `X-Correlation-Id` khác; response chỉ có id của gateway |
| FR-003 `traceparent` truyền xuống, cùng trace | 6.4, 9 | B2 | @S03 | Upstream giả ghi `traceparent`; so trace id với client |
| FR-001 `404 problem+json` mang `correlationId` thật | 6.3, D5 | B1a, B2 | @S04 | Host test: `correlationId` trong body bằng header response |
| Lỗi chưa lường → `500 INTERNAL_ERROR` (gán lát 2) | 6.3, 6.4 | B1b, B2 | @S05 | Unit handler; host test ép lỗi (A-03) |
| NFR-002 trace xuyên gateway → service trong Jaeger | 9 | B3 | @S06 | Test Docker: một trace (theo trace id do test sinh) có span `gateway` và `board-service` |
| NFR-004 cấu hình, không hardcode | D3 | B3 | @S06 | Biến OTLP qua env ở compose; không secret |
| NFR-006 không log token/body/id thô, không lộ nội bộ | D5, 8 | B1a, B1b | @S02, @S05 | Unit với `ILoggerProvider` bắt log và scope: id sai không xuất hiện; 500 không chứa tên exception hay stack |
| Gate xanh (tiêu chí issue) | – | B1a–B3 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất trong từng file, tag `@TB-022 @gateway`):
- @S01 (Outline) Request không có `X-Correlation-Id`, hoặc có id hợp lệ (gồm độ dài 64): upstream và response cùng nhận một id; id hợp lệ giữ nguyên, thiếu thì là GUID mới.
- @S02 (Outline) Id không hợp lệ (khoảng trắng, `;`, `<`, dài 65 ký tự, rỗng, hai giá trị, `"abc\n"`, chữ số Unicode, tab): gateway thay bằng id mới; upstream và response nhận id mới, không phải giá trị gốc.
- @S03 Request có `traceparent`: upstream nhận `traceparent` cùng trace id với client, span id khác.
- @S04 Request tới path lạ nhận `404 problem+json` có `correlationId` bằng `X-Correlation-Id` của response (cả khi client tự gửi id); `/health/live` cũng trả header này.
- @S05 Lỗi chưa lường trả `500 problem+json` mã `INTERNAL_ERROR`, có `X-Correlation-Id` và `correlationId`, body không chứa tên exception hay stack trace.
- @S06 (`@docker`) Compose chạy `gateway`, `board-service`, `jaeger`: request qua gateway mang `traceparent` do test sinh; trace có đúng trace id đó trong Jaeger chứa span của cả `gateway` và `board-service`.
- @S07 Upstream tự trả `X-Correlation-Id` khác: response tới client chỉ có đúng một giá trị, là id của gateway.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng (stub ném `NotImplementedException`) chỉ để test biên dịch được.

### B1a. Thư viện: correlation và `ProblemFactory`
- **Kịch bản/test sẽ viết (đỏ trước):** unit test trong `Http.Tests` (host `TestServer` tối thiểu): luật id (giữ nếu hợp lệ gồm độ dài 64; thay nếu thiếu, rỗng, nhiều giá trị, ký tự lạ, 65 ký tự, `"abc\n"`, chữ số Unicode, tab; id mới là GUID hợp lệ theo luật), header response có mặt cả với lỗi, `Activity` có tag `correlationId` (test đăng ký `ActivityListener` cho source `Microsoft.AspNetCore`; middleware kiểm `Activity` null), log scope có `correlationId`, id sai gốc không xuất hiện trong log, scope hay tag (`ILoggerProvider` bắt log); `ProblemFactory` dùng `correlationId` của context. Đỏ vì chưa có middleware.
- **File/module dự kiến chạm:** `src/shared/Http/` (`CorrelationMiddleware`, `CorrelationExtensions` (`UseCorrelation`), `CorrelationHeaders`), `ProblemFactory.cs`, `src/shared/Http.Tests/` (thêm `Microsoft.AspNetCore.TestHost` 10.0.13, ghim chính xác), `config/dependencies.yaml` (đã có dòng TestHost T0; không thêm hàng trùng).
- **Hợp đồng thay đổi:** header `X-Correlation-Id` theo `api-endpoints.md` mục 6; `problem+json` giữ dạng cũ, `correlationId` đổi nguồn.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (hợp đồng đã có ở `api-endpoints.md`).
- **Kiểm chứng:** test thư viện xanh; `dotnet build -warnaserror`; `git diff` không chứa business logic trong `src/shared`.
- **Phụ thuộc:** không.

### B1b. Thư viện: ExceptionHandler dùng chung
- **Kịch bản/test sẽ viết (đỏ trước):** unit test (`TestServer`): endpoint thử ném lỗi → `500 problem+json` mã `INTERNAL_ERROR`, `X-Correlation-Id` vẫn có (header đặt bằng `OnStarting` sống sót qua `Response.Clear()` của handler), body không chứa tên exception hay stack trace, log lỗi có scope `correlationId`. Đỏ vì chưa có `UseTeamboardExceptionHandler` (khung rỗng).
- **File/module dự kiến chạm:** `src/shared/Http/ErrorHandlingExtensions.cs` (`UseTeamboardExceptionHandler`, `UseExceptionHandler(app => app.Run(...))`), `src/shared/Http.Tests/`.
- **Hợp đồng thay đổi:** `500 INTERNAL_ERROR` problem+json (design 6.3).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** test xanh; `dotnet build -warnaserror`.
- **Phụ thuộc:** B1a.

### B2. Nối vào gateway và kiểm bằng host test (@S01–@S05, @S07)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/correlation.feature` (@S01, @S02, @S03, @S04, @S07) và `error-handling.feature` (@S05); `UpstreamStub` ghi thêm header nhận được (`X-Correlation-Id`, `traceparent`) và có chế độ echo id khác; @S05 ép lỗi chưa lường bằng cách thay `IHttpForwarder` qua `ConfigureTestServices` (giả định A-03, spike ở test-init; phương án dự phòng: `IStartupFilter` thêm middleware ném lỗi sau `UseTeamboardExceptionHandler`, hoặc thay `IForwarderHttpClientFactory`). Đỏ vì gateway chưa gọi middleware.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`UseCorrelation`, `UseTeamboardExceptionHandler`, ghi chú thứ tự đích và việc `UseRouting` tự thêm), `src/gateway.Tests/` (step, `UpstreamStub`), `document/spec/features/gateway/`.
- **Hợp đồng thay đổi:** mọi response của gateway có `X-Correlation-Id` (gồm health và fallback); request tới upstream mang id đã kiểm; `problem+json` do gateway tạo có `correlationId` khớp header.
- **Migration / cấu hình / biến môi trường:** không có.
- **Tài liệu cần cập nhật:** chuyển sang B3.
- **Kiểm chứng:** @S01–@S05, @S07 xanh; test lát 1 (route, health, 404) vẫn xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`; chạy lặp 10 lần nhóm test nhanh của `gateway.Tests` và `Http.Tests` (test `Activity`).
- **Phụ thuộc:** B1a, B1b.

### B3. Trace xuyên service trong Jaeger (@S06)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/tracing.feature` (@S06, `@docker`): chạy compose chính + observability với `gateway`, `board-service`, `jaeger` (tên dự án riêng, `down` không `-v`), chờ Jaeger sẵn sàng, gọi `GET /api/v1/boards` ở cổng 8080 với `traceparent` do test sinh (board-service trả 404 vì chưa có endpoint, vẫn tạo span), rồi lấy đúng trace theo trace id từ API v3 của Jaeger (đường dẫn cụ thể kiểm ở test-init; repo mới chỉ dùng tìm theo service) và khẳng định cùng trace có `service.name` `gateway` và `board-service`. Bản sao `HasTrace` mở rộng cho nhiều service (ghi nợ như `DockerCli`). Thời gian tối đa: build hai image 10 phút, chờ Jaeger 90 giây. Đỏ vì gateway chưa nối OTLP.
- **File/module dự kiến chạm:** `src/docker-compose.observability.yml` (env OTLP và `depends_on: jaeger` cho `gateway`), `src/gateway.Tests/` (step, helper Jaeger), `COMMANDS.md`.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình / biến môi trường:** `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` cho `gateway` ở file observability; không secret.
- **Tài liệu cần cập nhật:** `COMMANDS.md` (ghi rõ chạy nhóm docker mỗi lần một assembly vì dùng chung các cổng 6060, 8080, 16686, 4317, 4318), bảng lát (lát 2 → `done` khi merge), `document/issues/README.md`.
- **Kiểm chứng:** @S06 xanh (cần Docker Desktop, các cổng trên trống); các test Docker khác (TB-001, TB-010, TB-021) vẫn xanh khi chạy từng assembly; `docker compose -f src/docker-compose.yml -f src/docker-compose.observability.yml --env-file src/.env.example config -q`.
- **Phụ thuộc:** B2.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit (host in-process nhỏ) | Luật id, middleware, handler, `ProblemFactory`, log (`Http.Tests`) | `TestServer` với pipeline tối thiểu, `ActivityListener`, `ILoggerProvider` bắt log | Không mock ASP.NET Core |
| Integration (host in-process) | Gateway: correlation, `traceparent`, 404, 500, id từ upstream (`gateway.Tests`) | `WebApplicationFactory<Program>` + upstream giả Kestrel cổng ngẫu nhiên; @S05 ép lỗi (A-03) | Không mock YARP ngoài điểm thay để ép lỗi chưa lường |
| Integration Docker (nhóm `docker`) | Trace xuyên `gateway` → `board-service` trong Jaeger (@S06) | Docker local, tên dự án riêng | Gọi `docker compose` qua Process; tự bỏ qua khi không có Docker |

Kim tự tháp: nhiều unit/host test nhanh; một test Docker. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Thay `IHttpForwarder` không ép được lỗi chưa lường (A-03) | @S05 không viết được | Spike ở test-init; phương án dự phòng đã ghi ở B2; hỏi người dùng nếu phải đổi hướng | Revert B2 |
| `UseExceptionHandler` xoá header (`Response.Clear()`, đã xác minh) | Lỗi thiếu `X-Correlation-Id` | Đặt header bằng `OnStarting`; kiểm ở B1b và @S05 | Sửa thứ tự middleware |
| `traceparent` ở upstream là span đi ra của HttpClient, không phải span hosting | Hiểu nhầm "span của gateway" | @S03 chỉ khẳng định cùng trace id, span id khác; ghi chú trong test | Không áp dụng |
| Trace trong Jaeger chậm xuất hiện làm @S06 flaky; probe `/health/*` tạo trace gateway-only gây khớp nhầm | Test đỏ oan | Tìm theo trace id do test sinh; chờ có điều kiện hạn 90 giây | Sửa test, không nới điều kiện |
| Test Docker chồng cổng với assembly khác khi `dotnet test` chạy song song | Đỏ oan | Ghi trong `COMMANDS.md`: chạy nhóm docker mỗi lần một assembly (cùng tiền lệ TB-010, TB-021) | Không áp dụng |
| Ghi đè header request làm mất id gốc của client để debug | Khó điều tra | Chấp nhận (design D5); không log giá trị gốc (tránh log injection, người dùng chốt) | Không áp dụng |

Ảnh hưởng consumer (service khác): `board-service` nhận thêm `X-Correlation-Id` và `traceparent` (header mới, tương thích ngược); contract công khai thêm header `X-Correlation-Id` ở mọi response của gateway. Bảo mật: luật id chặn log injection và id quá dài (STRIDE design mục 8); lỗi 500 không lộ nội bộ; không secret. Quyền riêng tư: id hợp lệ do client chọn có thể chứa dữ liệu cá nhân (ví dụ `john.doe`) và đi vào log; design mục 8 đã chấp nhận (chỉ ảnh hưởng tra log). Chi phí: chỉ local; test Docker có thể mất tới khoảng 10 phút build ở lần đầu.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `security`, `workflow`): bài học từ review TB-001 (query string lọt vào log) áp dụng: không ghi giá trị gốc của id không hợp lệ vào log; LL-005 áp dụng cho câu hỏi mở.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx` (gồm nhóm `docker`, mỗi lần một assembly nếu tranh cổng); `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; quét secret thô (`gitleaks` chưa cài); `git status --short && git diff --stat`.
- Tiêu chí hoàn thành: theo issue TB-022.
- Tài liệu/config cập nhật: như B3; bảng lát (lát 2 → `done`), `document/issues/README.md`.

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở bước tương ứng; sai thì dừng và hỏi): A-01 trong gateway `Activity.Current` có vì ServiceDefaults đăng ký `AddAspNetCoreInstrumentation` (ở `Http.Tests` không có ServiceDefaults nên test tự đăng ký `ActivityListener`); A-02 YARP truyền `traceparent` bằng `ReverseProxyPropagator` (đã đọc mã nguồn, kiểm ở @S03); A-03 `ConfigureTestServices` thay được `IHttpForwarder` (chưa xác minh vòng đời đăng ký, spike ở test-init với phương án dự phòng); A-04 `OnStarting` giữ được header qua `Response.Clear()` của handler (đã đọc mã nguồn, kiểm ở B1b).

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Header `X-Correlation-Id` xuất hiện nhiều lần | B1a, @S02 | sondv | answered: coi là không hợp lệ, tạo id mới (2026-10-25) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-022:p01-multi-value | Nhiều giá trị `X-Correlation-Id` | Coi là không hợp lệ, tạo id mới | 1 | 2026-10-25 |
| 2 | plan-review:TB-022:a03-forwarder-swap | Cách ép lỗi chưa lường cho @S05 | Spike ở test-init, ghi sẵn phương án dự phòng (`IStartupFilter`, thay `IForwarderHttpClientFactory`) | 1 | 2026-10-25 |
| 3 | plan-review:TB-022:invalid-id-log-policy | Log khi thay id | Không log; đếm bằng metric ở lát 7 nếu cần | 1 | 2026-10-25 |
| 4 | plan-review:TB-022:docker-ports-parallel | Test Docker tranh cổng giữa các assembly | Giữ nguyên; ghi trong `COMMANDS.md` chạy nhóm docker mỗi lần một assembly | 1 | 2026-10-25 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-25 | Toàn bộ checklist A0, A–H (sub-agent độc lập) | 4 (đã chốt cả 4) | 12 (đã sửa) | NEEDS_CONFIRM |
| 2 | 2026-10-25 | Delta: tách B1, thêm @S07, test log, luật id, trace theo id | 0 | 0 | PASS |

### Fix log vòng 1

| Fingerprint | Sửa gì |
|---|---|
| s06-trace-by-id | @S06 tìm trace theo trace id do test sinh; assert hai service cùng trace; `depends_on: jaeger`; ghi thời gian tối đa |
| log-injection-test-missing | Thêm test `ILoggerProvider`: id sai không xuất hiện trong log, scope, tag; log lỗi có scope `correlationId`; 500 không chứa tên exception |
| id-validation-edge | Luật id dùng vòng lặp hoặc `\z`, đếm theo `char`; thêm `"abc\n"`, chữ số Unicode, tab vào @S02 |
| activity-test-listener | Test tự đăng ký `ActivityListener`; middleware kiểm `Activity` null; A-01 ghi rõ khác nhau giữa `Http.Tests` và gateway |
| s01-upstream-echo-header | Thêm @S07: upstream trả id khác thì response chỉ có đúng một giá trị |
| exception-handler-mechanism | Nêu cơ chế `UseExceptionHandler(app => app.Run(...))`, không dùng `ExceptionHandlingPath`; response đã bắt đầu thuộc lát 3 |
| guid-format | Ghi `Guid.NewGuid().ToString()` (dạng `D`, 36 ký tự) |
| b1-size-split | Tách B1 thành B1a (correlation, `ProblemFactory`) và B1b (ExceptionHandler) |
| deps-yaml-touch | Ghi rõ `config/dependencies.yaml` đã có dòng TestHost, không thêm hàng trùng |
| traceid semantic (ghi nhận) | Giữ `traceId = Activity.Current.Id` (dạng W3C như ví dụ `api-endpoints.md` mục 5); không đổi trong lát này |
| middleware order | Ghi chú `UseRouting` tự thêm và thứ tự đích trong `Program.cs` (B2) |
| docker cost | Ghi thời gian tối đa và chi phí build vào mục rủi ro |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init cho TB-022 (lát 2 của gateway) |
| 2026-10-25 | plan-review vòng 1–2: ghi quyết định P-01 và 3 CONFIRM, sửa 12 FIX, tách B1, thêm @S07; plan `approved` | plan-review TB-022 |
