# Part 4 — Advanced (Caching)

> [!NOTE]
> **Mục tiêu:** chống stampede, chọn chiến lược invalidation hợp bài toán, hiểu consistent hashing và hash slot, và đo hit ratio đúng cách.
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** chọn và giải thích được một cách chống stampede, một chiến lược invalidation, và vì sao `hash mod N` không hợp khi thêm node.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Bốn vấn đề của cache ở quy mô lớn

#### Stampede (thundering herd)
**Là gì:** một key nóng hết hạn, nhiều yêu cầu cùng miss và **cùng** đổ về nguồn để tính lại.
**Use case:** trang chủ có Board nổi bật được đọc 200 lần/giây; lúc hết hạn 50 yêu cầu cùng gọi DB.

#### Invalidation nâng cao
**Là gì:** các cách giữ cache khớp nguồn tốt hơn "chờ TTL": xoá khi ghi, key có phiên bản, tag, sự kiện thay đổi.
**Use case:** sửa tên Board phải thấy ngay ở mọi trang, không đợi 5 phút.

#### Phân mảnh cache (sharding)
**Là gì:** chia key ra nhiều node cache. Cần quy tắc "key này nằm ở node nào" vẫn đúng khi thêm bớt node.
**Use case:** một node cache không chứa hết dữ liệu hoặc không chịu hết tải.

#### Đo hit ratio
**Là gì:** theo dõi tỉ lệ hit để biết cache có đáng giữ, nhưng phải đọc kèm tải lên nguồn và độ cũ.

## L2 Why — Vì sao các vấn đề này làm cache hại hơn lợi

Cache giảm tải nguồn **trung bình**, nhưng tải dồn vào khoảnh khắc miss: nguồn được cấu hình cho tải sau cache có thể không chịu nổi vài giây không cache. Một cache khởi động lạnh hoặc một đợt hết hạn đồng loạt biến một hệ thống nhanh thành sập (Part 7 có các sự cố thật). Sai ở invalidation thì không sập mà **âm thầm trả sai**, khó phát hiện hơn.

## L3 How — Cơ chế đáng nhớ

### Chống stampede

#### a. Gom request (coalescing / single-flight)
**Là gì:** các yêu cầu đồng thời cho cùng key chờ **một** lần tính, rồi cùng nhận kết quả.
**Use case:** `HybridCache.GetOrCreateAsync`, output cache có khoá, hoặc `Lazy<Task>` tự viết (Lab 3.2: 50 yêu cầu → 1 lần chạm nguồn).
**Giới hạn:** chỉ trong **một tiến trình**; N instance vẫn có thể có N lần tính.

#### b. Lease (token trước khi nạp)
**Là gì:** khi miss, cache cấp cho **một** client một lease (token) để nạp; client khác nhận "chờ" hoặc bản cũ.
**Use case:** nhiều instance cùng miss một key; Facebook dùng lease (token hết hạn khoảng 10 giây) và giảm tải DB từ 17K xuống 1,3K truy vấn/giây trong một đo của họ. Lease còn chặn nạp bản cũ vào cache sau khi key bị xoá (Lab 1.5 kịch bản C).
**Cái giá:** cần cache hỗ trợ lease; client phải xử lý "chờ".

#### c. Làm mới sớm theo xác suất (XFetch)
**Là gì:** mỗi yêu cầu khi đọc có xác suất nhỏ **tính lại trước hạn**, tăng dần khi gần hạn, nên thường chỉ **một** yêu cầu làm mới trước khi hết hạn. Công thức bài gốc (Vattani và cộng sự): tính lại nếu `Time() − δ·β·log(rand()) ≥ expiry`, `δ` là thời gian tính lại, `β` thường 1.
**Use case:** key nóng, tính lại đắt, không có lease sẵn. Không cần phối hợp giữa các instance.
**Cái giá:** thêm một số lần tính dư; cần biết `δ`.

#### d. Soft TTL và stale-while-revalidate
**Là gì:** mục có hạn mềm và hạn cứng. Quá hạn mềm: **trả bản cũ ngay**, làm mới ở nền. Quá hạn cứng: bắt buộc chờ. HTTP có `stale-while-revalidate` (RFC 5861 ⚠) cho ý tưởng tương tự.
**Use case:** nội dung chấp nhận cũ vài giây; nguồn lỗi thì vẫn phục vụ được bản cũ (`stale-if-error`).

#### e. Jitter và làm nóng cache
Jitter trải thời điểm hết hạn (Part 2, Lab 2.4). Làm nóng nạp trước các key phổ biến sau khi khởi động hoặc triển khai, để không bắt đầu lạnh.

| Cách | Phạm vi | Cần cache hỗ trợ | Cái giá |
|---|---|---|---|
| Gom request | một tiến trình | không | không chống giữa instance |
| Lease | toàn bộ cache chung | có | client phải xử lý chờ |
| XFetch | mỗi client, độc lập | không | vài lần tính dư |
| Soft TTL / SWR | tuỳ cài đặt | không | phục vụ dữ liệu cũ có chủ đích |

### Chiến lược invalidation

#### Xoá khi ghi, và thứ tự
Ghi nguồn rồi xoá key (Part 1, Lab 1.5). Có cửa sổ hiếm: người đọc muộn nạp bản cũ. Cần TTL hoặc lease.

#### Key có phiên bản
**Là gì:** đưa số phiên bản vào key (`board:1:v7`); đổi dữ liệu là đổi key, bản cũ tự hết hạn, không cần xoá.
**Use case:** dữ liệu có sẵn số phiên bản (cột `RowVersion`), hoặc cấu hình theo release.
**Cái giá:** nơi đọc phải biết phiên bản hiện tại (thường lấy từ một nguồn nhỏ).

#### Tag
**Là gì:** gắn mục vào nhóm (`boards`, `board:1`); xoá cả nhóm một lần (output cache tag, `HybridCache` tag).
**Use case:** sửa một Board phải xoá danh sách, chi tiết và tìm kiếm liên quan.

#### Theo sự kiện, CDC, outbox
**Là gì:** mỗi thay đổi dữ liệu phát một sự kiện; bộ lắng nghe xoá hoặc cập nhật cache. Có thể phát từ chính log của DB (CDC) hoặc từ bảng outbox ghi cùng giao dịch.
**Use case:** nhiều service hoặc nhiều instance cùng giữ bản sao một dữ liệu. Facebook dùng `mcsqueal` đọc log commit để xoá key trong Memcache; Uber dùng CDC (Flux) cộng TTL và xoá ở đường ghi cho CacheFront.
**Cái giá:** thêm hạ tầng; sự kiện có thể trễ hoặc mất, nên **vẫn giữ TTL làm lưới an toàn**. Meta mô tả việc đo nhất quán bằng theo dõi từng lần ghi (Polaris) vì lỗi nhất quán của cache rất khó thấy.

| Chiến lược | Độ cũ | Thêm hạ tầng | Rủi ro |
|---|---|---|---|
| Chỉ TTL | tối đa bằng TTL | không | cũ trong cả TTL |
| Xoá khi ghi | thấp | không | cửa sổ race hiếm; quên xoá chỗ khác |
| Key có phiên bản | gần 0 | nguồn phiên bản | key cũ chiếm chỗ đến khi hết hạn |
| Tag | thấp | tuỳ cài đặt | tag rộng xoá quá nhiều |
| Sự kiện / CDC / outbox | thấp | có | trễ, mất sự kiện; cần TTL |

### Consistent hashing và hash slot

**Vấn đề:** chọn node bằng `hash(key) mod N`; thêm một node thì **hầu hết key đổi node**, cache gần như rỗng cùng lúc (Lab 4.2).
**Consistent hashing:** đặt node và key lên cùng một vòng băm; key thuộc node đầu tiên theo chiều kim đồng hồ. Thêm hoặc bớt node chỉ chuyển khoảng `1/N` key. Mỗi node nhiều **điểm ảo** (virtual node) để tải đều.
**Hash slot (Valkey/Redis Cluster):** thay vì vòng, chia không gian key thành **16.384 slot** cố định; mỗi key thuộc một slot (CRC16 mod 16384 ⚠); mỗi node giữ một dải slot, thêm node là **chuyển slot** (cluster-spec ⚠).
**Use case:** cache nhiều node; node chết hoặc thêm node không làm lạnh cả cụm.

### Đo hit ratio và tuning

- Lấy từ chính cache (`keyspace_hits`, `keyspace_misses` ở Valkey ⚠; `GetCurrentStatistics()` ở `IMemoryCache`).
- Hit ratio cao không bằng đúng: cache trả cũ vẫn là hit. Đọc kèm **tải nguồn** và **tỉ lệ đọc cũ**.
- Tăng kích thước cache có lợi giảm dần; xem đường cong theo kiểu truy cập (Lab 1.4) trước khi mua RAM.
- Tuning eviction: chọn chính sách theo kiểu truy cập (LRU/LFU), đặt `maxmemory`, theo dõi `evicted_keys`.

## L4 Hands-on — Lab Part 4

Các lab là **mô phỏng tự viết** (mô hình), trừ chỗ ghi khác. Số đo chỉ tương đối.

#### Lab 4.1 — Stampede: chỉ hết hạn so với XFetch (mô phỏng)
1 key nóng, 200 yêu cầu/giây, TTL 10 giây, tính lại mất 0,2 giây, mô phỏng 600 giây, bắt đầu với cache nóng.
**Đầu ra thật:**
```text
chỉ hết hạn thì tính lại (không gom)  : tổng số lần tính  2338, lớn nhất trong một lần hết hạn  59, số yêu cầu gặp dữ liệu hết hạn 2338
XFetch (β = 1)                        : tổng số lần tính   165, lớn nhất trong một lần hết hạn   8, số yêu cầu gặp dữ liệu hết hạn    0
```
**Giải thích:** không gom, mỗi lần hết hạn có tới 59 yêu cầu cùng tính; XFetch giảm xuống tối đa 8 và không yêu cầu nào gặp dữ liệu hết hạn, tổng số lần tính cũng giảm. Giả định mô hình: tính lại mất cố định 0,2 giây, đến theo phân phối Poisson.

#### Lab 4.2 — `hash mod N` so với consistent hashing
100.000 key, băm MD5 lấy 32 bit.
**Đầu ra thật:**
```text
## hash mod N, 100000 key
N 4 -> 5: di chuyển 79.7% key
N 4 -> 3: di chuyển 75.0% key
## consistent hashing (vnode=100)
4 -> 5 node: di chuyển 20.6% key (lý thuyết ~20%)
4 -> 3 node: di chuyển 21.9% key (lý thuyết ~25%)
vnode=1: tải mỗi node (%) 72.8 / 17.0 / 3.8 / 6.4
vnode=100: tải mỗi node (%) 21.9 / 21.7 / 28.8 / 27.6
```
**Giải thích:** thêm một node vào cụm 4 node: `mod N` mất gần 80% key (cache lạnh gần hết), consistent hashing chỉ chuyển khoảng 20%. Với 1 điểm ảo mỗi node, một node ôm tới 72,8% key; 100 điểm ảo cho tải gần đều (21,7% đến 28,8%).

## L5 Troubleshooting — Stampede và dữ liệu cũ

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| DB tăng vọt theo chu kỳ đúng bằng TTL | nhiều key cùng hạn, hoặc một key nóng hết hạn | đồ thị truy vấn DB theo giây so với TTL | jitter, gom request, XFetch, soft TTL |
| Sau khi triển khai, DB quá tải vài phút | khởi động lạnh | so thời điểm triển khai với tải | làm nóng, triển khai từng phần, giới hạn tốc độ nạp |
| Dữ liệu cũ kéo dài hơn TTL | bản cũ bị nạp lại sau khi xoá (Lab 1.5 C) | so thời điểm ghi, xoá, nạp trong log | lease, TTL ngắn làm lưới an toàn |
| Thêm node cache làm DB quá tải | `hash mod N` đổi chỗ hầu hết key (Lab 4.2) | so tỉ lệ miss trước và sau | consistent hashing hoặc hash slot |
| Hit ratio cao nhưng người dùng báo dữ liệu sai | cache trả cũ vẫn được tính là hit | đo tỉ lệ đọc cũ, không chỉ hit ratio | sửa invalidation; đo nhất quán |
| Xoá theo tag mà vẫn thấy bản cũ | tag logic chỉ có hiệu lực ở nơi có nó; L1 instance khác | kiểm tra phạm vi tag | phát sự kiện, rút hạn L1 |

## L6 Production — Quyết định khi chạy thật

- **TTL:** chọn theo **độ cũ chấp nhận được** của nghiệp vụ, không theo cảm tính; ghi lý do cạnh giá trị.
- **Mục tiêu hit ratio:** đặt theo tải nguồn chịu được (bảng L2 Part 1: 90% → 99% giảm tải nguồn 10 lần), không đặt con số phẳng.
- **Tải khi miss:** nguồn phải chịu được tải lúc cache lạnh hoặc chết; nếu không, cache đã trở thành thành phần **bắt buộc** chứ không còn là tối ưu (Part 5).
- **Giới hạn nạp lại:** giới hạn số lần tính đồng thời trên mỗi nguồn để một đợt miss không kéo sập nguồn.
- **Đo nhất quán:** với dữ liệu quan trọng, có cách phát hiện cache lệch nguồn (so mẫu định kỳ, theo dõi từng lần ghi như Polaris của Meta).

## L7 Architecture — Chọn cách chống stampede và invalidation

| Tình huống | Chọn | Vì sao |
|---|---|---|
| Một instance, key nóng | gom request | rẻ, không cần cache hỗ trợ |
| Nhiều instance, cache hỗ trợ lease | lease | một lần nạp trên toàn cụm |
| Nhiều instance, không có lease | XFetch + jitter | không cần phối hợp |
| Chấp nhận cũ vài giây, nguồn hay lỗi | soft TTL / stale-while-revalidate | phục vụ được khi nguồn lỗi |
| Nhiều service giữ bản sao | sự kiện/outbox + TTL | thay đổi lan nhanh, TTL đỡ khi mất sự kiện |
| Chỉ cần đúng khi có thay đổi nhỏ, ít | xoá khi ghi + TTL | đơn giản nhất đủ dùng |
| Không muốn xử lý invalidation | không cache, hoặc key có phiên bản | loại bỏ vấn đề thay vì xử lý |

## Tự kiểm tra

1. Vì sao gom request trong một tiến trình chưa đủ khi có 10 instance?
2. XFetch giảm stampede bằng cách nào, không cần phối hợp?
3. Thêm một node vào cụm `hash mod N` gây ra điều gì, và consistent hashing sửa ra sao?
4. Vì sao hit ratio cao có thể vẫn là hệ thống sai?

<details>
<summary>Đáp án ngắn</summary>

1. 10 instance vẫn có thể cùng miss và cùng chạm nguồn 10 lần; cần lease hoặc làm mới sớm.
2. Mỗi yêu cầu có xác suất tính lại trước hạn, tăng khi gần hạn, nên thường một yêu cầu làm mới trước khi hết hạn (Lab 4.1).
3. Khoảng 80% key đổi node (Lab 4.2); consistent hashing chỉ chuyển khoảng 1/N.
4. Cache trả cũ vẫn tính là hit; cần đo độ cũ và tải nguồn.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 (mô phỏng, đầu ra thật) và Lab 4.2 (mô phỏng băm, đầu ra thật); cả hai là mô hình.
**Đã tự mở:** bài XFetch của Vattani và cộng sự (công thức trích từ PDF); bài "Scaling Memcache at Facebook" (lease, `mcsqueal`, số liệu 17K → 1,3K); Uber Engineering về CacheFront (26/08/2025); Meta "Cache made consistent" (08/06/2022).
**Chỉ qua agent (⚠):** RFC 5861 (`stale-while-revalidate`, `stale-if-error`); Valkey cluster-spec (16.384 slot, CRC16); Karger và cộng sự về consistent hashing; `keyspace_hits` và `keyspace_misses` trong `INFO`; Debezium/CDC.
**Chưa kiểm:** hành vi lease và hash slot trên Valkey thật (chưa chạy).
