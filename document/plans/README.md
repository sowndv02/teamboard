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
| [TB-023-gateway-downstream-errors](TB-023-gateway-downstream-errors.md) | TB-023 (lát 3 gateway) | approved |
| [TB-024-gateway-rate-limit](TB-024-gateway-rate-limit.md) | TB-024 (lát 4 gateway) | approved |
| [TB-025-gateway-body-limit](TB-025-gateway-body-limit.md) | TB-025 (lát 5 gateway) | approved |
| [TB-026-gateway-cors](TB-026-gateway-cors.md) | TB-026 (lát 6 gateway) | approved |
| [TB-027-gateway-observability-ops](TB-027-gateway-observability-ops.md) | TB-027 (lát 7 gateway) | approved |
| [TB-029-identity-register](TB-029-identity-register.md) | TB-029 (lát 1b identity) | approved |
| [TB-030-identity-login](TB-030-identity-login.md) | TB-030 (lát 2 identity) | approved |
| [TB-031-identity-lockout-rate-limit](TB-031-identity-lockout-rate-limit.md) | TB-031 (lát 3 identity) | approved |
| [TB-032-identity-access-token](TB-032-identity-access-token.md) | TB-032 (lát 4 identity) | approved |
| [TB-033-auth-token-validation-library](TB-033-auth-token-validation-library.md) | TB-033 (lát 5 identity) | approved |
| [TB-034-identity-jwks](TB-034-identity-jwks.md) | TB-034 (lát 6 identity) | approved |
