---
key: TB-030
title: Identity lát 2, đăng nhập với lỗi chung không lộ email
status: in-progress
phase: 1
branch: feature/TB-030-identity-login
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-030 — Identity lát 2, đăng nhập với lỗi chung không lộ email

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 2 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1b. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-002, BR-003): Đăng nhập, lỗi chung không lộ email
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `POST` đăng nhập (đường dẫn chốt ở plan-review): credential đúng → thành công theo P-02, không trả mật khẩu/hash/token JWT (FR-002)
- [ ] Email không tồn tại và mật khẩu sai → cùng `401 problem+json`, cùng `code` (BR-003, FR-002)
- [ ] Email không hợp lệ / mật khẩu không đạt BR-002 → 400 `VALIDATION_FAILED`
- [ ] Email chuẩn hoá khớp user đã đăng ký (BR-001)
- [ ] Không lộ mật khẩu trong log (BR-007, FR-004)
- [ ] Metric `login_failures_total` khi 401 credential (design mục 9)
- [ ] Gate xanh đầy đủ; TB-029/lát 1b đồng bộ `done`

## Kế hoạch
Plan: [`document/plans/TB-030-identity-login.md`](../plans/TB-030-identity-login.md) (`draft`, B1–B3, @S01–@S08; P-01..P-04 mở).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-25: TB-029 merged PR #24; `plan-init` trên nhánh `feature/TB-030-identity-login` từ `main` `068212c`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-030-identity-login.md` draft B1–B3, P-01..P-04 |
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
