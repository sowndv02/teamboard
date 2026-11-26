---
title: Identity lát 8, refresh token xoay vòng
issue: TB-036
status: draft
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
- **Login thành công:** phát thêm **refresh token** (7 ngày, A-03) + `family_id` mới; response additive: `refresh_token`, `refresh_expires_in` (hoặc tương đương OAuth snake_case).
- **`POST /api/v1/auth/refresh`:** body gửi refresh token; hợp lệ → cặp access + refresh **mới**, token cũ đánh dấu `used_at` (xoay vòng). Hết hạn / không tồn tại / đã thu hồi (không có `used_at`) → **401** `problem+json`, không lộ token thật.
- **Đồng thời (FR-009):** hai request cùng refresh token → **tối đa một** 200; request thua → 401 (hành vi chi tiết khi phát hiện **dùng lại** sau khi đã xoay → **TB-037**, FR-010).
- Chỉ lưu **hash**; không log refresh token (BR-007). Rate limit policy `auth-public` như login (cờ hiện có).
- BDD `refresh-token.feature` @S01–@S05; mở rộng login @S01 kiểm có refresh; TDD repository/CAS.
- Metric (NFR-007 mở rộng): ví dụ `refresh_tokens_issued_total` / `refresh_rotations_total` (tên chốt plan-review).

**Không làm (trong lát này):**
- Thu hồi cả `family_id` khi reuse (FR-010), metric `refresh_reuse_total`, security event đầy đủ → **TB-037**.
- Đăng xuất / thu hồi refresh (FR-011) → **TB-038**; đổi mật khẩu thu hồi refresh (FR-022) → **TB-039**.
- Client registry đa `client_id` (FR-014) → dùng **client mặc định** cố định config (O-2).
- Gateway route, cookie phiên I5, OAuth `/connect/token`.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-009 cấp refresh khi login | D4 | B3 | login @S01 mở rộng | BDD |
| FR-009 xoay vòng khi đổi | D4 | B4 | @S02 | BDD + integration |
| FR-009 từ chối hết hạn/không hợp lệ | D4 | B4 | @S03 | BDD |
| FR-009 chỉ lưu băm | D4, BR-007 | B2 | unit hash | unit |
| BR-004 refresh 7 ngày | A-03 | B3 | @S04 | BDD/config |
| FR-009 đồng thời một thắng | D4 | B5 | @S05 | integration 2 thread |
| NFR-007 metric | mục 9 | B6 | step metric | BDD/unit |
| Gate | – | – | hồi quy login/JWKS | `!~Compose` |

Kịch bản dự kiến (`document/spec/features/identity-service/refresh-token.feature`):
- **@S01** Login 200 → có `refresh_token` opaque; body không chứa chuỗi refresh trong log step (không echo hash DB).
- **@S02** `POST /refresh` với token hợp lệ → 200, access mới + refresh mới **khác** token cũ; gọi lại với token cũ → 401.
- **@S03** Token ngẫu nhiên / hết hạn (test clock hoặc DB seed) → 401 chung.
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
- Slice `Features/Refresh/`; repository raw SQL hoặc `ExecuteSql` CAS (D4 pattern).
- Kiểm chứng: @S02, @S03.

### B5. Đồng thời
- Integration test hai `Task` gọi refresh; đếm 200 vs 401.
- Kiểm chứng: @S05; không yêu cầu family revoke ở lát này.

### B6. Metric + quan sát
- Counter khi phát refresh mới / khi rotation thành công; không ghi token trong log.
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

| # | Điều chưa rõ | Ảnh hưởng | Ghi chú |
|---|---|---|---|
| O-1 | Request thua đồng thời: chỉ 401 hay gọi luôn logic reuse (FR-010)? | B5, TB-037 ranh giới | **Giả định plan:** chỉ 401 ở TB-036; family revoke TB-037 |
| O-2 | `client_id` khi chưa có TB-040 | Bảng DB | **Giả định:** config `Identity:RefreshToken:DefaultClientId` = `teamboard` |
| O-3 | Tên field response refresh | BDD | **Giả định:** `refresh_token` + `refresh_expires_in` (snake_case như access) |
| O-4 | Body refresh: JSON `{ "refresh_token": "..." }` vs chỉ field | Endpoint | **Giả định:** một field JSON như OAuth token request đơn giản |
| O-5 | Rate limit `/refresh` | Policy | **Giả định:** cùng `auth-public` khi cờ bật |

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-035 |