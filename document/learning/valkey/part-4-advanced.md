# Part 4 — Advanced (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** xử lý hết RAM (eviction), big key và hot key, hiểu Cluster và hash slot, dùng khoá phân tán và rate limit đúng giới hạn của chúng.
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chọn `maxmemory-policy` cho một workload, tìm và xử lý big key, giải thích `CROSSSLOT`, nêu giới hạn của khoá phân tán và rate limit cửa sổ cố định.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Năm vấn đề ở quy mô lớn

#### Eviction (`maxmemory`)
**Là gì:** khi dùng RAM chạm giới hạn, server từ chối ghi hoặc xoá bớt key theo một chính sách.
**Use case:** Valkey làm cache có giới hạn RAM cố định.

#### Big key và hot key
**Big key:** một key rất lớn (set hàng triệu phần tử). **Hot key:** một key bị truy cập dồn dập.
**Use case để nhận ra:** server khựng khi xoá hay duyệt một key; một node quá tải còn node khác rảnh.

#### Cluster
**Là gì:** chia key ra nhiều node bằng **16.384 hash slot**; mỗi node giữ một dải slot.
**Use case:** dữ liệu hoặc tải vượt một node.

#### Khoá phân tán
**Là gì:** loại trừ lẫn nhau giữa nhiều tiến trình dựa trên Valkey.
**Use case:** chỉ một worker chạy một việc ("hiệu quả"), hoặc bảo vệ tài nguyên dùng chung ("đúng đắn").

#### Rate limit
**Là gì:** giới hạn số yêu cầu mỗi khoảng thời gian cho một khoá (IP, người dùng).
**Use case:** bộ đếm tập trung cho gateway nhiều instance.

## L2 Why — Vì sao các vấn đề này khó

Đơn luồng nghĩa là một thao tác nặng trên một key làm **cả server** chậm. Giới hạn RAM nghĩa là phải quyết định bỏ gì. Mở rộng ngang nghĩa là một thao tác nhiều key có thể nằm ở nhiều node. Khoá phân tán và rate limit nghe đơn giản nhưng có các điều kiện tranh chấp và thời gian mà cài đặt ngây thơ không xử lý.

## L3 How — Cơ chế đáng nhớ

### Eviction

| Chính sách | Hành vi | Hợp khi |
|---|---|---|
| `noeviction` (mặc định) | từ chối ghi khi đầy | Valkey là kho chính, không được mất key |
| `allkeys-lru` | bỏ key ít dùng gần đây nhất (xấp xỉ) | cache chung, truy cập có tính gần đây |
| `allkeys-lfu` | bỏ key ít dùng nhất | truy cập lệch, có key nóng ổn định |
| `volatile-lru` / `volatile-lfu` / `volatile-ttl` / `volatile-random` | chỉ xét key **có TTL** | trộn cache (có TTL) và dữ liệu không được bỏ |
| `allkeys-random` / `volatile-random` | bỏ ngẫu nhiên | truy cập đồng đều |

- `maxmemory 0` là không giới hạn (64 bit). LRU và LFU là **xấp xỉ**: lấy mẫu `maxmemory-samples` key (mặc định 5) rồi bỏ key tệ nhất trong mẫu.
- LFU: bộ đếm có `lfu-log-factor` (mặc định 10) và `lfu-decay-time` (mặc định 1 phút); bộ đếm khởi tạo ở mức 5 (Lab 4.2).
- Chính sách `volatile-*` mà không key nào có TTL thì hành xử như `noeviction`.

### Big key và hot key

- **Big key:** `MEMORY USAGE key` ước tính bộ nhớ (lấy mẫu 5 phần tử cho kiểu lồng nhau); `valkey-cli --bigkeys` và `--memkeys` tìm key lớn bằng `SCAN`. Xoá big key bằng `DEL` có thể chặn server: ở Valkey 9.1.2 (đã quan sát) mặc định `lazyfree-lazy-user-del yes` khiến `DEL` giải phóng nền; đặt `no` thì `DEL` chặn (Lab 4.3). Dùng `UNLINK` để chắc chắn.
- **Hot key:** `valkey-cli --hotkeys` chỉ hoạt động khi chính sách là `*lfu`. Giải pháp: nhân bản key nóng ra nhiều key (`hot:1..N`) rồi đọc ngẫu nhiên; cache cục bộ trước Valkey.
- Chia một cấu trúc lớn thành nhiều key nhỏ theo bucket (ví dụ `members:b1:{0..15}`).

### Cluster và hash slot

- `slot = CRC16(key) mod 16384`; **hash tag** `{...}`: chỉ phần trong ngoặc nhọn đầu tiên không rỗng được băm, để ép nhiều key vào cùng slot (Lab 4.4).
- Lệnh nhiều key chỉ chạy khi mọi key cùng slot, không thì lỗi `CROSSSLOT`.
- Node không làm proxy: trả `-MOVED slot host:port` (cố định, cập nhật bản đồ) hoặc `-ASK` (tạm thời khi đang di chuyển slot).
- Phát hiện lỗi: `PFAIL` khi một node không thấy phản hồi trong `NODE_TIMEOUT`; `FAIL` khi đa số primary báo. Phía thiểu số của một phân vùng không khả dụng.
- Resharding: chuyển slot giữa các node (Valkey 9.0 có chuyển slot nguyên tử `CLUSTER MIGRATESLOTS`).
- So sánh với consistent hashing: [Caching Part 4](../caching/part-4-advanced.md) (slot cố định thay vì vòng băm).

### Khoá phân tán và Redlock

- **Một instance:** `SET key chủ NX PX hạn` + mở khoá bằng Lua (Part 2). Với **primary cùng replica bất đồng bộ**, primary chết trước khi sao chép khoá thì hai client có thể cùng giữ khoá.
- **Redlock:** 5 primary độc lập, lấy khoá trên đa số (3) trong thời gian ngắn hơn hạn; giả định thời gian trôi và độ trễ bị chặn.
- **Phê bình của Martin Kleppmann:** phân biệt khoá **hiệu quả** (tránh làm trùng, chấp nhận lỗi hiếm; một node là đủ) và khoá **đúng đắn** (tránh hỏng dữ liệu; Redlock không an toàn vì dựa vào giả định thời gian, một tiến trình có thể tạm dừng (GC, chậm mạng) quá hạn rồi vẫn ghi). Cách sửa: **fencing token** là số tăng dần mỗi lần cấp khoá, nơi lưu từ chối ghi có token cũ hơn.
- Trang Redlock của redis.io tự khuyên triển khai fencing token và ghi rằng hạn Redis không dùng đồng hồ đơn điệu.
- **Quy tắc:** khoá để tránh làm trùng thì `SET NX PX` + Lua là đủ; khoá để bảo vệ tính đúng thì cần fencing token ở nơi lưu (ví dụ cột phiên bản trong SQL), hoặc dùng hệ thống đồng thuận.

### Rate limit

- **Cửa sổ cố định:** `INCR` + đặt hạn khi giá trị bằng 1. Làm hai lệnh tách rời có **điều kiện tranh chấp**: `INCR` xong mà `EXPIRE` không chạy thì key không có hạn (Lab 4.5). Sửa bằng Lua hoặc `SET ... EX NX` rồi `INCR`.
- **Ranh giới cửa sổ:** cửa sổ cố định cho phép **gần gấp đôi** giới hạn quanh ranh giới (Lab 4.5).
- **Cửa sổ trượt (Sorted Set) và token bucket (Lua)** là hai cách khác, đắt hơn một chút, mượt hơn ở ranh giới; mô tả ở mức ý tưởng ⚠ chưa chạy.

### Client-side caching ⚠
Valkey có cơ chế theo dõi key để client giữ bản cục bộ và được báo huỷ khi đổi (`CLIENT TRACKING`). Chưa đọc kỹ trang chính thức và chưa chạy; Valkey GLIDE C# được ghi nhận là chưa hỗ trợ.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0. Container `lh-vk-ev`, `lh-vk-cl`; đã dọn sau lab.

#### Lab 4.1 — Eviction với `maxmemory 5mb`
Ghi 20.000 key, mỗi giá trị 1000 byte (khoảng 20 MB, gấp bốn giới hạn).
```text
noeviction: errors: 16038 (OOM command not allowed...), DBSIZE 3962, evicted_keys 0, used_memory 5.05M
allkeys-lru: errors: 0, DBSIZE 3952, evicted_keys 16071, used_memory 5.00M
allkeys-lfu: errors: 0, DBSIZE 3951, evicted_keys 16072, used_memory 5.00M
```
**Giải thích:** `noeviction` giữ 3962 key đầu rồi **từ chối** 16.038 lần ghi (ứng dụng nhận lỗi OOM); `lru` và `lfu` luôn chấp nhận ghi và bỏ ~16.000 key cũ. Ghi tuần tự mỗi key một lần nên lab **không** phân biệt chất lượng LRU với LFU (xem mô phỏng ở Caching Lab 1.4).

#### Lab 4.2 — LFU: bộ đếm của key nóng
```text
policy allkeys-lfu: SET hot 1 ; SET cold 1 ; 300 lần GET hot
> OBJECT FREQ hot  → 13
> OBJECT FREQ cold → 5
```
**Giải thích:** key mới bắt đầu ở 5; 300 lần đọc chỉ đẩy lên 13 vì bộ đếm là **logarit** (`lfu-log-factor` 10), không phải số lần đọc.

#### Lab 4.3 — Big key: `MEMORY USAGE`, `DEL` và `UNLINK`
Tạo set 3.000.000 số nguyên.
```text
> MEMORY USAGE big:set → 74569776   (khoảng 71 MiB)       > OBJECT ENCODING big:set → hashtable
> CONFIG GET lazyfree-lazy-user-del → yes   (mặc định của Valkey 9.1.2)
SLOWLOG: DEL big → 25 µs      UNLINK big → 27 µs
CONFIG SET lazyfree-lazy-user-del no ; SLOWLOG: DEL big → 239919 µs  (~240 ms, chặn server)
```
**Giải thích:** một set 3 triệu phần tử chiếm ~71 MiB. Với mặc định hiện tại `DEL` hành xử như `UNLINK` (xoá nền, ~25 µs); tắt `lazyfree-lazy-user-del` thì `DEL` chặn khoảng 240 ms. Cấu hình cũ hay tắt tuỳ nâng cấp; **kiểm cấu hình**, đừng giả định.

#### Lab 4.4 — Hash slot và `CROSSSLOT` (một node bật cluster)
```text
CLUSTER KEYSLOT board:1 → 7925       board:2 → 11926
CLUSTER KEYSLOT {user1}:boards → 8106    {user1}:tasks → 8106   (cùng slot nhờ hash tag)
CLUSTER ADDSLOTSRANGE 0 16383 → OK ; cluster_state:ok ; cluster_slots_assigned:16384
MSET {u}:a 1 {u}:b 2 → OK
MSET board:1 1 board:2 2 → CROSSSLOT Keys in request don't hash to the same slot
```
Ngay sau khi tạo cluster và thêm slot, lệnh ghi trả `CLUSTERDOWN The cluster is down` cho đến khi `cluster_state` thành `ok` (vài giây).
**Giải thích:** hai key khác slot không dùng chung một lệnh nhiều key; hash tag đưa chúng về cùng slot. Lab chỉ có **một node**, nên không thấy `MOVED`, `ASK` hay chuyển slot; ⚠ phần đó chưa chạy.

#### Lab 4.5 — Rate limit cửa sổ cố định từ .NET (5 yêu cầu mỗi giây)
```text
8 yêu cầu liên tiếp: cho cho cho cho cho chặn chặn chặn
sau 1,1 giây: cho
-- INCR rồi EXPIRE tách riêng, tiến trình chết giữa hai lệnh
TTL của rl:broken = không có (-1)   <- key sống mãi, bộ đếm không reset
-- 5 yêu cầu cuối cửa sổ 1 và 5 yêu cầu ngay đầu cửa sổ 2
trong khoảng ~1,05 giây có 10 yêu cầu được cho (giới hạn 5/giây)
```
**Giải thích:** Lua (`INCR` rồi `PEXPIRE` khi bằng 1) chặn đúng; biến thể hai lệnh để lại key không hạn nếu chết giữa chừng; cửa sổ cố định cho qua 10 yêu cầu quanh ranh giới.

## L5 Troubleshooting — Quy mô và tranh chấp

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Ứng dụng nhận lỗi OOM khi ghi | `noeviction` và RAM đầy | `INFO memory`, `CONFIG GET maxmemory-policy` | tăng RAM, đặt TTL, đổi sang chính sách `allkeys-*` nếu là cache |
| Key biến mất không rõ | eviction | `evicted_keys` trong `INFO stats` | tăng RAM, kiểm tập nóng, tăng `maxmemory-samples` |
| Server khựng khi xoá một key | big key với `DEL` chặn | `SLOWLOG GET`, `MEMORY USAGE` | `UNLINK`, bật `lazyfree-lazy-user-del`, chia key |
| Một node cluster quá tải | hot key hoặc key lệch slot | `--hotkeys` (cần LFU), phân bố slot | nhân bản key nóng, cache cục bộ, hash tag có chủ đích |
| `CROSSSLOT` | lệnh nhiều key khác slot | `CLUSTER KEYSLOT` từng key | hash tag cho nhóm key liên quan, hoặc tách lệnh |
| Hai worker cùng chạy một việc | khoá hết hạn khi chủ còn chạy (Lab 2.6) | so thời gian chạy với TTL khoá | gia hạn khoá, fencing token, thiết kế việc idempotent |
| Rate limit không bao giờ reset | `INCR` không có `EXPIRE` (Lab 4.5) | `TTL` của key bộ đếm là `-1` | Lua `INCR` + `PEXPIRE`, dọn key không hạn |
| Bùng nổ gấp đôi quanh ranh giới | cửa sổ cố định | so số yêu cầu qua ranh giới | cửa sổ trượt hoặc token bucket |

## L6 Production — Quyết định khi chạy thật

- **`maxmemory` và chính sách:** đặt `maxmemory` thấp hơn RAM máy; bộ đệm replication, AOF rewrite và fork cần dư địa (không tính vào `maxmemory`). Cache chọn `allkeys-lru` hoặc `allkeys-lfu`; kho trộn chọn `volatile-*` và **bảo đảm** key cần bỏ có TTL.
- **Giới hạn kích thước key:** quy ước tối đa phần tử mỗi cấu trúc; kiểm `--bigkeys` ngoài giờ cao điểm hoặc trên replica (quét tốn CPU).
- **Khoá phân tán:** ghi rõ khoá để "hiệu quả" hay "đúng đắn"; loại sau cần fencing token ở nơi lưu.
- **Cluster:** thiết kế key và hash tag từ đầu; thử `CROSSSLOT` ở môi trường thử; theo dõi phân bố slot và tải từng node.
- **Rate limit:** dùng Lua; quyết định chịu được bùng nổ ranh giới hay không; chọn hành vi khi Valkey lỗi (mở hay đóng) và ghi log.

## L7 Architecture — Chọn cách

| Bài toán | Chọn | Vì sao |
|---|---|---|
| Cache có RAM cố định | `allkeys-lru` hoặc `allkeys-lfu` | luôn nhận ghi, bỏ key ít giá trị |
| Dữ liệu không được bỏ | `noeviction` và theo dõi RAM | thà lỗi ghi còn hơn mất key |
| Dữ liệu vượt một node | Cluster, thiết kế key theo hash tag | mở rộng ngang, đổi lại hạn chế lệnh nhiều key |
| Tránh làm trùng việc | `SET NX PX` + Lua | đủ khi lỗi hiếm chấp nhận được |
| Bảo vệ tính đúng của dữ liệu | fencing token ở kho, hoặc DB/đồng thuận | Redlock dựa vào giả định thời gian |
| Rate limit đơn giản | Lua cửa sổ cố định | rẻ, chấp nhận bùng nổ ranh giới |
| Rate limit mượt | cửa sổ trượt hoặc token bucket | tốn thêm bộ nhớ hoặc script |
| Một instance, tải thấp | **không dùng** Valkey: bộ đếm trong bộ nhớ (như gateway hiện tại) | chưa cần thêm thành phần |

## Tự kiểm tra

1. `noeviction` và `allkeys-lru` khác nhau gì khi RAM đầy, theo Lab 4.1?
2. Vì sao 300 lần đọc chỉ đẩy `OBJECT FREQ` lên 13?
3. Khi nào khoá `SET NX PX` đủ, khi nào cần fencing token?
4. Vì sao `MSET board:1 ... board:2 ...` lỗi trong Cluster còn `MSET {u}:a ... {u}:b ...` thì không?

<details>
<summary>Đáp án ngắn</summary>

1. `noeviction` từ chối ghi (OOM); `allkeys-lru` nhận ghi và bỏ key cũ.
2. Bộ đếm LFU là logarit, không phải đếm tuyến tính.
3. Đủ khi chỉ tránh làm trùng; cần fencing token khi bảo vệ tính đúng của dữ liệu.
4. Khác slot gây `CROSSSLOT`; hash tag `{u}` đưa hai key về cùng slot.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 đến 4.5 (đầu ra thật). Lab 4.4 chỉ một node.
**Đã tự mở:** redis.io `/docs/latest/develop/clients/patterns/distributed-locks/` (Redlock và khuyến nghị fencing); martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html (khoá hiệu quả và đúng đắn, fencing token); redis.io `/commands/incr/` (mẫu rate limiter và điều kiện tranh chấp).
**Chỉ qua agent (⚠):** valkey.io `/topics/lru-cache/` (chính sách, mẫu, LFU), `/topics/cluster-spec/` (16.384 slot, `MOVED`, `ASK`, `PFAIL`/`FAIL`, `CLUSTER MIGRATESLOTS` từ 9.0), `/topics/cli/` (`--bigkeys`, `--hotkeys`), `/commands/memory-usage/`, `/commands/unlink/`.
**Chưa kiểm:** cluster nhiều node, `MOVED`/`ASK`, failover; cửa sổ trượt và token bucket; `CLIENT TRACKING`; bản phản hồi của antirez cho Kleppmann (chưa mở).
