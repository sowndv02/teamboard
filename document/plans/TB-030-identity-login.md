---
title: Identity lát 2, đăng nhập (lỗi chung không lộ email)
issue: TB-030
status: approved
owner: sondv
updated: 2026-11-25
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-030 — Identity lát 2, đăng nhập với lỗi chung không lộ email

Lát 2 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-030-identity-login` từ `origin/main` `068212c` (sau merge PR #24 / TB-029). Đã có: `POST /api/v1/auth/register`, bảng `users`, `Security/Passwords` (PBKDF2 600k), `IdentityErrorCodes`, BDD `register.feature` + hồi quy TB-021/TB-029 trong `identity-service.Tests`.

## 1. Phạm vi của lát

**Làm:**
- Vertical slice `Features/Login/`: `POST /api/v1/auth/login` (Confirm #1), email chuẩn hoá (BR-001, tái dùng `EmailAddress` từ slice Register), kiểm mật khẩu qua `IPasswordHasher.VerifyPassword` (FR-004).
- Thành công: **200** + `{ "id": "<uuid>" }` (Confirm #2) — credential đúng, **chưa** phát JWT/refresh (TB-032); TB-032 sẽ mở rộng body hoặc endpoint mà không phá xác thực credential. Không trả mật khẩu/hash/token trong body.
- Thất bại: cùng một `401 problem+json` cho email không tồn tại và mật khẩu sai (FR-002, BR-003): `code` **INVALID_CREDENTIALS**, `title` cố định *"Invalid email or password."*, không `detail` phân biệt nguyên nhân (Confirm #3).
- Độ trễ xấp xỉ nhau: khi không có user, vẫn gọi `VerifyPassword` với salt/hash cố định trong code (dummy, không lưu DB) trước khi trả 401 (design 6.4).
- `400 VALIDATION_FAILED` cho email/mật khẩu không hợp lệ (độ dài, format) — cùng kiểu với register.
- BDD `login.feature` + steps Reqnroll; integration PostgreSQL (Testcontainers, `@docker`).
- Counter `login_failures_total` khi 401 credential (design mục 9); không ghi email/mật khẩu trong log (BR-007, LL-001).
- Đọc IP client từ `HttpContext.Connection.RemoteIpAddress` (Confirm #4) để chuẩn bị TB-031; **không** ghi IP vào DB trong lát này.

**Không làm (trong lát này):** khoá tạm / bảng `login_attempt` (TB-031, FR-003), JWT và refresh (TB-032+), rate limit identity (TB-031 / gateway), route gateway (lát 19), đổi mật khẩu (FR-022), `Retry-After` khi khoá.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-002 đăng nhập email/mật khẩu | mục 6.4 (JWT ở TB-032) | B2, B3 | @S01 | 200 `{ "id" }`; không password/token trong body |
| BR-003 lỗi chung, không lộ email tồn tại | D5 | B2, B3 | @S02, @S03, @S09 | Cùng `code`, `title`, status 401 |
| BR-001 chuẩn hoá email | Register slice | B2 | @S04 | Đăng ký `a@x.com`, đăng nhập `A@x.com` → 200 |
| BR-002 độ dài mật khẩu | spec A-03 | B2 | @S05 | 400 khi độ dài không hợp lệ |
| FR-004 verify hằng thời gian | D8 | B2 | @S02 | `VerifyPassword` + unit hasher |
| BR-007 không log mật khẩu | mục 8 | B2 | @S06 | `CapturingLoggerProvider` |
| Độ trễ đồng đều (xấp xỉ) | 6.4 băm giả | B3 | @S07 | Cả hai 401 trong cùng bậc thời gian (ngưỡng lỏng) |
| Metric đăng nhập sai | mục 9 | B1, B3 | @S08 | Counter tăng sau 401 credential |
| Gate issue | – | B1–B3 | – | build, test, format |

Kịch bản (tag `@TB-030 @identity-service @docker`):
- @S01 Đăng ký + đăng nhập đúng → 200, body `{ "id" }`, không chứa `password`, `hash`, `token`, `access`.
- @S02 Sai mật khẩu user đã có → 401, `INVALID_CREDENTIALS`, `title` cố định.
- @S03 Email chưa đăng ký → 401, cùng `code` và `title` với @S02.
- @S04 Đăng ký `a@x.com`, đăng nhập `A@x.com` + đúng mật khẩu → 200.
- @S05 Mật khẩu không đạt BR-002 → 400 `VALIDATION_FAILED`.
- @S06 Email không hợp lệ → 400 `VALIDATION_FAILED`.
- @S07 Log không chứa mật khẩu gửi lên.
- @S08 Hai lần 401 (@S02 và @S03) có cùng `title` và `code` trong body (so sánh chuỗi).
- @S09 Sau 401 credential, metric `login_failures_total` tăng (MeterListener).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B1. Mã lỗi và metric nền
- **Kịch bản/test (đỏ trước):** meter đăng ký `login_failures_total`; `IdentityErrorCodes.InvalidCredentials` (401).
- **File:** `IdentityErrorCodes.cs`, `Security/IdentityMetrics.cs`, test meter (pattern TB-029 @S09).
- **Hợp đồng:** chưa có route.
- **Kiểm chứng:** build; BDD @S09 đỏ trước endpoint.

### B2. Slice Login và BDD (@S01–@S08)
- **Kịch bản:** `document/spec/features/identity-service/login.feature` + `IdentityServiceSteps` (Given đăng ký, When login).
- **File:** `Features/Login/LoginEndpoints.cs`, `Program.cs` map; tái dùng `EmailAddress`, `PasswordOptions`, `User`, `ProblemFactory`.
- **Hợp đồng:** Confirm #1–#3; đường dẫn tạm theo design 6.3 và `.agents/rules/api-endpoints.md` (ADR exception giống register).
- **Kiểm chứng:** login scenarios đỏ→xanh; `register.feature` hồi quy xanh.

### B3. Băm giả và độ trễ (@S07)
- **Kịch bản:** @S07 — thời gian @S02 và @S03 cùng bậc (ví dụ mỗi bên ≥ 50ms, không so sánh chênh lệch ms).
- **File:** `LoginEndpoints` nhánh user không tồn tại gọi `VerifyPassword` với dummy `HashedPassword` cố định.
- **Kiểm chứng:** toàn bộ TB-030 xanh; gate đầy đủ.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường | Ghi chú |
|---|---|---|---|
| Unit | Metric, error definition | Không DB | Nhanh |
| Integration (docker) | Login + users | Testcontainers PostgreSQL | Given register |
| Host | HTTP, log, metric | WebApplicationFactory + DB | `FullyQualifiedName!~Compose` trong gate chính |

Công cụ: xUnit + Reqnroll (`config/tool-versions.yaml`); pattern giống TB-029.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| 200 `{ id }` thay vì token đến TB-032 | Đổi contract lát 4 | Ghi trong issue TB-032; test contract | Revert endpoint |
| @S07 flaky | CI | Ngưỡng lỏng; không fail PR nếu &lt; 50ms (ghi Giới hạn đã biết) | Bỏ @S07 hoặc chỉ đo manual |
| Timing enumeration | BR-003 | Dummy verify B3 | — |

Ảnh hưởng consumer: không (chưa gateway). Rollback: revert PR; không migration mới.

Bài học: LL-001 (không log password).

## 6. Gate và Definition of Done

- Gate: `code-change-gate.md` — `dotnet build -warnaserror`, `dotnet test identity-service.Tests` (lọc `!~Compose` + docker khi cần), `dotnet format --verify-no-changes`, `dotnet list package --vulnerable`, quét secret.
- Tiêu chí: issue TB-030.
- Tài liệu: `identity-auth-slices.md` lát 2 `in-progress` khi coding.

## 7. Giả định đã chốt

Không còn câu hỏi mở: Confirm #1–#4 (plan-review v1, 2026-11-25).

- **Đường API:** `POST /api/v1/auth/login` (tạm, design 6.3).
- **Thành công (trước TB-032):** 200 + `{ "id": "<uuid>" }`; không JWT/refresh.
- **401 credential:** `INVALID_CREDENTIALS`; title *"Invalid email or password."*; không phân biệt email tồn tại.
- **IP:** `RemoteIpAddress` khi có; không persist trong lát này.

**Giả định:** FR-005 (JWT) và refresh thuộc TB-032; khoá tạm TB-031.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-030:login-path | Đường API đăng nhập | `POST /api/v1/auth/login` | 1 | 2026-11-25 |
| 2 | plan-review:TB-030:login-success-body | Phản hồi 200 trước JWT | `{ "id": "<uuid>" }` (TB-032 bổ sung token) | 1 | 2026-11-25 |
| 3 | plan-review:TB-030:credential-error | Mã/title 401 | `INVALID_CREDENTIALS` + title cố định BR-003 | 1 | 2026-11-25 |
| 4 | plan-review:TB-030:client-ip | IP client | `RemoteIpAddress`, không DB lát này | 1 | 2026-11-25 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-25 | A–G; spec FR-002/BR-003; design 6.4/D5; TB-029 patterns | 4 (P-01..P-04) | 3 | NEEDS_CONFIRM |
| 2 | 2026-11-25 | Delta: Confirm #1–#4 chốt; @S08–@S09; B1–B3 | 0 | 0 | PASS |

### Fix log vòng 1

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | plan-review:TB-030:s04-normalize | FIX | mục 2 | @S04 mô tả chưa rõ thứ tự đăng ký/đăng nhập | Đăng ký `a@x.com`, login `A@x.com` | BR-001 |
| 1 | plan-review:TB-030:br003-title | FIX | mục 1, 7 | 401 chưa nêu title cố định | Title BR-003 trong plan | spec BR-003 |
| 1 | plan-review:TB-030:s08-problem-shape | FIX | mục 2 | Thiếu assert cùng title giữa @S02/@S03 | Thêm @S08 so sánh body | FR-002 |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-25 | Tạo bản nháp | plan-init lát 2 sau TB-029 merge |
| 2026-11-25 | plan-review v1→v2 PASS; Confirm #1–#4; `approved` | plan-review TB-030 |