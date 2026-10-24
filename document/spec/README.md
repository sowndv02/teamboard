# Spec

Đặc tả hành vi hiện hành của hệ thống (nguồn sự thật cho hành vi; issue và ADR là lịch sử, xem `.agents/rules/docs-update-specs.md`).

- Tạo spec mới từ yêu cầu mơ hồ: skill `requirements-discovery` (`.agents/skills/requirements-discovery/`).
- Mẫu: [`_template.md`](_template.md). Tên file: `<tinh-nang>.md` (kebab-case).
- Kịch bản BDD đi kèm: `document/spec/features/<service>/*.feature`.

## Danh sách

| Spec | Trạng thái | Service | Issue |
|---|---|---|---|
| [api-gateway](api-gateway.md) | confirmed | gateway, board-service | TB-007 |
| [identity-auth](identity-auth.md) | confirmed (review vòng 3; I5 phụ thuộc spec FE) | identity-service | – |
| [web-identity-ui](web-identity-ui.md) | draft | web, identity-service | – |
