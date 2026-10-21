# Khung ra quyết định

Dùng ở bước 3–4 của skill. Danh mục các quyết định thường gặp, tiêu chí đánh giá, và gợi ý "khi nào chọn gì". Đây là điểm xuất phát để suy nghĩ, **không phải đáp án**: luôn đối chiếu với drivers trong spec.

## 1. Thuộc tính chất lượng (tiêu chí)

| Thuộc tính | Câu hỏi |
|---|---|
| Nhất quán | Có chấp nhận dữ liệu lệch tạm thời? Bao lâu? |
| Sẵn sàng | Ngừng bao lâu là chịu được? Có điểm lỗi đơn? |
| Độ trễ | p95 mục tiêu? Đồng bộ hay chấp nhận chờ? |
| Thông lượng & mở rộng | Tải hiện tại, tăng thế nào, nghẽn ở đâu trước? |
| Độ bền dữ liệu | Mất dữ liệu có chấp nhận được không? |
| Độ phức tạp vận hành | Bao nhiêu thành phần phải chạy, giám sát, nâng cấp? |
| Khả năng test & debug | Tái hiện lỗi, truy vết dễ không? |
| Bảo mật | Bề mặt tấn công, phân quyền, dữ liệu nhạy cảm |
| Chi phí | Tiền (AWS) và công sức (đội nhỏ) |
| Khả năng đảo ngược | Chọn sai thì sửa tốn thế nào? |
| Phù hợp ràng buộc | Stack, kiến trúc bắt buộc, AWS đã chốt |

Chỉ chọn 4–6 tiêu chí quan trọng nhất cho quyết định đang xét, ghi trọng số (cao/vừa/thấp). Quyết định **khó đảo ngược** (mô hình dữ liệu, ranh giới service, contract công khai) cần thận trọng hơn quyết định dễ đổi.

## 2. Danh mục quyết định và cách cân nhắc

Với mỗi mục: *chọn A khi…*, *chọn B khi…*, *cạm bẫy*.

### 2.1 Ranh giới service (một service hay nhiều)
- **Một khối (modular monolith)** khi: đội nhỏ, domain còn đang học, nhất quán mạnh quan trọng, tải thấp.
- **Tách service** khi: vòng đời triển khai/độc lập mở rộng khác nhau, ranh giới dữ liệu rõ, đội/quyền sở hữu tách.
- Cạm bẫy: tách quá sớm tạo "distributed monolith", giao dịch xuyên service, nhiều call đồng bộ. (Dự án này cố ý tách để luyện: gắn `[Chọn để học]`.)

### 2.2 Giao tiếp: đồng bộ (REST/gRPC) và bất đồng bộ (sự kiện/hàng đợi)
- **Đồng bộ** khi: cần kết quả ngay để tiếp tục, luồng truy vấn, tải thấp, phụ thuộc đơn giản.
- **Bất đồng bộ** khi: tách rời thời gian/độ sẵn sàng, tác vụ nền, fan-out nhiều bên nhận, làm phẳng đỉnh tải.
- Cạm bẫy: chuỗi call đồng bộ làm nhân độ trễ và xác suất lỗi; bất đồng bộ cần idempotency, DLQ, quan sát khó hơn, nhất quán cuối cùng.

### 2.3 Hàng đợi (SQS) và luồng (Kafka/Kinesis) và pub/sub (SNS)
- **SQS** khi: tác vụ nền, mỗi message một consumer xử lý, retry/DLQ, không cần phát lại lịch sử.
- **SNS + SQS (fan-out)** khi: một sự kiện cho nhiều consumer độc lập.
- **Luồng có log (Kafka/Kinesis)** khi: cần phát lại, thứ tự theo khoá, thông lượng rất lớn, nhiều consumer đọc cùng lịch sử.
- Cạm bẫy: luồng tốn vận hành/chi phí; chọn khi cần phát lại thật sự.

### 2.4 Nhất quán giữa service
- **Giao dịch cục bộ + outbox** khi: cần "ghi dữ liệu và phát sự kiện" nguyên tử trong một service.
- **Saga (choreography/orchestration)** khi: quy trình nhiều bước xuyên service cần bồi hoàn. Choreography: ít thành phần, khó theo dõi khi nhiều bước. Orchestration: rõ luồng, thêm điểm trung tâm.
- **Hai pha (2PC)** hiếm khi phù hợp với hệ phân tán hiện đại; nêu lý do nếu cân nhắc.
- Cạm bẫy: dual write (ghi DB rồi publish riêng) làm mất/trùng sự kiện.

### 2.5 Lưu trữ dữ liệu
- **Quan hệ (SQL Server/PostgreSQL)** khi: ràng buộc toàn vẹn, truy vấn đa dạng, giao dịch, dữ liệu có cấu trúc.
- **Khoá-giá trị/tài liệu (DynamoDB)** khi: truy cập theo khoá rõ ràng, tải lớn dự đoán được, ghi nhanh, mô hình truy cập đã biết trước.
- **Event store** khi: cần lịch sử bất biến, kiểm toán, tái dựng trạng thái (đi kèm CQRS/ES).
- **Tìm kiếm (OpenSearch)** khi: tìm theo văn bản/lọc phức tạp mà SQL không đáp ứng.
- Cạm bẫy: NoSQL không có join/ad-hoc query, đổi mô hình truy cập sau rất đau; chọn công nghệ lạ chỉ vì mốt.

### 2.6 CQRS và Event Sourcing
- **CRUD thường** khi: đọc/ghi cùng mô hình, domain đơn giản.
- **CQRS** khi: mô hình đọc và ghi khác nhau rõ, tỷ lệ đọc/ghi lệch lớn, nhiều view đọc.
- **Event Sourcing** khi: cần lịch sử đầy đủ/audit, "du hành thời gian", nghiệp vụ gắn với sự kiện.
- Cạm bẫy: độ phức tạp, tiến hoá schema sự kiện, eventual consistency giữa ghi và đọc. (Task-service chọn để luyện: gắn nhãn.)

### 2.7 Bộ nhớ đệm (cache)
- **Không cache** khi: tải thấp, dữ liệu thay đổi liên tục, truy vấn đã nhanh.
- **Cache-aside (Redis/ElastiCache, in-memory)** khi: đọc nhiều, dữ liệu chịu được hơi cũ, truy vấn đắt.
- Cạm bẫy: vô hiệu hoá cache (invalidation), cache stampede, nhất quán, thêm một thành phần vận hành. Đo trước khi cache.

### 2.8 Xử lý trùng lặp và độ tin cậy
- **Idempotency key** ở API ghi có thể bị gửi lại; **dedupe theo `eventId`** ở consumer.
- **Retry** chỉ cho thao tác idempotent, có backoff + jitter, có giới hạn; kèm circuit breaker.
- **At-least-once + idempotent** thường thực tế hơn "exactly-once".

### 2.9 Truy vấn và phân trang
- **Offset/page** khi: dữ liệu vừa, cần nhảy trang, thứ tự ổn định.
- **Cursor/keyset** khi: dữ liệu lớn, tiếp nối liên tục, ổn định khi có chèn mới.

### 2.10 Xác thực và phân quyền
- **Token do dịch vụ nhận dạng cấp (JWT)** + kiểm tra ở gateway; từng service vẫn tự kiểm tra quyền theo tài nguyên.
- **IdP có sẵn (Cognito/OIDC)** khi: không muốn tự quản mật khẩu/MFA. **Tự xây** khi: mục tiêu học hoặc yêu cầu đặc thù (gắn `[Chọn để học]`).
- Cạm bẫy: lưu token phía client, thu hồi token, refresh.

### 2.11 Triển khai và tính toán
- **Container (ECS Fargate)** khi: dịch vụ chạy liên tục, cần kiểm soát runtime, tải ổn định.
- **Serverless (Lambda)** khi: tải thất thường/sự kiện, ít việc vận hành, chấp nhận giới hạn thời gian/khởi động nguội.
- **Kubernetes** khi: cần hệ sinh thái/độ linh hoạt lớn và có người vận hành; thường quá nặng cho đội nhỏ.

### 2.12 Multi-tenancy và dữ liệu nhiều khách (nếu có)
- DB riêng mỗi khách: cô lập mạnh, đắt. Schema riêng: trung gian. Chung bảng + `tenant_id`: rẻ, cần kỷ luật lọc dữ liệu (rủi ro rò rỉ).

## 3. Phương pháp so sánh

1. Chốt 4–6 tiêu chí và trọng số **từ spec** (không từ sở thích).
2. Với mỗi phương án và tiêu chí: Tốt/Vừa/Kém + một dòng lý do.
3. Đừng cộng điểm giả chính xác; nhìn xem phương án nào **vi phạm tiêu chí trọng số cao** (loại ngay) và phương án nào thắng ở tiêu chí còn lại.
4. Thử "phương án đơn giản nhất": có đáp ứng NFR không? Nếu có, cần lý do mạnh để chọn phức tạp hơn.
5. Kiểm tra **khả năng đảo ngược**: nếu chọn sai, đổi tốn gì? Ưu tiên lựa chọn dễ đổi khi chưa chắc.
6. Ghi **revisit trigger** bằng chỉ số đo được.

## 4. Mẫu đoạn "vì sao không chọn B"

> **Không chọn B (Kafka)** vì spec chỉ cần ~50 sự kiện/giây và không có yêu cầu phát lại; B đắt và tăng vận hành. **B sẽ đúng khi** cần phát lại lịch sử cho consumer mới hoặc vượt ~10.000 sự kiện/giây với thứ tự theo khoá. **Tín hiệu xem lại:** consumer mới cần dữ liệu quá khứ hoặc độ trễ SQS vượt ngưỡng.

## 5. Nhãn `[Cần]` / `[Chọn để học]`

- `[Cần]`: yêu cầu/NFR trong spec buộc phải có, hoặc ràng buộc đã chốt.
- `[Chọn để học]`: không cần cho yêu cầu hiện tại nhưng chọn để luyện kiến trúc (khớp mục tiêu dự án). Luôn kèm "nếu làm sản phẩm thật, phương án đơn giản hơn là …", **cách dự án thực tế thường làm** và **mức xa thực tế**.
  - *Gần*: đúng cách thực tế dùng, hoặc bản thu nhỏ hợp lý (ví dụ tách identity thành service riêng, dùng thư viện JWT chuẩn và học qua test tấn công). Mặc định.
  - *Xa*: khác hẳn thực tế (ví dụ tự viết mật mã/giao thức/JWT thay vì thư viện chuẩn, hạ tầng nặng hơn nhiều so với tải). Chỉ khi người dùng xác nhận rõ, nêu rủi ro, và giới hạn trong lab hoặc phạm vi ngoài đường chính; không thì đổi sang phương án *Gần*.
  - Mục đích học không biện minh cho việc đi xa thực tế: học bằng cách làm giống thực tế (test, vận hành, quan sát, đọc mã nguồn, thử tấn công).
