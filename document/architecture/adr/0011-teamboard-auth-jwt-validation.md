# ADR 0011 — Kiểm access JWT qua `Teamboard.Auth`

Trạng thái: Đề xuất · Ngày: 2026-11-26
Nguồn: TB-033, `document/plans/TB-033-auth-token-validation-library.md`, design D2
Nhãn: `[Chọn để học]`

## Bối cảnh
Resource server phải tự kiểm access JWT (FR-006) mà không gọi identity mỗi request. Design D2 chốt thư viện dùng chung `Teamboard.Auth` trong `src/shared`, bọc `JwtBearer` + JWKS cache, không tự viết parser. Identity đã phát RS256 (ADR 0010). TB-034 sẽ công bố JWKS HTTP; TB-033 dùng file JWKS tĩnh dev/CI và client HTTP tùy chọn.

## Quyết định
- Thư viện: `Teamboard.Shared.Auth` (`src/shared/Auth`), extension `AddTeamboardJwtAuthentication`.
- Xác thực: `Microsoft.AspNetCore.Authentication.JwtBearer`, ép **RS256**, validate `iss`, `aud`, `exp`, `nbf`; map `sub` làm user id.
- Cấu hình `Auth:Jwt:*`:
  - `ValidIssuer` (mặc định `http://localhost:6061` — đồng bộ `Identity:Jwt:Issuer` dev; gateway sau lát 19).
  - `ValidAudience` (`teamboard`).
  - `StaticJwksFile` (dev/CI).
  - `JwksUri` (tùy chọn; bật khi TB-034 có endpoint).
- JWKS cache TTL **10 phút**; `kid` lạ → refresh tối đa **1 lần/phút**; lỗi tải giữ bản cache cũ (D2).
- Claim kỳ vọng: `token_use=user` cho access token người (BR-008); contract với ADR 0010.
- **Không** trong lát: policy scope (TB-045), gateway JWT, machine token.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. `Teamboard.Auth` bọc JwtBearer (chọn) | Một nơi cấu hình; giảm lệch service | Nâng version đồng loạt | Chọn |
| B. Mỗi service tự JwtBearer | Nhanh ban đầu | Lệch `aud`/`alg` | Không (D2) |
| C. Chỉ gateway | Ít code RS | Trái FR-006 | Không |

## Hệ quả
- `board-service` và service .NET khác tham chiếu `Teamboard.Shared.Auth`.
- Phụ thuộc `Microsoft.AspNetCore.Authentication.JwtBearer` + IdentityModel (T0).
- Test tấn công (NFR-006) trong `Teamboard.Shared.Auth.Tests` và BDD endpoint mẫu `GET /api/v1/auth/me`.

## Liên kết
ADR 0010, design D2, TB-033 plan, `config/signing-keys.example.json`.