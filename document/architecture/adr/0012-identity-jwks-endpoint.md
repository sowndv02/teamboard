# ADR 0012 — Endpoint JWKS `/.well-known/jwks.json`

Trạng thái: Chấp nhận · Ngày: 2026-11-26
Nguồn: TB-034, `document/plans/TB-034-identity-jwks.md`, design D2 mục 6.3
Nhãn: `[Chọn để học]`

## Bối cảnh
Resource server tải khoá công khai qua HTTP (FR-007, FR-006). ADR 0010/0011 đã chốt RS256 và `Teamboard.Auth`; TB-033 dùng file tĩnh dev và HTTP fallback. TB-034 công bố JWKS trên identity.

## Quyết định
- Đường dẫn: **`GET /.well-known/jwks.json`** (ngoại lệ `api-endpoints.md` mục 1; chuẩn OIDC/JWK Set).
- Nội dung: JSON **JWK Set** gồm mọi khoá **công khai** RS256 **được công bố** (`pending`, `active`, `retiring` khi đang xoay — ADR 0013); không xoay thì thường chỉ một khoá `active`.
- Header: **`Cache-Control: public, max-age=600`** (khớp TTL cache consumer, design D2).
- Không có khoá `active` để xuất bản: **HTTP 503** (plan-review Confirm #1; nhất quán login không phát token).
- Metric: counter **`jwks_served_total`** mỗi phản hồi JWKS **200** (Confirm #2).
- Nguồn khoá: cùng file/config `Identity:SigningKeys:JsonFile` như ADR 0010 (dev); không lộ `privateKeyPem` trong response.
- **Không** trong lát TB-034: route gateway, discovery OIDC đầy đủ. Chu trình xoay và JWKS đa khoá: **ADR 0013** (TB-035).

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. `/.well-known/jwks.json` (chọn) | Chuẩn OIDC; consumer đã chuẩn bị URI | Ngoại lệ path API nội bộ | Chọn |
| B. `/api/v1/auth/jwks` | Khớp api-endpoints | Lệch OIDC; đổi consumer sau | Không |
| C. 200 + `keys:[]` khi không có khoá | JSON hợp lệ | Resource server không phân biệt lỗi cấu hình | Không (Confirm #1) |

## Hệ quả
- `Teamboard.Auth` đảo **HTTP primary** khi `JwksUri` cấu hình (TB-034 B4).
- Gateway lát 19 sẽ proxy cùng path công khai.

## Liên kết
FR-007, ADR 0010, ADR 0011, `document/design/identity-auth.md` D2/D3, TB-034 plan.