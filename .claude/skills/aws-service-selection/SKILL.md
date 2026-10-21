---
name: aws-service-selection
description: Chọn dịch vụ AWS (compute, dữ liệu, hàng đợi, mạng, bảo mật, quan sát) cho từng service của hệ thống, đặt chi phí lên đầu rồi cân theo workload, hiệu năng/quy mô, dữ liệu/nhất quán, sẵn sàng/xử lý lỗi, bảo mật/tuân thủ, độ phức tạp vận hành; có giải thích vì sao chọn/không chọn. Dùng khi cần quyết định hạ tầng AWS cho một service trước khi viết Terraform, so sánh hai dịch vụ AWS, ước tính chi phí hoặc review stack AWS đã chọn.
---

Skill này là bản trỏ cho Claude Code. Nội dung canonical nằm ở `.agents/skills/aws-service-selection/SKILL.md`; đọc và làm đúng theo đó, kèm `references/cost-model.md` (cách ước tính chi phí) và `references/aws-service-catalog.md` (danh mục dịch vụ). Mẫu: `document/infra/_template.md`.
