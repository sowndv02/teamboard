---
title: Identity lát 7, xoay khoá ký không hỏng token còn hạn
issue: TB-035
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service, Teamboard.Shared.Auth]
---

# Plan: TB-035 — Identity lát 7, xoay khoá ký (FR-008, D7)

Lát 7 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp trạng thái: nhánh `feature/TB-035-identity-signing-key-rotation` từ `origin/main` `21ffee1` (sau merge PR #29 / TB-034). Đã có: một khoá `active` file-based (ADR 0010), JWKS chỉ `active` (ADR 0012), `Teamboard.Auth` HTTP JWKS + cache 10 phút (ADR 0011).

## 1. Phạm vi của lát

**Làm:**
- **ADR 0013** (đề xuất): chu trình xoay D7, trạng thái `pending` / `active` / `retiring`, ba lệnh CLI, giữ `retiring` ≥ 24 giờ; nguồn khoá dev/CI vẫn **file JSON** (`Identity:SigningKeys:JsonFile`) — khớp pattern TB-032/TB-034 (Secrets Manager D3 production hoãn).
- Mở rộng **mô hình file** và `ISigningKeyStore` / `FileSigningKeyStore`: nhiều khoá RS256, đúng một `active` để ký; JWKS xuất mọi khoá **được công bố** (`pending`, `active`, `retiring`).
- **CLI** (D7, Confirm design #6): `keys rotate start` → `keys rotate activate` → (sau ≥ 10 phút cache JWKS) → `keys rotate retire` (sau ≥ 24 giờ ở `retiring`). Ghi audit tối thiểu (xem O-1).
- **FR-008:** sau `activate`, access token mới mang `kid` mới; token đã phát trước `activate` vẫn xác minh được qua JWKS (`retiring`); sau `retire` khoá cũ không còn trong JWKS.
- BDD + test tích hợp: luồng xoay trên file tạm; consumer `Auth.Tests` / JWKS multi-key.
- Cập nhật ADR 0012 (JWKS nhiều khoá); reload khoá trong process (xem O-2).

**Không làm (trong lát này):**
- Secrets Manager / `PutSecretValue` (D3 production).
- Xoay tự động theo lịch (D7 phương án B).
- Gateway route JWKS (lát 19).
- Refresh token / đăng nhập mới (hồi quy login + access token giữ nguyên hành vi).

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản / test |
|---|---|---|---|
| FR-008 xoay không hỏng token còn hạn | D7 | B2–B5 | @S01–@S06, `JwtRetiringKidTokenValidationIntegrationTests` |
| JWKS nhiều khoá khi xoay | D7, spec 8.3 | B2 | jwks @S01 mở rộng |
| CLI ba lệnh | D7 | B3 | @S02–@S04, unit CLI |
| Chờ TTL JWKS 10 phút trước activate | D7 | B3 | @S03 (thời gian giả lập) |
| Giữ retiring ≥ 24h trước retire | D7 | B3 | @S04 |
| `security_event` `key_rotation` | D7, 6.2 | B3/B6 | O-1 |
| FR-006 consumer thấy nhiều `kid` | D2 | B5 | Auth integration |
| NFR-007 / health | D3 | – | Hồi quy ready + metrics |

Kịch bản dự kiến (`document/spec/features/identity-service/signing-key-rotation.feature`):
- **@S01** Sau `rotate start`: JWKS **200** có `kid` mới trạng thái `pending` (và khoá `active` cũ nếu còn); login **chưa** dùng `kid` mới.
- **@S02** `rotate activate` (sau cửa sổ publish, xem O-3): login → JWT `kid` mới; token cũ (phát trước activate) vẫn validate qua JWKS.
- **@S03** `activate` từ chối nếu chưa đủ thời gian publish (kể từ `start`).
- **@S04** `retire` từ chối nếu `retiring` chưa ≥ 24h.
- **@S05** Hồi quy: một khoá `active` duy nhất (không xoay) — JWKS và login như TB-034.
- **@S06** Sau `retire` hợp lệ: JWKS không còn `kid` retiring; token cũ fail validation (FR-008) — Auth test cho token còn hạn sau activate.

## 3. Các bước

### B0. ADR 0013 (xoay khoá)
- File: `document/architecture/adr/0013-identity-signing-key-rotation.md`.
- Cập nhật ADR 0012: JWKS gồm `pending`/`active`/`retiring`; metric không đổi.
- Kiểm chứng: link trong `document/architecture/README.md`.

### B1. Đa khoá + reload store
- Mở rộng `SigningKeyFileEntry` (timestamp `publishedAt` / `retiringSince` cho guard CLI — định dạng trong ADR).
- `ISigningKeyStore`: `GetActiveKey()`, `GetPublishedKeysForJwks()`; **reload từ file trước mỗi sign/JWKS** (Confirm #2).
- `IdentityJwksSetBuilder`: serialize **tất cả** khoá được công bố.
- Kiểm chứng: unit test export JWKS 2 keys; hồi quy tests TB-032/TB-034.

### B2. Test-init (đỏ)
- Feature `signing-key-rotation.feature` @S01–@S05; steps gọi CLI + factory identity.
- Kiểm chứng: kịch bản **đỏ**; hồi quy `jwks.feature` / login xanh.

### B3. Lệnh CLI `keys rotate *`
- Module dự kiến: `Cli/` hoặc `Tools/SigningKeyRotation/` dùng **System.CommandLine** (thêm package — ADR 0007).
- Thao tác file JSON atomic (write temp + move); validate trạng thái máy trạng thái.
- Guards: activate sau ≥ `JwksCacheTtlMinutes` (600s); retire sau ≥ 24h + dung sai 60s (D7).
- Kiểm chứng: @S02–@S04; exit code ≠ 0 khi guard fail.

### B4. Tích hợp JWKS + login
- JWKS endpoint dùng published set; login chỉ `active`.
- Kiểm chứng: @S01, @S05.

### B5. Consumer multi-key
- `Auth.Tests`: issue token với `kid` cũ/mới qua cùng JWKS URI sau activate.
- Kiểm chứng: FR-008 end-to-end; board hồi quy.

### B6. Audit (Confirm #1)
- Structured log `key_rotation` + `IRotationAudit` (không migration DB trong lát).

## 4. Chiến lược test

| Tầng | Phạm vi | Môi trường | Ghi chú |
|---|---|---|---|
| BDD | @S01–@S05 | `WebApplicationFactory`, file khoá tạm | `TimeProvider` fake CLI + metadata file (Confirm #3) |
| CLI unit | Máy trạng thái JSON | xUnit | Không RSA thật nếu có builder fake |
| Integration | Auth multi-kid | Factory identity + Auth | Như TB-034 |

Công cụ: Reqnroll + xUnit (repo hiện tại).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Process không thấy file mới sau CLI | Activate không áp dụng | O-2 reload; test gọi `Reload()` | Revert store |
| Gỡ `retiring` quá sớm | Token hợp lệ fail | Guard 24h + test @S04 | Revert CLI |
| Lộ PEM trong JWKS | Critical | Chỉ export public; assert BDD | Revert builder |

Ảnh hưởng consumer: JWKS có thêm keys — `Teamboard.Auth` đã hỗ trợ nhiều `kid` trong set.

## 6. Gate và Definition of Done

- Gate: build `--warnaserror`, `identity-service.Tests` `!~Compose`, `Auth.Tests`, `board-service.Tests` `Category!=docker`, `dotnet format`.
- Tiêu chí issue (sau plan-review PASS):
  - Ba lệnh CLI D7 hoạt động trên file dev với guard thời gian.
  - JWKS có `pending`/`active`/`retiring` đúng chu trình.
  - Token cũ còn hạn vẫn verify sau activate; sau retire theo spec FR-008.
- ADR 0013; plan `approved`.

## 7. Câu hỏi mở và giả định

Không còn câu hỏi mở sau plan-review v1.

**Giả định giữ:** Chỉ RS256; một chu kỳ xoay tại một thời điểm (một `pending`); nguồn khoá **file JSON** (hoãn Secrets Manager).

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-035:audit-scope | O-1 audit rotation | Structured log + `IRotationAudit`, không DB | 1 | 2026-11-26 |
| 2 | plan-review:TB-035:reload-keys | O-2 reload sau CLI | Reload file trước mỗi sign/JWKS | 1 | 2026-11-26 |
| 3 | plan-review:TB-035:time-guards-test | O-3 guard 10m/24h trong test | Fake `TimeProvider` CLI + metadata | 1 | 2026-11-26 |
| 4 | plan-review:TB-035:key-source | O-4 nguồn khoá | File-only, hoãn SM | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B0–B6, O-1..O-4 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-034 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#4 |