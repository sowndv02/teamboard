# Danh mục kiến trúc và pattern hay dùng trong dự án thực tế

Mục đích: bản đồ để học và để chọn kiến trúc nội bộ cho từng service (`architecture.md` rule, `system-design` skill). Mỗi pattern: **giải quyết vấn đề gì → khi dùng / tránh → đánh đổi → dùng ở đâu trong TeamBoard → học thêm**.

**Nguồn:** khung nội dung lấy từ slide ".NET 8 Microservices" (xem `references.md`), kiến thức kiến trúc phổ biến (Martin Fowler, Microsoft Architecture Guides, *Microservices Patterns*, *DDIA*...). "Mức phổ biến" là ước lượng theo kinh nghiệm chung, không phải số liệu thống kê. Kiểm tra tài liệu gốc trước khi dựa vào chi tiết.

## 0. Nguyên tắc đi kèm (từ slide, đáng nhớ trước khi chọn)

- **Monolith-first là mặc định hợp lý.** Chỉ tách microservice khi có lý do thật: triển khai độc lập không downtime, scale độc lập một phần, dữ liệu cần công nghệ khác nhau, đội tự chủ. Tránh **distributed monolith** (tách service nhưng vẫn phụ thuộc chặt) và **shared database**.
- **Không dùng microservice khi thiếu DevOps/đội nhỏ/sản phẩm mới còn đổi mạnh.**
- **Mỗi service một database** (database-per-service) và **polyglot persistence**; service chỉ lấy dữ liệu của service khác qua API hoặc sự kiện.
- **Ranh giới service từ Bounded Context** (DDD, Context Mapping); một bounded context có thể thành nhiều service tuỳ nhu cầu scale/độc lập.
- **Mỗi service chọn kiến trúc nội bộ theo bài toán của nó** (slide gọi là *multi-architectural polyglot microservices*): đây chính là mục tiêu học của dự án này.

> Dự án này **cố ý** tách nhiều service để luyện tập, nên mọi lựa chọn "microservice" ở đây mang nhãn `[Chọn để học]` (`decision-framework.md`). Một phương án thực tế hơn cho domain nhỏ là **modular monolith** (mục 1.4).

## 1. Kiến trúc bên trong một service (cấu trúc code)

| Pattern | Mức phổ biến | Vấn đề giải | Dùng khi | Tránh khi | Đánh đổi | Trong TeamBoard |
|---|---|---|---|---|---|---|
| **1.1 Layered / N-tier** (Presentation → Business → Data) | Rất cao | Tách trách nhiệm đơn giản | CRUD, domain đơn giản, đội mới | Domain phức tạp (logic rải ở service layer, phụ thuộc xuống DB) | Dễ hiểu; dễ thành "anemic domain", phụ thuộc hướng xuống hạ tầng | Làm mốc so sánh (slide dùng cho Discount.Grpc); không dùng cho service bắt buộc kiến trúc khác |
| **1.2 Clean / Onion / Hexagonal** (Domain ở trung tâm, phụ thuộc hướng vào trong) | Cao | Domain độc lập framework/DB/UI, dễ test | Domain có quy tắc nghiệp vụ đáng kể, sống lâu | CRUD thuần (thừa nghi thức) | Nhiều lớp, nhiều ánh xạ; cần kỷ luật | `board-service` (Clean), `notification-service` (Hexagonal) |
| **1.3 Vertical Slice** (tổ chức theo tính năng, mỗi use case một lát: endpoint + handler + validator) | Cao (đặc biệt cùng MediatR/Minimal API) | Giảm lan truyền thay đổi giữa tầng; code theo use case | Nhiều use case độc lập, đội muốn thay đổi cục bộ | Logic nghiệp vụ dùng chung rất nhiều giữa use case (cần rút ra miền) | Có thể lặp code; cần tách phần miền khi phình | `identity-service` (Vertical Slice) |
| **1.4 Modular Monolith** (một deployable, ranh giới module rõ, DB schema riêng mỗi module) | Cao và đang lên | Giữ lợi ích ranh giới rõ mà không chịu chi phí phân tán | Domain còn học, đội nhỏ, cần nhất quán mạnh | Cần scale/triển khai độc lập từng phần ngay | Một deployable; cần kỷ luật để module không rò rỉ | **Phương án đối chứng** nên hiểu; có thể làm bài tập: dựng cùng domain dạng modular monolith rồi so với microservice |
| **1.5 DDD tactical** (Entity, Value Object, Aggregate, Domain Event, Repository, strongly-typed ID) | Cao (với domain phức tạp) | Mô hình hoá nghiệp vụ, bảo vệ bất biến | Quy tắc nghiệp vụ phức tạp, nhiều trạng thái | CRUD đơn giản | Đường cong học; mapping ORM | Domain layer của `board-service`/`task-service` |
| **1.6 Rich vs Anemic domain model** | Chủ đề then chốt | Entity chứa hành vi hay chỉ dữ liệu | Rich: logic nghiệp vụ gắn với entity (`order.AddItem`) | Anemic chấp nhận được với CRUD | Rich khó thiết kế hơn nhưng nhất quán | Ưu tiên rich ở service Clean/DDD |

**Đọc thêm:** *Clean Architecture* (R. Martin), *Domain-Driven Design* (E. Evans), *Implementing DDD* (V. Vernon), tài liệu Microsoft ".NET Microservices: Architecture for Containerized .NET Applications" (`Chắc` về sự tồn tại).

## 2. Mô hình dữ liệu và xử lý lệnh/truy vấn

| Pattern | Phổ biến | Vấn đề giải | Dùng khi | Tránh khi | Đánh đổi | Trong TeamBoard |
|---|---|---|---|---|---|---|
| **2.1 CQRS** (tách lệnh ghi và truy vấn đọc, thường qua mediator/handler) | Cao | Mô hình đọc/ghi khác nhau, tối ưu độc lập | Nhiều view đọc, tải đọc/ghi lệch, validation/logic theo use case | Domain đơn giản | Thêm đường đi; nhất quán cuối cùng nếu tách kho đọc | `task-service` |
| **2.2 Event Sourcing** (lưu chuỗi sự kiện thay vì trạng thái) | Trung bình, chuyên biệt | Lịch sử đầy đủ, audit, tái dựng trạng thái | Nghiệp vụ gắn sự kiện, cần kiểm toán | Domain đơn giản, đội chưa quen | Tiến hoá schema sự kiện, projection, truy vấn khó | `task-service` `[Chọn để học]` |
| **2.3 Repository + Unit of Work** | Rất cao | Che giấu truy cập dữ liệu | Hầu hết service có DB | Khi ORM (EF `DbContext`) đã là UoW và repo chỉ bọc mỏng (thừa) | Có thể thừa một tầng | Chọn có chủ đích, tránh bọc thừa |
| **2.4 Document DB trên SQL** (JSON column; slide dùng Marten trên PostgreSQL) | Trung bình | Linh hoạt schema nhưng vẫn có giao dịch | Dữ liệu dạng tài liệu, cần giao dịch ACID | Quan hệ phức tạp/đa chiều | Truy vấn tài liệu khác SQL thường | Tuỳ chọn cho dữ liệu dạng hồ sơ |
| **2.5 Cache-aside + Decorator** (slide: cached repository bọc repository bằng Decorator/Scrutor, Redis) | Rất cao (cache-aside) | Giảm tải/độ trễ đọc | Đọc nhiều, chịu được độ cũ | Dữ liệu thay đổi liên tục, đọc ít | Vô hiệu hoá cache, thêm thành phần vận hành | Chưa cần (tải thấp); bài tập học Decorator + cache-aside |
| **2.6 Database-per-service / Polyglot persistence** | Nguyên tắc nền | Độc lập dữ liệu | Luôn với microservice | – | Nhất quán xuyên service khó hơn | Mọi service (`architecture.md`) |
| **2.7 Transactional Outbox / Inbox** | Rất cao khi có event | Ghi dữ liệu và phát sự kiện nguyên tử; xử lý trùng | Mọi service phát sự kiện | – | Thêm bảng và tiến trình publisher | `task-service`, `board-service` (`messaging.md`) |

## 3. Giao tiếp và tích hợp

| Pattern | Phổ biến | Vấn đề giải | Dùng khi | Tránh khi | Đánh đổi | Trong TeamBoard |
|---|---|---|---|---|---|---|
| **3.1 REST đồng bộ** | Rất cao | Giao tiếp yêu cầu-phản hồi đơn giản | Truy vấn, API công khai | Chuỗi gọi dài (nhân độ trễ và lỗi) | Ghép nối thời gian | Qua gateway; `api-endpoints.md` |
| **3.2 gRPC** (nhị phân, hợp đồng Protobuf, HTTP/2) | Cao (nội bộ) | Giao tiếp nội bộ hiệu năng cao, hợp đồng chặt | Service-to-service tần suất cao, độ trễ thấp | API công khai cho trình duyệt (cần gRPC-Web) | Ít thân thiện debug, ràng buộc công cụ | Chưa dùng; **ứng viên bài tập** (kiểu Discount.Grpc của slide) |
| **3.3 Pub/Sub (event-driven)** (SNS/SQS; slide: RabbitMQ + MassTransit) | Rất cao | Tách thời gian/độ sẵn sàng, fan-out | Sự kiện nghiệp vụ nhiều bên quan tâm | Cần phản hồi ngay | Nhất quán cuối cùng; idempotent | `teamboard-events` (`messaging.md`) |
| **3.4 Saga** (choreography/orchestration) | Cao khi có quy trình nhiều bước | Nhất quán xuyên service không có giao dịch chung | Quy trình nhiều service có bồi hoàn | Quy trình trong một service | Phức tạp theo dõi/gỡ lỗi | "Xoá Board" (bài tập) |
| **3.5 API Gateway / BFF** (YARP: reverse proxy, định tuyến, rate limit; BFF riêng cho từng loại client) | Rất cao | Điểm vào thống nhất, ẩn topology | Nhiều service/nhiều loại client | Một service đơn lẻ | Điểm lỗi tập trung nếu thiết kế kém | `gateway` (YARP) |
| **3.6 Anti-Corruption Layer (ACL)** | Cao khi tích hợp hệ cũ/ngoài | Bảo vệ mô hình miền khỏi mô hình bên ngoài | Tích hợp hệ thống cũ/của người khác | Không có ranh giới ngoài | Thêm lớp dịch | Dùng khi tách khỏi `teamboard-api` cũ hoặc tích hợp bên thứ ba |
| **3.7 Strangler Fig** (thay dần monolith bằng service mới sau một mặt tiền) | Cao (di chuyển hệ cũ) | Di chuyển từng phần an toàn | Có monolith đang chạy | Hệ mới hoàn toàn | Chạy song song, chi phí tạm thời | **`teamboard-api` hiện tại là monolith**: có thể coi việc tách thành `board-service`/`task-service` là một bài tập Strangler |

## 4. Độ tin cậy, quan sát, vận hành

| Pattern | Phổ biến | Dùng ở | Tài liệu |
|---|---|---|---|
| Retry + backoff + jitter, Circuit Breaker, Bulkhead, Timeout, Fallback | Rất cao | Mọi call ngoài (`backend-engineering.md` mục 8) | `resilience.md` |
| Health checks (liveness/readiness) | Rất cao | Mọi service | `observability.md` |
| Distributed tracing, structured logging, metrics (OpenTelemetry) | Rất cao | Mọi service | `observability.md` |
| Idempotent consumer, DLQ, redelivery | Rất cao | Consumer SQS | `messaging.md` |
| Feature flags, config theo môi trường | Cao | `web`, service | `frontend-engineering.md` |

## 5. Tính toán và triển khai

| Pattern | Phổ biến | Vấn đề giải | Dùng khi | Ghi chú |
|---|---|---|---|---|
| **Container trên ECS/Kubernetes** | Rất cao | Triển khai nhất quán | Dịch vụ chạy liên tục | ADR 0003 (ECS Fargate) |
| **Serverless / event-driven functions** (Lambda + SQS/SNS/DynamoDB) | Cao | Trả theo dùng, ít vận hành | Tải thất thường, xử lý sự kiện | `activity-service` |
| **Sidecar / Ambassador / Service mesh** | Trung bình (nâng cao) | Đẩy mối quan tâm chéo (mTLS, retry, quan sát) ra ngoài ứng dụng | Nhiều service, cần chính sách mạng đồng nhất | Chưa cần; ADOT collector dạng sidecar là ví dụ nhỏ (`observability.md`) |
| **Cell-based / multi-region** | Thấp (rất lớn) | Cô lập sự cố, mở rộng địa lý | Quy mô lớn | Ngoài phạm vi hiện tại |
| **Multi-tenancy** | Tuỳ sản phẩm | Phục vụ nhiều khách trên một hệ | SaaS | Nếu spec có yêu cầu thì thiết kế riêng |

## 6. Đối chiếu slide ↔ TeamBoard

| Service của slide (eShop) | Kiến trúc của slide | Tương ứng/khác trong TeamBoard |
|---|---|---|
| Catalog | Vertical Slice + CQRS (MediatR) + Marten (PostgreSQL document) + Minimal API (Carter) | `identity-service` dùng Vertical Slice; cân nhắc Minimal API thuần |
| Basket | Vertical Slice + Redis cache-aside qua Decorator; phát sự kiện checkout | Bài tập cache-aside/decorator; phát sự kiện qua SNS thay RabbitMQ |
| Discount | N-layer + gRPC + SQLite | Mốc so sánh Layered; gRPC là ứng viên bài tập |
| Ordering | DDD + CQRS + Clean Architecture (EF Core, SQL Server); Domain Event vs Integration Event; EF interceptor để dispatch | `board-service`/`task-service`; chú ý **Domain Event (trong một service)** khác **Integration Event (giữa service, qua bus)** |
| Giao tiếp bất đồng bộ | RabbitMQ + MassTransit (pub/sub, topic exchange); nêu Transactional Outbox | SNS → SQS + Outbox; **ta tự viết lớp nhắn tin mỏng** hay dùng thư viện là một quyết định (chưa chốt) |
| API Gateway | YARP (route theo `/catalog`, `/shoppingcart`, `/ordering`), rate limiting | `gateway` YARP; lưu ý `api-endpoints.md` bắt buộc không có tên service trong path, nên khác slide ở điểm này |
| Client | Shopping Web (Refit) | `web` Angular qua gateway |

## 7. Gợi ý bổ sung cho lộ trình học (không bắt buộc)

Theo thứ tự giá trị học, kèm chi phí:
1. **Modular monolith đối chứng** của cùng domain Board/Task (so sánh độ phức tạp vận hành, tốc độ phát triển).
2. **Strangler Fig** từ `teamboard-api` hiện có sang `board-service` (có thật, không cần dựng giả).
3. **gRPC** cho một call đồng bộ nội bộ (so với REST: hợp đồng, hiệu năng, debug).
4. **Cache-aside + Decorator** với Redis cho một truy vấn đọc nhiều (nhớ chi phí chạy rỗng của ElastiCache: `aws-service-selection`).
5. **BFF riêng cho `web`** nếu gateway bắt đầu chứa logic riêng cho UI.

Mỗi bài tập cần issue riêng và qua `workflow`; chọn dịch vụ AWS qua `aws-service-selection` (chi phí đi đầu).

## Lưu ý về thư viện trong slide

Slide .NET 8 dùng MediatR, MassTransit, FluentValidation, Mapster, Carter, Marten... Một số đã chuyển sang giấy phép thương mại (MediatR, MassTransit): **học pattern, không bắt buộc dùng thư viện**. Chính sách: `.agents/rules/dependency-policy.md`, ADR 0007.
