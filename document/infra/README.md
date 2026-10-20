# Infra (lựa chọn dịch vụ AWS theo service)

Mỗi service có một tài liệu chọn dịch vụ AWS, đặt **chi phí lên đầu**, rồi cân theo workload, hiệu năng/quy mô, dữ liệu/nhất quán, sẵn sàng/xử lý lỗi, bảo mật/tuân thủ, vận hành. Kết quả gồm **danh sách tài nguyên đề xuất duyệt**: đầu vào cho Terraform (`.agents/rules/terraform.md` mục 2).

- Tạo/so sánh/ước tính/review: skill `aws-service-selection` (`.agents/skills/aws-service-selection/`).
- Mẫu: [`_template.md`](_template.md). Tên file: `<service>.md` (khớp tên trong `config/services.yaml`).
- Trần chi phí: `config/project.yaml` → `quality.monthlyBudgetUsd`.
- Quan hệ: spec → design → **infra (tài liệu này)** → ADR → issue hạ tầng → Terraform.

## Danh sách

| Service | Trạng thái | Chi phí dự kiến/tháng (dev/prod) | Issue/ADR |
|---|---|---|---|
