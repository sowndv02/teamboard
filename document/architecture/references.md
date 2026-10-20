# Tài liệu tham khảo

Nguồn dùng để viết các tài liệu kiến trúc, kèm cách dùng và mức tin cậy. Nội dung ở đây là **ghi chú học tập, không phải nguồn chuẩn tắc**: quyết định của dự án nằm ở ADR.

## 1. Slide (H:\learning\coding\, tài liệu cá nhân của chủ dự án)

| Tài liệu | Nội dung thực tế | Dùng cho | Ghi chú |
|---|---|---|---|
| *Securing Microservices with IdentityServer4 and Ocelot* (M. Ozkaya) | **Dàn ý slide** (12 section): JWT, OAuth2, OpenID Connect, IdentityServer4, bảo vệ API, MVC client, hybrid flow, claim-based authorization, Ocelot gateway; ít chữ, phần lớn là demo | `identity-learning.md` | IdentityServer4 đã hết hỗ trợ; chỉ học khái niệm. Slide không có chi tiết cài đặt, nên lộ trình xây tự làm dựa trên RFC và OpenID Connect |
| *Microservices Observability, Resilience, Monitoring on .NET* (M. Ozkaya) | **Dàn ý slide**: logging phân tán (Elasticsearch/Kibana + Serilog), Retry/Circuit Breaker/Bulkhead (Polly), health checks (WatchDog), tracing (OpenTelemetry + Zipkin); dẫn tới tài liệu Microsoft | `observability.md`, `resilience.md` | Slide liệt kê chủ đề; chi tiết kỹ thuật ở các trang Microsoft Learn mà slide dẫn (".NET Microservices Architecture", "Cloud-native: observability/resiliency patterns") |
| *.NET 8 Microservices* slides (M. Ozkaya) | Slide đầy đủ nhất: nguyên tắc microservice, khi nào dùng/không, database-per-service, bounded context; .NET 8/C# 12; Catalog (Vertical Slice + CQRS + Marten), Basket (Redis, Decorator), Discount (gRPC), Ordering (DDD + CQRS + Clean), RabbitMQ + MassTransit, Outbox, YARP gateway | `patterns-catalog.md` | Chỉ mang tính tham khảo (theo yêu cầu). Các thư viện cụ thể (MediatR, Carter, Marten, MassTransit...) không bắt buộc cho dự án này |

Mã nguồn đi kèm slide (kho `aspnetrun` trên GitHub) là tài liệu học đáng đọc nhưng **chưa được tôi xem** (chỉ đọc slide); kiểm tra trực tiếp trước khi dựa vào.

Trong cùng thư mục còn hai file chưa dùng: `Microservices+Remastered.pdf`, `mma+slides.pdf` (chưa đọc, có thể bổ sung sau).

## 2. Chuẩn và đặc tả công khai (đọc bản gốc, kiểm tra số hiệu và trạng thái hiện hành)

| Chủ đề | Tài liệu | Tin cậy |
|---|---|---|
| JWT, JWS, JWK | RFC 7519, RFC 7515, RFC 7517 | Chắc |
| OAuth 2.0 | RFC 6749; PKCE RFC 7636; Token revocation RFC 7009; Introspection RFC 7662 | Chắc |
| OAuth Security BCP, OAuth 2.1 | Bản BCP mới nhất (số RFC cần kiểm), dự thảo OAuth 2.1 | Vừa về số hiệu |
| JWT access token profile | RFC 9068 | Vừa |
| OpenID Connect | OpenID Connect Core 1.0, Discovery 1.0 | Chắc |
| OWASP | Top 10, ASVS, Password Storage Cheat Sheet, Cheat Sheet Series | Chắc |
| W3C Trace Context | `traceparent` | Chắc |
| OpenTelemetry | Tài liệu OpenTelemetry và .NET SDK | Chắc |

## 3. Sách và tài liệu kiến trúc

| Nguồn | Dùng cho |
|---|---|
| *Designing Data-Intensive Applications* — M. Kleppmann | Dữ liệu phân tán, nhất quán |
| *Microservices Patterns* — C. Richardson; microservices.io | Saga, outbox, CQRS |
| *Building Microservices* — S. Newman | Ranh giới service, vận hành |
| *Domain-Driven Design* — E. Evans; *Implementing DDD* — V. Vernon | DDD chiến lược và chiến thuật |
| *Clean Architecture* — R. C. Martin | Clean/Hexagonal |
| *Release It!* — M. Nygard | Resilience |
| Microsoft ".NET Microservices: Architecture for Containerized .NET Applications" và mục Cloud-native | Kiến trúc .NET, resilience, observability |
| AWS Well-Architected Framework, AWS Builders' Library, tài liệu CloudWatch/X-Ray/ADOT/ECS | Kiến trúc và vận hành trên AWS |

## 4. Tài liệu trong repo dùng các nguồn trên

- [patterns-catalog.md](patterns-catalog.md): danh mục kiến trúc/pattern
- [identity-learning.md](identity-learning.md): lộ trình tự xây `identity-service`
- [observability.md](observability.md): log/metric/trace/health, local và AWS
- [resilience.md](resilience.md): pattern chịu lỗi
- [adr/0006-identity-self-built.md](adr/0006-identity-self-built.md): quyết định tự xây identity
