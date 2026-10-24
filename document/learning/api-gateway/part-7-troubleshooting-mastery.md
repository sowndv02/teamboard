# Part 7 — Troubleshooting & Mastery (API Gateway)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán sự cố gateway từ triệu chứng, học từ các case thực tế, rồi tự thiết kế và bảo vệ một gateway.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống, trả lời được câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: sau khi đổi cấu hình, mọi API trả 404

Một thay đổi cấu hình route được đẩy lên; vài phút sau mọi request tới gateway trả `404` rỗng.
<details>
<summary>Chẩn đoán</summary>

Route không còn khớp (path, host hoặc method sai), hoặc cấu hình mới thiếu route (Lab 2.1, 5.1). Sửa: rollback cấu hình trước; kiểm cấu hình khi khởi động và trong test; rollout theo giai đoạn (Cloudflare 2019).
</details>

#### Tình huống 2: một nửa yêu cầu lỗi mãi, nửa kia bình thường

Có hai instance upstream, một cái chết từ sáng; 50% yêu cầu lỗi 502.
<details>
<summary>Chẩn đoán</summary>

Không có health check nên `RoundRobin` vẫn gửi vào instance chết (Lab 3.2). Sửa: bật health check chủ động hoặc thụ động; hiểu chi phí của mỗi loại.
</details>

#### Tình huống 3: upstream chậm làm cả cụm chậm

Một instance chậm; thời gian phản hồi trung bình tăng gấp ba dù hai instance còn lại rất nhanh.
<details>
<summary>Chẩn đoán</summary>

`RoundRobin` hay `Random` đưa 1/3 tải vào instance chậm (Lab 3.1). Sửa: `PowerOfTwoChoices` (mặc định) hoặc `LeastRequests`; kiểm xem cấu hình có đổi mặc định không.
</details>

#### Tình huống 4: rate limit không chặn kẻ tấn công

Một nguồn gửi hàng nghìn yêu cầu mỗi giây vào API ghi nhưng không thấy `429`.
<details>
<summary>Chẩn đoán</summary>

Hoặc policy chưa gán cho route, hoặc tin mọi `X-Forwarded-For` nên kẻ tấn công đổi IP mỗi yêu cầu (Lab 4.4). Sửa: gán policy, `KnownProxies` hẹp, cấm bật mọi proxy.
</details>

#### Tình huống 5: sự cố dây chuyền sau một lỗi nhỏ ở một service

Một service phía sau chậm; vài phút sau cả hệ thống nghẽn, tải vào service đó tăng gấp nhiều lần thay vì giảm.
<details>
<summary>Chẩn đoán</summary>

Retry ở nhiều tầng nhân tải (Lab 4.3: 3 tầng × 3 lần = 27). Sửa: retry một chỗ, chỉ request idempotent, backoff có jitter, ngân sách retry, timeout hợp lý, load shedding (Stripe).
</details>

## L7 Architecture — Trong thực tế

Mức: **đã mở** là tự mở nguồn gốc; **⚠ qua agent** là agent nghiên cứu đã đọc, chưa tự đối chiếu.

| Công ty / dự án | Bối cảnh và vấn đề | Cách làm | Kết quả, bài học | Nguồn (mức) |
|---|---|---|---|---|
| **Microsoft, Azure Architecture Center** | client gọi thẳng nhiều service gặp 6 vấn đề | gateway làm routing, aggregation, offloading; lựa chọn: reverse proxy, mesh ingress, Application Gateway, API Management | ưu tiên giải pháp dựng sẵn của nền tảng; quản lý route bằng IaC | [API gateways](https://learn.microsoft.com/en-us/azure/architecture/microservices/design/gateway) (đã mở) |
| **Microsoft, .NET Microservices guide** | ứng dụng tham chiếu eShopOnContainers 2.0 cần gateway .NET nhẹ | chọn Ocelot làm gateway; nhiều gateway theo client và ranh giới nghiệp vụ | rủi ro tự nêu: phình thành monolith, điểm lỗi đơn, thêm hop, một đội sở hữu thành nút thắt | [Gateway vs direct](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/direct-client-to-microservice-communication-versus-the-api-gateway-pattern) (đã mở) |
| **Uber, DOMA (2020)** | khoảng 2.200 microservice trọng yếu; "networked monolith" | khoảng 70 domain, mỗi domain một gateway làm điểm vào duy nhất | theo bài: onboarding giảm 25 đến 50%; gateway giúp thay nền tảng mà upstream không đổi; đây là gateway từng domain, không phải edge | [DOMA](https://www.uber.com/en-GB/blog/microservice-architecture/) (đã mở) |
| **Stripe (2017)** | bảo vệ API khỏi lạm dụng và quá tải | 4 lớp: giới hạn tốc độ theo user, giới hạn đồng thời, load shedder theo fleet, theo worker | rate limit chống lạm dụng thường ngày; load shedding là biện pháp khẩn cấp theo trạng thái hệ thống | [Scaling your API with rate limiters](https://stripe.com/blog/rate-limiters) (đã mở) |
| **Cloudflare, rate limiting (2017)** | đếm theo hàng triệu tên miền | xấp xỉ cửa sổ trượt: số cửa sổ trước × tỉ lệ chồng + số cửa sổ hiện tại | trên 400 triệu yêu cầu sai 0,003%; không đếm chính xác vì tốn bộ nhớ; leaky bucket khó chỉnh và không nguyên tử | [Counting things](https://blog.cloudflare.com/counting-things-a-lot-of-different-things/) (đã mở) |
| **Microsoft, YARP 1.0 (09/11/2021)** | nhiều đội tự dựng reverse proxy | gom thành thư viện reverse proxy trên ASP.NET Core | Azure App Service dùng trực tiếp request forwarder của YARP | [Announcing YARP 1.0](https://devblogs.microsoft.com/dotnet/announcing-yarp-1-0-release/) (đã mở) |
| **Microsoft, CETO (2024)** | ingress cho trải nghiệm AI, hơn 1 tỷ người dùng hằng tháng | YARP 2.1 trên .NET 8; route cấu hình nạp lại, mirror, rate limit theo route | "every single millisecond at this layer counts"; không có số RPS | [Building a scalable gateway](https://devblogs.microsoft.com/dotnet/building-a-scalable-gateway-for-microsoft-ai/) (⚠ qua agent) |
| **SoundCloud, BFF (2015)** | một API chung buộc nhiều lời gọi mỗi màn hình trên mobile | backend riêng cho từng giao diện, đội client sở hữu | logic lặp ở khoảng 5 BFF, sau đó tách ra service miền | [BFF pattern](https://philcalcado.com/2015/09/18/the_back_end_for_front_end_pattern_bff.html) (⚠ qua agent) |
| **Netflix Zuul** | lưu lượng API từ nhiều thiết bị | filter ở edge: xác thực, định tuyến động, load shedding | gateway là nơi gom việc chéo và công tắc vận hành | [Zuul wiki](https://github.com/Netflix/zuul/wiki) (⚠ qua agent) |

### Khi gateway hay proxy là nguyên nhân sự cố

| Sự cố | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **Cloudflare, 02/07/2019** | quy tắc WAF có regex backtracking đẩy toàn cầu lúc 13:42 UTC, hết CPU, lỗi 502; tắt WAF 14:07, ổn định 14:09 (khoảng 27 phút) | quy tắc cũng là mã; cần giới hạn tài nguyên, test hiệu năng, rollout theo giai đoạn | [Details of the outage](https://blog.cloudflare.com/details-of-the-cloudflare-outage-on-july-2-2019/) (đã mở) |
| **Cloudflare, 18/11/2025** | tệp cấu hình Bot Management sinh tự động gấp đôi do thay đổi quyền DB; vượt giới hạn 200 feature, proxy panic, lỗi 5xx (11:20 đến 17:06 UTC) | kiểm tệp máy sinh như đầu vào người dùng; giữ bản cuối tốt; công tắc khẩn cấp | [18 November 2025 outage](https://blog.cloudflare.com/18-november-2025-outage/) (⚠ qua agent) |
| **Fastly, 08/06/2021** | lỗi tiềm ẩn từ bản triển khai 12/05 bị kích hoạt bởi một thay đổi cấu hình hợp lệ của khách; khoảng 85% mạng lỗi | một khách làm sập nền tảng dùng chung: cần cách ly | [Summary of June 8 outage](https://www.fastly.com/blog/summary-of-june-8-outage) (⚠ qua agent) |
| **Retry nhiều tầng (thiết kế)** | ngăn xếp 5 tầng, 3 lần thử mỗi tầng nhân tải 243 lần lên DB lỗi (ví dụ minh hoạ, không phải sự cố có ngày) | retry một chỗ, ngân sách retry; Google SRE: 3 tầng × 4 lần = 64 | [Timeouts, retries and backoff with jitter](https://builder.aws.com/content/3EumjoZascWd1oZiEgL8ORlv3qE/timeouts-retries-and-backoff-with-jitter) (⚠ qua agent); SRE book (đã mở) |

**Mô hình phổ biến:** một điểm vào làm routing, xác thực, rate limit, tracing; BFF hoặc gateway theo domain khi một gateway phình; ưu tiên giải pháp dựng sẵn của nền tảng.
**Chưa tìm được nguồn gốc đã mở cho:** postmortem công khai của sự cố gateway cụ thể trên AWS API Gateway hay ALB, và case load shedding của GitHub, Shopify; không nêu các case đó. Netflix API re-architecture chỉ thấy qua snippet (bài gốc trả 403).

### Đánh đổi khi thiết kế

- **Một gateway so với nhiều:** một gateway đơn giản nhưng thành nút thắt; nhiều gateway (BFF, theo domain) tách trách nhiệm nhưng nhân thành phần và code lặp.
- **Mỏng so với tiện:** càng nhiều logic ở gateway càng khó đổi; aggregation phức tạp đặt vào service riêng.
- **Tự dựng so với managed:** tuỳ biến bằng code đổi lại tự lo WAF, HA, quan sát; managed đổi lại bị gắn vào nhà cung cấp.
- **Cấu hình là mã:** đổi cấu hình nhanh nhưng phạm vi sự cố là toàn hệ thống.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Gateway khác reverse proxy, load balancer, service mesh, BFF thế nào?
2. Kể sáu vấn đề khi client gọi thẳng nhiều service và cách gateway giải quyết từng cái.
3. `PowerOfTwoChoices` hay `RoundRobin` cho cụm có một instance chậm? Vì sao?
4. Vì sao tin mọi `X-Forwarded-For` làm rate limit vô dụng?
5. Gateway có nên retry? Khi nào, ở tầng nào, và rủi ro nhân tải?
6. Cấu hình route sai kéo sập hệ thống: kể hai sự cố thật và biện pháp.
7. Rate limit cửa sổ cố định so với cửa sổ trượt: khác nhau thế nào ở ranh giới?
8. Khi nào **không** nên có gateway?

### Bài mở

#### Bài 1 — Thiết kế gateway cho TeamBoard giai đoạn nhiều service
Có identity, board, task, notification, activity. Thiết kế: một hay nhiều gateway, route, xác thực, rate limit, quan sát, nơi đặt TLS, khi nào cần đếm tập trung. **Tiêu chí:** nêu lý do chọn, tối thiểu hai phương án bị loại, kế hoạch khi gateway chết, rủi ro cấu hình.

#### Bài 2 — Xử lý điểm lệch với design
Lấy bảng điểm lệch ở Part 6 và đề xuất cách xử lý từng điểm (sửa design hay thêm code), kèm test để chứng minh. **Tiêu chí:** mỗi đề xuất trỏ tới Lab hoặc test; nêu rõ điểm nào cần người dùng chốt.

#### Bài 3 — Phản biện một thiết kế
Đồng nghiệp đề xuất "đặt retry 5 lần ở gateway cho mọi route, tắt rate limit để đỡ chặn nhầm, mở upstream ra Internet cho tiện". Viết phản biện nêu ít nhất 5 rủi ro đã học và phương án thay thế. **Tiêu chí:** mỗi rủi ro trỏ tới một Lab hay case.

### Tiêu chí thành thạo

- Giải thích không nhìn tài liệu: vai trò gateway, các cơ chế, mặc định quan trọng của YARP (502, 504, 400, 405; cân bằng tải mặc định).
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có số đo hoặc lab của chính bạn và nêu giới hạn của nó.
- Nói được ít nhất ba tình huống không nên có gateway.

## Tự kiểm tra

1. Case nào cho thấy cấu hình là một lần triển khai có thể kéo sập toàn nền tảng?
2. Stripe và Cloudflare 2017 khác nhau ở khía cạnh nào của rate limit?
3. Uber DOMA là edge gateway hay gateway theo domain?

<details>
<summary>Đáp án ngắn</summary>

1. Cloudflare 2019 (quy tắc WAF), Cloudflare 2025 và Fastly 2021 (qua agent).
2. Stripe: các lớp giới hạn và load shedding; Cloudflare: cách đếm rẻ và gần chính xác.
3. Gateway theo domain, không phải edge gateway.
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** Azure Architecture Center "API gateways"; Uber DOMA (23/07/2020); Stripe rate limiters (30/03/2017); Cloudflare "Details of the Cloudflare outage on July 2, 2019"; Cloudflare "How we built rate limiting capable of scaling to millions of domains" (07/06/2017); YARP 1.0 (09/11/2021); Google SRE "Addressing Cascading Failures".
**Chỉ qua agent (⚠):** Cloudflare 18/11/2025; Fastly 08/06/2021; Microsoft CETO 17/04/2024; SoundCloud BFF 18/09/2015; Netflix Zuul wiki; AWS Builders' Library về retry.
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ cơ chế đã học và từ lab, không phải sự cố đã gặp ở dự án này.
