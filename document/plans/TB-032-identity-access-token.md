---
title: Identity lát 4, phát access token JWT khi đăng nhập
issue: TB-032
status: draft
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-032 — Identity lát 4, phát access token JWT khi đăng nhập

Lát 4 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-032-identity-access-token` từ `origin/main` `cbc8124` (sau merge PR #26 / TB-031). Đã có: đăng ký, đăng nhập `{ "id" }`, lockout, rate limit cờ, `login_attempt`, BDD `register` / `login` / `lockout-rate-limit`.

## 1. Phạm vi của lát

**Làm:**
- **ADR** (trước code): RS256, khoá ký (D1/D3), phát token bằng `Microsoft.IdentityModel.JsonWebTokens` (D2 — không tự dựng JWT); sửa phạm vi học tập ADR 0006 nếu cần.
- FR-005 / BR-004 (access **10 phút**, A-03): sau login thành công, phản hồi 200 gồm **access JWT** (RS256) + giữ `id` (mở rộng contract TB-030); claim `iss`, `sub` (user id), `aud` (`teamboard`), `exp`, `iat`, `nbf`, `jti`, `token_use=user` (BR-008).
- D3 local: nạp khoá ký từ cấu hình (file PEM / JSON trong `Identity:SigningKeys:*`, không commit secret); `/health/ready` **không đạt** nếu không có khoá `active` khi bật phát token.
- `Identity:Jwt:*` options: `Issuer`, `Audience`, `AccessTokenLifetimeSeconds` (mặc định 600); test factory override thời gian ngắn.
- Metric (NFR-007): ví dụ `access_tokens_issued_total` hoặc tái dùng pattern hiện có — ghi trong B1.
- BDD: mở rộng `login.feature` (@S01 cập nhật) + `access-token.feature` (@S01–@Sx) — JWT hợp lệ, claim, không lộ trong response khi lỗi; không trả token khi 401.
- Chuẩn bị **Secrets Manager** production (cấu hình + contract secret JSON); implement nạp AWS chỉ khi có binding rõ trong plan-review (có thể lát con: dev file trước, SM sau nếu CONFIRM).

**Không làm (trong lát này):**
- Refresh token (TB-036, FR-009); JWKS endpoint (TB-034); `Teamboard.Auth` / JwtBearer resource server (TB-033); xoay khoá CLI (TB-035); rate limit `/token` (TB-043); gateway route (lát 19).
- File `document/contracts/` đầy đủ (ghi claim trong plan/ADR; contract file có thể TB-033).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-005 access JWT | D1, D2, D3 | B1–B3 | @S01–@S04 | Token RS256, claim đủ |
| BR-004 TTL 10 phút | A-03 | B3 | @S02 | `exp` trong cửa sổ |
| BR-008 `token_use` user | D2 | B3 | @S03 | Claim `token_use` |
| FR-002 không lộ token khi lỗi | mục 6.4 | B4 | @S05, login hồi quy | 401 không JWT |
| NFR-007 metric | mục 9 | B1 | @S06 | counter |
| Ready khoá ký | D3, spec 8.3 | B1 | health test | ready 503 thiếu khoá |
| Gate | – | B1–B4 | – | build, test, format |

Kịch bản dự kiến (`@TB-032 @identity-service @docker`):
- @S01 Login thành công → 200, có `id`, `access_token`, `token_type` Bearer, `expires_in`; JWT decode được, `alg` RS256, `kid` có mặt.
- @S02 `exp` - `iat` ≈ cấu hình lifetime (test dùng 120s).
- @S03 Payload có `sub` = user id, `aud` = `teamboard`, `iss` = cấu hình, `token_use` = `user`, `jti` không rỗng.
- @S04 Response body không chứa private key / PEM / chuỗi `BEGIN RSA`.
- @S05 Sai mật khẩu → 401, body không chứa `access_token` hay JWT ba phần.
- @S06 Metric phát token tăng khi @S01.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B0. ADR và khoá dev
- **File:** `document/architecture/adr/0007-identity-jwt-signing.md` (số xác nhận khi review), cập nhật ADR 0006 phạm vi nếu cần.
- **Kiểm chứng:** ADR accepted trong PR; `config/signing-keys.example.json` (hoặc `*.example.pem`) + `.gitignore` secret thật.

### B1. Signing key loader + options + ready
- **File:** `Security/Signing/`, `IdentityJwtOptions`, health ready check, packages `Microsoft.IdentityModel.*`.
- **Kiểm chứng:** build; unit test load key; ready fail khi thiếu khoá.

### B2. Access token issuer
- **File:** `IAccessTokenIssuer`, `AccessTokenIssuer` (JsonWebTokenHandler), đăng ký DI.
- **Kiểm chứng:** unit test claim + chữ ký RS256 với key test cố định.

### B3. Login contract
- **File:** `LoginEndpoints`, `LoginResponse`; metrics.
- **Kiểm chứng:** integration đỏ → xanh @S01–@S04, @S06; cập nhật `login.feature` @S01.

### B4. BDD lỗi + hồi quy
- **File:** `access-token.feature`, steps decode JWT (test only, không lộ logic vào production).
- **Kiểm chứng:** @S05; lockout/register/login regression xanh.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | Issuer, options, key loader | xUnit |
| Integration | Login + JWT | Testcontainers, WebApplicationFactory |
| BDD | FR-005, BR-004 | Reqnroll |

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Breaking login JSON | Giữ `id`; thêm field (additive) | Revert; client chỉ đọc `id` vẫn OK |
| Khoá dev lọt git | `.example` + gitignore | Rotate dev key |
| `login.feature` @S01 đổi | Cập nhật cùng lát | Revert B4 |

## 6. Gate và Definition of Done

- Gate: `dotnet test identity-service.Tests` (`!~Compose`).
- Lát 4 `done` khi PR merge.

## 7. Giả định (chờ plan-review CONFIRM nếu cần)

- P-01: Response shape **additive** — `{ "id", "access_token", "token_type", "expires_in" }` (snake_case giống OAuth, khớp `api-endpoints` nếu có).
- P-02: `iss` configurable; local default trỏ URL identity (D0.5 gateway chưa có).
- P-03: Chỉ **access** token; refresh TB-036.

## Câu hỏi mở (plan-review)

| # | Fingerprint | Câu hỏi | Ghi chú |
|---|---|---|---|
| 1 | plan-init:TB-032:login-response-shape | Trường JSON chính xác và có giữ `id` không? | Đề xuất P-01 |
| 2 | plan-init:TB-032:issuer-url | `iss` local khi chưa gateway? | Đề xuất P-02 |
| 3 | plan-init:TB-032:secrets-manager-scope | SM trong lát 4 hay file dev only + SM issue sau? | D3 |
| 4 | plan-init:TB-032:adr-number | Số ADR 0007 hay khác? | Index ADR |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-031 |