# Part 5 — Production, Security, Operations (Microservice)

> [!NOTE]
> **Mục tiêu:** vận hành nhiều service: lần ra lỗi xuyên service, phát hành hợp đồng không phá consumer, đo độ sẵn sàng của cả chuỗi, và hiểu chi phí tổ chức của việc "mỗi service một thứ".
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** nêu chỉ số và cảnh báo cho một hệ nhiều service, thực hiện đổi tên field theo expand-contract, tính SLO của một chuỗi, nêu điều kiện tiên quyết để vận hành N service.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Microservice khi đã chạy thật

#### Quan sát xuyên service
Log có cấu trúc, metric và trace với `correlationId` và `traceparent`; không có thì debug xuyên service gần như bất khả thi.

#### Triển khai độc lập và phát hành hợp đồng
Mỗi service phát hành theo nhịp riêng; hợp đồng giữa chúng đổi theo cách **không đòi phát hành đồng thời**.

#### Độ sẵn sàng và SLO của cả chuỗi
Độ sẵn sàng của một thao tác là tích các service nó đi qua (Lab 1.1), không phải của từng service.

#### Chi phí vận hành và tổ chức
Mỗi service thêm một pipeline, một bộ giám sát, một lịch vá lỗi, một đội phải hiểu nó.

## L2 Why — Vì sao vận hành mới là phần khó

Viết một service dễ; giữ 10 hay 100 service chạy được là chuyện khác. Phần lớn lợi ích của tách service (độc lập) chỉ có khi **hạ tầng và kỷ luật** cho phép: phát hành không phá nhau, lần ra lỗi nhanh, ai sở hữu service nào rõ ràng. Thiếu các thứ đó thì có chi phí phân tán mà không có lợi ích (distributed monolith).

## L3 How — Cơ chế đáng nhớ

### Quan sát
| Chỉ số | Ý nghĩa | Dấu hiệu xấu |
|---|---|---|
| tỉ lệ lỗi, độ trễ p95/p99 theo service và route | sức khoẻ từng service | tăng ở một service |
| độ trễ và lỗi của lời gọi ra từng đích | đích nào làm chậm | một đích tăng |
| tuổi dòng outbox cũ nhất | relay còn sống | tăng liên tục |
| độ sâu và tuổi message ở hàng đợi, DLQ | consumer theo kịp | DLQ khác rỗng, độ sâu tăng |
| số lần retry, trạng thái breaker | khuếch đại lỗi | breaker mở, retry tăng |
| tỉ lệ event bị loại vì trùng (dedupe) | giao trùng | tăng đột biến |
| tỉ lệ saga lỗi và bước đang chạy dở | nhất quán xuyên service | saga kẹt |

Trace phân tán cho thấy đường đi một yêu cầu; log gắn `correlationId` (Lab 2.3); metric theo dạng **tỉ lệ** (không phải số tuyệt đối). Ở Monzo (hơn 2.800 service) việc thống nhất công nghệ (một ngôn ngữ, monorepo, thư viện lõi chung) là điều kiện để quan sát và migrate ở quy mô đó.

### Phát hành hợp đồng không phá consumer (expand and contract)
Đổi tên hay xoá field là thay đổi phá vỡ (Part 2). Cách làm an toàn gồm ba pha: **mở rộng** (producer phát cả field cũ lẫn mới), **chuyển** (từng consumer chuyển sang field mới), **thu hẹp** (khi không còn consumer cũ, bỏ field cũ) (Lab 5.1). Cho API tương tự: chạy song song `/v1` và `/v2` đến khi người gọi cũ chuyển xong; ghi rõ ngày ngừng. Test hợp đồng (consumer-driven) kiểm trước khi phát hành.

### SLO của chuỗi
Nếu một thao tác đi qua N service có độ sẵn sàng `a` độc lập thì độ sẵn sàng cả chuỗi là `a^N`: 3 service × 99,9% ≈ 99,7%; 5 service × 99% ≈ 95,1% (tính tay, đúng quy luật Lab 1.1). Để giữ SLO: giảm độ dài chuỗi đồng bộ, dùng bất đồng bộ, timeout, dự phòng (Part 4), và đặt SLO theo **hành trình người dùng** chứ không theo từng service.

### Triển khai và hạ tầng
Mỗi service có pipeline CI/CD, phiên bản, rollback riêng; triển khai theo giai đoạn (canary, từng phần); tương thích ngược giữa phiên bản cũ và mới của cùng một service chạy song song trong lúc triển khai. Đóng gói bằng container ([Docker](../docker/README.md)); cửa vào bằng gateway ([API Gateway](../api-gateway/README.md)).

### Bảo mật
Mỗi service là một bề mặt tấn công: xác thực giữa service (mTLS hoặc token), ủy quyền ở từng service (gateway không thay phân quyền nghiệp vụ), không tin mạng nội bộ, quản lý secret từng service, quyền DB riêng (database-per-service được ép bằng tài khoản).

### Chi phí vận hành và tổ chức
| Chi phí theo số service | Biểu hiện |
|---|---|
| Pipeline và hạ tầng | mỗi service một pipeline, cấu hình, giám sát |
| Phối hợp xuyên đội | đổi hợp đồng cần thương lượng; "networked monolith" khi buộc deploy chung (Uber) |
| Vá lỗi và nâng thư viện | thư viện dùng chung lệch phiên bản (Segment: hơn 140 service) |
| Gỡ lỗi | một lỗi trải qua nhiều service và nhiều đội |
| Nhận thức | không ai hiểu toàn hệ |

Cách giảm: thống nhất công nghệ (Monzo), gom thành domain với một cửa vào (Uber DOMA), hoặc **gộp lại** khi chi phí vượt lợi ích (Segment, Istio).

## L4 Hands-on — Lab Part 5

#### Lab 5.1 — Mở rộng rồi thu hẹp khi đổi `title` thành `name` (`System.Text.Json`)
Hai consumer: v1 đọc `title`, v2 đọc `name`.
**Đầu ra thật:**
```text
1. producer cũ (chỉ title):               consumer v1 đọc Title = Sprint; consumer v2 đọc Name = null
2. producer phát CẢ title và name:        consumer v1 đọc Title = Sprint; consumer v2 đọc Name = Sprint
3. producer mới (chỉ name):               consumer v1 đọc Title = null;   consumer v2 đọc Name = Sprint
```
**Giải thích:** pha 2 (phát cả hai field) là lúc **cả hai consumer đều đúng**; nhảy thẳng từ pha 1 sang 3 làm consumer v1 nhận `Title = null` mà không có lỗi. Chỉ bỏ field cũ (pha 3) khi chắc chắn mọi consumer v1 đã chuyển.

#### Lab 5.2 — SLO của chuỗi (tính tay)
- 3 service, mỗi service 99,9%: 0,999³ ≈ 0,997 (99,7%).
- 5 service, mỗi service 99%: 0,99⁵ ≈ 0,951 (95,1%).
- Số đo ở [Lab 1.1](part-1-fundamentals.md): 3 service, mỗi service 95%: đo 85,4% (lý thuyết 85,7%).
**Giải thích:** SLO của hành trình là tích; đó là lý do giảm độ dài chuỗi đồng bộ.

#### Lab 5.3 — Correlation id xuyên service (đã chạy ở Part 2)
Xem [Lab 2.3](part-2-core-skills.md): thiếu chuyển tiếp ở một hop thì mất dấu từ đó trở đi.

## L5 Troubleshooting — Sự cố khi vận hành nhiều service

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Phát hành một service làm service khác lỗi | phá hợp đồng | so hợp đồng trước sau (Lab 5.1) | expand-contract, test hợp đồng |
| Hai service luôn phải phát hành cùng nhau | distributed monolith | lịch sử phát hành, chung DB | tách hợp đồng và dữ liệu, hoặc gộp lại |
| Không lần ra request lỗi | mất correlation hay trace | tìm id ở từng service | chuyển tiếp id ở mọi lời gọi và message |
| SLO người dùng thấp dù service nào cũng đạt | chuỗi đồng bộ dài | tích độ sẵn sàng (Lab 5.2) | giảm độ dài chuỗi, bất đồng bộ, dự phòng |
| Lỗi dây chuyền từ một dependency dùng chung | phụ thuộc ẩn (mạng, provisioning, giám sát) | sơ đồ phụ thuộc | tách hạ tầng giám sát khỏi thứ nó giám sát; test khi tắt dependency |
| Nâng thư viện chung phải sửa hàng chục service | thư viện chứa logic dùng chung | số service phụ thuộc | thư viện chỉ chứa contract; công cụ migrate tự động |
| Không ai biết service thuộc đội nào | thiếu sở hữu rõ ràng | danh mục service | chủ sở hữu và on-call cho từng service |
| Chi phí hạ tầng và công sức tăng nhanh | số service tăng quá sớm | chi phí theo service | gộp các service luôn đi cùng nhau |

## L6 Production — Danh sách kiểm trước khi thêm một service

- **Có thật sự cần tách?** Có nhu cầu deploy hay scale độc lập, đội riêng, công nghệ riêng; không thì giữ trong module.
- **Ranh giới:** theo năng lực nghiệp vụ; dữ liệu riêng; không chung DB; hợp đồng ghi rõ.
- **Quan sát có trước:** correlation, trace, metric và log có cấu trúc, cảnh báo theo tỉ lệ.
- **Chịu lỗi:** timeout, retry một tầng, bulkhead, outbox và consumer idempotent cho event, DLQ có cảnh báo.
- **Phát hành:** pipeline riêng, tương thích ngược hai phiên bản, expand-contract cho hợp đồng, triển khai theo giai đoạn, rollback.
- **Bảo mật:** xác thực giữa service, ủy quyền ở từng service, secret riêng, quyền DB riêng.
- **Sở hữu:** đội, on-call, runbook, chi phí.
- **SLO:** đặt theo hành trình người dùng; tính tích cho chuỗi.
- **Lối ra:** biết trước nếu chi phí vượt lợi ích thì gộp lại thế nào (Segment, Istio).

## L7 Architecture — Mức độ phân tán và nơi dừng

| Phương án | Khi chọn | Cái giá |
|---|---|---|
| Giữ monolith chia module | đội nhỏ, ranh giới chưa rõ | scale và deploy chung |
| Modular monolith có kiểm ranh giới trong CI | muốn ranh giới rõ, một deployable | cần công cụ và kỷ luật (Shopify) |
| Ít service lớn theo domain | vài đội, cần độc lập vừa phải | ranh giới thô hơn |
| Nhiều service nhỏ, công nghệ thống nhất | quy mô lớn, nền tảng mạnh (Monzo) | đầu tư nền tảng và công cụ migrate |
| Nhiều service có cấu trúc domain và gateway từng domain | số service rất lớn (Uber DOMA) | thêm lớp cấu trúc |
| Gộp lại khi chi phí vượt lợi ích | cùng đội, cùng ngôn ngữ, phát hành chung (Segment, Istio) | mất cô lập lỗi, scale chung |
| **Không tách thêm** | chưa đo thấy nhu cầu | giữ các đau hiện tại |

**Câu hỏi quyết định:** *nếu bỏ một nửa số service này và gộp lại, chúng ta mất gì?* Nếu không mất độc lập thật nào, chúng không nên là hai service.

## Tự kiểm tra

1. Vì sao nhảy thẳng từ producer cũ sang producer mới làm consumer cũ hỏng ngầm?
2. 5 service đồng bộ, mỗi service 99%: SLO cả chuỗi?
3. Nêu ba điều kiện tiên quyết trước khi thêm service thứ N.
4. Vì sao Monzo vận hành được hơn 2.800 service?

<details>
<summary>Đáp án ngắn</summary>

1. Consumer cũ đọc field cũ đã biến mất nên nhận `null` mà không lỗi (Lab 5.1).
2. Khoảng 95,1% (0,99⁵).
3. Ví dụ: quan sát có trước, hợp đồng tương thích ngược và pipeline riêng, sở hữu và on-call rõ.
4. Thống nhất công nghệ (Go, monorepo, thư viện lõi chung) và công cụ migrate tập trung; không phải vì microservice tự nhiên dễ.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 (đầu ra thật); Lab 5.2 là phép tính tay, có đối chiếu với số đo Lab 1.1; Lab 5.3 tham chiếu Lab 2.3.
**Đã tự mở:** Segment "Goodbye Microservices" (10/07/2018: hơn 140 service, thư viện chung lệch phiên bản); Uber DOMA (23/07/2020: networked monolith, deep call chains); Istio "Istio is getting simpler" (19/03/2020: lý do gộp thành istiod).
**Chỉ qua agent (⚠):** Monzo "How we run migrations across 2800 microservices" (26/08/2024) và The Register (09/03/2020); Fowler "Microservices" (nine characteristics, đã mở ở phiên trước); Azure patterns (Compensating Transaction, Bulkhead, Circuit Breaker).
**Chưa kiểm:** canary và rollout thật, test hợp đồng consumer-driven (Pact), mTLS giữa service, số liệu SLO thật của hệ nào.
