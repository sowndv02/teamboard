---
key: TB-042
title: Identity lát 14a, Authorize + PKCE validation (I5 pha 1)
status: in-progress
phase: 1
branch: feature/TB-042-identity-authorize-pkce
estimate: L             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-042 — Identity lát 14a, Authorize + PKCE validation

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 14 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 2, 12, ADR trang đăng nhập. Ước lượng của bảng lát: 4–5 ngày.

## Phạm vi
- Làm (14a): design phụ lục I5, migration `authorization_code`, PKCE S256, `GET /connect/authorize` + validation, feature flag, BDD lỗi (@S02, @S03).
- Không làm (14a): Razor login, cookie IdP, redirect `code` → **lát 14b** (issue TBD). Token exchange → TB-043.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Design `identity-auth-i5.md` approved.
- [x] `redirect_uri` / params sai → không redirect URI chưa tin cậy (@S02, @S03, @S03b, @S03c).
- [x] PKCE + registry validation unit/BDD xanh.
- [x] Hồi quy `!~Compose` xanh (104/104).

## Kế hoạch
Plan: [`document/plans/TB-042-identity-authorize-pkce.md`](../plans/TB-042-identity-authorize-pkce.md) (`approved`, 14a B0–B4; plan-review v1 PASS). Design: [`document/design/identity-auth-i5.md`](../design/identity-auth-i5.md).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: `plan-init` từ `main` `138b81a`; chia 14a/14b; `identity-auth-i5.md` approved.
- 2026-11-26: plan-review v1 **PASS** — Confirm O-1..O-11 (design i5, split, Razor 14b, flag, HTML errors, DB context, S256, scope, nonce, redirect BDD).
- 2026-11-26: test-init — `authorize-pkce.feature` @S02,@S03,@S03b,@S07; stub GET `/connect/authorize` 501; **8 fail, 94 pass** `!~Compose`.
- 2026-11-26: test-review v1 **PASS** — FIX @S03c invalid scope; **9 fail, 94 pass** `!~Compose`.
- 2026-11-26: coding-start — `GET /connect/authorize` validate PKCE/redirect/scope; **104/104** `!~Compose`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | 14a scope, O-1..O-11 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#10 |
| test-init | done | – | 2026-11-26 | 8 fail 94 pass !~Compose |
| test-review | PASS | 1 | 2026-11-26 | FIX @S03c scope; 9 fail |
| coding-start | done | – | 2026-11-26 | **104/104** `!~Compose` |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S02 | `authorize-pkce.feature` | | **xanh** |
| @S03 | `authorize-pkce.feature` | | **xanh** |
| @S03b | `authorize-pkce.feature` | | **xanh** |
| @S03c | `authorize-pkce.feature` | | **xanh** |
| @S07 | `authorize-pkce.feature` | | **xanh** |
| unit PKCE | `PkceValidatorTests` ×3 | | **xanh** |
| unit authorize | `AuthorizeRequestValidatorTests` ×2 | | **xanh** |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **104/104** (coding-start).

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `2a9f906` + FIX @S03c | Sẵn sàng code | – |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:authorize-missing-scope | FIX P1 | `authorize-pkce.feature` | Confirm O-7 scope subset thiếu kịch bản | @S03c | đỏ |

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
