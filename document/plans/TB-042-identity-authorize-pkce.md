---
title: Identity lát 14, Authorization Code + PKCE (authorize và trang đăng nhập)
issue: TB-042
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-042 — Identity lát 14a, Authorize + PKCE validation (FR-012, BR-005)

Lát 14 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-042-identity-authorize-pkce` từ `main` `138b81a` (sau merge PR #37 / TB-041). Đã có: registry client + `RedirectUriMatcher`, login JSON + lockout/rate limit, refresh, `POST /connect/token` (client_credentials, TB-041). **Chưa có:** `GET /connect/authorize`, lưu mã uỷ quyền, PKCE, phiên cookie IdP, trang đăng nhập trên origin identity, redirect kèm `code`.

**Phụ thuộc design:** `document/design/identity-auth.md` chỉ PASS I1–I4, I6; I5 chi tiết **hoãn** đến spec FE confirmed (D6, Confirm log #1). Plan này dựa trên spec identity (FR-012, FR-019, BR-005, NFR-008, NFR-010) + hướng D6; phần chưa có trong design → **Câu hỏi mở** tại `plan-review`.

**Giới hạn lát / TB-043:** Lát này **không** implement `authorization_code` grant trên `POST /connect/token` (FR-013) → **TB-043**. BDD có thể assert `code` trong redirect; đổi mã lấy token là lát 15.

**Chia lát (Confirm O-9):** TB-042 = **14a** (lát này). **14b** (login Razor + cookie IdP + phát `code` + BDD happy path) → issue mới (TBD key), phụ thuộc 14a xong.

## 1. Phạm vi của lát (14a)

**Làm:**
- Hoàn thiện và duyệt [`document/design/identity-auth-i5.md`](../design/identity-auth-i5.md) (Confirm O-1).
- Bảng `authorization_code` + migration (chuẩn bị cho 14b/TB-043).
- `PkceValidator` (S256) + authorize query parser; unit tests.
- `AuthorizeRequestValidator` dùng `IClientRegistry` + `RedirectUriMatcher` (BR-005, FR-012).
- `GET /connect/authorize`: validate; sai/thiếu → lỗi **không** redirect URI chưa tin cậy; flag `Identity:Features:AuthorizationCodeEnabled` default **false** (Confirm O-8).
- Rate limit `AuthPublic` khi bật (pattern TB-041).
- BDD phần **lỗi/validation** @S02, @S03, @S07 (client unknown); chưa yêu cầu redirect happy path có `code`.

**Không làm (14a — chuyển 14b):**
- Razor login, cookie IdP, redirect kèm `code` (FR-019) → **lát 14b**.
- `POST /connect/token` `authorization_code` → **TB-043** (sau 14b).
- OIDC, gateway, SPA client.

**Không làm (chung):**
- Consent, đăng ký OAuth, huỷ cookie trong logout JSON (follow-up sau 14b).

## 2. Bảng truy vết

| Yêu cầu | Design / spec | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-012 authorize + PKCE params | spec 8.1, D6 hướng | B3–B4 | @S01, @S03 | BDD + unit |
| FR-012 redirect_uri exact | BR-005, registry | B3 | @S02 | BDD + unit |
| FR-019 trang đăng nhập origin | i5 D-I5-8 | 14b | @S04, @S05 | lát kế |
| FR-019 phiên cookie 30 phút | i5 D-I5-3 | 14b | @S01 | lát kế |
| FR-003 lockout trên trang | FR-003 | 14b | @S06 | lát kế |
| BR-005 mã một lần, ngắn hạn | 6.2 (I5) | B1, 14b | @S01 | 14b + TB-043 |
| NFR-008 rate limit | AuthPublic | B4 | optional | integration |
| NFR-010 CSRF / clickjacking | NFR-010 | B6 | @S08 | BDD/header assert |
| Hồi quy registry/CC | – | B8 | client-registry, CC | `!~Compose` |

Kịch bản dự kiến (`document/spec/features/identity-service/authorize-pkce.feature`):
- **@S01** *(14b)* Public client + PKCE + login → redirect `code` + `state`.
- **@S02** `redirect_uri` không trong registry → lỗi trên origin, **không** redirect tới URI tấn công.
- **@S03** Thiếu `state` hoặc `code_challenge` → lỗi, không redirect.
- **@S04** Sai mật khẩu → cùng thông báo lỗi chung, **không** redirect tới client.
- **@S05** Đang khoá tạm → `Retry-After` / thông báo, không redirect.
- **@S06** Đã có cookie IdP hợp lệ → không hiện form, redirect kèm `code`.
- **@S07** `client_id` không tồn tại → lỗi an toàn (không redirect).
- **@S08** Header bảo vệ clickjacking cơ bản (ví dụ `Content-Security-Policy: frame-ancestors 'none'` hoặc tương đương — theo Confirm).

## 3. Các bước

### B0. Design phụ lục I5
- **Mục tiêu:** `identity-auth-i5.md` status `approved` sau plan-review; chốt TBD (O-3, O-4, O-6…).
- **Kiểm chứng:** design-review ngắn hoặc plan-review PASS với Confirm log.

### B1. Persistence `authorization_code`
- Entity + migration EF: `code_hash`, `client_id`, `redirect_uri`, `code_challenge`, `code_challenge_method`, `user_id`, `expires_at`, `used_at`, `created_at`.
- Index unique `code_hash`; TTL dọn (hosted hoặc query lúc redeem — redeem ở TB-043).
- **Kiểm chứng:** migration apply trong test DB.

### B2. PKCE và authorize request parsing
- `PkceValidator` (S256: hash verifier, so `code_challenge`); reject plain nếu Confirm #6.
- Parse query: `client_id`, `redirect_uri`, `response_type`, `scope`, `state`, `code_challenge`, `code_challenge_method`.
- **Kiểm chứng:** unit xanh.

### B3. Authorize validation service
- Dùng `IClientRegistry`: client tồn tại, **public** (hoặc cả confidential nếu Confirm), `ValidateRedirectUri`, scope ⊆ `allowed_scopes` (nếu Confirm #7).
- Gom lỗi thành “safe authorize error” (không leak client existence nếu Confirm #3 chọn).
- **Kiểm chứng:** unit + integration đỏ.

### B4. `GET /connect/authorize` (validation only — 14a)
- Map route; feature flag `AuthorizationCodeEnabled`; rate limit khi bật.
- Không hợp lệ → error theo Confirm O-3; **không** redirect tới URI chưa tin cậy.
- Hợp lệ → 501 / redirect tới login placeholder **hoặc** message “login slice pending” cho đến 14b (chọn tại test-init).
- **Kiểm chứng:** BDD @S02, @S03, @S07.

### B5–B7. *(Chuyển lát 14b)* Cookie IdP, Razor login, phát mã redirect.

### B8. BDD + config (14a)
- Feature file + steps (follow client chuyển hướng / HttpClient không auto-redirect nếu cần).
- Seed `teamboard` redirect URI khớp BDD (`https://localhost/callback` hoặc Confirm).
- Metric: ví dụ `authorization_codes_issued_total` (Confirm #8).
- **Kiểm chứng:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.

### B9. — (ngoài lát)
- Wire logout JSON huỷ cookie IdP → issue follow-up hoặc TB-038 mở rộng.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | PKCE S256, redirect matcher tái sử dụng, authorize validator | xUnit |
| Integration | Cookie sign-in, authorize redirect | WebApplicationFactory |
| BDD | FR-012, FR-019, BR-005 | Reqnroll + Postgres Docker |

Công cụ: Reqnroll/xUnit (`config/tool-versions.yaml`).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Design I5 chưa đủ → rework | B0 + plan-review Confirm | Revert |
| Lát quá lớn | Chia bước/flag; CONFIRM split issue | Revert từng phần |
| Lỗi redirect mở (open redirect) | BR-005 exact match + test @S02 | Revert |
| Cookie/session scale | Data Protection keys (Confirm #5) | Doc + env |
| Spec FE draft (Q-13) → UI đổi | UI tối thiểu, contract authorize ổn định | Chỉ đổi view |

**Consumer:** TB-043 cần `code_challenge` trong DB; gateway lát 19 sau.

**Bài học:** LL-003 (không Co-Author trailer); LL design scope I5 deferred — không tự chốt cookie store.

## 6. Gate và Definition of Done

- Gate: `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.
- Issue: tiêu chí hoàn thành tick; merge → lát 14 `done` (authorize+login; token exchange vẫn TB-043).

## 7. Câu hỏi mở và giả định

| # | Điều chưa rõ | Ảnh hưởng | Ghi chú |
|---|---|---|---|
| O-1 | Viết design phụ lục I5 / ADR OAuth trước code hay chỉ Confirm trong plan | B0, review | Design gốc deferred (Q-FE) |
| O-2 | Công nghệ trang đăng nhập: Razor Pages vs minimal HTML vs chờ `web-identity-ui.md` confirmed | B6, NFR-010 | Spec FE vẫn `draft` |
| O-3 | Lỗi authorize: HTML trên identity vs redirect `?error=` khi `redirect_uri` đã hợp lệ | B4 | OAuth vs spec “không redirect nếu URI chưa xác thực” |
| O-4 | Lưu authorize context (query gốc) giữa redirect login: encrypted cookie vs server session | B4–B6 | Bảo mật state |
| O-5 | Data Protection key ring (Docker/restart) | B5 | Scale identity |
| O-6 | `code_challenge_method`: chỉ `S256`? | B2 | PKCE |
| O-7 | `scope` trên authorize: bắt buộc và subset như CC? | B3 | FR-015 pattern |
| O-8 | Feature flag tên + default | B4 | Giống TB-041 |
| O-9 | Chia issue 042 / giữ một lát | Toàn plan | 4–5 ngày |
| O-10 | `nonce` lưu cho OIDC (TB-044) hay bỏ qua | B1 | I7 |
| O-11 | Redirect URI mặc định BDD (`teamboard`) | B8 | Seed config |

**Giả định tạm (chưa là quyết định):** Path `GET /connect/authorize`; chỉ public client cho PKCE trong BDD; TB-043 đổi mã.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-042:design-doc | O-1 design trước code | `identity-auth-i5.md` phụ lục | 1 | 2026-11-26 |
| 2 | plan-review:TB-042:login-ui | O-2 UI | Razor tối thiểu (**lát 14b**) | 1 | 2026-11-26 |
| 3 | plan-review:TB-042:split-lat | O-9 chia lát | TB-042 = 14a; login = 14b issue TBD | 1 | 2026-11-26 |
| 4 | plan-review:TB-042:authz-flag | O-8 flag | `AuthorizationCodeEnabled` default false | 1 | 2026-11-26 |
| 5 | plan-review:TB-042:authz-error | O-3 lỗi | HTML/4xx trên identity | 1 | 2026-11-26 |
| 6 | plan-review:TB-042:authz-context | O-4 context | DB authorize_request (14b dùng) | 1 | 2026-11-26 |
| 7 | plan-review:TB-042:pkce-method | O-6 | S256 only | 1 | 2026-11-26 |
| 8 | plan-review:TB-042:authz-scope | O-7 | scope bắt buộc subset | 1 | 2026-11-26 |
| 9 | plan-review:TB-042:nonce | O-10 | Lưu nonce (authorize/code path) | 1 | 2026-11-26 |
| 10 | plan-review:TB-042:bdd-redirect | O-11 | BDD `http://localhost:4200/callback` + seed | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | 14a B0–B4, O-1..O-11 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init từ `main` `138b81a` sau TB-041 merge |