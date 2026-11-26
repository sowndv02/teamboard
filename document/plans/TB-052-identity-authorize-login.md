---
title: Identity lát 14b, Razor login + cookie IdP + phát code
issue: TB-052
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth-i5.md
services: [identity-service]
---

# Plan: TB-052 — Identity lát 14b (FR-019, happy path FR-012)

Ảnh chụp: nhánh `feature/TB-052-identity-authorize-login` từ `main` `8652c52` (sau merge PR #38 / TB-042). **Đã có:** validation `GET /connect/authorize`, `authorization_code` table (chưa ghi), PKCE S256, registry, login JSON API + lockout. **Chưa có:** Razor, cookie IdP, `authorize_request`, redirect kèm `code`, thay 501.

**Giới hạn lát:** Không implement redeem mã tại token endpoint → **TB-043**.

## 1. Phạm vi của lát (14b)

**Làm:**

- Bảng `authorize_request` (context TTL ngắn — design D-I5-4, TB-042 Confirm O-4).
- Cookie authentication scheme **IdP** (D-I5-3: 30 phút sliding, `HttpOnly`, `Secure`, `SameSite=Lax`).
- Luồng authorize hợp lệ: lưu context → redirect login (hoặc bỏ qua form nếu cookie IdP còn hiệu lực).
- Razor Page login (email/mật khẩu); tái sử dụng logic đăng nhập/lockout hiện có (FR-002/FR-003) **trên trang**, không redirect client khi sai mật khẩu/khoá (FR-019).
- Phát `authorization_code` (hash DB, plaintext một lần trong redirect), redirect 302 tới `redirect_uri` với `code` + `state` (BR-005).
- Header bảo vệ clickjacking cơ bản (NFR-010, @S08).
- BDD mở rộng `authorize-pkce.feature`: @S01, @S04, @S05, @S06 (+ giữ hồi quy @S02…@S07).
- Metric (ví dụ `authorization_codes_issued_total`) — chốt tại plan-review.
- Thay `501` trên authorize hợp lệ bằng luồng thật.

**Không làm (14b):**

- `grant_type=authorization_code` + PKCE verify tại `POST /connect/token` → **TB-043**.
- Consent, đăng ký OAuth, đăng xuất JSON huỷ cookie IdP (ghi follow-up).
- OIDC, gateway, Angular app client.

## 2. Bảng truy vết

| Yêu cầu | Design / spec | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-012 happy authorize | i5, spec 8.1 | B4–B6 | @S01 | BDD + integration |
| FR-019 trang login origin | D-I5-8 | B5 | @S04, @S05 | BDD |
| FR-019 cookie 30 phút | D-I5-3 | B3 | @S06 | BDD |
| FR-003 lockout trên trang | FR-003 | B5 | @S05 | BDD |
| BR-005 mã một lần | D-I5-5 | B6 | @S01 | BDD (redirect có code; redeem TB-043) |
| NFR-008 rate limit | AuthPublic | B4 | (hồi quy) | optional |
| NFR-010 clickjacking | NFR-010 | B7 | @S08 | BDD/header |
| Confirm O-10 nonce | TB-042 | B1, B6 | @S01 | DB/assert + BDD |

Kịch bản trong `authorize-pkce.feature` (dự kiến):

- **@S01** Public client + PKCE + login → redirect `code` + `state`.
- **@S01b** *(optional cùng feature)* Confidential client + redirect + login → redirect `code` (Confirm O-10).
- **@S04** Sai mật khẩu → lỗi chung, **không** redirect client.
- **@S05** Đang khoá → thông báo / `Retry-After`, không redirect.
- **@S06** Cookie IdP hợp lệ → không form, redirect kèm `code`.
- **@S08** Header chống clickjacking trên trang login (và/hoặc authorize).

(@S02–@S07 đã xong 14a / TB-042.)

## 3. Các bước

### B0. Điểm xuất phát (đã xong 14a)

- **Kiểm chứng:** `GET /connect/authorize` validation + flag; migration `authorization_code` (6 migrations).

### B1. Persistence `authorize_request`

- Entity + migration: liên kết request authorize chờ login (client_id, redirect_uri, scope, state, code_challenge, code_challenge_method, **nonce**, expires_at, created_at; PK/id opaque).
- TTL **15 phút** (`expires_at`); dọn khi hết hạn hoặc sau phát mã (Confirm O-5).
- PK `authorize_request`: **Guid** (Confirm O-2).
- **Kiểm chứng:** migration apply; BDD @S04 dùng DB thật.

### B2. Cookie IdP + Data Protection

- `AddAuthentication` cookie scheme `Identity.Idp` (tách JWT bearer API nếu cần).
- Sliding expiration 30 phút idle (A-03).
- Data Protection: `Identity:DataProtection:KeysDirectory` (pattern signing keys; Confirm O-4).
- **Kiểm chứng:** unit/integration cookie sign-in; không ảnh hưởng login JSON API.

### B3. Authorize → login redirect

- Sau validate (reuse `AuthorizeRequestValidator`): persist `authorize_request` → 302 tới **`/login`** với `returnUrl` nội bộ an toàn (Confirm O-1).
- Cookie IdP đã hợp lệ + context khớp → nhảy B6.
- **Kiểm chứng:** integration: GET authorize → Location login (chưa user).

### B4. Razor Pages bootstrap

- Bật Razor Pages; layout tối thiểu; không script bên thứ ba (NFR-010).
- **Kiểm chứng:** smoke route login 200; build warnaserror.

### B5. Login form (FR-019)

- Page POST: email/mật khẩu; gọi cùng domain login/lockout như API JSON.
- Sai mật khẩu / khoá: HTML lỗi chung trên origin, **không** Location tới `redirect_uri` (@S04, @S05).
- Razor **ValidateAntiForgeryToken** (Confirm O-3).
- **Kiểm chứng:** BDD @S04, @S05.

### B6. Phát mã + redirect

- Sinh code: **32 byte** random → **base64url** plaintext (~43 chars) một lần; lưu **hash** (Confirm O-6).
- Migration (expand): cột **`scope`** varchar trên `authorization_code` (space-delimited; Confirm O-7).
- Lưu row: user_id, PKCE copy, **scope**, **nonce** từ context, expires **60s** D-I5-5; xoá/done `authorize_request`.
- 302 `redirect_uri`?`code=...&state=...`; metric **`authorization_codes_issued_total`** (Confirm O-8).
- Confidential client có redirect đăng ký: cùng luồng trong BDD (Confirm O-10); PKCE vẫn theo client type policy hiện có.
- **Kiểm chứng:** BDD @S01 — client không auto-redirect: assert `Location` hoặc follow manual; URL chứa `code` và `state`.

### B7. NFR-010 headers

- **`Content-Security-Policy: frame-ancestors 'none'`** trên `/login` (và authorize HTML nếu render); không X-Frame-Options (Confirm O-9).
- **Kiểm chứng:** @S08.

### B8. BDD + hồi quy

- Steps Reqnroll: register user, submit login form, assert redirect URL query, cookie jar cho @S06.
- **Kiểm chứng:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.

### B9. — (ngoài lát)

- Logout JSON huỷ cookie IdP; consent UI.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | Code generator/hash, authorize context TTL | xUnit |
| Integration | Cookie auth, redirect chain | WebApplicationFactory |
| BDD | FR-019, happy FR-012 | Reqnroll + Postgres Docker |

Công cụ: Reqnroll/xUnit (`config/tool-versions.yaml`).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Open redirect qua `returnUrl` | Chỉ path nội bộ, không echo `redirect_uri` client trong login | Revert |
| Cookie không survive restart Docker | Data Protection path/volume — Confirm | Doc |
| Razor tăng surface XSS | Encode mặc định, không raw HTML user input | Review + @S08 |
| Lát quá lớn | Feature flag giữ `AuthorizationCodeEnabled` | Revert từng bước |

**Consumer:** TB-043 cần row `authorization_code` + `code_challenge`. **Lesson:** LL-003 (không Co-Author trailer).

## 6. Gate và Definition of Done

- Gate: `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.
- Issue: tiêu chí tick; merge → lát 14b `done`; TB-043 unblock.

## 7. Câu hỏi mở và giả định

Không còn mục mở sau plan-review v1 (quyết định trong Confirm log).

**Giả định:** Reuse `LoginService`/lockout nội bộ; không duplicate hash logic.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-052:login-path | O-1 | `/login` Razor | 1 | 2026-11-26 |
| 2 | plan-review:TB-052:authz-req-id | O-2 | Guid PK `authorize_request` | 1 | 2026-11-26 |
| 3 | plan-review:TB-052:antiforgery | O-3 | Razor ValidateAntiForgeryToken | 1 | 2026-11-26 |
| 4 | plan-review:TB-052:dp-keys | O-4 | `Identity:DataProtection:KeysDirectory` | 1 | 2026-11-26 |
| 5 | plan-review:TB-052:authz-req-ttl | O-5 | TTL 15 phút | 1 | 2026-11-26 |
| 6 | plan-review:TB-052:code-format | O-6 | 32 byte → base64url | 1 | 2026-11-26 |
| 7 | plan-review:TB-052:code-scope-col | O-7 | Migration `scope` varchar trên `authorization_code` | 1 | 2026-11-26 |
| 8 | plan-review:TB-052:authz-code-metric | O-8 | `authorization_codes_issued_total` | 1 | 2026-11-26 |
| 9 | plan-review:TB-052:clickjack-header | O-9 | CSP `frame-ancestors 'none'` only | 1 | 2026-11-26 |
| 10 | plan-review:TB-052:bdd-client-type | O-10 | BDD public **và** confidential + redirect | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | 14b B1–B8, O-1..O-10 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau TB-042 merge |
| 2026-11-26 | plan-review v1 PASS | Confirm O-1..O-10 |