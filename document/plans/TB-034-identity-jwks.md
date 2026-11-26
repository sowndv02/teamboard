---
title: Identity lát 6, công bố JWKS
issue: TB-034
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service, Teamboard.Shared.Auth, board-service]
---

# Plan: TB-034 — Identity lát 6, công bố JWKS (FR-007)

Lát 6 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-034-identity-jwks` từ `origin/main` `e2a046f` (sau merge PR #28 / TB-033). Đã có: khoá RS256 file-based (ADR 0010), access token (TB-032), `Teamboard.Auth` với JWKS cache + file tĩnh dev, HTTP client là **fallback** khi file lỗi (ADR 0011, TB-033).

## 1. Phạm vi của lát

**Làm:**
- **ADR 0012** (đề xuất): ngoại lệ đường dẫn `/.well-known/jwks.json` khỏi quy ước `api-endpoints.md` mục 1 (design 6.3).
- **`GET /.well-known/jwks.json`** trên `identity-service`: JSON JWK Set chỉ **khoá công khai** RS256; mỗi phần tử có `kid`; nguồn từ `ISigningKeyStore` / file khoá hiện tại (chỉ trạng thái `active` trong lát này).
- Header **`Cache-Control: public, max-age=600`** (design D2).
- BDD + test xác minh hợp đồng (không lộ `privateKeyPem`, `alg`/`use`/`kty` hợp lệ).
- **Consumer (TB-033 nợ):** khi `Auth:Jwt:JwksUri` được cấu hình, `Teamboard.Auth` **tải JWKS qua HTTP làm nguồn chính**, file tĩnh làm fallback (đảo logic `JwksKeySetCache`); cấu hình mẫu board test/compose trỏ `http://localhost:6061/.well-known/jwks.json`.
- Một test tích hợp: token do identity phát **pass** khi board/Auth chỉ dùng JWKS HTTP (không đọc file signing chứa private key làm nguồn chính).

**Không làm (trong lát này):**
- Xoay khoá nhiều `kid`, `pending`/`retiring` trong JWKS (TB-035, FR-008).
- Route JWKS qua **gateway** (lát 19 / I9); discovery OIDC (FR-018).
- Secrets Manager làm nguồn khoá (chỉ file dev như TB-032).
- Đổi hành vi đăng nhập / phát token.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản / test |
|---|---|---|---|
| FR-007 JWKS công khai | D2, mục 6.3 | B2 | @S01–@S03 |
| Cache JWKS 10 phút | D2 | B2 | @S02 |
| Không lộ khoá riêng | spec FR-007 | B2, B3 | @S01 assert body |
| FR-006 dùng JWKS HTTP | D2, TB-033 | B4 | @S04 (board hoặc Auth.Tests) |
| NFR-001 health / sẵn sàng | D3 | – | Hồi quy `SigningKeyReadyTests` |

Kịch bản dự kiến (`document/spec/features/identity-service/jwks.feature`):
- **@S01** Có khoá `active` → `GET /.well-known/jwks.json` **200**; `keys` chứa đúng `kid`; có `kty`,`use`,`n`,`e`; **không** có trường PEM/private.
- **@S02** Response có `Cache-Control: public, max-age=600`.
- **@S03** Không có khoá `active` → **503** (Confirm #1, nhất quán login).
- **@S04** (integration, Confirm #3): **board BDD** auth-me + **Auth.Tests** — login identity → Bearer board `auth/me` với `JwksUri` HTTP → **200** + `sub`.

## 3. Các bước

### B0. ADR 0012 (đường dẫn JWKS)
- File: `document/architecture/adr/0012-identity-jwks-endpoint.md`.
- Kiểm chứng: link trong `document/architecture/README.md`.

### B1. Test-init (đỏ)
- Feature `jwks.feature` @S01–@S03; step definitions `JwksSteps` (hoặc mở rộng pattern `access-token.feature`).
- Test tích hợp @S04 (board `AuthMe` hoặc `Auth.Tests` với TestServer identity + board).
- Kiểm chứng: `dotnet test identity-service.Tests` — JWKS scenarios **đỏ**; hồi quy `!~Compose` xanh.

### B2. Endpoint JWKS (xanh BDD chính)
- Module dự kiến: `Features/Jwks/` (`MapJwksEndpoint`), service **`IJwksSetProvider`** (hoặc mở rộng `ISigningKeyStore`) xuất `JsonWebKeySet` từ khoá `active` (dùng `RsaSecurityKey` / export public).
- Map route **không** prefix `/api/v1`.
- Kiểm chứng: @S01–@S02 xanh.

### B3. Bảo mật & quan sát
- Assert không serialize private material; log/metric không chứa PEM (NFR-007).
- Metric **`jwks_served_total`** (Confirm #2) khi phục vụ JWKS 200.

### B4. Teamboard.Auth HTTP-first + consumer
- Đảo `JwksKeySetCache`: `JwksUri` → `HttpJwksKeySetSource` primary, `StaticSigningKeyJwksSource` fallback.
- `board-service.Tests` / `appsettings`: `Auth:Jwt:JwksUri` = identity well-known URL; có thể giữ file tĩnh chỉ fallback.
- Kiểm chứng: @S04 xanh; `Auth.Tests` + board regression xanh.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường | Ghi chú |
|---|---|---|---|
| BDD | @S01–@S03 | `WebApplicationFactory`, `test-signing-keys.json` | Không Docker |
| Integration | @S04 | Factory kép hoặc một host + URI thật | Mock HTTP tối thiểu |
| Unit | Export JWK | xUnit nếu logic tách provider | Không mock crypto |

Công cụ: Reqnroll + xUnit như `identity-service.Tests`, `board-service.Tests` (theo repo hiện tại).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Lộ private key trong JSON | Critical | Test assert body; code review; chỉ export public | Revert endpoint |
| Consumer vẫn chỉ đọc file | FR-006 không end-to-end HTTP | B4 bắt buộc trong lát | Revert Auth cache order |
| Gateway chưa proxy JWKS | Client ngoài không thấy qua 8080 | Ghi giới hạn; lát 19 | Không đổi gateway |

Ảnh hưởng consumer: `board-service` và mọi service dùng `Teamboard.Auth` có thể bỏ file JWKS tĩnh khi `JwksUri` có.

## 6. Gate và Definition of Done

- Gate: build `--warnaserror`, `identity-service.Tests` `FullyQualifiedName!~Compose`, `Auth.Tests`, `board-service.Tests` `Category!=docker`, `dotnet format`.
- Tiêu chí issue (sẽ chép vào issue sau plan-review PASS):
  - `GET /.well-known/jwks.json` đúng FR-007 + `Cache-Control`.
  - Không private key trong response.
  - Board (hoặc Auth integration) xác nhận token qua JWKS HTTP.
- ADR 0012; plan `approved`; issue workflow tới `plan-review` PASS.

## 7. Câu hỏi mở và giả định

Không còn câu hỏi mở sau plan-review v1.

**Giả định giữ:** Chỉ khoá `active` trong JWKS; consumer local `http://localhost:6061/.well-known/jwks.json`.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-034:jwks-no-key | JWKS khi không có khoá active | HTTP **503** | 1 | 2026-11-26 |
| 2 | plan-review:TB-034:jwks-metric | Metric phục vụ JWKS | `jwks_served_total` | 1 | 2026-11-26 |
| 3 | plan-review:TB-034:jwks-http-consumer | @S04 chứng minh consumer | Board BDD **và** Auth.Tests integration | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B0–B4, O-1..O-3 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-033 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#3 |