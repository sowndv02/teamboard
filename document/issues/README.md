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
| [TB-001](TB-001-servicedefaults-and-rename.md) | Đổi tên board-service, thêm ServiceDefaults và Jaeger | 0 | review |
| [TB-002](TB-002-agent-rules-and-workflow.md) | Thiết lập quy tắc agent, workflow và tài liệu nền | 0 | in-progress |
| [TB-003](TB-003-skills-real-world-practices.md) | Đưa skill, rule và gate workflow sát thực hành dự án thực tế (làn, chia lát, Angular) | 0 | review |
| [TB-006](TB-006-agent-skills-rules.md) | Cải tiến skill, rule và template từ workflow identity-auth (D0, uỷ quyền, cổng xác nhận, thuật ngữ, câu hỏi mở) | 0 | review |
| [TB-007](TB-007-api-gateway.md) | API gateway (YARP) mỏng: spec, design và bảng lát (issue tài liệu; code theo từng lát TB-010..) | 0 | review |
| [TB-008](TB-008-review-fix-report.md) | Báo cáo mọi FIX agent đã sửa sau mỗi vòng review (không sửa im lặng) | 0 | review |
| [TB-009](TB-009-commit-per-step.md) | Commit file của từng bước workflow ngay khi bước đạt | 0 | review |
| [TB-010](TB-010-gateway-skeleton-routing.md) | Gateway lát 1, khung YARP, route boards, health và 404 problem+json (chờ TB-001) | 1 | todo |
| [TB-011](TB-011-learning-hub.md) | Skill learning-hub gom kiến thức theo chủ đề vào document/learning | 0 | review |
| [TB-012](TB-012-learning-hub-realworld.md) | learning-hub bắt buộc real-world có nguồn và phân tầng khái niệm/công cụ | 0 | done |
| [TB-013](TB-013-learning-roadmap-docker.md) | Lộ trình học hai chiều (Part × L1–L8) cho learning-hub, mẫu trên Docker | 0 | in-progress |
| [TB-014](TB-014-learning-roadmap-caching.md) | Lộ trình học Caching (khái niệm gốc) theo Part 0–7 × L1–L8 | 0 | in-progress |
| [TB-015](TB-015-learning-roadmap-valkey.md) | Lộ trình học Redis / Valkey (công cụ) theo Part 0–7 × L1–L8 | 0 | in-progress |
| [TB-016](TB-016-learning-roadmap-api-gateway.md) | Lộ trình học API Gateway và YARP theo Part 0–7 × L1–L8 | 0 | in-progress |
| [TB-017](TB-017-learning-roadmap-microservice.md) | Lộ trình học Microservice (khái niệm) theo Part 0–7 × L1–L8 | 0 | in-progress |
| [TB-018](TB-018-learning-roadmap-container.md) | Lộ trình học Container (khái niệm gốc) theo Part 0–7 × L1–L8 | 0 | in-progress |
| [TB-019](TB-019-learning-roadmap-redis.md) | Lộ trình học Redis (phần riêng) và ghi chú so sánh Redis với Valkey | 0 | in-progress |
