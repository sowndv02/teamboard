# Part 1 — Fundamentals & Mental Models (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** hiểu Valkey/Redis là gì, vì sao có hai tên, mô hình thực thi đơn luồng, và chọn đúng kiểu dữ liệu cho từng bài toán.
> **Cần biết trước:** [Caching Part 1](../caching/part-1-fundamentals.md), [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** chọn kiểu dữ liệu cho 5 bài toán cho trước, giải thích vì sao một lệnh chậm chặn mọi client, nêu khác biệt giấy phép Redis/Valkey.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Valkey và Redis

#### Valkey / Redis
**Là gì:** server key-value giữ dữ liệu **trong RAM**; mỗi key trỏ tới một **cấu trúc dữ liệu** (không chỉ chuỗi), kèm TTL, thao tác nguyên tử, replication và persistence tuỳ chọn.
**Ví dụ đời thường:** bảng ghi chú dán ở văn phòng chung: ai cũng đọc nhanh, nhưng nếu mất điện thì phải xem có sao lưu không.
**Use case:**
- Nhiều instance `board-service` cần chung một cache hoặc bộ đếm.
- Phiên đăng nhập, khoá chống chạy trùng, bảng xếp hạng, hàng đợi nhẹ.

#### Quan hệ Redis, Valkey
Redis ra đời trước. Ngày 20/03/2024 Redis đổi giấy phép từ BSD-3 sang RSALv2 hoặc SSPLv1 (từ bản 7.4); ngày 28/03/2024 Linux Foundation công bố **Valkey**, fork từ Redis 7.2.4, giữ BSD-3, được AWS, Google Cloud, Oracle, Ericsson, Snap hậu thuẫn. Ngày 01/05/2025 Redis thêm AGPLv3 làm lựa chọn từ Redis 8. Hai bên đã tách nhau ở tính năng sau bản 7.2.4; **kiểm từng lệnh với đúng phiên bản bạn dùng**.
**Chính sách dự án:** Redis server `banned` (T3), Valkey `watch` (T1); AGPL bị cấm mặc định nên Redis 8 cũng cần ADR nếu muốn dùng (xem `.agents/rules/dependency-policy.md`).

#### Kiểu dữ liệu
String, Hash, List, Set, Sorted Set, Stream, Bitmap, HyperLogLog, Geo: mỗi loại có thẻ riêng ở L3.

#### TTL (expire)
Gắn thời gian sống cho key; hết hạn thì key biến mất. Dùng cho cache, phiên, khoá.

## L2 Why — Vì sao dùng một server riêng cho dữ liệu nhanh

Cache trong tiến trình (`IMemoryCache`) mỗi instance một bản riêng và mất khi restart ([Caching Part 3](../caching/part-3-multi-component.md)). Valkey là **kho dùng chung qua mạng**: mọi instance thấy cùng một bản. Nhờ có cấu trúc dữ liệu và thao tác nguyên tử (`INCR`, `SADD`, `ZADD`) nên nó còn làm được việc ngoài cache: bộ đếm tập trung, bảng xếp hạng, khoá, hàng đợi, nhật ký sự kiện.

**Cái giá:** thêm một chuyến mạng và một thành phần phải vận hành, giám sát, vá lỗi; dữ liệu trong RAM hữu hạn; không phải nguồn sự thật bền (Part 3).

## L3 How — Cơ chế đáng nhớ

### Mô hình thực thi

#### Đơn luồng cho việc chạy lệnh
**Là gì:** mọi lệnh chạy **tuần tự** trên một luồng; I/O mạng có thể dùng luồng phụ (`io-threads`, mặc định tắt, nên bật khi có từ 3 lõi trở lên; nội dung nâng cấp 8.0 chưa kiểm).
**Hệ quả:** mỗi lệnh nguyên tử, không cần khoá; nhưng **một lệnh chậm chặn mọi client** (`KEYS`, xoá big key, Lua dài; Lab 2.3 và 4.3).

#### Hết hạn key
Hai cách chạy cùng lúc: **lazy** (key hết hạn bị xoá khi có người truy cập) và **active** (chu kỳ nền lấy mẫu 20 key, lặp lại nếu hơn 25% mẫu đã hết hạn). Vì vậy key hết hạn có thể còn chiếm RAM một lúc nhưng không bao giờ được đọc ra.

#### Giao thức RESP
Văn bản có tiền tố kiểu (`+` chuỗi đơn giản, `-` lỗi, `:` số, `$` chuỗi lớn, `*` mảng); client gửi lệnh là mảng chuỗi. `HELLO 3` chuyển sang RESP3.

### Các kiểu dữ liệu: mỗi loại một thẻ

#### String
**Là gì:** dãy byte (văn bản, số, JSON, blob); có `INCR`, `SET ... EX`, `SET ... NX`.
**Use case:** cache một đối tượng đã tuần tự hoá; bộ đếm (`INCR views:board:1`); khoá (`SET lock:board:1 worker-A NX EX 30`).

#### Hash
**Là gì:** bản ghi gồm nhiều field-value trong một key (`HSET`, `HGET`, `HGETALL`).
**Use case:** hồ sơ Board (`name`, `owner`, `columns`) đọc/ghi từng field mà không phải tuần tự hoá cả đối tượng. `IDistributedCache` lưu mỗi mục dưới dạng Hash (Lab 2.5).

#### List
**Là gì:** danh sách theo thứ tự chèn; chèn/lấy ở hai đầu O(1) (`LPUSH`, `RPUSH`, `LPOP`, `LRANGE`, `LTRIM`, `BLPOP`).
**Use case:** "N Board xem gần nhất" của người dùng (đẩy vào đầu, cắt còn N bằng `LTRIM`); hàng đợi việc nền đơn giản.

#### Set
**Là gì:** tập phần tử duy nhất, không thứ tự; kiểm tra thành viên O(1); có giao, hợp, hiệu (`SADD`, `SISMEMBER`, `SINTER`).
**Use case:** thành viên của một Board; "người dùng chung của hai Board" bằng `SINTER`; gắn thẻ.

#### Sorted Set
**Là gì:** các phần tử duy nhất, mỗi phần tử một **score**, luôn sắp theo score; hầu hết thao tác O(log N) (`ZADD`, `ZRANGE`, `ZRANK`, `ZRANGEBYSCORE`).
**Use case:** bảng xếp hạng; hàng đợi ưu tiên theo thời điểm; cửa sổ trượt cho rate limit (Part 4).

#### Stream
**Là gì:** nhật ký chỉ ghi thêm, mỗi mục có ID `thời-gian-số-thứ-tự`; có consumer group (`XADD`, `XREADGROUP`, `XACK`).
**Use case:** nhật ký hoạt động Board, hàng đợi sự kiện cần ghi nhận đã xử lý (Part 3).

#### Bitmap
**Là gì:** thao tác bit trên một String (`SETBIT`, `BITCOUNT`).
**Use case:** "ngày nào người dùng hoạt động" với một bit mỗi ngày; thống kê đếm theo ngày rất gọn.

#### HyperLogLog
**Là gì:** ước lượng số phần tử **khác nhau** với bộ nhớ nhỏ cố định (`PFADD`, `PFCOUNT`); chấp nhận sai số nhỏ.
**Use case:** đếm lượt người xem duy nhất của một trang mà không lưu danh sách.

#### Geo
**Là gì:** chỉ mục toạ độ (`GEOADD`, `GEODIST`, tìm theo bán kính).
**Use case:** tìm văn phòng gần nhất; khoảng cách giữa hai điểm.

| Kiểu | Chọn khi | Thao tác tiêu biểu | Đừng dùng khi |
|---|---|---|---|
| String | giá trị đơn, bộ đếm, khoá | `SET`, `INCR`, `SET NX` | cần sửa từng phần của bản ghi lớn |
| Hash | bản ghi nhiều field | `HSET`, `HGETALL` | field rất nhiều và rất lớn (big key) |
| List | hàng đợi, N mục gần nhất | `LPUSH`, `LTRIM` | cần truy cập giữa danh sách (O(N)) |
| Set | thành viên, giao/hợp | `SADD`, `SINTER` | cần thứ tự |
| Sorted Set | xếp hạng, thứ tự theo score | `ZADD`, `ZRANGE` | không cần thứ tự (dùng Set) |
| Stream | nhật ký, sự kiện có ack | `XADD`, `XREADGROUP` | thông báo "bắn rồi quên" (pub/sub) |

## L4 Hands-on — Lab Part 1

Môi trường: Valkey 9.1.2 trong container `lh-valkey` (`127.0.0.1:16379`), lệnh qua `docker exec lh-valkey valkey-cli`. Đầu ra là đầu ra thật của lần chạy (rút gọn dòng trống).

#### Lab 1.1 — Chạy từng kiểu dữ liệu
```text
> SET board:1:name Sprint            → OK
> SET views:board:1 0 ; INCR ; INCRBY views:board:1 10   → 1, 11
> HSET board:1 name Sprint owner u1 columns 3   → 3
> HGET board:1 owner                → u1
> LPUSH recent:u1 b1 b2 b3 ; LTRIM recent:u1 0 1 ; LRANGE recent:u1 0 -1   → b3, b2
> SADD members:b1 u1 u2 u3 ; SADD members:b2 u2 u3 u4 ; SINTER members:b1 members:b2   → u2, u3
> ZADD leaderboard 120 alice 95 bob 150 carol ; ZRANGE leaderboard 0 -1 WITHSCORES   → bob 95, alice 120, carol 150
> ZREVRANK leaderboard alice        → 1
> XADD activity * board b1 action created   → 1792767178182-0
> SETBIT active:2026-10-23 5 1 ; SETBIT ... 9 1 ; BITCOUNT active:2026-10-23   → 2
> PFADD visitors u1 u2 u3 u1 ; PFCOUNT visitors   → 3
> GEOADD offices 105.8342 21.0278 hanoi 106.6297 10.8231 hcm ; GEODIST offices hanoi hcm km   → 1138.2050
```
**Giải thích:** `LPUSH` ba lần rồi `LTRIM 0 1` giữ 2 mục gần nhất; `SINTER` ra phần tử chung; `PFADD` thêm `u1` hai lần nhưng `PFCOUNT` vẫn là 3 (đếm phần tử khác nhau); `GEODIST` ra khoảng 1138 km giữa Hà Nội và TP.HCM.

#### Lab 1.2 — Kiểu và mã hoá nội bộ
```text
TYPE board:1:name → string   OBJECT ENCODING → embstr
TYPE board:1      → hash     OBJECT ENCODING → listpack
TYPE recent:u1    → list     OBJECT ENCODING → listpack
TYPE members:b1   → set      OBJECT ENCODING → listpack
TYPE leaderboard  → zset     OBJECT ENCODING → listpack
TYPE activity     → stream   OBJECT ENCODING → stream
```
**Giải thích:** cấu trúc nhỏ dùng mã hoá gọn (`listpack`) để tiết kiệm RAM; khi lớn lên server tự đổi sang mã hoá khác (Lab 4.3: set 3.000.000 phần tử là `hashtable`).

#### Lab 1.3 — TTL
```text
> SET session:abc token EX 2   → OK
> TTL session:abc              → 2
(đợi 3 giây)
> GET session:abc              → (nil)      > EXISTS session:abc → 0
> SET k v EX 100 ; TTL k       → 100        > PERSIST k ; TTL k → -1
> TTL nokey                    → -2
```
**Giải thích:** hết hạn thì `GET` trả nil; `TTL = -1` nghĩa là key tồn tại **không có** hạn, `-2` nghĩa là key không tồn tại.

## L7 Architecture — Valkey so với các lựa chọn

| Lựa chọn | Khi chọn | Cái giá |
|---|---|---|
| Valkey | cần cấu trúc dữ liệu, replication, persistence, giấy phép BSD-3 | thêm thành phần vận hành; chi tiết khác Redis |
| Redis (7.4+, 8) | cần tính năng riêng của Redis (ví dụ module đóng gói trong Redis 8) | giấy phép RSALv2/SSPLv1/AGPLv3; bị cấm theo chính sách dự án |
| Memcached | key-value thuần, đa luồng, không cần cấu trúc dữ liệu | không replication, persistence hay pub/sub sẵn |
| `IMemoryCache` / `HybridCache` | một instance, hoặc cần L1 nhanh trước L2 | không chia sẻ (L1); xoá không lan ([Caching Part 3](../caching/part-3-multi-component.md)) |
| Cơ sở dữ liệu | cần bền, truy vấn phức tạp, là nguồn sự thật | chậm hơn RAM cho đọc nóng |
| **Không dùng** | tải thấp, một instance, chưa đo thấy cần | không chia sẻ được giữa instance |

**Cách chọn:** hỏi (1) nhiều instance có cần chung dữ liệu không, (2) có cần cấu trúc dữ liệu hay thao tác nguyên tử không, (3) mất dữ liệu có chấp nhận được không (nếu không, nó không là nơi lưu duy nhất). Mặc định của dự án: **không dùng khi chưa cần** (`dependency-policy.md`).

## Tự kiểm tra

1. Chọn kiểu dữ liệu cho: (a) số lượt xem Board, (b) 10 Board xem gần nhất, (c) bảng xếp hạng, (d) số người xem duy nhất, (e) nhật ký hoạt động.
2. Vì sao một lệnh chậm làm khựng mọi client?
3. `TTL` trả `-1` và `-2` khác nhau thế nào?
4. Vì sao Redis 8 chưa chắc dùng được ở dự án này?

<details>
<summary>Đáp án ngắn</summary>

1. (a) String với `INCR`; (b) List với `LPUSH` + `LTRIM`; (c) Sorted Set; (d) HyperLogLog; (e) Stream.
2. Lệnh chạy tuần tự trên một luồng; lệnh chậm giữ luồng nên lệnh khác phải chờ.
3. `-1`: key tồn tại nhưng không có hạn; `-2`: key không tồn tại.
4. Redis 8 có AGPLv3 làm lựa chọn; AGPL bị cấm mặc định, cần người dùng duyệt và ADR.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 đến 1.3 (Valkey 9.1.2, đầu ra thật).
**Đã tự mở:** redis.io (đổi giấy phép 20/03/2024; AGPLv3 01/05/2025), Linux Foundation (Valkey 28/03/2024); Docker Hub và valkey.io/download (phiên bản và tag).
**Chỉ qua agent (⚠):** valkey.io `/topics/data-types/` và các trang từng kiểu (complexity, ví dụ dùng); `/topics/latency/` (đơn luồng, hết hạn lazy và active); `/topics/protocol/` (RESP); `valkey.conf` về `io-threads`.
**Chưa kiểm:** thay đổi I/O thread ở Valkey 8.0 (không có số đo); khác biệt tính năng Redis 8 so với Valkey.
