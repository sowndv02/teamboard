---
title: Identity lát 8, refresh token xoay vòng
issue: TB-036
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-036 — Identity lát 8, refresh token xoay vòng (FR-009, D4)

Lát 8 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-036-identity-refresh-token` từ `origin/main` `94dd3a2` (sau merge PR #30 / TB-035). Đã có: login phát access JWT (TB-032), JWKS + xoay khoá (TB-034/035), `login_attempt`, pattern EF + migration + BDD Reqnroll.

## 1. Phạm vi của lát

**Làm:**
- **ADR 0014** (đề xuất): refresh token opaque, băm SHA-256 lưu DB; xoay vòng bằng một câu `UPDATE … WHERE used_at IS NULL` trong transaction với `INSERT` token mới cùng `family_id` (D4).
- Bảng `refresh_token` + migration EF (design mục 6 / bảng dữ liệu).
- **Login thành công:** phát thêm **refresh token** (7 ngày, A-03) + `family_id` mới; response additive: `refresh_token`, `refresh_expires_in` (snake_case như access).
- **`POST /api/v1/auth/refresh`:** body `{ "refresh_token": "..." }`; hợp lệ → **200** `access_token`, `token_type`, `expires_in`, `refresh_token`, `refresh_expires_in` (**không** `id`). Token cũ → `used_at` (xoay vòng). Hết hạn / không tồn tại / `revoked_at` → **401** `InvalidCredentials` (chung, không lộ chi tiết).
- **Đồng thời (FR-009):** tối đa một 200; request thua CAS → **401** chung. **Reuse** (`used_at` đã set, FR-010 thu hồi family + metric) → **TB-037**.
- Chỉ lưu **hash**; không log refresh token (BR-007). Rate limit policy `auth-public` như login (cờ hiện có).
- BDD `refresh-token.feature` @S01–@S05; mở rộng login @S01 kiểm có refresh; TDD repository/CAS.
- Metric: `refresh_tokens_issued_total` (login + mỗi refresh mới); `refresh_rotations_total` (POST `/refresh` 200).

**Không làm (trong lát này):**
- Thu hồi cả `family_id` khi reuse (FR-010), metric `refresh_reuse_total`, security event đầy đủ → **TB-037**.
- Đăng xuất / thu hồi refresh (FR-011) → **TB-038**; đổi mật khẩu thu hồi refresh (FR-022) → **TB-039**.
- Client registry đa `client_id` (FR-014) → `Identity:RefreshToken:DefaultClientId` = `teamboard` (Confirm #2).
- Gateway route, cookie phiên I5, OAuth `/connect/token`.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-009 cấp refresh khi login | D4 | B3 | login @S01 mở rộng | BDD |
| FR-009 xoay vòng khi đổi | D4 | B4 | @S02 | BDD + integration |
| FR-009 từ chối hết hạn/không hợp lệ | D4 | B4 | @S03 | BDD |
| FR-009 chỉ lưu băm | D4, BR-007 | B2 | unit hash | unit |
| BR-007 không log refresh | BR-007 | B4, B6 | @S01 | BDD + review log |
| BR-004 refresh 7 ngày | A-03 | B3 | @S04 | BDD/config |
| FR-009 đồng thời một thắng | D4 | B5 | @S05 | integration 2 thread |
| NFR-007 metric | mục 9 | B6 | step metric | BDD/unit |
| Gate | – | – | hồi quy login/JWKS | `!~Compose` |

Kịch bản dự kiến (`document/spec/features/identity-service/refresh-token.feature`):
- **@S01** Login 200 → có `refresh_token` opaque; body không chứa chuỗi refresh trong log step (không echo hash DB).
- **@S02** `POST /refresh` với token hợp lệ → 200, access mới + refresh mới **khác** token cũ; gọi lại với token cũ → 401.
- **@S03** Token ngẫu nhiên / hết hạn / `revoked_at` (seed) → 401 chung.
- **@S04** `refresh_expires_in` hoặc TTL DB ≈ 7 ngày (test rút ngắn qua config).
- **@S05** Hai request song song cùng token → đúng một 200 (integration; BDD có thể đơn giản hoá hoặc tag `@integration`).

## 3. Các bước

### B0. ADR 0014 (refresh token nguyên tử)
- File: `document/architecture/adr/0014-identity-refresh-token-rotation.md`.
- Cập nhật index `document/architecture/README.md`.
- Kiểm chứng: mô tả khớp D4, phân ranh FR-009 vs FR-010 (TB-037).

### B1. Migration + entity `RefreshToken`
- `Persistence/Entities/RefreshToken.cs`, migration `AddRefreshToken`, index unique `token_hash`.
- Kiểm chứng: `dotnet ef` / migration apply trong BDD Given.

### B2. Refresh token cryptography + options
- `Identity:RefreshToken:*` — lifetime seconds (mặc định 604800), default `client_id` string.
- Generator: 32 bytes RNG → base64url; hash SHA-256 trước khi lưu.
- Kiểm chứng: unit test hash deterministic, không lưu plaintext trong entity snapshot test.

### B3. Login phát refresh (additive)
- `LoginEndpoints` / issuer service: sau access token, tạo family + refresh, persist transaction với user login.
- Mở rộng `login.feature` @S01 (hoặc scenario mới @S10).
- Kiểm chứng: BDD xanh; hồi quy login 401 không có refresh.

### B4. Endpoint `POST /api/v1/auth/refresh`
- Slice `Features/Refresh/`; repository raw SQL hoặc `ExecuteSql` CAS (D4 pattern); rate limit `auth-public` khi bật (Confirm #5).
- Kiểm chứng: @S02, @S03.

### B5. Đồng thời
- Integration test hai `Task` gọi refresh; đếm 200 vs 401.
- Kiểm chứng: @S05; không yêu cầu family revoke ở lát này.

### B6. Metric + quan sát
- `refresh_tokens_issued_total`, `refresh_rotations_total`; không ghi token trong log (BR-007).
- Kiểm chứng: step BDD hoặc unit `IdentityMetrics`.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | hash, options, mapper response | không DB |
| Integration | CAS hai luồng | Testcontainers PostgreSQL (pattern hiện có) |
| BDD | @S01–@S05 | Docker identity + DB như login |

Công cụ: Reqnroll/xUnit (`config/tool-versions.yaml`).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Login contract đổi | Additive fields only | Revert PR; clients chỉ đọc `id` vẫn OK |
| Migration lỗi | expand-only bảng mới | Drop table migration down (dev) |
| TB-037 phụ thuộc CAS | Giữ `used_at` semantics D4 | – |

Ảnh hưởng consumer: chưa có FE; chỉ identity BDD. Không đổi JWT claim.

## 6. Gate và Definition of Done
- `dotnet build src/teamboard.slnx -warnaserror`, format, identity tests `!~Compose`, board `Category!=docker`.
- Issue TB-036 tiêu chí + ADR 0014 + plan `approved`.

## 7. Câu hỏi mở và giả định

Không còn câu hỏi mở sau plan-review v1.

**Giới hạn đã biết:** FR-010 đầy đủ (family revoke, `refresh_reuse_total`) chờ **TB-037**; @S02 gọi lại token cũ sau xoay → 401 (chưa thu hồi chuỗi).

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-036:fr010-boundary | O-1 FR-009/010 | TB-036 CAS + 401 thua; reuse family → TB-037 | 1 | 2026-11-26 |
| 2 | plan-review:TB-036:default-client | O-2 client_id | `DefaultClientId` = `teamboard` | 1 | 2026-11-26 |
| 3 | plan-review:TB-036:refresh-contract | O-3/O-4 JSON | Body `refresh_token`; 200 không `id` | 1 | 2026-11-26 |
| 4 | plan-review:TB-036:refresh-rate-limit | O-5 rate limit | Policy `auth-public` khi bật | 1 | 2026-11-26 |
| 5 | plan-review:TB-036:refresh-metrics | Metric names | `refresh_tokens_issued_total`, `refresh_rotations_total` | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B0–B6, O-1..O-5 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-035 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#5 |