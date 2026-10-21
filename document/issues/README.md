# Issues

Issue được quản lý bằng file Markdown trong repo. Cấu hình: `config/project.yaml` → `issueTracker`.

- **Key:** `TB-<số>` (TB = TeamBoard), ví dụ `TB-001`. Số tăng dần, không tái sử dụng.
- **File:** `document/issues/TB-001-<slug>.md`.
- **Trạng thái:** `todo` → `in-progress` → `review` → `done` (hoặc `dropped`). Ghi trong front matter của file và trong bảng dưới.
- **Branch:** `feature/TB-001-<slug>`. **Commit:** `feat(TB-001): <tóm tắt>`. **PR:** tiêu đề `TB-001: <tóm tắt>`.
- Tạo issue mới: copy `_template.md`, lấy số kế tiếp trong bảng, thêm một dòng vào bảng.
- Đổi trạng thái thì cập nhật cả file issue và bảng này trong cùng commit.

## Danh sách

| Key | Tiêu đề | Giai đoạn | Trạng thái |
|---|---|---|---|
| [TB-001](TB-001-servicedefaults-and-rename.md) | Đổi tên board-service, thêm ServiceDefaults và Jaeger | 0 | todo |
| [TB-002](TB-002-agent-rules-and-workflow.md) | Thiết lập quy tắc agent, workflow và tài liệu nền | 0 | in-progress |
