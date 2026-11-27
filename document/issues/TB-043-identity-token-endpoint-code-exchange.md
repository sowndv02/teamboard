---
key: TB-043
title: Identity lát 15, endpoint token đổi mã (code_verifier)
status: in-progress
phase: 1
branch: feature/TB-043-identity-token-endpoint-code-exchange
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null
---

# TB-043 — Identity lát 15, endpoint token đổi mã (code_verifier)

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 15 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc:** lát 14 (TB-042 + TB-052 merged PR #39). Ước lượng: 2–3 ngày.

## Phạm vi

- Làm (15): `POST /connect/token` `grant_type=authorization_code`, verify PKCE, đánh dấu mã dùng, phát access token user (FR-013).
- Không làm: `id_token` (TB-044), scope policy JWT (TB-045 defer nếu O-4), introspection (TB-046).

## Tiêu chí hoàn thành (kiểm chứng được)

- [x] `authorization_code` đổi được với `code_verifier` đúng (BDD @S01).
- [x] Sai verifier / reuse / hết hạn / sai client hoặc `redirect_uri` → từ chối (@S02–@S06).
- [x] Không regress `client_credentials` + authorize BDD.
- [x] Metric exchange (sau plan-review); không log code/verifier.

## Kế hoạch

Plan: [`document/plans/TB-043-identity-token-endpoint-code-exchange.md`](../plans/TB-043-identity-token-endpoint-code-exchange.md) (`approved`, plan-review v1 PASS).

## Ghi chú / Nhật ký

- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: plan-init từ `main` `062c557` (sau merge PR #39); nhánh `feature/TB-043-identity-token-endpoint-code-exchange`.
- 2026-11-26: plan-review v1 **PASS** — Confirm O-1..O-5 (access only, shared flag, confidential secret, no scope claim, OAuth errors).
- 2026-11-26: test-init — `authorization-code-token.feature` @S01–@S08 + @S01b; `PkceValidatorTests.VerifyS256`; **10 fail (9 BDD + 1 unit), 112 pass** `!~Compose` (BDD đỏ: `unsupported_grant_type` / status 200; unit: `NotImplementedException`).
- 2026-11-26: test-review v1 **PASS** — FIX @S07b missing secret, @S08 assert 200 (BR-007 end-to-end), tiêu đề S05/S06; **11 fail, 111 pass** `!~Compose`.
- 2026-11-27: coding-start — redeem service, token endpoint, metric; BDD xanh sau FIX helper DB.
- 2026-11-27: coding-review v1 **PASS** — FIX `GetUserIdByEmailAsync` (`"Id"`, `email_norm`); **122/122** `!~Compose`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B1–B6; O-1..O-5 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm O-1..O-5 |
| test-init | done | – | 2026-11-26 | 10 đỏ 112 pass !~Compose |
| test-review | PASS | 1 | 2026-11-26 | FIX @S07b @S08 |
| coding-start | done | – | 2026-11-27 | 122 pass sau FIX DB |
| coding-review | PASS | 1 | 2026-11-27 | FIX PostgresDatabase user lookup |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1–5 | plan-review:TB-043:O-1..O-5 | Xem plan Confirm log | PASS v1 (gợi ý plan) | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01–@S08, @S01b, @S07b | `authorization-code-token.feature` | | **xanh** |
| unit PKCE | `PkceValidatorTests.VerifyS256` | | **xanh** |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **122/122 pass** (coding-review v1).

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | (local) | PASS | 0 |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-27 | (local) | PASS | 0 |

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | test-review:TB-043:S07-missing-secret | FIX | authorization-code-token.feature | Plan @S07 thiếu secret | Thêm @S07b | 11 fail |
| 1 | test-review:TB-043:S08-vacuous-green | FIX | authorization-code-token.feature | @S08 pass trước code | Then 200 + Bearer | 11 fail |
| 1 | test-review:TB-043:scenario-titles | FIX | authorization-code-token.feature | P3 tiêu đề | Title case S05/S06 | — |
| 1 | coding-review:TB-043:bdd-user-id-sql | FIX | PostgresDatabase.cs | Sai cột `id`/`email` | `"Id"`, `email_norm` + normalize | 122 pass |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
