# Backlog

Cập nhật: 2026-10-21. Thứ tự từ trên xuống = ưu tiên. Kích thước sơ bộ S/M/L (tương đối; chưa có số liệu để hiệu chỉnh). Mục không có issue là **ứng viên**, chưa cam kết; tạo issue khi người dùng đồng ý.

Ưu tiên: **M**ust / **S**hould / **C**ould / **W**on't (lần này). Chặn nhiều việc nhất lên đầu (đường găng).

## Now (đang làm / sẵn sàng làm)

| Mục | Issue | Ưu tiên | Cỡ | Phụ thuộc/Chặn | Vì sao |
|---|---|---|---|---|---|
| Thiết lập quy tắc agent, workflow, tài liệu nền | [TB-002](../issues/TB-002-agent-rules-and-workflow.md) (in-progress) | M | L | – | Nền cho mọi việc sau; đã có 6 commit trên branch, chờ chủ dự án review và PR |
| Xoay mật khẩu `SA_PASSWORD` (nếu là thật) | – (hành động của chủ dự án) | M | S | – | Mật khẩu nằm trong lịch sử git (LL-001) |
| Chốt các quyết định D1–D4 (xem roadmap) | – | M | S | – | Gỡ các `TODO` đang chặn nhiều việc |

## Next (làm ngay sau, cần làm rõ DoR)

| Mục | Issue | Ưu tiên | Cỡ | Phụ thuộc/Chặn | Vì sao |
|---|---|---|---|---|---|
| Đổi tên `board-service`, `ServiceDefaults`, Jaeger | [TB-001](../issues/TB-001-servicedefaults-and-rename.md) (todo) | M | M | D3 (cho test), công cụ observability | Mốc M0: có trace và health chung cho mọi service |
| Cài gate: `.editorconfig`, analyzer, `gitleaks`, quét giấy phép | (ứng viên) | S | M | – | Biến rule thành kiểm tra tự động; hoàn tất LL-001 |
| Spec + design `board-service` (qua `requirements-discovery`, `system-design`) | (ứng viên) | M | M | D1 (cho infra), D3 | Dịch vụ đầu tiên theo kiến trúc Clean |
| Spec `identity-service` I1–I2 | (ứng viên) | M | M | ADR 0006 đã có | Chuẩn bị M1 |

## Later (ý tưởng, chưa làm rõ)

| Mục | Ưu tiên | Cỡ | Ghi chú |
|---|---|---|---|
| `task-service` CQRS/ES + outbox | M | L | Chờ D2 |
| `notification-service`, `activity-service` | S | L | Chờ `aws-service-selection` |
| Terraform nền (bootstrap, network, ECR, ECS) | M | L | Chờ D1, D4, hồ sơ AWS chỉ đọc |
| Observability trên AWS (CloudWatch, ADOT) | S | M | Chờ M4 |
| Debug lab, mô phỏng tấn công sandbox | C | L | Chờ M5 |
| Bài tập tuỳ chọn: modular monolith đối chứng, Strangler Fig, gRPC, cache-aside | C | M mỗi bài | `patterns-catalog.md` mục 7 |
| Script `workflow-check` (kiểm trạng thái cổng) | C | M | Nếu thấy agent lách cổng |

## Won't (lần này, ghi rõ để chống phình phạm vi)
- Kubernetes/EKS (ADR 0003 chọn ECS).
- Multi-region, cell-based, service mesh.
- Tính năng nghiệp vụ ngoài Board/Task cơ bản.
- Dùng thư viện T3 (thương mại) khi chưa được duyệt (`dependency-policy.md`).

## Quy tắc quản lý backlog
- Chia lát dọc giao được trong một lượt (một issue một lát), có tiêu chí kiểm chứng.
- Mục chuyển từ Next sang Now khi đạt **Definition of Ready** (tiêu chí rõ, phụ thuộc đã giải, `TODO` chặn đã chốt).
- WIP tối đa theo `config/project.yaml` → `pm.wipLimit`.
- Xem lại mỗi tuần (`status`/`sprint`).
