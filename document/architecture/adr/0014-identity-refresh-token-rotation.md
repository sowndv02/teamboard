# ADR 0014 — Refresh token xoay vòng nguyên tử (D4)

Trạng thái: Chấp nhận · Ngày: 2026-11-26
Nguồn: TB-036, `document/plans/TB-036-identity-refresh-token.md`, design D4
Nhãn: `[Cần]`

## Bối cảnh
Access token JWT đã có (TB-032–035). FR-009 yêu cầu refresh opaque, chỉ lưu băm, xoay vòng nguyên tử khi `POST /api/v1/auth/refresh`. Design D4 chọn PostgreSQL compare-and-set một câu `UPDATE` trong transaction với `INSERT` token mới cùng `family_id`. FR-010 (reuse → thu hồi chuỗi) tách sang TB-037.

## Quyết định
- **Lưu trữ:** bảng `refresh_token` (`token_hash` PK, `family_id`, `user_id`, `client_id`, `expires_at`, `used_at`, `revoked_at`). Chỉ lưu **SHA-256** của chuỗi opaque (Base64 của digest); không log plaintext (BR-007).
- **Opaque token:** 32 byte `RandomNumberGenerator`, mã hóa **base64url** không padding; băm bằng UTF-8 của chuỗi đó trước khi lưu.
- **Login thành công:** phát additive `refresh_token`, `refresh_expires_in`; `family_id` mới mỗi lần đăng nhập.
- **Xoay vòng:** `UPDATE refresh_token SET used_at = now() WHERE token_hash = @h AND used_at IS NULL AND revoked_at IS NULL AND expires_at > now() RETURNING ...` trong transaction với `INSERT` token mới cùng `family_id`. Một thắng; request thua (đồng thời hoặc token đã dùng) → **401** `INVALID_CREDENTIALS` chung (TB-036 không thu hồi family — TB-037).
- **Hợp đồng refresh 200:** `access_token`, `token_type`, `expires_in`, `refresh_token`, `refresh_expires_in` — **không** `id`.
- **Cấu hình:** `Identity:RefreshToken:LifetimeSeconds` (mặc định 604800), `DefaultClientId` = `teamboard` (đến TB-040 client registry).
- **Metric:** `refresh_tokens_issued_total` (login + mỗi refresh mới); `refresh_rotations_total` (mỗi refresh 200).
- **Rate limit:** policy `auth-public` khi bật (giống login).

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. UPDATE CAS + transaction (chọn) | Khớp D4; một instance DB | SQL thô trong slice | Chọn |
| B. Redis GETDEL | Nhanh | Hạ tầng thêm | Không (D4) |
| C. SELECT FOR UPDATE | Đúng | Deadlock, phức tạp | Không |

## Hệ quả
- Consumer chỉ đọc access vẫn OK; client mới đọc refresh.
- TB-037 bổ sung reuse family revoke và `refresh_reuse_total`.
- ADR 0012/JWKS không đổi.

## Liên kết
FR-009, design D4, TB-036 plan, TB-037 (FR-010).