# Part 2 — Core Skills (Microservice)

> [!NOTE]
> **Mục tiêu:** gọi service này từ service khác một cách an toàn: timeout, hợp đồng lỗi, tiến hoá hợp đồng không phá người gọi, và lần ra được một request đi qua nhiều service.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** đặt timeout đúng chỗ và giải thích vì sao thiếu nó thì service giữ kết nối, thêm field vào hợp đồng mà không phá consumer, truyền correlation id qua ba service.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Bộ kỹ năng gọi nhau

#### Gọi đồng bộ có timeout
**Là gì:** mọi lời gọi qua mạng phải có giới hạn thời gian chờ; hết hạn thì coi là lỗi.
**Use case:** `board-service` gọi `identity-service` lấy tên người dùng và chờ tối đa 500 ms.

#### Hợp đồng lỗi
**Là gì:** một dạng lỗi thống nhất (ví dụ `problem+json` với `code` ổn định, `traceId`, `correlationId`) để người gọi phân biệt lỗi nghiệp vụ, lỗi của mình, lỗi của service kia.
**Use case:** client biết `RATE_LIMIT_EXCEEDED` khác `UPSTREAM_TIMEOUT` để quyết định chờ hay thử lại.

#### Tiến hoá hợp đồng (versioning)
**Là gì:** thay đổi request, response, event sao cho consumer cũ vẫn chạy: thêm field là an toàn, đổi tên hay xoá field thì không.
**Use case:** thêm `color` vào event `BoardCreated`; consumer cũ bỏ qua.

#### Correlation id
**Là gì:** một id đi xuyên mọi service (`X-Correlation-Id`, cùng `traceparent`) để gom log của một request.
**Use case:** người dùng báo lỗi; một id tìm ra log của gateway, board-service và task-service.

#### Sở hữu dữ liệu
Mỗi service là nơi **duy nhất** ghi dữ liệu của mình; người khác đọc qua API hoặc event.

## L2 Why — Vì sao các kỹ năng này là tối thiểu

Gọi qua mạng không như gọi hàm: nó có thể chậm, mất, hoặc trả về sau khi người gọi đã bỏ cuộc. Không có timeout thì một service chậm làm mọi service gọi nó giữ kết nối và tài nguyên đến khi cạn (Lab 2.1). Không có hợp đồng ổn định thì mỗi lần một đội đổi, đội khác hỏng (Lab 2.2). Không có correlation id thì không lần ra được lỗi (Lab 2.3).

## L3 How — Cơ chế đáng nhớ

### Timeout và cái giá của việc thiếu nó
- **Mỗi lời gọi** có timeout; chọn theo p99 của đích (AWS Builders' Library: chọn p99,9 nếu chấp nhận 0,1% timeout giả). Timeout tính cả thời gian thiết lập kết nối.
- **Timeout lồng nhau:** timeout của người gọi phải **lớn hơn** timeout của lời gọi con cộng thời gian xử lý, nếu không người gọi bỏ cuộc trong khi con vẫn chạy.
- **Lan truyền huỷ:** khi người gọi ngắt, service phía sau nên nhận được tín hiệu huỷ (`CancellationToken`) để không làm việc vô ích.
- **Khi hết hạn:** trả lỗi rõ (`504`) thay vì treo; quyết định có retry hay không ở Part 4.

### Hợp đồng lỗi
Theo `api-endpoints.md` của dự án: lỗi dạng `problem+json` với `code` ổn định (là một phần hợp đồng công khai), `traceId`, `correlationId`; không lộ host nội bộ hay stack. Người gọi **không** dựa vào thông điệp tự do mà vào `code` và mã trạng thái.

### Tiến hoá hợp đồng an toàn
- **An toàn (tương thích ngược):** thêm field tuỳ chọn, thêm endpoint, thêm giá trị enum nếu consumer bỏ qua giá trị lạ.
- **Phá vỡ:** đổi tên hay xoá field, đổi kiểu, đổi nghĩa, thêm field **bắt buộc**.
- **Tolerant reader:** consumer chỉ đọc field nó cần và **bỏ qua** phần còn lại. Mặc định của `System.Text.Json` là bỏ qua field lạ; bật `Disallow` thì thêm field cũng làm hỏng (Lab 2.2).
- **Cạm bẫy im lặng:** đổi tên hay xoá field không gây lỗi giải mã mà để giá trị `null` (Lab 2.2). Cần test hợp đồng (consumer-driven) hoặc kiểm bắt buộc ở nơi nhận.
- **Phá vỡ có chủ đích:** phát hành phiên bản mới song song (`/v2`, `BoardCreated.v2`), chạy cả hai cho tới khi consumer cũ chuyển xong (Part 5).

### Correlation và trace
Nhận id hợp lệ từ client hoặc tạo mới; **chuyển tiếp** ở mọi lời gọi ra ngoài (HTTP header, thuộc tính message); ghi vào mọi dòng log. Thiếu ở **một** hop là mất dấu từ đó trở đi (Lab 2.3). `traceparent` (W3C) cho trace phân tán; cần OpenTelemetry để tạo span ([observability.md](../../architecture/observability.md)).

### Sở hữu dữ liệu và API
Lấy dữ liệu của service khác bằng **API** hoặc **bản sao nhận qua event**; không đọc DB của người khác (Lab 1.2). Bản sao cục bộ là eventually consistent và phải chấp nhận cũ.

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0. Service giả bằng Kestrel trên `127.0.0.1`; hợp đồng đọc bằng `System.Text.Json`.

#### Lab 2.1 — Thiếu timeout làm B giữ hết yêu cầu
C chậm 1 giây; 60 yêu cầu đồng thời vào A → B → C.
**Đầu ra thật:**
```text
B không timeout:   B còn giữ 60/60 yêu cầu sau 400 ms; mã trả về 200 x 60; tổng 1096 ms
B timeout 100 ms:  B còn giữ  0/60 yêu cầu sau 400 ms; mã trả về 504 x 60; tổng 410 ms (gồm 400 ms chờ lấy mẫu)
```
**Giải thích:** không timeout thì cả 60 yêu cầu giữ kết nối ở B suốt 1 giây (nếu có hàng nghìn thì cạn kết nối và luồng); có timeout 100 ms thì B giải phóng sớm và A nhận `504` thay vì treo. Đổi lại người dùng nhận lỗi: đó là quyết định có chủ ý.

#### Lab 2.2 — Tiến hoá hợp đồng: thêm, đổi tên, xoá field
Consumer v1 đọc `{id, title}`:
```text
nhận đúng v1:                                          đọc được: StrictV1 { Id = 1, Title = Sprint }
nhận thêm field color (mặc định, bỏ qua field lạ):      đọc được: StrictV1 { Id = 1, Title = Sprint }
nhận thêm field color, chế độ Disallow:                 LỖI JsonException
nhận name thay cho title (đổi tên field):               đọc được: StrictV1 { Id = 1, Title = }   <- Title = null
nhận event bỏ field title (xoá field):                  đọc được: StrictV1 { Id = 1, Title = }   <- Title = null
```
**Giải thích:** thêm field an toàn nếu consumer tolerant; đổi tên hay xoá field **không báo lỗi** mà để `Title` rỗng, nghĩa là dữ liệu hỏng chảy tiếp. Cần kiểm hợp đồng và test, không dựa vào lỗi giải mã.

#### Lab 2.3 — Correlation id đi xuyên A → B → C
```text
-- B chuyển tiếp header:       A: correlationId=req-42   B: correlationId=req-42   C: correlationId=req-42
-- B KHÔNG chuyển tiếp header: A: correlationId=req-42   B: correlationId=req-42   C: correlationId=(không có)
```
**Giải thích:** thiếu chuyển tiếp ở B làm log của C không gắn được với request gốc. Lab chỉ kiểm header truyền qua HTTP, không có trace thật (OpenTelemetry).

## L5 Troubleshooting — Khi hai service gọi nhau hỏng

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Service ngốn kết nối rồi cạn khi một service khác chậm | thiếu timeout (Lab 2.1) | số kết nối đang chờ, độ trễ đích | timeout mọi lời gọi, `CancellationToken`, bulkhead (Part 4) |
| Người gọi hết hạn mà service con vẫn làm việc | timeout lồng nhau sai, không lan truyền huỷ | so timeout hai tầng | timeout cha > timeout con; truyền token huỷ |
| Dữ liệu rỗng sau khi một đội phát hành | đổi tên hoặc xoá field (Lab 2.2) | so schema trước sau, giá trị `null` bất thường | khôi phục field, phát hành song song hai phiên bản |
| Consumer lỗi giải mã sau khi thêm field | chế độ nghiêm ngặt, không tolerant | xem cấu hình serializer | bỏ qua field lạ; test hợp đồng |
| Không lần ra lỗi xuyên service | mất correlation ở một hop (Lab 2.3) | tìm id trong log từng service | chuyển tiếp header và thuộc tính message ở mọi lời gọi |
| Người dùng thấy dữ liệu cũ sau khi sửa | bản sao cục bộ chưa nhận event | so thời điểm ghi với thời điểm event tới | chấp nhận eventual consistency hoặc gọi API |
| Một service đổi schema làm service khác hỏng | chung DB (Lab 1.2) | xem ai truy cập bảng | database-per-service, đi qua API |
| `504` lẫn lộn với lỗi nghiệp vụ | hợp đồng lỗi không có `code` ổn định | xem thân lỗi | `problem+json` với `code` |

## Tự kiểm tra

1. Vì sao timeout của A phải lớn hơn timeout của lời gọi A→B?
2. Thêm field và đổi tên field khác nhau thế nào với consumer cũ?
3. Vì sao đổi tên field nguy hiểm hơn thêm field kể cả khi không có lỗi giải mã?
4. Thiếu correlation ở hop B thì mất gì?

<details>
<summary>Đáp án ngắn</summary>

1. Nếu không A bỏ cuộc trong khi B vẫn đang chờ C và làm việc vô ích.
2. Thêm field: consumer bỏ qua; đổi tên: giá trị mới nằm ở field consumer không đọc nên field cũ thành `null` (Lab 2.2).
3. Không có lỗi để phát hiện: dữ liệu rỗng chảy tiếp.
4. Log của C và các hop sau không gắn được với request gốc (Lab 2.3).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.3 (HTTP thật với lỗi được tiêm; `System.Text.Json`; đầu ra thật).
**Đã tự mở:** Fowler "Microservices" (decentralized data management, design for failure).
**Chỉ qua agent (⚠):** microservices.io `database-per-service`; AWS Builders' Library "Timeouts, retries and backoff with jitter" (chọn timeout theo p99,9, kết nối nằm trong timeout); Azure pattern Retry (không lồng retry).
**Chưa kiểm:** consumer-driven contract test (Pact), `grpc`, lan truyền huỷ qua nhiều tiến trình thật, `traceparent` với OpenTelemetry.
