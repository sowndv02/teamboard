---
title: Identity lát 9, phát hiện dùng lại refresh token
issue: TB-037
status: draft
owner: sondv
updated: 2026-11-26
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service]
---

# Plan: TB-037 — Identity lát 9, phát hiện reuse refresh (FR-010, D4)

Lát 9 trong [`identity-auth-slices.md`](identity-auth-slices.md). Ảnh chụp: nhánh `feature/TB-037-identity-refresh-reuse-detection` từ `origin/main` `1bfb081` (sau merge PR #32 / TB-036). Đã có: xoay vòng CAS (ADR 0014), `refresh_token`, metric `refresh_tokens_issued_total` / `refresh_rotations_total`; **chưa** thu hồi `family_id`, `refresh_reuse_total`, security event reuse.

## 1. Phạm vi của lát

**Làm:**
- **`RefreshTokenService.TryRotateAsync`:** khi CAS không thắng, tra `token_hash` — nếu tồn tại và `used_at IS NOT NULL` ⇒ **reuse** (D4): trong transaction `UPDATE refresh_token SET revoked_at = now() WHERE family_id = @f AND revoked_at IS NULL`; **401** chung; không phát token mới.
- **Đồng thời (FR-009 + FR-010):** request thua CAS với token đã `used_at` (gồm loser song song sau khi một bên đã xoay) ⇒ xử lý reuse (thu hồi family), không chỉ 401 "unknown".
- **Metric** `refresh_reuse_total` (design mục 8).
- **Ghi nhận bảo mật:** structured log (loại `refresh_reuse`) kèm `user_id`, `family_id`, `correlationId` — **không** refresh plaintext/hash (BR-007).
- BDD: mở rộng `refresh-token.feature` — @S02 bổ sung assert token mới từ lần xoay cũng 401 sau khi replay token cũ; kịch bản **@S06** (tuỳ chốt) cho reuse rõ ràng / metric step.
- Cập nhật **ADR 0014** (delta reuse) hoặc **ADR 0015** ngắn nếu cần tách quyết định FR-010.

**Không làm (trong lát này):**
- Bảng `security_event` DB (design 6.2) — hoãn tới lát có điều tra/retention (O-1).
- Đăng xuất / thu hồi theo user (FR-011, FR-022) → TB-038/TB-039.
- Gateway, FE.

## 2. Bảng truy vết

| Yêu cầu | Design | Bước | Kịch bản | Test |
|---|---|---|---|---|
| FR-010 reuse token đã xoay | D4 | B2 | @S02 mở rộng, @S06 | BDD |
| FR-010 thu hồi family | D4 | B2 | @S02 / @S06 | BDD + SQL unit optional |
| FR-009 loser sau CAS + reuse | D4 | B2 | @S05 (tuỳ chốt O-3) | BDD |
| NFR-007 `refresh_reuse_total` | mục 8 | B3 | step metric | BDD/unit |
| Ghi security reuse | D4, 6.5 | B4 | log assertion | BDD/unit |
| Hồi quy rotation | FR-009 | – | @S01–@S05 | `!~Compose` |

## 3. Các bước

### B0. ADR (reuse trong D4)
- Bổ sung ADR 0014 mục reuse/family revoke **hoặc** ADR 0015 một trang.
- Kiểm chứng: README index.

### B1. test-init (đỏ)
- Cập nhật `.feature` + steps: sau replay token cũ, refresh bằng **token mới nhất** vẫn 401.
- Stub / giữ hành vi TB-036 → đỏ đúng lý do (family chưa revoke).

### B2. Reuse detection + family revoke
- Trong `TryRotateAsync` (hoặc helper): path post-CAS-fail — `SELECT family_id, user_id WHERE token_hash = @h AND used_at IS NOT NULL`.
- `UPDATE … WHERE family_id` set `revoked_at`.
- Phân biệt: không tồn tại / hết hạn / đã `revoked_at` → 401, **không** tăng reuse metric.

### B3. Metric
- `IdentityMetrics.RecordRefreshReuse()` → `refresh_reuse_total`.

### B4. Structured log
- `ILogger` warning/info có `event=refresh_reuse`, `UserId`, `FamilyId` (guid), không token.

### B5. coding-start + gate
- Xanh @S01–@S06; hồi quy login/JWKS.

## 4. Chiến lược test

| Tầng | Phạm vi |
|---|---|
| BDD | @S02 mở rộng, @S06 reuse/family |
| Unit | optional: reuse detector với DB in-memory không đủ — ưu tiên BDD Testcontainers |

## 5. Rủi ro và rollback

| Rủi ro | Giảm thiểu | Rollback |
|---|---|---|
| Thu hồi family nhầm (client chậm) | Chỉ khi `used_at` set (spec) | Revert PR |
| Loser concurrent bị logout | Đã chấp nhận spec FR-010 | Document |

## 6. Gate và DoD
- `dotnet build -warnaserror`, format, `identity-service.Tests` `!~Compose`.

## 7. Câu hỏi mở (plan-review)

| # | Fingerprint | Câu hỏi | Khuyến nghị (tham khảo) |
|---|---|---|---|
| O-1 | plan-review:TB-037:security-event-store | Reuse ghi **structured log** only (như TB-035 `key_rotation`) hay tạo bảng `security_event` ngay? | Log only trong lát này |
| O-2 | plan-review:TB-037:concurrent-loser-reuse | @S05 loser (cùng token, sau khi một request 200) có **revoke family** không? | Có — token đã `used_at` ⇒ FR-010 |
| O-3 | plan-review:TB-037:s06-vs-s02 | Kịch bản reuse: mở rộng @S02 only hay thêm @S06 riêng + metric step? | @S02 family assert + @S06 metric |
| O-4 | plan-review:TB-037:revoked-refresh-response | Sau family revoke, refresh token mới nhất (chưa dùng) → 401 chung hay vẫn 401 không phân biệt? | 401 chung `InvalidCredentials` (BR-003 pattern) |

## Confirm log

| # | Fingerprint | Câu hỏi | Quyết định | Vòng | Ngày |
|---|---|---|---|---|---|
| | | | | | |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| | | | | | |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-11-26 | Tạo bản nháp | plan-init sau merge TB-036 PR #32 |