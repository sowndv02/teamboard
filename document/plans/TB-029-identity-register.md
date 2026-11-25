---
title: Identity lát 1b, đăng ký email/mật khẩu PBKDF2
issue: TB-029
status: approved
owner: sondv
updated: 2026-11-25
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-029 — Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2

Lát 1b trong [`identity-auth-slices.md`](identity-auth-slices.md) (lát 1a = TB-021 `done`, PR #14 `e79560d` trên `main`). Ảnh chụp trạng thái: nhánh `feature/TB-029-identity-register` từ `origin/main` `0b58fdc` (sau merge TB-027/TB-049), working tree sạch. Đã có: `src/identity-service` (health, EF Core, migration nền, `IdentityDbContext` trống), `identity-service.Tests` (Reqnroll, Testcontainers cho DB), spec `features/identity-service/*` cho lát 1a; chưa có bảng `user`, `Security/Passwords`, endpoint đăng ký.

## 1. Phạm vi của lát

**Làm:**
- Bảng người dùng (email chuẩn hoá, hash mật khẩu + salt, metadata thời gian) và migration EF (D8, FR-004, BR-001).
- `Security/Passwords` (hoặc tương đương): PBKDF2-HMAC-SHA256 qua BCL (NFR-002, ADR 0006, design D8), so sánh hằng thời gian khi cần.
- Vertical slice `Features/Register/`: `POST` đăng ký (đường dẫn chốt P-01), 201 không trả hash/mật khẩu (FR-001), 409 email trùng (BR-003 ngoại lệ), 400 mật khẩu yếu/quá dài hoặc email không hợp lệ (BR-002, FR-001).
- Metric `password_hash_duration_seconds` (histogram hoặc tương đương OTel, NFR-007); benchmark ghi vào plan/issue để chốt số vòng PBKDF2 (design mục 5, D8).
- BDD `register.feature` + bước Reqnroll; integration với PostgreSQL (Testcontainers, nhóm `docker`).

**Không làm (trong lát này):** đăng nhập (TB-030), khoá tạm / rate limit identity (TB-031, NFR-008), JWT và khoá ký (TB-032+), route gateway (lát 19), đổi mật khẩu (FR-022), xác minh email, ADR đường dẫn OIDC (`/connect/*`).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-001 đăng ký email/mật khẩu | D8, slice `Register` | B3 | @S01, @S02, @S03, @S11 | Host + DB: 201, 409, 400 |
| FR-004 hash + salt, không lộ | D8 | B2, B3 | @S04, @S05, @S06 | Unit + integration: hash khác nhau; response/log không chứa mật khẩu/hash |
| BR-001 chuẩn hoá email | mục 6.2 | B1, B3 | @S07 | `A@x.com` = `a@x.com` → 409 khi trùng |
| BR-002 độ dài mật khẩu | spec A-03 | B3 | @S03, @S08 | 400 khi &lt; 12 hoặc quá dài |
| BR-003 ngoại lệ 409 đăng ký | spec | B3 | @S02 | 409 `problem+json`, không lộ stack |
| NFR-002 PBKDF2 BCL | D8 | B2 | @S04 | Unit: cùng mật khẩu, salt khác → hash khác |
| NFR-007 metric băm | mục 9 | B4 | @S09 | Meter/histogram ghi sau băm thật |
| Concurrent đăng ký | design mục 10 | B3 | @S10 | Hai request cùng email: một 201, một 409 |
| Gate issue | – | B1–B4 | – | build, test, format, không secret trong diff |

Kịch bản (tag `@TB-029 @identity-service`):
- @S01 Email và mật khẩu hợp lệ → 201, body không chứa `password`, `hash`, `salt`.
- @S02 Email đã tồn tại → 409, `problem+json`, có `code` ổn định (ví dụ `EMAIL_ALREADY_EXISTS`).
- @S03 Mật khẩu &lt; 12 ký tự → 400, `VALIDATION_FAILED` (P-03).
- @S11 Email không đúng định dạng → 400, `VALIDATION_FAILED` (cùng hợp đồng lỗi với @S03).
- @S08 Mật khẩu &gt; 128 ký tự → 400, `VALIDATION_FAILED`.
- @S07 Gửi `A@x.com` rồi `a@x.com` → lần hai 409.
- @S04 Hai tài khoản khác nhau cùng mật khẩu → hash trong DB khác nhau (đọc qua test host hoặc assert qua service nội bộ).
- @S05 Log đăng ký không chứa chuỗi mật khẩu gửi lên.
- @S06 Response 201 không chứa trường mật khẩu/hash.
- @S09 Sau đăng ký thành công, metric `password_hash_duration_seconds` được ghi (ít nhất một observation &gt; 0).
- @S10 Hai request đồng thời cùng email mới → đúng một 201 và một 409.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B1. Mô hình người dùng và migration
- **Kịch bản/test sẽ viết (đỏ trước):** test migration hoặc integration đọc schema: thực thể `user` (design 6.2), cột `email_norm` unique, cột hash+salt+tham số băm, không cột plain password (tên bảng PostgreSQL theo quy ước EF: thường là `users` hoặc `user`, ghi rõ trong migration).
- **File/module dự kiến chạm:** `Persistence/Entities/User.cs` (`EmailNorm`, hash fields), cấu hình EF, `Persistence/Migrations/*`, `IdentityDbContext`.
- **Hợp đồng thay đổi:** chưa có API công khai.
- **Migration / cấu hình:** migration `AddUsers` (expand-only).
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** `dotnet test identity-service.Tests --filter "Category=docker"` cho test DB; `Category!=docker` vẫn xanh.
- **Phụ thuộc:** không.

### B2. PBKDF2 và unit test (@S04)
- **Kịch bản/test sẽ viết (đỏ trước):** xUnit cho `IPasswordHasher`: hash/verify, salt ngẫu nhiên, thời gian hợp lý; không dùng mật khẩu trong assert log.
- **File/module dự kiến chạm:** `Security/Passwords/PasswordHasher.cs` (quy ước TB-021 #7), đăng ký DI trong `Program.cs`, test unit trong `identity-service.Tests`.
- **Hợp đồng thay đổi:** không.
- **Migration / cấu hình:** `PasswordOptions` — `Iterations` mặc định **600_000**, bind `Password:Iterations`; `MaxLength` **128** (Confirm #2, #5); `MinLength` 12 (BR-002).
- **Tài liệu cần cập nhật:** `config/dependencies.yaml` nếu không thêm gói ngoài BCL.
- **Kiểm chứng:** `dotnet test` unit xanh; không package vulnerable mới.
- **Phụ thuộc:** B1.

### B3. Endpoint Register (@S01–@S08, @S10)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/identity-service/register.feature` + steps; gọi trực tiếp cổng 6061 (chưa qua gateway).
- **File/module dự kiến chạm:** `Features/Register/*` (endpoint Minimal API theo slice), `IdentityErrorCodes.cs` (mã riêng identity, cùng kiểu `ErrorDefinition` như `GatewayErrorCodes`), `Teamboard.Shared.Http` (`ProblemFactory`, `SharedErrors.ValidationFailed`), `Program.cs` map route.
- **Hợp đồng thay đổi:** `POST /api/v1/auth/register` (ngoại lệ tạm theo design 6.3, Confirm #1). Body JSON: `email`, `password` (camelCase). 201: `{ "id": "<uuid>" }` + `Location: /api/v1/users/{id}` (Confirm #4). Lỗi: `problem+json` — `EMAIL_ALREADY_EXISTS` (409), `VALIDATION_FAILED` (400, Confirm #3).
- **Migration / cấu hình:** không thêm ngoài B1.
- **Tài liệu cần cập nhật:** phụ lục identity trong spec hoặc `document/contracts/` khi chốt P-01/P-04 (cùng lát, không đợi gateway).
- **Kiểm chứng:** `dotnet test identity-service.Tests` xanh (kết hợp docker + non-docker).
- **Phụ thuộc:** B1, B2.

### B4. Metric và benchmark (@S09, NFR-007)
- **Kịch bản/test sẽ viết (đỏ trước):** test đọc `MeterListener` hoặc pattern giống `gateway.Tests` cho `password_hash_duration_seconds` sau một lần đăng ký.
- **File/module dự kiến chạm:** đăng ký meter trong `Program.cs` / `Security` instrumentation.
- **Hợp đồng thay đổi:** không đổi API.
- **Migration / cấu hình:** không.
- **Tài liệu cần cập nhật:** ghi kết quả benchmark ngắn vào issue (Ghi chú) hoặc plan khi chốt P-02.
- **Kiểm chứng:** @S09 xanh; ghi nhận p95 trên máy dev trong plan-review.
- **Phụ thuộc:** B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú |
|---|---|---|---|
| Unit | PBKDF2, chuẩn hoá email | Không DB | Nhanh, gate chính |
| Integration (docker) | Register + DB unique | Testcontainers PostgreSQL | Nhóm `docker` |
| Host | HTTP contract, log redaction | WebApplicationFactory | Có thể dùng chung factory với docker DB cho @S01–@S07 |
| Contract/e2e | Chưa | – | Gateway lát 19 sau |

Công cụ: xUnit + Reqnroll (`config/tool-versions.yaml`); Testcontainers đã duyệt (TB-021).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| PBKDF2 quá chậm trên vCPU thấp (design mục 5) | Phá NFR p95 đăng nhập sau này | Benchmark B4; ghi số vòng đã chọn | Giảm vòng chỉ sau review (P-02) |
| Đường dẫn API đổi khi chốt `api-endpoints` | Client test phải đổi | P-01; một route duy nhất trong lát | Đổi map + test |
| Race đăng ký trùng email | Hai 201 | Unique index + xử lý `DbUpdateException` → 409 (@S10) | Migration drop index (rollback migrate) |

Ảnh hưởng consumer: không (chưa gateway route).

Bài học: LL-001 (secret), không log mật khẩu.

## 6. Gate và Definition of Done

- Gate: `code-change-gate.md` — `dotnet build -warnaserror`, `dotnet test` (cả `Category!=docker` và `docker`), `dotnet format --verify-no-changes`, `dotnet list package --vulnerable`, quét secret.
- Tiêu chí: theo issue TB-029 (sau plan-review).
- Tài liệu: `identity-auth-slices.md` (lát 1b), issue TDD log.

## 7. Giả định đã chốt

Không còn câu hỏi mở: P-01..P-05 đã chốt ở Confirm log (2026-11-25).

- **Đường API:** `POST /api/v1/auth/register` (tạm, design 6.3); `Location` của 201 trỏ `/api/v1/users/{id}` (tài nguyên số nhiều).
- **PBKDF2:** 600_000 vòng mặc định, cấu hình `Password:Iterations`; benchmark B4 ghi p95, không giảm dưới OWASP.
- **Lỗi:** `IdentityErrorCodes.EmailAlreadyExists` (409); `SharedErrors.ValidationFailed` (400).
- **Mật khẩu:** 12–128 ký tự.

**Giả định:** không bật rate limit tại identity trong lát này (NFR-008 thuộc TB-031 / gateway).

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-029:register-path | Đường API đăng ký | `POST /api/v1/auth/register` (tạm, design 6.3) | 1 | 2026-11-25 |
| 2 | plan-review:TB-029:pbkdf2-iterations | Số vòng PBKDF2 | Mặc định 600_000, bind `Password:Iterations`; benchmark B4 ghi p95 | 1 | 2026-11-25 |
| 3 | plan-review:TB-029:error-codes | Mã `problem+json` | 409 `EMAIL_ALREADY_EXISTS`; 400 `VALIDATION_FAILED` | 1 | 2026-11-25 |
| 4 | plan-review:TB-029:201-response | Phản hồi 201 | Body `{ "id" }` + `Location: /api/v1/users/{id}` | 1 | 2026-11-25 |
| 5 | plan-review:TB-029:password-max | Độ dài mật khẩu tối đa | 128 ký tự (@S08) | 1 | 2026-11-25 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-25 | Lát vừa: A–G; đối chiếu `identity-service`, `SharedErrors`, design 6.2/6.3, spec FR-001/BR-002 | 5 (P-01..P-05) | 0 (đã sửa trong plan) | NEEDS_CONFIRM |
| 2 | 2026-11-25 | Delta: 5 CONFIRM đã chốt; hợp đồng B2/B3; mục 7 | 0 | 0 | PASS |

### Fix log vòng 1

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | plan-review:TB-029:schema-names | FIX | B1, mục 2 | Tên `users`/`email_normalized` lệch design 6.2 (`user`, `email_norm`) | Khớp design; ghi rõ EF table name trong migration | design 6.2 |
| 1 | plan-review:TB-029:api-endpoints-path | FIX | B3 | Link `document/architecture/api-endpoints.md` không tồn tại | Trỏ `.agents/rules/api-endpoints.md` | glob repo |
| 1 | plan-review:TB-029:fr001-invalid-email | FIX | mục 2, B3 | Thiếu kịch bản email không hợp lệ (FR-001) | Thêm @S11 | spec FR-001 |
| 1 | plan-review:TB-029:error-codes-file | FIX | B3 | Chưa nêu `IdentityErrorCodes` như gateway | Ghi `IdentityErrorCodes.cs` + `SharedErrors` cho 400 | `GatewayErrorCodes.cs` |
| 1 | plan-review:TB-029:security-folder | FIX | B2 | Đường dẫn `Security/` chưa khớp TB-021 #7 | `Security/Passwords/PasswordHasher.cs` | plan TB-021 Confirm #7 |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-25 | Tạo bản nháp | `plan-init` lát 1b sau TB-021 done |
| 2026-11-25 | plan-review vòng 1: 5 FIX, 5 CONFIRM mở | plan-review TB-029 |
| 2026-11-25 | plan-review vòng 2 PASS; Confirm #1–#5; status `approved` | plan-review TB-029 |