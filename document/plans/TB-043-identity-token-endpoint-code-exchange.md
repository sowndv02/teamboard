---
title: Identity lát 15, token endpoint đổi authorization code (PKCE)
issue: TB-043
status: draft
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth-i5.md
services: [identity-service]
---

# Plan: TB-043 — Identity lát 15 (FR-013, hoàn tất I5 redeem)

Ảnh chụp: nhánh `feature/TB-043-identity-token-endpoint-code-exchange` từ `main` `062c557` (sau merge PR #39 / TB-052). **Đã có:** `GET /connect/authorize` + login IdP (TB-052), bảng `authorization_code` (hash, PKCE challenge, `scope`, `nonce`, TTL 60s), `POST /connect/token` chỉ `client_credentials` (TB-041), `PkceValidator` (S256), `AccessTokenIssuer.IssueForUser`, registry redirect URI. **Chưa có:** `grant_type=authorization_code`, verify `code_verifier`, CAS `used_at`, BDD đổi mã.

**Giới hạn lát:** Không `id_token` / OIDC (TB-044); không scope claim policy trên resource (TB-045); không refresh token rotation mới ngoài quyết định plan-review.

## 1. Phạm vi của lát (15)

**Làm:**

- Mở rộng `POST /connect/token` (form-urlencoded) cho **`grant_type=authorization_code`** (FR-013, spec 8.1 bước 4, design D-I5-5/D-I5-6).
- Tra cứu `authorization_code` theo hash của `code` plaintext; từ chối: hết hạn, đã dùng (`used_at`), sai `client_id`, sai `redirect_uri` (khớp **chính xác** BR-005), PKCE `code_verifier` không khớp `code_challenge` (S256).
- **Public client:** không `client_secret`; **confidential client:** xác thực secret (registry) khi plan-review chốt O-3.
- Phát **access token người dùng** (`token_use=user`, `sub=user_id` từ row mã) qua `IAccessTokenIssuer`; response OAuth: `access_token`, `token_type`, `expires_in` (cùng pattern TB-041).
- Đánh dấu mã đã dùng (nguyên tử, một winner — pattern refresh CAS).
- Metric (ví dụ `authorization_code_exchanges_total` / `authorization_code_exchange_failures_total`) — chốt plan-review.
- BDD feature mới (ví dụ `authorization-code-token.feature`): happy path end-to-end authorize→token; lỗi verifier/reuse/expiry/redirect/client.
- Hồi quy `client-credentials.feature`, `authorize-pkce.feature`.

**Không làm (15):**

- `id_token`, `userinfo`, discovery (TB-044).
- Claim `scope` trên JWT user (FR-020) nếu chưa chốt — xem O-4.
- Thu hồi/introspection access token (BR-006, TB-046).
- Gateway / Angular.

## 2. Bảng truy vết

| Yêu cầu | Design / spec | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-013 đổi mã + PKCE | spec 8.1, D-I5-6 | B3–B6 | @S01 | BDD e2e + unit PKCE |
| BR-005 một lần, TTL | D-I5-5 | B4, B5 | @S03, @S04 | BDD |
| BR-005 redirect_uri exact | BR-005 | B5 | @S05 | BDD |
| FR-012 context gắn client | TB-052 row | B6 | @S01 | BDD |
| FR-014 client registry | registry | B5 | @S06, @S07 | BDD |
| BR-007 không log code/verifier | BR-007 | B6 | @S08 | BDD/log |
| NFR-008 rate limit | auth-public | B6 | (hồi quy) | optional |
| FR-015 không regress | TB-041 | B6 | client-credentials | BDD |

Kịch bản dự kiến (`authorization-code-token.feature`):

- **@S01** Sau authorize + login (reuse steps TB-052): POST token với `code`, đúng `code_verifier`, `client_id`, `redirect_uri` → 200, user JWT.
- **@S02** Sai `code_verifier` → từ chối (OAuth `invalid_grant`), mã chưa `used_at`.
- **@S03** Dùng lại cùng `code` lần hai → từ chối.
- **@S04** `code` hết hạn (TTL 60s hoặc clock test) → từ chối.
- **@S05** `redirect_uri` khác row → từ chối.
- **@S06** `client_id` khác row → từ chối.
- **@S07** Confidential client: đúng secret (nếu O-3 bật) vs thiếu/sai secret.
- **@S08** Response/problem không chứa plaintext `code`/`code_verifier` trong log test (smoke).

## 3. Các bước

### B0. Điểm xuất phát (TB-052 merged)

- **Kiểm chứng:** migration 7; authorize happy @S01; `TokenEndpoints` chỉ `client_credentials`.

### B1. PKCE verify helper (mở rộng)

- `PkceValidator.VerifyS256(codeVerifier, codeChallenge)` (so sánh challenge đã lưu).
- Unit: verifier khớp BDD challenge `E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM` + verifier mẫu RFC.
- **Kiểm chứng:** unit xanh.

### B2. Authorization code lookup + redeem service

- `AuthorizationCodeRedemptionService` (hoặc tương đương): hash presented code → load row; validate expiry, client, redirect, PKCE; **CAS** set `used_at` khi issue token (transaction).
- **Kiểm chứng:** unit/integration với DB test (hoặc BDD @S02–@S04 đỏ).

### B3. Token endpoint routing

- `TokenEndpoints`: nhánh `authorization_code` sau khi parse `grant_type`; không phá nhánh `client_credentials`.
- Feature flag (Confirm O-2): cùng `AuthorizationCodeEnabled` hoặc flag riêng.
- **Kiểm chứng:** unsupported_grant vẫn 400 khi flag tắt.

### B4. Request validation

- Form fields: `grant_type`, `code`, `code_verifier`, `client_id`, `redirect_uri`; optional `client_secret` (confidential).
- Format `code_verifier` (43–128, charset RFC 7636) — từ chối sớm `invalid_request`.
- **Kiểm chứng:** BDD @S02, @S05, @S06 đỏ đúng lý do.

### B5. Issue user token

- Thành công → `IssueForUser(user_id)`; metric success; **không** log code/verifier.
- Refresh token (Confirm O-1): phát qua `RefreshTokenService` giống login JSON hoặc chỉ access token.
- **Kiểm chứng:** BDD @S01.

### B6. BDD + hồi quy + observability

- Steps Reqnroll: POST `/connect/token` form; decode JWT như `access-token.feature`.
- Counter failure theo lý do (optional cardinality thấp).
- **Kiểm chứng:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.

### B7. — (ngoài lát)

- Refresh token rotation policy riêng cho SPA; scope claim TB-045.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | PKCE verify, code hash | xUnit |
| Integration | CAS `used_at` | Postgres |
| BDD | FR-013 end-to-end | Reqnroll + Docker |

Công cụ: Reqnroll/xUnit (`config/tool-versions.yaml`).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Race đổi mã hai lần | CAS `used_at` | Revert |
| Lộ code trong log | BR-007 review | Test @S08 |
| Phá client_credentials | Tách nhánh grant trong handler; test hồi quy | Revert |
| JWT thiếu scope khi FE cần | O-4 chốt trước code | Feature flag |

**Lesson:** LL-003 (không Co-Author); LL commit hook.

**Deploy:** expand-only (không migration mới nếu không O-4); bật flag sau deploy.

## 6. Gate và Definition of Done

- `dotnet build src/teamboard.slnx -warnaserror`
- `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`
- Issue tiêu chí tick; slice 15 → `review` khi PR merge.

## 7. Câu hỏi mở và giả định

| # | Fingerprint (draft) | Câu hỏi | Gợi ý (không quyết) |
|---|---|---|---|
| O-1 | plan-review:TB-043:refresh-on-exchange | Sau đổi mã có phát **refresh_token** như JSON login không? | Chỉ `access_token` trong 15; refresh follow-up nếu SPA cần |
| O-2 | plan-review:TB-043:token-flag | Dùng chung `Identity:Features:AuthorizationCodeEnabled` hay flag token riêng? | Cùng flag I5 |
| O-3 | plan-review:TB-043:confidential-secret | Confidential client có bắt buộc `client_secret` tại token không? | Có secret (OAuth thường) |
| O-4 | plan-review:TB-043:jwt-scope-claim | JWT user có claim `scope` từ row `authorization_code.scope` trong lát này? | Defer TB-045; chỉ `sub` + `token_use` |
| O-5 | plan-review:TB-043:oauth-errors | Map lỗi: `invalid_grant` vs `invalid_client` vs `invalid_request` | invalid_grant cho code/pkce/expiry/reuse; invalid_client secret |

**Giả định:** `code` presentation = plaintext từ redirect; hash SHA256 base64 (giống refresh hasher pattern).

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | Verdict |
|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge PR #39 |