---
title: Identity lát 10, đăng xuất thu hồi phiên (refresh)
issue: TB-038
status: draft
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
- Sau logout, `POST /refresh` với bất kỳ token nào trong family → **401** chung `InvalidCredentials` (không kích hoạt reuse FR-010 vì không phải replay `used_at`).
- **BR-006:** access token đã cấp vẫn có hiệu lực đến `exp`; không endpoint thu hồi access trong lát (ghi trong DoD / BDD note).
- BDD mới `document/spec/features/identity-service/logout-token.feature` (tags `@TB-038`, `@S01`–`@S03` tối thiểu): login → logout thành công → refresh thất bại; logout token không hợp lệ; logout lần hai / token đã revoke (phụ thuộc O-2).
- Cập nhật **ADR 0014** (mục logout / explicit revoke) — delta, không tách ADR mới trừ khi plan-review yêu cầu.
- Quan sát (tùy O-3): structured log `event=logout` (`user_id`, `family_id`; không token) và/hoặc counter `logout_total`.

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
| Idempotent / đã revoke | – | B1 | @S03 | BDD (nếu O-2 chọn idempotent) |
| Hồi quy refresh/reuse | FR-009/010 | B5 | refresh-token @S01–@S06 | `!~Compose` |

## 3. Các bước

### B0. ADR 0014 delta (logout)
- Ghi đường đi **explicit logout** (khác reuse: không cần `used_at`).
- Kiểm chứng: ADR khớp SQL family revoke.

### B1. test-init (đỏ)
- Tạo `logout-token.feature`: @S01 happy path; @S02 token không tồn tại; @S03 gọi logout lại sau khi đã revoke (hành vi theo O-2).
- Steps: gửi logout với stored refresh; assert status; reuse refresh step hiện có.
- Stub endpoint **501** hoặc absent → đỏ đúng lý do.

### B2. `RefreshTokenService` + revoke by session
- VD `TryRevokeSessionAsync(plaintext)` → hash → `SELECT family_id` với `expires_at > now()`, `revoked_at IS NULL` → family revoke.
- Không tăng `refresh_reuse_total` (không phải FR-010 replay).

### B3. `Features/Logout/LogoutEndpoints.cs`
- Map `POST /api/v1/auth/logout` trong `Program.cs`.
- Response theo **O-1**; lỗi theo **O-2**.

### B4. Observability (theo O-3)
- Log/metric nếu được chốt.

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
| Logout token xấu lộ trạng thái | 401 chung hoặc 204 idempotent (O-2) | Revert PR |
| Nhầm family | Chỉ revoke `family_id` của token match hash | Revert |
| Access JWT vẫn dùng được | BR-006 đã chốt | không rollback schema |

Ảnh hưởng consumer: không đổi contract access; thêm endpoint logout (additive).

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, identity `!~Compose`.
- Issue: tiêu chí FR-011 (phần refresh) tick + hồi quy refresh.

## 7. Câu hỏi mở (plan-review)

| # | Fingerprint | Câu hỏi | Gợi ý (không chốt) |
|---|---|---|---|
| O-1 | plan-review:TB-038:logout-success-status | Thành công: **204 No Content** hay **200** body rỗng? | 204 (REST revoke); login/refresh dùng 200 JSON |
| O-2 | plan-review:TB-038:logout-invalid-token | Token không tồn tại / hết hạn / đã revoke khi logout → **401** (spec: cần refresh hợp lệ) hay **204 idempotent**? | 401 chung giống refresh |
| O-3 | plan-review:TB-038:logout-observability | Chỉ structured log, thêm `logout_total`, hay cả hai? | log + `logout_total` (design liệt kê metric bảo mật) |
| O-4 | plan-review:TB-038:logout-after-rotate-which-token | Client gửi token **mới nhất** sau xoay (refresh cuối) — có bắt buộc trong BDD không? | @S01 dùng token sau login; @S01b tuỳ chọn rotate rồi logout token mới |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-037 PR #33 |