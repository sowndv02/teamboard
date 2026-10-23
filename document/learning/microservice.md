# Kiến trúc microservice

> Loại: Khái niệm (kiểu kiến trúc; xem [Bản đồ giải pháp](#bản-đồ-giải-pháp) để thấy microservice chỉ là một lựa chọn) · Khái niệm liên quan: [Caching](caching.md) (một công cụ giảm tải cho service), [API Gateway](api-gateway.md) (cửa vào của hệ nhiều service)
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: giai đoạn 0, mới có `src/teamboard-api` (sẽ đổi tên `board-service`); các service còn lại chưa có code · Nguồn sự thật: `document/architecture/README.md`, ADR 0001, 0002

## 1. Vấn đề nó giải quyết

Một ứng dụng lớn chạy thành một khối (monolith): sửa một dòng ở module Thông báo vẫn phải build, test, deploy cả khối; một chỗ rò bộ nhớ làm sập mọi chức năng; 20 người cùng sửa một codebase giẫm chân nhau; muốn scale riêng chức năng đọc nhiều thì phải nhân bản cả ứng dụng. Microservice tách khối đó thành nhiều phần nhỏ triển khai độc lập để giảm các đau này, đổi lại bằng độ phức tạp của hệ phân tán. Đây là một **đánh đổi**, không phải bản nâng cấp: mục 7 có cả những công ty đã quay lại monolith.

## 2. Là gì (hiểu trong 1 phút)

Chia ứng dụng thành các **service** nhỏ, mỗi service: chạy tiến trình riêng, **sở hữu dữ liệu riêng**, bao quanh một năng lực nghiệp vụ, deploy độc lập, nói chuyện với nhau qua mạng (REST hoặc event).

```
Client ─► gateway ─┬─► identity-service ─► PostgreSQL
                   ├─► board-service    ─► SQL Server
                   └─► task-service     ─► PostgreSQL
                              │ event (SNS → SQS)
                              ├──► notification-service ─► DynamoDB
                              └──► activity-service     ─► DynamoDB
```

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ |
|---|---|---|
| Bounded context | Ranh giới nơi một thuật ngữ nghiệp vụ có nghĩa nhất quán (DDD) | "Board" và "Task" là hai ngữ cảnh |
| Database-per-service | Mỗi service sở hữu DB, không ai đọc DB của người khác | board-service không query DB của task-service |
| Đồng bộ vs bất đồng bộ | REST chờ phản hồi; event gửi rồi đi tiếp | tạo Task → phát `TaskAssigned` |
| Eventual consistency | Dữ liệu giữa service nhất quán sau một khoảng, không tức thì | feed hoạt động trễ vài giây |
| Outbox | Ghi event vào bảng cùng transaction với dữ liệu, rồi publisher gửi | tránh "dual write" mất event |
| Idempotent consumer | Nhận trùng một event vẫn chỉ tác động một lần | dedupe theo `eventId` |
| Saga | Chuỗi giao dịch cục bộ có bước bồi hoàn | "Xoá Board" → huỷ Task → dọn feed |
| Circuit breaker | Ngắt tạm call tới service đang lỗi để khỏi kéo sập chuỗi | call đồng bộ giữa service |
| Distributed monolith | Tách service nhưng vẫn dính chặt (deploy cùng nhau, chung DB) | dạng thất bại điển hình |
| Observability | Log/trace/metric có `correlationId` để lần ra request xuyên service | OpenTelemetry + Jaeger |

### 3.1 Các cơ chế và pattern chính

Mỗi mục dưới đây trả lời một vấn đề cụ thể sinh ra khi tách service. Nguồn: microservices.io (Richardson), Fowler/Lewis, Google SRE (agent đã mở; mức Vừa trừ khi ghi khác); phần áp dụng cho TeamBoard theo `.agents/rules/messaging.md`.

#### a. Giao tiếp đồng bộ (REST/gRPC) vs bất đồng bộ (event)
- **Đồng bộ:** gọi và **chờ** phản hồi (REST/JSON qua gateway; gRPC cho nội bộ). **Ưu:** đơn giản, cần kết quả ngay. **Nhược:** ghép nối thời gian: service kia chậm/chết thì mình hỏng; chuỗi gọi dài nhân độ trễ và lỗi. **Dùng khi:** truy vấn cần đáp án ngay, API công khai.
- **Bất đồng bộ:** gửi **event** qua broker (SNS → SQS) rồi đi tiếp; bên quan tâm tự xử lý. **Ưu:** tách thời gian và độ sẵn sàng, fan-out nhiều bên. **Nhược:** eventual consistency, trùng/lệch thứ tự, khó theo dõi. **Dùng khi:** sự kiện nghiệp vụ nhiều bên quan tâm.

#### b. Database-per-service
Mỗi service sở hữu DB riêng, không ai đọc DB của người khác; lấy dữ liệu của service khác qua API hoặc event.
- **Ưu:** deploy/scale/đổi schema độc lập; chọn công nghệ lưu trữ phù hợp từng service. **Nhược:** **không còn giao dịch ACID xuyên service** (kéo theo outbox, saga); truy vấn nối bảng xuyên service khó; dữ liệu nhân bản phải đồng bộ. Chung DB là dấu hiệu distributed monolith.

#### c. Transactional Outbox (giải quyết "dual write")
**Vấn đề:** khi tạo Task, service phải lưu DB **và** báo service khác. Làm tuần tự thì có khoảng hở: lưu xong mà chết trước khi publish thì event mất; publish trước mà lưu lỗi thì event nói dối. Không giao dịch nào bao được cả DB lẫn broker.
```
service: [ transaction DB ]  ghi dữ liệu  +  ghi dòng "event cần gửi" vào bảng outbox   (nguyên tử)
relay nền: đọc outbox ─► publish lên broker ─► đánh dấu đã gửi   (chết giữa chừng thì gửi lại)
```
- **Ưu:** event chỉ phát nếu transaction commit; giữ thứ tự trong cùng service. **Nhược:** thêm bảng và tiến trình relay; dễ quên ghi outbox; **relay có thể gửi trùng** nên phía nhận phải idempotent (mục d).
- **Dùng khi:** mọi service phát event kèm thay đổi dữ liệu (`task-service`, `board-service` trong TeamBoard).

#### d. Idempotent consumer (nhận trùng vẫn đúng)
Broker như SQS giao **ít nhất một lần**, nên cùng event có thể tới hai lần (và không giả định thứ tự). **Idempotent**: xử lý hai lần cho kết quả như một lần.
- **Cách làm thường gặp:** lưu `eventId` đã xử lý, thấy lại thì bỏ qua. **Chỗ tinh tế:** "đánh dấu đã xử lý" và "thực hiện tác động" phải nhất quán: đánh dấu rồi chết trước khi làm xong thì **mất việc**; làm xong rồi chết trước khi đánh dấu thì **làm lại**. Cần đặt hai việc trong cùng một giao dịch hoặc dùng ghi có điều kiện.
- **Kèm theo:** DLQ cho message lỗi vĩnh viễn (poison), `maxReceiveCount` rõ ràng (`messaging.md`).

#### e. Saga (giao dịch xuyên service bằng chuỗi giao dịch cục bộ)
Không có một transaction bao nhiều service, nên saga là **chuỗi giao dịch cục bộ**; bước sau lỗi thì chạy **giao dịch bồi hoàn** (compensating) để hoàn tác các bước trước (ví dụ "huỷ Task" bù cho "xoá Board" nửa chừng).
```
Choreography:  A xong ─event─► B xong ─event─► C lỗi ─event─► B bồi hoàn ─► A bồi hoàn
Orchestration: Orchestrator ─lệnh─► A ─► B ─► C (lỗi) ─► ra lệnh bồi hoàn B, rồi A
```
- **Choreography:** các service tự phản ứng với event của nhau. **Ưu:** ít điểm trung tâm. **Nhược:** khó nhìn toàn cảnh và gỡ lỗi.
- **Orchestration:** một bộ điều phối ra lệnh từng bước. **Ưu:** dễ theo dõi, dễ thêm bước. **Nhược:** thêm một thành phần, nguy cơ phình logic.
- **Cái chung (microservices.io):** không rollback tự động nên phải tự viết bước bồi hoàn; thiếu isolation (người khác thấy dữ liệu trung gian); cần cách báo kết quả bất đồng bộ cho client; thường cần outbox để cập nhật DB và phát message nguyên tử.
- **Dùng khi:** quy trình nhiều bước qua nhiều service có bồi hoàn. **Không** khi quy trình nằm gọn trong một service.

#### f. Chịu lỗi cho call đồng bộ: timeout, retry, circuit breaker
- **Timeout:** không chờ vô hạn. **Retry có jitter:** thử lại có giãn cách ngẫu nhiên, chỉ cho thao tác idempotent. **Circuit breaker:** ngắt tạm call tới service đang lỗi để khỏi kéo sập chuỗi.
- **Cái giá/bẫy:** retry ở nhiều tầng nhân lên (sách Google SRE: 3 tầng mỗi tầng 4 lần thử → 4×4×4 = 64 lần lên backend quá tải); circuit breaker cũng có thể lỗi (Discord, [caching.md](caching.md) mục 7). Chi tiết: `document/architecture/resilience.md`.

#### g. Quan sát xuyên service
`correlationId` + `traceparent` ở mọi request/event, log/trace/metric (OpenTelemetry); không có thì debug xuyên service gần như bất khả thi. Chi tiết: `document/architecture/observability.md`.

#### So sánh nhanh

| Vấn đề | Cơ chế | Cái giá chính |
|---|---|---|
| Cần đáp án ngay | Đồng bộ (REST/gRPC) | ghép nối thời gian, lỗi lan |
| Nhiều bên quan tâm sự kiện | Bất đồng bộ (event) | eventual consistency |
| Độc lập dữ liệu | Database-per-service | mất ACID xuyên service |
| Lưu DB và báo service khác | Outbox | thêm relay; có thể gửi trùng |
| Nhận event trùng | Idempotent consumer | cần lưu `eventId`, nhất quán đánh dấu/tác động |
| Quy trình nhiều service có bồi hoàn | Saga | bồi hoàn tự viết, thiếu isolation |
| Service kia chậm/chết | Timeout, retry, circuit breaker | retry khuếch đại nếu nhiều tầng |
| Truy vết lỗi | Correlation + trace | cần kỷ luật ở mọi service |

## 4. Cách dùng

**Quy trình chọn và cắt ranh giới (rút gọn):**
1. Bắt đầu từ nhu cầu thật: cần deploy/scale độc lập, đội tự chủ, hay công nghệ dữ liệu khác nhau? Không có thì chưa cần tách (mục 7: MonolithFirst, Shopify).
2. Cắt theo bounded context, không theo bảng hay tầng kỹ thuật.
3. Mỗi service một DB; dữ liệu của người khác lấy qua API hoặc event.
4. Chọn giao tiếp: cần câu trả lời ngay → REST (kèm timeout + retry có jitter + circuit breaker); sự kiện nhiều bên quan tâm → event.
5. Dựng quan sát trước khi dựng nhiều service.

**Ví dụ code: Outbox ở phía ghi, idempotent ở phía nhận** (minh hoạ chung, **Chưa kiểm**: chưa biên dịch trong dự án; tên kiểu là giả định, hợp đồng event thật nằm ở `src/shared` theo `.agents/rules/messaging.md`):

```csharp
// Phía ghi (task-service): dữ liệu và outbox trong CÙNG một SaveChanges (một transaction)
public async Task AssignAsync(AssignTask cmd, CancellationToken ct)
{
    var task = await db.Tasks.FindAsync([cmd.TaskId], ct) ?? throw new NotFoundException();
    task.AssignTo(cmd.UserId);

    db.Outbox.Add(new OutboxMessage(
        EventId: Guid.NewGuid(), Type: "TaskAssigned", OccurredAt: DateTime.UtcNow,
        CorrelationId: cmd.CorrelationId,
        Payload: JsonSerializer.Serialize(new TaskAssigned(task.Id, cmd.UserId))));

    await db.SaveChangesAsync(ct);   // nếu lỗi thì cả hai cùng không được ghi
}

// Phía nhận (notification-service): bỏ qua event đã xử lý
public async Task HandleAsync(EventEnvelope e, CancellationToken ct)
{
    if (!await dedupe.TryMarkAsync(e.EventId, ct)) return;   // đã thấy eventId này
    await SendNotificationAsync(e, ct);
}
```

Hai chỗ cố ý để bạn tìm (bài tập mục 10): relay nền đọc bảng outbox ở đâu và đánh dấu "đã gửi" thế nào; và `TryMarkAsync` rồi crash trước `SendNotificationAsync` thì chuyện gì xảy ra.

**Chạy thử một "service" thật trong repo hiện tại** (chỉ `teamboard-api`):

```bash
docker compose -f src/docker-compose.yml up --build -d
docker compose -f src/docker-compose.yml ps
```
Mong đợi: `teamboard-api` chạy ở http://localhost:6060. Chưa chạy lại trong phiên này.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Deploy độc lập từng service, pipeline nhanh hơn | Gọi qua mạng chậm và hay lỗi hơn gọi hàm trong tiến trình |
| Scale riêng phần cần | Không có giao dịch ACID xuyên service; eventual consistency, saga |
| Mỗi đội/service chọn công nghệ riêng | Vận hành, giám sát, debug phức tạp hơn nhiều |
| Lỗi một service có thể bị cô lập | Coupling lúc chạy làm giảm sẵn sàng nếu chuỗi gọi dài; thư viện dùng chung lệch phiên bản (Segment) |
| Đội nhỏ sở hữu trọn một service | Quá nhiều service thì phối hợp xuyên đội và debug thành gánh nặng (Uber) |

**Nên dùng khi:** nhiều đội, cần deploy độc lập thường xuyên, các phần có nhu cầu scale/công nghệ khác nhau, đã có nền DevOps.
**Không nên khi:** đội nhỏ, sản phẩm mới còn đổi mạnh, domain nhỏ, thiếu DevOps, workload đồng đều.
### Bản đồ giải pháp

Microservice là **một kiểu kiến trúc triển khai** trong một dải lựa chọn; chọn nó là quyết định đánh đổi, không phải mặc định tốt hơn. Mức: Chắc nếu tôi tự mở nguồn; Vừa nếu chỉ agent đọc hoặc nguồn nội bộ.

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá | Nguồn (mức) |
|---|---|---|---|---|
| **Monolith phân lớp (layered)** | Một ứng dụng, một DB, các tầng presentation/business/data | CRUD nhỏ, đội mới, domain đơn giản, sản phẩm còn đổi mạnh | Logic dễ rải ra, thành khối lớn khó đổi/deploy từng phần | `patterns-catalog.md` 1.1 (nội bộ); Fowler MonolithFirst (Vừa) |
| **Modular monolith** | Shopify: một deployable, chia code theo domain, mỗi component có API công khai và dữ liệu riêng, công cụ Wedge bắt vi phạm ranh giới | Muốn ranh giới rõ mà không chịu chi phí phân tán; cần nhất quán mạnh | Cần kỷ luật để module không rò rỉ; vẫn một deployable, scale/deploy chung | [Shopify](https://shopify.engineering/deconstructing-monolith-designing-software-maximizes-developer-productivity) (Chắc, đã mở) |
| **Microservice** | Segment (trước khi gộp), Netflix, TeamBoard | Nhiều đội, cần deploy/scale độc lập, đã có DevOps | Chi phí phân tán: mạng, eventual consistency, vận hành; có thể thành distributed monolith | [Segment](https://www.twilio.com/en-us/blog/developers/best-practices/goodbye-microservices) (Chắc, đã mở) |
| **Microservice có cấu trúc domain** | Uber DOMA: gom khoảng 2.200 service thành khoảng 70 domain, mỗi domain một gateway | Số service rất lớn, cần giảm phối hợp xuyên đội | Thêm lớp cấu trúc (domain, tầng, extension) cần đầu tư | [Uber DOMA](https://www.uber.com/en-GB/blog/microservice-architecture/) (Chắc, đã mở) |
| **Serverless / event-driven functions** | `activity-service` (Lambda + SNS/SQS + DynamoDB) | Tải thất thường, xử lý theo sự kiện, ít vận hành | Khó debug/test cục bộ, giới hạn runtime, chi phí có thể tăng ở tải ổn định lớn (Prime Video chỉ là thiết kế lại một dịch vụ, nguồn thứ cấp) | `patterns-catalog.md` mục 5 (nội bộ); Catchpoint (Vừa) |
| **Di chuyển dần từ monolith** | Strangler Fig từ `teamboard-api` sang các service | Có monolith đang chạy | Chạy song song, chi phí tạm thời | `patterns-catalog.md` 3.7 (nội bộ) |

**Cách chọn:** bắt đầu từ nhu cầu (deploy độc lập? scale riêng? đội tự chủ?) rồi đi từ ít phân tán đến nhiều; nhiều công ty nêu ở mục 7 chọn hoặc quay về các họ ít phân tán hơn khi chi phí vượt lợi ích. Không có kiểu nào "đúng" mọi nơi (Fowler: có "phí bảo hiểm" cho microservice).

## 6. Lỗi hay gặp và cách tránh

- **Distributed monolith / networked monolith.** Triệu chứng: đổi một service buộc deploy cùng lúc nhiều service. Nguyên nhân: ranh giới sai hoặc chung DB/thư viện chứa logic. Cách tránh: database-per-service; `src/shared/` chỉ có contract.
- **Chuỗi gọi đồng bộ dài.** Triệu chứng: độ trễ và lỗi nhân lên. Cách tránh: ít sync call; timeout, retry có jitter, circuit breaker; chuyển sang event. **Không retry ở nhiều tầng** (sách Google SRE chương "Addressing Cascading Failures": 3 tầng cùng retry 3 lần thành 4×4×4 = 64 lần thử lên backend).
- **Mất hoặc trùng event.** Nguyên nhân: publish sau commit (dual write) hoặc consumer không idempotent. Cách tránh: outbox + dedupe theo `eventId`.
- **Không truy vết được lỗi.** Nguyên nhân: không có correlation. Cách tránh: `correlationId` + `traceparent` ở mọi request/event.
- **Quá nhiều service quá sớm.** Cách tránh: đi theo giai đoạn, mỗi giai đoạn chạy end-to-end; xem MonolithFirst và Segment ở mục 7.

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **Segment** (2018) | Pipeline gửi sự kiện tới hơn 100 API đối tác; mỗi "destination" một service riêng, hơn 140 service; thư viện dùng chung lệch phiên bản, scale tay từng service | Gộp thành một service, một monorepo; dựng Traffic Recorder để test không phụ thuộc đối tác | Cả bộ test từng mất tới một giờ (một destination có thể tới 5 phút), sau khi có Traffic Recorder chỉ còn mili-giây; một kỹ sư deploy trong vài phút; số cải tiến thư viện chung 32 → 46 trong năm sau. Tự nhận đánh đổi: cô lập lỗi kém hơn, cache trong bộ nhớ kém hiệu quả, nâng dependency rủi ro hơn | [Goodbye Microservices](https://www.twilio.com/en-us/blog/developers/best-practices/goodbye-microservices) (A. Noonan, 10/07/2018) | Chắc (đã mở) |
| **Shopify** (2019) | Ứng dụng Rails lõi hơn 10 năm; muốn modular hơn nhưng microservice kéo theo nhiều pipeline, hạ tầng, độ trễ mạng | **Modular monolith**: giữ một deployable, chia code theo domain (orders, shipping, billing), mỗi component có API công khai và dữ liệu riêng; công cụ nội bộ Wedge bắt vi phạm ranh giới trong CI; ~6.000 class chuyển bằng một PR lớn | Bài không đưa số đo; ví dụ nêu được là thay engine tính thuế cũ, việc trước đó gần như bất khả thi | [Deconstructing the Monolith](https://shopify.engineering/deconstructing-monolith-designing-software-maximizes-developer-productivity) (K. Westeinde, 21/02/2019) | Chắc (đã mở) |
| **Uber** (2020) | Khoảng 2.200 microservice trọng yếu; nhiều khi phải lần qua khoảng 50 service của 12 đội để tìm nguyên nhân gốc; "networked monolith" (service tách nhưng phải deploy cùng nhau) | **DOMA**: gom service thành khoảng 70 domain, 5 tầng (chỉ phụ thuộc xuống tầng dưới), mỗi domain một gateway, "extension" để thêm logic/dữ liệu không sửa lõi | Theo bài: onboarding giảm 25–50%; một tích hợp tính năng từ khoảng 3 ngày xuống 3 giờ; khoảng một nửa số domain đã triển khai lúc viết | [DOMA](https://www.uber.com/en-GB/blog/microservice-architecture/) (A. Gluck, 23/07/2020) | Chắc (đã mở) |
| **Netflix** (2016) | Sự cố hỏng DB tháng 8/2008 làm ngừng giao DVD 3 ngày; muốn bỏ điểm lỗi đơn của data center | Tái kiến trúc cloud-native (không lift-and-shift): tách monolith thành hàng trăm microservice trên AWS nhiều region | Hoàn tất sau khoảng 7 năm; bài học nêu: chuyển nguyên trạng mang theo giới hạn cũ, cần dự phòng và diễn tập lỗi, thay đổi văn hoá tốn công ngang kỹ thuật | [Completing the Netflix Cloud Migration](https://about.netflix.com/en/news/completing-the-netflix-cloud-migration) (12/02/2016) | Vừa (agent đã đọc, tôi chưa mở lại) |
| **Amazon Prime Video** (2023) | Dịch vụ giám sát chất lượng luồng video thiết kế serverless/phân tán, chi phí cao ở tải lớn | Gộp các thành phần vào một tiến trình, bớt orchestration và trung chuyển qua S3 | Chỉ là thiết kế lại **một** dịch vụ, không phải bỏ microservice nói chung. **Bài gốc không mở được** (đã chuyển hướng mất nội dung), con số "giảm 90%" chỉ thấy qua nguồn thứ cấp nên **không trích** | [Catchpoint phân tích](https://www.catchpoint.com/blog/debunking-misconceptions-amazon-prime-videos-approach-to-microservices-and-serverless) (18/07/2023) | Vừa (thứ cấp) |
| **Martin Fowler** (2015) | Nên bắt đầu thế nào | *MonolithFirst*: bắt đầu bằng monolith module hoá tốt, vì ranh giới service khó đoán đúng lúc đầu và đổi trong monolith rẻ hơn; *MicroservicePremium*: microservice có "phí bảo hiểm" chỉ đáng khi hệ đủ phức tạp. Tự nhận bằng chứng còn ít | Lời khuyên mang tính tạm thời; đội đã có kinh nghiệm có thể bắt đầu thẳng | [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html), [MicroservicePremium](https://martinfowler.com/bliki/MicroservicePremium.html) | Vừa (agent đã đọc, tôi chưa mở lại) |

**Khung tổng quan (Chắc, đã mở):** bài "Microservices" của Fowler và Lewis (2014) và trang pattern của Chris Richardson (microservices.io) cùng nêu cái giá: gọi từ xa tốn hơn gọi hàm, eventual consistency thay cho giao dịch phân tán, vận hành và giám sát phức tạp.

**Mô hình phổ biến:** nhiều công ty bắt đầu bằng monolith rồi tách có chọn lọc; quy mô rất lớn thì thêm cấu trúc (domain, gateway) để kiểm soát số service; modular monolith là lựa chọn giữa.

**Khi nó thất bại hoặc bị bỏ:** Segment và (một phần) Prime Video gộp lại vì chi phí vận hành và phối hợp vượt lợi ích; Uber phải thêm cấu trúc DOMA vì số service quá lớn. Bài học chung: chọn theo workload và quy mô đội, không theo mốt.

**Chưa mở được nguồn gốc cho:** Amazon "two-pizza team" (bài ACM Queue của Vogels trả 403), nên không nêu.

## 8. Trong TeamBoard

- Sáu service, mỗi service một kiến trúc nội bộ khác nhau: `document/architecture/README.md` mục 1, ADR 0001. Ràng buộc: `.agents/rules/architecture.md` (không "đơn giản hoá" về CRUD layered).
- **Đây là chủ ý học tập (`[Chọn để học]`), không phải khuyến nghị cho domain nhỏ.** ADR 0001 ghi rõ: domain bị chia "quá tay" có chủ đích. Mục 7 cho thấy vì sao: Shopify chọn modular monolith, Segment gộp lại. `patterns-catalog.md` mục 1.4 và 7 gợi ý dựng modular monolith đối chứng và Strangler Fig từ `teamboard-api`.
- Giao tiếp: REST qua gateway, event SNS → SQS + DLQ, outbox, consumer idempotent (ADR 0002, `.agents/rules/messaging.md`).
- Quan sát: `document/architecture/observability.md`; chịu lỗi: `document/architecture/resilience.md`.
- Lộ trình: README mục 6; trạng thái hiện tại giai đoạn 0, TB-001 đang đổi tên `teamboard-api` và thêm `ServiceDefaults`.
- **Điểm lệch cần biết:** slide tham khảo dùng RabbitMQ + MassTransit; dự án dùng SNS/SQS và tự viết lớp nhắn tin mỏng (MassTransit từ v9 bị cấm theo `dependency-policy.md`). Học pattern, không bắt buộc thư viện.

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Mức |
|---|---|---|---|---|
| 1 | `document/architecture/README.md`, `adr/0001-*.md`, `0002-*.md` | Nội bộ | bản đồ service, vì sao chia thế này | Chắc |
| 2 | `document/architecture/patterns-catalog.md` | Nội bộ | danh mục pattern, khi dùng/tránh | Chắc |
| 3 | `.agents/rules/architecture.md`, `messaging.md` | Nội bộ | luật bắt buộc | Chắc |
| 4 | `document/architecture/resilience.md`, `observability.md` | Nội bộ | chịu lỗi, truy vết | Chắc |
| 5 | https://martinfowler.com/articles/microservices.html | Bài gốc | đặc điểm và cái giá | Chắc (đã mở) |
| 6 | https://microservices.io/patterns/microservices.html , `/data/transactional-outbox.html` , `/data/saga.html` | Pattern | khi dùng, outbox, saga | Chắc (trang đầu đã mở; hai trang sau qua agent) |
| 7 | Các case ở mục 7 | Bài gốc | ai làm gì, vì sao | Chắc/Vừa (nhãn từng dòng) |
| 8 | *Microservices Patterns* (C. Richardson), *Building Microservices* (S. Newman) | Sách | saga, outbox, ranh giới | Chưa kiểm |

## 10. Tự kiểm tra / thực hành

1. Vì sao `board-service` không được query thẳng DB của `task-service` dù cùng công ty, cùng VPC? (Gợi ý: coupling, deploy độc lập, ADR 0001.)
2. Segment gộp 140+ service thành một còn Uber gom 2.200 service thành 70 domain. Hai quyết định ngược nhau có mâu thuẫn không? Chỉ ra điều kiện (đội, workload, quy mô) khiến mỗi bên hợp lý.
3. Với đoạn code mục 4: relay nền cần đọc outbox thế nào để không bỏ sót và không gửi vô hạn? Nếu consumer `TryMarkAsync` xong rồi crash trước `SendNotificationAsync`, điều gì xảy ra và cách sửa?
4. Tạo Task xong nhưng feed chưa thấy ngay: lỗi hay hành vi mong đợi? Giải thích bằng eventual consistency.
5. Việc chạy được: `docker compose -f src/docker-compose.yml config -q` (mong đợi: không in lỗi), rồi liệt kê service nào trong README mục 1 chưa có thư mục trong `src/`.
