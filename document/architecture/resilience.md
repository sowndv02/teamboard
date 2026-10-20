# Resilience (chịu lỗi) cho microservice

Hệ phân tán luôn có lỗi một phần: mạng chập chờn, phụ thuộc chậm, instance chết. Resilience là thiết kế để **lỗi không lan** và **hệ thống tự phục hồi**. Quy tắc ngắn: `.agents/rules/backend-engineering.md` mục 8, `messaging.md`; tài liệu này là kiến thức đầy đủ. Quan sát: `observability.md`.

**Nguồn:** slide ".NET Observability, Resilience, Monitoring" (Retry, Circuit Breaker, Bulkhead, thứ tự áp dụng, retry khi migrate DB, dùng Polly) và slide .NET 8 (`Microsoft.Extensions.Http.Resilience`, health checks). Chi tiết cấu hình **kiểm tra tài liệu Polly/Microsoft hiện hành**.

## 1. Tư duy

- **Thiết kế cho lỗi** ("design for failure"): giả định mọi lời gọi ra ngoài có thể chậm, lỗi, hoặc trả sai.
- **Lỗi tạm thời** (transient) nên thử lại; **lỗi vĩnh viễn** (lỗi logic, 4xx) **không** thử lại.
- **Mỗi cơ chế giải một bài toán riêng**; dùng đúng chỗ, đúng thứ tự (mục 3), và **đo được** (metric: số retry, trạng thái breaker).
- Resilience **không thay** cho idempotency: retry chỉ an toàn với thao tác idempotent (`api-endpoints.md` mục 6, `messaging.md`).

## 2. Các pattern

| Pattern | Giải quyết | Cách dùng | Cạm bẫy |
|---|---|---|---|
| **Timeout** | Treo vô hạn khi phụ thuộc chậm | Đặt timeout cho **từng lần thử** và **tổng thời gian** | Quên đặt (mặc định `HttpClient` quá dài); timeout dài hơn của bên gọi |
| **Retry** (backoff mũ + **jitter**) | Lỗi tạm thời (mạng, 503, timeout) | Số lần giới hạn; chỉ cho thao tác idempotent; chỉ lỗi tạm thời | **Bão retry** (retry ở nhiều tầng nhân lên); không jitter làm đồng loạt đập cùng lúc; retry vô hạn |
| **Circuit Breaker** | Phụ thuộc đã hỏng: ngừng gọi để nó hồi phục và để mình thất bại nhanh | Trạng thái Closed → Open (sau chuỗi lỗi/ tỷ lệ lỗi) → Half-Open (thử vài request) | Ngưỡng sai gây mở nhầm/không mở; cần fallback hợp lý; breaker theo từng đích |
| **Bulkhead** (cô lập tài nguyên) | Một phụ thuộc chậm chiếm hết luồng/kết nối làm sập phần khác | Giới hạn đồng thời theo phụ thuộc/nhóm (pool riêng, hàng đợi giới hạn) | Đặt quá chặt gây từ chối oan |
| **Fallback / degrade** | Vẫn phục vụ phần nào khi phụ thuộc lỗi | Trả dữ liệu cache/mặc định/ẩn tính năng | Trả dữ liệu cũ sai nghiệp vụ; **tránh fallback ngầm che lỗi** |
| **Rate limiting / load shedding** | Quá tải | Giới hạn ở gateway và trong service; từ chối sớm có `429` | Công bằng giữa người dùng |
| **Hedging** (gửi yêu cầu dự phòng) | Giảm độ trễ đuôi | Chỉ cho đọc idempotent, chịu tải thêm | Tăng tải; hiếm khi cần |
| **Idempotency + dedupe** | Retry/trùng message | `Idempotency-Key`, dedupe `eventId` | Quên lưu khoá trong cùng giao dịch |
| **Retry khi khởi động (migration/kết nối DB)** | DB chưa sẵn sàng lúc service lên | Thử lại có giới hạn khi migrate/kết nối đầu (slide: retry migration DB) | Không tự `Migrate()` ở stg/prod (`backend-engineering.md` mục 7) |

## 3. Thứ tự áp dụng (slide có mục "Order of Resilience Patterns")

Nguyên tắc: policy **ngoài cùng** quyết định hành vi tổng thể, policy **trong cùng** bọc từng lần gọi. Thứ tự thường gặp (từ ngoài vào trong):

```
Rate limiter  →  Timeout tổng  →  Retry  →  Circuit breaker  →  Timeout từng lần thử  →  lời gọi
```

Lý do chính: **retry bọc ngoài circuit breaker** để mỗi lần thử đi qua breaker (breaker mở thì thử lại thất bại nhanh); **timeout từng lần thử ở trong cùng** để một lần chậm không giữ cả chuỗi; **timeout tổng ở ngoài** để giới hạn tổng thời gian khách chờ. `Microsoft.Extensions.Http.Resilience` có "standard resilience handler" dựng sẵn chuỗi tương tự (kiểm tra thứ tự cụ thể theo tài liệu hiện hành).

## 4. Trong .NET

- **`Microsoft.Extensions.Http.Resilience`** (.NET 8, dựa trên Polly v8) cho `HttpClient` qua `IHttpClientFactory`: `AddStandardResilienceHandler()` (cấu hình tuỳ chỉnh được) hoặc `AddResilienceHandler(...)` tự dựng pipeline. Dùng **typed client + factory**, không `new HttpClient()`.
- **Polly `ResiliencePipeline`** cho lời gọi không phải HTTP (SDK AWS, DB, hàng đợi). Slide dùng Polly kiểu cũ (policy); khái niệm tương đương, API khác giữa các phiên bản (kiểm tra).
- **Cấu hình qua Options** (số lần retry, ngưỡng breaker, timeout) có validate (`backend-engineering.md` mục 2), không literal.
- **Quan sát:** log mỗi lần retry/breaker đổi trạng thái (có `correlationId`); metric số retry, số lần breaker mở, thời gian ở trạng thái mở.
- **Test:** dùng fake/handler giả để mô phỏng lỗi tạm thời, timeout, lỗi liên tục; kiểm tra retry đúng số lần, breaker mở/đóng đúng, không retry với 4xx, idempotent giữ nguyên.

## 5. Với tin nhắn bất đồng bộ (SQS/SNS)

- **At-least-once + consumer idempotent**; **visibility timeout** dài hơn thời gian xử lý tối đa.
- **Retry của hàng đợi** (message trả về) + `maxReceiveCount` → **DLQ**; cảnh báo khi DLQ có message; quy trình **redrive** có kiểm soát.
- **Poison message**: không retry vô hạn; đưa DLQ kèm bối cảnh để xử lý thủ công.
- **Outbox** cho phía phát để không mất sự kiện khi lỗi giữa chừng (`messaging.md`).
- **Backpressure:** giới hạn đồng thời của consumer; đừng để một downstream chậm làm tràn hàng đợi mà không cảnh báo.

## 6. Trên AWS (hạ tầng)

| Cơ chế | Vai trò |
|---|---|
| **Multi-AZ** (ECS tasks ≥ 2 AZ, RDS Multi-AZ khi đáng chi phí) | Chịu sự cố một AZ; **đánh đổi chi phí** (CONFIRM theo `aws-service-selection`) |
| **ALB health check + ECS service** | Loại instance không khoẻ khỏi lưu lượng; ngưỡng/thời gian hợp lý với `/health/ready` |
| **ECS deployment circuit breaker** + rolling/blue-green | Tự rollback khi bản triển khai mới không lên khoẻ (kiểm tra tính năng hiện hành) |
| **Auto Scaling** có `min`/`max` | Chịu tải đột biến; **`max` để chặn chi phí** (denial of wallet) |
| **Backup, PITR, kiểm thử khôi phục (RTO/RPO)** | Phục hồi dữ liệu |
| **Throttling/WAF/rate limit ở biên** | Chống quá tải |
| **SQS DLQ + CloudWatch Alarm** | Phát hiện message độc |

## 7. Kiểm thử chịu lỗi và "game day"

- **Fault injection** bằng cờ `FAULT_*` (độ trễ, lỗi 5xx, ngắt kết nối, drop/duplicate message, poison message): bật có kiểm soát ở local/dev, **không bao giờ ở stg/prod** (`observability.md` rule).
- Mỗi bài thử: **giả thuyết** ("nếu `task-service` chậm 10 lần, `gateway` vẫn trả lỗi nhanh") → bật lỗi → quan sát trace/metric → so với giả thuyết → ghi runbook và sửa cấu hình (timeout/breaker/bulkhead).
- Test tải nhẹ (k6) để thấy hành vi dưới áp lực trước khi nghĩ tới scale.
- Mô phỏng tấn công/hạ tầng: `document/security/infra-attack-scenarios.md` (chỉ sandbox).

## 8. Anti-pattern hay gặp

- Retry ở **mọi tầng** (client, gateway, service) → nhân lên thành bão.
- Retry **không idempotent** (tạo trùng bản ghi).
- **Không có timeout** hoặc timeout mặc định rất dài.
- Circuit breaker **chung cho mọi đích**.
- Fallback **che lỗi** thay vì làm lộ lỗi để sửa.
- Health check `liveness` kiểm cả DB → khởi động lại hàng loạt khi DB chậm.
- Bỏ qua DLQ (không ai nhìn).

## 9. Việc cần chốt
- Dùng `Microsoft.Extensions.Http.Resilience`/Polly (đề xuất) hay tự viết lớp chịu lỗi để học (nếu muốn hiểu sâu, có thể tự cài retry/breaker đơn giản như bài tập, rồi so với thư viện).
- Ngưỡng mặc định (timeout, số retry, breaker) cho từng loại call (`TODO` khi có số đo thực tế).
- Mức Multi-AZ theo ngân sách và RTO/RPO (CONFIRM).
