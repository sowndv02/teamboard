# ADR 0003 — Runtime: ECS Fargate thay vì EKS

Trạng thái: Đề xuất · Ngày: 2026-10-20

## Quyết định
Local: docker-compose. AWS: ECS Fargate sau ALB, một module Terraform `ecs-service` dùng lại cho mọi service.

## Lý do
Khớp lộ trình khoá (Docker → AWS → CI/CD), ít thành phần hơn K8s, chi phí thấp hơn (EKS control plane ~$73/tháng chưa tính node).

## Đánh đổi
Không học Kubernetes. Có thể thêm giai đoạn sau: chạy cùng image trên `kind`/EKS và viết ADR so sánh.
