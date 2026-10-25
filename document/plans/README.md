# Plans

Kế hoạch triển khai cho từng issue (một lát giao được), tạo bởi skill `plan-init`, duyệt bởi `plan-review`. Plan nói làm gì, thứ tự nào, kiểm chứng ra sao theo TDD/BDD; quyết định kiến trúc nằm ở design/ADR.

- Mẫu: [`_template.md`](_template.md). Tên file: `TB-NNN-<slug>.md` (khớp tên issue).
- Chỉ sửa nội dung plan của issue thuộc branch hiện tại (`docs-update-specs.md`); plan của issue khác là lịch sử, chỉ đọc.
- Quy trình tổng: skill `workflow` (`.agents/skills/workflow/`).

## Danh sách

| Plan | Issue | Trạng thái |
|---|---|---|
| [api-gateway-slices](api-gateway-slices.md) | TB-007 (bảng lát; lát 1 = TB-010) | approved |
| [TB-001-servicedefaults-and-rename](TB-001-servicedefaults-and-rename.md) | TB-001 | approved |
| [TB-010-gateway-skeleton-routing](TB-010-gateway-skeleton-routing.md) | TB-010 (lát 1 gateway) | approved |
| [identity-auth-slices](identity-auth-slices.md) | TB-004 (bảng lát; lát 1a = TB-021) | approved |
| [TB-021-identity-service-skeleton](TB-021-identity-service-skeleton.md) | TB-021 (lát 1a identity) | approved |
| [TB-022-gateway-correlation-error-handling](TB-022-gateway-correlation-error-handling.md) | TB-022 (lát 2 gateway) | approved |
| [TB-023-gateway-downstream-errors](TB-023-gateway-downstream-errors.md) | TB-023 (lát 3 gateway) | draft |
