---
key: TB-029
title: Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2
status: todo
phase: 1
branch: feature/TB-029-identity-register
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: null           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-029 — Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 1b của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1a. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-001, FR-004, BR-001, BR-002, NFR-002, NFR-007 (metric băm)): Đăng ký bằng email/mật khẩu băm PBKDF2, benchmark số vòng và vCPU
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Chưa lập: viết khi `plan-init` cho lát này, từ FR/NFR của hàng lát.

## Kế hoạch
Chưa soạn (bước plan-init, khi các lát phụ thuộc đã xong).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | pending | – | | |
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
