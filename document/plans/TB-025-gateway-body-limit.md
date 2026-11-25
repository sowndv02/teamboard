---
title: Gateway lát 5, giới hạn body 1 MiB
issue: TB-025
status: approved         # draft | in-review | approved | done
owner: sondv
updated: 2026-11-25
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway]
---

# Plan: TB-025 — Gateway lát 5, giới hạn body 1 MiB

Lát 5 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`), phụ thuộc lát 1 và 2 (đã `done`). Làn vừa: một middleware và một mã lỗi mới, không đổi DB; spec và design đã `confirmed` ở TB-007 nên plan ngắn, không có state file (tuỳ chọn ở làn vừa). Lát 4 (TB-024) đã merge (PR 18), nên `RateLimiter` đã nằm trong pipeline và thứ tự design 6.4 (`RateLimiter` trước `BodyLimit`) kiểm được ngay trong lát này.

Ảnh chụp trạng thái branch (cập nhật 2026-11-25): `feature/TB-025-gateway-body-limit` đã được đưa lên `main` `66673c2` bằng fast-forward (nhánh chưa có commit riêng; bản nháp plan cũ lập khi nhánh còn ở `feb4278`, trước TB-024), working tree chỉ có các file tài liệu của lát này.

Đã có (đã đọc code, trên `main` `66673c2`): `src/gateway/Program.cs` (thứ tự hiện tại `UseForwardedHeaders` → `UseRouting` → `UseCorrelation` → `UseTeamboardExceptionHandler` → `UseRequestTimeouts` → `UpstreamErrorMiddleware` → `UseRateLimiter` → `MapDefaultEndpoints` → `MapReverseProxy` → `MapFallback`; chưa có CORS, chưa có `BodyLimit`), `GatewayErrorCodes` (5 mã, thiếu `PAYLOAD_TOO_LARGE`; `All` liệt kê mã), mẫu Options của TB-024 (`RateLimiting/RateLimitOptions.cs` + `RateLimitOptionsValidator.cs`, đăng ký bằng `AddOptions().BindConfiguration().ValidateOnStart()` trong `RateLimitingExtensions`), `UpstreamErrorMiddleware` + `UpstreamErrorClassifier` (lỗi forwarder có hậu tố `*Client` được coi là client huỷ: không ghi response), `src/shared/Http` (`ErrorDefinition`, `ProblemFactory`, handler `500` bắt mọi ngoại lệ), `src/gateway.Tests` (`GatewayContext` dùng `WebApplicationFactory` TestServer với `Settings`, `OuterResponseHeader`, `Logs`; `TestRemoteIpStartupFilter` đặt IP giả bằng header `X-Test-Remote-Ip`; `UpstreamStub` Kestrel thật ghi method, path, header nhưng **chưa đếm byte body**; `CapturingLoggerProvider`). Chưa có code nào về body (`grep 413|BadHttpRequest|MaxRequestBody` trong `src` không có kết quả).

Hành vi đã biết từ tài liệu học: `MaxRequestBodySize` của Kestrel/route trả `413` nhưng upstream vẫn thấy yêu cầu (`document/learning/api-gateway/part-5-production-security-ops.md`, Lab 5.2), nên muốn "không gửi xuống upstream" phải tự kiểm `Content-Length` bằng middleware đứng trước YARP (design 6.3).

**Kết quả spike của plan-review vòng 1** (bản sao tạm trong `gateway.Tests`, đã xoá; .NET 10.0.13, Mvc.Testing 10.0.13, giới hạn 1024 byte đặt bằng `IHttpMaxRequestBodySizeFeature` ở đầu pipeline, body 2048 byte; kiểm lại ở test-init):

| Điều đã thử | Kết quả |
|---|---|
| TestServer, có `Content-Length` hoặc chunked | Feature `IHttpMaxRequestBodySizeFeature` là `null`: không có giới hạn nào, request `201` và đi hết xuống upstream |
| Kestrel thật (`UseKestrel(0)` + `StartServer()` trên factory dẫn xuất), có `Content-Length` quá giới hạn | Kestrel trả `413` **rỗng** (không `problem+json`), nhưng upstream **vẫn nhận request** (`Received` = 1) |
| Kestrel thật, chunked quá giới hạn | Cùng kết quả: `413` rỗng, upstream đã nhận request; YARP ghi cảnh báo `RequestBodyClient` (nguyên nhân `BadHttpRequestException` "Request body too large") |
| `ExceptionHandler` có đổi lỗi này thành `500` không | **Không**: ngoại lệ do YARP bắt, không tới handler |
| `UpstreamErrorMiddleware` với `RequestBodyClient` | Phân loại `ClientCancelled`, không ghi gì: phản hồi `413` rỗng đến từ Kestrel/YARP, không có `code` và `correlationId` trong thân |
| Hai host Kestrel thật chạy song song trong cùng tiến trình | Một host lỗi `AddressInUseException` (cổng 5000); chạy riêng từng host thì xanh |

## 1. Phạm vi của lát

**Làm:**
- Mã lỗi `PAYLOAD_TOO_LARGE` (413) trong `GatewayErrorCodes` (hợp đồng công khai, DA-02) và mở rộng `GatewayErrorCodesTests`.
- Giới hạn cấu hình được `Gateway:BodyLimit:MaxBytes`, mặc định 1.048.576 byte (1 MiB), qua Options có `ValidateOnStart` (`backend-engineering.md` mục 7); khoảng hợp lệ 1 đến 10.485.760 byte (người dùng chốt, Confirm #3), ngoài khoảng thì khởi động thất bại.
- Middleware `BodyLimit` đứng ngay trước YARP, **sau** `RateLimiter` (design 6.4; request vượt hạn mức nhận `429` trước khi bị xét kích thước, và request bị `413` vẫn tính vào hạn mức của IP): request có `Content-Length` > giới hạn bị `413 problem+json` ngay, **không** gửi xuống upstream; đúng bằng giới hạn thì chuyển tiếp bình thường.
- Body chunked (không có `Content-Length`) vượt giới hạn: middleware đặt `IHttpMaxRequestBodySizeFeature.MaxRequestBodySize` cho request (spike: có hiệu lực trên Kestrel thật, đặt được trước khi đọc body); khi vượt, gateway trả **`413 problem+json` `PAYLOAD_TOO_LARGE`** thay cho `413` rỗng của Kestrel (người dùng chốt, Confirm #1): `UpstreamErrorMiddleware`/`UpstreamErrorClassifier` nhận ra `RequestBodyClient` có nguyên nhân `BadHttpRequestException` 413 khi response chưa bắt đầu và ghi `problem+json` (thêm một giá trị phân loại, không còn là `ClientCancelled`). Upstream có thể đã nhận request và một phần body (đã chấp nhận ở design-review, spike xác nhận). Giới hạn chunked chỉ có hiệu lực ở route proxy (nơi body được đọc); route khác (health, fallback) chỉ chịu kiểm `Content-Length`.
- Phạm vi áp dụng: **mọi request** (kể cả route không tồn tại: `Content-Length` lớn thì `413` thay vì `404`; người dùng chốt, Confirm #2).
- Phản hồi `413` mang `code`, `traceId`, `correlationId` trong body và `X-Correlation-Id` ở header; không lộ giá trị giới hạn nội bộ ngoài `detail` chuẩn, không lộ host/tên nội bộ; log không ghi body (NFR-006).
- Việc TB-022/TB-023 để lại (đã kiểm bằng spike): `ExceptionHandler` không đổi `BadHttpRequestException` 413 thành `500` vì YARP bắt trước; phần còn lại là bước phân loại ở mục trên.
- Test: host test in-process (`gateway.Tests`), `.feature` ở `document/spec/features/gateway/body-limit.feature`.

**Không làm (trong lát này):** sửa rate limit và `ForwardedHeaders` (lát 4, TB-024, đã merge; chỉ thêm một test thứ tự), CORS (lát 6), metric riêng cho body bị từ chối và metric độ trễ (lát 7), các `BadHttpRequestException` khác 413 (ví dụ chunk sai định dạng, header quá lớn: ghi nhận là việc ngoài phạm vi, báo lại nếu spike thấy bị `500`), giới hạn riêng theo route (design chỉ có một giới hạn chung), test Docker (không có hành vi phụ thuộc môi trường; cấu hình mặc định kiểm bằng `ConfigurationTests`), smoke với `board-service` thật (lát 8).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-007 body đúng 1.048.576 byte (có `Content-Length`) → chuyển tiếp | 6.3, 6.4 | B1 | @S01 | Host test: `POST` 1.048.576 byte; upstream nhận đủ số byte; status của upstream |
| FR-007 `Content-Length` 1.048.577 → `413 problem+json`, không tới upstream | 6.3, bảng mã | B1 | @S02 | Host test: `POST` 1.048.577 byte → 413 `PAYLOAD_TOO_LARGE`, `UpstreamStub.Received` rỗng |
| FR-007 `413` mang `X-Correlation-Id`, không lộ nội bộ | D5, mục 8 | B1 | @S02 (bước chung) | Header bằng `correlationId` của body; body không chứa tên upstream/exception |
| FR-007 giới hạn cấu hình được (không hardcode) | 6.3 "cấu hình được" | B1, B3 | @S03, `BodyLimitOptionsTests`, `ConfigurationTests` | Ghi đè 1024 byte → 1025 byte bị 413; unit Options (khoảng hợp lệ); đọc `appsettings.json` thật: 1.048.576 |
| FR-007 chunked đúng giới hạn → chuyển tiếp | 6.3 | B2 | @S04 | Host test (Kestrel thật): chunked tổng 1.048.576 byte → upstream nhận đủ |
| FR-007 chunked vượt giới hạn → ngắt luồng và trả lỗi | 6.3 | B2 | @S05 | Host test (Kestrel thật): chunked 1.048.577 byte → `413 problem+json` `PAYLOAD_TOO_LARGE` có `X-Correlation-Id` bằng `correlationId` trong thân; không `500`; unit classifier: `RequestBodyClient` do `BadHttpRequestException` 413 → giá trị phân loại mới, `RequestBodyClient` khác vẫn `ClientCancelled` |
| NFR-006 log không chứa body | mục 11 | B1, B2 | @S06 | `CapturingLoggerProvider`: log của 413 không chứa nội dung body và có `correlationId` |
| Thứ tự middleware (`BodyLimit` ngay trước YARP) khoá phần quan sát được | 6.4 | B1 | @S02 (kèm), @S07 | 413 mang `X-Correlation-Id` (Correlation ngoài); khi IP đã hết hạn mức ghi, request có `Content-Length` lớn nhận `429` chứ không phải `413` (`RateLimiter` trước `BodyLimit`) |

Kịch bản (ID duy nhất trong `body-limit.feature`; viết ở test-init):
- @S01 Body đúng 1.048.576 byte có `Content-Length` được chuyển tiếp nguyên vẹn.
- @S02 `Content-Length` 1.048.577 byte nhận `413` `PAYLOAD_TOO_LARGE`, upstream không nhận gì, có `X-Correlation-Id`.
- @S03 Giới hạn đọc từ cấu hình (ghi đè 1024 byte): 1024 byte qua, 1025 byte bị `413`.
- @S04 Body chunked đúng giới hạn được chuyển tiếp.
- @S05 Body chunked vượt giới hạn: client nhận `413 problem+json` `PAYLOAD_TOO_LARGE` kèm `X-Correlation-Id`, không `500`.
- @S06 Log của gateway khi từ chối body không chứa nội dung body (NFR-006).
- @S07 Thứ tự với rate limit: IP đã hết hạn mức ghi, request kế tiếp có `Content-Length` lớn nhận `429`, không phải `413` (dùng `X-Test-Remote-Ip` và ngưỡng nhỏ qua cấu hình; không ghi đè `WindowSeconds` dưới 30 giây).
- Test xUnit không qua feature: `GatewayErrorCodesTests` (mã `PAYLOAD_TOO_LARGE` + status 413), `BodyLimitOptionsTests` (mặc định, khoảng 1 đến 10.485.760, ngoài khoảng thì lỗi), `UpstreamErrorClassifierTests` mở rộng (nhánh body quá lớn), `ConfigurationTests` (giá trị trong `appsettings.json`), unit của middleware nếu tách logic quyết định.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng chỉ để test biên dịch được. **Spike ở test-init** kiểm lại bảng spike đầu plan trên mã repo (sai lệch thì dừng và hỏi).

### B1. Hợp đồng lỗi, giới hạn cấu hình và từ chối theo `Content-Length` (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/body-limit.feature` (@S01, @S02, @S03, @S06 phần log, @S07 thứ tự với rate limit); unit `GatewayErrorCodesTests` (mã mới), `BodyLimitOptionsTests`; `UpstreamStub` đếm số byte body nhận được; `GatewayContext` hỗ trợ gửi body theo số byte với `Content-Length`. Đỏ vì chưa có mã, Options hay middleware (S01 xanh sẵn vì hiện chưa giới hạn: ghi TDD log là hồi quy).
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs`; mới `src/gateway/BodyLimitOptions.cs` (`Gateway:BodyLimit:MaxBytes`, `ValidateOnStart`, theo mẫu `RateLimitOptions`); mới `src/gateway/BodyLimitMiddleware.cs` (so `Content-Length` với giới hạn, ghi `problem+json` bằng `ProblemFactory`); `src/gateway/Program.cs` (đăng ký Options, `UseMiddleware<BodyLimitMiddleware>()` ngay sau `UseRateLimiter()` và trước `MapReverseProxy`, cập nhật comment thứ tự); `src/gateway/appsettings.json`; `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `413 problem+json` `PAYLOAD_TOO_LARGE` công khai. `api-endpoints.md` không cần sửa (kiểm lại khi viết; design bảng mã đã có dòng này).
- **Migration / cấu hình:** khoá `Gateway:BodyLimit:MaxBytes` (mặc định 1048576) trong `appsettings.json`; không biến môi trường mới.
- **Tài liệu cần cập nhật:** không thêm (design 6.3 đã mô tả).
- **Kiểm chứng:** @S01–@S03, @S06 (phần `Content-Length`) xanh; test lát 1–3 vẫn xanh; `dotnet build src/teamboard.slnx -warnaserror`; `dotnet format src/teamboard.slnx --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Body chunked: ngắt luồng khi vượt giới hạn
- **Kịch bản/test sẽ viết (đỏ trước):** @S04, @S05 (và @S06 phần chunked); unit classifier cho nhánh body quá lớn; cần host Kestrel thật vì TestServer không có feature giới hạn (spike); test gửi stream không biết độ dài (chunked) qua cổng thật; khẳng định `413 problem+json`, không `500`. Đỏ vì chunked hiện đi hết xuống upstream (TestServer) hoặc nhận `413` rỗng (Kestrel).
- **File/module dự kiến chạm:** `src/gateway/BodyLimitMiddleware.cs` (đặt `MaxRequestBodySize` cho request khi feature có mặt và chưa chỉ đọc); `src/gateway/UpstreamErrorClassifier.cs` (đã có thật; thêm giá trị phân loại cho `RequestBodyClient` do `BadHttpRequestException` 413 khi response chưa bắt đầu) và `src/gateway/UpstreamErrorMiddleware.cs` (ghi `413 problem+json`, không `Response.Clear()`); `src/gateway.Tests/GatewayContext.cs` (chế độ host Kestrel thật, chạy tuần tự, không song song hai host) và `Support/`.
- **Hợp đồng thay đổi:** không thêm (cùng `413`); nếu response đã bắt đầu thì không đổi được status (giữ nhánh hiện có).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** design 6.3 và 6.4 (thêm `BodyLimit` vào mô tả chunked: trả `413 problem+json` thay cho "ngắt luồng"; spec FR-007 đã nói "ngắt luồng và trả lỗi" nên không cần đổi); sửa trong cùng commit coding, ghi vào Fix log của review code.
- **Kiểm chứng:** @S04, @S05 xanh; không có `500` trong log; chạy lặp 10 lần nhóm host test để bắt flaky; test lát 1–3 xanh.
- **Phụ thuộc:** B1.

### B3. Cấu hình mặc định và đồng bộ tài liệu
- **Kịch bản/test sẽ viết (đỏ trước):** `ConfigurationTests` (đọc `appsettings.json` thật: `MaxBytes` = 1.048.576); test hồi quy: route đọc (`GET`, không body) không bị ảnh hưởng. Có thể xanh sẵn nếu B1 đã đặt mặc định: ghi TDD log.
- **File/module dự kiến chạm:** `src/gateway.Tests/ConfigurationTests.cs`; tài liệu.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật (LL-006):** bảng lát (`api-gateway-slices.md`) và `document/issues/README.md` cùng commit với issue: lát 5 → `planning` (bước này, plan-init), `in-progress` khi bắt đầu coding, `review` khi mở PR, `done` sau merge; `document/plans/README.md` thêm plan; `PROJECT.md` không đổi (chưa chuyển giai đoạn).
- **Kiểm chứng:** `ConfigurationTests` xanh; toàn gate mục 6.
- **Phụ thuộc:** B1, B2.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Mã lỗi `PAYLOAD_TOO_LARGE`; `BodyLimitOptions` (mặc định, khoảng); logic quyết định theo `Content-Length` nếu tách | xUnit thuần | Không mock |
| Integration (host in-process) | 413 theo `Content-Length`, đúng bằng giới hạn, cấu hình ghi đè, header correlation, log không có body (`gateway.Tests`) | `WebApplicationFactory<Program>` (TestServer) + `UpstreamStub` Kestrel cổng ngẫu nhiên đếm byte nhận; ghi đè qua `UseSetting` | Không mock YARP; upstream giả là socket thật |
| Integration (host Kestrel thật) | Body chunked bằng và vượt giới hạn | Gateway chạy trên Kestrel cổng ngẫu nhiên (`UseKestrel(0)` + `StartServer()`; spike: TestServer không có feature giới hạn) | Chỉ cho nhóm chunked, giữ nhỏ, chạy tuần tự (hai host song song gây `AddressInUse`) |

Không có test Docker (không phụ thuộc môi trường). Công cụ: xUnit + Reqnroll.xUnit theo `config/tool-versions.yaml` (đã dùng ở lát trước). Test BDD viết tiếng Anh, ASCII (`test-review.md` mục 1). Kim tự tháp: nhiều unit/host test nhanh, nhóm Kestrel thật chỉ cho chunked.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| R-01 `BodyLimit` đặt sai chỗ so với `RateLimiter` (design yêu cầu `RateLimiter` trước `BodyLimit`) | `413` thoát khỏi hạn mức hoặc hạn mức bị bỏ qua | TB-024 đã merge nên đặt đúng chỗ ngay (một dòng sau `UseRateLimiter`); @S07 khoá thứ tự quan sát được | Revert B1 |
| R-02 Hành vi Kestrel/YARP với body quá lớn khác kết quả spike khi cài (spike: `413` rỗng, upstream đã nhận request, `RequestBodyClient` bị coi là client huỷ) | `413` rỗng hoặc `500` thay cho problem+json | Spike lặp lại ở test-init; B2 phân loại tường minh; lệch thì dừng và hỏi | Revert B2 |
| R-03 TestServer không có feature giới hạn nên test chunked xanh giả | Bỏ sót lỗi thật | Dùng host Kestrel thật cho @S04, @S05 (đã xác nhận bằng spike) | Không áp dụng |
| R-06 Hai host Kestrel thật song song gây `AddressInUse` (spike) | Test đỏ ngẫu nhiên | Một host mỗi lần, đặt nhóm chunked cùng collection tuần tự, chạy lặp 10 lần | Sửa test |
| R-07 Mỗi request chunked quá lớn làm YARP ghi một cảnh báo `RequestBodyClient` kèm ngoại lệ | Log ồn khi bị spam | Rate limit đứng trước; chấp nhận ở lát này, lát 7 xem lại log | Không áp dụng |
| R-04 Client ghi body lớn trong khi gateway đã trả 413 và đóng kết nối nên client thấy lỗi ghi thay vì 413 | Test flaky | Test dùng `Content-Length` giả lớn với body nhỏ nơi kiểm header, và body thật 1 MiB+1 chỉ ở bước kiểm byte; chờ có điều kiện | Sửa test |
| R-05 Giá trị `MaxBytes` ngoài khoảng (0, âm, rất lớn) | Gateway vô tác dụng hoặc chặn mọi thứ | `ValidateOnStart` với khoảng hợp lệ; unit test | Revert B1 |

Ảnh hưởng consumer (service khác): thêm `413 problem+json` ở contract công khai của gateway (cộng thêm, trước đây request lớn đi hết tới upstream); `board-service` không đổi. Bảo mật: giới hạn kích thước chặn request khổng lồ sớm (STRIDE design mục 8, D: body lớn); 413 không lộ host/tên nội bộ; log không ghi body, `Authorization`; không secret mới. Chi phí: không tài nguyên mới, không Docker.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `security`, `observability`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát với issue) áp dụng ở B3; LL-005 (câu hỏi mở có lựa chọn và khuyến nghị) áp dụng cho mục 7; biện pháp 4 của LL-006 (kiểm lát trước ở đầu `plan-init`) đã làm: lát 4 đã merge (PR 18), bảng lát và `document/issues/README.md` còn ghi lát 4 và TB-024 là `review`, được đồng bộ sang `done` cùng commit với plan này; từ TB-023/TB-024: không `Response.Clear()` để giữ header của middleware khác, test log chỉ trên logger của gateway, không ghi đè `WindowSeconds` dưới 30 giây.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"`; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; quét secret thô (`gitleaks` chưa cài, ghi rõ); nhóm `docker` không cần chạy (không đổi compose/Dockerfile), chạy hồi quy `gateway` nếu bước nào chạm Dockerfile.
- Tiêu chí hoàn thành: theo issue TB-025.
- Tài liệu/config cập nhật: như B3; bảng lát và `document/issues/README.md` (LL-006).

## 7. Câu hỏi mở và giả định

Giả định A-01..A-04 của bản nháp đã được spike của plan-review kiểm (bảng đầu plan): A-01 đúng (feature có hiệu lực trên Kestrel thật); A-02 đúng (TestServer không có feature); A-03 **sai** (không có `500`, ngoại lệ do YARP bắt); A-04 đúng một phần (bị coi là client huỷ, nhưng phản hồi `413` rỗng vẫn tới client; cần phân loại mới). Chưa kiểm: body chunked qua route không proxy (không đọc body nên không bị giới hạn chunked, chỉ kiểm `Content-Length`; ghi ở mục 1), `Expect: 100-continue`, hành vi khi client ngắt giữa chừng.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Thứ tự làm/merge với lát 4 | R-01, B1 | – | answered bởi thực tế: TB-024 đã merge (PR 18, `main` `66673c2`); lát 5 đặt `BodyLimit` sau `RateLimiter` theo design 6.4, thêm @S07 |
| P-02 | Chunked vượt giới hạn: `413 problem+json` hay `413` rỗng của Kestrel | B2, @S05 | sondv | answered: `413 problem+json` (Confirm #1, 2026-11-25) |
| P-03 | `413` áp cho mọi request hay chỉ route proxy | B1 | sondv | answered: mọi request (Confirm #2, 2026-11-25) |
| P-04 | Khoảng hợp lệ của `MaxBytes` | B1 | sondv | answered: 1 đến 10.485.760 byte, mặc định 1.048.576, không giới hạn riêng cho health (Confirm #3, 2026-11-25) |
| P-05 | Cỡ lát (7 kịch bản, 3 bước, ~1 ngày) | cả plan | sondv | answered: giữ một lát (Confirm #4, 2026-11-25) |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-025:chunked-413-shape | Body chunked vượt giới hạn: `413 problem+json` hay `413` rỗng của Kestrel | `413 problem+json` (thêm nhánh phân loại ở `UpstreamErrorClassifier`) | 1 | 2026-11-25 |
| 2 | plan-review:TB-025:scope-all-requests | `413` áp cho mọi request hay chỉ route proxy | Mọi request | 1 | 2026-11-25 |
| 3 | plan-review:TB-025:maxbytes-range | Khoảng hợp lệ của `MaxBytes` | 1 đến 10.485.760 byte | 1 | 2026-11-25 |
| 4 | plan-review:TB-025:slice-size | Giữ một lát hay chia | Giữ một lát | 1 | 2026-11-25 |

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-25 | Làn vừa: A, B, D, E, F; kèm spike thực nghiệm trên mã repo | 4 → đã chốt | 3 → đã sửa | NEEDS_CONFIRM → PASS sau khi chốt và sửa |
| 2 | 2026-11-25 | Delta: kiểm nhất quán giữa mục 1, truy vết, B2, rủi ro, câu hỏi mở, issue | 0 | 0 | PASS |

### Fix log vòng 1

| Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào |
|---|---|---|---|---|
| spike-assumptions-stale | FIX | mục 7, đầu plan, R-02, R-03 | A-03 sai và A-04 thiếu: spike cho thấy `413` rỗng của Kestrel tới client, không có `500` | Thêm bảng spike đầu plan; viết lại giả định và rủi ro theo kết quả; thêm R-06, R-07 |
| chunked-only-on-proxy-route | FIX | mục 1 | Giới hạn chunked chỉ hiệu lực ở route đọc body (proxy); phạm vi "mọi request" gây hiểu nhầm | Ghi rõ: route khác chỉ chịu kiểm `Content-Length` |
| b2-classifier-missing | FIX + CONFIRM #1 | B2, truy vết @S05, danh sách test | B2 chưa nêu rõ file thật phải đổi (`UpstreamErrorClassifier`, `UpstreamErrorMiddleware`) và test classifier | Cập nhật B2, @S05, thêm `UpstreamErrorClassifierTests` mở rộng, design 6.3/6.4 cần cập nhật khi coding |
| decisions-applied | CONFIRM #1-#4 | mục 1, 4, 7, B2 | Áp dụng quyết định người dùng | `problem+json` cho chunked; mọi request; khoảng 1 đến 10 MiB; giữ một lát; test Kestrel chạy tuần tự |

## Dây chuyền đã kiểm ở vòng 2
Đổi `problem+json` kéo theo: unit classifier (đã thêm), design 6.3/6.4 (ghi ở B2), issue TB-025 (đồng bộ tiêu chí @S05 và số câu hỏi mở), bảng lát (giữ `planning` đến coding-start), `PROJECT.md` không đổi.

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init lát 5 |
| 2026-11-25 | plan-review vòng 1-2: spike (TestServer không có feature, Kestrel trả `413` rỗng, upstream đã nhận request, không `500`); 4 quyết định (Confirm #1-#4); sửa 3 FIX; plan `approved` | plan-review TB-025 |
| 2026-11-25 | Cập nhật tại chỗ sau khi TB-024 merge: nhánh fast-forward lên `main` `66673c2`, cập nhật mục "Đã có", thứ tự `BodyLimit` sau `RateLimiter`, thêm @S07, đóng P-01, cập nhật rủi ro R-01 và bài học | plan-init chạy lại, bản nháp cũ lệch `main` |
