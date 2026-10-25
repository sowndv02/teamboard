---
key: TB-023
title: Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)
status: in-progress
phase: 1
branch: feature/TB-023-gateway-downstream-errors
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-023 — Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)

## Bối cảnh
Lát 3 của `document/plans/api-gateway-slices.md` (feature api-gateway; spec `document/spec/api-gateway.md` FR-006 và NFR-003, design `document/design/api-gateway.md` D4 và mục 8; issue tài liệu TB-007). Lát 1 (TB-010) và lát 2 (TB-022, PR 15) đã merge: gateway có route boards, health, `X-Correlation-Id`, ExceptionHandler `500` và trace nối sang `board-service`. Hiện lỗi kết nối tới upstream còn rơi vào ExceptionHandler hoặc mã mặc định của YARP; lát này đặt hợp đồng lỗi công khai của phía sau.

**Phụ thuộc:** TB-010, TB-022 đã merge (2026-10-25). Branch tạo từ `main` ngày 2026-10-25.

## Phạm vi
- Làm (theo FR-006, NFR-003, design D4):
  - Timeout cấu hình được: tổng 30 giây theo `Timeout` của route và `ActivityTimeout` ≤ 30 giây của cluster (giả định A-03).
  - Ánh xạ lỗi của forwarder thành `problem+json` **trước khi gửi header**: `503 UPSTREAM_UNAVAILABLE` (không có đích khả dụng hoặc không kết nối được), `502 UPSTREAM_BAD_GATEWAY` (lỗi giao thức, upstream ngắt khi chưa trả header), `504 UPSTREAM_TIMEOUT` (quá timeout trước header); mã lỗi ở bảng mã của gateway, không lộ host nội bộ hay stack trace.
  - `Retry-After` trên `503` (mặc định 5 giây, cấu hình được; giả định DA-05).
  - Upstream đã trả header rồi mới ngắt hoặc treo: gateway ngắt kết nối với client, ghi log kèm `correlationId` và tăng metric `gateway_upstream_aborted` (metric chỉ đếm, không nhãn `correlationId`) (không đổi được status đã gửi).
- Không làm: retry và circuit breaker (spec mục 4), rate limit (lát 4), body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention và graceful shutdown (lát 7), smoke với endpoint boards thật (lát 8), sửa `board-service` hay `identity-service`.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Upstream không kết nối được (cổng đóng) trả `503 problem+json` `UPSTREAM_UNAVAILABLE` kèm `Retry-After`, có `traceId` và `correlationId`.
- [x] Upstream ngắt kết nối hoặc trả lỗi giao thức trước khi trả header trả `502 problem+json` `UPSTREAM_BAD_GATEWAY`.
- [x] Upstream không trả header trong thời gian timeout (cấu hình ngắn trong test; timeout kết nối cũng là 504) trả `504 problem+json` `UPSTREAM_TIMEOUT`.
- [x] Upstream đã trả header rồi ngắt hoặc treo: client bị ngắt kết nối, có log Warning kèm `correlationId` và metric `gateway_upstream_aborted` (không nhãn).
- [x] Phản hồi lỗi không chứa host, cổng hay tên nội bộ của upstream, không có stack trace.
- [x] Giá trị timeout và `Retry-After` đọc từ cấu hình, không hardcode.
- [x] `/health/live` và `/health/ready` vẫn 200 khi upstream chết.
- [x] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.

## Kế hoạch
Plan chi tiết: `document/plans/TB-023-gateway-downstream-errors.md`.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ lát 3 của bảng lát; branch tạo từ `main` (sau PR 15).
- 2026-10-25: coding-start xong B1-B4 (cục bộ, chưa push). @S10 xác nhận đỏ trên bản mã trước B1 rồi xanh sau B1. `gitleaks` chưa cài, chỉ grep thô diff.
- 2026-10-25: test-review chấp nhận rủi ro log `Proxying to ...` của YARP chứa query string trong lát này; đề xuất hạ `Logging:LogLevel:Yarp.ReverseProxy` xuống Warning ở lát 7 (TB-027).
- 2026-10-25: plan-review chốt 7 điểm; sửa spec FR-006 và design D4, 6.4, 9 (thêm `RequestTimeouts` sau `ExceptionHandler`, timeout kết nối là 504, metric không mang `correlationId`).

## Workflow

Làn: lớn (hợp đồng lỗi công khai của gateway, rủi ro hành vi YARP khác giả định, design mục 16). Spec/design/bảng lát dùng chung ở TB-007; bước bắt đầu từ plan-init của lát.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | `document/plans/TB-023-gateway-downstream-errors.md` (draft), 4 bước B1-B4, 10 kịch bản @S01-@S10, 4 câu hỏi mở P-01..P-04, 5 giả định cần spike ở test-init |
| plan-review | PASS | 1 | 2026-10-25 | plan `approved`; 7 quyết định (vị trí `UseRequestTimeouts`, metric không mang `correlationId`, timeout kết nối 504, lỗi lạ 502, client huỷ, giữ cỡ lát, khoảng `Retry-After`), 11 FIX; review độc lập kèm spike YARP |
| test-init | done | – | 2026-10-25 | 34 test đỏ, 52 xanh ở lần đầu (sau test-review: 49 đỏ, 56 xanh, tổng 105); @S10 Docker chưa chạy được lúc đó (Docker Desktop tắt), đã xác nhận ở coding-start |
| test-review | PASS | 2 | 2026-10-25 | 16 finding vòng 1 (1 P1, 7 P2, 8 P3) đã sửa, 3 quyết định (client huỷ kiểm ở host test xUnit, @S08 thành xUnit, log YARP để lát 7); review độc lập (sub-agent) kèm cài đặt tham chiếu 105/105 xanh và 4 đột biến bị bắt; @S10 chưa chạy được (Docker tắt) |
| coding-start | done | – | 2026-10-25 | xanh 105/105 gateway (10 lần liên tiếp) và toàn solution không docker; nhóm docker từng assembly xanh (gateway 3 gồm @S10, board-service 2, identity 7); 4 commit cục bộ B1 ae73c92, B2 1577fab, B3 a50ac6b, B4 (ghi nhật ký); build, format, vulnerable, compose config sạch; gitleaks chưa cài |
| coding-review | PASS | 2 | 2026-10-25 | review độc lập (sub-agent) 2 vòng, không P0-P1; 4 CONFIRM đã chốt (bỏ Response.Clear(), log ngoại lệ, danh sách SocketError, AddMeter), 6 FIX đã sửa, 3 P3 vòng 2 đã sửa; test, build, format, compose config xanh sau sửa |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-023:p01-request-timeouts-position | Vị trí `UseRequestTimeouts` | Ngay sau `ExceptionHandler`, sửa design 6.4 | 1 | 2026-10-25 |
| 2 | plan-review:TB-023:metric-correlationid | Metric có `correlationId`? | Chỉ ở log; sửa spec, design, issue | 1 | 2026-10-25 |
| 3 | plan-review:TB-023:connect-timeout-mapping | Timeout kết nối 503 hay 504 | 504, ghi vào design và spec | 1 | 2026-10-25 |
| 4 | plan-review:TB-023:p03-unmapped-forwarder-errors | Lỗi forwarder ngoài bảng trước header | 502 kèm log chi tiết | 1 | 2026-10-25 |
| 5 | plan-review:TB-023:p02-client-cancel | Client huỷ request | Không ghi response, log Information | 1 | 2026-10-25 |
| 6 | plan-review:TB-023:slice-size | Giữ hay chia lát | Giữ nguyên | 1 | 2026-10-25 |
| 7 | plan-review:TB-023:retry-after-range | Khoảng `Retry-After` | Số nguyên 1..3600, mặc định 5 | 1 | 2026-10-25 |
| 8 | test-review:TB-023:client-cancel-no-host-test | Kiểm hành vi client huỷ ở đâu | Host test xUnit không qua feature (`ClientCancellationTests`) | 1 | 2026-10-25 |
| 9 | test-review:TB-023:s08-config-scenario-placement | Kịch bản @S08 không có When | Chuyển thành test xUnit `ConfigurationTests`, bỏ @S08 khỏi feature | 1 | 2026-10-25 |
| 10 | test-review:TB-023:yarp-log-leaks-query | Log Information của YARP ghi cả query string | Chỉ kiểm log của gateway; hạ log YARP xuống Warning ở lát 7 (NFR-006); rủi ro được chấp nhận trong lát này | 2 | 2026-10-25 |
| 11 | coding-review:TB-023:position-and-clear-vs-cors | Middleware lỗi gọi Response.Clear() và đứng ngoài vị trí CORS | Bỏ Response.Clear(); ghi vị trí `UpstreamError` vào design 6.4 | 1 | 2026-10-25 |
| 12 | coding-review:TB-023:warning-logs-exception-duplicate-and-host | Log ngoại lệ trùng YARP và chứa host:port | 503/504 chỉ log loại lỗi và tên kiểu ngoại lệ; 502 và ngắt sau header giữ ngoại lệ | 1 | 2026-10-25 |
| 13 | coding-review:TB-023:connection-error-whitelist | Danh sách SocketError cho 503 | Giữ danh sách, thêm unit test các mã còn lại | 1 | 2026-10-25 |
| 14 | coding-review:TB-023:addmeter-untested | AddMeter chưa có test | Chấp nhận, ghi giới hạn (không thêm package OpenTelemetry in-memory) | 1 | 2026-10-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 (503, GET/HEAD/POST) | `downstream-errors.feature` @S01 (Outline 3 dòng) | Đỏ: mong 503, nhận 502 (YARP trả 502 mặc định khi cổng đóng) | Xanh 2026-10-25 (B1-B3) |
| @S02 (không có upstream khả dụng) | `downstream-errors.feature` @S02 | Đỏ: 503 của YARP không có thân `problem+json` | Xanh 2026-10-25 (B1-B3) |
| @S03 (502 đóng trước header) | `downstream-errors.feature` @S03 | Đỏ: thân không phải `problem+json` (status 502 mặc định đã đúng) | Xanh 2026-10-25 (B1-B3) |
| @S04 (504 vượt thời gian cả request) | `downstream-errors.feature` @S04 | Đỏ: route `Timeout` thiếu `UseRequestTimeouts` làm request 500 (đúng như spike) | Xanh 2026-10-25 (B1-B3) |
| @S05 (504 upstream im lặng) | `downstream-errors.feature` @S05 | Đỏ: cùng lý do 500 | Xanh 2026-10-25 (B1-B3) |
| @S06 (header rồi đóng) | `downstream-errors.feature` @S06 | Đỏ ở log Warning của gateway (client bị ngắt đã xanh sẵn vì YARP tự ngắt; cảnh báo của chính YARP không được tính) | Xanh 2026-10-25 (B1-B3) |
| @S07 (header rồi treo, 2 dòng) | `downstream-errors.feature` @S07 Outline | Đỏ: 500 do route `Timeout` thiếu middleware nên client không bị ngắt | Xanh 2026-10-25 (B1-B3) |
| @S09 (health khi upstream chết) | `downstream-errors.feature` @S09 Outline | Xanh sẵn: hành vi đúng từ lát 1; giữ làm test hồi quy | Xanh sẵn |
| @S10 (`@docker`, compose, cổng đóng) | `downstream-errors.feature` @S10 | Đỏ trên mã trước B1 (worktree riêng ở a607d6a, Docker bật): mong 503, nhận 502 | Xanh 2026-10-25 (B1) |
| @S11 (Retry-After từ cấu hình) | `downstream-errors.feature` @S11 | Đỏ: mong 503 kèm `Retry-After` 7, nhận 502 | Xanh 2026-10-25 (B1-B3) |
| @S12 (không lộ thông tin nhạy cảm vào log) | `downstream-errors.feature` @S12 | Đỏ: mong 503, nhận 502 (chưa tới bước kiểm log) | Xanh 2026-10-25 (B1-B3) |
| Timeout mặc định 30 giây (@S08 cũ) | `ConfigurationTests` (2 test) | Đỏ: `appsettings.json` chưa có `Timeout` và `ActivityTimeout` | Xanh 2026-10-25 (B1-B3) |
| Client huỷ request (P-02) | `ClientCancellationTests` | Đỏ: chưa có log Information của gateway | Xanh 2026-10-25 (B1-B3) |
| Bộ phân loại lỗi | `UpstreamErrorClassifierTests` (31 ca, gồm quy tắc `*Client`, `*Destination`, giá trị chưa liệt kê) | Đỏ: khung trả `None`; ca `None` xanh sẵn | Xanh 2026-10-25 (B1-B3) |
| Khoảng `Retry-After` | `UpstreamOptionsTests` (4 ca sai, 3 ca đúng) | 4 ca sai đỏ: chưa có kiểm tra khi khởi động; 3 ca đúng xanh sẵn | Xanh 2026-10-25 (B1-B3) |
| Mã lỗi gateway | `GatewayErrorCodesTests` (có sẵn, thêm status và message của ba mã mới) | Xanh sẵn: dữ liệu hợp đồng đi cùng khung biên dịch | Xanh sẵn |

Lệnh đã chạy (2026-10-25, sau test-review vòng 1): `dotnet build src/gateway.Tests/gateway.Tests.csproj -warnaserror`, `dotnet test src/gateway.Tests/gateway.Tests.csproj --no-build --filter "Category!=docker"` (3 lần liên tiếp: 49 đỏ, 56 xanh, tổng 105), `dotnet format src/teamboard.slnx --verify-no-changes` (sạch). Giới hạn đã biết: log `Proxying to ...` của YARP có thể chứa query string; kiểm log chỉ áp cho logger của gateway (namespace `Gateway.`), phần YARP thuộc NFR-006 ở lát 7.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-25 | 82471e9 | Cần sửa feature | 1 P1 (`s04-undefined-step`), 7 P2, 8 P3 (gồm 2 CONFIRM) đã chốt (Confirm log #8, #9); review độc lập |
| 2 | 2026-10-25 | working tree sau vòng 1 | PASS | 0; CONFIRM `yarp-log-leaks-query` đã chốt (#10); 2 P3 (`plan-stray-char`, `issue-rounds-not-updated`) đã sửa, `upgrade-cancel-rule` chấp nhận (hệ quả của P-03) |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-25 | 364a9b6 | Sẵn sàng sau khi sửa | 0 sau khi chốt 4 CONFIRM (#11-#14) và sửa 6 FIX (3 P2, 3 P3); không P0-P1; review độc lập |
| 2 | 2026-10-25 | working tree sau vòng 1 | PASS | 0; 3 P3 (`cors-position-wording`, `ll006-readme-row-stale`, `tb022-issue-log-no-merge-row`) đã sửa; 4 đột biến bị test bắt |


### Fix log coding-review

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | position-and-clear-vs-cors | CONFIRM #11 | `UpstreamErrorMiddleware.cs`, design 6.4 | `Response.Clear()` xoá header của middleware khác (CORS lát 6) | Bỏ `Clear()`; ghi vị trí `UpstreamError` vào design 6.4 và comment `Program.cs` | `UpstreamErrorHeaderTests` (đỏ trước, xanh sau); đột biến thêm lại `Clear()` làm đỏ |
| 1 | warning-logs-exception-duplicate-and-host | CONFIRM #12 | `UpstreamErrorMiddleware.cs` | Hai stack trace mỗi 503, message chứa host:port | 503/504 chỉ log enum và tên kiểu; 502 và abort giữ ngoại lệ | Step `that gateway warning carries no exception details` ở S01, S04 (đỏ trước, xanh sau) |
| 1 | logmessage-literals | FIX | `UpstreamErrorMiddleware.cs` | Log literal thay vì `LoggerMessage` | `[LoggerMessage]` (class partial) | Build `-warnaserror`, S01-S07 xanh |
| 1 | connection-error-whitelist | CONFIRM #13 | `UpstreamErrorClassifierTests.cs` | Mã SocketError ngoài Refused/HostNotFound không có test | Thêm 4 ca 503 và 4 ca 502 cùng ca không có SocketException | Unit test mới (xanh sẵn, hồi quy) |
| 1 | design-6.4-not-documented | FIX | design 6.4, `Program.cs` | Thứ tự middleware chưa nhắc `UpstreamError` | Thêm vào thứ tự, lý do và lịch sử | Design, comment |
| 1 | TB-022-status-not-synced | FIX | issue TB-022, README, bảng lát | TB-022 đã merge nhưng còn `review` | `done`, `closed`; hàng lát 2 `done`; LL-006 `recurred` | Diff tài liệu |
| 2 | cors-position-wording | FIX | comment và doc test | Ghi "in front of" sai chiều | "set by other middleware" | Diff |
| 2 | ll006-readme-row-stale | FIX | `document/lessons/README.md` | Cột biện pháp còn "chờ kiểm chứng" | Ghi kết quả và biện pháp 4 `todo` | Diff |
| 2 | tb022-issue-log-no-merge-row | FIX | issue TB-022 | Dòng create-pr chưa ghi merge | Ghi đã merge 2026-10-25 | Diff |

### Giới hạn đã biết (coding-review)

- Counter `gateway_upstream_aborted` chỉ được test bằng `MeterListener`; xoá `AddMeter(...)` thì test vẫn xanh nhưng OpenTelemetry không xuất counter (Confirm log #14).
- Log `Proxying to ...?token=...` của YARP (Information) vẫn chứa query; thuộc lát 7 (Confirm log #10).
- Timeout kết nối 15 giây chưa có test tự động (kiểm tay trên Windows: 504 sau 15,2 giây); hành vi trên Linux chỉ có bằng chứng từ @S10 (cổng đóng).
- `UseExceptionHandler` (500) vẫn gọi `Response.Clear()`: khi thêm CORS ở lát 6, response 500 có thể mất header CORS; xử lý ở TB-026.
- Sau header, mọi lỗi không kết thúc bằng `Client` đều được đếm là ngắt (kể cả `Upgrade*Canceled`); xem lại khi có route WebSocket.
- `gitleaks` chưa cài; chỉ grep thô diff.

### Bài học đề xuất (coding-review)

LL-006 `recurred`: bước `done` sau merge của lát trước không có chủ; biện pháp 4 (kiểm ở đầu `plan-init`/`coding-start`) chờ người dùng đồng ý sửa `.agents`.

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- [LL-006](../lessons/LL-006-slice-table-status-not-synced-with-issues.md): `recurred` ở lát 3 (bước `done` sau merge của lát trước bị bỏ sót).
