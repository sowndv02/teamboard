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
| [TB-003](TB-003-skills-real-world-practices.md) | Đưa skill, rule và gate workflow sát thực hành dự án thực tế (làn, chia lát, Angular) | 0 | review |
| [TB-006](TB-006-agent-skills-rules.md) | Cải tiến skill, rule và template từ workflow identity-auth (D0, uỷ quyền, cổng xác nhận, thuật ngữ, câu hỏi mở) | 0 | review |
| [TB-008](TB-008-review-fix-report.md) | Báo cáo mọi FIX agent đã sửa sau mỗi vòng review (không sửa im lặng) | 0 | review |
| [TB-009](TB-009-commit-per-step.md) | Commit file của từng bước workflow ngay khi bước đạt | 0 | review |
