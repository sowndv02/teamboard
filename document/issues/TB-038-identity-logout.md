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
- [ ] Login → logout với refresh hợp lệ → **204**; sau đó refresh → **401**; không `refresh_reuse_total`.
- [ ] Logout refresh không hợp lệ / đã revoked → **401** chung; logout lặp @S03 → **401**.
- [ ] Metric `logout_total` + structured log `event=logout` (không token).
- [ ] Gate identity `!~Compose` xanh; hồi quy `refresh-token.feature` @S01–@S06.

## Kế hoạch
Plan: [`document/plans/TB-038-identity-logout.md`](../plans/TB-038-identity-logout.md) (`approved`, B0–B5; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-037 merged PR #33; `plan-init` trên nhánh `feature/TB-038-identity-logout` từ `main` `895d738`.
- 2026-11-26: plan-review v1 PASS — Confirm #1–#4 (204; 401; log+metric; @S01–@S03).
- 2026-11-26: test-init — `logout-token.feature` @S01–@S03; stub `/logout` **501**; **3 đỏ**, **63 hồi quy** (`!~Compose`).
- 2026-11-26: test-review v1 **PASS** — 3 đỏ đúng lý do (stub 501); sẵn sàng code.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B5; O-1..O-4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | done | – | 2026-11-26 | 3 đỏ @S01–@S03; 63 hồi quy |
| test-review | PASS | 1 | 2026-11-26 | Sẵn sàng code; 3 đỏ |
| coding-start | pending | – | | next |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-038:logout-success-status | O-1 | 204 No Content | 1 | 2026-11-26 |
| 2 | plan-review:TB-038:logout-invalid-token | O-2 | 401 chung | 1 | 2026-11-26 |
| 3 | plan-review:TB-038:logout-observability | O-3 | Log + `logout_total` | 1 | 2026-11-26 |
| 4 | plan-review:TB-038:logout-after-rotate-which-token | O-4 | @S01–@S03 only | 1 | 2026-11-26 |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| logout @S01–@S03 | `logout-token.feature` | **đỏ** | |
| hồi quy refresh/login | các `.feature` khác | | **xanh** |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **3 failed, 63 passed**; format verify xanh.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `69f1cfe` | Sẵn sàng code | – |

Đối chiếu kịch bản ↔ test:

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 logout + refresh block | `logout-token.feature` | **đỏ** | |
| @S02 invalid logout | `logout-token.feature` | **đỏ** | |
| @S03 second logout | `logout-token.feature` | **đỏ** | |
| hồi quy | refresh/login features | | xanh |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:logout-metric-bdd | NOTE P3 | plan B4 | Chưa BDD `logout_total` | Kiểm coding-start/review | plan B4 |
| 1 | test:identity:logout-no-reuse-metric | NOTE P3 | @S01 | Chưa assert `refresh_reuse_total` | FR-010 path; coding-review | TB-037 precedent |

**Giới hạn đã biết:** metric `logout_total` và assert không tăng reuse — coding-start (không có @S04 metric trong plan Confirm #4).

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

- 