# ADR 0010 — Ký access token JWT (RS256) trong identity-service

Trạng thái: Đề xuất · Ngày: 2026-11-26
Nguồn: TB-032, `document/plans/TB-032-identity-access-token.md`, design D1/D2/D3
Nhãn: `[Chọn để học]`

> **Ghi chú đánh số:** plan-review TB-032 ghi “ADR 0007”; số **0007** đã dùng cho [`0007-dependency-policy.md`](0007-dependency-policy.md). ADR JWT ký dùng **0010**.

## Bối cảnh
Sau đăng nhập thành công, identity phải phát access JWT RS256 (FR-005, BR-004, BR-008) với TTL cấu hình (~10 phút). Design D2 đã chốt dùng `Microsoft.IdentityModel.JsonWebTokens` (`JsonWebTokenHandler`), không tự dựng JWT. Design D3 chốt khoá ký ngoài DB; TB-032 triển khai nạp từ file JSON dev (Secrets Manager sau).

## Quyết định
- Thuật toán ký: **RS256**, RSA 2048 (D1).
- Phát token: **`JsonWebTokenHandler`** + gói `Microsoft.IdentityModel.JsonWebTokens` / Tokens (D2); claim tối thiểu: `iss`, `sub` (user id), `aud` (`teamboard`), `iat`, `nbf`, `exp`, `jti`, `token_use=user`.
- Response login 200 (additive): `id`, `access_token`, `token_type` (`Bearer`), `expires_in` (giây, OAuth-style snake_case).
- Cấu hình: `Identity:Jwt:Issuer`, `Identity:Jwt:Audience`, `Identity:Jwt:AccessTokenLifetimeSeconds` (mặc định 600).
- Khoá ký dev: file JSON `Identity:SigningKeys:JsonFile` — mảng `keys[]` với `kid`, `algorithm` (`RS256`), `status` (`active` | …), `privateKeyPem`; không commit file thật (chỉ `config/signing-keys.example.json` + khoá test trong project test).
- `/health/ready`: **Unhealthy (503)** khi DB không dùng được (hiện có) **hoặc** không có khoá `active` để phát token (TB-032).
- Metric: counter `access_tokens_issued_total` (NFR-007), tăng mỗi lần phát access token thành công.
- **Không** trong lát: JWKS HTTP (TB-034), refresh (TB-036), `Teamboard.Auth` (TB-033), nạp AWS Secrets Manager (chỉ contract/chuẩn bị cấu hình).

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. RS256 + JsonWebTokenHandler + file JSON dev (đã chọn) | Chuẩn OIDC; giảm lỗi tự viết; debug dễ | Token lớn hơn ES256; khoá trong bộ nhớ process | Chọn |
| B. Tự ký/parse JWT trên BCL (ADR 0006 nguyên bản) | Học byte-level | Rủi ro bảo mật; trái design D2 | Không chọn |
| C. ES256 | Token nhỏ hơn | Ít quen hơn RS256 trong lab OIDC | Không chọn (D1 đã RS256) |
| D. HS256 shared secret | Đơn giản | Không phù hợp đa service / JWKS | Không chọn |

## Vì sao không chọn các phương án còn lại
- **B** trái quyết định D2 và thực tế triển khai; chỉ phù hợp lab byte-level có test tấn công tách biệt.
- **C/D** lệch D1 và mục tiêu JWKS/OIDC các lát sau.

## Hệ quả và đánh đổi
- Thu hẹp phạm vi “tự viết phát token” trong ADR 0006: học cấu hình, claim, khoá, test tấn công — không viết lại JWT encoder.
- Login JSON đổi additive; client chỉ đọc `id` vẫn tương thích đến khi cần bearer.
- Khoá dev trong file: rủi ro lộ repo → `.gitignore` + example; production dùng SM sau.

## Tín hiệu xem lại
- Dùng ngoài môi trường học / dữ liệu thật (ADR 0006).
- Cần ES256 hoặc ký trong KMS (D3 phương án B).

## Liên kết
`document/spec/identity-auth.md`, `document/design/identity-auth.md`, ADR 0006, TB-032 plan, `config/signing-keys.example.json`.