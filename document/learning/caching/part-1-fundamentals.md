# Part 1 — Fundamentals & Mental Models (Caching)

> [!NOTE]
> **Mục tiêu:** hiểu cache là gì, vì sao nó đổi độ cũ lấy tốc độ, sáu cơ chế đọc/ghi, các chính sách eviction và invalidation, và biết lúc nào không nên cache.
> **Cần biết trước:** [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** vẽ được luồng của từng cơ chế, giải thích vì sao thứ tự ghi–xoá quan trọng, chọn được chính sách eviction hợp một kiểu truy cập.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Cache và các khái niệm nền

#### Cache
**Là gì:** nơi lưu tạm **bản sao** của dữ liệu hoặc kết quả tính toán ở chỗ **gần và rẻ hơn** nguồn gốc (RAM thay vì đĩa, cùng tiến trình thay vì qua mạng, CDN thay vì máy chủ gốc).
**Ví dụ đời thường:** kệ sách cạnh bàn so với thư viện thành phố: lần đầu bạn đi thư viện, mượn về để cạnh bàn; lần sau lấy ngay. Cái giá: sách trên kệ có thể là bản cũ so với thư viện.
**Use case:**
- Danh sách Board của một người được đọc hàng nghìn lần mỗi phút nhưng hiếm khi đổi → giữ kết quả trong bộ nhớ vài chục giây.
- Một phép tính đắt (báo cáo thống kê) cho cùng đầu vào → tính một lần, dùng lại.

#### Hit, miss và hit ratio
**Là gì:** **hit** là có bản sao trong cache; **miss** là không có, phải hỏi nguồn. **Hit ratio** = hit ÷ (hit + miss).
**Use case:** theo dõi hit ratio để biết cache có đáng giữ không; nhưng đọc kèm độ cũ và tải nguồn (Part 4).

#### Độ cũ (staleness)
**Là gì:** khoảng chênh giữa bản sao và nguồn hiện tại. Mọi cache đều đổi một phần độ đúng lấy tốc độ.
**Use case:** chấp nhận giá hiển thị lệch vài giây thì cache; số dư tài khoản cần đúng tuyệt đối thì không cache theo cách này.

#### Các tầng cache
Càng gần người dùng càng rẻ nhưng càng khó kiểm soát độ cũ.

| Tầng | Nằm ở đâu | Ví dụ |
|---|---|---|
| Trình duyệt | máy người dùng | `Cache-Control`, `ETag` |
| CDN / proxy | giữa người dùng và máy chủ | Cloudflare, CloudFront |
| Trong tiến trình | RAM của ứng dụng | `IMemoryCache` |
| Phân tán | dịch vụ chung qua mạng | Valkey, Redis, Memcached |
| Tầng dữ liệu | cạnh hoặc trong DB | buffer pool, DAX |

#### TTL, eviction, invalidation
- **TTL** (time to live): thời gian sống; hết hạn thì coi như miss.
- **Eviction:** khi cache đầy, bỏ bớt mục theo một chính sách (LRU, LFU...).
- **Invalidation:** bỏ hoặc cập nhật bản sao khi nguồn đổi.

## L2 Why — Vì sao cần cache

**Tình huống:** `board-service` trả danh sách Board từ SQL Server. Mỗi lần đọc mất vài chục ms và tải DB tăng đúng bằng số lần đọc. Phần lớn lần đọc cho cùng kết quả vì dữ liệu hiếm khi đổi.

Bảng dưới là **phép tính minh hoạ** (cache 1 ms, nguồn 50 ms, 10.000 yêu cầu/giây; độ trễ trung bình = 1 + miss × 50):

| Hit ratio | Độ trễ trung bình | Tải lên nguồn |
|---|---|---|
| 90% | 6 ms | 1.000 yêu cầu/giây |
| 95% | 3,5 ms | 500 yêu cầu/giây |
| 99% | 1,5 ms | 100 yêu cầu/giây |
| 99,9% | 1,05 ms | 10 yêu cầu/giây |

**Bài học:** nhìn vào **miss ratio** mới thấy lợi ích: từ 90% lên 99% giảm tải nguồn 10 lần, dù hit ratio chỉ tăng 9 điểm.
**Cái giá:** độ cũ, một thành phần mới phải vận hành, và các lỗi mới (stampede, dữ liệu riêng rò ra). Mục L7 nói khi nào không đáng.

## L3 How — Cơ chế đáng nhớ

### Sáu cơ chế đọc và ghi

Câu hỏi phân biệt: **ai nạp và ai ghi cache**, theo thứ tự nào. Tên gọi giữa các tài liệu không hoàn toàn thống nhất; hãy đọc "ai làm gì, thứ tự nào".

#### a. Cache-aside (lazy loading)
**Ai nạp:** ứng dụng, chỉ khi có người hỏi.
```
ĐỌC: app ─► cache ─hit──► trả
              └─miss─► app ─► nguồn ─► app ghi vào cache ─► trả
GHI: app ─► nguồn, rồi app XOÁ key trong cache
```
**Use case:**
- Truy vấn "danh sách Board của tôi": đọc nhiều, chịu được hơi cũ → mặc định hợp lý.
- Cache không có sẵn read-through → ứng dụng tự làm (Decorator, Part 2).

**Ưu:** cache chỉ chứa thứ được hỏi; node cache hỏng không làm sập ứng dụng (chỉ chậm hơn).
**Nhược:** miss tốn thêm lượt gọi; có thể cũ; không đảm bảo nhất quán.
**Thứ tự ghi–xoá:** ghi nguồn **trước**, xoá cache **sau** (Lab 1.5).

#### b. Read-through
**Ai nạp:** chính lớp cache; ứng dụng chỉ nói chuyện với cache.
**Use case:** thư viện cache có sẵn cơ chế nạp khi miss (ví dụ DAX trước DynamoDB) để code ứng dụng đơn giản.
**Ưu:** một giao diện đồng nhất. **Nhược:** cache thành phụ thuộc về độ sẵn sàng; cache lỗi thì mọi truy cập lỗi.

#### c. Write-through
**Ai ghi:** ghi cache và nguồn **cùng một thao tác**; chỉ trả thành công khi cả hai xong.
**Use case:** đọc nhiều và cần "đọc ngay sau ghi" thấy giá trị mới (cấu hình hiển thị cho người dùng vừa sửa).
**Ưu:** luôn tươi; ít đọc nguồn. **Nhược:** ghi chậm hơn (hai chuyến); dữ liệu ít ai đọc cũng vào cache, cache lớn và đắt.
**Thực tế:** thường đi cùng lazy loading (để miss được nạp) và TTL (để dọn dữ liệu chết).

#### d. Write-behind (write-back)
**Ai ghi nguồn:** cache, **sau** khi đã ghi vào cache và trả thành công cho ứng dụng (bất đồng bộ, thường gom lô).
**Use case:** số đo, bộ đếm xấp xỉ, dữ liệu mất được hoặc tái tạo được, ghi rất nhiều.
**Ưu:** ghi rất nhanh; gom nhiều lần ghi thành ít lần. **Nhược:** **mất dữ liệu nếu cache chết trước khi ghi nguồn**; nguồn trễ so với cache. Trái nguyên tắc "đừng chỉ giữ dữ liệu quý trong cache" ⚠ nên không dùng cho Board hay Task.

#### e. Write-around
**Ai ghi cache:** không ai khi ghi: ghi **thẳng nguồn**, cache chỉ được nạp khi đọc và miss.
**Use case:** dữ liệu ghi nhiều nhưng ít khi đọc lại (nhật ký hoạt động) → không làm bẩn cache.
**Nhược:** đọc ngay sau ghi là miss; key cũ nếu còn phải bị xoá hoặc hết hạn ⚠.

#### f. Refresh-ahead (làm mới trước hạn)
**Ai làm mới:** cache hoặc ứng dụng chủ động nạp lại mục sắp hết hạn **trước khi** có người hỏi.
**Use case:** key nóng mà việc tính lại tốn kém (bảng xếp hạng) → người dùng không bao giờ gặp miss ở key đó.
**Cơ chế có nguồn:** soft TTL (quá hạn mềm thì làm mới nền, nguồn lỗi thì vẫn trả bản cũ) và làm mới sớm theo xác suất (XFetch, Part 4). Tên "refresh-ahead" ⚠ chưa có nguồn định nghĩa riêng.

| Cơ chế | Ai nạp / ghi cache | Đọc | Ghi | Rủi ro mất dữ liệu |
|---|---|---|---|---|
| Cache-aside | ứng dụng | miss → nguồn → ghi cache | ghi nguồn, xoá key | không |
| Read-through | lớp cache | cache tự nạp | tuỳ sản phẩm | không |
| Write-through | cả hai cùng lúc | thường hit | ghi cả hai | thấp |
| Write-behind | cache, nguồn sau | hit | ghi cache trước | **có** |
| Write-around | không ghi cache | miss rồi nạp | ghi nguồn | không |
| Refresh-ahead | làm mới trước hạn | gần như luôn hit | – | không |

**Kết hợp thường gặp:** cache-aside + TTL + xoá khi ghi; write-through + lazy loading + TTL.

### Chính sách eviction

Khi cache đầy phải bỏ bớt. Chọn chính sách theo **kiểu truy cập**.

#### LRU (ít dùng gần đây nhất)
**Là gì:** bỏ mục lâu chưa được dùng nhất.
**Use case:** nội dung "đọc gần đây thì sắp đọc lại" (trang đang hot). Là mặc định hợp lý (Valkey gợi ý `allkeys-lru` khi chưa chắc).
**Nhược:** một lần quét tuần tự lớn đẩy sạch dữ liệu nóng ra (Lab 1.4).

#### LFU (ít dùng nhất)
**Là gì:** bỏ mục được dùng ít lần nhất.
**Use case:** tài nguyên phổ biến ổn định (danh mục top); chống quét tuần tự.
**Nhược:** mục từng nóng rồi nguội chiếm chỗ lâu nếu bộ đếm không suy giảm. Valkey dùng bộ đếm xấp xỉ có suy giảm theo thời gian (`lfu-decay-time`).

#### FIFO và Random
**FIFO** bỏ mục vào sớm nhất, không quan tâm đang nóng hay không; **Random** bỏ ngẫu nhiên.
**Use case:** gần như không tốn metadata; hợp khi truy cập đồng đều (khi đó chính sách nào cũng như nhau).

#### TTL-based và noeviction
`volatile-ttl` bỏ mục có TTL còn ngắn nhất trước; `noeviction` từ chối ghi khi đầy (hợp khi cache là kho chính). Các chính sách `volatile-*` chỉ xét mục có TTL; không có mục đủ điều kiện thì hành xử như `noeviction`.

**Valkey xấp xỉ LRU:** không giữ danh sách LRU chính xác (tốn bộ nhớ và con trỏ); lấy mẫu vài key, bỏ key cũ nhất trong mẫu; `maxmemory-samples` mặc định thường là 5, tăng lên gần với LRU thật, đổi lại tốn CPU.

### Invalidation cơ bản
- **TTL:** đơn giản; độ cũ tối đa bằng TTL. TTL quá ngắn thì miss liên tục; quá dài thì dữ liệu cũ.
- **Xoá khi ghi:** ghi nguồn, rồi xoá key; lần đọc sau tự nạp lại.
- **Cập nhật khi ghi (write-through):** ghi luôn bản mới vào cache.
Các chiến lược nâng cao (version trong key, tag, sự kiện, lease) ở Part 4.

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0 (.NET 10.0.13). Chương trình lab nằm ngoài repo. Mô phỏng là mô hình tự viết; số đo thời gian chỉ tương đối.

#### Lab 1.1 — Hit, miss và TTL với `IMemoryCache`
```csharp
var cache = new MemoryCache(new MemoryCacheOptions { TrackStatistics = true });
async Task<string> Get(string k) => (await cache.GetOrCreateAsync(k, async e =>
{
    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(300);
    var n = Interlocked.Increment(ref loads); await Task.Delay(10); return $"value-{n}";
}))!;
```
**Đầu ra thật:**
```text
lần 1: value-1  (miss, nạp từ nguồn)
lần 2: value-1  (hit)
sau 400 ms (TTL 300 ms): value-2  (hết hạn, nạp lại)
thống kê: hits=1 misses=2 entries=1 loads=2
```
**Giải thích:** lần 1 miss nạp từ nguồn; lần 2 hit; hết TTL thì miss lại. `GetCurrentStatistics()` đếm hit/miss để tính hit ratio.

#### Lab 1.2 — Hết hạn tuyệt đối và hết hạn trượt
Hai mục cùng 300 ms; mục `sliding` được đọc mỗi 200 ms.
**Đầu ra thật:**
```text
t=200 ms  sliding=còn  absolute=còn
t=400 ms  sliding=còn  absolute=mất
t=600 ms  sliding=còn  absolute=mất
t=800 ms  sliding=còn  absolute=mất
```
**Giải thích:** sliding được gia hạn mỗi lần đọc nên **có thể không bao giờ hết hạn** nếu luôn có người đọc; absolute có hạn cứng. Thực hành tốt: kết hợp cả hai để có trần.

#### Lab 1.3 — Negative caching
100 lần hỏi một id **không tồn tại**.
**Đầu ra thật:**
```text
negative caching = tắt: chạm DB 100 lần
negative caching = bật: chạm DB 1 lần
```
**Giải thích:** cache cả kết quả "không có" (kèm TTL ngắn) để khỏi hỏi lại nguồn; thiếu nó, id không tồn tại (kể cả do bị dò) đổ hết xuống DB.

#### Lab 1.4 — Chính sách eviction (mô phỏng)
10.000 key, cache 500 mục, 200.000 lượt truy cập, phân phối Zipf s = 1,0 (một số ít key rất nóng). Workload thứ hai chèn **một lần quét tuần tự 5.000 key hiếm** vào giữa.
**Đầu ra thật** (hit ratio chỉ tính truy cập Zipf; cột phải là 2.000 truy cập ngay sau điểm giữa/sau lần quét):

| Chính sách | Zipf thuần | 2.000 truy cập sau điểm giữa | Zipf + quét | 2.000 truy cập sau lần quét |
|---|---|---|---|---|
| LRU | 58,8% | 59,1% | 58,7% | **53,8%** |
| LFU | 66,2% | 66,2% | 66,2% | **66,2%** |
| FIFO | 54,1% | 54,1% | 54,0% | 50,7% |
| Random | 54,1% | 54,9% | 54,0% | 47,1% |

**Giải thích:** với truy cập lệch (Zipf), LFU giữ đúng các key nóng nên cao nhất; sau lần quét tuần tự, LRU, FIFO và Random đều bị "ô nhiễm" tạm thời (LRU tụt 59,1% xuống 53,8%), còn LFU giữ nguyên. **Giới hạn của mô phỏng:** LFU ở đây đếm lý tưởng khi mục còn trong cache; LFU thật (Valkey) dùng bộ đếm xấp xỉ có suy giảm.

#### Lab 1.5 — Thứ tự ghi–xoá và xen kẽ (mô phỏng có kiểm soát)
Ba kịch bản với cổng đồng bộ để ép đúng thứ tự xen kẽ.
**Đầu ra thật:**
```text
A (xoá cache trước, ghi DB sau):              DB=v2, cache=v1  -> CŨ, và sẽ cũ tới khi hết TTL
B (ghi DB trước, xoá cache sau, không xen kẽ): DB=v2, cache=v2  -> đúng
C (thứ tự đúng nhưng người đọc ghi cache muộn): DB=v2, cache=v1  -> CŨ (hiếm, cần TTL hoặc lease)
```
**Giải thích:** A: giữa lúc xoá và ghi, một người đọc thấy miss, đọc nguồn **còn bản cũ** rồi nạp lại cache, và bản cũ ở lại. B là thứ tự đúng. C cho thấy ngay cả thứ tự đúng vẫn có một cửa sổ hiếm: người đọc đã đọc nguồn trước khi ghi, nhưng ghi cache sau khi đã bị xoá. Hai phòng ngừa: **TTL làm lưới an toàn** và **lease** (Part 4).

## L7 Architecture — Chọn giải pháp, và khi không nên cache

### Bản đồ giải pháp

Cache là một **họ kỹ thuật ở nhiều tầng**, không phải một công cụ. Redis/Valkey chỉ là **một dòng**.

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá |
|---|---|---|---|
| Cache của trình duyệt | `Cache-Control`, `ETag` | nội dung tĩnh có hash trong URL; kiểm tra lại bằng 304 | không kiểm soát được máy người dùng |
| CDN / edge | Cloudflare, CloudFront | nội dung công khai, người dùng phân tán | cache key và purge cần thiết kế |
| Trong tiến trình | `IMemoryCache` | một instance, dữ liệu nhỏ ít đổi | mỗi instance một bản, mất khi restart |
| Hai tầng L1 + L2 | `HybridCache` | nhanh như bộ nhớ mà vẫn chia sẻ | thêm phụ thuộc; xoá không lan sang L1 instance khác |
| Phân tán kiểu cấu trúc dữ liệu | Valkey, Redis | nhiều instance cần chung cache hoặc bộ đếm | thêm thành phần vận hành; vấn đề giấy phép Redis |
| Phân tán key-value đơn giản | Memcached | mô hình đơn giản nhất, node nhiều lõi | không replication hay failover sẵn |
| Cache tầng dữ liệu | DAX, buffer pool của DB | đọc rất nhanh, hot key | DAX không hợp đọc strongly consistent hay ghi nhiều |
| **Không cache** | tối ưu truy vấn, index, read replica, read model (CQRS) | tải thấp, dữ liệu đổi liên tục, cần nhất quán mạnh | không giảm được tải đọc khi DB đã tối đa |

**Cách chọn tầng:** bắt đầu ở tầng **gần người dùng nhất** mà dữ liệu cho phép; chỉ thêm cache phân tán khi nhiều instance cần chung. Mỗi tầng thêm là một nơi có thể cũ.

### Khi KHÔNG nên cache
- Dữ liệu nhạy cảm hoặc liên quan bảo mật, nhất là ở cache dùng chung.
- Tập dữ liệu tĩnh nhỏ: nạp sẵn lúc khởi động và không cho hết hạn.
- Hầu hết yêu cầu không hit: kiểm cache và nạp tốn hơn lợi.
- Dữ liệu cần đúng tuyệt đối (số dư, quyền truy cập).
- Chưa đo: truy vấn chậm có thể sửa bằng index; cache chỉ che lỗi thiết kế.

### Phạm vi và những gì công cụ cụ thể KHÔNG đại diện
Khái niệm cache **không phải** Redis. [Redis / Valkey](../redis.md) là một lựa chọn trong bản đồ trên; HTTP cache, cache trong tiến trình và cache của DB thường là bước đầu rẻ hơn và không cần thêm hạ tầng.

## Tự kiểm tra

1. Vì sao hit ratio từ 90% lên 99% lại giảm tải nguồn 10 lần?
2. Cache-aside: vì sao ghi nguồn trước, xoá cache sau? Còn cửa sổ nào hiếm?
3. Khi nào LFU hơn LRU, và khi nào ngược lại?
4. Nêu ba trường hợp không nên cache.

<details>
<summary>Đáp án ngắn</summary>

1. Tải nguồn tỷ lệ với **miss ratio**: từ 10% xuống 1% là giảm 10 lần.
2. Xoá trước thì một người đọc xen vào nạp lại bản cũ (Lab 1.5 A). Cửa sổ hiếm: người đọc đã đọc nguồn trước khi ghi nhưng ghi cache sau khi bị xoá (Lab 1.5 C); dùng TTL hoặc lease.
3. LFU hơn khi có key nóng ổn định và có quét tuần tự; LRU hơn khi độ phổ biến thay đổi nhanh theo thời gian.
4. Dữ liệu nhạy cảm ở cache dùng chung; tập tĩnh nhỏ; hầu hết miss; dữ liệu cần đúng tuyệt đối.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** Lab 1.1 đến 1.5 (đầu ra thật, môi trường ở Part 0); bảng hit ratio ở L2 là phép tính của chính mình.
**Đã tự mở:** Azure Cache-Aside pattern, Azure Caching guidance, AWS whitepaper "Database caching strategies using Redis" (cơ chế cache-aside, write-through, thứ tự ghi–xoá); AWS ElastiCache caching strategies và trang so sánh engine (Memcached so với Valkey); Valkey `lru-cache` (các chính sách eviction); MDN HTTP caching; Facebook "Scaling Memcache" (lease, đọc từ PDF).
**Chỉ qua agent (⚠):** mô tả LFU xấp xỉ, `maxmemory-samples` và `lfu-decay-time` (valkey.io/topics/lru-cache); read-through và DAX (AWS Builders' Library); cơ chế write-behind, write-around, refresh-ahead (không có nguồn định nghĩa; mô tả là kiến thức chung).
**Chưa kiểm:** hành vi LFU/LRU thật của Valkey (chỉ mô phỏng); số liệu của eviction phụ thuộc phân phối Zipf tự chọn.
