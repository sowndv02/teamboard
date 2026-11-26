---
title: Identity lát 5, bộ kiểm tra token Teamboard.Auth
issue: TB-033
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [Teamboard.Auth, board-service]
---

# Plan: TB-033 — Identity lát 5, `Teamboard.Auth` (JWT validation)

Lát 5 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-033-auth-token-validation-library` từ `origin/main` `315449c` (sau merge PR #27 / TB-032). Đã có: identity phát access JWT RS256 (ADR 0010), khoá dev, claim `iss`/`aud`/`sub`/`token_use`, metric `access_tokens_issued_total`.

## 1. Phạm vi của lát

**Làm:**
- **ADR 0011** — `Teamboard.Auth` trong `src/shared`: bọc `Microsoft.AspNetCore.Authentication.JwtBearer`, ép RS256, kiểm `iss`/`aud`/`exp`/`nbf`, claim `token_use` user; không tự viết parser JWT (D2).
- Project `src/shared/Teamboard.Auth` (+ tests): `AddTeamboardJwtAuthentication` / options `Auth:Jwt:*`.
- **JWKS:** cache 10 phút (D2); nguồn khoá (Confirm #1):
  - **Dev/CI:** `Auth:Jwt:StaticJwksFile` (từ khoá dev TB-032).
  - **Production path:** HTTP client qua `Auth:Jwt:JwksUri` (tuỳ chọn; bật mặc định khi TB-034 có `/.well-known/jwks.json`).
  - `kid` lạ: refresh JWKS tối đa 1 lần/phút; lỗi tải giữ cache cũ.
- **Contract + attack tests (NFR-006):** token do identity phát (TB-032) **pass**; từ chối: sửa payload, `alg=none`, sai `iss`/`aud`, hết hạn, `kid` lạ, HMAC tráo khoá.
- Tích hợp **board-service** (Confirm #2): `GET /api/v1/auth/me` (bảo vệ, trả `sub` từ claim) — FR-006 end-to-end local (login identity → Bearer tới board).

**Không làm (trong lát này):**
- HTTP `/.well-known/jwks.json` trên identity (TB-034); gateway JWT (lát 19); refresh token; scope policy (TB-045); file `document/contracts/` đầy đủ (ghi claim trong ADR/plan).
- Bắt buộc mọi route `board-service` đăng nhập (chỉ endpoint mẫu + test).

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản / test |
|---|---|---|---|
| FR-006 tự kiểm JWKS | D2 | B1–B3 | Contract + attack table |
| NFR-006 test tấn công | mục 11 | B3 | Unit/integration attack |
| Claim `aud`/`iss` | D2 | B2 | Valid token pass |
| BR-008 `token_use=user` | D2 | B2 | Reject machine-only misuse |
| Cache JWKS | D2 | B1 | kid refresh behavior (unit) |

Kịch bản dự kiến (`@TB-033`):
- **BDD** `auth-me.feature` @board-service: @S01 token hợp lệ → 200 + `sub`; @S02 thiếu Bearer → 401; @S03 sai `aud` → 401; @S04 hết hạn → 401.
- **xUnit** `Teamboard.Shared.Auth.Tests` (NFR-006): contract valid RS256; từ chối tampered payload, sai `aud`/`iss`, `alg=none`, hết hạn; JWKS `kid` lạ refresh.

## 3. Các bước

### B0. ADR 0011
- File: `document/architecture/adr/0011-teamboard-auth-jwt-validation.md`.

### B1. Thư viện + JWKS loader
- `Teamboard.Auth/` options, JWKS cache, static file + HTTP retriever.

### B2. JwtBearer registration
- Extension method, default policies, map `NameIdentifier` = `sub`.

### B3. Contract & attack tests
- `Teamboard.Auth.Tests` + optional `board-service` protected route + integration test.

### B4. Wire `board-service` + hồi quy
- Program.cs gọi extension; gate solution build + tests.

## 4. Chiến lược test

| Tầng | Phạm vi |
|---|---|
| Unit | JWKS cache, kid refresh throttle |
| Integration | Token identity → validator; attack vectors |
| BDD (tối thiểu) | @S01–@S04 trên board-service hoặc test host |

## 5. Rủi ro

| Rủi ro | Giảm thiểu |
|---|---|
| TB-034 chưa có JWKS HTTP | Static JWKS file + config URI sẵn; đổi default khi TB-034 merge |
| `iss` lệch gateway vs identity local | `ValidIssuer` mặc định `http://localhost:6061` (Confirm #3); đổi sang gateway khi lát 19 |

## 6. Confirm log (plan-review v1)

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-033:jwks-before-034 | JWKS trước TB-034 | Static file dev/CI + HTTP client (`JwksUri` tùy chọn) | 1 | 2026-11-26 |
| 2 | plan-review:TB-033:board-scope | Wire resource server | `board-service` `GET /api/v1/auth/me` | 1 | 2026-11-26 |
| 3 | plan-review:TB-033:issuer-validation | ValidIssuer local | `http://localhost:6061` (= `Identity:Jwt:Issuer`) | 1 | 2026-11-26 |

## Lịch sử thay đổi

| Ngày | Thay đổi |
|---|---|
| 2026-11-26 | plan-init sau merge TB-032 |
| 2026-11-26 | plan-review v1 PASS (Confirm #1–#3) |