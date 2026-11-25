---
title: Gateway lát 6, CORS theo allowlist
issue: TB-026
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-11-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-026 — Gateway lát 6, CORS theo allowlist

Lát 6 trong [`api-gateway-slices.md`](api-gateway-slices.md), phụ thuộc lát 1 (TB-010, người dùng chốt coi là `done`) và lát 4 (TB-024, `done`, để kiểm `429` có header CORS). Làn vừa: một policy CORS, một khối cấu hình có validate, một middleware đã có sẵn trong ASP.NET Core; không đổi DB, không đổi contract lỗi (không có mã mới). Plan ngắn, không có spike bắt buộc ở plan-review; spike ở test-init (R-01).

Ảnh chụp trạng thái branch (2026-11-25): nhánh `feature/TB-026-gateway-cors` tạo từ `origin/main` (có TB-025 merge, PR 19, commit `1397f24`). Local `main` còn ở `66673c2` (cũ hơn TB-025): không ảnh hưởng vì nhánh tạo từ `origin/main`. Working tree sạch trước khi ghi plan.

Đã có (đã đọc code, trên `origin/main` `1397f24`): `src/gateway/Program.cs` có thứ tự `UseForwardedHeaders` → `UseRouting` → `UseCorrelation` → `UseTeamboardExceptionHandler` → `UseRequestTimeouts` → `UpstreamErrorMiddleware` → `UseRateLimiter` → `BodyLimitMiddleware` → `MapDefaultEndpoints` → `MapReverseProxy` → `MapFallback`; **chưa có** CORS (grep `Cors` trong `src/gateway` không có kết quả). `RateLimitingExtensions` đặt `RejectionStatusCode = 429` và `OnRejected` ghi `problem+json` + `Retry-After` (không đụng header khác). `appsettings.json` có `ForwardedHeaders:KnownProxies` là mảng rỗng (mẫu cấu hình mảng để bắt chước). `RateLimitOptions` + `RateLimitOptionsValidator` và `BodyLimitOptions` là mẫu Options có `ValidateOnStart`. Route `boards-read`/`boards-write` khai báo `Methods` `GET, HEAD` và `POST, PUT, PATCH, DELETE`, **không có `OPTIONS`**: preflight không khớp route và sẽ rơi vào fallback `404` nếu CORS không trả lời trước (xem R-01).

Test: `src/gateway.Tests` dùng Reqnroll (`config/tool-versions.yaml`, `bdd_runner_dotnet: Reqnroll`) với các `*Steps.cs` và `GatewayContext` (`Settings`, `UpstreamStub` đếm request nhận được, `X-Test-Remote-Ip`). `.feature` nằm ở `document/spec/features/gateway/`.

## 1. Phạm vi của lát

**Làm:**
- Policy CORS theo allowlist origin từ cấu hình `Cors:AllowedOrigins` (mặc định `http://localhost:4200`, người dùng chốt, Confirm #3), so khớp **chính xác** từng origin.
- Preflight `OPTIONS` từ origin trong allowlist: gateway trả `204` với header CORS, **không** chuyển xuống upstream và **không** bị tính vào rate limit (FR-005, design 6.4).
- Header cho phép (`Authorization, Content-Type, X-Correlation-Id, Idempotency-Key, If-Match`), method (`GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS`), header lộ ra (`X-Correlation-Id, Retry-After, ETag, Location`), `Access-Control-Max-Age: 600` (design 6.3, DA-05).
- Response `429` của rate limiter mang header CORS khi origin hợp lệ (design 6.4: CORS đứng trước RateLimiter).
- Origin ngoài allowlist: response không có header `Access-Control-Allow-Origin` (trình duyệt chặn; FR-005 Given/Then).
- Không bật credentials, không dùng `*` (design 6.3; rule `api-endpoints.md` chỉ cấm `*` khi có credential, plan chọn nghiêm hơn theo design).
- Validate cấu hình khi khởi động: mỗi phần tử phải là origin tuyệt đối `scheme://host[:port]` (không path, không query, không `*`); sai thì host không khởi động (`ValidateOnStart`, theo mẫu `RateLimitOptionsValidator`).
- Đồng bộ trạng thái tài liệu theo LL-006 (xem B3).

**Không làm (trong lát này):**
- Kiểm JWT, rate limit theo user, xác thực (spec mục 4; CORS không đổi quyền truy cập).
- Phản hồi CORS cho `503/502/504`, `413`, `404`, `500` (không có yêu cầu; response vẫn mang header CORS nếu middleware đã đặt, không kiểm riêng).
- Frontend Angular và giao diện (ADR 0008; không có client thật trong lát này).
- Metric hay log riêng cho CORS (lát 7).
- Thay đổi `docker-compose.yml`/Dockerfile: mặc định đã đủ cho local; biến `Cors__AllowedOrigins__0` chỉ dùng khi triển khai (giai đoạn 4).
- Smoke với `board-service` thật (lát 8).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-005 preflight origin trong allowlist → header CORS, không tới upstream | 6.3, 8.2 | B1 | @S01 | Host test: `OPTIONS /api/v1/boards` có `Origin` + `Access-Control-Request-Method: POST` → `204`, `Access-Control-Allow-Origin` đúng origin, `UpstreamStub.Received` rỗng |
| FR-005 origin ngoài allowlist → không có header CORS | 6.3, 8.3 | B1 | @S02 | Host test: `GET` và preflight từ `http://evil.example` → không có `Access-Control-Allow-Origin` |
| FR-005 header cho phép/lộ ra đúng danh sách | 6.3 | B1 | @S03 | Host test: `GET` origin hợp lệ có `Access-Control-Expose-Headers` chứa `X-Correlation-Id`, `Retry-After`; preflight có `Access-Control-Allow-Headers` và `Access-Control-Max-Age: 600` |
| FR-005 preflight không tính rate limit | 6.4, 8.2 | B2 | @S04 | Host test: đầy hạn mức `write` (ngưỡng nhỏ) rồi `OPTIONS` từ origin hợp lệ → vẫn `204`, không `429` |
| FR-005 `429` vẫn mang header CORS | 6.4 | B2 | @S05 | Host test: request ghi thứ vượt ngưỡng từ origin hợp lệ → `429` `RATE_LIMIT_EXCEEDED` kèm `Access-Control-Allow-Origin` và `Retry-After` |
| FR-005 không credentials, không `*` (design 6.3) | 6.3 | B3 | @S06 | Host test: preflight và `GET` không có `Access-Control-Allow-Credentials`; không bao giờ có `Access-Control-Allow-Origin: *` |
| NFR-004 origin nằm trong cấu hình (không hardcode) | 6.3, NFR-004 | B3 | @S07 | Host test: ghi đè `Cors:AllowedOrigins` thành origin khác → origin cũ bị chặn, origin mới được phép |
| NFR-004 cấu hình sai bị chặn khi khởi động | 6.3, 7 | B3 | `CorsOptionsTests` (unit) | Unit: `*`, origin có path, chuỗi không phải URL, phần tử rỗng → lỗi; danh sách rỗng hợp lệ (xem P-02) |
| Thứ tự middleware (CORS trước RateLimiter, sau UpstreamError) | 6.4 | B1, B2 | @S04, @S05 (khoá thứ tự) | Kiểm bằng @S04/@S05 trong cùng pipeline; comment `Program.cs` cập nhật |

Kịch bản (ID duy nhất trong `cors.feature`, viết ở test-init):
- @S01 Preflight từ origin trong allowlist nhận `204` có header CORS và không tới upstream.
- @S02 Origin ngoài allowlist không nhận header CORS (cả `GET` và preflight).
- @S03 Header cho phép và lộ ra khớp danh sách design 6.3; `Max-Age` 600.
- @S04 Preflight không bị giới hạn khi IP đã hết hạn mức ghi.
- @S05 `429` từ rate limiter mang `Access-Control-Allow-Origin` và `Retry-After` cho origin hợp lệ.
- @S06 Không có `Access-Control-Allow-Credentials`, không có `*`.
- @S07 Origin đọc từ `Cors:AllowedOrigins`: ghi đè cấu hình thay đổi kết quả.

Test không qua feature: `CorsOptionsTests` (validation cấu hình), `ConfigurationTests` mở rộng (đọc `appsettings.json` thật: mặc định `["http://localhost:4200"]`).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test viết theo từng bước, đỏ trước khi code; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng chỉ để test biên dịch.

### B1. Preflight và header CORS theo allowlist (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/cors.feature` (@S01, @S02, @S03, @S07); @S01 và @S03 khẳng định `Vary` chứa `Origin` (P-01 đã chốt: có; @S02 không có Allow-Origin nên không áp dụng); `CorsSteps.cs` (gửi `OPTIONS` và `GET` với `Origin`, đọc header); `CorsOptionsTests` (validation: `*`, path, chuỗi không phải URL, phần tử rỗng; danh sách rỗng hợp lệ, P-02 đã chốt); `UpstreamStub.Received` để khẳng định preflight không tới upstream. Đỏ vì chưa có CORS (@S01 đỏ vì `OPTIONS` hiện rơi vào fallback `404`).
- **File/module dự kiến chạm:** mới `src/gateway/CorsOptions.cs` (`SectionName = "Cors"`, `string[] AllowedOrigins`, mặc định rỗng trong class, giá trị thật trong `appsettings.json`); mới `src/gateway/CorsOptionsValidator.cs` (`IValidateOptions<CorsOptions>`, theo mẫu `RateLimitOptionsValidator`, đăng ký cùng chỗ với `AddOptions().BindConfiguration().ValidateOnStart()` như `RateLimitingExtensions`); mới `src/gateway/CorsExtensions.cs` (`AddGatewayCors`: `AddCors` với một default policy đọc allowlist, methods, headers, expose, `SetPreflightMaxAge(600)`, không `AllowCredentials`); `src/gateway/Program.cs` (đăng ký `AddGatewayCors()`; `app.UseCors()` đặt **sau** `UpstreamErrorMiddleware` và **trước** `UseRateLimiter()`; cập nhật comment thứ tự design 6.4); `src/gateway/appsettings.json` (`Cors:AllowedOrigins: ["http://localhost:4200"]`); `src/gateway.Tests/` (`cors.feature`, `CorsSteps.cs`, `CorsOptionsTests.cs`).
- **Hợp đồng thay đổi (API):** thêm hành vi `OPTIONS` có `Origin` từ origin trong allowlist: `204` với header CORS (trước đây `404`). Không mã lỗi mới. Thay đổi cộng thêm, không breaking (design 10: thêm là cộng thêm).
- **Migration / cấu hình / biến môi trường:** khoá `Cors:AllowedOrigins` (mảng) trong `appsettings.json`; ghi đè bằng `Cors__AllowedOrigins__0` khi triển khai (không đổi compose ở lát này).
- **Tài liệu cần cập nhật:** không (design 6.3 đã mô tả đúng; kiểm lại khi viết).
- **Kiểm chứng:** @S01, @S02, @S03, @S07 xanh; `CorsOptionsTests` xanh; test lát 1–5 vẫn xanh; `dotnet build src/teamboard.slnx -warnaserror`; `dotnet format src/teamboard.slnx --verify-no-changes`.
- **Phụ thuộc:** không (trong lát).

### B2. Preflight không bị giới hạn, `429` mang header CORS
- **Kịch bản/test sẽ viết (đỏ trước):** @S04, @S05 (dùng `X-Test-Remote-Ip` và ngưỡng ghi nhỏ qua `UseSetting`, không ghi đè `WindowSeconds` dưới 30 giây, theo bài học TB-024); thêm kiểm `Retry-After` vẫn có khi `429` có CORS. Đỏ vì nếu CORS đặt sau `RateLimiter` thì `OPTIONS` bị `429` và `429` không có header CORS.
- **File/module dự kiến chạm:** `src/gateway.Tests/cors.feature` (thêm @S04, @S05), `CorsSteps.cs`; `src/gateway/Program.cs` chỉ khi thứ tự B1 chưa đúng (không đổi thêm).
- **Hợp đồng thay đổi:** không đổi. `429` vẫn `RATE_LIMIT_EXCEEDED`, chỉ thêm header CORS khi origin hợp lệ.
- **Migration / cấu hình:** không.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** @S04, @S05 xanh; @S01 vẫn xanh; test rate-limit của TB-024 (`rate-limit.feature`) vẫn xanh; chạy lặp nhóm rate limit 3 lần để bắt flaky (cửa sổ trượt).
- **Phụ thuộc:** B1.

### B3. Chặn credentials và `*`, đồng bộ tài liệu
- **Kịch bản/test sẽ viết (đỏ trước):** @S06 (không credentials, không `*`); `ConfigurationTests` (đọc `appsettings.json` thật: allowlist mặc định đúng, không có `*`). @S06 có thể xanh sẵn vì B1 không bật credentials: ghi TDD log là hồi quy, giữ làm khoá hợp đồng.
- **File/module dự kiến chạm:** `src/gateway.Tests/cors.feature` (@S06), `ConfigurationTests.cs`; tài liệu: `document/issues/TB-010-gateway-skeleton-routing.md` (front matter `status: review` → `done`, theo quyết định người dùng); `document/issues/TB-025-gateway-body-limit.md` (front matter `status: review` → `done`, P-03 đã chốt) và dòng TB-025 trong `document/issues/README.md` + lát 5 trong `api-gateway-slices.md` (`review` → `done`); `document/issues/README.md` (dòng TB-010 `review` → `done`; dòng TB-026 giữ `todo` đến coding-start); `document/plans/api-gateway-slices.md` (dòng lát 6 `todo` → `planning` khi plan-init, LL-006; lát 1 đã `done`).
- **Hợp đồng thay đổi:** không.
- **Migration / cấu hình:** không.
- **Tài liệu cần cập nhật (LL-006):** như trên, cùng commit với issue liên quan. `document/plans/README.md` thêm dòng plan TB-026.
- **Kiểm chứng:** @S06 xanh; `ConfigurationTests` xanh; toàn gate mục 6; đối chiếu bảng lát và `document/issues/README.md` bằng grep sau khi sửa (không còn lệch cho lát 1 và lát 6).
- **Phụ thuộc:** B1, B2.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | `CorsOptions` validation (`*`, path, chuỗi không phải URL, phần tử rỗng, danh sách rỗng) | xUnit thuần | Không mock |
| Integration (host in-process) | Preflight, header CORS, `429` có CORS, không credentials, cấu hình ghi đè (@S01–@S07) | `WebApplicationFactory<Program>` (TestServer) + `UpstreamStub` Kestrel cổng ngẫu nhiên đếm request nhận được; ghi đè qua `Settings`; `X-Test-Remote-Ip` | Không mock YARP hay CORS; upstream giả là socket thật |
| Cấu hình | `appsettings.json` thật | Đọc file trong `ConfigurationTests` | Không secret |

Không có test Docker (không đổi compose/Dockerfile). Công cụ: xUnit + Reqnroll (`config/tool-versions.yaml`). Test BDD viết tiếng Anh, ASCII (`test-review.md` mục 1). Kim tự tháp: phần lớn host test nhanh, không có test e2e ở lát này.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| R-01 Preflight `OPTIONS` không khớp route nào (route chỉ có GET/HEAD/POST/PUT/PATCH/DELETE), rơi vào fallback `404` trước khi CORS trả lời | Trình duyệt không gọi được API từ Angular | `UseCors()` là middleware toàn cục và trả `204` cho preflight; @S01 đỏ trước để kiểm; nếu không đúng, dừng và hỏi (không thêm `OPTIONS` vào route tuỳ tiện) | Revert B1 |
| R-02 CORS đặt sau `RateLimiter` (sai thứ tự design 6.4) | Preflight bị tính hạn mức; `429` thiếu header CORS | Thứ tự duy nhất ở 6.4; @S04, @S05 khoá thứ tự quan sát được | Revert B2 |
| R-03 So khớp origin sai (khác hoa/thường, dấu `/` cuối, cổng) | Origin hợp lệ bị chặn hoặc origin lạ được cho qua | So khớp chính xác (`WithOrigins`), validate cấu hình không path/query; test @S07 | Sửa cấu hình |
| R-04 Cấu hình `*` hoặc path được đưa vào | Mở CORS cho mọi origin hoặc không khớp | `ValidateOnStart` trong `CorsOptions`; unit test | Revert B1 |
| R-05 Header CORS bị xoá bởi middleware phía sau (`Response.Clear()`) | Lỗi không có header CORS | Không dùng `Response.Clear()` (bài học TB-023); chỉ kiểm `429` ở lát này, lỗi khác ghi ở lát 7 | Không áp dụng |
| R-06 Test cửa sổ trượt flaky khi đếm hạn mức | Test đỏ ngẫu nhiên | Ngưỡng nhỏ qua cấu hình, không ghi đè `WindowSeconds` dưới 30 giây; chạy lặp 3 lần | Sửa test |

Ảnh hưởng consumer (service khác): không. `board-service` không đổi; request preflight không còn tới upstream. Client thật (Angular, ADR 0009) sẽ là consumer đầu tiên; lát này chưa có client.

Bảo mật (STRIDE design mục 8, bước kiểm quyền/validate): origin allowlist chính xác; không credentials, không `*`; origin ngoài allowlist không có header; không bật CORS cho mọi origin; không secret mới. Test tương ứng: @S02, @S06, `CorsOptionsTests`.

Chi phí: không tài nguyên mới, không Docker, không AWS.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `security`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát và issue) áp dụng ở B3: lát 1 và lát 4 đã `done` trong bảng lát; trạng thái lát 1 trong `document/issues/README.md` và TB-010 được đồng bộ sang `done` theo quyết định người dùng. Biện pháp 4 của LL-006 (kiểm lát trước ở đầu plan-init) đã làm: lát 1 (TB-010) và lát 4 (TB-024) đều `done`. Từ TB-023/TB-024/TB-025: không `Response.Clear()`; test log/ngưỡng theo cách đã dùng; không ghi đè `WindowSeconds` dưới 30 giây.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"`; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; quét secret thô (`gitleaks` chưa cài, ghi rõ khi báo xong); nhóm `docker` không cần chạy (không đổi compose/Dockerfile).
- Tiêu chí hoàn thành: theo issue TB-026, mục "Tiêu chí hoàn thành" đã điền từ FR-005 (spec mục 7 Given/When/Then và luồng 8.1–8.3) ở vòng plan-review 1.
- Tài liệu/config cập nhật: như B3 (issue TB-010, `document/issues/README.md`, bảng lát, `document/plans/README.md`); design 6.3/6.4 không đổi.

## 7. Câu hỏi mở và giả định

Giả định (cần kiểm ở test-init, sai thì dừng và hỏi):
- **A-01:** ASP.NET `UseCors()` toàn cục trả `204` cho preflight có `Origin` + `Access-Control-Request-Method` ngay cả khi không có route khớp `OPTIONS` (R-01). Nếu sai, cần quyết định cách khác.
- **A-02:** Preflight từ origin ngoài allowlist trả status tuỳ thư viện nhưng **không** có `Access-Control-Allow-Origin` (đúng FR-005); test @S02 khẳng định header, không khẳng định status.
- **A-03:** `Access-Control-Expose-Headers` và `Access-Control-Allow-Headers` được ghi theo danh sách cấu hình trong code, không từ cấu hình (design 6.3 không yêu cầu cấu hình hoá); nếu cần cấu hình thì mở câu hỏi mới.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-01 | Có khẳng định `Vary: Origin` không? | B1, @S01, @S03 | sondv | **Đã chốt (CONFIRM #1): có.** |
| P-02 | `Cors:AllowedOrigins` rỗng (`[]`) là hợp lệ hay lỗi khởi động? | B1, `CorsOptionsTests` | sondv | **Đã chốt (CONFIRM #2): hợp lệ** (CORS tắt); phần tử sai (`*`, path, không phải URL) vẫn lỗi khi khởi động. |
| P-03 | TB-025 còn `review` trong bảng lát và `document/issues/README.md` dù PR 19 đã merge | Đồng bộ LL-006, B3 | sondv | **Đã chốt (CONFIRM #3): đồng bộ TB-025 sang `done` ngay trong nhánh này** (ngoại lệ có chủ ý của người dùng với `docs-update-specs.md`; ghi trong B3). |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-init:TB-026:base-branch | Nhánh TB-026 tạo từ đâu, khi TB-025 đang `review` trên nhánh riêng | Từ `main` (TB-025 đã merged). Ghi chú: thực tế TB-025 đã merge vào `origin/main` (PR 19, `1397f24`); local `main` còn cũ, nhánh tạo từ `origin/main` | 1 | 2026-11-25 |
| 2 | plan-init:TB-026:lat1-status | Trạng thái lát 1 (TB-010) lệch giữa bảng lát (`done`) và issue (`review`) | Coi TB-010 là `done`; đồng bộ issue và `document/issues/README.md` | 1 | 2026-11-25 |
| 3 | plan-init:TB-026:origin-config-key | Khoá cấu hình danh sách origin CORS | `Cors:AllowedOrigins` (mặc định `http://localhost:4200`) | 1 | 2026-11-25 |
| 4 | plan-review:TB-026:vary-origin | Có khẳng định `Vary: Origin` không? | Có (kiểm ở @S01–@S03) | 1 | 2026-11-25 |
| 5 | plan-review:TB-026:empty-allowlist | `Cors:AllowedOrigins` rỗng: hợp lệ hay lỗi khởi động? | Hợp lệ (CORS tắt); phần tử sai vẫn lỗi khởi động | 1 | 2026-11-25 |
| 6 | plan-review:TB-026:lat5-sync | Đồng bộ TB-025 (đã merge) sang `done` trong nhánh TB-026? | Có, trong nhánh này (ngoại lệ có chủ ý với `docs-update-specs.md`) | 1 | 2026-11-25 |
| 7 | plan-review:TB-026:assumptions | Chấp nhận giả định A-01 (204 cho preflight không có route OPTIONS, kiểm ở test-init), A-02 (không có Allow-Origin ngoài allowlist, không ép status), A-03 (header cho phép/lộ ra cố định trong code) | Chấp nhận cả ba | 1 | 2026-11-25 |

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-25 | Làn vừa: A, B, D, E, F, G, H; đối chiếu code (`Program.cs` trên `origin/main`, `RateLimitingExtensions`, `UpstreamStub`, `.feature` có sẵn), rule `api-endpoints.md` | 4 (P-01, P-02, P-03, giả định A-01..A-03) | 3 → đã sửa | NEEDS_CONFIRM |
| 2 | 2026-11-25 | Delta: 4 CONFIRM đã chốt (#4–#7) và dây chuyền: test Vary, test allowlist rỗng, đồng bộ TB-025 | 0 | 0 (sau khi áp dụng 3 thay đổi delta, mục Fix log vòng 2) | PASS |

### Fix log vòng 1

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | plan-review:TB-026:cors-star-citation | FIX | mục 1, mục 2 | Plan trích `api-endpoints.md` §7 cho quy tắc "không `*`", nhưng rule chỉ cấm `*` khi có credential; quy tắc nghiêm hơn nằm ở design 6.3 | Trích design 6.3 làm nguồn; ghi rõ rule chỉ cấm `*` khi có credential | `.agents/rules/api-endpoints.md` dòng 106; design 6.3 |
| 1 | plan-review:TB-026:validator-file-open | FIX | B1 | "`CorsOptionsValidator.cs` hoặc `ValidateDataAnnotations` (chọn khi viết)" là lựa chọn chưa quyết | Chốt `CorsOptionsValidator.cs` (`IValidateOptions<CorsOptions>`) theo mẫu `RateLimitOptionsValidator` | `src/gateway/RateLimiting/RateLimitOptionsValidator.cs`, `RateLimitingExtensions.cs` |
| 1 | plan-review:TB-026:done-criteria-empty | FIX | issue TB-026 mục "Tiêu chí hoàn thành", plan mục 6 | Tiêu chí hoàn thành còn `Chưa lập`, plan dời việc viết sang coding-start | Điền năm mục checklist từ FR-005 (spec mục 7, 8.2) và kịch bản @S01–@S07 | `document/spec/api-gateway.md` dòng 71, 89–104; `document/issues/TB-026-gateway-cors.md` |
| 2 | plan-review:TB-026:vary-origin | CONFIRM #4 | B1, §7 P-01 | Chưa có kiểm `Vary` | Thêm vào mô tả @S01–@S03 trong B1: khẳng định `Vary` chứa `Origin` | Plan mục 3 B1; Confirm log #4 |
| 2 | plan-review:TB-026:empty-allowlist | CONFIRM #5 | B1, §2 (NFR-004), §7 P-02 | Chưa rõ allowlist rỗng | Ghi: rỗng hợp lệ, phần tử sai vẫn lỗi khi khởi động; thêm vào `CorsOptionsTests` | Plan mục 2–3; Confirm log #5 |
| 2 | plan-review:TB-026:lat5-sync | CONFIRM #6 | B3, bảng lát, issue README | TB-025 còn `review` dù đã merge | Đồng bộ TB-025 sang `done` (front matter, `document/issues/README.md`, `api-gateway-slices.md`) | Diff nhánh TB-026; Confirm log #6 |
| 2 | plan-review:TB-026:assumptions | CONFIRM #7 | §7 A-01..A-03 | Giả định chưa xác nhận | Đánh dấu chấp nhận; A-01 vẫn kiểm ở test-init | Confirm log #7 |

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-25 | Tạo bản nháp | plan-init lát 6 (TB-026) |
| 2026-11-25 | plan-review vòng 1: sửa 3 FIX (trích nguồn `*`, chốt validator, điền tiêu chí hoàn thành); 4 CONFIRM mở chờ người dùng | plan-review TB-026 |
| 2026-11-25 | plan-review vòng 2 (delta): chốt CONFIRM #4–#7 (Vary, allowlist rỗng, đồng bộ TB-025, giả định A-01..A-03); plan `approved` | plan-review TB-026 |
