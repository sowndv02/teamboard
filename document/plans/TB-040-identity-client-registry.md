---
title: Identity lát 12, quản lý client đăng ký
issue: TB-040
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-040 — Identity lát 12, quản lý client đăng ký (FR-014)

Lát 12 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-040-identity-client-registry` từ `main` `a923cb2` (sau merge PR #35 / TB-039). Đã có: refresh lưu `client_id` string (`DefaultClientId` = `teamboard`, TB-036 Confirm #2); **chưa** có bảng `client`, lookup hay seed.

## 1. Phạm vi của lát

**Làm:**
- Bảng `client` + migration EF (design mục 6.2: `client_id`, loại, băm secret, `redirect_uris`, scope).
- Entity `RegisteredClient` (hoặc `OAuthClient`) và cấu hình EF; PK `client_id`.
- **Đồng bộ từ cấu hình khi khởi động** (hoặc migration seed tối thiểu + sync): danh sách client khai báo trong config/env; **không** endpoint đăng ký/quản trị (FR-014, spec mục «Phạm vi»).
- Bí mật confidential client: chỉ lưu **băm**; plaintext lấy từ biến môi trường / user-secrets / file ngoài repo lúc seed (BR-007, FR-014).
- Dịch vụ **`IClientRegistry`** (tên có thể điều chỉnh khi code): `TryGet(client_id)`, `ValidateRedirectUri` (khớp **chính xác** BR-005), `ValidateClientSecret` (constant-time, chỉ confidential).
- Seed mặc định client **`teamboard`** (public hoặc theo Confirm) khớp `Identity:RefreshToken:DefaultClientId`; redirect URIs và scope placeholder cho I5/I6.
- **Ready (tuỳ chọn theo Confirm):** kiểm default client tồn tại sau sync.
- Tích hợp mỏng: refresh/login tiếp tục dùng `DefaultClientId` nhưng **phải** tồn tại trong registry sau seed (fail fast startup hoặc ready).
- BDD `client-registry.feature` @S01–@S05; unit test băm/redirect/secret.
- File mẫu `config/clients.example.json` (hoặc section trong `appsettings` — theo Confirm); không commit secret.

**Không làm (trong lát này):**
- `POST /connect/token` grant `client_credentials` (FR-015) → **TB-041**.
- `GET /connect/authorize`, PKCE, so khớp redirect trên HTTP (FR-012) → **TB-042** (dùng `IClientRegistry` từ lát này).
- API CRUD client; thay đổi contract login/register/refresh JSON.
- FK cứng `refresh_token.client_id` → `client` (nếu chưa chốt: giữ string như hiện tại).
- Gateway, FE, OIDC discovery.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-014 lưu client (id, loại, redirect, scope) | 6.2 `client` | B1 | @S01 | BDD/integration |
| FR-014 secret confidential chỉ băm | BR-007, 6.2 | B2 | @S02 | unit |
| FR-014 khai báo config/migration, không API | spec FR-014 | B3, B4 | @S01 | BDD startup |
| BR-005 redirect khớp chính xác | spec BR-005 | B3 | @S03 | unit + BDD |
| BR-007 không log secret | BR-007 | B2, B6 | @S05 | unit/BDD |
| NFR-009 ready (nếu áp dụng) | 6.3 health | B4 | health | BDD `health.feature` hoặc integration |
| Hồi quy I4 | – | B5 | refresh @S01 | `!~Compose` |

Kịch bản dự kiến (`document/spec/features/identity-service/client-registry.feature`):
- **@S01** Sau deploy/seed: DB có client `teamboard` (hoặc id mặc định config); `TryGet` thành công.
- **@S02** Confidential client trong config: secret đúng → verify OK; sai → fail; DB không chứa plaintext secret.
- **@S03** `ValidateRedirectUri`: URI đúng danh sách → true; URI khác một ký tự / path → false (BR-005).
- **@S04** `client_id` không tồn tại → không tìm thấy (không crash).
- **@S05** Log/structured output không chứa client secret (kiểm step hoặc test host).

## 3. Các bước

### B1. Migration + entity `client`
- `Persistence/Entities/RegisteredClient.cs` (tên entity chốt khi code).
- Cột dự kiến: `client_id` (varchar 64, PK), `client_type` (`public` | `confidential`), `secret_hash`, `secret_salt`, `secret_iterations` (PBKDF2 — Confirm #1), `redirect_uris` (jsonb), `allowed_scopes` (jsonb), `updated_at`.
- Migration `AddRegisteredClient`; `IdentityDbContext` + snapshot.
- Kiểm chứng: migration apply trong test DB; BDD `database.feature` hồi quy nếu có.

### B2. Client secret hashing + options
- `Security/Clients/ClientSecretHasher` (PBKDF2-HMAC-SHA256, pattern tương tự `PasswordHasher`, iterations riêng trong options client).
- `Identity:Clients` options: danh sách client (id, type, redirect URIs, scopes); path file tùy chọn.
- Binding `IOptions` + validator (bắt buộc có ít nhất default client; confidential phải có secret ref env).
- Kiểm chứng: unit — hash/verify; secret không xuất hiện trong exception message.

### B3. `IClientRegistry` + redirect matcher
- `Security/Clients/IClientRegistry.cs`, implementation đọc DB (cache in-memory optional sau first read).
- `RedirectUriMatcher`: khớp chuỗi **chính xác** như đã đăng ký (Confirm #3); không chuẩn hoá.
- Kiểm chứng: unit @S03, @S04.

### B4. Startup sync / seed
- `IHostedService` hoặc `ExecuteAsync` lúc boot: upsert clients từ config; secret plaintext **chỉ** từ env/user-secrets `Identity__Clients__{ClientId}__Secret` (Confirm #4).
- Đảm bảo client **`teamboard`** loại **public** (không secret, Confirm #5) luôn được upsert.
- Kiểm chứng: @S01; identity khởi động với compose/BDD; **`/health/ready` fail** nếu default client thiếu sau sync (Confirm #6).

### B5. Tích hợp với refresh default client
- `RefreshTokenService` / validator: `DefaultClientId` phải resolve được qua registry (startup check hoặc lần đầu phát refresh).
- Kiểm chứng: hồi quy `refresh-token.feature` @S01–@S06; login vẫn có refresh.

### B6. BDD + tài liệu config
- `client-registry.feature` + steps (Given/When DB hoặc API nội bộ test hook — **ưu tiên kiểm DB qua test container**, không thêm API công khai).
- `config/clients.example.json`; README hoặc comment trong example.
- Cập nhật `.gitignore` nếu cần file local secrets.
- Kiểm chứng: gate `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Unit | secret hash, redirect match, options validation | không DB |
| Integration | sync seed → query registry | Testcontainers PostgreSQL |
| BDD | @S01–@S05 | Docker identity + DB như các lát trước |

Công cụ: Reqnroll/xUnit (`config/tool-versions.yaml`).

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Sync ghi đè tay sửa DB | Chỉ upsert từ config; không API admin | Revert PR; drop table migration down (dev) |
| Secret lộ qua config repo | example only; env/user-secrets | Rotate secret sau lộ |
| Login/refresh break nếu thiếu default client | Startup fail loud + @S01 | Revert hoặc seed migration cố định |

Ảnh hưởng consumer: không đổi JSON login/refresh. TB-041/042 phụ thuộc `IClientRegistry`.

## 6. Gate và Definition of Done

- `dotnet build src/teamboard.slnx -warnaserror`, format, identity tests `!~Compose` xanh.
- Issue TB-040 tiêu chí + plan `approved` sau plan-review.
- Không secret trong commit; `clients.example.json` có placeholder.

## 7. Câu hỏi mở và giả định

Không còn câu hỏi mở sau plan-review v1.

**Giới hạn đã biết:** BDD không thêm API công khai; kiểm registry qua DB/test hook. Client credentials → TB-041.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-040:client-secret-hash | O-1 băm secret | PBKDF2-HMAC-SHA256 + salt/iterations | 1 | 2026-11-26 |
| 2 | plan-review:TB-040:uri-scope-storage | O-2 storage | `jsonb` redirect_uris + allowed_scopes | 1 | 2026-11-26 |
| 3 | plan-review:TB-040:redirect-normalization | O-3 redirect | Khớp chính xác, không chuẩn hoá | 1 | 2026-11-26 |
| 4 | plan-review:TB-040:secret-source | O-4 secret seed | Chỉ env / user-secrets | 1 | 2026-11-26 |
| 5 | plan-review:TB-040:teamboard-client-type | O-5 teamboard | Public, không secret | 1 | 2026-11-26 |
| 6 | plan-review:TB-040:ready-clients | O-6 ready | Fail ready nếu thiếu default client | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B1–B6, O-1..O-6 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-039 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#6 |