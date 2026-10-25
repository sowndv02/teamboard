# Issues

Issue được quản lý bằng file Markdown trong repo. Cấu hình: `config/project.yaml` → `issueTracker`.

- **Key:** `TB-<số>` (TB = TeamBoard), ví dụ `TB-001`. Số tăng dần, không tái sử dụng.
- **File:** `document/issues/TB-001-<slug>.md`.
- **Trạng thái:** `todo` → `in-progress` → `review` → `done` (hoặc `dropped`). Ghi trong front matter của file và trong bảng dưới.
- **Branch:** `feature/TB-001-<slug>`. **Commit:** `feat(TB-001): <tóm tắt>`. **PR:** tiêu đề `TB-001: <tóm tắt>`.
- Tạo issue mới: copy `_template.md`, lấy số kế tiếp trong bảng, thêm một dòng vào bảng.
- Tính năng chia lát: key của mọi lát được cấp một lần khi bảng lát được duyệt (issue khung `todo` + dòng trong bảng này); xem `.agents/skills/workflow/references/slicing.md`.
- Đổi trạng thái thì cập nhật cả file issue và bảng này trong cùng commit.

## Danh sách

| Key | Tiêu đề | Giai đoạn | Trạng thái |
|---|---|---|---|
| [TB-001](TB-001-servicedefaults-and-rename.md) | Đổi tên board-service, thêm ServiceDefaults và Jaeger | 0 | review |
| [TB-002](TB-002-agent-rules-and-workflow.md) | Thiết lập quy tắc agent, workflow và tài liệu nền | 0 | in-progress |
| [TB-003](TB-003-skills-real-world-practices.md) | Đưa skill, rule và gate workflow sát thực hành dự án thực tế (làn, chia lát, Angular) | 0 | review |
| [TB-004](TB-004-identity-auth-docs.md) | Tài liệu identity-auth (spec, design, bảng lát) và spec frontend Angular phần identity | 1 | review |
| [TB-006](TB-006-agent-skills-rules.md) | Cải tiến skill, rule và template từ workflow identity-auth (D0, uỷ quyền, cổng xác nhận, thuật ngữ, câu hỏi mở) | 0 | review |
| [TB-007](TB-007-api-gateway.md) | API gateway (YARP) mỏng: spec, design và bảng lát (issue tài liệu; code theo từng lát TB-010..) | 0 | review |
| [TB-008](TB-008-review-fix-report.md) | Báo cáo mọi FIX agent đã sửa sau mỗi vòng review (không sửa im lặng) | 0 | review |
| [TB-009](TB-009-commit-per-step.md) | Commit file của từng bước workflow ngay khi bước đạt | 0 | review |
| [TB-010](TB-010-gateway-skeleton-routing.md) | Gateway lát 1, khung YARP, route boards, health và 404 problem+json | 1 | review |
| [TB-011](TB-011-learning-hub.md) | Skill learning-hub gom kiến thức theo chủ đề vào document/learning | 0 | review |
| [TB-012](TB-012-learning-hub-realworld.md) | learning-hub bắt buộc real-world có nguồn và phân tầng khái niệm/công cụ | 0 | done |
| [TB-013](TB-013-learning-roadmap-docker.md) | Lộ trình học hai chiều (Part × L1–L8) cho learning-hub, mẫu trên Docker | 0 | review |
| [TB-014](TB-014-learning-roadmap-caching.md) | Lộ trình học Caching (khái niệm gốc) theo Part 0–7 × L1–L8 | 0 | review |
| [TB-015](TB-015-learning-roadmap-valkey.md) | Lộ trình học Redis / Valkey (công cụ) theo Part 0–7 × L1–L8 | 0 | review |
| [TB-016](TB-016-learning-roadmap-api-gateway.md) | Lộ trình học API Gateway và YARP theo Part 0–7 × L1–L8 | 0 | review |
| [TB-017](TB-017-learning-roadmap-microservice.md) | Lộ trình học Microservice (khái niệm) theo Part 0–7 × L1–L8 | 0 | review |
| [TB-018](TB-018-learning-roadmap-container.md) | Lộ trình học Container (khái niệm gốc) theo Part 0–7 × L1–L8 | 0 | review |
| [TB-019](TB-019-learning-roadmap-redis.md) | Lộ trình học Redis (phần riêng) và ghi chú so sánh Redis với Valkey | 0 | review |
| [TB-020](TB-020-learning-roadmap-identity.md) | Lộ trình học Identity (xác thực và ủy quyền) theo Part 0–7 × L1–L8 | 0 | review |
| [TB-021](TB-021-identity-service-skeleton.md) | Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền | 1 | review |
| [TB-022](TB-022-gateway-correlation-error-handling.md) | Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json | 1 | done |
| [TB-023](TB-023-gateway-downstream-errors.md) | Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng) | 1 | in-progress |
| [TB-024](TB-024-gateway-rate-limit.md) | Gateway lát 4, rate limit theo IP và 429 problem+json | 1 | todo |
| [TB-025](TB-025-gateway-body-limit.md) | Gateway lát 5, giới hạn body 1 MiB | 1 | todo |
| [TB-026](TB-026-gateway-cors.md) | Gateway lát 6, CORS theo allowlist | 1 | todo |
| [TB-027](TB-027-gateway-observability-ops.md) | Gateway lát 7, quan sát và vận hành | 1 | todo |
| [TB-028](TB-028-gateway-smoke-board-service.md) | Gateway lát 8, smoke với board-service thật (hoãn) | 1 | todo |
| [TB-029](TB-029-identity-register.md) | Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2 | 1 | todo |
| [TB-030](TB-030-identity-login.md) | Identity lát 2, đăng nhập với lỗi chung không lộ email | 1 | todo |
| [TB-031](TB-031-identity-lockout-rate-limit.md) | Identity lát 3, khoá tạm và giới hạn tốc độ đăng nhập | 1 | todo |
| [TB-032](TB-032-identity-access-token.md) | Identity lát 4, phát access token JWT khi đăng nhập | 1 | todo |
| [TB-033](TB-033-auth-token-validation-library.md) | Identity lát 5, bộ kiểm tra token dùng chung Teamboard.Auth | 1 | todo |
| [TB-034](TB-034-identity-jwks.md) | Identity lát 6, công bố JWKS | 1 | todo |
| [TB-035](TB-035-identity-signing-key-rotation.md) | Identity lát 7, xoay khoá ký không hỏng token còn hạn | 1 | todo |
| [TB-036](TB-036-identity-refresh-token.md) | Identity lát 8, refresh token xoay vòng | 1 | todo |
| [TB-037](TB-037-identity-refresh-reuse-detection.md) | Identity lát 9, phát hiện dùng lại refresh token | 1 | todo |
| [TB-038](TB-038-identity-logout.md) | Identity lát 10, đăng xuất thu hồi phiên | 1 | todo |
| [TB-039](TB-039-identity-change-password.md) | Identity lát 11, đổi mật khẩu và thu hồi refresh | 1 | todo |
| [TB-040](TB-040-identity-client-registry.md) | Identity lát 12, quản lý client đăng ký | 1 | todo |
| [TB-041](TB-041-identity-client-credentials.md) | Identity lát 13, Client Credentials và token máy | 1 | todo |
| [TB-042](TB-042-identity-authorize-pkce.md) | Identity lát 14, Authorization Code + PKCE: authorize và trang đăng nhập | 1 | todo |
| [TB-043](TB-043-identity-token-endpoint-code-exchange.md) | Identity lát 15, endpoint token đổi mã (code_verifier) | 1 | todo |
| [TB-044](TB-044-identity-oidc.md) | Identity lát 16, OIDC: id_token, userinfo, discovery | 1 | todo |
| [TB-045](TB-045-identity-scope-authorization.md) | Identity lát 17, ủy quyền theo scope | 1 | todo |
| [TB-046](TB-046-identity-revocation-introspection.md) | Identity lát 18, thu hồi token và introspection tối thiểu | 1 | todo |
