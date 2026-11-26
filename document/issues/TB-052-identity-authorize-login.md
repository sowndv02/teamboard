---
key: TB-052
title: Identity lát 14b, Razor login + cookie IdP + phát code
status: review
phase: 1
branch: feature/TB-052-identity-authorize-login
estimate: L             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-11-26
started: 2026-11-26
closed: null
---

# TB-052 — Identity lát 14b, Razor login + cookie IdP + phát `code`

## Bối cảnh

Lát **14b** trong [`identity-auth-slices.md`](../plans/identity-auth-slices.md), tiếp [`TB-042`](TB-042-identity-authorize-pkce.md) (14a merged `main` `8652c52`). Design: [`identity-auth-i5.md`](../design/identity-auth-i5.md). Spec: FR-019, FR-012 (happy path), FR-003 trên trang login, BR-005 phát mã, NFR-008/NFR-010.

**Phụ thuộc:** TB-042 (14a), lát 2 (login), 3 (lockout), 12 (registry). **TB-043** phụ thuộc **14 hoàn chỉnh** (14a + lát này).

## Phạm vi

- Làm (14b): `authorize_request` + luồng authorize hợp lệ → login/cookie → redirect `code` + `state`; cookie scheme IdP; Razor login tối thiểu; BDD @S01, @S04–@S06, @S08 (theo plan).
- Không làm: `POST /connect/token` authorization_code (**TB-043**); consent UI; logout huỷ cookie IdP (follow-up TB-038); OIDC; SPA client.

## Tiêu chí hoàn thành (kiểm chứng được)

- [x] `authorize_request` migration + TTL 15 phút.
- [x] Cookie IdP 30 phút sliding; Data Protection keys directory.
- [x] Valid authorize → `/login` → code redirect (@S01, @S01b confidential).
- [x] Sai mật khẩu / lockout: không redirect client (@S04, @S05).
- [x] Cookie IdP còn hiệu lực → skip form (@S06).
- [x] CSP `frame-ancestors 'none'` trên login (@S08).
- [x] Migration `scope` trên `authorization_code`; metric `authorization_codes_issued_total`.
- [x] Hồi quy `!~Compose` xanh; hồi quy @S02–@S07.

## Kế hoạch

Plan: [`document/plans/TB-052-identity-authorize-login.md`](../plans/TB-052-identity-authorize-login.md) (`approved`, plan-review v1 PASS).

## Ghi chú / Nhật ký

- 2026-11-26: `plan-init` từ `main` `8652c52` sau merge TB-042 / PR #38.
- 2026-11-26: plan-review v1 **PASS** — Confirm O-1..O-10.
- 2026-11-26: test-init — `authorize-pkce.feature` @S01,@S01b,@S04–@S06,@S08 + `AuthorizationCodeGeneratorTests`; **7 fail, 104 pass** `!~Compose`.
- 2026-11-26: test-review v1 **PASS** — FIX @S05 lockout 401; @S06 second When → And; **7 fail, 104 pass** `!~Compose`.
- 2026-11-26: coding-start **done** — Razor `/login`, cookie `Identity.Idp`, authorize flow, migration #7; **111/111** `!~Compose`.
- 2026-11-26: coding-review v1 **PASS** — commit `81b8cfa` + FIX `tb.authz.pending` Secure/Path; **111/111** `!~Compose`.
- 2026-11-26: [PR #39](https://github.com/sowndv02/teamboard/pull/39) (head `0b7c6ea`; không `Co-authored-by`).

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B9; O-1..O-10 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#10 |
| test-init | done | – | 2026-11-26 | 7 đỏ 104 pass !~Compose |
| test-review | PASS | 1 | 2026-11-26 | FIX @S05 @S06 |
| coding-start | done | – | 2026-11-26 | 111/111 !~Compose |
| coding-review | PASS | 1 | 2026-11-26 | FIX pending cookie Secure |
| create-pr | done | – | 2026-11-26 | [PR #39](https://github.com/sowndv02/teamboard/pull/39) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1–10 | plan-review:TB-052:O-1..O-10 | Xem plan Confirm log | PASS v1 | 1 | 2026-11-26 |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | `authorize-pkce.feature` | | **xanh** |
| @S01b | `authorize-pkce.feature` | | **xanh** |
| @S04 | `authorize-pkce.feature` | | **xanh** |
| @S05 | `authorize-pkce.feature` | | **xanh** |
| @S06 | `authorize-pkce.feature` | | **xanh** |
| @S08 | `authorize-pkce.feature` | | **xanh** |
| @S02–@S07 | `authorize-pkce.feature` | | **xanh** (TB-042) |
| unit code | `AuthorizationCodeGeneratorTests` | | **xanh** |
| database @S04 | `database.feature` | | **xanh** (7 migrations) |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **111 pass**.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `d20dc00` + FIX | Sẵn sàng code | – |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:authz-s05-lockout-status | FIX P2 | `authorize-pkce.feature` @S05 | Lockout trên login phải khớp `lockout.feature` (401 + Retry-After) | `Then 401` | đỏ đúng lý do |
| 1 | test:identity:authz-s06-gherkin-when | FIX P2 | `authorize-pkce.feature` @S06 | Hai `When` trong một scenario | GET lặp → `And` | rule Gherkin |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `81b8cfa` (+ fix) | PASS | – |

## Fix log

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | security:tb-052:pending-cookie-secure | FIX P2 | `AuthorizePendingRequestCookie.cs` | `Secure=false` cố định, khác IdP cookie | `Secure = IsHttps`, `Path=/` | 111/111 |

## Bài học (lessons learned)

-