---
key: TB-041
title: Identity lát 13, Client Credentials và token máy
status: in-progress
phase: 1
branch: feature/TB-041-identity-client-credentials
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-041 — Identity lát 13, Client Credentials và token máy

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 13 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 4, 12. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (FR-015, BR-008, BR-007): grant `client_credentials` trên token endpoint; confidential client + secret qua registry; JWT máy (`token_use=machine`, `sub=client_id`, scope ⊆ allowed); metric; BDD @S01–@S06.
- Không làm: code exchange (TB-043), authorize/PKCE (TB-042), introspection (TB-046), scope trên board API (TB-045) trừ khi plan Confirm #5 mở rộng `Teamboard.Auth` unit test only.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Confidential client đúng secret → access token máy hợp lệ (@S01, @S02).
- [ ] Scope vượt `allowed_scopes` bị từ chối (@S03).
- [ ] Sai secret/client không lộ chi tiết (@S04).
- [ ] Không log client secret (@S05).
- [ ] Public client không dùng được grant (@S06).
- [ ] Hồi quy `!~Compose` xanh.

## Kế hoạch
Plan: [`document/plans/TB-041-identity-client-credentials.md`](../plans/TB-041-identity-client-credentials.md) (`draft`, B1–B5; chờ plan-review O-1..O-7).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-040 merged PR #36 → `main` `93cdd46`; `plan-init` nhánh `feature/TB-041-identity-client-credentials`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B1–B5; O-1..O-7 |
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
