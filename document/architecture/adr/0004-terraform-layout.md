# ADR 0004 — Terraform layout & state

Trạng thái: Đề xuất · Ngày: 2026-10-20

## Quyết định
- `infra/modules/*` tái sử dụng; `infra/envs/{dev,stg}` mỗi env một root module + state riêng; `infra/bootstrap` tạo S3 state bucket.
- Pin version provider; `terraform fmt/validate/plan` chạy trong CI, `apply` có bước duyệt thủ công.
- CI xác thực AWS qua GitHub OIDC; secret lưu SSM/Secrets Manager, không commit vào `.env`.

## Đánh đổi
Nhiều root module → lặp một ít code, đổi lại blast radius nhỏ và destroy từng env an toàn.
