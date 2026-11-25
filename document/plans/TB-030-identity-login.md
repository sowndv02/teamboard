---
title: Identity lát 2, đăng nhập (lỗi chung không lộ email)
issue: TB-030
status: draft
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
- Vertical slice `Features/Login/`: `POST` đăng nhập (đường dẫn chốt tại plan-review), email chuẩn hoá (BR-001, tái dùng `EmailAddress`), kiểm mật khẩu qua `IPasswordHasher.VerifyPassword` (FR-004).
- Thành công: xác thực credential **trước** lát JWT (TB-032) — phản hồi tạm do người dùng chốt (Câu hỏi mở P-02); không trả mật khẩu/hash/token JWT trong body.
- Thất bại: cùng một `401 problem+json` cho email không tồn tại và mật khẩu sai (FR-002, BR-003); cùng `code` và `title`, không lộ stack.
- Độ trễ xấp xỉ nhau: khi không có user, vẫn chạy PBKDF2 trên salt/hash giả (design 6.4, D5) trước khi trả 401.
- `400 VALIDATION_FAILED` cho email/mật khẩu không hợp lệ (độ dài, format) — cùng kiểu với register.
- BDD `login.feature` + steps Reqnroll; integration PostgreSQL (Testcontainers, `@docker`).
- Metric `login_failures_total` (counter, design mục 9) khi 401 credential; không ghi email/mật khẩu trong log (BR-007, LL-001).

**Không làm (trong lát này):** khoá tạm / bảng `login_attempt` (TB-031, FR-003), JWT và refresh (TB-032+), rate limit identity (TB-031 / gateway), route gateway (lát 19), đổi mật khẩu (FR-022), `Retry-After` khi khoá.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-002 đăng nhập email/mật khẩu | mục 6.4 (chưa phát JWT ở lát này) | B2, B3 | @S01 | 200 tạm; không password trong body |
| BR-003 lỗi chung, không lộ email tồn tại | D5 | B2, B3 | @S02, @S03 | Cùng `code`, cùng status, cùng cấu trúc problem |
| BR-001 chuẩn hoá email | Register slice | B2 | @S04 | `A@x.com` đăng nhập sau đăng ký `a@x.com` |
| BR-002 độ dài mật khẩu | spec A-03 | B2 | @S05 | 400 khi độ dài không hợp lệ |
| FR-004 verify hằng thời gian | D8 | B2 | @S02 | Verify dùng hash lưu; unit đã có |
| BR-007 không log mật khẩu | mục 8 | B2 | @S06 | CapturingLogger tương tự register |
| Độ trễ đồng đều (xấp xỉ) | 6.4 băm giả | B3 | @S07 (nhẹ) | Thời gian unknown user trong biên của sai mật khẩu (không so sánh chặt ms) |
| Metric đăng nhập sai | mục 9 | B3 | @S08 | Counter tăng sau 401 credential |
| Gate issue | – | B1–B3 | – | build, test, format |

Kịch bản (tag `@TB-030 @identity-service`):
- @S01 Đã đăng ký, đăng nhập đúng → phản hồi thành công (theo P-02), body không chứa `password`, `hash`, `token`, `access`.
- @S02 Sai mật khẩu → 401, `problem+json`, `code` ổn định.
- @S03 Email chưa đăng ký → 401, cùng `code`/cấu trúc với @S02.
- @S04 Email không hợp lệ → 400 `VALIDATION_FAILED`.
- @S05 Mật khẩu không đạt BR-002 → 400.
- @S06 Log không chứa mật khẩu gửi lên.
- @S07 Thời gian phản hồi unknown user và wrong password cùng bậc (ví dụ cả hai ≥ 0.1s khi PBKDF2 600k).
- @S08 Sau 401 credential, metric `login_failures_total` tăng.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B1. Mã lỗi và metric nền
- **Kịch bản/test (đỏ trước):** unit hoặc host nhỏ đảm bảo `IdentityErrorCodes` có mã 401 credential; counter `login_failures_total` đăng ký meter.
- **File:** `IdentityErrorCodes.cs`, `Security/IdentityMetrics.cs`, `IdentitySecurityExtensions.cs`, test meter.
- **Hợp đồng:** chưa có route.
- **Kiểm chứng:** build; test metric đỏ trước khi có endpoint.

### B2. Slice Login và BDD (@S01–@S06)
- **Kịch bản:** `document/spec/features/identity-service/login.feature` + mở rộng `IdentityServiceSteps`.
- **File:** `Features/Login/*`, `Program.cs` map route; tái dùng `EmailAddress`, `PasswordOptions`, `User` entity.
- **Hợp đồng:** `POST` đăng nhập (P-01), body `email`/`password`; thành công (P-02); lỗi credential (P-03).
- **Kiểm chứng:** `dotnet test identity-service.Tests` — scenario login đỏ→xanh; register hồi quy xanh.

### B3. Băm giả và độ trễ (@S07, @S08)
- **Kịch bản:** @S07, @S08.
- **File:** `LoginEndpoints` hoặc helper timing; ghi metric khi 401.
- **Kiểm chứng:** toàn bộ TB-030 xanh; gate đầy đủ.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường | Ghi chú |
|---|---|---|---|
| Unit | Error codes, metric registration | Không DB | Nhanh |
| Integration (docker) | Login + users | Testcontainers PostgreSQL | Given register trước |
| Host | HTTP contract, log | WebApplicationFactory + DB | Lọc `!~Compose` trong gate chính |

Công cụ: xUnit + Reqnroll (`config/tool-versions.yaml`); pattern giống TB-029.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Phản hồi thành công tạm lệch FR-005 | TB-032 phải đổi contract | Chốt P-02; ghi ADR/issue | Revert endpoint |
| Timing test flaky | CI không ổn định | @S07 dùng ngưỡng thấp, không so sánh hai request | Nới hoặc skip có lý do |
| User enumeration qua timing | BR-003 mờ | Băm giả B3 | — |

Ảnh hưởng consumer: không (chưa gateway).

Bài học: LL-001 (không log password).

## 6. Gate và Definition of Done

- Gate: `code-change-gate.md` — build, `identity-service.Tests`, format, vulnerable, không secret.
- Tiêu chí: bảng trong issue TB-030 (sau plan-review).
- Tài liệu: `identity-auth-slices.md` lát 2; không ADR mới trừ khi chốt đường dẫn API.

## 7. Câu hỏi mở (chờ plan-review)

| # | Điều chưa rõ | Ảnh hưởng | Ghi chú |
|---|---|---|---|
| P-01 | Đường API đăng nhập | B2, BDD | Đề xuất: `POST /api/v1/auth/login` (song song `auth/register`) |
| P-02 | Body 200 khi đúng credential trước TB-032 | FR-002 vs I2 | Đề xuất: `200` + `{ "id": "<uuid>" }` (không JWT); TB-032 bổ sung `access_token`/`refresh_token` hoặc thay body |
| P-03 | Mã lỗi 401 credential | Contract | Đề xuất: `INVALID_CREDENTIALS` trong `IdentityErrorCodes`, cùng `detail`/`title` cố định |
| P-04 | IP client cho log/metric (chuẩn bị FR-003) | Observability | Đề xuất: đọc `HttpContext.Connection.RemoteIpAddress` (test trực tiếp host); không lưu DB trong lát này |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| | | | | | |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| | | | | | |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-25 | Tạo bản nháp | plan-init lát 2 sau TB-029 merge |