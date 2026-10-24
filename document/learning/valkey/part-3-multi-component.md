# Part 3 — Multi-component (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** hiểu khi nào dữ liệu trong Valkey mất (persistence), cách replication hoạt động và giới hạn của nó, khi nào pub/sub mất tin, dùng Streams có ack, và cho nhiều instance dùng chung (`HybridCache`).
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** chọn cấu hình persistence theo mức mất dữ liệu chấp nhận được, giải thích vì sao `WAIT` không đảm bảo bền, dựng được consumer group có ack.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Dữ liệu, bản sao và thông điệp

#### Persistence (RDB và AOF)
**RDB:** ảnh chụp tại một thời điểm (`dump.rdb`). **AOF:** nhật ký mọi lệnh ghi, phát lại khi khởi động.
**Use case:** muốn dữ liệu sống sót qua restart (phiên, bộ đếm, hàng đợi).

#### Replication
Một **primary** nhận ghi, một hoặc nhiều **replica** sao chép bất đồng bộ; replica mặc định chỉ đọc.
**Use case:** chịu lỗi (có bản dự phòng), tách tải đọc.

#### Pub/Sub
Gửi tin tới các client **đang** lắng nghe kênh.
**Use case:** thông báo tạm thời, mất cũng được (làm mới giao diện).

#### Streams và consumer group
Nhật ký chỉ ghi thêm; nhóm consumer chia việc và **ghi nhận (ack)** từng mục.
**Use case:** hàng đợi sự kiện cần xử lý ít nhất một lần.

## L2 Why — Vì sao phải hiểu các giới hạn này

Mặc định Valkey lưu trong RAM; restart, sự cố hay triển khai nhầm có thể làm mất dữ liệu. Replication cho bản dự phòng nhưng **bất đồng bộ**: primary chết trước khi bản ghi tới replica thì bản ghi mất khi promote replica. Pub/sub không giữ tin. Hiểu từng cơ chế mất dữ liệu ở đâu là điều kiện để quyết định thứ gì được phép nằm trong Valkey.

## L3 How — Cơ chế đáng nhớ

### Persistence

| Chế độ | Cách hoạt động | Mất tối đa | Cái giá |
|---|---|---|---|
| Tắt | chỉ RAM | tất cả khi restart | không có |
| RDB | chụp theo điểm (`save 3600 1 300 100 60 10000` là mặc định) | từ lần chụp cuối | chụp tốn CPU/RAM (fork) |
| AOF `everysec` | ghi nhật ký, fsync mỗi giây | khoảng 1 giây | file lớn hơn, cần rewrite |
| AOF `always` | fsync mỗi lệnh | gần như không | rất chậm |
| AOF `no` | để hệ điều hành quyết định | khoảng 30 giây trên Linux | không kiểm soát |

- **Dừng êm** (SIGTERM, `docker restart`) với RDB mặc định thì server **lưu trước khi thoát**; **bị giết** (SIGKILL, `docker kill`) thì mất những gì ghi sau lần chụp cuối (Lab 3.1).
- **AOF rewrite** chạy nền: tiến trình con ghi file base mới, tiến trình chính ghi file incremental mới; nhiều file nằm trong thư mục `appendonlydir` cùng một manifest.
- Khuyến nghị tài liệu: dùng **cả hai** nếu cần độ bền cao; khi khởi động nếu có AOF thì dùng AOF vì đầy đủ hơn.

### Replication

- Bất đồng bộ; replica báo offset. Khi nối lại, `PSYNC` gửi replication ID và offset để chỉ nhận **phần thiếu**; nếu backlog không đủ thì **đồng bộ lại toàn bộ**.
- `repl-diskless-sync` mặc định `yes` và đợi `repl-diskless-sync-delay` (mặc định 5 giây) trước khi bắt đầu: vì thế replica mới **chưa đồng bộ ngay** (Lab 3.2).
- **Nguy hiểm:** primary không có persistence mà khởi động lại rỗng, replica sẽ đồng bộ theo bản rỗng và **mất sạch**.
- `WAIT n timeout` chờ n replica nhận các ghi trước đó; trả số replica đã ack. Nó giảm rủi ro nhưng **không phải nhất quán mạnh**: ghi vẫn có thể mất khi failover.

### Sentinel (tự động failover) ⚠ chưa chạy
Nhiều Sentinel theo dõi primary. `SDOWN`: một Sentinel không thấy phản hồi hợp lệ trong `down-after-milliseconds`. `ODOWN`: đủ **quorum** Sentinel cùng báo. Quorum chỉ để **phát hiện**; failover cần đa số Sentinel cho phép. Chọn replica để promote theo `replica-priority`, rồi offset, rồi run ID nhỏ hơn. Triển khai tối thiểu 3 Sentinel ở các miền lỗi độc lập. `min-replicas-to-write` và `min-replicas-max-lag` giới hạn cửa sổ mất ghi.

### Pub/Sub
Gửi **một lần nếu có người nghe**; không lưu, không ack. Mất tin nếu người nghe đang ngắt kết nối hoặc không xử lý kịp. Cần mức bảo đảm cao hơn thì dùng Streams.

### Streams và consumer group

#### Đọc theo nhóm
`XREADGROUP ... >` trả các mục **chưa giao** cho bất kỳ consumer nào trong nhóm; mỗi mục chỉ giao cho **một** consumer. Mục đã giao nhưng chưa `XACK` nằm trong **danh sách chờ (PEL)** kèm consumer, thời gian rảnh, số lần giao.

#### Xử lý lại
`XAUTOCLAIM` (hoặc `XCLAIM`) cho consumer khác **giành lại** mục treo quá một thời gian, tăng bộ đếm số lần giao. Số lần giao cao nghĩa là có thể là **thông điệp độc**: chuyển sang stream "dead-letter". Vì xử lý có thể lặp, **consumer phải idempotent** (khử trùng theo `eventId`).

### Nhiều instance ứng dụng dùng chung
Valkey làm L2 của `HybridCache`: L1 trong tiến trình, L2 trong Valkey; `RemoveAsync` xoá L2 và L1 của instance gọi, **không xoá L1 của instance khác**; gom request chỉ trong một instance ([Caching Part 3](../caching/part-3-multi-component.md)). Lab 3.5 kiểm lại với Valkey thật.

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0. Container `lh-vk-*` trên mạng `lh-vk-net`, volume `lh-vk-*`; đã dọn sau lab.

#### Lab 3.1 — Persistence: tắt, RDB mặc định, AOF
```text
A (--save "" --appendonly no): SET a 1 ; docker restart → GET a → (nil) ; DBSIZE 0
B (RDB mặc định, volume):   CONFIG GET save → 3600 1 300 100 60 10000
   SET b 2 ; docker restart  → GET b → 2        (dừng êm: lưu khi thoát)
   SET c 3 ; docker kill     → GET b → 2, GET c → (nil)   (bị giết: mất ghi sau lần chụp cuối)
C (--appendonly yes --appendfsync everysec): SET d 4 ; đợi 2 giây ; docker kill → GET d → 4
   appendonlydir: appendonly.aof.1.base.rdb, appendonly.aof.1.incr.aof, appendonly.aof.manifest
```
**Giải thích:** tắt persistence thì restart là mất hết; RDB mất những gì ghi sau lần chụp khi bị giết; AOF `everysec` giữ được ghi cũ hơn 1 giây. Phép thử AOF **không** kiểm cửa sổ 1 giây (chờ 2 giây trước khi kill).

#### Lab 3.2 — Replication: sync trễ, replica chỉ đọc, `WAIT`
Hai container `lh-vk-p` (primary) và `lh-vk-r` (`--replicaof lh-vk-p 6379`).
```text
(ngay sau khi khởi động) master_link_status:down
(sau 9 giây)             master_link_status:up
> SET board:1 v1 (primary) ; GET board:1 (replica) → v1
> SET x 1 (replica)        → READONLY You can't write against a read only replica.
> WAIT 1 500 (primary)     → 1
INFO replication (primary): role:master connected_slaves:1 slave0:...state=online,offset=85,lag=0
INFO replication (replica): role:slave master_link_status:up slave_repl_offset:85 slave_read_only:1
```
Ở lần chạy đầu (chỉ chờ 2 giây) `INFO` của primary báo `state=wait_bgsave,offset=0`, replica `master_link_status:down`, `GET board:1` trả rỗng và `WAIT 1 500` trả `0`.
**Giải thích:** replica cần thời gian đồng bộ ban đầu; đọc từ replica quá sớm cho kết quả rỗng; `WAIT` trả đúng số replica đã nhận, nên **phải so với số mong đợi**.

#### Lab 3.3 — Primary chết, promote thủ công
```text
docker stop lh-vk-p → replica: role:slave master_link_status:down ; GET board:1 → v1
> REPLICAOF NO ONE → OK ; SET x 1 → OK ; role:master
```
**Giải thích:** khi primary chết, replica **vẫn là replica** và vẫn đọc được dữ liệu đã sao chép; chỉ ghi được sau khi bị promote (Sentinel làm bước này tự động; chưa chạy).

#### Lab 3.4 — Pub/Sub và Streams
```text
> PUBLISH news "không ai nghe" → 0                       (không có người nghe: tin mất)
SUBSCRIBE news (nền) ; PUBLISH news hello → 1 ; người nghe nhận message hello

> XGROUP CREATE orders g1 $ MKSTREAM → OK
> XADD orders * id o1 ... o2 ... o3 (3 mục)
> XREADGROUP GROUP g1 c1 COUNT 2 STREAMS orders > → o1, o2
> XPENDING orders g1 → 2 mục chờ, consumer c1
> XACK orders g1 <id o1> → 1 ; XPENDING → còn 1 (o2)
(đợi 2 giây) XAUTOCLAIM orders g1 c2 1000 0-0 → c2 nhận o2
> XPENDING orders g1 - + 10 → o2 thuộc c2, số lần giao 2
```
**Giải thích:** pub/sub không có người nghe thì `PUBLISH` trả 0 và tin biến mất; Streams giữ lại, ack từng mục, và mục treo được consumer khác giành lại (số lần giao tăng lên 2).

#### Lab 3.5 — `HybridCache` hai instance dùng Valkey làm L2 (.NET)
Thay Lab 3.1 của Caching bằng Valkey thật (L2 là Valkey, `LocalCacheExpiration` 1 giây).
```text
A đọc: v1  (chạm DB 1)
ngay sau A: Valkey có 0 key                          (ghi L2 chạy nền)
sau 200 ms: Valkey có 1 key; TTL = 30 giây
B đọc: v1  (chạm DB 1: B lấy từ L2 trong Valkey)
A xoá khoá; Valkey còn 0 key
B đọc ngay: v1  (chạm DB 1)  <- L1 của B còn bản cũ
B đọc sau 1200 ms: v2  (chạm DB 2)
40 yêu cầu đồng thời trên 2 instance: chạm DB 2 lần
Valkey còn các key: hy:hot, hy:t:1, hy:__MSFT_HCT__boards
A đọc sau RemoveByTag: v2; B đọc ngay: v1
```
**Giải thích:** khớp với L2 giả ở Caching Lab 3.1: ghi L2 là nền, xoá không lan sang L1 của B. Hai instance cùng miss thì **mỗi instance chạy một lần** (tổng 2), vì gom request chỉ trong một tiến trình. Tag `boards` được lưu thành một key riêng `__MSFT_HCT__boards` (đánh dấu vô hiệu, không quét xoá từng key); sau `RemoveByTagAsync` trên A, B vẫn trả `v1` từ L1. Key `hy:hot` là bản ghi nền của lab stampede đến muộn sau lần dọn dữ liệu.

## L5 Troubleshooting — Mất dữ liệu và đồng bộ

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Restart xong dữ liệu mất sạch | không bật persistence, hoặc không gắn volume | `CONFIG GET save`, `appendonly`; xem volume | bật RDB/AOF và gắn volume dữ liệu |
| Mất ghi gần nhất sau khi bị kill | RDB giữa hai lần chụp; AOF `everysec` | so thời điểm ghi với lần chụp/fsync | AOF `everysec` hoặc `always` nếu cần; chấp nhận cửa sổ |
| Replica trả rỗng sau khi tạo | đồng bộ ban đầu chưa xong (diskless delay) | `INFO replication`: `master_link_status` | đợi `up`; đọc có kiểm tra trạng thái |
| Replica mất sạch sau khi primary restart | primary không persistence, restart rỗng | primary có RDB/AOF không | bật persistence ở primary; không tự restart primary rỗng |
| Ghi bị mất sau failover | bất đồng bộ; primary chết trước khi sao chép | so offset trước và sau | `WAIT`, `min-replicas-to-write`; chấp nhận cửa sổ |
| Tin pub/sub biến mất | người nghe ngắt kết nối hoặc chưa đăng ký | `PUBLISH` trả 0 | dùng Streams nếu không được phép mất |
| Một mục stream xử lý hoài không xong | thông điệp độc, consumer lỗi | `XPENDING`: số lần giao cao | đặt ngưỡng, chuyển sang dead-letter, consumer idempotent |
| Instance khác vẫn thấy bản cũ sau khi xoá | L1 của `HybridCache` không bị xoá (Lab 3.5) | so `LocalCacheExpiration` | rút hạn L1 hoặc phát sự kiện xoá |

## L7 Architecture — Chọn mức bền và kiểu thông điệp

| Nhu cầu | Chọn | Cái giá |
|---|---|---|
| Cache, mất cũng dựng lại được | không persistence, hoặc RDB | khởi động lạnh sau restart |
| Phiên, bộ đếm chấp nhận mất vài giây | AOF `everysec` (+ RDB) | file lớn hơn, rewrite |
| Dữ liệu không được mất | **không** để Valkey là nơi duy nhất; dùng DB, hoặc MemoryDB | chi phí, độ phức tạp |
| Thông báo mất cũng được | pub/sub | không lưu, không ack |
| Hàng đợi có ack, xử lý ít nhất một lần | Streams + consumer group | phải idempotent; async replication vẫn có cửa sổ mất |
| Hàng đợi quan trọng, bền, lớn | broker chuyên dụng (SQS/Kafka) | thêm hạ tầng; xem case Slack ở Part 7 |

## Tự kiểm tra

1. Vì sao `docker restart` giữ được dữ liệu RDB còn `docker kill` thì không?
2. `WAIT 1 500` trả `0` nghĩa là gì?
3. Vì sao pub/sub không hợp cho tác vụ không được mất?
4. Hai instance `HybridCache` cùng miss một key: DB bị gọi mấy lần?

<details>
<summary>Đáp án ngắn</summary>

1. Dừng êm thì server lưu RDB trước khi thoát; bị giết thì không kịp (Lab 3.1).
2. Chưa có replica nào ack các ghi trước đó trong 500 ms (Lab 3.2: replica chưa đồng bộ); không đảm bảo bền.
3. Không lưu, không ack; tin mất khi không có người nghe (Lab 3.4).
4. Mỗi instance một lần, tổng 2 (Lab 3.5).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 đến 3.5 (Valkey 9.1.2; đầu ra thật). Lab 3.5 thay lab L2 giả của Caching bằng Valkey thật.
**Đã tự mở:** redis.io (khoá phân tán, `WAIT` nhắc tới trong tài liệu Redis); trang Slack job queue (case, Part 7).
**Chỉ qua agent (⚠):** valkey.io `/topics/persistence/`, `/replication/`, `/sentinel/`, `/streams-intro/`, `/pubsub/`, `/commands/wait/` (mặc định `save`, `appendfsync`, PSYNC, diskless delay 5 giây, bước failover của Sentinel, tuyên bố "gửi một lần nếu có").
**Chưa kiểm:** Sentinel (không chạy), failover tự động, hành vi đồng bộ khi backlog thiếu, `appendfsync always`, cửa sổ mất 1 giây của AOF `everysec`.
