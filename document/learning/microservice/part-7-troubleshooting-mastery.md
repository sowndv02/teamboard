# Part 7 — Troubleshooting & Mastery (Microservice)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán sự cố của hệ nhiều service từ triệu chứng, học từ các case thực tế (cả những công ty tách, gộp, và gom lại), rồi tự thiết kế và bảo vệ một kiến trúc.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống, trả lời được câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: có Board nhưng không bao giờ có thông báo

Một số Board tạo thành công nhưng `notification-service` không bao giờ nhận event.
<details>
<summary>Chẩn đoán</summary>

Dual write mất event: ghi DB rồi publish, chết giữa hai bước (Lab 4.1). Sửa: transactional outbox; cảnh báo theo tuổi dòng outbox cũ nhất; test chèn lỗi giữa hai bước.
</details>

#### Tình huống 2: khách nhận hai email giống nhau

Cùng một thông báo gửi hai lần cách nhau vài giây.
<details>
<summary>Chẩn đoán</summary>

Broker giao trùng (hoặc relay gửi trùng) và consumer không idempotent, hoặc "đánh dấu" và "tác động" tách giao dịch (Lab 3.1, 3.2). Sửa: inbox có khoá chính trên `eventId`; với email, gửi kèm khoá yêu cầu ổn định cho nhà cung cấp.
</details>

#### Tình huống 3: một service chậm làm cả hệ thống ngừng

Một service phụ chậm; vài phút sau mọi service gọi nó không phản hồi, rồi cả chuỗi phía trên.
<details>
<summary>Chẩn đoán</summary>

Thiếu timeout (kết nối bị giữ, Lab 2.1), thiếu bulkhead (một đích chiếm hết pool, Lab 4.4), và có thể retry nhiều tầng nhân tải (Lab 4.3). Sửa: timeout cho mọi lời gọi, pool riêng theo đích, retry một tầng với ngân sách, circuit breaker.
</details>

#### Tình huống 4: sau khi một đội phát hành, đội khác báo dữ liệu rỗng

Không có lỗi trong log, nhưng tên Board hiện trống ở màn hình thông báo.
<details>
<summary>Chẩn đoán</summary>

Producer đổi tên field `title` thành `name`; consumer cũ đọc `null` mà không lỗi (Lab 2.2, 5.1). Sửa: expand-contract (phát cả hai field cho đến khi consumer chuyển xong); test hợp đồng; kiểm bắt buộc ở nơi nhận.
</details>

#### Tình huống 5: xoá Board xong, người dùng vẫn thấy Board nhưng không còn Task

Một thao tác xoá lỗi giữa chừng.
<details>
<summary>Chẩn đoán</summary>

Saga không bồi hoàn hoặc bồi hoàn không chạy tiếp được, và thiếu isolation: người đọc thấy trạng thái trung gian (Lab 4.2). Sửa: bước bồi hoàn idempotent, lưu tiến trình bền, trạng thái "đang xoá", hàng chờ xử lý thủ công khi bồi hoàn thất bại.
</details>

## L7 Architecture — Trong thực tế

Mức: **đã mở** là tự mở nguồn gốc; **⚠ qua agent** là agent nghiên cứu đã đọc, chưa tự đối chiếu; **⚠ thứ cấp** là chỉ thấy qua bài của bên thứ ba.

| Công ty / dự án | Bối cảnh và vấn đề | Cách làm | Kết quả, bài học | Nguồn (mức) |
|---|---|---|---|---|
| **Segment (2018)** | pipeline gửi sự kiện tới hơn 100 API đối tác; mỗi destination một service, hơn 140 service; thư viện chung lệch phiên bản | gộp thành một service, một monorepo; dựng Traffic Recorder để test không phụ thuộc đối tác | bộ test từng mất tới một giờ, sau đó còn mili-giây; tự nhận cô lập lỗi kém hơn | [Goodbye Microservices](https://www.twilio.com/en-us/blog/developers/best-practices/goodbye-microservices) (10/07/2018, đã mở) |
| **Shopify (2019)** | ứng dụng Rails lõi hơn 10 năm; muốn modular nhưng microservice kéo theo nhiều pipeline | modular monolith: một deployable, chia theo domain, mỗi component có API công khai; công cụ Wedge bắt vi phạm ranh giới trong CI | bài không đưa số đo; ví dụ nêu được là thay engine tính thuế cũ | [Deconstructing the Monolith](https://shopify.engineering/deconstructing-monolith-designing-software-maximizes-developer-productivity) (21/02/2019, đã mở) |
| **Uber (2020)** | khoảng 2.200 microservice trọng yếu; "networked monolith"; tìm lỗi qua khoảng 50 service của 12 đội | DOMA: khoảng 70 domain, 5 tầng, mỗi domain một gateway | theo bài: onboarding giảm 25 đến 50%; một tích hợp từ khoảng 3 ngày xuống 3 giờ | [DOMA](https://www.uber.com/en-GB/blog/microservice-architecture/) (23/07/2020, đã mở) |
| **Istio (2020)** | control plane gồm Pilot, Galley, Citadel, sidecar injector; độ phức tạp cài đặt, nâng cấp, gỡ lỗi | gộp thành một binary `istiod` | cùng ngôn ngữ, một đội, luôn phát hành chung, scale theo một yếu tố; kết quả: cài, nâng cấp, gỡ lỗi dễ hơn, khởi động nhanh | [Istio is getting simpler](https://istio.io/latest/blog/2020/istiod/) (19/03/2020, đã mở) |
| **Stripe (2017)** | retry làm trùng thao tác trên API | `Idempotency-Key` do client sinh; server lưu kết quả theo khoá | retry an toàn; client dùng backoff mũ có jitter | [Designing robust and predictable APIs with idempotency](https://stripe.com/blog/idempotency) (22/02/2017, đã mở) |
| **Monzo (2020, 2024)** | khoảng 1.600 service (2020), hơn 2.800 (2024) | một ngôn ngữ (Go), monorepo, thư viện lõi chung, công cụ migrate tập trung | số service vận hành được nhờ chuẩn hoá mạnh, không phải tự nhiên | [How we run migrations across 2800 microservices](https://monzo.com/blog/how-we-run-migrations-across-2800-microservices) (26/08/2024) (⚠ qua agent) |
| **Netflix (2016)** | sự cố DB tháng 8/2008 làm ngừng giao DVD 3 ngày | tái kiến trúc cloud-native, tách thành hàng trăm microservice trên AWS | hoàn tất sau khoảng 7 năm; chuyển nguyên trạng mang theo giới hạn cũ | [Completing the Netflix Cloud Migration](https://about.netflix.com/en/news/completing-the-netflix-cloud-migration) (12/02/2016, ⚠ qua agent) |
| **Fowler (2015)** | bắt đầu thế nào | MonolithFirst: bắt đầu bằng monolith chia module; MicroservicePremium: phí chỉ đáng khi độ phức tạp đủ lớn | tự nhận bằng chứng còn ít | [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html), [MicroservicePremium](https://martinfowler.com/bliki/MicroservicePremium.html) (⚠ qua agent) |

### Khi microservice là nguyên nhân hoặc nơi lan sự cố

| Sự cố | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **Slack, 04/01/2021** | Transit Gateway quá tải sau kỳ nghỉ; web tier nghẽn, health check và autoscaling phản tác dụng; giám sát nằm cùng VPC nên mất tầm nhìn | cascade do phụ thuộc dùng chung (mạng, cấp phát, giám sát), không phải coupling giữa service; giám sát phải sống độc lập | [Slack's Outage on January 4th 2021](https://slack.engineering/slacks-outage-on-january-4th-2021/) (⚠ qua agent) |
| **Amazon Prime Video (22/03/2023, thứ cấp)** | dịch vụ phân tích chất lượng video serverless dùng Step Functions; điều phối và trung chuyển dữ liệu tốn chi phí | gộp các thành phần vào một tiến trình trên EC2/ECS; theo nguồn thứ cấp giảm 90% chi phí hạ tầng | bài gốc **không mở được**; chỉ thiết kế lại **một** workload; số 90% không đối chiếu được nên không trích như sự thật | [The New Stack](https://thenewstack.io/amazon-prime-videos-microservices-move-doesnt-lead-to-a-monolith-after-all/) (⚠ thứ cấp) |

**Mô hình phổ biến:** nhiều công ty bắt đầu bằng monolith rồi tách có chọn lọc; quy mô rất lớn thì thêm cấu trúc (domain, gateway) hoặc chuẩn hoá mạnh; gộp lại khi chi phí vượt lợi ích.
**Chưa tìm được nguồn gốc đã mở cho:** Amazon "two-pizza team" (bài ACM Queue của Vogels trả 403), một postmortem chỉ riêng do "distributed monolith" của GitHub hay Atlassian, và Airbnb (chỉ thấy qua snippet); không nêu các case đó. Etsy và Amazon về idempotency không tìm được nguồn.

### Đánh đổi khi thiết kế

- **Độc lập so với chi phí phân tán:** microservice đổi cô lập lỗi, deploy và scale độc lập lấy mạng, eventual consistency, vận hành.
- **Nhất quán so với tự chủ:** saga và eventual consistency đổi lấy dữ liệu riêng; ACID đơn giản hơn nhưng buộc chung.
- **Nhiều service nhỏ so với ít service lớn:** càng nhỏ càng nhiều biên giới phải quản lý; chuẩn hoá là điều kiện để nhiều service sống được (Monzo).
- **Tách so với gộp:** quyết định đảo được; Segment và Istio gộp lại, Uber thêm cấu trúc.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Khi nào nên tách service, khi nào nên giữ monolith? Cho một ví dụ mỗi bên.
2. Vì sao chung DB giữa hai service là dấu hiệu distributed monolith?
3. Giải thích dual write và cách outbox sửa; outbox còn rủi ro gì?
4. Consumer idempotent làm thế nào, và "đánh dấu trước" khác "tác động trước" ra sao?
5. Saga khác giao dịch phân tán 2PC thế nào? Choreography và orchestration chọn khi nào?
6. Retry nhiều tầng nguy hiểm vì sao? Làm sao tránh?
7. Segment gộp lại còn Uber gom domain: hai quyết định có mâu thuẫn không?
8. Thêm field, đổi tên field vào event: cái nào an toàn và vì sao?

### Bài mở

#### Bài 1 — Thiết kế giao tiếp cho `task-service` và `notification-service`
`task-service` dùng CQRS và event sourcing với outbox; `notification-service` dùng kiến trúc hexagonal với consumer idempotent và DLQ. Thiết kế luồng `TaskAssigned`: hợp đồng event, outbox, SNS và SQS, thứ tự, idempotent, DLQ, quan sát, phát hành hợp đồng. **Tiêu chí:** nêu cách xử lý từng loại sự cố (mất, trùng, đảo thứ tự, message độc); có ít nhất hai phương án bị loại kèm lý do; chỉ rõ phần nào là suy luận chưa kiểm.

#### Bài 2 — Quyết định có nên gộp lại không
Cho một hệ 12 service do hai đội sở hữu, luôn phát hành cùng nhau, cùng ngôn ngữ, chung một thư viện nghiệp vụ. Dùng các case Segment, Istio và các Lab để đề xuất: giữ, gộp một phần hay gộp hết. **Tiêu chí:** nêu số liệu cần đo trước khi quyết (tần suất phát hành chung, thời gian build, tỉ lệ sự cố do phối hợp); nêu cái mất khi gộp.

#### Bài 3 — Phản biện một thiết kế
Đồng nghiệp đề xuất "mỗi bảng một service, gọi nhau đồng bộ qua REST, chung một DB cho dễ join, retry 3 lần ở mọi tầng". Viết phản biện nêu ít nhất 5 rủi ro đã học và phương án thay thế. **Tiêu chí:** mỗi rủi ro trỏ tới một Lab hay case.

### Tiêu chí thành thạo

- Giải thích không nhìn tài liệu: khi nào không tách, outbox, saga, idempotent consumer, retry nhiều tầng, expand-contract.
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có số đo hoặc lab của chính bạn và nêu giới hạn của nó.
- Nói được ít nhất ba tình huống nên gộp hoặc giữ monolith.

## Tự kiểm tra

1. Istio gộp control plane vì những lý do nào, và chúng áp lên hệ của bạn thế nào?
2. Vì sao case Slack 2021 không phải ví dụ sạch về distributed monolith?
3. Vì sao cảnh báo số 90% của Prime Video cần thận trọng?

<details>
<summary>Đáp án ngắn</summary>

1. Cùng ngôn ngữ, một đội vận hành, luôn phát hành cùng phiên bản, scale theo một yếu tố, quyền gần như nhau; áp dụng khi các service của bạn cũng như vậy.
2. Nguyên nhân là phụ thuộc dùng chung (mạng, cấp phát, giám sát), không phải coupling giữa service.
3. Bài gốc không mở được; chỉ có nguồn thứ cấp, và đó là thiết kế lại một workload.
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** Segment "Goodbye Microservices" (10/07/2018), Shopify "Deconstructing the Monolith" (21/02/2019), Uber DOMA (23/07/2020), Istio "Istio is getting simpler" (19/03/2020), Stripe "idempotency" (22/02/2017), Fowler "Microservices" (2014).
**Chỉ qua agent (⚠):** Monzo (26/08/2024; The Register 09/03/2020), Netflix (12/02/2016), Fowler MonolithFirst và MicroservicePremium, Slack 04/01/2021, Prime Video qua The New Stack (thứ cấp, bài gốc không mở được).
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ cơ chế đã học và từ lab, không phải sự cố đã gặp ở dự án này.
