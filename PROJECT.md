# TeamBoard Microservices

Dự án học tập: hệ thống microservice (domain Board/Task nhỏ) để luyện **system design** (mỗi service một kiến trúc khác nhau), **vận hành/debug nhiều service**, và **Terraform/AWS**. Ưu tiên chất lượng kiến trúc và khả năng quan sát hơn nghiệp vụ.

Nguồn sự thật: `document/architecture/README.md` (bản đồ service, lộ trình) và `document/architecture/adr/`.

## Trạng thái hiện tại

Giai đoạn 0 (xem lộ trình trong README). Mới có `src/teamboard-api` (ASP.NET Core .NET 10 + EF Core + SQL Server, sẽ đổi tên thành `board-service`). Cập nhật dòng này khi chuyển giai đoạn.

## Service và kiến trúc BẮT BUỘC

| Service | Kiến trúc | Lưu trữ |
|---|---|---|
| web | Angular, feature-based, standalone + Signals | – |
| gateway | API Gateway/BFF (YARP) | – |
| identity-service | Vertical Slice | PostgreSQL |
| board-service | Clean Architecture | SQL Server |
| task-service | CQRS + Event Sourcing + Outbox | PostgreSQL |
| notification-service | Hexagonal, consumer idempotent + DLQ | DynamoDB |
| activity-service | Serverless / event-driven | DynamoDB |

Không "đơn giản hoá" một service về CRUD layered: kiến trúc khác nhau chính là mục đích bài tập.

## Quy ước riêng của dự án

- Tech stack chính (ADR 0005): **ASP.NET Core (.NET 10)** cho backend, **Angular** cho frontend (`src/web`, chỉ gọi backend qua gateway). Công nghệ khác chỉ khi ADR cho phép.
- Mỗi service sở hữu database riêng; không truy cập DB của service khác.
- `src/shared/` chỉ chứa contract (event schema) và cross-cutting (OTel, health). Không đặt business logic.
- Giao tiếp: REST qua gateway; event qua SNS → SQS (+ DLQ). Producer dùng outbox, consumer idempotent theo `eventId`. Mọi request/event mang `correlationId` + `traceparent`.
- Mọi service có `/health/live`, `/health/ready` và OpenTelemetry (trace, metric, log).
- Infra trong `infra/` (modules + envs/dev, envs/stg).
- Tài liệu và comment giải thích viết tiếng Việt.

## Lệnh

Xem `COMMANDS.md`: các lệnh cần chạy đầu phiên, kiểm tra trước khi báo xong, chạy local, infra, và danh sách lệnh không được tự chạy.

## Workflow (issue, branch, commit, PR)

Cấu hình trong `config/project.yaml`. Tóm tắt:

- Issue là file Markdown ở `document/issues/`, key `TB-<số>` (xem `document/issues/README.md`). Mọi thay đổi code gắn với một issue; nếu chưa có thì tạo issue trước.
- Branch `feature/TB-001-<slug>` từ `main`; commit tiếng Anh `type(TB-001): summary` kèm body bắt buộc giải thích vì sao (chi tiết `.agents/rules/git-commit.md`).
- PR dùng GitHub (`gh`), tiêu đề `TB-001: <tóm tắt>`, mô tả link tới file issue và cách kiểm chứng.
- Đổi trạng thái issue → cập nhật file issue và bảng trong `document/issues/README.md`.

## Cấu hình (`config/`)

Tra cổng, tên service, môi trường, phiên bản công cụ tại `config/` (xem `config/README.md`). Không chứa secret. Giá trị `TODO` là chưa quyết định: hỏi người dùng, đừng tự điền.

## Thư mục `local/`

`local/` bị gitignore, dùng cho ghi chú, script, dữ liệu, credential chỉ dùng trên máy cá nhân. Không đặt thứ repo cần vào đây và không tham chiếu nó từ code hay tài liệu được commit.

## Vấn đề đã biết

- `src/.env.develop` và `src/.env.stg` từng được commit (chứa `SA_PASSWORD`, connection string). Đã gỡ khỏi git và có file `*.example`, nhưng giá trị vẫn nằm trong lịch sử git → xoay mật khẩu nếu là thật.
