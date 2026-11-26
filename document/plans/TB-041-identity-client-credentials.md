---
title: Identity lát 13, Client Credentials và token máy
issue: TB-041
status: draft
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-041 — Identity lát 13, Client Credentials (FR-015, BR-008)

Lát 13 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-041-identity-client-credentials` từ `main` `93cdd46` (sau merge PR #36 / TB-040). Đã có: bảng `client`, `IClientRegistry`, seed confidential/public, PBKDF2 secret, JWT user token (`token_use=user`, `sub=userId`), refresh/login JSON dưới `/api/v1/auth/*`. **Chưa có:** endpoint token OAuth grant `client_credentials`, JWT máy.

## 1. Phạm vi của lát

**Làm:**
- `POST` token endpoint (grant `client_credentials`) cho **confidential** client đã seed trong registry (FR-015, spec 8.1 I6).
- Xác thực `client_id` + `client_secret` qua `IClientRegistry.ValidateClientSecret`; từ chối public client và client không tồn tại (401/400 theo Confirm).
- Phát access JWT **máy** (BR-008): `token_use=machine`, `sub=client_id`, `client_id`, `scope` (chuỗi space-separated = tập con `allowed_scopes`), không claim người dùng; cùng `iss`, `aud`, TTL như user token (design D2).
- Kiểm **scope yêu cầu** ⊆ `allowed_scopes`; scope vượt quyền → từ chối (FR-015).
- Response OAuth-style: `access_token`, `token_type` Bearer, `expires_in` (giây); **không** refresh token, **không** `id`.
- BDD `client-credentials.feature` (@S01–@S06); unit issuer + scope validator; metric phát token máy (NFR-007 pattern).
- Cấu hình: seed BDD thêm confidential client (ví dụ `svc-board`) trong test; cập nhật `config/clients.example.json` mẫu confidential + scope (secret chỉ env).
- Không log secret (BR-007).

**Không làm (trong lát này):**
- Grant khác (`authorization_code`, `refresh_token` trên `/connect/token`) → TB-042/TB-043.
- Introspection, thu hồi access token (FR-021) → TB-046.
- Resource server chấp nhận `token_use=machine` trên API protected (FR-020) → TB-045; lát này có thể thêm **unit test** trong `Teamboard.Auth` nếu Confirm #5 chọn “mở rộng validator”, không bắt buộc board-service.
- Gateway route (lát 19); discovery OIDC liệt kê grants (TB-044).
- Đổi contract login/refresh JSON.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-015 phát token máy | D2, I6 | B2–B4 | @S01, @S02 | BDD + unit JWT |
| FR-015 scope vượt → từ chối | spec FR-015 | B2 | @S03 | BDD |
| FR-015 secret/client sai | FR-014 registry | B2 | @S04 | BDD |
| BR-008 phân biệt user/machine | D2 `token_use` | B1 | @S02 | unit + BDD decode |
| BR-007 không log secret | BR-007 | B4 | @S05 | BDD/log assert |
| FR-014 chỉ confidential + secret | TB-040 | B2 | @S06 | BDD public client |
| NFR-007 metric | mục 9 | B3 | @S01 | metric test |
| NFR-008 rate limit (nếu Confirm) | auth-public policy | B4 | optional | integration |

Kịch bản dự kiến (`document/spec/features/identity-service/client-credentials.feature`):
- **@S01** Confidential client + đúng secret + scope hợp lệ → 200, JWT RS256, `token_use=machine`, `sub` = `client_id`, `scope` khớp yêu cầu.
- **@S02** JWT máy không có claim kiểu user-only (không `email`); `token_use` ≠ `user`.
- **@S03** Yêu cầu scope `admin` không trong `allowed_scopes` → lỗi (không token).
- **@S04** Sai secret hoặc `client_id` → lỗi chung, body không lộ secret/hash.
- **@S05** Log không chứa `client_secret`.
- **@S06** Public client (ví dụ `teamboard`) gọi grant → từ chối (không Client Credentials cho public).

## 3. Các bước

### B1. Machine access token issuer
- **Mục tiêu:** `IAccessTokenIssuer.IssueForClient(string clientId, IReadOnlyList<string> scopes)`.
- **Claim:** `sub` = `client_id`; `client_id`; `scope` = join scopes; `token_use` = `machine`; `jti`, `iat`, `nbf`, `exp`, `iss`, `aud` như user.
- **File:** `Security/Jwt/AccessTokenIssuer.cs`, `IAccessTokenIssuer.cs`, unit `AccessTokenIssuerTests` (machine claims).
- **Kiểm chứng:** unit xanh trước BDD.

### B2. Client credentials handler
- **Mục tiêu:** parse grant; validate client confidential + secret; resolve scope (theo Confirm #3).
- **File:** `Features/Token/ClientCredentialsTokenService.cs` (hoặc slice tương đương), `ScopeValidator` (subset check).
- **Kiểm chứng:** unit scope subset; integration đỏ → xanh @S03–@S06.

### B3. Token endpoint
- **Mục tiêu:** map endpoint (path theo Confirm #1), body theo Confirm #2, map lỗi theo Confirm #6.
- **File:** `Features/Token/TokenEndpoints.cs`, `Program.cs`; rate limit policy `AuthPublic` nếu Confirm #7.
- **Kiểm chứng:** @S01, @S04; OpenAPI nội bộ optional.

### B4. BDD + ví dụ config + metric
- **Mục tiêu:** feature file, steps (form body), `clients.example.json` thêm confidential mẫu; counter `machine_access_tokens_issued_total` (tên chốt khi code).
- **File:** `client-credentials.feature`, `IdentityServiceSteps`, `IdentityMetrics`.
- **Kiểm chứng:** `dotnet test identity-service.Tests --filter "FullyQualifiedName!~Compose"` xanh; @S05.

### B5. (Tuỳ chọn Confirm #5) `Teamboard.Auth`
- Nếu chọn mở rộng trong lát: thêm API validate `token_use=machine` (hoặc overload) + test shared; **không** đổi hành vi user API board trong lát này.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | Issuer machine, scope validator | xUnit |
| Integration | Token endpoint + registry seed | WebApplicationFactory + Postgres |
| BDD | FR-015, BR-008 | Reqnroll, tag `@TB-041` |

BDD bật registry (`Identity:Clients:Enabled`) và seed confidential qua config test (pattern TB-040).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Endpoint OAuth lệch quy ước `/api/v1` | ADR/design 6.3 ngoại lệ | Revert route |
| Machine token không dùng được ở RS | Document giới hạn; TB-045 | Revert |
| Lộ secret trong log | BR-007 test @S05 | Revert |

**Consumer:** service gọi CC cần confidential client trong seed + biết URL token; chưa có consumer production trong repo.

**Bài học:** LL-003 (không Co-Author trailer); commit English + filter-branch nếu cần.

## 6. Gate và Definition of Done

- Gate: build `-warnaserror`, `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.
- Issue: tiêu chí hoàn thành đã tick; PR merge → lát 13 `done`.

## 7. Câu hỏi mở và giả định

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| O-1 | Đường dẫn endpoint: `/connect/token` (design 6.3) hay tài nguyên `/api/v1/...` | B3, gateway sau | User | |
| O-2 | Body `application/x-www-form-urlencoded` (RFC 6749) hay JSON như `/api/v1/auth/login` | B3, BDD steps | User | |
| O-3 | `scope` thiếu/rỗng: từ chối hay cấp toàn bộ `allowed_scopes` | B2, @S01 | User | |
| O-4 | Feature flag riêng `ClientCredentialsEnabled` hay luôn bật khi `Clients:Enabled` | B3, deploy | User | |
| O-5 | Cập nhật `Teamboard.Auth` chấp nhận `machine` trong lát này hay defer TB-045 | B5, shared tests | User | |
| O-6 | Lỗi OAuth `{error,error_description}` hay `problem+json` | B3, contract | User | |
| O-7 | Rate limit `AuthPublic` như login/refresh? | B4 | User | |

**Giả định tạm (chưa chốt):** chỉ grant `client_credentials`; `client_secret` gửi trong body (không Basic auth) trừ khi plan-review chọn khác.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init từ `main` `93cdd46` sau TB-040 |