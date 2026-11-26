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
Plan: [`document/plans/TB-041-identity-client-credentials.md`](../plans/TB-041-identity-client-credentials.md) (`approved`, B1–B4; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-040 merged PR #36 → `main` `93cdd46`; `plan-init` nhánh `feature/TB-041-identity-client-credentials`.
- 2026-11-26: plan-review v1 **PASS** — Confirm #1–#7 (`/connect/token`, form body, scope bắt buộc, feature flag, defer Teamboard.Auth, OAuth errors, AuthPublic rate limit).
- 2026-11-26: test-init — `client-credentials.feature` @S01–@S06; stub `/connect/token` 501; **5 failed, 87 passed** `!~Compose`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B1–B5; O-1..O-7 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#7 |
| test-init | done | – | 2026-11-26 | 5 đỏ @S01–S04,@S06; @S05 xanh; +2 unit |
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
| @S01 | `client-credentials.feature` | **đỏ** (501 ≠ 200) | |
| @S02 | `client-credentials.feature` | **đỏ** (501 ≠ 200) | |
| @S03 | `client-credentials.feature` | **đỏ** (501 ≠ invalid_scope) | |
| @S04 | `client-credentials.feature` | **đỏ** (501 ≠ invalid_client) | |
| @S05 | `client-credentials.feature` | | **xanh** |
| @S06 | `client-credentials.feature` | **đỏ** (501 ≠ unauthorized_client) | |
| unit issuer | `AccessTokenIssuerMachineTests` | **đỏ** (NIE) | |
| unit scope | `ScopeValidatorTests` | **đỏ** (NIE) | |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **87 pass, 5 fail** (test-init).

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
