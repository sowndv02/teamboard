---
title: Identity lát 10, đăng xuất thu hồi phiên (refresh)
issue: TB-038
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-038 — Identity lát 10, logout thu hồi refresh (FR-011 I4, D4)

Lát 10 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp: nhánh `feature/TB-038-identity-logout` từ `origin/main` `895d738` (sau merge PR #33 / TB-037). Đã có: xoay vòng + reuse + family revoke (ADR 0014), `POST /api/v1/auth/refresh`, bảng `refresh_token`; **chưa** `POST /api/v1/auth/logout`.

## 1. Phạm vi của lát

**Làm (FR-011 phần I4 — refresh, design mục scope):**
- **`POST /api/v1/auth/logout`:** body `{ "refresh_token": "..." }` (cùng kiểu snake_case như refresh); policy `auth-public` + rate limit như login/refresh.
- **Thu hồi phiên refresh:** với refresh token **hợp lệ** (tồn tại, chưa hết hạn, chưa `revoked_at`) → `UPDATE refresh_token SET revoked_at = now() WHERE family_id = @f AND revoked_at IS NULL` — thu hồi **cả chuỗi** (mọi token mới từ xoay vòng cùng phiên).
- Sau logout, `POST /refresh` với bất kỳ token nào trong family → **401** chung `InvalidCredentials` (không kích hoạt reuse FR-010 vì không phải replay `used_at`). Logout thành công → **204** không body.
- **BR-006:** access token đã cấp vẫn có hiệu lực đến `exp`; không endpoint thu hồi access trong lát (ghi trong DoD / BDD note).
- BDD `logout-token.feature` (`@TB-038`, `@S01`–`@S03`): @S01 login → logout **204** → refresh **401**; @S02 token không hợp lệ → logout **401**; @S03 logout lại sau revoke → **401**. Không @S04 rotate (Confirm #4).
- Cập nhật **ADR 0014** (mục logout / explicit revoke) — delta, không tách ADR mới trừ khi plan-review yêu cầu.
- Structured log `event=logout` (`user_id`, `family_id`; không token); metric `logout_total` (Confirm #3).

**Không làm (trong lát này):**
- Huỷ **phiên cookie** identity (FR-019) → **I5 / TB-042+**.
- OIDC RP-initiated logout, `end_session_endpoint`.
- Thu hồi refresh theo user toàn cục (đổi mật khẩu FR-022) → **TB-039**.
- Gateway route mới (đã ` /api/v1/auth/**`); FE; introspection FR-021.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-011 refresh không dùng được sau logout | D4, slice Logout | B2 | @S01 | BDD |
| BR-006 access đến `exp` | spec BR-006 | – | (ghi chú, không assert JWT) | review |
| BR-007 không log token | BR-007 | B4 | review | review |
| NFR-008 rate limit | design 6 | B3 | hồi quy policy | `!~Compose` |
| Thu hồi family | D4 | B2 | @S01 refresh sau logout | BDD |
| Từ chối token xấu | spec 8.1 | B1/B2 | @S02 | BDD |
| Logout lặp / đã revoke | – | B1 | @S03 | BDD → 401 |
| Hồi quy refresh/reuse | FR-009/010 | B5 | refresh-token @S01–@S06 | `!~Compose` |

## 3. Các bước

### B0. ADR 0014 delta (logout)
- Ghi đường đi **explicit logout** (khác reuse: không cần `used_at`).
- Kiểm chứng: ADR khớp SQL family revoke.

### B1. test-init (đỏ)
- `logout-token.feature`: @S01 (204 + refresh 401); @S02 invalid → 401; @S03 second logout → 401.
- Steps: gửi logout với stored refresh; assert status; reuse refresh step hiện có.
- Stub endpoint **501** hoặc absent → đỏ đúng lý do.

### B2. `RefreshTokenService` + revoke by session
- VD `TryRevokeSessionAsync(plaintext)` → hash → `SELECT family_id` với `expires_at > now()`, `revoked_at IS NULL` → family revoke.
- Không tăng `refresh_reuse_total` (không phải FR-010 replay).

### B3. `Features/Logout/LogoutEndpoints.cs`
- Map `POST /api/v1/auth/logout` trong `Program.cs`.
- Thành công **204**; token xấu **401** `InvalidCredentials`; body rỗng/validation như refresh.

### B4. Observability
- `IdentityMetrics.RecordLogout()` → `logout_total`; `LogInformation` hoặc `LogWarning` template cố định `event=logout`.

### B5. Gate
- `dotnet build -warnaserror`, format, `identity-service.Tests` `!~Compose`.

## 4. Chiến lược test

| Tầng | Phạm vi |
|---|---|
| BDD | `logout-token.feature` @S01–@S03; hồi quy `refresh-token.feature` |
| Unit | optional: revoke SQL edge (token hết hạn) |

Công cụ: Reqnroll + Testcontainers PostgreSQL (giống TB-036/037).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Logout token xấu lộ trạng thái | 401 chung (Confirm #2) | Revert PR |
| Nhầm family | Chỉ revoke `family_id` của token match hash | Revert |
| Access JWT vẫn dùng được | BR-006 đã chốt | không rollback schema |

Ảnh hưởng consumer: không đổi contract access; thêm endpoint logout (additive).

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, identity `!~Compose`.
- Issue: tiêu chí FR-011 (phần refresh) tick + hồi quy refresh.

## 7. Câu hỏi mở

Không còn sau plan-review v1.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-038:logout-success-status | O-1 | 204 No Content | 1 | 2026-11-26 |
| 2 | plan-review:TB-038:logout-invalid-token | O-2 | 401 chung | 1 | 2026-11-26 |
| 3 | plan-review:TB-038:logout-observability | O-3 | Log + `logout_total` | 1 | 2026-11-26 |
| 4 | plan-review:TB-038:logout-after-rotate-which-token | O-4 | @S01–@S03, login token only | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B0–B5, O-1..O-4 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-037 PR #33 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#4 |