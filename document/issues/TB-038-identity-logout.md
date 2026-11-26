---
key: TB-038
title: Identity lát 10, đăng xuất thu hồi phiên
status: in-progress
phase: 1
branch: feature/TB-038-identity-logout
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-038 — Identity lát 10, đăng xuất thu hồi phiên

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 10 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 9 (TB-037 merged PR #33, `main` `895d738`). Ước lượng của bảng lát: 1 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: **FR-011 phần I4**): `POST /api/v1/auth/logout` thu hồi chuỗi refresh (`family_id`); refresh sau logout → 401. BR-006: access JWT đến `exp`.
- Không làm: huỷ cookie phiên identity (FR-019 → I5), OIDC logout, đổi mật khẩu thu hồi refresh (TB-039), gateway, FE.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Login → logout với refresh hợp lệ → thành công (status theo O-1).
- [ ] Sau logout, `POST /refresh` với token cùng phiên → **401** chung; **không** tăng `refresh_reuse_total`.
- [ ] Logout với refresh không tồn tại / hết hạn (hành vi theo O-2).
- [ ] Structured log/metric theo O-3 (nếu chốt); không log plaintext refresh.
- [ ] Gate identity `!~Compose` xanh; hồi quy `refresh-token.feature` @S01–@S06.

## Kế hoạch
Plan: [`document/plans/TB-038-identity-logout.md`](../plans/TB-038-identity-logout.md) (`draft`, B0–B5; **plan-review** chốt O-1..O-4).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-037 merged PR #33; `plan-init` trên nhánh `feature/TB-038-identity-logout` từ `main` `895d738`.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B5; O-1..O-4 mở |
| plan-review | pending | – | | next |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| | | | | | |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| | | | |

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