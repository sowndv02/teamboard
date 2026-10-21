# TeamBoard Microservices — Kế hoạch kiến trúc

Mục tiêu học tập:
1. **System design**: mỗi service dùng một kiến trúc nội bộ + một kiểu giao tiếp/lưu trữ khác nhau.
2. **Vận hành nhiều service**: build, deploy, cấu hình, quan sát, debug sự cố liên service.
3. **Infra as Code**: toàn bộ hạ tầng AWS bằng Terraform.

Domain giữ nhỏ (Board, Task, thông báo, thống kê) để thời gian dành cho kiến trúc, không dành cho nghiệp vụ.

## 1. Bản đồ service

| Service | Trách nhiệm | Kiến trúc nội bộ (điểm luyện) | Lưu trữ | Giao tiếp |
|---|---|---|---|---|
| `gateway` | Entry point, routing, kiểm tra JWT, rate limit | **API Gateway** (YARP) | – | HTTP vào, HTTP ra |
| `identity-service` | Đăng ký, đăng nhập, JWT | **Vertical Slice** (Minimal API) | PostgreSQL | REST; phát JWKS |
| `board-service` | CRUD Board, thành viên (từ `teamboard-api` hiện có) | **Clean Architecture** + EF Core | SQL Server (RDS) | REST; publish event |
| `task-service` | Vòng đời Task (tạo, gán, đổi trạng thái) | **CQRS + Event Sourcing** + Outbox | PostgreSQL (event store + read model) | REST + publish event |
| `notification-service` | Gửi email/webhook khi có event | **Hexagonal (Ports & Adapters)**, consumer idempotent, DLQ | DynamoDB (dedupe) | Consume SQS |
| `activity-service` | Feed hoạt động + thống kê theo Board | **Serverless / Event-driven** (Lambda hoặc worker Go) | DynamoDB | Consume SNS→SQS |

Ghi chú: tech stack chính là ASP.NET Core (.NET 10) (ADR 0005); frontend tạm hoãn ở giai đoạn này (ADR 0008); `activity-service` là ứng viên dùng Go để thử polyglot (không bắt buộc).

## 2. Giao tiếp

- **Đồng bộ**: client → gateway → service (REST/JSON). Service-to-service sync chỉ dùng khi bắt buộc (vd. task-service kiểm tra board tồn tại), kèm timeout + retry + circuit breaker (Polly).
- **Bất đồng bộ**: SNS topic `teamboard-events` → SQS queue riêng từng consumer (+ DLQ). Event có `eventId`, `type`, `occurredAt`, `correlationId`, `version`.
- **Tin cậy**: Transactional Outbox ở `task-service` và `board-service`; consumer idempotent.
- **Saga** (bài tập mở rộng): "Xoá Board" → huỷ Task → dọn feed (choreography trước, orchestration sau).

## 3. Observability & debug (phần quan trọng nhất của mục tiêu 2)

- **OpenTelemetry** mọi service: trace, metric, log có `traceId`/`correlationId`.
- Local: Jaeger (trace), Prometheus + Grafana (metric), Seq hoặc Loki (log).
- AWS: CloudWatch Logs + X-Ray/ADOT collector.
- Health: `/health/live`, `/health/ready` cho mọi service.
- **Debug lab**: flag `FAULT_*` (chèn latency, lỗi 500, drop message, poison message) để chủ động tạo sự cố rồi truy vết bằng trace/log. Mỗi sự cố ghi lại thành runbook trong `document/runbooks/`.

## 4. Cấu trúc repo đề xuất

```
src/
  gateway/
  identity-service/
  board-service/          # đổi tên từ teamboard-api
  task-service/
  notification-service/
  activity-service/
  shared/                 # contracts (event schema), ServiceDefaults (OTel, health) — giữ MỎNG
  docker-compose.yml      # + compose.observability.yml, compose.localstack.yml
infra/
  modules/                # network, ecr, ecs-service, rds, sqs-sns, dynamodb, alb, observability
  envs/dev/  envs/stg/    # mỗi env 1 state riêng
  bootstrap/              # S3 state bucket + lock
document/
  architecture/  runbooks/
.github/workflows/        # build+test+push ECR, terraform plan/apply (OIDC)
```

## 5. Infra (Terraform → AWS)

- Remote state: S3 + lock, bootstrap riêng.
- Network: VPC 2 AZ, public/private subnet, ALB public, service ở private.
- Compute: ECS Fargate (module `ecs-service` dùng lại cho mọi service); Lambda cho `activity-service` nếu chọn serverless.
- Data: RDS SQL Server (board) và RDS PostgreSQL (identity, task), DynamoDB, SNS/SQS.
- Secrets: SSM Parameter Store / Secrets Manager; IAM role riêng từng service (least privilege).
- CI/CD: GitHub Actions + OIDC, không dùng access key tĩnh.
- Local: docker-compose + LocalStack giả lập SNS/SQS/DynamoDB để dev không tốn tiền.
- **Chi phí**: RDS/ALB/NAT chạy 24/7 tốn tiền → `terraform destroy` sau mỗi buổi lab, ở dev dùng chung 1 RDS Postgres cho nhiều DB logic, đặt AWS Budget alert.

## 6. Lộ trình

| Giai đoạn | Nội dung | Kết quả kiểm chứng |
|---|---|---|
| 0 | Chốt ADR, đổi tên `teamboard-api` → `board-service`, `shared/ServiceDefaults` (OTel, health) | compose chạy 1 service có trace trong Jaeger |
| 1 | `gateway` + `identity-service` + `board-service` (sync only) | login → tạo board qua gateway |
| 2 | `task-service` (CQRS/ES + outbox) + SNS/SQS trên LocalStack | tạo task → event xuất hiện đúng 1 lần |
| 3 | `notification-service`, `activity-service` (consumer, idempotency, DLQ) | poison message vào DLQ, replay được |
| 4 | Terraform: network, ECR, ECS, RDS, SNS/SQS; deploy `board-service` đầu tiên | URL ALB trả health 200 |
| 5 | Deploy đủ service + CI/CD + dashboard/alarm | push → tự deploy dev |
| 6 | Debug lab: fault injection, load test (k6), saga, autoscaling | 3+ runbook sự cố |

## 7. Bẫy cần tránh

- Shared library chứa business logic → mất độc lập; `shared/` chỉ chứa contract + cross-cutting.
- Dùng chung database giữa service (mỗi service sở hữu data của mình).
- Làm đủ 6 service cùng lúc; đi theo từng giai đoạn, mỗi giai đoạn chạy được end-to-end.

## ADR

- [0001 Chia service & kiến trúc từng service](adr/0001-service-boundaries.md)
- [0002 Giao tiếp: REST + SNS/SQS + Outbox](adr/0002-communication.md)
- [0003 Runtime: ECS Fargate thay vì EKS](adr/0003-ecs-fargate.md)
- [0004 Terraform layout & state](adr/0004-terraform-layout.md)
- [0005 Tech stack chính: ASP.NET Core + Angular](adr/0005-techstack-aspnet-angular.md) (phần Angular tạm hoãn bởi ADR 0008)
- [0006 Tự xây identity-service (không dùng framework identity)](adr/0006-identity-self-built.md)
- [0007 Chính sách phụ thuộc bên thứ ba](adr/0007-dependency-policy.md)
- [0008 Tạm hoãn frontend ở giai đoạn hiện tại](adr/0008-defer-frontend.md)

## Tài liệu kiến trúc chi tiết

- [patterns-catalog.md](patterns-catalog.md): danh mục kiến trúc/pattern hay dùng trong dự án thực tế, đối chiếu với TeamBoard
- [identity-learning.md](identity-learning.md): lộ trình tự xây `identity-service` (JWT, OAuth 2.0, OIDC)
- [observability.md](observability.md): log, metric, trace, health; stack local và AWS (CloudWatch, ADOT/X-Ray)
- [resilience.md](resilience.md): retry, circuit breaker, bulkhead, timeout, chịu lỗi trên AWS
- [references.md](references.md): nguồn tham khảo (slide, chuẩn, sách) và cách dùng
