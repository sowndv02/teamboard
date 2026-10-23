# Workflow state

Mỗi tính năng/issue đang chạy trong workflow có một state file để agent khác, sub-agent hoặc người tiếp tục và theo dõi mà không phải đoán. Giao thức: `.agents/skills/workflow/references/state-protocol.md`. Điều phối: skill `workflow`.

- Mẫu: [`_template.yaml`](_template.yaml). Tên file: `<id>.yaml`, id là `TB-NNN` (theo issue) hoặc `feature-<ten>` (bước theo tính năng).
- State file theo dõi **quy trình** (bước, vòng, người nhận việc, việc con, bằng chứng, việc tiếp theo, nhật ký). Nội dung spec/design/plan và quyết định người dùng nằm ở artifact tương ứng.
- Không chứa secret. Chỉ sửa theo giao thức (đọc trước khi ghi, nhật ký chỉ thêm).

## Đang chạy

| State | Phạm vi | Bước hiện tại | Chờ ai | Cập nhật |
|---|---|---|---|---|
| [TB-007](TB-007.yaml) | feature | plan-init (slices xong; chờ TB-001) | sondv | 2026-10-22 |
