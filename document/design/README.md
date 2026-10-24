# Design

Thiết kế hệ thống cho từng tính năng/service, dựa trên spec đã xác nhận (`document/spec/`). Mỗi quyết định lớn có phương án đã cân nhắc, lý do chọn, và lý do **không** chọn các phương án khác; quyết định lớn được chốt thành ADR (`document/architecture/adr/`).

- Tạo thiết kế: skill `system-design` (`.agents/skills/system-design/`), các chế độ `design`, `compare`, `review`, `explain`, `scale`.
- Mẫu: [`_template.md`](_template.md). Tên file: `<tinh-nang>.md` (kebab-case), cùng tên với spec tương ứng.
- Quan hệ: spec (cái gì) → design (làm thế nào và vì sao) → ADR (quyết định chốt) → issue (làm theo lát).

## Danh sách

| Design | Trạng thái | Spec | ADR | Issue |
|---|---|---|---|---|
| [api-gateway](api-gateway.md) | confirmed | [api-gateway](../spec/api-gateway.md) | chưa có | TB-007 |
| [identity-auth](identity-auth.md) | draft (đã PASS vòng 2; thêm D0 chờ xác nhận; phạm vi I1–I4, I6) | [identity-auth](../spec/identity-auth.md) | 0009 đã có; ADR khác đề xuất, chưa tạo (xem mục 16 của design) | TB-004 |
