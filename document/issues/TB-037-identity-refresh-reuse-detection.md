---
key: TB-037
title: Identity lát 9, phát hiện dùng lại refresh token
status: in-progress
phase: 1
branch: feature/TB-037-identity-refresh-reuse-detection
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-037 — Identity lát 9, phát hiện dùng lại refresh token

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 9 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 8 (TB-036 merged PR #32, `main` `1bfb081`). Ước lượng của bảng lát: 2 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-010): Phát hiện dùng lại refresh token, thu hồi chuỗi (`family_id`), metric `refresh_reuse_total`, ghi log bảo mật (D4).
- Không làm: bảng `security_event` DB (hoãn), đăng xuất (TB-038), thu hồi theo user/đổi mật khẩu (TB-039).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Gửi lại refresh token đã xoay (`used_at` set) → **401** chung và **thu hồi toàn bộ family** (token mới từ lần xoay cũng không refresh được).
- [x] Loser đồng thời sau khi một request refresh thành công → xử lý reuse (family revoke), không chỉ 401 vô hại.
- [x] Token không tồn tại / hết hạn / đã `revoked_at` (không phải reuse) → 401 **không** tăng `refresh_reuse_total`.
- [x] Metric `refresh_reuse_total`; structured log `refresh_reuse` không chứa token.
- [x] Gate identity `!~Compose` xanh; hồi quy `refresh-token.feature` @S01–@S06.

## Kế hoạch
Plan: [`document/plans/TB-037-identity-refresh-reuse-detection.md`](../plans/TB-037-identity-refresh-reuse-detection.md) (`approved`, B0–B5; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-036 merged PR #32; `plan-init` trên nhánh `feature/TB-037-identity-refresh-reuse-detection` từ `main` `1bfb081`.
- 2026-11-26: plan-review v1 PASS — Confirm #1–#4 (log only; concurrent reuse revoke; @S02+@S06; 401 chung).
- 2026-11-26: test-init — mở rộng `refresh-token.feature` @S02 (family), @S06 (metric); **2 đỏ**, **61 xanh** (`!~Compose`).
- 2026-11-26: test-review v1 PASS — 2 đỏ đúng lý do (family revoke + metric chưa có).
- 2026-11-26: coding-start — reuse detection, family revoke, `refresh_reuse_total`, log; **63/63** identity `!~Compose`.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B5; O-1..O-4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | done | – | 2026-11-26 | 2 đỏ @S02/@S06; 61 hồi quy |
| test-review | PASS | 1 | 2026-11-26 | Sẵn sàng code; 2 đỏ |
| coding-start | done | – | 2026-11-26 | 63/63 `!~Compose` |
| coding-review | pending | – | | next |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-037:security-event-store | O-1 | Structured log only | 1 | 2026-11-26 |
| 2 | plan-review:TB-037:concurrent-loser-reuse | O-2 | Loser concurrent ⇒ family revoke | 1 | 2026-11-26 |
| 3 | plan-review:TB-037:s06-vs-s02 | O-3 | @S02 + @S06 metric | 1 | 2026-11-26 |
| 4 | plan-review:TB-037:revoked-refresh-response | O-4 | 401 chung sau revoke | 1 | 2026-11-26 |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| refresh @S01–@S05 (hồi quy) | `refresh-token.feature` | | **xanh** |
| reuse family @S02 mở rộng | `refresh-token.feature` | | **xanh** |
| reuse metric @S06 | `refresh-token.feature` | | **xanh** |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `f199fe8` | Sẵn sàng code | – |

Đối chiếu kịch bản ↔ test:

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01–@S05 | `refresh-token.feature` | | xanh |
| @S02 FR-010 (family) | `refresh-token.feature` | **đỏ** | |
| @S06 FR-010 (metric) | `refresh-token.feature` | **đỏ** | |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **2 failed, 61 passed**; format verify xanh.

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:s05-concurrent-family | SKIP P2 | @S05 | Chưa assert family sau concurrent | S02/@S06 replay đủ FR-010; S05 giữ FR-009 | plan B2 |
| 1 | test:identity:s02-s06-multi-when | SKIP P2 | @S02/@S06 | Nhiều `When` | Precedent rotation/reuse flow | TB-036 |
| 1 | test:identity:reuse-log-bdd | NOTE | B4 plan | Chưa BDD log | Structured log kiểm coding-review | plan B4 |

**Giới hạn đã biết:** @S06 đỏ vì chưa có counter `refresh_reuse_total`; @S02 đỏ bước cuối (rotated token vẫn 200). Metric listener không publish instrument cho tới coding-start.

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

-