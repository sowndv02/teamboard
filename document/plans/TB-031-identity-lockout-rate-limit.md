---
title: Identity lát 3, khoá tạm và rate limit (cờ cấu hình)
issue: TB-031
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-031 — Identity lát 3, khoá tạm (email, IP) và giới hạn tốc độ

Lát 3 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-031-identity-lockout-rate-limit` từ `origin/main` `916fcc7` (sau merge PR #25 / TB-030). Đã có: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `users`, PBKDF2, `login_failures_total`, BDD `register.feature` + `login.feature`, hồi quy trong `identity-service.Tests`.

## 1. Phạm vi của lát

**Làm:**
- Migration bảng `login_attempt` (design 6.2, D5): PK (`email_norm`, `ip` text), `failures`, `locked_until`, `updated_at`.
- FR-003 / A-03: sau **5** lần sai credential **liên tiếp** theo cặp (email, IP), khoá **15 phút** — từ chối cả khi nhập đúng mật khẩu; `401` chung `INVALID_CREDENTIALS` + HTTP header **`Retry-After`** (giây còn lại đến hết khoá); hết hạn cho phép đăng nhập; đăng nhập thành công **reset** bộ đếm cặp; IP khác không ảnh hưởng. **400 validation** không tăng `failures`.
- `IClientIpResolver` (Confirm #1): mặc định `Connection.RemoteIpAddress`; khi `Identity:ForwardedHeaders:Enabled=true` và cấu hình proxy tin cậy — `UseForwardedHeaders()` (chỉ `X-Forwarded-For|Proto`, mẫu giống gateway TB-024 rút gọn).
- `LoginEndpoints`: kiểm khoá **trước** verify; khi locked vẫn dummy verify nếu cần timing; tăng failures chỉ sau 401 credential (sai PW / unknown user path như hiện tại).
- Counter `lockout_total` khi 401 do **khoá** (không phải mọi `login_failures_total`).
- NFR-008 (Q-G = A): cờ `Identity:RateLimiting:Enabled` (mặc định **true** cho chạy trực tiếp) — **một** policy `auth-public`, sliding window **10 req / 60s / IP** áp cho `POST /api/v1/auth/login` và `POST /api/v1/auth/register` (Confirm #2, #4); vượt → **429** `RATE_LIMIT_EXCEEDED` + `Retry-After` (độ dài cửa sổ, pattern TB-024); cờ **false** tắt limiter (chuẩn bị gateway lát 4).
- BDD `document/spec/features/identity-service/lockout-rate-limit.feature` @S01–@S10; steps Reqnroll; Testcontainers.
- Test: `LockoutOptions` cho phép override `MaxFailures` / `LockoutMinutes` qua cấu hình test factory (ví dụ 1 phút → tránh flaky 15 phút).

**Không làm (trong lát này):** JWT/`/token` rate limit (TB-032), FR-022, job dọn `login_attempt` 7 ngày (migration comment / issue sau), rate limit gateway (TB-024), route gateway identity (lát 19), `security_event` đầy đủ, khoá tạm trên register (chỉ NFR-008 429).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-003 khoá tạm | D5, A-03 | B2, B3 | @S01–@S05 | 401 + Retry-After header |
| BR-003 lỗi chung khi khoá | D5 | B2 | @S02, @S06 | Cùng code/title |
| IP client | D5, TB-030 chuẩn bị | B1, B2 | @S04 | Hai IP tách bộ đếm |
| NFR-008 rate limit | Q-G=A | B4 | @S07–@S09 | 429 / cờ tắt |
| Metric khoá | mục 9 | B1, B2 | @S10 | `lockout_total` |
| Gate issue | – | B1–B4 | – | build, test, format |

Kịch bản (`@TB-031 @identity-service @docker`):
- @S01 Năm lần sai liên tiếp → lần 6 (kể cả đúng mật khẩu) 401 + `Retry-After` > 0.
- @S02 Trong khoá, sai mật khẩu → 401 chung + `Retry-After`.
- @S03 Vài lần sai rồi đăng nhập đúng **trước** ngưỡng → reset; chuỗi sai mới phải đủ 5 mới khoá.
- @S04 Cùng email, hai IP khác nhau (cấu hình test / connection) — khoá IP A không chặn IP B.
- @S05 `locked_until` hết hạn → đăng nhập đúng 200.
- @S06 Khi khoá: body vẫn `INVALID_CREDENTIALS`, không chữ “lock”.
- @S07 Rate limit **bật**: request thứ 11 login/phút/IP → 429 + `Retry-After`.
- @S08 Rate limit **bật**: register vượt ngưỡng → 429.
- @S09 Rate limit **tắt** (`Enabled=false`): burst không 429.
- @S10 `lockout_total` tăng khi 401 do khoá (không khi sai PW chưa khoá).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B1. Schema, options, IP, metric
- **File:** migration `login_attempt`, `LockoutOptions`, `IdentityRateLimitOptions`, `IdentityErrorCodes.RateLimitExceeded`, `IdentityMetrics.RecordLockout`, `ClientIpResolver`, optional `ForwardedHeadersExtensions`.
- **Kiểm chứng:** build; migration trong test DB.

### B2. Lockout store + login
- **File:** `Features/Login/` hoặc `Security/Lockout/` — upsert nguyên tử (SQL thô hoặc `ExecuteSql`); sửa `LoginEndpoints`.

### B3. BDD khoá (@S01–@S06, @S10)
- **File:** `lockout-rate-limit.feature`, steps (headers `Retry-After`).

### B4. Rate limiter (@S07–@S09)
- **File:** `RateLimiting/IdentityRateLimitingExtensions.cs`, `Program.cs`, test host `UseSetting` bật/tắt.

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường |
|---|---|---|
| Integration | Lockout + rate limit | Testcontainers, WebApplicationFactory |
| Unit | Options, IP key | xUnit |

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Khoá 15 phút flaky | Override phút ngắn trong test | Revert migration |
| Rate limit per-instance | Ghi Giới hạn đã biết (D5) | Tắt cờ |

## 6. Gate và Definition of Done

- Gate: `code-change-gate.md` — `dotnet test identity-service.Tests` (`!~Compose`).
- Lát 3 `done` khi PR merge.

## 7. Giả định đã chốt

- A-03: 5 fails, 15 minutes, pair (email, IP).
- Q-G = A; NFR-008: 10 req/min/IP login+register khi cờ bật.
- P-01..P-04: xem Confirm log v1.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-031:client-ip-source | IP cho khoá/rate limit | `IClientIpResolver`; ForwardedHeaders **cờ** (default off, test RemoteIp); bật khi sau proxy | 1 | 2026-11-26 |
| 2 | plan-review:TB-031:rate-limit-policy | Một policy hay tách | **Một** policy `auth-public` / IP cho login+register (NFR-008) | 1 | 2026-11-26 |
| 3 | plan-review:TB-031:lockout-response | Retry-After ở đâu | **Chỉ HTTP header** (giây); body giữ BR-003 | 1 | 2026-11-26 |
| 4 | plan-review:TB-031:rate-limit-scope | Endpoint nào | **login + register**; `/token` ở TB-032 | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | A–H; FR-003; NFR-008; D5 | 0 | 0 | PASS |

### Fix log vòng 1

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | plan-review:TB-031:validation-failures | FIX | mục 1 | 400 không được tăng lockout | Ghi rõ trong phạm vi | FR-003 |
| 1 | plan-review:TB-031:feature-file | FIX | mục 1 | Tên file feature mơ hồ | `lockout-rate-limit.feature` | convention |
| 1 | plan-review:TB-031:lockout-metric | FIX | mục 1 | Metric nhầm với login_failures | `lockout_total` chỉ khi khoá | design 9 |
| 1 | plan-review:TB-031:test-lockout-duration | FIX | B3 | 15 phút flaky | LockoutOptions override trong test | TB-024 pattern |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau TB-030 |
| 2026-11-26 | plan-review v1 PASS; Confirm #1–#4 | plan-review TB-031 |