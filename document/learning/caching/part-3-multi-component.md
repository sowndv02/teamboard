# Part 3 — Multi-component (Caching)

> [!NOTE]
> **Mục tiêu:** hiểu vì sao một cache trong tiến trình không đủ khi có nhiều instance, dùng cache phân tán qua `IDistributedCache` và `HybridCache`, và biết xoá một key ở nơi này **không** tự xoá ở mọi nơi.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** giải thích đường đi L1 → L2 → nguồn, dự đoán khi nào instance còn trả bản cũ sau khi xoá.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Cache khi có nhiều thành phần

#### Cache phân tán
**Là gì:** một dịch vụ cache chạy riêng, nhiều instance của ứng dụng cùng truy cập qua mạng.
**Use case:** ba instance `board-service` cùng cần một bản Board đã đọc; instance mới khởi động không phải bắt đầu với cache rỗng.

#### `IDistributedCache`
**Là gì:** interface .NET chung cho cache phân tán (giá trị là `byte[]` hoặc chuỗi, có hết hạn). Cài đặt thật (Redis/Valkey, SQL Server...) cắm vào bằng DI.
**Use case:** đổi backend cache mà không sửa code gọi. Nhược: tự lo serialization, tự chống stampede.

#### `HybridCache`
**Là gì:** thư viện .NET kết hợp **L1 (bộ nhớ cục bộ)** và **L2 (`IDistributedCache`)** trong một API `GetOrCreateAsync`, có gom request đồng thời.
**Use case:** muốn tốc độ cache trong tiến trình mà vẫn chia sẻ giữa instance, không tự viết lớp nối hai tầng.

#### Memcached và Valkey/Redis
Hai họ dịch vụ cache phân tán. Memcached: key-value đơn giản, đa luồng, không có replication hay persistence sẵn. Valkey/Redis: thêm cấu trúc dữ liệu, replication, failover, persistence (xem bảng ở L7).

## L2 Why — Vì sao cần hơn một tầng

**Vấn đề 1: mỗi instance một cache riêng.** Ba instance, `IMemoryCache` ở mỗi nơi: Board bị đọc từ nguồn tối đa ba lần, và sau khi sửa, ba bản có thể cũ vào ba thời điểm khác nhau.
**Vấn đề 2: khởi động lạnh.** Instance mới hoặc vừa restart bắt đầu rỗng và đẩy tải lên nguồn.
**Vấn đề 3: mỗi lần đi qua mạng đến cache vẫn tốn** (cỡ ms) so với đọc RAM tại chỗ.

`HybridCache` giải quyết cả ba: L2 chung cho mọi instance, L1 nhanh trong tiến trình. Cái giá: **hai nơi có thể cũ**, và xoá không tự lan hết (L3).

## L3 How — Cơ chế đáng nhớ

#### Đường đọc của `HybridCache`
```
GetOrCreateAsync(key):
  1. L1 (RAM của instance) có? ─► trả
  2. L2 (cache chung) có?     ─► nạp vào L1, trả
  3. cả hai miss              ─► chạy factory (đọc nguồn), ghi L2 và L1, trả
```
**Quan sát khi lab (Lab 3.1):** việc ghi L2 chạy **nền**: ngay sau khi trả kết quả, L2 có thể còn trống. Instance thứ hai đọc **quá sớm** có thể miss L2 và tự đọc nguồn.

#### Gom request trong một instance
Nhiều yêu cầu đồng thời cho cùng key trong **một instance** chỉ chạy factory một lần (Lab 3.2). Bảo vệ này **không** phối hợp giữa các instance: N instance cùng miss có thể cùng chạm nguồn N lần.

#### Xoá không lan hết
`RemoveAsync(key)` xoá ở L1 của instance gọi và ở L2. **L1 của các instance khác không bị xoá**, chúng tiếp tục trả bản cũ đến khi `LocalCacheExpiration` hết (Lab 3.1). Muốn xoá nhanh trên mọi instance cần một kênh phát sự kiện (L3 bên dưới) hoặc chấp nhận độ cũ bằng thời hạn L1.

#### Đồng bộ invalidation giữa các instance
| Cách | Cơ chế | Độ cũ tối đa | Cái giá |
|---|---|---|---|
| Chờ L1 hết hạn | đặt `LocalCacheExpiration` ngắn | bằng hạn L1 | nhiều lượt đọc L2 hơn |
| Phát sự kiện "key X đổi" | message bus, mỗi instance tự xoá L1 | vài trăm ms đến vài giây ⚠ | thêm hạ tầng, sự kiện có thể mất |
| Xuất sự kiện từ DB (CDC) hoặc outbox | sự kiện ghi cùng giao dịch với dữ liệu | thấp | phức tạp hơn; xem Part 4 |

#### Tag
`HybridCache` hỗ trợ **tag** để xoá theo nhóm. Tag hoạt động **logic** (đánh dấu thời điểm vô hiệu), không quét xoá từng key ⚠.

#### Serialization
Giá trị đi qua L2 phải được tuần tự hoá: chi phí CPU và kích thước; đổi cấu trúc kiểu dữ liệu là đổi định dạng trong cache (đặt tiền tố phiên bản vào key, Part 2).

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0. **L2 trong lab này là một cài đặt `IDistributedCache` tự viết chạy trong cùng tiến trình** (hai instance `HybridCache` dùng chung một đối tượng). Nó mô phỏng cache chung đúng giao diện, nhưng **không phải Valkey**, không có mạng, không có độ trễ thật.

#### Lab 3.1 — Hai instance `HybridCache` dùng chung một L2
Nguồn trả `v1`, sau đó đổi sang `v2`. Instance A đọc, B đọc, A xoá khoá, B đọc lại.
**Đầu ra thật:**
```text
A đọc: v1  (chạm DB 1 lần)
ngay sau A: L2 có 0 khoá  (ghi L2 chạy nền, chưa chắc xong)
sau 100 ms: L2 có 1 khoá
B đọc: v1  (chạm DB 1 lần: không tăng nghĩa là B lấy từ L2 dùng chung)
sau khi A xoá khoá: L2 còn khoá? không
B đọc ngay: v1  (chạm DB 1 lần)  <- B vẫn giữ bản trong bộ nhớ cục bộ (L1)
B đọc sau 1200 ms (LocalCacheExpiration=1 giây): v2  (chạm DB 2 lần)
```
**Giải thích:** B lấy được bản của A qua L2 (DB vẫn 1 lần), nhưng sau khi A xoá khoá L2, B **vẫn trả `v1` từ L1** cho đến khi hạn L1 (1 giây) qua. Lần chạy đầu tiên của lab giả định B đọc từ L2 ngay sau A và sai vì ghi L2 là nền; thêm 100 ms trước khi B đọc mới đúng.

#### Lab 3.2 — Gom request đồng thời
50 yêu cầu đồng thời cho cùng một key, nguồn mất 200 ms.
**Đầu ra thật:**
```text
IMemoryCache.GetOrCreateAsync trần : chạm nguồn 50 lần, 218 ms
gom request tự viết (Lazy<Task>)   : chạm nguồn  1 lần, 216 ms
HybridCache.GetOrCreateAsync        : chạm nguồn  1 lần, 238 ms
```
**Giải thích:** `IMemoryCache.GetOrCreateAsync` **không** gom: 50 lần chạm nguồn. `Lazy<Task>` tự viết và `HybridCache` đều còn 1 lần, chênh vài chục ms là nhiễu đo. Phạm vi: chỉ trong **một tiến trình**.

#### Lab 3.3 — Valkey thật ⚠ chưa chạy
Lab này cần Docker Desktop đang chạy và kéo image Valkey (tên và tag cần kiểm trên Docker Hub trước khi kéo). Hướng làm, **chưa chạy, không có đầu ra**:
```bash
docker run -d --name lh-valkey -p 127.0.0.1:16379:6379 valkey/valkey:<tag-đã-kiểm>
docker exec lh-valkey valkey-cli SET board:1 v1 EX 30
docker exec lh-valkey valkey-cli TTL board:1
docker rm -f lh-valkey
```
Trong ứng dụng: `AddStackExchangeRedisCache(o => o.Configuration = "127.0.0.1:16379")` rồi `AddHybridCache()` ⚠ (tên gói và phương thức theo tài liệu Microsoft, chưa chạy). Khi chạy thật cần ghi đầu ra thật và đối chiếu với Lab 3.1.

## L5 Troubleshooting — Khi nhiều instance không khớp

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Sửa xong, một số instance trả cũ vài giây | L1 của instance khác chưa hết hạn (Lab 3.1) | xem `LocalCacheExpiration`, so hai instance | rút hạn L1, hoặc phát sự kiện xoá L1 |
| Instance thứ hai vẫn chạm nguồn dù instance đầu đã đọc | ghi L2 chạy nền chưa xong, hoặc key khác nhau giữa hai instance | xem L2 có key không, so chuỗi key | kiểm tra key sinh ra giống nhau; chấp nhận trễ ngắn |
| N instance cùng chạm nguồn lúc hết hạn | gom request chỉ trong một instance | đếm số lần chạm nguồn theo instance | jitter, làm mới sớm, lease (Part 4) |
| Lỗi giải mã sau khi triển khai phiên bản mới | đổi cấu trúc dữ liệu, L2 còn bản cũ | xem lỗi serialization và key | đặt phiên bản vào key, hoặc xoá namespace cũ |
| Mọi yêu cầu chậm khi L2 chết | không có lối thoát khi L2 lỗi | xem timeout và log kết nối | timeout ngắn, đọc thẳng nguồn khi L2 lỗi (Part 5) |
| Hai ứng dụng khác nhau ghi đè key của nhau | dùng chung L2 mà không có tiền tố | liệt kê key | tiền tố theo ứng dụng và môi trường |

## L7 Architecture — Chọn tầng cache

| Lựa chọn | Khi chọn | Cái giá |
|---|---|---|
| `IMemoryCache` | một instance, dữ liệu nhỏ, chịu được mất khi restart | mỗi instance một bản; khởi động lạnh |
| `IDistributedCache` + Valkey | nhiều instance cần chung dữ liệu hoặc bộ đếm | mỗi lần đọc qua mạng; tự lo stampede và serialization |
| `HybridCache` (L1 + L2) | muốn nhanh như RAM và chia sẻ được | hai nơi có thể cũ; xoá không lan hết (Lab 3.1) |
| Memcached | key-value thuần, ưu tiên đơn giản, không cần cấu trúc dữ liệu | không có replication, failover sẵn |
| Valkey/Redis | cần cấu trúc dữ liệu, replication, failover, persistence | vận hành phức tạp hơn; [ghi chú công cụ](../redis.md) |
| Không dùng cache phân tán | một instance, hoặc tải đọc thấp | không chia sẻ được giữa instance |

**Cách chọn:** hỏi (1) có nhiều instance cần thấy cùng dữ liệu không, (2) độ cũ tối đa chấp nhận được là bao lâu, (3) mất cache có làm sập nguồn không. Một instance và chịu được độ cũ: `IMemoryCache` đủ. Nhiều instance, chịu được độ cũ vài giây: `HybridCache`. Cần xoá đồng bộ nhanh: thêm kênh sự kiện hoặc dùng L2 trực tiếp (bỏ L1).

**Phạm vi:** Valkey chỉ là một lựa chọn trong họ này. Cùng một bài toán có thể giải bằng CDN, HTTP cache hoặc read model; xem [Bản đồ giải pháp](part-1-fundamentals.md#bản-đồ-giải-pháp).

## Tự kiểm tra

1. Sau `RemoveAsync` ở instance A, vì sao instance B vẫn có thể trả bản cũ?
2. `HybridCache` chống stampede ở phạm vi nào, và không chống ở đâu?
3. Vì sao Lab 3.1 phải chờ 100 ms trước khi B đọc?
4. Khi nào `IMemoryCache` là đủ?

<details>
<summary>Đáp án ngắn</summary>

1. `RemoveAsync` xoá L2 và L1 của chính A; L1 của B còn đến khi `LocalCacheExpiration` hết.
2. Trong một instance (cùng tiến trình); không phối hợp giữa các instance.
3. Ghi L2 chạy nền; B đọc quá sớm sẽ miss L2.
4. Một instance, dữ liệu nhỏ, chịu được độ cũ và mất khi restart.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 và 3.2 (đầu ra thật; L2 là bản tự viết trong tiến trình, không phải Valkey).
**Đã tự mở:** AWS ElastiCache "Choosing an engine" (so sánh Memcached và Valkey: đa luồng, replication, persistence).
**Chỉ qua agent (⚠):** tài liệu Microsoft Learn về `IDistributedCache` và `HybridCache` (L1/L2, tag logic, đăng ký `AddHybridCache`); gói `Microsoft.Extensions.Caching.StackExchangeRedis`.
**Chưa kiểm:** Lab 3.3 (Valkey thật) chưa chạy; độ trễ mạng thật, hành vi khi L2 chết, và đồng bộ L1 giữa nhiều tiến trình thật chưa quan sát.
