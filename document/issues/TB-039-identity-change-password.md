---
key: TB-039
title: Identity lát 11, đổi mật khẩu và thu hồi refresh
status: planning
phase: 1
branch: feature/TB-039-identity-change-password
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-039 — Identity lát 11, đổi mật khẩu và thu hồi refresh

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 11 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 3 (lockout), 8 (refresh). Thực tế đã có thêm lát 9–10 (reuse, logout) trên `main` `b90590c`. Ước lượng bảng lát: 1–2 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: **FR-022**): `POST /api/v1/auth/password` với Bearer; đổi hash khi `current_password` đúng và `new_password` BR-002; thu hồi **mọi** refresh của user; sai current → 401 chung + FR-003 lockout; BR-006 access đến `exp`.
- Không làm: quên mật khẩu, FR-019 cookie, introspection, gateway, FE.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] @S01: hai login → đổi mật khẩu **204** → cả hai refresh **401**.
- [ ] @S02/@S06: sai `current_password` → **401** chung; @S06 lockout + `Retry-After` (FR-003).
- [ ] @S03: `new_password` BR-002 vi phạm → **400**; new trùng current → **400**.
- [ ] @S04: thiếu/sai Bearer → **401**.
- [ ] @S05: access JWT vẫn dùng được đến `exp` sau đổi mật khẩu (BR-006).
- [ ] `password_change_total` + log `event=password_change` (không password).
- [ ] Gate identity `!~Compose` xanh; hồi quy logout/login.

## Kế hoạch
Plan: [`document/plans/TB-039-identity-change-password.md`](../plans/TB-039-identity-change-password.md) (`approved`, B1–B6; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-038 merged PR #34; `plan-init` trên nhánh `feature/TB-039-identity-change-password` từ `main` `b90590c`.
- 2026-11-26: plan-review v1 **PASS** — Confirm #1–#6 (Teamboard.Auth, 204, metric, auth-public, @S06, reject same password).

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B1–B6; O-1..O-6 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#6 |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-039:jwt-on-identity | O-1 JWT | Teamboard.Auth + StaticJwksFile | 1 | 2026-11-26 |
| 2 | plan-review:TB-039:change-password-success-status | O-2 | 204 No Content | 1 | 2026-11-26 |
| 3 | plan-review:TB-039:password-change-observability | O-3 | log + `password_change_total` | 1 | 2026-11-26 |
| 4 | plan-review:TB-039:password-rate-limit | O-4 | `auth-public` | 1 | 2026-11-26 |
| 5 | plan-review:TB-039:lockout-bdd-scope | O-5 | @S06 lockout | 1 | 2026-11-26 |
| 6 | plan-review:TB-039:same-password-allowed | O-6 | 400 if new==current | 1 | 2026-11-26 |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

- 