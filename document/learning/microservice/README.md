# Microservice: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu microservice như **một lựa chọn kiến trúc có giá**, nắm từng cơ chế sinh ra khi tách service (database-per-service, giao tiếp đồng bộ và bất đồng bộ, outbox, idempotent consumer, saga, timeout/retry/bulkhead, quan sát), tự chứng minh bằng lab, và biết lúc nào **không** nên tách.
> **Cần biết trước:** HTTP, C# cơ bản, khái niệm cơ sở dữ liệu và giao dịch. Chưa cần biết AWS.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Loại:** khái niệm (kiểu kiến trúc). Liên quan: [API Gateway](../api-gateway/README.md) (cửa vào), [Caching](../caching/README.md) và [Valkey](../valkey/README.md) (giảm tải và chia sẻ trạng thái), [Docker](../docker/README.md) (đóng gói service).

## Đây không phải mặc định tốt hơn

Microservice **không** là bản nâng cấp của monolith. Fowler gọi chi phí này là "phí bảo hiểm" (MicroservicePremium) và khuyên đa số hệ thống nên bắt đầu bằng monolith chia module rõ (MonolithFirst); Segment gộp hơn 140 service lại thành một; Shopify chọn modular monolith; Istio gộp control plane thành một binary. Lộ trình dạy **cả** cách dùng lẫn lúc không dùng. TeamBoard chia 6 service **có chủ ý để học** (ADR 0001), không phải khuyến nghị cho một domain nhỏ.

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự triển khai và đo bằng code hoặc lệnh thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác (hoặc không dùng)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | monolith, modular monolith, microservice; ranh giới; database-per-service; đồng bộ và bất đồng bộ; Bản đồ giải pháp | Giải thích và chọn được có tách service hay không |
| [2 Core skills](part-2-core-skills.md) | gọi service-to-service, timeout, hợp đồng lỗi và versioning, sở hữu dữ liệu, correlation | Gọi nhau đúng và lần ra được lỗi |
| [3 Multi-component](part-3-multi-component.md) | broker, at-least-once, thứ tự, idempotent consumer, DLQ | Nhận event đúng khi bị giao trùng |
| [4 Advanced](part-4-advanced.md) | outbox, saga, retry và circuit breaker, bulkhead | Giữ nhất quán và chịu lỗi xuyên service |
| [5 Production](part-5-production-security-ops.md) | quan sát, triển khai độc lập, phiên bản hợp đồng, chi phí vận hành | Vận hành nhiều service |
| [6 Projects](part-6-projects.md) | Board → Notification đầu-cuối, saga xoá Board (mô phỏng) | Áp lên TeamBoard có số đo |
| [7 Mastery](part-7-troubleshooting-mastery.md) | sự cố, case thực tế và phản ví dụ, phỏng vấn, bài mở | Tự đánh giá mức thành thạo |

## Ma trận phủ

`✓` là Part có mục tương ứng, `–` là không áp dụng (lý do ở dưới). Mỗi ô `✓` có đúng một mục `## Lx` trong file Part.

| Part | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 |
|---|---|---|---|---|---|---|---|---|
| 1 | ✓ | ✓ | ✓ | ✓ | – | – | ✓ | – |
| 2 | ✓ | ✓ | ✓ | ✓ | ✓ | – | – | – |
| 3 | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | – |
| 4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 5 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 6 | ✓ | – | – | ✓ | ✓ | – | – | – |
| 7 | – | – | – | – | ✓ | – | ✓ | ✓ |

**Lý do các ô `–`:**
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi hai service gọi nhau ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, .NET SDK 10.0.402 (runtime 10.0.13), ngày 23/10/2026; gói duy nhất ngoài .NET là `Microsoft.Data.Sqlite` 10.0.13 (giao dịch SQLite thật cho các lab outbox, idempotent consumer, đầu-cuối). Dự án thử **ngoài repo**; các service giả là Kestrel trên `127.0.0.1` hoặc đối tượng trong bộ nhớ; không dùng Docker.
**Quan trọng:** đây là **mô phỏng trong một tiến trình**. Các lab chứng minh *cơ chế* (mất event, trùng, nhân retry, bồi hoàn) bằng sự cố được tiêm có chủ ý, **không** chứng minh hành vi của SNS, SQS, Kafka hay nhiều tiến trình thật; mỗi lab nói rõ phần nào là HTTP và giao dịch thật, phần nào là mô hình. Số đo thời gian chỉ tương đối. **Chưa chạy (⚠):** SNS/SQS và LocalStack, nhiều tiến trình hay máy thật, circuit breaker thật (Polly), event sourcing, CQRS, service mesh.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: nêu khi nào tách service là hợp lý và khi nào không; giải thích vì sao mất giao dịch ACID xuyên service.
- [ ] L3: kể các cơ chế (database-per-service, đồng bộ/bất đồng bộ, outbox, idempotent consumer, saga, timeout/retry/bulkhead) và vấn đề mỗi cái giải quyết.
- [ ] L4: tái hiện dual write mất event và sửa bằng outbox; làm consumer idempotent trong một giao dịch; bồi hoàn một saga.
- [ ] L5: từ triệu chứng (event mất hoặc trùng, dữ liệu lệch, lỗi dây chuyền, không lần ra request) tìm nguyên nhân gốc.
- [ ] L6: nêu chỉ số, cách phát hành hợp đồng không phá consumer, chi phí vận hành.
- [ ] L7: chọn giữa monolith, modular monolith, microservice, serverless; giải thích vì sao Segment và Uber làm ngược nhau.
- [ ] L8: thiết kế ranh giới và giao tiếp cho một hệ mới kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Nguồn sự thật của dự án: [architecture/README.md](../../architecture/README.md), [ADR 0001](../../architecture/adr/0001-service-boundaries.md), [ADR 0002](../../architecture/adr/0002-communication.md), [resilience.md](../../architecture/resilience.md), [observability.md](../../architecture/observability.md), [patterns-catalog.md](../../architecture/patterns-catalog.md). Lộ trình **không** sửa các tài liệu đó; chỗ lệch chỉ được báo ở Part 6.
