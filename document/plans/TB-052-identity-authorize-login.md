---
title: Identity lát 14b, Razor login + cookie IdP + phát code
issue: TB-052
status: draft
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
- TTL ngắn (ví dụ 10–15 phút — **Confirm**); dọn khi hết hạn hoặc sau phát mã.
- **Kiểm chứng:** migration apply; BDD @S04 dùng DB thật.

### B2. Cookie IdP + Data Protection

- `AddAuthentication` cookie scheme `Identity.Idp` (tách JWT bearer API nếu cần).
- Sliding expiration 30 phút idle (A-03).
- Cấu hình Data Protection (dev: file volume; prod — **Confirm** O-4 từ TB-042).
- **Kiểm chứng:** unit/integration cookie sign-in; không ảnh hưởng login JSON API.

### B3. Authorize → login redirect

- Sau validate (reuse `AuthorizeRequestValidator`): persist `authorize_request` → 302 tới login với `returnUrl` an toàn (chỉ nội bộ).
- Cookie IdP đã hợp lệ + context khớp → nhảy B6.
- **Kiểm chứng:** integration: GET authorize → Location login (chưa user).

### B4. Razor Pages bootstrap

- Bật Razor Pages; layout tối thiểu; không script bên thứ ba (NFR-010).
- **Kiểm chứng:** smoke route login 200; build warnaserror.

### B5. Login form (FR-019)

- Page POST: email/mật khẩu; gọi cùng domain login/lockout như API JSON.
- Sai mật khẩu / khoá: HTML lỗi chung trên origin, **không** Location tới `redirect_uri` (@S04, @S05).
- Anti-forgery (**Confirm** O-3).
- **Kiểm chứng:** BDD @S04, @S05.

### B6. Phát mã + redirect

- Sau login thành công: sinh code (cryptographically random), lưu `authorization_code` (hash, user_id, PKCE fields copy, expires ~60s D-I5-5), xoá/done `authorize_request`.
- 302 `redirect_uri`?`code=...&state=...` (exact match registry); đánh dấu metric.
- **Kiểm chứng:** BDD @S01 — client không auto-redirect: assert `Location` hoặc follow manual; URL chứa `code` và `state`.

### B7. NFR-010 headers

- `Content-Security-Policy: frame-ancestors 'none'` (hoặc `X-Frame-Options: DENY` — **Confirm**) trên login (+ authorize pages nếu cần).
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

| # | Điều chưa rõ | Ảnh hưởng | Ghi chú |
|---|---|---|---|
| O-1 | Đường dẫn login Razor (`/login`, `/account/login`, `/connect/login`) | B4, B5 | Hướng: `/login` ngắn gọn |
| O-2 | `authorize_request` id: GUID vs random base64url | B1, B3 | |
| O-3 | Anti-forgery: built-in Razor vs custom | B5, NFR-010 | |
| O-4 | Data Protection key ring (compose volume) | B2 | TB-042 O-5 deferred |
| O-5 | TTL `authorize_request` (phút) | B1 | Design: “ngắn” |
| O-6 | Độ dài/format `code` plaintext (OAuth practice ~32–64) | B6 | |
| O-7 | Lưu `scope` trên `authorization_code` row (TB-043) | B6, migration | Cột chưa có 14a |
| O-8 | Metric tên + labels | B6 | TB-042 plan B8 |
| O-9 | CSP vs X-Frame-Options | B7 | Confirm @S08 |
| O-10 | Chỉ **public** client trong BDD happy path (như 14a) | B6 | |

**Giả định tạm:** Reuse `LoginService`/lockout nội bộ; không duplicate hash logic.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau TB-042 merge |