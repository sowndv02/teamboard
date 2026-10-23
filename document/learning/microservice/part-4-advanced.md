# Part 4 — Advanced (Microservice)

> [!NOTE]
> **Mục tiêu:** giữ nhất quán xuyên service khi không còn giao dịch ACID (outbox, saga) và giữ hệ thống sống khi một service lỗi (timeout, retry, circuit breaker, bulkhead).
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** giải thích vì sao ghi DB rồi publish mất event và outbox sửa thế nào, thiết kế một saga có bồi hoàn, tính nhân retry nhiều tầng, chọn nơi đặt bulkhead.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Bốn công cụ nhất quán và chịu lỗi

#### Transactional outbox
**Là gì:** ghi event vào bảng `outbox` **cùng giao dịch** với thay đổi dữ liệu; một tiến trình relay đọc bảng rồi publish.
**Use case:** `board-service` tạo Board và phát `BoardCreated` mà không bao giờ có Board mà thiếu event hoặc ngược lại.

#### Saga
**Là gì:** quy trình nhiều service là **chuỗi giao dịch cục bộ**; bước sau lỗi thì chạy giao dịch **bồi hoàn** để hoàn tác các bước trước.
**Use case:** "Xoá Board": huỷ Task, xoá feed, xoá Board; bước cuối lỗi thì khôi phục hai bước đầu.

#### Retry, circuit breaker
Retry: thử lại có kiểm soát. Circuit breaker: ngắt tạm lời gọi tới đích đang lỗi (Closed, Open, Half-Open).
**Use case:** lỗi thoáng qua của một `GET` được thử lại; đích lỗi liên tục thì ngắt để khỏi nhân tải.

#### Bulkhead
**Là gì:** chia tài nguyên (kết nối, luồng, hàng đợi) thành các khoang riêng cho từng đích để một đích chậm không chiếm hết.
**Use case:** gọi `identity-service` và `task-service` từ cùng một service mà `identity-service` chậm không làm `task-service` cạn kết nối.

## L2 Why — Vì sao cần các công cụ này

Không có giao dịch nào bao được cả DB lẫn broker (2PC "không phải một lựa chọn" theo microservices.io), nên ghi rồi gửi là hai việc có khoảng hở. Không có giao dịch ACID xuyên service, nên quy trình nhiều bước cần bồi hoàn. Và gọi đồng bộ mà không giới hạn thì một đích chậm kéo cạn tài nguyên rồi lan ngược lên mọi người gọi (Lab 4.3, 4.4).

## L3 How — Cơ chế đáng nhớ

### Dual write và outbox
**Vấn đề:** lưu DB rồi publish: chết giữa hai bước thì event **mất**; publish rồi lưu: DB lỗi thì event **nói dối**.
```
service: [ giao dịch DB ]  ghi dữ liệu  +  ghi dòng "event cần gửi" vào bảng outbox   (nguyên tử)
relay nền: đọc outbox ─► publish lên broker ─► đánh dấu đã gửi   (chết giữa chừng thì gửi lại)
```
- **Ưu:** event chỉ phát nếu giao dịch commit; giữ thứ tự trong cùng service. **Nhược:** thêm bảng và tiến trình relay; dễ quên ghi outbox; **relay có thể gửi trùng** nên phía nhận phải idempotent (Part 3).
- **Cách relay đọc:** quét `sent = 0` theo `event_id` tăng dần; sau khi publish mới đánh dấu; chết giữa chừng thì lần quét sau gửi lại (Lab 4.1). Với NoSQL, outbox là một thuộc tính của bản ghi; với change feed (Cosmos DB) hoặc CDC có thể đọc từ log thay vì quét bảng.
- **Dọn dẹp:** xoá hoặc TTL các dòng đã gửi (Azure gợi ý khoảng 10 ngày ở ví dụ Cosmos).

### Saga
```
Choreography:  A xong ─event─► B xong ─event─► C lỗi ─event─► B bồi hoàn ─► A bồi hoàn
Orchestration: Orchestrator ─lệnh─► A ─► B ─► C (lỗi) ─► ra lệnh bồi hoàn B, rồi A
```
- **Choreography:** các service tự phản ứng với event của nhau. **Ưu:** đơn giản khi ít service, không có điểm trung tâm. **Nhược:** quy trình rối khi thêm bước, nguy cơ phụ thuộc vòng, test tích hợp khó.
- **Orchestration:** một bộ điều phối ra lệnh từng bước. **Ưu:** hợp quy trình phức tạp, tránh vòng, trách nhiệm rõ. **Nhược:** thêm một thành phần, điều phối có thể là điểm lỗi.
- **Bồi hoàn không phải rollback:** nó là nghiệp vụ riêng, không đưa về trạng thái gốc nếu có thay đổi xen vào; **phải idempotent** vì bản thân bồi hoàn cũng có thể lỗi; ghi lại tiến trình để chạy tiếp; xác định **điểm không quay lại (pivot)**, các bước sau pivot phải thử lại được (Azure).
- **Thiếu isolation:** người khác nhìn thấy trạng thái trung gian (Lab 4.2); đối sách: khoá ngữ nghĩa (trạng thái "đang xoá"), cập nhật giao hoán, đọc lại giá trị.
- **Báo kết quả bất đồng bộ cho client:** trả `202` kèm id và để client hỏi `GET /…/{id}` hoặc nhận thông báo đẩy.

### Retry, circuit breaker
- **Retry:** chỉ cho thao tác **idempotent**; backoff mũ với jitter; **không lồng retry** ở nhiều tầng (Azure Retry).
- **Nhân retry nhiều tầng:** mỗi tầng retry N lần thì số lần thử là N^tầng: 3 tầng × 4 lần = 64 lần lên backend đã quá tải (sách Google SRE); AWS Builders' Library: ngăn xếp 5 tầng, 3 lần mỗi tầng = 243 lần (ví dụ minh hoạ). Số đo cho trường hợp gateway ở [API Gateway Part 4, Lab 4.3](../api-gateway/part-4-advanced.md). Chỉ retry **một chỗ** trong ngăn xếp và giới hạn bằng ngân sách retry.
- **Circuit breaker:** ba trạng thái; Closed đếm lỗi trong cửa sổ, vượt ngưỡng thì Open trong một khoảng, rồi Half-Open cho vài yêu cầu thử; thành công thì Closed. Một breaker cho mỗi tài nguyên độc lập. **Không** thay cho xử lý lỗi nghiệp vụ và không cần cho luồng event đã có DLQ. Breaker cũng có thể lỗi (Discord, [Caching Part 7](../caching/part-7-troubleshooting-mastery.md)). ⚠ Lab không chạy breaker thật.

### Bulkhead
Tách tài nguyên theo đích (pool kết nối riêng cho từng service gọi, hàng đợi riêng cho từng nhóm consumer); kết hợp với timeout, retry, breaker. **Cái giá:** dùng tài nguyên kém hiệu quả hơn (khoang rảnh không cho khoang bận mượn) và phức tạp hơn.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0. Outbox dùng **giao dịch SQLite thật**; sự cố là ngoại lệ được tiêm; saga và bulkhead là **mô hình** trong bộ nhớ.

#### Lab 4.1 — Dual write và outbox (1000 lần tạo Task, 20% sự cố giữa hai bước)
**Đầu ra thật:**
```text
dual write (ghi DB rồi publish):  Task trong DB 1000, event đã publish 776  -> mất event: 224
publish trước, ghi DB sau:        event đã publish 1000, Task trong DB 776  -> event nói dối: 224
outbox:  Task trong DB 776, dòng outbox 776 (luôn bằng nhau); sự cố trước commit làm cả hai cùng không ghi: 224 lần
         relay gửi 853 message cho 776 event (chết 77 lần giữa publish và đánh dấu) -> mất 0, trùng 77
```
**Giải thích:** dual write mất 224 event (hoặc nói dối 224 lần nếu đảo thứ tự). Outbox làm Task và dòng outbox cùng có hoặc cùng không; relay không mất event nhưng **gửi trùng 77** khi chết giữa publish và đánh dấu, nên consumer phải idempotent (Dự án 6.1 nối cả hai).

#### Lab 4.2 — Saga "Xoá Board" (1000 saga, bước 3 lỗi 30%)
**Đầu ra thật:**
```text
không bồi hoàn: saga thành công 698, lỗi 302; Board còn nhưng Task đã huỷ hoặc feed đã xoá (không nhất quán): 302
có bồi hoàn:    saga thành công 698, lỗi 302; không nhất quán: 0
-- một reader đọc trạng thái trong lúc saga chạy (mỗi bước 100 ms):
   reader thấy: Task còn, feed còn, Board còn
   reader thấy: Task đã huỷ, feed còn, Board còn
   reader thấy: Task đã huỷ, feed đã xoá, Board còn
   reader thấy: Task đã huỷ, feed đã xoá, Board đã xoá
```
**Giải thích:** không bồi hoàn thì mọi saga lỗi để lại Board mất dữ liệu liên quan; có bồi hoàn thì 0 trường hợp lệch. Reader thấy hai trạng thái trung gian ("Board còn nhưng Task đã huỷ"): đó là **thiếu isolation**. Lab không mô phỏng bồi hoàn tự lỗi hay lỗi giữa chừng của orchestrator.

#### Lab 4.3 — Nhân retry nhiều tầng
Đã đo ở [API Gateway Part 4, Lab 4.3](../api-gateway/part-4-advanced.md): 1 tầng × 3 lần = 3; 2 tầng = 9; 3 tầng = 27; 3 tầng × 4 lần = 64 lần backend nhận cho một yêu cầu của người dùng (mô hình phép nhân, không phải đo hệ thật). Không chạy lại ở đây.

#### Lab 4.4 — Bulkhead: X chậm có chiếm hết pool dùng chung không? (mô hình)
100 lời gọi X (chậm 500 ms) và 30 lời gọi Y (5 ms); pool dùng chung 10 chỗ, Y chờ tối đa 200 ms.
**Đầu ra thật:**
```text
pool dùng chung (không bulkhead): Y thành công 0/30, bị từ chối sau 200 ms chờ 30
bulkhead (X tối đa 5, Y tối đa 5): Y thành công 30/30, bị từ chối 0, p95 93 ms
```
**Giải thích:** không bulkhead thì X chiếm cả 10 chỗ và Y (vốn nhanh) hoàn toàn không chạy được; có bulkhead thì Y không bị ảnh hưởng. Đổi lại X chỉ dùng 5 chỗ dù pool còn rảnh. Mô hình `SemaphoreSlim`, không có mạng.

## L5 Troubleshooting — Nhất quán và chịu lỗi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Có Board mà không có thông báo | dual write mất event (Lab 4.1) | so số bản ghi với số event | outbox cùng giao dịch |
| Thông báo cho Board không tồn tại | publish trước, DB lỗi sau (Lab 4.1) | so thứ tự publish và commit | outbox; không publish trước commit |
| Outbox có dòng nhưng không event nào ra | relay chết hoặc kẹt | số dòng `sent = 0`, tuổi dòng cũ nhất | cảnh báo trên tuổi outbox; giám sát relay |
| Cùng event tới consumer nhiều lần | relay gửi trùng khi chết giữa chừng | đếm theo `eventId` | consumer idempotent (Part 3) |
| Dữ liệu liên quan mất mà chính nó còn | saga lỗi giữa chừng không bồi hoàn (Lab 4.2) | trạng thái từng bước | bước bồi hoàn idempotent, ghi tiến trình |
| Người dùng thấy trạng thái nửa vời | thiếu isolation của saga | đọc trong lúc saga chạy | trạng thái "đang xoá", khoá ngữ nghĩa |
| Tải backend tăng gấp nhiều lần khi sự cố | retry nhiều tầng (Lab 4.3) | đếm lượt nhận ở backend | retry một chỗ, ngân sách, backoff có jitter |
| Một đích chậm làm cả service ngừng | không có bulkhead (Lab 4.4) | pool kết nối đang chiếm bởi đích nào | pool riêng cho từng đích |
| Breaker không bao giờ đóng lại | không có Half-Open hoặc ngưỡng sai | trạng thái breaker | Half-Open với yêu cầu thử, theo dõi |

## L6 Production — Quyết định khi chạy thật

- **Outbox:** có chỉ số tuổi dòng cũ nhất và số dòng chưa gửi; cảnh báo khi relay chậm; dọn dòng đã gửi; quyết định có đảm bảo thứ tự theo thực thể không; relay chạy như một thành phần có giám sát và có thể nhiều bản (cần khoá hoặc phân vùng để không gửi trùng quá mức).
- **Saga:** mỗi bước và bước bồi hoàn **idempotent** và có id quy trình; lưu trạng thái saga bền (không chỉ trong bộ nhớ); điểm không quay lại rõ; cách xử lý khi bồi hoàn thất bại (hàng chờ xử lý thủ công); mỗi saga có timeout toàn quy trình.
- **Retry:** chỉ ở một tầng, chỉ cho thao tác idempotent, có backoff và jitter, có ngân sách; test tải khi **tắt dependency** để thấy nhân tải.
- **Breaker và bulkhead:** một breaker cho mỗi tài nguyên; cảnh báo khi breaker mở; kích thước khoang theo p99 và tải thật, không đoán.
- **Chiến lược test:** tiêm lỗi giữa hai bước (như Lab 4.1) vào test của chính service.

## L7 Architecture — Chọn cách nhất quán và chịu lỗi

| Bài toán | Chọn | Cái giá |
|---|---|---|
| Lưu dữ liệu và báo service khác | transactional outbox | thêm bảng và relay; có thể gửi trùng |
| Quy trình nhiều service, ít bước | saga choreography | khó nhìn toàn cảnh khi lớn lên |
| Quy trình nhiều service, phức tạp | saga orchestration | thêm thành phần điều phối |
| Cần nhất quán mạnh, các phần luôn đi cùng nhau | một service, một DB, giao dịch ACID | không tách độc lập |
| Lỗi thoáng qua của `GET` | retry một tầng có ngân sách | nhân tải nếu thêm tầng khác |
| Đích lỗi liên tục | circuit breaker | thêm trạng thái; breaker cũng có thể lỗi |
| Một đích chậm không được ảnh hưởng đích khác | bulkhead | dùng tài nguyên kém hiệu quả hơn |
| Lịch sử sự kiện là nguồn sự thật | event sourcing (ADR của `task-service`) | ⚠ chưa chạy; phức tạp |
| **Tránh saga** | gộp thao tác vào một service | ranh giới service phải điều chỉnh |

**Dấu hiệu ranh giới sai:** nếu hầu hết thao tác cần saga hoặc nhiều service luôn đổi cùng nhau, hai service đó có lẽ nên là một.

## Tự kiểm tra

1. Vì sao outbox vẫn gửi trùng, và ai chịu trách nhiệm xử lý?
2. Choreography và orchestration khác nhau ở đâu, chọn khi nào?
3. Vì sao bồi hoàn không phải rollback?
4. Retry 3 tầng, mỗi tầng 4 lần thì backend nhận bao nhiêu lần?
5. Bulkhead đổi cái gì lấy cái gì?

<details>
<summary>Đáp án ngắn</summary>

1. Relay có thể chết giữa publish và đánh dấu; consumer idempotent theo `eventId` (Lab 4.1, Part 3).
2. Choreography: tự phản ứng theo event, hợp ít bước; orchestration: một bộ điều phối, hợp quy trình phức tạp.
3. Bồi hoàn là nghiệp vụ riêng, có thể lỗi, không khôi phục trạng thái gốc nếu có thay đổi xen vào; phải idempotent.
4. 64 lần (4 × 4 × 4).
5. Cô lập đích chậm; đổi lại dùng tài nguyên kém hiệu quả hơn (Lab 4.4).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 (giao dịch SQLite thật, sự cố được tiêm), Lab 4.2 và 4.4 (mô hình trong bộ nhớ); đầu ra thật. Lab 4.3 tham chiếu số đã đo ở API Gateway Part 4.
**Đã tự mở:** Google SRE "Addressing Cascading Failures" (ví dụ 4×4×4 = 64, đã mở ở các phiên trước); microservices.io `/patterns/microservices.html`.
**Chỉ qua agent (⚠):** microservices.io `transactional-outbox` và `saga`; Azure patterns Saga, Compensating Transaction, Circuit Breaker, Bulkhead, Retry, Transactional Outbox (Cosmos); AWS Builders' Library về retry (243 lần là ví dụ minh hoạ).
**Chưa kiểm:** circuit breaker thật (Polly), saga với bồi hoàn lỗi hay orchestrator chết, outbox với nhiều relay và CDC, event sourcing.
