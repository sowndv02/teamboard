---
key: TB-021
title: Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền
status: todo
phase: 1
branch: feature/TB-021-identity-service-skeleton
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: null           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-021 — Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền

## Bối cảnh
Lát 1a của `document/plans/identity-auth-slices.md` (`approved`; spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md` `confirmed` cho I1–I4, I6; issue tài liệu TB-004, PR 12). Xương sống để các lát sau (đăng ký, đăng nhập, khoá tạm, token) cắm vào. Gateway đã có lát 1 (TB-010), identity chưa được route; chạy và test trực tiếp ở local (design D0.5, Q-G).

**Phụ thuộc:** TB-001 (`src/shared/ServiceDefaults`) và TB-010 (`src/shared/Http`) đã merge. Branch tạo từ `main` ngày 2026-10-25.

## Phạm vi
- Làm:
  - Dự án `src/identity-service` (ASP.NET Core .NET 10, Vertical Slice theo design D0.4) và project test.
  - `/health/live`, `/health/ready` (ready kiểm PostgreSQL; kiểm khoá ký thêm ở lát 4) và OpenTelemetry qua `ServiceDefaults` (NFR-001).
  - PostgreSQL riêng cho identity (database + user riêng, D0.2), EF Core + Npgsql, migration nền, cấu hình qua biến môi trường, không secret trong repo.
  - Lỗi chưa xử lý trả `500 problem+json` không lộ chi tiết nội bộ (NFR-003, dùng `Teamboard.Shared.Http`).
  - Dockerfile và compose (identity-service + PostgreSQL), `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`.
- Không làm: đăng ký, băm mật khẩu, bảng `user` và các bảng nghiệp vụ (lát 1b trở đi), khoá ký và Secrets Manager (lát 4), rate limit (lát 3), route ở gateway (lát 19), `X-Correlation-Id` (chờ gateway lát 2).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `GET /health/live` trả 200; `GET /health/ready` trả 200 khi PostgreSQL chạy và lỗi (không phải 200) khi DB dừng.
- [ ] Migration nền áp dụng được trên PostgreSQL trống và tạo bảng lịch sử migration.
- [ ] Compose chạy identity-service cùng PostgreSQL, `ready` đạt trong thời gian quy định ở plan.
- [ ] Lỗi chưa xử lý trả `500 problem+json` có `code`, `traceId`, không có stack trace.
- [ ] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` xanh.
- [ ] `config/services.yaml`, compose, `PROJECT.md` khớp; không secret trong repo (có `*.example`).

## Kế hoạch
Plan chi tiết: `document/plans/TB-021-identity-service-skeleton.md` (`draft`, chờ `plan-review`).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ hàng 1a của bảng lát; nhánh từ `main` `e79560d`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | Plan `draft`, có câu hỏi mở chờ `plan-review` |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
