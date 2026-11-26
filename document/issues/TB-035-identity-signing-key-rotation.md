---
key: TB-035
title: Identity lát 7, xoay khoá ký không hỏng token còn hạn
status: in-progress
phase: 1
branch: feature/TB-035-identity-signing-key-rotation
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-035 — Identity lát 7, xoay khoá ký không hỏng token còn hạn

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 7 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 6, ADR xoay khoá. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-008): Xoay khoá ký không hỏng token còn hạn (CLI, D7)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Ba lệnh CLI D7: `keys rotate start` / `activate` / `retire` trên file khoá dev với guard thời gian.
- [ ] JWKS công bố `pending`, `active`, `retiring`; login chỉ ký bằng `active`.
- [ ] Sau activate: token mới dùng `kid` mới; token phát trước đó còn hạn vẫn verify qua JWKS.
- [ ] Sau retire hợp lệ: khoá cũ không còn trong JWKS (theo FR-008).
- [ ] Gate identity + Auth + board tests xanh (`!~Compose`, `Category!=docker`).

## Kế hoạch
Plan: [`document/plans/TB-035-identity-signing-key-rotation.md`](../plans/TB-035-identity-signing-key-rotation.md) (`approved`, B0–B6; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-034 merged PR #29; `plan-init` trên nhánh `feature/TB-035-identity-signing-key-rotation` từ `main` `21ffee1`.
- 2026-11-26: plan-review v1 PASS — log audit; reload file mỗi sign/JWKS; fake time CLI; file-only keys.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B6; O-1..O-4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-035:audit-scope | O-1 audit | Log + `IRotationAudit`, không DB | 1 | 2026-11-26 |
| 2 | plan-review:TB-035:reload-keys | O-2 reload | Reload file mỗi sign/JWKS | 1 | 2026-11-26 |
| 3 | plan-review:TB-035:time-guards-test | O-3 guard test | Fake `TimeProvider` CLI | 1 | 2026-11-26 |
| 4 | plan-review:TB-035:key-source | O-4 nguồn khoá | File-only | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 