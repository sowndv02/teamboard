# Part 3 — Multi-component (Microservice)

> [!NOTE]
> **Mục tiêu:** giao tiếp bất đồng bộ qua broker đúng: hiểu giao ít nhất một lần và lệch thứ tự, viết consumer idempotent, xử lý message độc bằng DLQ, và chọn giữa các kiểu thông điệp.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** giải thích vì sao event bị giao trùng, dedupe theo `eventId` trong một giao dịch, nêu thứ tự "đánh dấu" và "tác động" nào gây mất hay trùng, cấu hình DLQ.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Thông điệp giữa các service

#### Broker, hàng đợi, pub/sub
**Broker:** thành phần trung gian lưu và chuyển thông điệp (SNS, SQS, Kafka, RabbitMQ). **Hàng đợi:** mỗi message tới một consumer của hàng đó. **Pub/sub:** một event tới nhiều bên.
**Use case:** `board-service` phát `BoardCreated`; `notification-service` và `activity-service` mỗi bên có hàng đợi riêng (SNS fan-out tới SQS, ADR 0002).

#### Giao ít nhất một lần (at-least-once)
Broker bảo đảm message **không mất** nhưng có thể **tới nhiều lần**. Exactly-once thực tế không đạt; đạt "hiệu lực một lần" bằng consumer idempotent.

#### Consumer idempotent
**Là gì:** xử lý một message hai lần cho kết quả như một lần.
**Use case:** gửi thông báo cho `BoardCreated` có `eventId = 17` đúng một lần dù broker giao hai lần.

#### Dead-letter queue (DLQ)
Hàng đợi chứa message bị nhận quá `maxReceiveCount` lần mà vẫn lỗi (message độc).

## L2 Why — Vì sao broker làm khác gọi đồng bộ

Bất đồng bộ tách thời gian và độ sẵn sàng: consumer chết thì message nằm chờ, người phát không bị ảnh hưởng, và nhiều bên cùng nhận. Đổi lại, các đảm bảo mạnh của gọi hàm biến mất: không còn "gọi một lần thì chạy một lần". Mọi consumer phải được thiết kế cho việc bị giao trùng, lệch thứ tự và kẹt message độc (Lab 3.1 đến 3.3).

## L3 How — Cơ chế đáng nhớ

### Giao trùng, lệch thứ tự
- **SQS standard:** giao ít nhất một lần, thứ tự "best-effort" (có thể đảo); lưu bản sao ở nhiều AZ.
- **SQS FIFO:** thứ tự nghiêm ngặt **trong cùng `MessageGroupId`**; khử trùng **phía gửi** trong cửa sổ 5 phút (bằng `MessageDeduplicationId` hay nội dung). Nhưng consumer vẫn có thể thấy message giao lại khi hết visibility timeout, nên **vẫn cần idempotent**.
- **Hệ quả cho thiết kế:** không giả định thứ tự giữa các message; nếu cần thứ tự theo một thực thể, dùng khoá nhóm hoặc số phiên bản trong event để bỏ event cũ.

### Visibility timeout và redelivery
Khi consumer nhận một message, nó tạm ẩn trong **visibility timeout** (mặc định 30 giây, từ 0 đến 12 giờ); không xoá (ack) kịp thì message hiện lại và bị giao lần nữa. Consumer chậm hơn timeout nên gây trùng. AWS ghi không có bảo đảm tuyệt đối chống giao lại trong timeout.

### Idempotent consumer
Cách thường gặp (microservices.io, Azure): lưu **`eventId` đã xử lý** trong một bảng (inbox) và kiểm trước khi xử lý.
- **Điểm tinh tế:** hành động "đánh dấu đã xử lý" và "tác động" phải **nhất quán**. Nếu cùng DB: đặt cả hai trong **một giao dịch**, khoá chính trên `eventId` chặn xử lý song song (Lab 3.1).
- **Tác động ngoài DB** (gửi email, gọi API): không thể nằm trong giao dịch DB. Hai thứ tự đều có điểm yếu (Lab 3.2): đánh dấu trước rồi làm thì sự cố ở giữa **mất việc**; làm trước rồi đánh dấu thì sự cố ở giữa **làm trùng**. Giải pháp: làm cho chính tác động idempotent (mã yêu cầu ổn định gửi tới bên ngoài, như `Idempotency-Key` của Stripe), hoặc chấp nhận trùng hiếm.
- **Khoá dedupe ổn định:** dùng `eventId` do **người phát** tạo, không dùng id sinh lại của transport; thời hạn lưu phải lớn hơn cửa sổ giao lại của broker và cả việc chạy lại từ DLQ (Azure).
- **Thao tác tự nhiên idempotent:** ghi trạng thái tuyệt đối ("đặt tên Board là X") dễ hơn "cộng 1".

### DLQ và message độc
`maxReceiveCount` (API mặc định 10; console cho 1 đến 1.000): vượt thì message sang DLQ. DLQ phải cùng loại, cùng tài khoản và vùng với hàng gốc và tạo trước. Với hàng standard, thời hạn lưu của message trong DLQ tính từ thời điểm vào hàng **gốc**, nên đặt retention của DLQ dài hơn. Phải có cảnh báo khi DLQ khác rỗng và quy trình xem, sửa và chạy lại.

### Chọn kiểu thông điệp
Theo microservices.io: năm kiểu (request/response, notification, request/async response, pub/sub, publish/async response). Trong dự án: REST qua gateway cho client; SNS fan-out tới SQS cho event giữa service; chưa dùng Kafka (ADR 0002).

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0. **Mô phỏng:** broker là danh sách trong bộ nhớ giao trùng và xáo trộn; DB là SQLite thật cho phần dedupe.

#### Lab 3.1 — Giao trùng và dedupe theo `eventId`
500 event, mỗi event giao 1 đến 3 lần, thứ tự xáo trộn.
**Đầu ra thật:**
```text
số lượt giao: 987 cho 500 event
không dedupe: gửi 987 thông báo (đúng ra 500)
dedupe theo eventId (đánh dấu và tác động trong CÙNG giao dịch): thông báo 500, trùng 0
```
**Giải thích:** không dedupe thì gần gấp đôi thông báo. Khoá chính trên `eventId` làm lần chèn thứ hai thất bại và giao dịch rollback cả "tác động" lẫn "đánh dấu", nên kết quả đúng 500.

#### Lab 3.2 — Tác động ngoài DB: hai thứ tự, 10% sự cố giữa hai bước
500 event; broker giao lại cho tới khi consumer báo xong.
**Đầu ra thật:**
```text
đánh dấu trước rồi gửi: đã gửi 455 lần, event được gửi ít nhất một lần 455 -> mất 45, trùng 0
gửi trước rồi đánh dấu: đã gửi 547 lần, event được gửi ít nhất một lần 500 -> mất 0, trùng 47
```
**Giải thích:** đánh dấu trước thì sự cố làm **mất** 45 việc; gửi trước thì **trùng** 47 lần. Với bên ngoài không idempotent không có thứ tự nào đúng tuyệt đối: phải chọn mất hay trùng hiếm là chấp nhận được, hoặc làm tác động idempotent. Tỉ lệ 10% là giả định của lab.

#### Lab 3.3 — DLQ và message độc
100 message, 3 message độc luôn lỗi (mô hình hàng đợi; `maxReceiveCount` = 3):
```text
không có DLQ:        xử lý xong 97/97 message tốt; lượt nhận 5000 (dừng bởi chốt an toàn của lab); trong DLQ 0; còn kẹt trong hàng đợi 3
maxReceiveCount = 3: xử lý xong 97/97 message tốt; lượt nhận 106; trong DLQ 3; còn kẹt trong hàng đợi 0
```
**Giải thích:** không DLQ, ba message độc quay vòng vô hạn và nuốt phần lớn lượt nhận (5000 so với 106 lượt); có DLQ thì chúng bị cách ly sau 3 lần nhận. Mô hình không có visibility timeout thật, nên không đo độ trễ.

## L5 Troubleshooting — Khi consumer nhận event

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Người dùng nhận hai thông báo | giao trùng, consumer không idempotent (Lab 3.1) | đếm theo `eventId` | inbox có khoá chính, cùng giao dịch với tác động |
| Event bị bỏ sót sau sự cố | đánh dấu trước, tác động sau (Lab 3.2) | so số event với số tác động | tác động idempotent; đảo thứ tự và chấp nhận trùng |
| Cùng message xử lý song song hai lần | visibility timeout ngắn hơn thời gian xử lý | so thời gian xử lý với timeout | tăng timeout hoặc gia hạn; khoá chính chặn song song |
| Trạng thái cũ ghi đè trạng thái mới | thứ tự không đảm bảo | so số phiên bản | số phiên bản trong event, bỏ event cũ; FIFO theo nhóm |
| Hàng đợi tắc, tải xử lý nhân lên | message độc quay vòng (Lab 3.3) | số lần nhận cao ở vài message | `maxReceiveCount` và DLQ có cảnh báo |
| DLQ đầy mà không ai biết | thiếu cảnh báo | độ sâu DLQ | cảnh báo khi DLQ khác rỗng; quy trình xử lý |
| Event ở DLQ biến mất | retention DLQ ngắn hơn hàng gốc | xem cấu hình retention | đặt retention DLQ dài hơn |
| Consumer nhận phong bì SNS thay vì dữ liệu | thông điệp SNS bọc trong JSON envelope | xem thân message | giải nén `Message` hoặc bật raw message delivery |

## L7 Architecture — Chọn kiểu thông điệp

| Nhu cầu | Chọn | Cái giá |
|---|---|---|
| Cần đáp án ngay | gọi đồng bộ (REST) kèm timeout | ghép nối lúc chạy |
| Nhiều bên quan tâm một sự kiện | SNS fan-out tới SQS mỗi consumer | giao trùng, cần idempotent và DLQ |
| Cần thứ tự theo thực thể | SQS FIFO với `MessageGroupId` theo thực thể | thông lượng thấp hơn, một message kẹt giữ cả nhóm |
| Thông lượng rất cao, phát lại lịch sử | Kafka | nặng và đắt trên AWS; ADR 0002 chưa dùng |
| Việc nền một-một đơn giản | hàng đợi SQS standard | không đảm bảo thứ tự |
| Cần kết quả sau, báo bất đồng bộ cho client | request/async response, trả `202` và theo dõi | client phải xử lý trạng thái |
| Hai service luôn đi cùng nhau, chung đội | gọi hàm trong một tiến trình | không tách độc lập |

**Cách chọn:** hỏi (1) người gọi cần kết quả ngay không, (2) có nhiều bên quan tâm không, (3) mất hay trùng message chịu được đến đâu, (4) thứ tự có quan trọng không. Mặc định dự án ở ADR 0002 là SNS/SQS; hành vi LocalStack có thể lệch nhẹ so với AWS thật.

## Tự kiểm tra

1. Vì sao SQS FIFO vẫn cần consumer idempotent?
2. Đánh dấu trước rồi gửi email khác gửi trước rồi đánh dấu thế nào về mất và trùng?
3. Vì sao đặt "đánh dấu" và "tác động" trong cùng một giao dịch giải được bài toán?
4. Khi nào nên đưa message sang DLQ?

<details>
<summary>Đáp án ngắn</summary>

1. Khử trùng chỉ ở phía gửi; message vẫn bị giao lại khi hết visibility timeout.
2. Đánh dấu trước: mất việc khi sự cố ở giữa; gửi trước: làm trùng (Lab 3.2).
3. Cả hai cùng thành hay cùng không; lần chèn trùng làm giao dịch rollback (Lab 3.1).
4. Khi vượt `maxReceiveCount` mà vẫn lỗi (message độc), để cách ly và xử lý riêng.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 đến 3.3 (SQLite thật cho phần dedupe; broker và hàng đợi là mô hình trong bộ nhớ; đầu ra thật).
**Đã tự mở:** ADR 0002 của dự án (SNS fan-out tới SQS, outbox, idempotent consumer).
**Chỉ qua agent (⚠):** AWS SQS (standard queues, visibility timeout 30 giây, retention 4 ngày, `maxReceiveCount`, FIFO khử trùng 5 phút, `MessageGroupId`), SNS tới SQS; microservices.io `idempotent-consumer` và `messaging`; Azure pattern Idempotent Consumer; Stripe idempotency (xem Part 7).
**Chưa kiểm:** SQS và SNS thật hoặc LocalStack; hành vi visibility timeout và redelivery thật; Kafka; exactly-once của một broker cụ thể.
