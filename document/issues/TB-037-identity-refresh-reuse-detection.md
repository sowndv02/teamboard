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
- [ ] Gửi lại refresh token đã xoay (`used_at` set) → **401** chung và **thu hồi toàn bộ family** (token mới từ lần xoay cũng không refresh được).
- [ ] Loser đồng thời sau khi một request refresh thành công → xử lý reuse (family revoke), không chỉ 401 vô hại.
- [ ] Token không tồn tại / hết hạn / đã `revoked_at` (không phải reuse) → 401 **không** tăng `refresh_reuse_total`.
- [ ] Metric `refresh_reuse_total`; structured log `refresh_reuse` không chứa token.
- [ ] Gate identity `!~Compose` xanh; hồi quy `refresh-token.feature` @S01–@S05.

## Kế hoạch
Plan: [`document/plans/TB-037-identity-refresh-reuse-detection.md`](../plans/TB-037-identity-refresh-reuse-detection.md) (`draft`, B0–B5; chờ plan-review O-1..O-4).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-036 merged PR #32; `plan-init` trên nhánh `feature/TB-037-identity-refresh-reuse-detection` từ `main` `1bfb081`.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B5; O-1..O-4 |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| | plan-review:TB-037:security-event-store | O-1 | | | |
| | plan-review:TB-037:concurrent-loser-reuse | O-2 | | | |
| | plan-review:TB-037:s06-vs-s02 | O-3 | | | |
| | plan-review:TB-037:revoked-refresh-response | O-4 | | | |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| refresh @S01–@S05 (hồi quy) | `refresh-token.feature` | | |
| reuse / family (dự kiến) | `refresh-token.feature` @S02+ / @S06 | | |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

-