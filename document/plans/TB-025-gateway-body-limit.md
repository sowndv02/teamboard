---
title: Gateway lát 5, giới hạn body 1 MiB
issue: TB-025
status: draft            # draft | in-review | approved | done
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

Hành vi đã biết từ tài liệu học (chưa kiểm trên mã repo): `MaxRequestBodySize` của Kestrel/route trả `413` nhưng upstream vẫn thấy yêu cầu (`document/learning/api-gateway/part-5-production-security-ops.md`, Lab 5.2), nên muốn "không gửi xuống upstream" phải tự kiểm `Content-Length` bằng middleware đứng trước YARP (design 6.3).

## 1. Phạm vi của lát

**Làm:**
- Mã lỗi `PAYLOAD_TOO_LARGE` (413) trong `GatewayErrorCodes` (hợp đồng công khai, DA-02) và mở rộng `GatewayErrorCodesTests`.
- Giới hạn cấu hình được `Gateway:BodyLimit:MaxBytes`, mặc định 1.048.576 byte (1 MiB), qua Options có `ValidateOnStart` (`backend-engineering.md` mục 7); ngoài khoảng hợp lệ thì khởi động thất bại.
- Middleware `BodyLimit` đứng ngay trước YARP, **sau** `RateLimiter` (design 6.4; request vượt hạn mức nhận `429` trước khi bị xét kích thước, và request bị `413` vẫn tính vào hạn mức của IP): request có `Content-Length` > giới hạn bị `413 problem+json` ngay, **không** gửi xuống upstream; đúng bằng giới hạn thì chuyển tiếp bình thường.
- Body chunked (không có `Content-Length`) vượt giới hạn: ngắt luồng và trả lỗi; upstream có thể đã nhận một phần body (đã chấp nhận ở design-review). Giới hạn do middleware đặt cho request (cơ chế cụ thể: giả định A-01).
- Phản hồi `413` mang `code`, `traceId`, `correlationId` trong body và `X-Correlation-Id` ở header; không lộ giá trị giới hạn nội bộ ngoài `detail` chuẩn, không lộ host/tên nội bộ; log không ghi body (NFR-006).
- Việc TB-022/TB-023 để lại: kiểm `ExceptionHandler` có đổi ngoại lệ kèm status riêng của framework (`BadHttpRequestException`, 413) thành `500` không, và xử lý đúng trong luồng chunked.
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
| FR-007 chunked vượt giới hạn → ngắt luồng và trả lỗi | 6.3 | B2 | @S05 | Host test (Kestrel thật): chunked 1.048.577 byte → 413 `PAYLOAD_TOO_LARGE` (nếu header chưa gửi) hoặc kết nối bị ngắt; không `500` |
| NFR-006 log không chứa body | mục 11 | B1, B2 | @S06 | `CapturingLoggerProvider`: log của 413 không chứa nội dung body và có `correlationId` |
| Thứ tự middleware (`BodyLimit` ngay trước YARP) khoá phần quan sát được | 6.4 | B1 | @S02 (kèm), @S07 | 413 mang `X-Correlation-Id` (Correlation ngoài); khi IP đã hết hạn mức ghi, request có `Content-Length` lớn nhận `429` chứ không phải `413` (`RateLimiter` trước `BodyLimit`) |

Kịch bản (ID duy nhất trong `body-limit.feature`; viết ở test-init):
- @S01 Body đúng 1.048.576 byte có `Content-Length` được chuyển tiếp nguyên vẹn.
- @S02 `Content-Length` 1.048.577 byte nhận `413` `PAYLOAD_TOO_LARGE`, upstream không nhận gì, có `X-Correlation-Id`.
- @S03 Giới hạn đọc từ cấu hình (ghi đè 1024 byte): 1024 byte qua, 1025 byte bị `413`.
- @S04 Body chunked đúng giới hạn được chuyển tiếp.
- @S05 Body chunked vượt giới hạn bị ngắt luồng, client không nhận `500`.
- @S06 Log của gateway khi từ chối body không chứa nội dung body (NFR-006).
- @S07 Thứ tự với rate limit: IP đã hết hạn mức ghi, request kế tiếp có `Content-Length` lớn nhận `429`, không phải `413` (dùng `X-Test-Remote-Ip` và ngưỡng nhỏ qua cấu hình; không ghi đè `WindowSeconds` dưới 30 giây).
- Test xUnit không qua feature: `GatewayErrorCodesTests` (mã `PAYLOAD_TOO_LARGE` + status 413), `BodyLimitOptionsTests` (mặc định, khoảng), `ConfigurationTests` (giá trị trong `appsettings.json`), unit của middleware nếu tách logic quyết định.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước; mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff). Khung rỗng chỉ để test biên dịch được. **Spike ở test-init** kiểm các giả định mục 7 trên mã repo.

### B1. Hợp đồng lỗi, giới hạn cấu hình và từ chối theo `Content-Length` (đường chính)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/body-limit.feature` (@S01, @S02, @S03, @S06 phần log, @S07 thứ tự với rate limit); unit `GatewayErrorCodesTests` (mã mới), `BodyLimitOptionsTests`; `UpstreamStub` đếm số byte body nhận được; `GatewayContext` hỗ trợ gửi body theo số byte với `Content-Length`. Đỏ vì chưa có mã, Options hay middleware (S01 xanh sẵn vì hiện chưa giới hạn: ghi TDD log là hồi quy).
- **File/module dự kiến chạm:** `src/gateway/GatewayErrorCodes.cs`; mới `src/gateway/BodyLimitOptions.cs` (`Gateway:BodyLimit:MaxBytes`, `ValidateOnStart`, theo mẫu `RateLimitOptions`); mới `src/gateway/BodyLimitMiddleware.cs` (so `Content-Length` với giới hạn, ghi `problem+json` bằng `ProblemFactory`); `src/gateway/Program.cs` (đăng ký Options, `UseMiddleware<BodyLimitMiddleware>()` ngay sau `UseRateLimiter()` và trước `MapReverseProxy`, cập nhật comment thứ tự); `src/gateway/appsettings.json`; `src/gateway.Tests/`.
- **Hợp đồng thay đổi:** thêm `413 problem+json` `PAYLOAD_TOO_LARGE` công khai. `api-endpoints.md` không cần sửa (kiểm lại khi viết; design bảng mã đã có dòng này).
- **Migration / cấu hình:** khoá `Gateway:BodyLimit:MaxBytes` (mặc định 1048576) trong `appsettings.json`; không biến môi trường mới.
- **Tài liệu cần cập nhật:** không thêm (design 6.3 đã mô tả).
- **Kiểm chứng:** @S01–@S03, @S06 (phần `Content-Length`) xanh; test lát 1–3 vẫn xanh; `dotnet build src/teamboard.slnx -warnaserror`; `dotnet format src/teamboard.slnx --verify-no-changes`.
- **Phụ thuộc:** không.

### B2. Body chunked: ngắt luồng khi vượt giới hạn
- **Kịch bản/test sẽ viết (đỏ trước):** @S04, @S05 (và @S06 phần chunked); cần host Kestrel thật vì TestServer có thể không áp `MaxRequestBodySize` (giả định A-02, spike); test gửi stream không biết độ dài (chunked) qua cổng thật; khẳng định client không nhận `500`, kết quả là `413` hoặc kết nối bị ngắt. Đỏ vì chunked hiện đi hết xuống upstream.
- **File/module dự kiến chạm:** `src/gateway/BodyLimitMiddleware.cs` (đặt giới hạn cho request qua `IHttpMaxRequestBodySizeFeature` khi không có `Content-Length`; xử lý `BadHttpRequestException` 413 để không thành `500`); có thể `src/gateway/UpstreamErrorClassifier.cs` (lỗi `RequestBodyClient` do vượt giới hạn không được coi là "client huỷ" mà bỏ qua nếu header chưa gửi); `src/gateway.Tests/GatewayContext.cs` (chế độ host Kestrel thật) và `Support/`.
- **Hợp đồng thay đổi:** không thêm (cùng `413`); chunked vượt giới hạn có thể thành ngắt kết nối nếu response đã bắt đầu.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** design 6.3 và spec FR-007 chỉ khi spike cho thấy hành vi chunked khác chữ hiện tại (hỏi người dùng trước, không tự sửa).
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
| Integration (host Kestrel thật) | Body chunked bằng và vượt giới hạn | Gateway chạy trên Kestrel cổng ngẫu nhiên (nếu TestServer không áp `MaxRequestBodySize`, giả định A-02) | Chỉ cho nhóm chunked, giữ nhỏ |

Không có test Docker (không phụ thuộc môi trường). Công cụ: xUnit + Reqnroll.xUnit theo `config/tool-versions.yaml` (đã dùng ở lát trước). Test BDD viết tiếng Anh, ASCII (`test-review.md` mục 1). Kim tự tháp: nhiều unit/host test nhanh, nhóm Kestrel thật chỉ cho chunked.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| R-01 `BodyLimit` đặt sai chỗ so với `RateLimiter` (design yêu cầu `RateLimiter` trước `BodyLimit`) | `413` thoát khỏi hạn mức hoặc hạn mức bị bỏ qua | TB-024 đã merge nên đặt đúng chỗ ngay (một dòng sau `UseRateLimiter`); @S07 khoá thứ tự quan sát được | Revert B1 |
| R-02 Hành vi Kestrel/YARP với body vượt giới hạn khác giả định (upstream vẫn thấy request; `BadHttpRequestException` bị handler đổi thành `500`; `RequestBodyClient` bị coi là client huỷ nên không có response) | Trả `500` hoặc không có phản hồi | Spike ở test-init; mục B2 xử lý tường minh; thấy lệch design thì dừng và hỏi | Revert B2 |
| R-03 TestServer không áp `MaxRequestBodySize` nên test chunked xanh giả | Bỏ sót lỗi thật | Dùng host Kestrel thật cho @S04, @S05 | Không áp dụng |
| R-04 Client ghi body lớn trong khi gateway đã trả 413 và đóng kết nối nên client thấy lỗi ghi thay vì 413 | Test flaky | Test dùng `Content-Length` giả lớn với body nhỏ nơi kiểm header, và body thật 1 MiB+1 chỉ ở bước kiểm byte; chờ có điều kiện | Sửa test |
| R-05 Giá trị `MaxBytes` ngoài khoảng (0, âm, rất lớn) | Gateway vô tác dụng hoặc chặn mọi thứ | `ValidateOnStart` với khoảng hợp lệ; unit test | Revert B1 |

Ảnh hưởng consumer (service khác): thêm `413 problem+json` ở contract công khai của gateway (cộng thêm, trước đây request lớn đi hết tới upstream); `board-service` không đổi. Bảo mật: giới hạn kích thước chặn request khổng lồ sớm (STRIDE design mục 8, D: body lớn); 413 không lộ host/tên nội bộ; log không ghi body, `Authorization`; không secret mới. Chi phí: không tài nguyên mới, không Docker.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `security`, `observability`, `workflow`, `testing`): LL-006 (đồng bộ bảng lát với issue) áp dụng ở B3; LL-005 (câu hỏi mở có lựa chọn và khuyến nghị) áp dụng cho mục 7; biện pháp 4 của LL-006 (kiểm lát trước ở đầu `plan-init`) đã làm: lát 4 đã merge (PR 18), bảng lát và `document/issues/README.md` còn ghi lát 4 và TB-024 là `review`, được đồng bộ sang `done` cùng commit với plan này; từ TB-023/TB-024: không `Response.Clear()` để giữ header của middleware khác, test log chỉ trên logger của gateway, không ghi đè `WindowSeconds` dưới 30 giây.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx --filter "Category!=docker"`; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; quét secret thô (`gitleaks` chưa cài, ghi rõ); nhóm `docker` không cần chạy (không đổi compose/Dockerfile), chạy hồi quy `gateway` nếu bước nào chạm Dockerfile.
- Tiêu chí hoàn thành: theo issue TB-025.
- Tài liệu/config cập nhật: như B3; bảng lát và `document/issues/README.md` (LL-006).

## 7. Câu hỏi mở và giả định

Giả định (chưa xác minh trên mã repo, kiểm ở test-init):
- A-01 Cơ chế ngắt chunked là `IHttpMaxRequestBodySizeFeature.MaxRequestBodySize` do middleware đặt cho từng request; thay thế nếu không đủ là bọc `Request.Body` bằng stream đếm byte.
- A-02 `TestServer` không áp `MaxRequestBodySize`, nên chunked cần host Kestrel thật.
- A-03 `ExceptionHandler` (handler `500` của gateway) bắt `BadHttpRequestException` 413 và trả `500` nếu không xử lý riêng (suy luận từ TB-022).
- A-04 Lỗi forwarder khi body bị ngắt có hậu tố `*Client` nên `UpstreamErrorClassifier` hiện coi là client huỷ và không ghi response.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-01 | Thứ tự làm/merge với lát 4 | R-01, B1 | – | answered bởi thực tế: TB-024 đã merge (PR 18, `main` `66673c2`); lát 5 đặt `BodyLimit` sau `RateLimiter` theo design 6.4, thêm @S07 |
| P-02 | Chunked vượt giới hạn khi header chưa gửi: trả `413 problem+json` (khuyến nghị: nhất quán với `Content-Length`, client biết lý do) hay chỉ ngắt kết nối (đúng chữ "ngắt luồng" của design, đơn giản hơn)? Spec FR-007: "ngắt luồng và trả lỗi". Nếu chưa chốt: plan viết theo khuyến nghị (413 khi còn kịp, ngắt khi header đã gửi), test @S05 chấp nhận cả hai. | B2, @S05 | sondv | open |
| P-03 | `413` áp cho mọi request kể cả route không tồn tại (`/foo` kèm `Content-Length` lớn thì 413 thay vì 404) hay chỉ route proxy? Khuyến nghị áp cho mọi request (đơn giản, đúng "chặn request khổng lồ sớm", middleware đứng trước endpoint). Nếu chưa chốt: áp cho mọi request. | B1 | sondv | open |
| P-04 | Khoảng hợp lệ của `MaxBytes` (đề xuất 1 đến 10 MiB) và có thêm giới hạn riêng cho health không (khuyến nghị không). Nếu chưa chốt: dùng 1 đến 10.485.760. | B1 | sondv | open |
| P-05 | Cỡ lát: 7 kịch bản, 3 bước, ~1 ngày: giữ nguyên (khuyến nghị) hay chia. | cả plan | sondv | open |

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | plan-init lát 5 |
| 2026-11-25 | Cập nhật tại chỗ sau khi TB-024 merge: nhánh fast-forward lên `main` `66673c2`, cập nhật mục "Đã có", thứ tự `BodyLimit` sau `RateLimiter`, thêm @S07, đóng P-01, cập nhật rủi ro R-01 và bài học | plan-init chạy lại, bản nháp cũ lệch `main` |
