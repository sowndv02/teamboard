# Architecture rules

Nguồn: `document/architecture/README.md`, ADR, `config/services.yaml`.

## Ranh giới service

- Mỗi service sở hữu DB riêng. Không query/ghi DB của service khác, không dùng chung connection string.
- Service gọi nhau qua REST (sync, khi bắt buộc) hoặc event (async). Không tham chiếu project/assembly của service khác.
- `src/shared/` chỉ chứa **contract** (event schema, DTO công khai) và **cross-cutting** (OTel, health, correlation). Cấm đặt business logic.
- Sync call giữa service phải có timeout, retry có jitter, circuit breaker.

## Kiến trúc từng service là BẮT BUỘC

| Service | Phải theo |
|---|---|
| gateway | YARP, chỉ routing/authN/rate limit; không có nghiệp vụ |
| identity-service | Vertical Slice: mỗi use case một thư mục (endpoint + handler + validator + model), không có tầng Service/Repository chung |
| board-service | Clean Architecture: Domain ← Application ← Infrastructure/Api; Domain không phụ thuộc framework |
| task-service | CQRS + Event Sourcing: command ghi event vào event store, query đọc read model; outbox cho publish |
| notification-service | Hexagonal: core chỉ biết port; adapter (SQS, email, DynamoDB) ở ngoài |
| activity-service | Event-driven/serverless: handler không giữ state cục bộ, idempotent |

Không "đơn giản hoá" một service về CRUD layered. Muốn đổi kiến trúc → viết ADR, hỏi người dùng.

## Thêm service mới

Cần: issue, mục trong `config/services.yaml`, cập nhật bảng trong `PROJECT.md`, ADR nếu kiến trúc mới, health `/health/live` + `/health/ready`, OTel, Dockerfile, mục compose, test.

## API

Quy ước endpoint thống nhất cho mọi service nằm ở `api-endpoints.md` (URL, status, lỗi `problem+json`, phân trang, header, bảo mật). Service không tự đặt quy ước riêng.

## Tham khảo

Danh mục kiến trúc/pattern và đối chiếu với từng service: `document/architecture/patterns-catalog.md`. Identity tự xây: `document/architecture/identity-learning.md` (ADR 0006).
