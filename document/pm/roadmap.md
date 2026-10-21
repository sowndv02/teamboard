# Roadmap

Cập nhật: 2026-10-21 · Chủ sở hữu: chủ dự án · Nguồn: `PROJECT.md`, `document/architecture/README.md` (mục 6).

## Mục tiêu và tiêu chí thành công

Dự án học tập (microservice, domain Board/Task nhỏ). Thành công gồm **kết quả giao hàng** và **kết quả học tập**:

| # | Mục tiêu | Tiêu chí thành công (đo được) | Trạng thái |
|---|---|---|---|
| G1 | **System design:** mỗi service một kiến trúc khác nhau | Mỗi service theo đúng kiến trúc đã giao; có ADR giải thích đánh đổi; giải thích được vì sao chọn/không chọn | Chưa bắt đầu code |
| G2 | **Vận hành và debug nhiều service** | Truy vết một request qua ≥ 3 service bằng trace/log; ≥ 3 runbook sự cố từ debug lab | Chưa bắt đầu |
| G3 | **Terraform/AWS** | Triển khai được ≥ 1 service lên ECS bằng Terraform, có Budgets và cách tắt | Chưa bắt đầu |
| G4 | **Bảo mật và chi phí có kỷ luật** | Không secret trong repo; chi phí ≤ ngân sách (`quality.monthlyBudgetUsd`, còn `TODO`) | Đang thiết lập (LL-001, LL-002) |
| G5 | **Quy trình làm việc với AI agent ổn định** | Một tính năng chạy hết `workflow` (spec → PR) với review lặp và state ghi đủ | Đã dựng quy trình; chưa chạy thử |
| G6 | **Kỹ năng PM** | `pm-journal.md` ghi ≥ 5 khái niệm đã áp dụng; ≥ 3 báo cáo tuần; ≥ 1 retro | Mới bắt đầu |

## Giai đoạn và mốc

Mỗi mốc có **tiêu chí hoàn thành kiểm chứng được** và **điều học được**. Thời lượng chưa ước lượng (chưa có số liệu năng lực: `pm.capacityHoursPerWeek` còn `TODO`).

| GĐ | Mốc | Nội dung | Tiêu chí hoàn thành | Học được | Trạng thái | Phụ thuộc/chặn |
|---|---|---|---|---|---|---|
| 0 | **M0 Nền tảng** | Quy tắc agent, workflow, tài liệu nền (TB-002); đổi tên `board-service`, `ServiceDefaults`, Jaeger (TB-001) | compose chạy 1 service có trace trong Jaeger; quy tắc được review | Quy trình, observability cơ bản | Đang làm (TB-002 in-progress; TB-001 todo) | Chốt công cụ giả lập AWS cục bộ (ADR 0007), BDD runner |
| 1 | **M1 Đường vào + danh tính** | `gateway` + `identity-service` (I1–I2 trước) + `board-service` (đồng bộ) | login → tạo board qua gateway | Gateway, JWT, Vertical Slice | Chưa bắt đầu | Spec identity; ADR 0006 đã chốt |
| 2 | **M2 Sự kiện** | `task-service` (CQRS/ES + outbox) + SNS/SQS | tạo task → sự kiện phát đúng 1 lần | CQRS/ES, outbox, idempotency | Chưa bắt đầu | Công cụ giả lập AWS; ngân sách (nếu dùng AWS thật) |
| 3 | **M3 Consumer** | `notification-service`, `activity-service` | poison message vào DLQ, replay được | Hexagonal, serverless, DLQ | Chưa bắt đầu | Chọn dịch vụ AWS (`aws-service-selection`) |
| 4 | **M4 Hạ tầng đầu tiên** | Terraform: network, ECR, ECS, RDS, SNS/SQS; deploy `board-service` | URL ALB trả health 200; Budgets bật | Terraform, chi phí AWS | Chưa bắt đầu | **Ngân sách, region, hồ sơ AWS chỉ đọc** (đều `TODO`) |
| 5 | **M5 Đủ service + CI/CD** | Deploy đủ service; GitHub Actions OIDC; dashboard/alarm | push → tự deploy dev | CI/CD, vận hành AWS | Chưa bắt đầu | M4 |
| 6 | **M6 Debug lab** | Fault injection, k6, saga, autoscaling, kịch bản tấn công sandbox | ≥ 3 runbook; ≥ 1 bài mô phỏng tấn công có ghi chép | Resilience, bảo mật vận hành | Chưa bắt đầu | M5; tài khoản sandbox |

## Quyết định lớn đang chờ (chặn mốc)

| # | Quyết định | Chặn | Người quyết |
|---|---|---|---|
| D1 | Ngân sách AWS/tháng (`quality.monthlyBudgetUsd`) | M4, `aws-service-selection` | Chủ dự án |
| D2 | Công cụ giả lập AWS cục bộ (ADR 0007) | M2 | Chủ dự án |
| D3 | BDD runner + test framework (`tool-versions.yaml`) | `test-init` mọi issue | Chủ dự án |
| D4 | Region AWS, account | M4 | Chủ dự án |

## Nguyên tắc cuốn chiếu
Rõ chi tiết cho 1–2 mốc gần; các mốc xa giữ mức mơ hồ có chủ đích và làm rõ khi tới gần (không lập kế hoạch chi tiết sớm).
