# Part 1 — Fundamentals & Mental Models (Microservice)

> [!NOTE]
> **Mục tiêu:** hiểu microservice đổi cái gì lấy cái gì, ranh giới service cắt theo đâu, vì sao mỗi service sở hữu dữ liệu riêng, giao tiếp đồng bộ khác bất đồng bộ ra sao, và chọn có tách service hay không.
> **Cần biết trước:** [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** phân biệt monolith, modular monolith, microservice; giải thích vì sao chuỗi gọi đồng bộ giảm độ sẵn sàng; chọn giải pháp từ Bản đồ cho 3 tình huống.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Microservice và các khái niệm nền

#### Microservice
**Là gì:** chia ứng dụng thành các **service** nhỏ; mỗi service chạy tiến trình riêng, **sở hữu dữ liệu riêng**, bao quanh một năng lực nghiệp vụ, triển khai độc lập, nói chuyện qua mạng (REST hoặc event).
**Ví dụ đời thường:** một toà nhà văn phòng cho thuê từng tầng: mỗi công ty có chìa khoá, điện nước và nội quy riêng, chỉ gặp nhau ở sảnh. Dễ độc lập, nhưng muốn họp chung thì phải đi qua sảnh và hẹn giờ.
**Use case:** TeamBoard tách `board-service`, `task-service`, `notification-service`... để mỗi service chọn kiến trúc và lưu trữ riêng (PostgreSQL, SQL Server, DynamoDB).

```
Client ─► gateway ─┬─► identity-service ─► PostgreSQL
                   ├─► board-service    ─► SQL Server
                   └─► task-service     ─► PostgreSQL
                              │ event (SNS → SQS)
                              ├──► notification-service ─► DynamoDB
                              └──► activity-service     ─► DynamoDB
```

#### Ba kiểu triển khai gần nhau

| Kiểu | Triển khai | Dữ liệu | Gọi nhau |
|---|---|---|---|
| Monolith | một khối | một DB chung | gọi hàm |
| Modular monolith | một khối, chia module có API công khai | module sở hữu bảng riêng trong một DB | gọi hàm qua API module |
| Microservice | nhiều khối độc lập | mỗi service một DB | qua mạng |

#### Bounded context (ranh giới service)
**Là gì:** ranh giới trong đó một thuật ngữ nghiệp vụ có nghĩa nhất quán (DDD). Cắt service theo **năng lực nghiệp vụ**, không theo bảng hay tầng kỹ thuật.
**Use case:** "Board" và "Task" là hai ngữ cảnh; `task-service` chỉ giữ `boardId`, không giữ dữ liệu của Board.

#### Eventual consistency
Dữ liệu giữa các service nhất quán **sau một khoảng**, không tức thì (feed hoạt động trễ vài giây).

#### Distributed monolith
Tách service nhưng vẫn dính chặt (deploy cùng nhau, chung DB): chịu chi phí phân tán mà không có lợi ích. Dạng thất bại điển hình.

## L2 Why — Vì sao tách và vì sao đừng tách vội

**Nỗi đau của monolith lớn** (theo microservices.io, lực "dark energy"): sửa một dòng phải build và deploy cả khối; một chỗ rò bộ nhớ làm sập mọi chức năng; nhiều đội giẫm chân nhau; không scale riêng phần cần; một công nghệ cho mọi việc.

**Nhưng lực kéo ngược** ("dark matter"): tương tác đơn giản và hiệu quả hơn, ACID đơn giản hơn saga, ít ghép nối lúc chạy và lúc thiết kế. Microservice đổi các đau trên lấy: gọi qua mạng chậm và hay lỗi, không còn giao dịch ACID xuyên service, vận hành và debug khó hơn.

**Hệ quả toán học của chuỗi gọi đồng bộ** (Lab 1.1): mỗi service lỗi 5% độc lập, chuỗi 3 service thành công khoảng 85,7%, và độ trễ cộng dồn. Đây là lý do mọi cơ chế ở Part 2 đến 4 tồn tại.

## L3 How — Các cơ chế nền

#### a. Database-per-service
**Là gì:** mỗi service sở hữu DB riêng; dữ liệu của người khác lấy qua API hoặc event; ép bằng tài khoản DB và quyền riêng.
**Use case:** `board-service` không query bảng của `task-service`; muốn biết task thì gọi API hoặc nghe event.
**Ưu:** deploy, scale, đổi schema độc lập; chọn công nghệ lưu trữ hợp từng service. **Nhược:** mất ACID xuyên service (kéo theo outbox, saga ở Part 4); join xuyên service khó; dữ liệu nhân bản phải đồng bộ. Lab 1.2 cho thấy chung DB làm một lần đổi tên cột phá service khác.

#### b. Giao tiếp đồng bộ (REST/gRPC)
**Là gì:** gọi và **chờ** phản hồi.
**Use case:** truy vấn cần đáp án ngay (lấy chi tiết Board), API công khai qua gateway.
**Ưu:** đơn giản. **Nhược:** ghép nối lúc chạy: service kia chậm hay chết thì mình hỏng; chuỗi dài nhân độ trễ và lỗi (Lab 1.1).

#### c. Giao tiếp bất đồng bộ (event qua broker)
**Là gì:** gửi **event** qua broker rồi đi tiếp; bên quan tâm tự xử lý.
**Use case:** tạo Task thì phát `TaskAssigned` cho `notification-service` và `activity-service`.
**Ưu:** tách thời gian và độ sẵn sàng, fan-out nhiều bên. **Nhược:** eventual consistency, giao trùng, lệch thứ tự, khó theo dõi (Part 3).

#### d. Hợp đồng (API contract)
**Là gì:** thoả thuận về dạng request, response, event và lỗi giữa các service; thay đổi phải **tương thích ngược** (Part 2, Part 5).
**Use case:** thêm field `color` vào event mà consumer cũ vẫn chạy.

#### e. Triển khai độc lập
Mỗi service build, test, phát hành riêng. Nếu hai service luôn phải phát hành cùng nhau thì chúng là một.

| Cơ chế | Giải quyết | Cái giá chính | Part |
|---|---|---|---|
| Database-per-service | độc lập dữ liệu, deploy, scale | mất ACID xuyên service | 1, 4 |
| Đồng bộ | cần đáp án ngay | lỗi và độ trễ lan | 2 |
| Bất đồng bộ | nhiều bên quan tâm, tách thời gian | eventual consistency, trùng | 3 |
| Hợp đồng | các bên đổi độc lập | cần kỷ luật tiến hoá | 2, 5 |
| Outbox, saga | nhất quán xuyên service | thêm thành phần, bồi hoàn tự viết | 4 |
| Timeout, retry, bulkhead | chịu lỗi gọi đồng bộ | retry khuếch đại | 2, 4 |
| Quan sát xuyên service | lần ra lỗi | cần kỷ luật ở mọi service | 2, 5 |

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0. **Mô phỏng:** service giả bằng Kestrel trên `127.0.0.1`, lỗi được tiêm có chủ ý.

#### Lab 1.1 — Chuỗi gọi đồng bộ A → B → C
Ba service, mỗi service lỗi `500` độc lập 5% và trễ ~20 ms; 2000 yêu cầu tuần tự.
**Đầu ra thật:**
```text
gọi thẳng C:         thành công 93,9%  p50 30,9 ms  p99 32,4 ms
chuỗi A -> B -> C:   thành công 85,4%  p50 92,7 ms  p99 95,3 ms   (0,95^3 = 85,7%)
```
**Giải thích:** chuỗi 3 service thành công 85,4% (khớp 0,95³) và độ trễ khoảng gấp ba (p50 92,7 ms so với 30,9 ms; con số ~31 ms cho "20 ms" là độ phân giải bộ định giờ của máy). Lỗi độc lập nhân lên; độ trễ cộng dồn. Phép đo là hàm của lỗi được **tiêm**, không phải đo hệ thật.

#### Lab 1.2 — Chung DB làm hai service dính nhau (SQLite thật)
```text
task-service đọc thẳng bảng của board-service: đọc được: Sprint
board-service đổi tên cột title -> name (đúng việc của nó), task-service đọc lại: LỖI: SQLite Error 1: 'no such column: title'.
```
**Giải thích:** khi `task-service` truy cập thẳng bảng, một thay đổi nội bộ hợp lệ của `board-service` làm `task-service` hỏng; hai service phải phát hành cùng nhau: đó là distributed monolith. Qua API, cột bên trong đổi không ảnh hưởng người gọi.

## L7 Architecture — Chọn kiểu kiến trúc, và khi KHÔNG nên tách

### Bản đồ giải pháp

Microservice là **một kiểu** trong dải lựa chọn; chọn nó là quyết định đánh đổi.

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá |
|---|---|---|---|
| Monolith phân lớp | một ứng dụng, một DB | CRUD nhỏ, đội mới, sản phẩm còn đổi mạnh | logic dễ rải ra, khối lớn khó đổi |
| Modular monolith | Shopify: một deployable, module có API công khai, công cụ bắt vi phạm ranh giới | muốn ranh giới rõ mà không chịu chi phí phân tán, cần nhất quán mạnh | cần kỷ luật để module không rò rỉ; scale và deploy chung |
| Microservice | Segment (trước khi gộp), Netflix, TeamBoard | nhiều đội, cần deploy và scale độc lập, đã có DevOps | chi phí phân tán, có thể thành distributed monolith |
| Microservice có cấu trúc domain | Uber DOMA: khoảng 2.200 service gom thành khoảng 70 domain | số service rất lớn, cần giảm phối hợp | thêm lớp cấu trúc cần đầu tư |
| Serverless, event-driven functions | `activity-service` (Lambda, SNS/SQS, DynamoDB) | tải thất thường, xử lý theo sự kiện | khó debug và test cục bộ, giới hạn runtime |
| Di chuyển dần (Strangler Fig) | tách dần khỏi monolith đang chạy | có monolith đang chạy | chạy song song, chi phí tạm thời |
| **Không tách** | giữ một khối chia module | đội nhỏ, domain nhỏ, chưa đo thấy nhu cầu | giữ các đau của monolith lớn |

**Cách chọn:** hỏi (1) có cần deploy hay scale độc lập thật không, (2) có nhiều đội tự chủ không, (3) có nền DevOps (CI/CD, quan sát, hạ tầng tự động) chưa, (4) ranh giới đã đủ rõ chưa. Đi từ ít phân tán đến nhiều.

### Khi KHÔNG nên tách (theo các nguồn)
- Dự án mới, ranh giới chưa rõ: MonolithFirst (Fowler): ranh giới service khó đoán đúng lúc đầu và đổi trong monolith rẻ hơn; hầu hết hệ thành công bắt đầu bằng monolith.
- Đội nhỏ, domain nhỏ: "phí bảo hiểm" (MicroservicePremium) chỉ đáng khi độ phức tạp hệ thống vượt chi phí.
- Chưa có CI/CD, quan sát, hạ tầng tự động: không vận hành nổi nhiều service.
- Các thành phần luôn phát hành cùng nhau, cùng ngôn ngữ, một đội, scale theo một yếu tố: Istio gộp control plane thành `istiod` vì đúng các lý do này (Part 7).
- Cần nhất quán mạnh ở mọi thao tác: ACID trong một khối đơn giản hơn saga.

**Phạm vi dự án:** TeamBoard chia 6 service **chủ ý để học** (ADR 0001); đừng suy ra đó là khuyến nghị cho domain nhỏ.

## Tự kiểm tra

1. Chuỗi 5 service, mỗi service thành công 99%: xác suất cả chuỗi thành công?
2. Vì sao chung DB giữa hai service là dấu hiệu distributed monolith?
3. Kể hai điều kiện khiến nên giữ monolith.
4. Vì sao Segment gộp lại còn Uber gom thành domain?

<details>
<summary>Đáp án ngắn</summary>

1. 0,99⁵ ≈ 95,1% (Lab 1.1 cho cùng quy luật).
2. Thay đổi nội bộ của một service phá service kia, buộc phát hành cùng nhau (Lab 1.2).
3. Đội nhỏ hoặc domain nhỏ; ranh giới chưa rõ; thiếu DevOps; cần ACID mạnh.
4. Segment: 140+ service nhỏ cùng một đội và cùng ngôn ngữ, chi phí vận hành vượt lợi ích; Uber: quy mô rất lớn nên cần cấu trúc để kiểm soát số service.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 (HTTP thật, lỗi được tiêm) và Lab 1.2 (SQLite thật); đầu ra thật, môi trường ở Part 0.
**Đã tự mở:** Fowler "Microservices" (đặc điểm và cái giá); microservices.io `/patterns/microservices.html` (dark energy và dark matter, drawbacks); Segment "Goodbye Microservices" (10/07/2018), Shopify "Deconstructing the Monolith" (21/02/2019), Uber DOMA (23/07/2020) (các trang này đã mở ở các phiên trước và còn tải được).
**Chỉ qua agent (⚠):** Fowler MonolithFirst (03/06/2015) và MicroservicePremium (13/05/2015); microservices.io `database-per-service`; bảng ranh giới monolith, modular monolith, microservice là tổng hợp của mình, không trích một nguồn.
**Chưa kiểm:** "hầu hết hệ thành công bắt đầu bằng monolith" là nhận định của Fowler với bằng chứng ít (chính ông nói vậy), không phải số liệu.
