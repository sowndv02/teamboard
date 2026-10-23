---
key: TB-007
title: Dựng API gateway (YARP) mỏng làm đường vào của hệ thống
status: review
phase: 0
branch: feature/TB-007-api-gateway
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-22
started: 2026-10-22
closed: null            # ngày done/dropped
---

# TB-007 — Dựng API gateway (YARP) mỏng làm đường vào của hệ thống

## Bối cảnh
Gateway là đường vào của kiến trúc (client → gateway → service) trong `document/architecture/README.md`. Quyết định làm gateway trước các service bên trong (identity-service tạm dừng, xem TB-004) để chốt sớm routing, correlation và rate limit, rồi các service sau cắm vào gateway thật thay vì sửa lại.

Thứ tự đã chọn: TB-001 (đổi tên `board-service`, ServiceDefaults, Jaeger) → gateway mỏng (issue này) → identity → gateway kiểm JWT.

## Phạm vi
- Làm (gateway mỏng):
  - Dự án `src/gateway` dùng YARP, định tuyến REST tới `board-service`.
  - `/health/live`, `/health/ready` và OpenTelemetry qua `ServiceDefaults`.
  - Sinh/chuyển tiếp `correlationId` và `traceparent` tới service phía sau.
  - Rate limit và CORS ở gateway.
  - Thêm gateway vào compose và `config/services.yaml`.
- Không làm:
  - Kiểm JWT / xác thực (để sau khi có identity-service).
  - Thêm hoặc đổi nghiệp vụ ở các service phía sau.
  - Frontend (ADR 0008), Terraform.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `dotnet build src/teamboard.slnx -warnaserror` thành công.
- [ ] Request qua gateway tới `board-service` trả đúng response của service.
- [ ] `/health/live` và `/health/ready` của gateway trả 200.
- [ ] Mọi request ra service phía sau mang `correlationId` và `traceparent`; trace xuyên gateway → service hiện trong Jaeger.
- [ ] Vượt ngưỡng rate limit trả 429; CORS chỉ cho origin cấu hình.
- [ ] Có test cho routing, correlation và rate limit.
- [ ] `config/services.yaml`, `PROJECT.md` và compose khớp.

## Kế hoạch
Chi tiết do workflow quyết định (spec/design → plan). Điều kiện trước khi vào design:
- D0 (vì sao YARP, phần lưu trữ không áp dụng) do người dùng chọn.
- Mục `[Chọn để học]` ghi mức Gần/Xa; câu hỏi mở có lựa chọn kèm khuyến nghị.
- Phụ thuộc TB-001 (ServiceDefaults, tên `board-service`): chốt thứ tự trước khi plan.

## Ghi chú / Nhật ký
- 2026-10-22: tạo issue, checkout nhánh `feature/TB-007-api-gateway` từ `main`.
- 2026-10-23: TB-007 trở thành issue tài liệu (spec, design, bảng lát); code đi theo từng lát (TB-010..). PR của nhánh này chỉ gồm tài liệu.
- 2026-10-23: mở PR tài liệu https://github.com/sowndv02/teamboard/pull/6 (chờ review, merge do người dùng).

## Workflow

Làn: lớn (service mới, đổi đường vào hệ thống). State: `document/workflow/TB-007.yaml`. Spec: `document/spec/api-gateway.md`.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| requirements-discovery | done | – | 2026-10-22 | spec api-gateway.md, FR-001..007 |
| spec-review | PASS | 2 | 2026-10-22 | spec `confirmed`; 5 quyết định trong Confirm log |
| system-design | done | – | 2026-10-22 | design draft; người dùng đã chọn D0.1, D0.4, D1 và xác nhận phần còn lại |
| design-review | PASS | 2 | 2026-10-23 | design `confirmed`; 15 quyết định trong Confirm log |
| plan-init | done | – | 2026-10-23 | chế độ slices: `document/plans/api-gateway-slices.md` (approved), lát 1 = TB-010; plan chi tiết lập theo từng lát |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | done | – | 2026-10-23 | PR tài liệu: https://github.com/sowndv02/teamboard/pull/6 |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | spec-review:api-gateway:compliance-todo | `quality.compliance` đang TODO | GDPR | 1 | 2026-10-22 |
| 2 | spec-review:api-gateway:a01-upstream-for-test | Cách kiểm chứng FR-001 khi chưa có `/api/v1/boards` | Stub upstream trong test | 1 | 2026-10-22 |
| 3 | spec-review:api-gateway:nfr005-no-source | NFR-005 giữ hay bỏ | Giữ (A-06) | 1 | 2026-10-22 |
| 4 | spec-review:api-gateway:q04-ip-logging | Che IP và thời hạn lưu log | Ghi IP nguyên văn, hạn lưu ngắn (dev 7 ngày) | 2 | 2026-10-22 |

Chi tiết đầy đủ ở mục 19 của `document/spec/api-gateway.md`.

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

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
