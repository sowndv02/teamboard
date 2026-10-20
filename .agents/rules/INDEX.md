# Rules index (tool-agnostic)

Mọi agent lấy rule từ `.agents/` và tài liệu dự án, không phụ thuộc công cụ cụ thể.
Nếu một công cụ có bản mirror/trigger riêng (ví dụ `.cursor/rules`), khi xung đột thì **`.agents/` thắng**.

## Thứ tự đọc

1. `AGENTS.md` (core, ngắn), luôn đọc.
2. Chỉ các file trong bảng dưới có cột **Khi nào đọc** khớp hoạt động hiện tại. Không quét toàn bộ `.agents/rules/`.
3. Skill (`.agents/skills/*/SKILL.md`, khi có) chỉ khi task khớp skill đó.

## Bản đồ rule

| Chủ đề | Khi nào đọc | File |
|---|---|---|
| Hành vi agent (trung thực, giao tiếp, không workaround) | Luôn | `agent-conduct.md` |
| **Code change gate** | Trước khi báo xong, commit hoặc mở PR cho mọi thay đổi code/config/docs | `code-change-gate.md` |
| Code review | Người dùng yêu cầu review diff/PR/branch, hoặc trước khi merge | `code-review.md` |
| Quality gate local (analyzer, lint, secret, severity, phạm vi test) | Đổi code .NET/Angular/infra; chọn test targeted hay full; đổi cấu hình analyzer/lint | `quality-gate.md` |
| Pipeline lifecycle (thứ tự giai đoạn, cổng chuyển, làn nhẹ/hạ tầng) | Nhận một issue từ đầu đến cuối; không chắc đang ở giai đoạn nào hoặc có được bỏ bước không | `pipeline-lifecycle.md` |
| Bài học kinh nghiệm (ghi khi lỗi/sai sót, tra theo tag trước khi làm, phòng ngừa phải mã hoá) | Đầu task trong một vùng (tra tag); sau lỗi/sự cố/người dùng sửa agent; khi đóng issue/PR | `lessons-learned.md` + `document/lessons/` |
| Quy trình làm task | Mọi task (vibe code, sửa nhỏ, tính năng) | `task-workflow.md` |
| Kiến trúc & ranh giới service | Tạo/đổi ranh giới service, chọn kiến trúc, thêm service | `architecture.md` |
| API endpoint | Thêm/sửa/xoá endpoint HTTP (mọi service), đổi request/response/lỗi/phân trang, sửa OpenAPI | `api-endpoints.md` |
| Backend engineering (mã lỗi, config/Options, hằng số, `Program.cs`, worker, data access, gọi ngoài) | Sửa code backend `src/**` (mọi service, `src/shared`) | `backend-engineering.md` |
| C#/.NET | Viết hoặc sửa code C# | `dotnet.md` |
| Angular/TypeScript | Sửa `src/web/**` | `angular.md` |
| Frontend engineering (i18n, mã lỗi, config runtime, route/RBAC, mock, bảo mật client) | Sửa `src/web/**` | `frontend-engineering.md` |
| Angular reactivity (Signals, `effect`, `resource`, RxJS) | Viết/sửa code dùng `signal`, `computed`, `effect`, `resource`, hoặc RxJS trong `src/web/**` | `angular-reactivity.md` |
| Messaging | Event, SNS/SQS, outbox, consumer, saga | `messaging.md` |
| Observability & debug | Log, trace, metric, health check, debug sự cố | `observability.md` |
| Test review (TDD/BDD, `.feature`) | Viết/review file `.feature`, trước khi bắt đầu code production (đỏ → xanh), review test thực thi, đối chiếu kịch bản ↔ test | `test-review.md` |
| Testing | Viết hoặc sửa test; trước khi báo xong | `testing.md` |
| Docker & compose | Dockerfile, docker-compose, chạy local | `docker.md` |
| Terraform & AWS (vibe code hạ tầng: an toàn/phê duyệt, IAM, chi phí, state, bằng chứng, mô phỏng tấn công) | Sửa `infra/**`, Terraform, AWS, CI/CD, hoặc học kịch bản tấn công hạ tầng (`document/security/infra-attack-scenarios.md`) | `terraform.md` |
| Phụ thuộc bên thứ ba (hạn chế thư viện, giấy phép, hạng T0–T3, tự viết thay thư viện) | Thêm/đổi/nâng cấp NuGet, npm, provider Terraform, image, công cụ CLI; chọn thư viện cho tính năng mới; review thay đổi có phụ thuộc | `dependency-policy.md` + `config/dependencies.yaml` |
| Pin dependency | Sửa `*.csproj`, `Directory.Packages.props`, `package.json`, provider Terraform, base image Docker | `dependency-pinning.md` |
| Git commit message | Soạn commit (tay hoặc agent), sửa message, revert | `git-commit.md` |
| Git workflow | Tạo branch, commit, PR, đổi trạng thái issue | `git-workflow.md` |
| Quy ước tài liệu | Tạo/đổi tên file trong `document/`, ADR, runbook, issue, vẽ Mermaid | `docs-conventions.md` |
| Cập nhật tài liệu (spec; issue/ADR cũ bất biến) | Sửa nội dung bất kỳ file trong `document/`, đồng bộ spec/contract khi đổi hành vi, sửa issue/ADR | `docs-update-specs.md` |

## Skills

| Skill | Khi nào dùng | Canonical |
|---|---|---|
| `requirements-discovery` | Có yêu cầu chung chung/chưa rõ cần khai phá thành spec; tinh chỉnh spec; soạn câu hỏi gửi khách hàng | `.agents/skills/requirements-discovery/SKILL.md` |

| `workflow` | Làm một tính năng từ đầu đến PR, tiếp tục workflow dở, hỏi đang ở bước nào (điều phối các skill dưới đây, giao thức review lặp ở `references/review-loop.md`) | `.agents/skills/workflow/SKILL.md` |
| `spec-review` | Review spec trước system design (tìm điểm chưa rõ/mâu thuẫn, người dùng chốt, lặp đến PASS) | `.agents/skills/spec-review/SKILL.md` |
| `design-review` | Review design doc trước plan | `.agents/skills/design-review/SKILL.md` |
| `plan-init` | Lập kế hoạch triển khai cho một issue (TDD/BDD) | `.agents/skills/plan-init/SKILL.md` |
| `plan-review` | Review plan trước khi viết test | `.agents/skills/plan-review/SKILL.md` |
| `test-init` | Viết `.feature` và test, chạy ra đỏ đúng lý do | `.agents/skills/test-init/SKILL.md` |
| `test-review` | Review bộ test trước khi code | `.agents/skills/test-review/SKILL.md` |
| `coding-start` | Code theo Đỏ → Xanh → Refactor đến khi mọi test pass | `.agents/skills/coding-start/SKILL.md` |
| `coding-review` | Review code sau khi test xanh hết | `.agents/skills/coding-review/SKILL.md` |
| `create-pr` | Soạn và mở PR sau khi người dùng xác nhận | `.agents/skills/create-pr/SKILL.md` |
| `lesson-learned` | Ghi bài học sau lỗi/sự cố (nguyên nhân gốc, phòng ngừa mã hoá), tra bài học theo tag, rà định kỳ | `.agents/skills/lesson-learned/SKILL.md` |
| `aws-service-selection` | Quyết định dịch vụ AWS cho một service (chi phí đi đầu, 7 tiêu chí); so sánh hai dịch vụ AWS; ước tính chi phí; review stack AWS trước khi viết Terraform | `.agents/skills/aws-service-selection/SKILL.md` |
| `system-design` | Có spec đã rõ cần thiết kế hệ thống có giải thích đánh đổi; so sánh phương án; review thiết kế; giải thích pattern; phân tích khi tải tăng | `.agents/skills/system-design/SKILL.md` |

Claude Code đọc skill từ `.claude/skills/<tên>/SKILL.md` (file trỏ về canonical). Thêm skill mới: viết ở `.agents/skills/` rồi tạo file trỏ ở `.claude/skills/`.

## Thêm rule mới

1. Chỉ thêm khi gặp lỗi lặp lại hoặc ràng buộc cần ghi nhớ; mỗi rule hẹp, một chủ đề.
2. Viết file trong `.agents/rules/`. Rule áp dụng cho **mọi** hoạt động thì đưa vào `AGENTS.md`; còn lại chỉ nằm ở đây.
3. Thêm một dòng vào bảng trên, bắt buộc điền cột **Khi nào đọc**. Rule không có trong bảng thì agent sẽ không thấy.
4. Nêu lý do (lỗi hoặc quyết định nào sinh ra rule) trong một dòng ở đầu file.
