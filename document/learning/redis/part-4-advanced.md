# Part 4 — Advanced (Redis)

> [!NOTE]
> **Mục tiêu:** hiểu các tính năng quy mô lớn mà Redis 8 thêm hoặc làm khác Valkey: khoá an toàn bằng `DELEX`, atomic slot migration, I/O threading, hot key, compact hash; và phân biệt điều **kiểm được** với điều chỉ là tuyên bố của hãng.
> **Cần biết trước:** [Part 3](part-3-multi-component.md), [Valkey Part 4](../valkey/part-4-advanced.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** viết khoá an toàn bằng `DELEX`, kể tên lệnh di chuyển slot ở hai bên, giải thích vì sao không dùng quảng cáo "nhanh hơn X%" để chọn engine.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Tính năng quy mô lớn của Redis 8

#### Khoá an toàn bằng `DELEX`
**Là gì:** `DELEX khoá IFEQ giá-trị` xoá khoá chỉ khi giá trị đúng chủ (Redis 8.4).
**Use case:** mở khoá phân tán mà không cần script Lua; kết hợp `SET ... NX PX` để lấy khoá (xem [Valkey Part 4](../valkey/part-4-advanced.md) về giới hạn của khoá phân tán).

#### Atomic slot migration
**Là gì:** chuyển slot giữa các node cluster **nguyên tử** bằng một lệnh, thay vì chuyển từng key. Redis 8.4 có `CLUSTER MIGRATION`; Valkey 9.0 có `CLUSTER MIGRATESLOTS`.
**Use case:** thêm node vào cluster mà không phải di chuyển từng key bằng `MIGRATE`.

#### I/O threading
**Là gì:** dùng luồng phụ cho I/O mạng và phân tích lệnh (lệnh vẫn thực thi tuần tự). Cả hai bên đã làm lại phần này (Redis 8.0; Valkey 8.0).
**Use case:** tăng thông lượng trên máy nhiều lõi khi I/O là nút cổ chai.

#### Hot key và compact hash
**Hot key:** Redis 8.10 có lệnh `HOTKEYS` (thấy trong danh sách lệnh, Lab 2.1). **Compact hash:** mã hoá hash mới ở Redis 8.10 cùng lệnh `HIMPORT` (release notes).

## L2 Why — Vì sao quy mô làm hai bên rẽ hướng

Cùng một nhu cầu ("di chuyển slot an toàn", "tăng thông lượng") được hai bên giải bằng hai cách đặt tên và tham số khác nhau, vì sau 7.2.4 không còn mã chung. Hệ quả cho người dùng: **runbook vận hành** (di chuyển slot, tìm hot key, tinh chỉnh luồng) **không chuyển nguyên** giữa Redis và Valkey.

## L3 How — Cơ chế đáng nhớ

### Khoá và so-sánh-rồi-xoá
Lấy khoá `SET khoá chủ NX PX hạn`; mở khoá bằng **một** lệnh compare-and-delete: `DELEX khoá IFEQ chủ` (Redis 8.4 trở lên). Với Valkey dùng `DELIFEQ` (Lab 2.2: có lệnh, **chưa chạy**) hoặc Lua. Giới hạn của loại khoá này (hết hạn khi chủ còn chạy, không bảo vệ tính đúng nếu không có fencing token) giống hệt Valkey: Kleppmann và Redlock (xem [Valkey Part 4](../valkey/part-4-advanced.md)); đổi lệnh không đổi bản chất.

### Di chuyển slot
- **Redis:** `CLUSTER MIGRATION` (8.4, release notes "atomic slot migration"); lệnh `trimslots` cũng có ở danh sách lệnh 8.10.2.
- **Valkey:** `CLUSTER MIGRATESLOTS`, `CLUSTER CANCELSLOTMIGRATIONS`, `CLUSTER GETSLOTMIGRATIONS`, `CLUSTER FLUSHSLOT` (9.0; có trong Lab 2.1).
- Cả hai: snapshot slot và chuyển một lần, hai node cùng hoạt động. Lab 2.1 chỉ chứng minh lệnh **tồn tại**; chưa dựng cluster nhiều node (⚠).

### I/O threading
Cấu hình `io-threads` (mặc định tắt: `io_threads_active:0` ở cả ba server, Lab 1.1). Redis 8.0 nói "triển khai I/O threading mới"; Redis 8.4 nói hơn 30% cho tải cache (4 lõi, 10% SET, 90% GET), Valkey 8.0 RC nói 1,2 triệu QPS so với 380 nghìn trước đó, Valkey 9.0 nói tăng tới 40% so với 8.1. **Đây đều là tuyên bố của hãng**, đo trên cấu hình riêng của từng bên; chúng **không** cho phép kết luận "Redis nhanh hơn Valkey" hay ngược lại (không cùng điều kiện, không tái lập ở đây).

### Hot key, compact hash
`HOTKEYS` (Redis 8.10) và `redis-cli --hotkeys`/`valkey-cli --hotkeys` (cần chính sách LFU) là hai cách khác nhau; compact hash là mã hoá nội bộ mới (tiết kiệm bộ nhớ theo release notes). ⚠ Cả hai chưa chạy.

### Kết luận về "nên đo gì"
Muốn chọn engine theo hiệu năng thì phải **tự đo trên tải của mình, cùng phần cứng, cùng cấu hình, cùng client**, và báo khoảng giá trị, không lấy số của hãng.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0. Tất cả số liệu là đầu ra thật của lần chạy trên Redis 8.10.2; lab chỉ chứng minh hành vi lệnh, **không** đo hiệu năng.

#### Lab 4.1 — Khoá an toàn bằng `DELEX`
```text
SET lock worker-A          -> OK
DELEX lock IFEQ worker-B   -> 0          (không phải chủ: không xoá)
GET lock                   -> worker-A
DELEX lock IFEQ worker-A   -> 1          (đúng chủ: xoá)
GET lock                   -> (nil)
```
**Giải thích:** một lệnh nguyên tử thay cho script Lua của Lab 2.1 ở lộ trình Valkey; kết quả tương đương (B không xoá được khoá của A). Hạn chế như đã nói: không giải được bài toán "chủ chạy quá hạn".

#### Lab 4.2 — Lệnh di chuyển slot tồn tại ở bên nào
Từ Lab 2.1: Redis 8.10.2 có `cluster|migration` và `trimslots`; Valkey 9.1.2 có `cluster|migrateslots`, `cluster|cancelslotmigrations`, `cluster|getslotmigrations`, `cluster|flushslot`; Redis 8.10.2 không có các lệnh `migrateslots` và ngược lại.
**Giải thích:** hai bên có **hai bộ lệnh khác tên** cho cùng ý tưởng; công cụ vận hành cluster viết cho một bên không chạy ở bên kia. Chưa dựng cluster để chạy thử.

## L5 Troubleshooting — Khi quy mô làm lộ khác biệt

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Runbook cluster lỗi sau khi đổi engine | lệnh di chuyển slot khác tên (Lab 4.2) | `COMMAND INFO` trên server thật | runbook riêng cho từng engine |
| `DELEX` không tồn tại | Redis < 8.4 hoặc Valkey | `COMMAND INFO DELEX` | `DELIFEQ` (Valkey) hoặc Lua |
| Hai worker cùng giữ khoá | khoá hết hạn khi chủ còn chạy | so thời gian chạy với TTL | gia hạn, fencing token ([Valkey Part 4](../valkey/part-4-advanced.md)) |
| Bật `io-threads` mà không nhanh hơn | I/O không phải nút cổ chai, hay ít lõi | `INFO`, `io_threads_active`, CPU | đo trước và sau trên tải thật |
| Quyết định chọn engine dựa trên bảng benchmark của hãng | số đo khác điều kiện | đọc phương pháp đo | tự đo trên tải của mình |
| Tìm hot key không ra | thiếu LFU hay lệnh khác engine | `OBJECT FREQ`, `HOTKEYS` | dùng cách của đúng engine |
| Di chuyển slot kẹt | cluster không khoẻ, một node chậm | `CLUSTER NODES` hay lệnh trạng thái | dừng, kiểm tra, thử lại có kiểm soát (⚠) |
| Mô-đun làm cluster khác hành vi | mô-đun chưa hỗ trợ đầy đủ ở chế độ cluster | tài liệu mô-đun | kiểm trước khi dùng ở cluster |

## L6 Production — Quyết định khi chạy thật

- **Vận hành theo engine:** runbook riêng cho Redis và Valkey; không dùng chung công cụ di chuyển slot hay tìm hot key.
- **Khoá:** `DELEX` hay `DELIFEQ` hay Lua, kèm TTL, kèm fencing token nếu cần đúng đắn; ghi rõ "khoá hiệu quả" hay "khoá đúng đắn".
- **Hiệu năng:** tự đo, cùng điều kiện; báo khoảng giá trị và giới hạn; không trích số của hãng như sự thật.
- **Cluster:** thử di chuyển slot ở môi trường thử trước; có kế hoạch quay lại.
- **Phiên bản:** ghim tag; theo dõi bản vá (Part 5).

## L7 Architecture — Chọn cách theo quy mô

| Bài toán | Chọn | Cái giá |
|---|---|---|
| Khoá tránh làm trùng việc | `SET NX PX` + `DELEX` (Redis) / `DELIFEQ` (Valkey) / Lua | hết hạn khi chủ còn chạy |
| Khoá bảo vệ tính đúng | fencing token ở nơi lưu, hoặc DB và đồng thuận | thêm thiết kế |
| Mở rộng cluster ít gián đoạn | atomic slot migration (hai bên có) | lệnh khác tên, chưa chạy ở đây |
| Cần tăng thông lượng | `io-threads` sau khi đo | chỉ có lợi khi I/O là nút cổ chai |
| Chọn engine theo hiệu năng | tự đo, cùng điều kiện | tốn công; không dùng số của hãng |
| Một node đủ | không cluster | không mở rộng ngang |
| **Không dùng cluster** | một primary, replica, hoặc dịch vụ quản lý | giới hạn một node |

## Tự kiểm tra

1. `DELEX` thay cái gì trong khoá an toàn, và nó **không** giải quyết điều gì?
2. Lệnh di chuyển slot nguyên tử ở Redis và Valkey tên gì?
3. Vì sao không dùng "nhanh hơn 87%" hay "1,2 triệu QPS" để chọn engine?
4. Muốn biết `io-threads` có lợi không, bạn đo gì?

<details>
<summary>Đáp án ngắn</summary>

1. Thay script Lua so chủ rồi xoá; không giải quyết việc chủ chạy quá hạn (cần fencing token).
2. Redis: `CLUSTER MIGRATION` (8.4); Valkey: `CLUSTER MIGRATESLOTS` (9.0).
3. Đó là số của hãng, khác điều kiện đo, không tái lập ở đây.
4. Thông lượng và độ trễ trên tải thật trước và sau, cùng phần cứng; kèm `io_threads_active` và CPU.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 (Redis 8.10.2, đầu ra thật); Lab 4.2 dựa trên danh sách lệnh của Lab 2.1 (chạy thật, chỉ chứng minh lệnh tồn tại).
**Đã tự mở:** release notes Redis 8.0 (I/O threading mới), 8.4 (`DELEX`, `DIGEST`, `CLUSTER MIGRATION`, hơn 30% cho tải cache, vendor claim), 8.10 (compact hash, `HIMPORT`), redis.io blog AGPLv3 (87% nhanh hơn, vendor claim); blog Valkey 8.0 RC1 (1,2 triệu QPS, vendor claim), Linux Foundation Valkey 9.0 (atomic slot migration, 40%, vendor claim), qua agent.
**Chỉ qua agent (⚠):** `HOTKEYS` (chỉ thấy ở danh sách lệnh); `DELIFEQ` của Valkey (chỉ `COMMAND INFO`).
**Chưa kiểm:** mọi hiệu năng; cluster nhiều node và di chuyển slot thật; hot key; compact hash; hành vi `io-threads` ở cả hai bên.
