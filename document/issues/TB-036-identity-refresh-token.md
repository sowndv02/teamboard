---
key: TB-036
title: Identity lát 8, refresh token xoay vòng
status: in-progress
phase: 1
branch: feature/TB-036-identity-refresh-token
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-036 — Identity lát 8, refresh token xoay vòng

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 8 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 4 (access token). Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-009): Refresh token xoay vòng (ADR refresh nguyên tử, D4)
- Không làm: FR-010 reuse chuỗi (TB-037), đăng xuất (TB-038), client registry (TB-040).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Login thành công trả refresh token opaque; chỉ lưu băm trong DB.
- [ ] `POST /api/v1/auth/refresh` xoay vòng: cặp access/refresh mới; token cũ không dùng lại được.
- [ ] Refresh hết hạn hoặc không hợp lệ → 401 chung (không lộ chi tiết).
- [ ] Hai request đồng thời cùng refresh → tối đa một thành công (FR-009; reuse đầy đủ FR-010 ở TB-037).
- [ ] Gate identity + hồi quy login/JWKS xanh (`!~Compose`, `Category!=docker`).

## Kế hoạch
Plan: [`document/plans/TB-036-identity-refresh-token.md`](../plans/TB-036-identity-refresh-token.md) (`draft`, B0–B6; chờ plan-review).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-035 merged PR #30; `plan-init` trên nhánh `feature/TB-036-identity-refresh-token` từ `main` `94dd3a2`.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B6; O-1..O-5 |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

- 