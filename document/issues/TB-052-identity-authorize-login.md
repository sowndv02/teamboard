---
key: TB-052
title: Identity lát 14b, Razor login + cookie IdP + phát code
status: in-progress
phase: 1
branch: feature/TB-052-identity-authorize-login
estimate: L             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-11-26
started: 2026-11-26
closed: null
---

# TB-052 — Identity lát 14b, Razor login + cookie IdP + phát `code`

## Bối cảnh

Lát **14b** trong [`identity-auth-slices.md`](../plans/identity-auth-slices.md), tiếp [`TB-042`](TB-042-identity-authorize-pkce.md) (14a merged `main` `8652c52`). Design: [`identity-auth-i5.md`](../design/identity-auth-i5.md). Spec: FR-019, FR-012 (happy path), FR-003 trên trang login, BR-005 phát mã, NFR-008/NFR-010.

**Phụ thuộc:** TB-042 (14a), lát 2 (login), 3 (lockout), 12 (registry). **TB-043** phụ thuộc **14 hoàn chỉnh** (14a + lát này).

## Phạm vi

- Làm (14b): `authorize_request` + luồng authorize hợp lệ → login/cookie → redirect `code` + `state`; cookie scheme IdP; Razor login tối thiểu; BDD @S01, @S04–@S06, @S08 (theo plan).
- Không làm: `POST /connect/token` authorization_code (**TB-043**); consent UI; logout huỷ cookie IdP (follow-up TB-038); OIDC; SPA client.

## Tiêu chí hoàn thành (kiểm chứng được)

- [ ] Chưa lập chi tiết: bổ sung khi `plan-review` PASS từ bảng truy vết plan.

## Kế hoạch

Plan: [`document/plans/TB-052-identity-authorize-login.md`](../plans/TB-052-identity-authorize-login.md) (`draft` — plan-init).

## Ghi chú / Nhật ký

- 2026-11-26: `plan-init` từ `main` `8652c52` sau merge TB-042 / PR #38.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B9; O-1..O-10 |
| plan-review | pending | | | |
| test-init | pending | | | |
| test-review | pending | | | |
| coding-start | pending | | | |
| coding-review | pending | | | |
| create-pr | pending | | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

-