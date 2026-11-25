---
key: TB-025
title: Gateway lát 5, giới hạn body 1 MiB
status: todo
phase: 1
branch: feature/TB-025-gateway-body-limit
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: null           # ngày chuyển in-progress
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
- [ ] FR-007: `POST` có `Content-Length` đúng 1.048.576 byte được chuyển tiếp nguyên vẹn (@S01)
- [ ] FR-007: `Content-Length` 1.048.577 byte nhận `413 problem+json` `PAYLOAD_TOO_LARGE`, upstream không nhận gì, có `X-Correlation-Id` (@S02)
- [ ] FR-007: giới hạn cấu hình được (`Gateway:BodyLimit:MaxBytes`, mặc định 1.048.576), khoảng hợp lệ 1 đến 10.485.760 byte, ngoài khoảng làm khởi động thất bại (@S03)
- [ ] FR-007: body chunked đúng giới hạn qua, vượt giới hạn nhận `413 problem+json` `PAYLOAD_TOO_LARGE` kèm `X-Correlation-Id`, không `500` (@S04, @S05)
- [ ] NFR-006: log khi từ chối không chứa nội dung body (@S06)
- [ ] Thứ tự design 6.4: IP đã hết hạn mức thì `429` đến trước `413` (@S07)
- [ ] Gate xanh (`code-change-gate.md`); bảng lát và `document/issues/README.md` đồng bộ (LL-006)

## Kế hoạch
Plan: [`document/plans/TB-025-gateway-body-limit.md`](../plans/TB-025-gateway-body-limit.md) (`approved`), 3 bước B1-B3, 7 kịch bản @S01-@S07; không còn câu hỏi mở (P-01..P-05 đã đóng); spike của plan-review đã kiểm giả định Kestrel/YARP, kiểm lại ở test-init.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-10-25: plan-init lần đầu ở worktree `board-tb-025` (branch tạo từ `main` feb4278, khi TB-024 chưa merge).
- 2026-11-25: TB-024 đã merge (PR 18); nhánh fast-forward lên `main` `66673c2`; plan-init cập nhật tại chỗ (`BodyLimit` sau `RateLimiter`, thêm @S07, đóng P-01); đồng bộ lát 4 và TB-024 sang `done` (LL-006).

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-025-gateway-body-limit.md` (draft, cập nhật sau TB-024), làn vừa, 3 bước, 7 kịch bản, 4 câu hỏi mở |
| plan-review | PASS | 2 | 2026-11-25 | plan `approved`; 4 quyết định (chunked `problem+json`, mọi request, khoảng 1-10 MiB, giữ một lát); 3 FIX theo spike (TestServer không có feature giới hạn, Kestrel trả `413` rỗng, không `500`); làn vừa |
| test-init | done | – | 2026-11-25 | 7 kịch bản (`body-limit.feature`) + 3 nhóm xUnit; đỏ 12 test đúng lý do (413 mong đợi nhưng nhận 201, thiếu mã `PAYLOAD_TOO_LARGE`, thiếu cấu hình và validation `MaxBytes`, classifier chưa có nhánh body quá lớn); 4 kịch bản xanh sẵn (hồi quy); gateway 188 test: 176 xanh, 12 đỏ |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-025:chunked-413-shape | Chunked vượt giới hạn: `413 problem+json` hay `413` rỗng | `413 problem+json` | 1 | 2026-11-25 |
| 2 | plan-review:TB-025:scope-all-requests | `413` áp mọi request hay chỉ route proxy | Mọi request | 1 | 2026-11-25 |
| 3 | plan-review:TB-025:maxbytes-range | Khoảng hợp lệ của `MaxBytes` | 1 đến 10.485.760 byte | 1 | 2026-11-25 |
| 4 | plan-review:TB-025:slice-size | Giữ một lát hay chia | Giữ một lát | 1 | 2026-11-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 body đúng 1.048.576 byte được chuyển tiếp | `body-limit.feature` @S01 (`BodyLimitSteps`) | không: xanh sẵn (chưa có giới hạn nào), test hồi quy | chờ coding |
| @S02 vượt 1 byte: `413 problem+json`, upstream không nhận | @S02 | có: mong đợi 413, nhận 201 | chờ coding |
| @S03 giới hạn từ cấu hình (1024 qua, 1025 chặn) | @S03 (Outline, 2 dòng) | dòng 1025: mong đợi 413, nhận 201; dòng 1024 xanh sẵn (hồi quy) | chờ coding |
| @S04 chunked đúng giới hạn qua | @S04 (Kestrel thật) | không: xanh sẵn (Kestrel mặc định 30 MB), test hồi quy | chờ coding |
| @S05 chunked vượt giới hạn: `413 problem+json` | @S05 (Kestrel thật) | có: mong đợi 413, nhận 201 | chờ coding |
| @S06 nội dung body bị từ chối không vào log | @S06 | có: mong đợi 413, nhận 201 (phần log xanh sẵn, chỉ có nghĩa khi 413 đã có) | chờ coding |
| @S07 hết hạn mức thì `429` trước `413` | @S07 | không: xanh sẵn vì chưa có `BodyLimit`; khoá thứ tự, sẽ đỏ nếu `BodyLimit` đặt trước `RateLimiter` | chờ coding |
| Mã `PAYLOAD_TOO_LARGE` 413 | `GatewayErrorCodesTests.All_ContainsPayloadTooLarge_As413WithoutInternals` | có: thiếu mã | chờ coding |
| `MaxBytes` mặc định 1 MiB | `BodyLimitConfigurationTests.ShippedLimit_IsOneMebibyte` | có: thiếu mục `Gateway:BodyLimit` | chờ coding |
| `MaxBytes` ngoài khoảng 1 đến 10.485.760 làm khởi động thất bại | `BodyLimitConfigurationTests.Start_WithInvalidLimit_FailsAndNamesTheSetting` (4 giá trị) | có: khởi động vẫn thành công | chờ coding |
| `MaxBytes` hợp lệ khởi động được | `BodyLimitConfigurationTests.Start_WithValidLimit_Succeeds` (3 giá trị) | không: xanh sẵn (hồi quy, để validation không chặn nhầm) | chờ coding |
| `RequestBodyClient` do body quá lớn → `PayloadTooLarge` (trực tiếp, bọc `AggregateException`) | `BodyLimitClassifierTests` (2 test) | có: nhận `ClientCancelled` | chờ coding |
| `RequestBodyClient` khác, không ngoại lệ, response đã bắt đầu → vẫn `ClientCancelled` | `BodyLimitClassifierTests` (3 test) | không: xanh sẵn (hồi quy, giữ hành vi cũ) | chờ coding |

Lệnh đã chạy: `dotnet build src/gateway.Tests -warnaserror` (0 cảnh báo, 0 lỗi); `dotnet test src/gateway.Tests --no-build --filter "Category!=docker"` → 188 test, 176 xanh, 12 đỏ (đúng 12 test đỏ ở bảng trên; 166 test cũ đều xanh); `dotnet test ... --filter "Category=TB-025"` chạy 3 lần: 4 đỏ, 4 xanh mỗi lần (không flaky); `dotnet format src/teamboard.slnx --verify-no-changes` sạch.

Khung tối thiểu cho biên dịch: một giá trị enum rỗng `UpstreamFailure.PayloadTooLarge` ở `src/gateway/UpstreamErrorClassifier.cs` (chưa có logic). Hỗ trợ test: `UpstreamStub` đếm byte body (`ReceivedBodyBytes`), `GatewayContext.UseRealKestrel` (host Kestrel thật cho chunked).

Ghi nhận cho test-review: (1) quyết định Confirm #2 "`413` áp cho mọi request" (kể cả route không tồn tại) chưa có kịch bản riêng vì plan chỉ liệt kê @S01-@S07; (2) rủi ro R-06 của plan (hai host Kestrel song song gây `AddressInUse`) không tái hiện được ở test-init: @S04 và @S05 chạy tuần tự (assembly đã tắt chạy song song) xanh 3 lần liên tiếp.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
