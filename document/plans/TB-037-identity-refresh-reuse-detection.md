---
title: Identity lát 9, phát hiện dùng lại refresh token
issue: TB-037
status: approved
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-037 — Identity lát 9, phát hiện reuse refresh (FR-010, D4)

Lát 9 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp: nhánh `feature/TB-037-identity-refresh-reuse-detection` từ `origin/main` `1bfb081` (sau merge PR #32 / TB-036). Đã có: xoay vòng CAS (ADR 0014), `refresh_token`, metric `refresh_tokens_issued_total` / `refresh_rotations_total`; **chưa** thu hồi `family_id`, `refresh_reuse_total`, security log reuse.

## 1. Phạm vi của lát

**Làm:**
- **`RefreshTokenService.TryRotateAsync`:** khi CAS không thắng, tra `token_hash` — nếu tồn tại và `used_at IS NOT NULL` ⇒ **reuse** (D4): trong transaction `UPDATE refresh_token SET revoked_at = now() WHERE family_id = @f AND revoked_at IS NULL`; **401** chung `InvalidCredentials`; không phát token mới; tăng `refresh_reuse_total`.
- **Đồng thời (FR-009 + FR-010, Confirm #2):** loser song song sau khi một request 200 (cùng token, đã `used_at`) ⇒ reuse → **revoke family**.
- **Metric** `refresh_reuse_total` (design mục 8).
- **Ghi nhận bảo mật (Confirm #1):** structured log `event=refresh_reuse` kèm `user_id`, `family_id`, `correlationId` — **không** refresh plaintext/hash (BR-007); **không** bảng `security_event` DB trong lát này.
- BDD (Confirm #3): mở rộng `refresh-token.feature` **@S02** — sau replay token cũ, refresh bằng **token mới** từ lần xoay vẫn **401**; **@S06** metric `refresh_reuse_total`.
- Cập nhật **ADR 0014** (mục reuse/family revoke) — không tách ADR 0015 trừ khi review yêu cầu.

**Không làm (trong lát này):**
- Bảng `security_event` DB (hoãn).
- Đăng xuất / thu hồi theo user (FR-011, FR-022) → TB-038/TB-039.
- Gateway, FE.

**Hành vi đã chốt (Confirm #4):** token trong family đã `revoked_at` (kể cả token mới chưa dùng) → **401 chung**, không lộ revoked.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-010 reuse token đã xoay | D4 | B2 | @S02 mở rộng | BDD |
| FR-010 thu hồi family | D4 | B2 | @S02 | BDD |
| FR-009/010 loser concurrent reuse | D4 | B2 | @S05 (hồi quy + family khi replay) | BDD |
| NFR-007 `refresh_reuse_total` | mục 8 | B3 | @S06 | BDD/unit step |
| Ghi security reuse | D4, 6.5 | B4 | log (không assert token trong log) | review/unit |
| Hồi quy rotation | FR-009 | B5 | @S01–@S05 | `!~Compose` |

## 3. Các bước

### B0. ADR 0014 delta (reuse)
- Bổ sung mục FR-010: điều kiện reuse, family revoke SQL, metric/log.
- Kiểm chứng: `document/architecture/README.md` nếu đổi trạng thái mô tả.

### B1. test-init (đỏ)
- @S02: sau When replay stored token → Then 401; **And** refresh with the latest rotated refresh token fails with 401.
- @S06: scenario reuse kích metric (login → rotate → replay → assert counter).
- Steps stub chưa revoke → đỏ đúng lý do.

### B2. Reuse detection + family revoke
- Post-CAS-fail: lookup `used_at IS NOT NULL` → `UPDATE … revoked_at WHERE family_id`.
- Không tồn tại / hết hạn / chỉ `revoked_at` (không reuse) → 401, **không** metric reuse.
- Kiểm chứng: @S02, @S05.

### B3. Metric
- `IdentityMetrics.RecordRefreshReuse()` → `refresh_reuse_total`.

### B4. Structured log
- `ILogger` với template cố định; không log token.

### B5. Gate
- coding-start; @S01–@S06 xanh; format/build.

## 4. Chiến lược test

| Tầng | Phạm vi |
|---|---|
| BDD | @S02 mở rộng, @S06 metric, hồi quy @S01–@S05 |
| Unit | optional; ưu tiên BDD Testcontainers |

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Thu hồi family nhầm | Chỉ khi `used_at` set | Revert PR |
| Loser concurrent logout | Chấp nhận FR-010 | Confirm #2 |

## 6. Gate và DoD
- `dotnet build -warnaserror`, format, `identity-service.Tests` `!~Compose`.

## 7. Câu hỏi mở

Không còn sau plan-review v1.

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-037:security-event-store | O-1 | Structured log only | 1 | 2026-11-26 |
| 2 | plan-review:TB-037:concurrent-loser-reuse | O-2 | Concurrent loser ⇒ reuse + family revoke | 1 | 2026-11-26 |
| 3 | plan-review:TB-037:s06-vs-s02 | O-3 | @S02 family + @S06 metric | 1 | 2026-11-26 |
| 4 | plan-review:TB-037:revoked-refresh-response | O-4 | 401 chung sau revoke | 1 | 2026-11-26 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-11-26 | B0–B5, O-1..O-4 | 0 | 0 | PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-036 PR #32 |
| 2026-11-26 | plan-review v1 PASS | Confirm #1–#4 |