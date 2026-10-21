---
title: <service> — lựa chọn dịch vụ AWS
service: <tên service>
status: draft            # draft | in-review | confirmed | superseded
owner: <người chịu trách nhiệm>
updated: YYYY-MM-DD
spec: document/spec/<ten>.md
design: document/design/<ten>.md
region: TODO
---

# Hạ tầng AWS: <service>

## 1. Bối cảnh
Service làm gì (link spec/design), kiến trúc nội bộ (theo `architecture.md`), ràng buộc đã chốt (ADR).

## 2. Chi phí (cổng đầu tiên)

- Trần ngân sách dự án (`quality.monthlyBudgetUsd`):
- Phần của service này (% và USD) và theo môi trường:

| Môi trường | Phần ngân sách (USD/tháng) | Chạy khi nào (24/7 / theo lịch / theo lab) |
|---|---|---|
| dev | | |
| stg | | |
| prod | | |

## 3. Bảy tiêu chí (tóm tắt đầu vào)

| # | Tiêu chí | Nội dung cho service này | Nguồn (spec/giả định) |
|---|---|---|---|
| 1 | Workload | | |
| 2 | Hiệu năng và quy mô (RPS, đồng thời, p95, payload, đột biến, tăng trưởng) | | |
| 3 | Dữ liệu và nhất quán (quan hệ, giao dịch, mẫu truy vấn, retention, backup, migrate) | | |
| 4 | Sẵn sàng và xử lý lỗi (SLA, Multi-AZ, RTO/RPO, retry, timeout, DLQ, idempotency) | | |
| 5 | Bảo mật và tuân thủ (public/private, IAM, mã hoá, bí mật, mạng, data residency, audit) | | |
| 6 | Chi phí | xem mục 2, 6 | |
| 7 | Vận hành và ràng buộc (ai quản OS/runtime, patch, scale, deploy, debug, lock-in, giới hạn, kỹ năng) | | |

## 4. Quyết định theo năng lực

### 4.x. <Năng lực: compute / dữ liệu / sự kiện / vào-ra mạng / bảo mật / quan sát>
- **Nhãn:** `[Cần]` / `[Chọn để học]`

| Ứng viên | Chi phí/tháng dev / prod (% trần) | Chạy rỗng? | Workload | Quy mô | Dữ liệu | Sẵn sàng | Bảo mật | Vận hành |
|---|---|---|---|---|---|---|---|---|
| A (rẻ nhất đủ dùng) | | | | | | | | |
| B | | | | | | | | |

- **Khuyến nghị:** … vì …
- **Vì sao không chọn B:** … **B sẽ đúng khi:** …
- **Đánh đổi / rủi ro / khả năng đảo ngược:**
- **Tín hiệu xem lại (chi phí, tải):**

(Lặp cho từng năng lực)

## 5. Danh sách tài nguyên đề xuất duyệt

Chỉ tài nguyên trong bảng này được tạo (`terraform.md` mục 2).

| Dịch vụ AWS | Mục đích | Cấu hình dev | Cấu hình stg/prod | Chi phí ước tính/tháng | Tag | Secret cần (tên, không giá trị) | Ghi chú bảo mật |
|---|---|---|---|---|---|---|---|

## 6. Chi phí chi tiết

Theo `cost-model.md` (công thức, giả định, nguồn giá, vùng, ngày tra, hoặc `chưa xác minh`).

| Thành phần | Đơn vị tính phí | Số đơn vị/tháng (giả định) | Giá đơn vị (nguồn, ngày) | Chi phí/tháng | Cố định (R) / theo dùng (U) / ẩn (H) |
|---|---|---|---|---|---|

| Kịch bản tải | Dev | Stg | Prod | % trần |
|---|---|---|---|---|
| Thấp (~0,1×) | | | | |
| Kỳ vọng (1×) | | | | |
| Đỉnh (~10×) | | | | |

- **Chạy rỗng (cố định) mỗi tháng:**
- **Chi phí ẩn đã tính / chưa tính:**
- **Chi phí vận hành con người (ước lượng):**
- **Kiểm lại sau khi chọn:** tổng vs phần ngân sách:
- **Kiểm soát:** Budgets (ngưỡng), `max_capacity`, retention log, lifecycle, lịch tắt/`destroy`, tag chi phí:

## 7. Bảo mật và độ tin cậy (tóm tắt quyết định)
IAM theo service, mạng (public/private), mã hoá, bí mật, DLQ/timeout/idempotency, Multi-AZ/backup/RTO/RPO, quan sát. Đánh đổi chi phí ↔ bảo mật/sẵn sàng đã được người dùng quyết (link Confirm log).

## 8. Rủi ro và pre-mortem
| Nguyên nhân thất bại khả dĩ (chi phí/vận hành/giới hạn dịch vụ) | Xác suất | Tác động | Giảm thiểu | Tín hiệu cảnh báo sớm |
|---|---|---|---|---|

## 9. Bàn giao cho Terraform
Module/tài nguyên cần viết, khác biệt theo môi trường, tên secret, phụ thuộc giữa tài nguyên, thứ tự triển khai, ADR liên quan, issue hạ tầng.

## 10. Câu hỏi mở và giả định
| # | Mục | Người xác nhận | Mặc định tạm dùng |
|---|---|---|---|

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| YYYY-MM-DD | Tạo bản nháp | |
