---
title: Identity lát 11, đổi mật khẩu và thu hồi refresh
issue: TB-039
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-039 — Identity lát 11, change password (FR-022 I1, D8)

Lát 11 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp: nhánh `feature/TB-039-identity-change-password` từ `origin/main` `b90590c` (sau merge PR #34 / TB-038). Đã có: đăng ký/đăng nhập + lockout (TB-031), access JWT (TB-032), JWKS + xoay khoá, refresh xoay vòng + reuse + logout; **chưa** `POST /api/v1/auth/password` và **chưa** `UseAuthentication` trên identity-service.

## 1. Phạm vi của lát

**Làm (FR-022, BR-002, BR-003, BR-006, FR-003 khi sai mật khẩu hiện tại, FR-004 qua băm, design slice `ChangePassword`):**
- **`POST /api/v1/auth/password`:** yêu cầu **Bearer** access JWT (`sub` = chủ tài khoản; design: không nhận `userId` từ client).
- Body JSON snake_case: `current_password`, `new_password` (cùng quy ước login/register).
- **Thành công → 204** No Content (Confirm #2).
- Mật khẩu hiện tại đúng, mật khẩu mới đạt BR-002 và **khác** mật khẩu hiện tại (Confirm #6 → 400 nếu trùng) → cập nhật hash/salt/iterations → **thu hồi mọi refresh** của `user_id`: `UPDATE refresh_token SET revoked_at = now() WHERE user_id = @u AND revoked_at IS NULL` (D4).
- **Sai mật khẩu hiện tại:** 401 chung `InvalidCredentials` (BR-003); đếm lockout `(email_norm, IP)` như login (FR-003) — `email_norm` từ user của `sub`.
- **BR-006:** access JWT dùng khi gọi endpoint vẫn hợp lệ đến `exp` sau đổi mật khẩu.
- **Rate limit:** policy `auth-public` theo IP như login (Confirm #4).
- BDD `change-password.feature` (`@TB-039`):
  - **@S01** — hai login (lưu refresh A và B) → đổi mật khẩu Bearer → **204** → refresh A và B đều **401**.
  - **@S02** — sai `current_password` → **401** chung.
  - **@S03** — `new_password` vi phạm BR-002 → **400** validation.
  - **@S04** — thiếu/sai Bearer → **401** (JwtBearer hoặc handler).
  - **@S05** — sau đổi mật khẩu thành công, gọi lại `/password` với **cùng** access token → **204** hoặc thao tác được đến `exp` (assert request không 401 do JWT).
  - **@S06** — 5 lần sai `current_password` → **401** + `Retry-After` (lockout FR-003 trên change-password).
- Steps BDD: header `Authorization: Bearer <stored access_token>`; lưu hai refresh từ hai login.
- **`AddTeamboardJwtAuthentication`** (Confirm #1): `identity-service.csproj` reference `Teamboard.Shared.Auth`; `Program.cs` `UseAuthentication`/`UseAuthorization`; BDD set `Auth:Jwt:StaticJwksFile` = cùng `test-signing-keys.json`, `ValidIssuer`/`ValidAudience` khớp `Identity:Jwt` (iss `http://localhost:6061`, aud `teamboard`).
- Log `event=password_change` (`user_id` only) + metric **`password_change_total`** (Confirm #3).

**Không làm (trong lát này):**
- Thu hồi access JWT / introspection FR-021; cookie FR-019; quên mật khẩu; gateway; FE.
- JwtBearer cho mọi route identity (chỉ `/password` `.RequireAuthorization()`).

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-022 đổi mật khẩu + revoke all refresh | ChangePassword, D4 | B3–B4 | @S01 | BDD |
| BR-002 mật khẩu mới | Passwords D8 | B4 | @S03 | BDD |
| BR-003 401 chung sai current | Login | B4 | @S02, @S06 | BDD |
| FR-003 lockout | D5 | B4 | @S06 | BDD |
| FR-004 băm | PasswordHasher | B4 | review | review |
| BR-006 access đến exp | spec | B4 | @S05 | BDD |
| Bearer bắt buộc | design | B2 | @S04 | BDD |
| NFR-008 rate limit | design 6 | B4 | auth-public | hồi quy |
| BR-007 không log password | BR-007 | B5 | review | review |
| Không IDOR | design AuthZ | B2–B4 | `sub` only | review |

## 3. Các bước

### B1. test-init (đỏ)
- `change-password.feature` @S01–@S06.
- Steps: change password với Bearer; lưu refresh thứ hai sau login lần 2; assert 204/401/400/Retry-After.
- Stub absent/501 → đỏ.

### B2. Teamboard.Auth trên identity
- Reference `Teamboard.Shared.Auth`; `AddTeamboardJwtAuthentication(configuration)` trong `IdentitySecurityExtensions` hoặc `Program.cs`.
- Middleware: sau exception handler, trước rate limiter/endpoints.
- Test factory: `Auth:Jwt:StaticJwksFile` → `test-signing-keys.json`; issuer/audience khớp issuer identity.
- Chỉ endpoint change-password `.RequireAuthorization()`.

### B3. `RefreshTokenService.RevokeAllForUserAsync`
- SQL revoke by `user_id`; không `refresh_reuse_total`.
- Gọi trong transaction sau cập nhật password thành công.

### B4. `Features/ChangePassword/ChangePasswordEndpoints.cs`
- `POST /api/v1/auth/password`; `RequireAuthorization()` + `auth-public` rate limit.
- Flow: lock check → verify current (dummy timing nếu user không tồn tại — không áp dụng khi đã auth) → reject new==current 400 → validate length → hash → save → `RecordSuccessAsync` → revoke all refresh → **204**.
- Sai current: `RecordFailureAsync` + 401 chung.
- `IdentityMetrics.RecordPasswordChange()` → `password_change_total`.

### B5. Observability
- `LogInformation` template cố định: `event=password_change UserId={UserId}`.

### B6. Gate
- `dotnet build -warnaserror`, format, `identity-service.Tests` `FullyQualifiedName!~Compose`.
- Hồi quy: `logout-token.feature`, `login.feature` @S01–@S03.

## 4. Chiến lược test

| Tầng | Phạm vi |
|---|---|
| BDD | `change-password.feature` @S01–@S06 |
| Integration | 2 families revoke; lockout @S06 |

Reqnroll + Testcontainers PostgreSQL.

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Public routes unaffected | Jwt optional until `.RequireAuthorization()` | Revert PR |
| Race refresh vs change password | Revoke sau hash trong transaction | Document BR-006 |
| JWKS file drift | Cùng file signing keys BDD | StaticSigningKeyJwksSource |

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, identity `!~Compose`.
- Issue FR-022 tick sau coding-review.

## 7. Câu hỏi mở và giả định

Không còn sau plan-review v1 (xem Confirm log).

**Giả định đã xác nhận:** `sub` = `Guid` string (`AccessTokenIssuer`).

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-039:jwt-on-identity | O-1 | Teamboard.Auth + StaticJwksFile | 1 | 2026-11-26 |
| 2 | plan-review:TB-039:change-password-success-status | O-2 | 204 No Content | 1 | 2026-11-26 |
| 3 | plan-review:TB-039:password-change-observability | O-3 | `event=password_change` + `password_change_total` | 1 | 2026-11-26 |
| 4 | plan-review:TB-039:password-rate-limit | O-4 | `auth-public` theo IP | 1 | 2026-11-26 |
| 5 | plan-review:TB-039:lockout-bdd-scope | O-5 | @S06 lockout trên change-password | 1 | 2026-11-26 |
| 6 | plan-review:TB-039:same-password-allowed | O-6 | 400 nếu new trùng current | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | A–H, O-1..O-6 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-038 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#6 |