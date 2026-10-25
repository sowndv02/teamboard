---
feature: api-gateway
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
status: approved            # draft | approved | done
updated: 2026-10-23
---

# Slices: api-gateway

Chia theo `../.agents/skills/workflow/references/slicing.md`. Mỗi lát là một hành vi chạy được qua host test in-process với upstream giả (A-01), test xanh riêng và merge độc lập. Lát nào cũng tuân thứ tự middleware duy nhất của design 6.4 và thêm test thứ tự tương ứng.

**Điều kiện trước lát 1:** TB-001 xong (đổi tên `board-service`, `src/shared/ServiceDefaults`, Jaeger) theo Q-D1.

| # | Lát (hành vi giao được) | FR/BR/NFR | Phụ thuộc | Ước lượng (khoảng + giả định) | Issue | Trạng thái |
|---|---|---|---|---|---|---|
| 1 | Khung gateway: host YARP, route `/api/v1/boards/**` tới upstream giả giữ path, `/health/live` + `/health/ready`, `404 problem+json` cho route lạ, thư viện nhỏ `src/shared` (problem+json), compose và `config/services.yaml` | FR-001, FR-002, BR-001, BR-002, BR-003, NFR-004, NFR-005 | TB-001 | 2–3 ngày; giả định ServiceDefaults đã có OTel và health | [TB-010](../issues/TB-010-gateway-skeleton-routing.md) | todo |
| 2 | Correlation và trace: tạo/giữ/thay `X-Correlation-Id`, ghi đè header xuống upstream, `traceparent`, trace nối trong Jaeger; middleware correlation vào `src/shared`; ExceptionHandler trả `500 INTERNAL_ERROR` dạng `problem+json` (nhận từ review TB-010) | FR-003, NFR-002 | 1 | 1–2 ngày; giả định Jaeger của TB-001 chạy được | [TB-022](../issues/TB-022-gateway-correlation-error-handling.md) | todo |
| 3 | Lỗi phía sau: timeout tổng 30 giây + idle, ánh xạ `503/502/504` trước header, ngắt kết nối sau header + metric, `Retry-After` cho `503` | FR-006, NFR-003 | 1, 2 | 2–3 ngày; rủi ro hành vi YARP khác giả định (design mục 16) | TB-0xx | todo |
| 4 | Rate limit theo IP: `ForwardedHeaders` an toàn, chính sách đọc/ghi sliding window, IPv6 /64, `429 problem+json` + `Retry-After`, health không bị giới hạn | FR-004 | 1, 2 | 2 ngày; giả định RateLimiter nền tảng đủ dùng | TB-0xx | todo |
| 5 | Giới hạn body 1 MiB: `413` khi `Content-Length` lớn, ngắt luồng khi chunked | FR-007 | 1, 2 | 1 ngày | TB-0xx | todo |
| 6 | CORS theo allowlist: preflight không tính rate limit, header cho phép/lộ ra, `429` vẫn mang header CORS | FR-005 | 1, 4 | 1 ngày | TB-0xx | todo |
| 7 | Quan sát và vận hành: metric độ trễ thêm vào, log không lộ `Authorization`/body kèm IP, lưu log 7 ngày (local), graceful shutdown, cảnh báo "không ra internet trước JWT" | NFR-001, NFR-006 | 2, 3, 4 | 1–2 ngày | TB-0xx | todo |
| 8 | Smoke với `board-service` thật qua compose (A-01), kiểm trace trong Jaeger | FR-001, NFR-002 | 1–7, `board-service` có `/api/v1/boards` | 0,5 ngày; chỉ làm khi service có endpoint | TB-0xx | todo (hoãn) |

Tổng ước lượng 10–14 ngày làm việc (khoảng, một người, giả định chưa kiểm).

## Phủ yêu cầu

| Yêu cầu | Lát |
|---|---|
| FR-001, FR-002 | 1 (và 8 cho smoke thật) |
| FR-003, NFR-002 | 2 |
| FR-004 | 4 |
| FR-005 | 6 |
| FR-006, NFR-003 | 3 |
| FR-007 | 5 |
| NFR-001, NFR-006 | 7 |
| NFR-004, NFR-005, BR-001..BR-003 | 1 (cấu hình và kiểm ở mọi lát sau) |

Mọi FR/NFR Must/Should của spec có ít nhất một lát. NFR xuyên suốt (cấu hình không secret, không log token/body) là tiêu chí của mọi lát, riêng NFR-006 được kiểm tập trung ở lát 7.

## Ngoài lát (hoãn/không làm)

- Kiểm JWT, rate limit theo user (sau khi có identity, theo spec mục 4).
- Retry, circuit breaker (bài sau, ghi lệch có chủ ý ở design D4).
- Route tới service khác ngoài boards (mỗi service tự thêm route khi xuất hiện).
- Triển khai AWS (ECS/ALB/Terraform, giai đoạn 4), log retention 7 ngày trên CloudWatch.

## Thứ tự và lý do

1. Lát 1 là xương sống: có host, route, health, hợp đồng lỗi và thư viện dùng chung để các lát sau cắm vào.
2. Lát 2 trước các lát lỗi vì mọi phản hồi lỗi phải mang `X-Correlation-Id` (design 6.4).
3. Lát 3, 4, 5, 6 độc lập sau lát 2 (riêng lát 6 cần lát 4 để kiểm `429` có header CORS). Thứ tự đề xuất 3 → 4 → 5 → 6 theo mức ưu tiên (FR Must trước Should) và rủi ro (lát 3 kiểm hành vi YARP chưa xác minh).
4. Lát 7 sau cùng vì kiểm tra tổng hợp trên pipeline đã đủ.
5. Lát 8 chỉ chạy khi `board-service` có endpoint thật; không chặn các lát còn lại.
6. Mỗi lát lập plan chi tiết ngay trước khi làm, không lập sẵn.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | slices:api-gateway:order-and-boundaries | Duyệt thứ tự và ranh giới 8 lát | Duyệt như đề xuất (1→2→3→4→5→6→7, lát 8 hoãn) | 1 | 2026-10-23 |
| 2 | slices:api-gateway:issue-numbering | Cách tạo issue cho từng lát | TB-007 là issue tài liệu (spec, design, bảng lát); mỗi lát một issue mới, chỉ tạo issue lát 1 bây giờ | 1 | 2026-10-23 |
| 3 | slices:api-gateway:internal-error-owner | Lát nào nhận ExceptionHandler và `500 INTERNAL_ERROR` (design 6.3, 6.4) | Lát 2 (correlation), cùng thứ tự middleware Correlation và ExceptionHandler | 2 | 2026-10-24 |
