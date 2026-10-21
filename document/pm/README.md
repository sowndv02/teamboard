# Quản lý dự án (PM)

Hồ sơ quản lý dự án TeamBoard, do skill `project-manager` duy trì (`.agents/skills/project-manager/`). Chủ dự án quyết định; PM tư vấn, theo dõi và nhắc rủi ro. Hồ sơ **gọn**, chỉ giữ thứ giúp ra quyết định tốt hơn.

| Tài liệu | Nội dung | Cập nhật khi |
|---|---|---|
| [roadmap.md](roadmap.md) | Mục tiêu, tiêu chí thành công, giai đoạn/mốc kèm kết quả học tập | Đổi phạm vi hoặc mốc |
| [backlog.md](backlog.md) | Danh sách việc có thứ tự (Now/Next/Later), ưu tiên, phụ thuộc | Mỗi lần `plan`/`change` |
| [raid.md](raid.md) | Rủi ro, giả định, vấn đề, phụ thuộc | Mỗi tuần và khi có rủi ro mới |
| [status/](status/_template.md) | Báo cáo tiến độ (`YYYY-MM-DD.md`) | Mỗi tuần / cuối giai đoạn |
| [sprints/](sprints/_template.md) | Kế hoạch tuần/sprint (`SNN.md`) | Đầu mỗi kỳ |
| [retro/](retro/_template.md) | Retro (`YYYY-MM-DD.md`) | Cuối kỳ/giai đoạn |
| [pm-journal.md](pm-journal.md) | Nhật ký học kỹ năng PM của chủ dự án | Mỗi lần `coach`/`simulate` |

Cấu hình PM: `config/project.yaml` → `pm`. Các nguồn trạng thái thật: `document/issues/`, `document/workflow/`, `git log`, `document/lessons/`.

Nguyên tắc: **bằng chứng, không bịa**; ước lượng là khoảng; WIP có giới hạn; tin xấu báo sớm; không tự đổi phạm vi khi chưa được duyệt.
