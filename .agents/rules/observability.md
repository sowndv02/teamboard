# Observability & debug rules

- Mọi service dùng `ServiceDefaults` chung: OpenTelemetry (trace, metric, log), `/health/live` (process sống, không check dependency), `/health/ready` (check dependency cần thiết).
- Mỗi request/message mang `correlationId` + W3C `traceparent`; truyền sang mọi call ra ngoài và vào mọi log.
- Log có cấu trúc, đúng mức: `Information` cho sự kiện nghiệp vụ, `Warning` cho tình huống phục hồi được, `Error` cho lỗi cần người xem. Không log secret/PII/body đầy đủ.
- Đặt tên span/metric theo `<service>.<operation>`; không dùng label cardinality cao (userId, taskId) cho metric.
- Local: Jaeger (trace), Prometheus + Grafana (metric), log tập trung. AWS: CloudWatch + X-Ray/ADOT.

## Debug sự cố

1. Tái hiện, lấy `correlationId`/`traceId`.
2. Theo trace qua các service để tìm span lỗi hoặc chậm, rồi đọc log cùng `traceId`.
3. Tìm **nguyên nhân gốc** trước khi sửa; không thêm retry/try-catch để che.
4. Sửa + thêm test tái hiện lỗi.
5. Ghi runbook ngắn vào `document/runbooks/<tên>.md`: triệu chứng, cách chẩn đoán, nguyên nhân, cách xử lý.

## Fault injection (debug lab)

Chỉ qua cờ `FAULT_*` bật bằng env, mặc định tắt, không bao giờ bật ở stg/prod. Không để code fault chạy khi cờ tắt.
