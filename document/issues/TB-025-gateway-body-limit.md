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
- [ ] FR-007: giới hạn cấu hình được (`Gateway:BodyLimit:MaxBytes`, mặc định 1.048.576), giá trị ngoài khoảng làm khởi động thất bại (@S03)
- [ ] FR-007: body chunked đúng giới hạn qua, vượt giới hạn bị ngắt luồng và không nhận `500` (@S04, @S05)
- [ ] NFR-006: log khi từ chối không chứa nội dung body (@S06)
- [ ] Thứ tự design 6.4: IP đã hết hạn mức thì `429` đến trước `413` (@S07)
- [ ] Gate xanh (`code-change-gate.md`); bảng lát và `document/issues/README.md` đồng bộ (LL-006)

## Kế hoạch
Plan: [`document/plans/TB-025-gateway-body-limit.md`](../plans/TB-025-gateway-body-limit.md) (`draft`), 3 bước B1-B3, 7 kịch bản @S01-@S07, 4 câu hỏi mở còn lại P-02..P-05 (P-01 đã đóng vì TB-024 merge), 4 giả định cần spike ở test-init.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-10-25: plan-init lần đầu ở worktree `board-tb-025` (branch tạo từ `main` feb4278, khi TB-024 chưa merge).
- 2026-11-25: TB-024 đã merge (PR 18); nhánh fast-forward lên `main` `66673c2`; plan-init cập nhật tại chỗ (`BodyLimit` sau `RateLimiter`, thêm @S07, đóng P-01); đồng bộ lát 4 và TB-024 sang `done` (LL-006).

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-025-gateway-body-limit.md` (draft, cập nhật sau TB-024), làn vừa, 3 bước, 7 kịch bản, 4 câu hỏi mở |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

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
