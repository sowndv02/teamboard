---
key: TB-001
title: Đổi tên board-service, thêm ServiceDefaults và Jaeger
status: todo
phase: 0
branch: feature/TB-001-servicedefaults-and-rename
---

# TB-001 — Đổi tên board-service, thêm ServiceDefaults và Jaeger

## Bối cảnh
Giai đoạn 0 của lộ trình (`document/architecture/README.md` §6): chuẩn bị nền để mọi service sau dùng chung cách quan sát và health check.

## Phạm vi
- Làm:
  - Đổi `src/teamboard-api` thành `src/board-service` (project, Dockerfile, compose, solution, `config/services.yaml`).
  - Tạo `src/shared/ServiceDefaults` (OpenTelemetry trace/metric/log, `/health/live`, `/health/ready`).
  - Thêm Jaeger vào compose (file riêng cho observability).
- Không làm: thêm service mới, đổi nghiệp vụ, Terraform.

## Tiêu chí hoàn thành
- [ ] `dotnet build src/teamboard.slnx` thành công.
- [ ] `docker compose up --build` chạy được `board-service`.
- [ ] `GET /health/live` và `/health/ready` trả 200.
- [ ] Một request tới `board-service` hiện thành trace trong Jaeger.
- [ ] `config/services.yaml` và `PROJECT.md` khớp tên mới.

## Kế hoạch
1. Đổi tên và sửa mọi tham chiếu.
2. Viết `ServiceDefaults`, gắn vào `board-service`.
3. Thêm Jaeger vào compose, kiểm chứng trace.

## Ghi chú / Nhật ký

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
