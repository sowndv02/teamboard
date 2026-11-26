---
key: TB-032
title: Identity lát 4, phát access token JWT khi đăng nhập
status: in-progress
phase: 1
branch: feature/TB-032-identity-access-token
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-032 — Identity lát 4, phát access token JWT khi đăng nhập

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 4 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 2, ADR thuật toán ký. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-005, BR-004): Phát access token JWT khi đăng nhập (ADR RS256 + Secrets Manager tạo trước lát này)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Login thành công trả access JWT RS256 + claim FR-005 (@S01–@S03)
- [ ] TTL access ~10 phút cấu hình được (BR-004, @S02)
- [ ] 401 không trả token (@S05)
- [ ] Ready fail khi thiếu khoá ký active
- [ ] ADR RS256 + loader khoá; gate xanh plan-review

## Kế hoạch
Plan: [`document/plans/TB-032-identity-access-token.md`](../plans/TB-032-identity-access-token.md) (`approved`, B0–B4; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-031 merged PR #26 (`cbc8124`); `plan-init` trên nhánh `feature/TB-032-identity-access-token`.
- 2026-11-26: plan-review v1 PASS (Confirm #1–#4).

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-032:login-response-shape | Login 200 body | id + access_token, token_type, expires_in | 1 | 2026-11-26 |
| 2 | plan-review:TB-032:issuer-url | iss local | Identity:Jwt:Issuer configurable | 1 | 2026-11-26 |
| 3 | plan-review:TB-032:secrets-manager-scope | SM trong lát | Dev file only | 1 | 2026-11-26 |
| 4 | plan-review:TB-032:adr-number | ADR | 0007 JWT signing | 1 | 2026-11-26 |

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
