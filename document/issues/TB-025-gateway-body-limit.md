---
key: TB-025
title: Gateway lát 5, giới hạn body 1 MiB
status: done
phase: 1
branch: feature/TB-025-gateway-body-limit
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-025 — Gateway lát 5, giới hạn body 1 MiB

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 5 của `document/plans/api-gateway-slices.md` (spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1, 2. Ước lượng của bảng lát: 1 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-007): Giới hạn body 1 MiB: `413` khi `Content-Length` lớn, ngắt luồng khi chunked
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] FR-007: `POST` có `Content-Length` đúng 1.048.576 byte được chuyển tiếp nguyên vẹn (@S01)
- [x] FR-007: `Content-Length` 1.048.577 byte nhận `413 problem+json` `PAYLOAD_TOO_LARGE`, upstream không nhận gì, có `X-Correlation-Id` (@S02)
- [x] FR-007: giới hạn cấu hình được (`Gateway:BodyLimit:MaxBytes`, mặc định 1.048.576), khoảng hợp lệ 1 đến 10.485.760 byte, ngoài khoảng làm khởi động thất bại (@S03)
- [x] FR-007: body chunked đúng giới hạn qua, vượt giới hạn nhận `413 problem+json` `PAYLOAD_TOO_LARGE` kèm `X-Correlation-Id`, không `500` (@S04, @S05)
- [x] NFR-006: log khi từ chối không chứa nội dung body (@S06)
- [x] Thứ tự design 6.4: IP đã hết hạn mức thì `429` đến trước `413` (@S07)
- [x] Gate xanh (`code-change-gate.md`); bảng lát và `document/issues/README.md` đồng bộ (LL-006)

## Kế hoạch
Plan: [`document/plans/TB-025-gateway-body-limit.md`](../plans/TB-025-gateway-body-limit.md) (`approved`), 3 bước B1-B3, 7 kịch bản @S01-@S07; không còn câu hỏi mở (P-01..P-05 đã đóng); spike của plan-review đã kiểm giả định Kestrel/YARP, kiểm lại ở test-init.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-10-25: plan-init lần đầu ở worktree `board-tb-025` (branch tạo từ `main` feb4278, khi TB-024 chưa merge).
- 2026-11-25: TB-024 đã merge (PR 18); nhánh fast-forward lên `main` `66673c2`; plan-init cập nhật tại chỗ (`BodyLimit` sau `RateLimiter`, thêm @S07, đóng P-01); đồng bộ lát 4 và TB-024 sang `done` (LL-006).
- 2026-11-25: mở PR https://github.com/sowndv02/teamboard/pull/19 (nhánh `feature/TB-025-gateway-body-limit` vào `main`); trạng thái review.
- 2026-11-25: bắt đầu coding (lát 5 `in-progress`). Sai lệch cơ chế so với plan B2: bản đầu dùng `IHttpMaxRequestBodySizeFeature` nhưng Kestrel đếm cả khung chunk nên từ chối body chunked đúng 1.048.576 byte (@S04 đỏ). Chuyển sang phương án thay thế mà plan A-01 đã nêu: `BodyLimitMiddleware` bọc `Request.Body` bằng `LimitedReadStream` đếm byte thân khi không có `Content-Length`, ném `BadHttpRequestException` 413, `UpstreamErrorClassifier` đổi thành `PayloadTooLarge` và `UpstreamErrorMiddleware` ghi `413 problem+json`. Hành vi spec không đổi. Design 6.3 đã cập nhật.
- Giới hạn đã biết: giới hạn chunked chỉ có hiệu lực ở route proxy (nơi body được đọc); route khác chỉ chịu kiểm `Content-Length`. YARP vẫn ghi cảnh báo `RequestBodyClient` kèm ngoại lệ cho mỗi request chunked quá lớn (R-07, lát 7 xem lại log).

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-025-gateway-body-limit.md` (draft, cập nhật sau TB-024), làn vừa, 3 bước, 7 kịch bản, 4 câu hỏi mở |
| plan-review | PASS | 2 | 2026-11-25 | plan `approved`; 4 quyết định (chunked `problem+json`, mọi request, khoảng 1-10 MiB, giữ một lát); 3 FIX theo spike (TestServer không có feature giới hạn, Kestrel trả `413` rỗng, không `500`); làn vừa |
| test-init | done | – | 2026-11-25 | 7 kịch bản (`body-limit.feature`) + 3 nhóm xUnit; đỏ 12 test đúng lý do (413 mong đợi nhưng nhận 201, thiếu mã `PAYLOAD_TOO_LARGE`, thiếu cấu hình và validation `MaxBytes`, classifier chưa có nhánh body quá lớn); 4 kịch bản xanh sẵn (hồi quy); gateway 188 test: 176 xanh, 12 đỏ |
| test-review | PASS | 1 | 2026-11-25 | 2 FIX đã sửa (thiếu kịch bản cho Confirm #2 và phần chunked của NFR-006: thêm @S08, @S09), 1 P3 người dùng bỏ qua; 9 kịch bản, gateway 191 test: 15 đỏ đúng lý do (413 mong đợi, nhận 201/200/404, thiếu mã, cấu hình, nhánh classifier), 176 xanh gồm 166 test cũ; review do agent đã viết test thực hiện |
| coding-start | done | – | 2026-11-25 | xanh 191/191 gateway (3 lần liên tiếp; nhóm TB-025 10/10) và toàn solution không docker (266/266: 40 + 11 + 3 + 21 + 191); build `-warnaserror` 0/0, format, vulnerable, compose config sạch; gitleaks chưa cài lúc đó (đã quét ở coding-review), quét secret thô sạch; nhóm docker không chạy (không đổi compose/Dockerfile); chưa commit |
| coding-review | PASS | 1 | 2026-11-25 | 0 P0-P2, 1 P3 test coverage (đã sửa: thêm `LimitedReadStreamTests`); probe F1-F8, B1-B4, BP1-BP6 đã kiểm; gate xanh: build `-warnaserror` 0/0, format, vulnerable, compose config, test không docker 271/271 (gateway 196); gitleaks chưa cài; review do agent đã viết code thực hiện, chưa có reviewer độc lập |
| create-pr | done | – | 2026-11-25 | https://github.com/sowndv02/teamboard/pull/19 |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-025:chunked-413-shape | Chunked vượt giới hạn: `413 problem+json` hay `413` rỗng | `413 problem+json` | 1 | 2026-11-25 |
| 2 | plan-review:TB-025:scope-all-requests | `413` áp mọi request hay chỉ route proxy | Mọi request | 1 | 2026-11-25 |
| 3 | plan-review:TB-025:maxbytes-range | Khoảng hợp lệ của `MaxBytes` | 1 đến 10.485.760 byte | 1 | 2026-11-25 |
| 4 | plan-review:TB-025:slice-size | Giữ một lát hay chia | Giữ một lát | 1 | 2026-11-25 |
| 5 | test:gateway:real-socket-given-leak | Step Given "the gateway runs on a real server socket" lộ chi tiết kỹ thuật (P3) | Bỏ qua (`skipped`): step hạ tầng bắt buộc vì TestServer không có giới hạn body; feature khác có step tương tự | 1 | 2026-11-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 body đúng 1.048.576 byte được chuyển tiếp | `body-limit.feature` @S01 (`BodyLimitSteps`) | không: xanh sẵn (chưa có giới hạn nào), test hồi quy | xanh (coding-start, 2026-11-25) |
| @S02 vượt 1 byte: `413 problem+json`, upstream không nhận | @S02 | có: mong đợi 413, nhận 201 | xanh (coding-start, 2026-11-25) |
| @S03 giới hạn từ cấu hình (1024 qua, 1025 chặn) | @S03 (Outline, 2 dòng) | dòng 1025: mong đợi 413, nhận 201; dòng 1024 xanh sẵn (hồi quy) | xanh (coding-start, 2026-11-25) |
| @S04 chunked đúng giới hạn qua | @S04 (Kestrel thật) | không: xanh sẵn (Kestrel mặc định 30 MB), test hồi quy | xanh (coding-start, 2026-11-25) |
| @S05 chunked vượt giới hạn: `413 problem+json` | @S05 (Kestrel thật) | có: mong đợi 413, nhận 201 | xanh (coding-start, 2026-11-25) |
| @S06 nội dung body bị từ chối không vào log | @S06 | có: mong đợi 413, nhận 201 (phần log xanh sẵn, chỉ có nghĩa khi 413 đã có) | xanh (coding-start, 2026-11-25) |
| @S07 hết hạn mức thì `429` trước `413` | @S07 | không: xanh sẵn vì chưa có `BodyLimit`; khoá thứ tự, sẽ đỏ nếu `BodyLimit` đặt trước `RateLimiter` | xanh (coding-start, 2026-11-25) |
| @S08 `413` áp mọi request (route lạ, health) | @S08 (Outline: `POST /no-such-route`, `GET /health/live`) | có: mong đợi 413, nhận 404 và 200 | xanh (coding-start, 2026-11-25) |
| @S09 nội dung body chunked bị từ chối không vào log | @S09 (Kestrel thật, giới hạn 1024) | có: mong đợi 413, nhận 201 | xanh (coding-start, 2026-11-25) |
| Mã `PAYLOAD_TOO_LARGE` 413 | `GatewayErrorCodesTests.All_ContainsPayloadTooLarge_As413WithoutInternals` | có: thiếu mã | xanh (coding-start, 2026-11-25) |
| `MaxBytes` mặc định 1 MiB | `BodyLimitConfigurationTests.ShippedLimit_IsOneMebibyte` | có: thiếu mục `Gateway:BodyLimit` | xanh (coding-start, 2026-11-25) |
| `MaxBytes` ngoài khoảng 1 đến 10.485.760 làm khởi động thất bại | `BodyLimitConfigurationTests.Start_WithInvalidLimit_FailsAndNamesTheSetting` (4 giá trị) | có: khởi động vẫn thành công | xanh (coding-start, 2026-11-25) |
| `MaxBytes` hợp lệ khởi động được | `BodyLimitConfigurationTests.Start_WithValidLimit_Succeeds` (3 giá trị) | không: xanh sẵn (hồi quy, để validation không chặn nhầm) | xanh (coding-start, 2026-11-25) |
| `RequestBodyClient` do body quá lớn → `PayloadTooLarge` (trực tiếp, bọc `AggregateException`) | `BodyLimitClassifierTests` (2 test) | có: nhận `ClientCancelled` | xanh (coding-start, 2026-11-25) |
| `RequestBodyClient` khác, không ngoại lệ, response đã bắt đầu → vẫn `ClientCancelled` | `BodyLimitClassifierTests` (3 test) | không: xanh sẵn (hồi quy, giữ hành vi cũ) | xanh (coding-start, 2026-11-25) |

Lệnh đã chạy: `dotnet build src/gateway.Tests -warnaserror` (0 cảnh báo, 0 lỗi); `dotnet test src/gateway.Tests --no-build --filter "Category!=docker"` → 188 test, 176 xanh, 12 đỏ (đúng 12 test đỏ ở bảng trên; 166 test cũ đều xanh); `dotnet test ... --filter "Category=TB-025"` chạy 3 lần: 4 đỏ, 4 xanh mỗi lần (không flaky); `dotnet format src/teamboard.slnx --verify-no-changes` sạch.

Khung tối thiểu cho biên dịch: một giá trị enum rỗng `UpstreamFailure.PayloadTooLarge` ở `src/gateway/UpstreamErrorClassifier.cs` (chưa có logic). Hỗ trợ test: `UpstreamStub` đếm byte body (`ReceivedBodyBytes`), `GatewayContext.UseRealKestrel` (host Kestrel thật cho chunked).

Ghi nhận cho test-review: (1) quyết định Confirm #2 "`413` áp cho mọi request" (kể cả route không tồn tại) chưa có kịch bản riêng vì plan chỉ liệt kê @S01-@S07; (2) rủi ro R-06 của plan (hai host Kestrel song song gây `AddressInUse`) không tái hiện được ở test-init: @S04 và @S05 chạy tuần tự (assembly đã tắt chạy song song) xanh 3 lần liên tiếp.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | `9dd37eb` (kèm sửa chưa commit) | Sẵn sàng code: 0 P0-P2 mở, P3 `real-socket-given-leak` đã bỏ qua có lý do (Confirm #5) | không |

Bảng reconcile (kịch bản ↔ test) nằm ở mục "Kịch bản và TDD log" phía trên, đủ 9 kịch bản và 4 nhóm xUnit, không có kịch bản thiếu test. Đã kiểm: A (khai báo, tag `@TB-025` và `@S01`-`@S09`, không `@wip`, ASCII), B (phủ FR-007, NFR-004, NFR-006, đường lỗi và biên 1024/1025, 1.048.576/1.048.577), D (đỏ đúng lý do, `ReceivedBodyBytes` assert byte thật, không `sleep`, không logic production), E (xUnit 2.9.3 + Reqnroll 3.3.4, lệnh ghi ở TDD log). Mutation nghĩ đến: `BodyLimit` đặt trước `RateLimiter` làm @S07 đỏ; bỏ nhánh `Content-Length` làm @S02, @S08 đỏ; bỏ `MaxRequestBodySize` làm @S05, @S09 đỏ.

Giới hạn đã biết: (1) @S05 và @S09 gửi body chunked qua socket thật; khi gateway trả `413` sớm client có thể gặp lỗi ghi thay vì đọc được `413` (R-04 của plan), cần quan sát khi coding, chạy lặp 10 lần; (2) review do agent đã viết test thực hiện, chưa có reviewer độc lập.

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | `314ee2b` (base `main` `66673c2`) | Sẵn sàng merge (S1-S15 đã kiểm; `gitleaks` sạch) | không |

**Báo cáo vòng 1 (tóm tắt):**

| Probe | Áp dụng? | Kết quả |
|---|---|---|
| F1 lỗi | Có | `BadHttpRequestException` được YARP bắt và phân loại, không nuốt lỗi; không `catch` rỗng trong code mới |
| F2 validation | Có | `MaxBytes` kiểm 1 đến 10.485.760 lúc khởi động (test 4 giá trị sai, 3 đúng) |
| F3 độ sâu test | Có | Assert byte thật ở upstream (`ReceivedBodyBytes`), upstream không nhận gì khi 413, log không chứa nội dung; P3 thiếu unit test đã sửa |
| F4 idempotency, F5, F7 | Không áp dụng | Không có consumer, field chung hay persistence |
| F6 tiêu chí issue | Có | Mọi tiêu chí có test xanh (xem bảng TDD log); tiêu chí đồng bộ bảng lát làm tiếp ở create-pr (lát 5 sang `review`) |
| F8 bảo mật | Có | 413 không lộ giá trị giới hạn, host hay ngoại lệ; log không chứa body; DoS body lớn bị chặn sớm (Content-Length) hoặc khi đọc (chunked) |
| B1 code dùng chung | Có | Chỉ đụng `src/gateway`; không đổi `src/shared`; grep `UpstreamFailure` chỉ dùng trong gateway |
| B2 contract | Có | Thêm `413 PAYLOAD_TOO_LARGE` (cộng thêm, khớp design 6.3); không consumer nào bị phá; không cần ADR |
| B3, B4 | B4 có | Không DB, không event; tác động: contract công khai gateway thêm một mã lỗi, health và route khác không đổi |
| BP1-BP6 | Có | Options theo mẫu dự án; async có `CancellationToken`; log không có nội dung body; một chỗ xử lý "body quá lớn" (classifier); không hardcode (cấu hình `appsettings.json`) |

**Bổ sung vòng 1 theo trụ cột Security mới của `code-review.md` (commit `58897e3`, đến sau khi vòng 1 chạy; đã chạy lại phần bảo mật trên cùng diff):**

| Probe | Áp dụng? | Kết quả |
|---|---|---|
| S1 AuthN/AuthZ | Không áp dụng | Không endpoint mới; gateway chưa kiểm JWT theo spec mục 10 (cảnh báo "không ra internet trước JWT" ở lát 7) |
| S2 Input, kích thước | Có | Đã đọc đường đi: `Content-Length` > giới hạn bị chặn trước khi chuyển tiếp; chunked bị đếm khi đọc (`LimitedReadStream`); `MaxBytes` kiểm khoảng lúc khởi động; không parse thân, không injection |
| S3 Secret, PII | Có | Không secret trong diff; log không chứa nội dung body (@S06, @S09); chuỗi `secret-board-title-` chỉ là dấu hiệu trong test, không phải secret |
| S4 Lộ thông tin | Có | `413` từ registry `problem+json`, không lộ giá trị giới hạn, host hay ngoại lệ (test @S02, `GatewayErrorCodesTests`) |
| S5 Token, cookie | Không áp dụng | Không có token hay cookie trong diff |
| S6 Mật mã | Không áp dụng | Không dùng mật mã |
| S7 Cấu hình, header | Có | Không đổi CORS, header, cổng hay bind; chỉ thêm khoá `Gateway:BodyLimit:MaxBytes` |
| S8 Giới hạn, DoS | Có | Đây chính là biện pháp giới hạn body; rate limit đứng trước (@S07). Tồn đọng đã ghi: giới hạn chunked chỉ ở route proxy, log cảnh báo YARP mỗi request chunked quá lớn (R-07) |
| S9 Phụ thuộc | Có | Không đổi file `.csproj`; `dotnet list package --vulnerable --include-transitive` không có gói lỗ hổng |
| S10 Race, trạng thái | Không áp dụng | Không ghi trạng thái; bộ đếm `_total` thuộc từng request |
| S11 Client-side | Không áp dụng | Diff không chạm `src/web` |
| S12 Mass assignment, quyền theo logic | Không áp dụng | Không có DTO hay entity; không có quyền theo trạng thái |
| S13 IaC, container, CI/CD | Không áp dụng | Diff không chạm `infra/`, Dockerfile, compose hay CI (chỉ `src/gateway`, `src/gateway.Tests`, tài liệu, `.agents/` do commit `58897e3`) |
| S14 Audit log | Không áp dụng | Không có hành động nhạy cảm mới; log "413" mang `correlationId` qua scope; rate limit đã có từ lát 4 |
| S15 Công cụ quét | Có | `dotnet list package --vulnerable` sạch; `gitleaks` 8.30.1 (người dùng cài 2026-11-25): `gitleaks git --log-opts="main..HEAD"` quét 8 commit, không có rò rỉ; `gitleaks dir` trên file issue chưa commit, không có rò rỉ. SAST (Semgrep/CodeQL) chưa có trong dự án: không bắt buộc cho đổi nhỏ theo rule |

Không có finding bảo mật. S15 đã đóng. `gitleaks` mới chạy ở lần này nên `COMMANDS.md` vẫn ghi "🚧 chưa cài" (ngoài phạm vi issue, báo lại).

Lăng kính: Chất lượng đạt (lỗi qua registry, không nuốt lỗi); Bảo mật đạt (không secret, phụ thuộc không lỗ hổng); Chi phí đạt (bọc stream rẻ, không log thêm ngoại lệ).

Giới hạn đã biết: giới hạn chunked chỉ hiệu lực ở route proxy; YARP vẫn ghi cảnh báo `RequestBodyClient` kèm ngoại lệ cho mỗi request chunked quá lớn (R-07); gitleaks chưa cài; review do agent đã viết code thực hiện.

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 (test-review) | test:gateway:scope-all-requests-no-scenario | FIX (theo Confirm #2) | `body-limit.feature`, plan mục 2 | Quyết định "`413` áp mọi request" chưa có kịch bản | Thêm @S08 (Outline: `POST /no-such-route`, `GET /health/live` với body 1.048.577 byte); cập nhật plan | @S08 đỏ: mong đợi 413, nhận 404 và 200 |
| 1 (test-review) | test:gateway:chunked-log-no-scenario | FIX (theo plan B2, NFR-006) | `body-limit.feature`, `BodyLimitSteps.cs`, plan mục 2 | Plan nói @S06 phủ cả phần chunked nhưng chưa có test | Thêm @S09 (Kestrel thật, giới hạn 1024, body chunked 2048 byte); thêm step "chunked body ... made of"; cập nhật plan | @S09 đỏ: mong đợi 413, nhận 201 |
| 1 (coding-review) | gateway:LimitedReadStream:no-unit-test | FIX (P3, test coverage) | `src/gateway.Tests/LimitedReadStreamTests.cs`, `src/gateway/LimitedReadStream.cs` | Luật đếm byte chunked (đúng giới hạn qua, quá 1 byte thì 413, một lần đọc vượt giới hạn, thân rỗng, đọc đồng bộ) chỉ được phủ bằng test tích hợp 1 MiB qua socket thật | Thêm 5 unit test với `MemoryStream`; đổi `LimitedReadStream` từ `internal` sang `public` để test truy cập (cùng cách `UpstreamErrorClassifier`) | `LimitedReadStreamTests` 5/5 xanh; gateway 196/196 |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
