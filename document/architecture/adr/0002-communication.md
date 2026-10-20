# ADR 0002 — Giao tiếp: REST + SNS/SQS + Outbox

Trạng thái: Đề xuất · Ngày: 2026-10-20

## Quyết định
- Client ↔ gateway ↔ service: REST/JSON.
- Event giữa service: SNS fan-out → SQS mỗi consumer + DLQ. Chưa dùng Kafka (nặng và đắt trên AWS cho mục đích học; MSK có thể là ADR sau).
- Producer dùng Transactional Outbox; consumer phải idempotent (dedupe theo `eventId`).
- Mọi request/event mang `correlationId` + W3C `traceparent`.
- Sync service-to-service: timeout, retry có jitter, circuit breaker.

## Đánh đổi
Eventual consistency, thứ tự message không đảm bảo (chỉ dùng SQS FIFO nơi cần). Local dùng LocalStack, hành vi có thể lệch nhẹ so với AWS thật.
