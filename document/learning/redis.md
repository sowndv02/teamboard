# Redis / Valkey

> Loại: Công cụ · Một trong các lựa chọn của [Caching](caching.md#bản-đồ-giải-pháp) (xem phần "Phạm vi" ở mục 5). Cache-aside, TTL, invalidation, stampede nằm ở caching.md; ghi chú này chỉ nói phần riêng của Redis/Valkey
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: **chưa dùng, chưa có code**; Redis server bị cấm (T3), **Valkey** là hướng thay thế đang theo dõi (`config/dependencies.yaml:58-59`) · Phiên bản: chưa ghim (`config/tool-versions.yaml` không có mục này)

## 1. Vì sao cần một công cụ riêng cho cache?

[caching.md](caching.md) nói cache là khái niệm; còn cache trong tiến trình (`IMemoryCache`) mỗi instance một bản riêng nên nhiều instance trả kết quả khác nhau và mất khi restart. Redis/Valkey là **kho dữ liệu trong RAM dùng chung qua mạng**: mọi instance thấy cùng một bản, lại có sẵn TTL, eviction và thao tác nguyên tử (tăng bộ đếm, thêm vào set). Vì có cấu trúc dữ liệu nên nó còn được dùng ngoài cache: bộ đếm rate limit tập trung, phiên, hàng đợi nhẹ, stream, pub/sub.

## 2. Là gì (hiểu trong 1 phút)

Một server key-value giữ dữ liệu trong RAM, mỗi key trỏ tới một **cấu trúc dữ liệu** (không chỉ chuỗi). Việc **thực thi lệnh** diễn ra tuần tự trên một luồng, nên một lệnh chậm chặn mọi client khác (mục 6); từ Valkey 8.0 phần đọc/ghi mạng (I/O) có thể chạy ở luồng riêng (theo trang so sánh engine của ElastiCache), nhưng thực thi lệnh vẫn tuần tự. **Valkey** là nhánh BSD-3 của Redis do Linux Foundation tạo năm 2024 (mục 7); client Redis nói chuyện với Valkey được ở mức thực dụng, nhưng tác giả StackExchange.Redis không duy trì danh sách tương thích chính thức, nên cần thử với đúng phiên bản bạn dùng.

## 3. Khái niệm cốt lõi (riêng của công cụ)

| Khái niệm | Nghĩa | Ví dụ |
|---|---|---|
| Cấu trúc dữ liệu | string, hash, list, set, sorted set, stream, bitmap... | hash cho object nhỏ; sorted set cho bảng xếp hạng |
| `maxmemory` + `maxmemory-policy` | Giới hạn RAM và cách bỏ key khi đầy | `allkeys-lru` (tài liệu gợi ý khi chưa chắc); `noeviction` từ chối ghi khi đầy |
| RDB | Snapshot theo thời điểm | gọn, khởi động nhanh, có thể mất vài phút dữ liệu gần nhất |
| AOF | Ghi lại từng lệnh ghi | `fsync everysec` (mặc định) mất tối đa khoảng 1 giây |
| Replication | Primary → replica, **bất đồng bộ** | failover có thể mất ghi |
| Cluster | 16384 hash slot, `slot = CRC16(key) mod 16384` | lệnh nhiều key chỉ chạy khi các key cùng slot; dùng hash tag `{...}` |
| Pub/sub, Stream | Truyền thông điệp trong Valkey | đẩy sự kiện realtime (Stack Overflow, mục 7) |

**Persistence, giải thích kỹ.** Khi dùng làm cache, mất dữ liệu khi restart chấp nhận được vì dựng lại từ DB; khi bạn tắt persistence thì restart cho cache rỗng (cold start, xem caching.md). Khi dùng như kho dữ liệu, bật RDB + AOF (tài liệu Valkey khuyên bật cả hai nếu cần độ bền gần như DB; khi bật cả hai nó dùng AOF để khôi phục). Cái giá: RDB dùng `fork()` và với dataset lớn có thể làm server khựng từ vài ms tới gần 1 giây. **Bẫy replication:** nếu primary tắt persistence rồi tự khởi động lại với dữ liệu rỗng, replica sẽ sao chép bản rỗng và mất sạch (theo tài liệu replication).

## 4. Cách dùng

**Chạy thử cục bộ** (tên image và tag **Chưa kiểm**: kiểm trên Docker Hub trước khi ghim; dùng Valkey vì Redis bị cấm):

```bash
docker run --rm -d --name vk -p 6379:6379 valkey/valkey
docker exec -it vk valkey-cli SET board:1 "Sprint 1" EX 30   # OK
docker exec -it vk valkey-cli TTL board:1                    # số giây còn lại (<= 30)
docker exec -it vk valkey-cli CONFIG GET maxmemory-policy    # chính sách eviction hiện tại
```

**Dùng từ .NET qua `IDistributedCache`** (**Chưa kiểm**: chưa biên dịch trong dự án; gói `Microsoft.Extensions.Caching.StackExchangeRedis` giấy phép MIT theo NuGet, phụ thuộc `StackExchange.Redis` giấy phép MIT theo GitHub, **chưa có trong `config/dependencies.yaml`**, phải kiểm và ghi vào đó trước khi thêm, theo `dependency-policy.md` mục 4):

```csharp
builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = builder.Configuration["Cache:Configuration"]; // chuỗi kết nối: biến môi trường, không commit
    o.InstanceName = "teamboard:";
});

public sealed class BoardCache(IDistributedCache cache, ILogger<BoardCache> logger)
{
    public async Task<BoardSummary?> GetAsync(Guid id, Func<Task<BoardSummary?>> load, CancellationToken ct)
    {
        var key = $"board:{id}";
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is not null) return JsonSerializer.Deserialize<BoardSummary>(bytes);
        }
        catch (Exception ex)
        {
            // Cache lỗi thì rơi về nguồn, nhưng phải để lại dấu vết để thấy hit ratio tụt
            logger.LogWarning(ex, "Đọc cache thất bại cho {Key}, chuyển sang đọc nguồn", key);
        }

        var value = await load();
        if (value is null) return null;
        try
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ghi cache thất bại cho {Key}", key);
        }
        return value;
    }
}
```

Khối `try/catch` là điểm khác với cache trong tiến trình: cache ngoài **có thể lỗi qua mạng**, và hệ thống không nên sập vì thế (bài học Discord và Honeycomb ở caching.md). Mỗi `catch` phải ghi log (thêm bộ đếm metric nếu có): nuốt lỗi âm thầm là vi phạm `AGENTS.md`, và khi đó Redis hỏng nhưng không ai biết, DB bị dội tải mà không có tín hiệu. Đặt lớp này sau interface của mình (Decorator như caching.md) để thay được công cụ.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Dùng chung giữa các instance, đọc rất nhanh (RAM) | Thêm một chuyến mạng và một thành phần phải vận hành, giám sát, vá lỗi |
| Cấu trúc dữ liệu và thao tác nguyên tử phong phú | Một luồng xử lý lệnh: lệnh chậm (như `KEYS`) chặn tất cả |
| TTL và eviction có sẵn | RAM hữu hạn; đầy mà `noeviction` thì kẹt cả ghi (Slack, mục 7) |
| Có HA (replication, cluster) và dịch vụ quản lý (ElastiCache) | Replication bất đồng bộ nên failover có thể mất ghi; ElastiCache chạy 24/7 nên tốn tiền |
| Valkey: BSD-3, do quỹ trung lập quản | Tương thích client và phiên bản cần tự kiểm; hệ sinh thái module có thể khác Redis |

**Nên dùng khi:** nhiều instance cần chung một cache hoặc bộ đếm, đã đo thấy cần.
**Không nên khi:** tải thấp (cache trong tiến trình hoặc không cache), cần nguồn sự thật bền (dùng DB; xem GitHub, mục 7), chưa được duyệt giấy phép.
### Phạm vi và những gì công cụ này KHÔNG đại diện

Redis/Valkey chỉ là **một dòng** ("cache phân tán kiểu cấu trúc dữ liệu") trong [bản đồ giải pháp caching](caching.md#bản-đồ-giải-pháp). Nó **không** đại diện cho cả khái niệm cache:

- **Cache không cần Redis.** HTTP cache của trình duyệt/CDN (`Cache-Control`, ETag) và cache trong tiến trình (`IMemoryCache`) thường là bước đầu rẻ hơn và không thêm hạ tầng.
- **Memcached** (theo bảng AWS ElastiCache đã mở): đa luồng, kiểu dữ liệu đơn giản, **không** replication/HA sẵn, không pub/sub, không durability; Redis/Valkey đơn luồng nhưng có cấu trúc dữ liệu phức tạp, replication, pub/sub và (Valkey 9.0 trên ElastiCache) durability.
- **HybridCache** (.NET) kết hợp cache trong tiến trình (L1) với cache phân tán (L2) và có bảo vệ stampede trong một instance; Redis/Valkey có thể đóng vai L2.
- **Cache của tầng dữ liệu** (DAX, buffer pool của DB) ở gần dữ liệu hơn, không phải thứ Redis thay thế.
- **Redis không là nguồn sự thật.** Dữ liệu chỉ nằm trong Redis là rủi ro (GitHub, Slack ở mục 7).
- Các tính năng ngoài cache (stream, search, vector) có trên Valkey/Redis nhưng ngoài phạm vi ghi chú này.

## 6. Lỗi hay gặp và cách tránh (riêng của công cụ)

- **`KEYS` trên production.** Triệu chứng: server khựng. Nguyên nhân: `KEYS` là O(N) và chặn đơn luồng; tài liệu Redis khuyên không dùng trong code ứng dụng, thay bằng `SCAN`. (Tôi không tìm được postmortem công khai chỉ riêng vì lệnh này.)
- **Đầy RAM.** Triệu chứng: ghi bị từ chối hoặc key biến mất. Cách tránh: đặt `maxmemory` và policy; để dư RAM vì bộ đệm replication và AOF không tính vào `maxmemory`.
- **Coi Redis là nguồn sự thật.** Cách tránh: mọi thứ trong đó phải dựng lại được; không thì đặt ở DB bền.
- **Replica mất sạch** sau khi primary khởi động lại rỗng (mục 3).
- **Giấy phép lẫn lộn.** Image/phiên bản Redis ≥ 7.4 không còn BSD (mục 7); cần xác nhận trước khi đưa vào compose hay Terraform.
- **Chuỗi kết nối nằm trong repo.** Cách tránh: biến môi trường/secret manager, file `*.example` (`AGENTS.md`).

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **GitHub** (2017, cập nhật 2024) | Dùng Redis hai kiểu: cache LRU (transient) và dữ liệu bền không có bản khác (cấu hình, activity feed...) | Tắt persistence; chuyển dữ liệu bền sang MySQL (kho key/value GitHub::KV và schema cho feed); chạy song song bằng feature flag; đo từng loại dữ liệu | Họ nói đã "vượt quá" Redis làm kho bền, nhưng cũng nhấn mạnh MySQL không phải đáp án cho mọi thứ: quyết định dựa trên số đo. Timeline tổ chức là 67% của hơn 350 triệu lượt ghi/ngày của nhóm timeline | [Moving persistent data out of Redis](https://github.blog/engineering/infrastructure/moving-persistent-data-out-of-redis/) | Chắc (đã mở) |
| **Stack Overflow** (2019) | Hệ thống nhiều site cần cache chung giữa nhiều web server | Cache hai tầng: RAM cục bộ trước, miss thì hỏi Redis; replica để HA; pub/sub cho cập nhật realtime | Một node lớn với CPU rất thấp gánh được tải lớn; cache hai tầng giảm số chuyến mạng. Số liệu chi tiết trong bài (lệnh/ngày, số key, trễ) chưa tự đối chiếu | [How Stack Overflow caches apps for a multi-tenant architecture](https://stackoverflow.blog/2019/08/06/how-stack-overflow-caches-apps-for-a-multi-tenant-architecture/) (06/08/2019) | Vừa (agent đã đọc, tôi chưa mở lại) |
| **Slack** (đăng 06/12/2017) | Job queue chạy trên Redis; DB chậm làm queue phình, Redis chạm giới hạn RAM, không enqueue được và cả dequeue cũng kẹt | Đặt Kafka phía trước làm bộ đệm bền; Redis ở phía sau; hai thành phần Go chuyển job có giới hạn tốc độ | Dùng Redis làm queue cần áp lực ngược (backpressure) và đệm bền ở trước; hết RAM thì kẹt cả đọc lẫn xoá | [Scaling Slack's job queue](https://slack.engineering/scaling-slacks-job-queue/) | Vừa (như trên) |
| **Redis Ltd. → giấy phép** (20/03/2024) | Doanh thu thương mại chảy qua các cloud lớn mà họ cho là hưởng lợi từ Redis | Từ Redis 7.4: RSALv2 hoặc SSPLv1, không còn BSD-3; tự nhận không phải open source theo OSI; bản ≤ 7.2 vẫn BSD, không hồi tố; dùng nội bộ không bị ảnh hưởng | Nền tảng cho rủi ro giấy phép trong `dependency-policy.md` | [Redis adopts dual source-available licensing](https://redis.io/blog/redis-adopts-dual-source-available-licensing/) | Chắc (đã mở) |
| **Valkey / Linux Foundation** (28/03/2024) | Cộng đồng và nhà cung cấp cloud cần bản Redis giấy phép mở | Fork từ Redis 7.2.4, giữ BSD-3; hậu thuẫn: AWS, Google Cloud, Oracle, Ericsson, Snap | Là nhánh thay thế mà `dependency-policy.md` chỉ ra | [Linux Foundation launches Valkey](https://www.linuxfoundation.org/press/linux-foundation-launches-open-source-valkey-community) | Chắc (đã mở) |
| **AWS ElastiCache** (08/10/2024) | Cần engine giấy phép mở cho dịch vụ quản lý | ElastiCache và MemoryDB hỗ trợ Valkey (7.2 lúc công bố) | Dùng Valkey trên AWS khả thi; số % giá trong bài là số tiếp thị, **không trích**, xem trang giá hiện hành | [Announcement](https://aws.amazon.com/blogs/database/amazon-elasticache-and-amazon-memorydb-announce-support-for-valkey/) | Vừa (agent đã đọc, tôi chưa mở lại) |

**Cập nhật giấy phép sau đó (Vừa):** theo bài redis.io/blog/agplv3 (agent đã đọc, tôi chưa mở lại), từ Redis 8 có thêm lựa chọn AGPLv3. `dependency-policy.md` mục 4 cấm AGPL theo mặc định, nên **Redis 8 cũng không tự động hợp lệ**; muốn dùng phải được người dùng duyệt và ghi ADR.

**Mô hình phổ biến (Chắc, theo tài liệu Valkey và các case trên):** cache với TTL + eviction `allkeys-lru`; Redis làm chỉ mục phụ/cache chứ không làm nguồn sự thật; nhiều tầng (RAM cục bộ trước, Redis sau); HA bằng replica, mở rộng bằng cluster.

**Khi nó bị dùng sai (Chắc):** GitHub phải chuyển dữ liệu bền ra khỏi Redis; Slack bị kẹt queue khi hết RAM. Hai bài học chung: đừng để Redis là nơi duy nhất giữ dữ liệu, và có áp lực ngược khi dùng làm queue.

**Chưa có nguồn gốc đã mở cho:** Instagram, Twitter/X, Pinterest, Uber (không mở được hoặc chưa tìm), nên không nêu.

## 8. Trong TeamBoard

- **Hiện không có Redis/Valkey** trong `src/` hay compose. Lưu trữ đã chọn: SQL Server, PostgreSQL, DynamoDB (`PROJECT.md`).
- **Chính sách:** `config/dependencies.yaml:58` ghi Redis server `status: banned` (T3); `:59` ghi Valkey T1 `status: watch`. `.agents/rules/dependency-policy.md` mục 5 và 7: dùng Valkey hoặc **không cache khi chưa cần**; image trong container phải kiểm giấy phép.
- **Chỗ có thể dùng:** bài tập cache-aside + Decorator (`document/architecture/patterns-catalog.md` mục 2.5, 7). Rate limit gateway đang đếm cục bộ vì 1 instance (`document/design/api-gateway.md:153`).
- **Chi phí:** ElastiCache chưa cần ở tải thấp (`.agents/skills/aws-service-selection/references/aws-service-catalog.md:35`) và thuộc nhóm chạy rỗng 24/7 (`cost-model.md:44`). Chọn dịch vụ qua `aws-service-selection` trước khi viết Terraform.
- **Điểm lệch (báo, không tự sửa):** `patterns-catalog.md` (dòng 38, 78, 91) vẫn viết "Redis"; `config/dependencies.yaml:58` ghi "ElastiCache dùng Valkey (kiểm tra)" nhưng nguồn AWS (mục 7) cho thấy đã hỗ trợ từ 10/2024, nên dòng này có thể cập nhật; client .NET chưa có trong `config/dependencies.yaml`.

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Mức |
|---|---|---|---|---|
| 1 | [caching.md](caching.md) | Nội bộ | khái niệm gốc, stampede, invalidation | Chắc |
| 2 | `.agents/rules/dependency-policy.md` mục 2, 4, 5 | Nội bộ | vì sao Valkey thay Redis | Chắc |
| 3 | `document/architecture/patterns-catalog.md` mục 2.5 | Nội bộ | cache-aside + Decorator | Chắc |
| 4 | https://valkey.io/topics/persistence/ , `/replication/` , `/cluster-spec/` , `/lru-cache/` | Chính thức | persistence, nhân bản, cluster, eviction | Chắc (đã mở trang `/lru-cache/` và mục lục; ba trang còn lại qua agent) |
| 5 | https://docs.aws.amazon.com/AmazonElastiCache/latest/dg/Strategies.html | Chính thức | chiến lược cache trên ElastiCache | Chắc (đã mở) |
| 6 | https://redis.io/docs/latest/commands/keys/ | Chính thức | vì sao tránh `KEYS` | Vừa (qua agent) |
| 7 | Tài liệu `IDistributedCache` trên learn.microsoft.com | Chính thức | API cache phân tán trong .NET | Chưa kiểm |

## 10. Tự kiểm tra / thực hành

1. Vì sao chạy `KEYS *` trên production có thể làm cả ứng dụng chậm, dù ứng dụng không gọi lệnh đó? (Gợi ý: đơn luồng.)
2. Cache của bạn dùng `allkeys-lru` và đầy RAM. Chuyện gì xảy ra với key chưa có TTL? Nếu đổi sang `volatile-lru` và không key nào có TTL thì sao? (Đối chiếu tài liệu eviction.)
3. Vì sao Slack đặt Kafka phía trước Redis thay vì chỉ tăng RAM? So với GitHub: hai case cùng dạy điều gì về việc Redis giữ dữ liệu?
4. Việc chạy được: chạy ví dụ ở mục 4, đặt `SET ... EX 5`, `GET` sau 6 giây (mong đợi: nil), rồi `CONFIG GET maxmemory`. Máy không có Docker thì ghi "không chạy được", đừng bịa kết quả.
