---
key: TB-004
title: Tài liệu identity-auth (spec, design, bảng lát) và spec frontend Angular phần identity
status: review
phase: 1
branch: feature/TB-004-identity-auth
estimate: M
created: 2026-10-22
started: 2026-10-21
closed: null
---

# TB-004 — Tài liệu identity-auth và spec frontend (phần identity)

## Bối cảnh
Giai đoạn 1 cần `identity-service`. Chạy workflow: khai phá yêu cầu, review spec, system design. Giữa chừng chủ dự án chọn làm Angular FE (ADR 0009), nên có thêm spec FE riêng cho phần identity.

## Phạm vi
- Làm: spec `identity-auth` (confirmed, review vòng 3), design `identity-auth` (draft, D1–D7), bảng lát nháp `identity-auth-slices.md`, spec `web-identity-ui` (draft), hai state file workflow, cập nhật các mục lục.
- Không làm: không viết code, không scaffold `src/web`, không tạo issue cho từng lát (chờ design confirmed và duyệt bảng lát), không viết ADR (ADR nơi lưu token, thư viện UI chờ).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Spec identity-auth `confirmed` (spec-review vòng 3 PASS)
- [x] Design identity-auth bản draft có D1–D7 theo lựa chọn của chủ dự án
- [x] Spec FE `web-identity-ui` bản draft kèm 6 câu hỏi mở có owner
- [ ] `design-review` identity-auth PASS
- [ ] `spec-review` web-identity-ui PASS
- [ ] Chốt Q-01 (nơi lưu token), Q-02 (hồ sơ lấy email từ đâu), ghi `quality.monthlyBudgetUsd` vào config
- [ ] Bảng lát `approved`, rồi tạo issue cho lát đầu

## Kế hoạch
Xem `document/workflow/feature-identity-auth.yaml` và `feature-web-identity-ui.yaml`. Bước kế: `design-review`, `spec-review` cho spec FE, chốt các câu hỏi mở, duyệt bảng lát.

## Ghi chú / Nhật ký
- 2026-10-21: khai phá, review spec (3 vòng), system design; đổi phạm vi sang Angular FE (Confirm log #13–#16 của spec identity).
- 2026-10-22: tạo issue và commit tài liệu; tài liệu kiến trúc source base tách sang TB-005.

## Bài học (lessons learned)

- Không có.
