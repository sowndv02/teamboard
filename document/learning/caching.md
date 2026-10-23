# Caching (bộ nhớ đệm)

> Loại: Khái niệm · Công cụ trong dự án (dự kiến): [Redis / Valkey](redis.md), chỉ là một trong các lựa chọn, xem [Bản đồ giải pháp](#bản-đồ-giải-pháp)
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: **chưa dùng cache nào** (xem mục 8)

## 1. Vấn đề nó giải quyết

Một truy vấn "danh sách Board của tôi" chạy hàng nghìn lần mỗi phút. Mỗi lần đều chạm DB, tốn vài chục ms, và tải DB tăng tuyến tính theo lượng người dùng. Phần lớn lần chạy trả về cùng một kết quả vì dữ liệu hiếm khi đổi. Làm lại việc đắt để ra cùng một đáp án là lãng phí; DB sẽ thành điểm nghẽn trước khi ứng dụng kịp scale.

## 2. Là gì (hiểu trong 1 phút)

**Cache** là nơi lưu tạm bản sao của dữ liệu/kết quả tính toán ở chỗ **rẻ và gần hơn** nguồn gốc (RAM thay vì đĩa, cùng máy thay vì qua mạng, edge CDN thay vì origin). Lần đầu hỏi thì đi đường dài rồi giữ lại bản sao; các lần sau lấy bản sao. Cái giá: bản sao có thể **cũ** so với nguồn, và bạn phải quyết khi nào bỏ nó.

```
App ──(1) hỏi cache──► hit ──► trả ngay
 │                       
 └─ miss ──(2) hỏi nguồn (DB) ──(3) ghi bản sao vào cache (kèm TTL) ──► trả
```

Cache có ở nhiều tầng, càng gần người dùng càng rẻ nhưng càng khó kiểm soát độ tươi: trình duyệt → CDN/edge → cache trong tiến trình (on-box) → cache ngoài dùng chung (Redis, Memcached) → cache của DB.

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ |
|---|---|---|
| Hit / miss | Có / không có bản sao trong cache | hit ratio = hit ÷ tổng lần hỏi |
| TTL | Thời gian sống; hết hạn thì coi như miss | 30 giây cho danh sách board |
| Stale (cũ) | Bản sao khác nguồn hiện tại | sửa Board nhưng cache còn bản trước |
| Invalidation | Bỏ/cập nhật bản sao khi nguồn đổi | xoá key khi sửa Board |
| Priming (làm nóng) | Nạp sẵn dữ liệu vào cache lúc khởi động | tập dữ liệu nhỏ, tĩnh |
| **Cơ chế đọc/ghi** | Ai nạp và ghi cache, theo thứ tự nào | cache-aside, read-through, write-through, write-behind, write-around, refresh-ahead: **mục 3.1** |
| Stampede (thundering herd) | Key nóng hết hạn, hàng loạt request cùng đập nguồn | xem dưới |
| Eviction | Cache đầy thì bỏ bớt entry theo chính sách (LRU, LFU...) | CDN đẩy nội dung ít truy cập |
| Cold start | Cache trống (deploy, node mới) nên mọi request thành miss | nguồn bị dội tải sau khi khởi động |
| Negative caching | Cache cả kết quả "không có" để khỏi hỏi lại | `404` cache với TTL riêng |

### 3.1 Các cơ chế đọc và ghi cache

Câu hỏi then chốt phân biệt các cơ chế: **ai chịu trách nhiệm nạp và ghi cache**: chính ứng dụng (cache-aside, write-around) hay lớp cache nằm trên đường truy cập dữ liệu (read-through, write-through, write-behind). Theo trang Cache-Aside của Azure (đã mở): nhiều hệ cache thương mại cung cấp sẵn read-through và write-through hoặc write-behind; với cache không có, ứng dụng tự làm phần đó. Mỗi cơ chế dưới đây có mức nguồn riêng; **tên gọi và ranh giới giữa chúng không hoàn toàn thống nhất giữa các tài liệu**, nên hãy đọc "ai làm gì, theo thứ tự nào" hơn là nhớ tên.

#### a. Cache-aside (còn gọi lazy loading, side cache) *(Chắc: Azure Cache-Aside, AWS whitepaper, đã mở)*
**Ai nạp cache:** ứng dụng. **Chỉ nạp khi có người hỏi.**

```
ĐỌC:  app ─► cache ─hit──► trả
              └─miss─► app ─► nguồn (DB) ─► app ghi vào cache ─► trả
GHI:  app ─► nguồn (DB)  rồi  app XOÁ key trong cache (lần đọc sau tự nạp lại)
```
- **Ưu:** cache chỉ chứa dữ liệu thực sự được hỏi nên gọn và rẻ; đơn giản, có lợi ngay; node cache hỏng không làm sập ứng dụng (chỉ chậm hơn, vì mọi request thành miss) (AWS ElastiCache docs).
- **Nhược:** lần miss tốn thêm lượt gọi (hỏi cache, hỏi DB, ghi cache); dữ liệu có thể cũ vì cache chỉ đổi khi miss hoặc khi bị xoá; **không đảm bảo nhất quán** (một tiến trình khác sửa DB thì cache không biết cho tới khi hết hạn hoặc bị xoá) (Azure).
- **Thứ tự khi ghi quan trọng:** ghi nguồn **trước**, xoá cache **sau**. Xoá cache trước thì có một khoảng hở: một request đọc xen vào, thấy miss, nạp lại **bản cũ** từ DB vào cache, và cache giữ bản cũ (Azure).
- **Dùng khi:** cache không có read-through/write-through sẵn; nhu cầu dữ liệu khó đoán trước. **Không dùng khi:** dữ liệu nhạy cảm/liên quan bảo mật, tập dữ liệu tĩnh nhỏ (nên nạp sẵn và không cho hết hạn), hầu hết request không hit (tốn công kiểm cache mà không lợi) (Azure).

#### b. Read-through *(Chắc về định nghĩa chung: Azure Cache-Aside, AWS Builders' Library, đã mở; ví dụ sản phẩm: Vừa)*
**Ai nạp cache:** chính lớp cache (inline cache). Ứng dụng chỉ nói chuyện với cache.

```
ĐỌC:  app ─► cache ─hit──► trả
              └─miss─► cache tự đọc nguồn, tự lưu ─► trả
```
- **Ưu:** code ứng dụng đơn giản và đồng nhất (một giao diện duy nhất); logic nạp nằm ở một chỗ.
- **Nhược:** cache **trở thành phụ thuộc về độ sẵn sàng**: cache lỗi thì mọi truy cập lỗi và client không tự bù được (AWS Builders' Library); phải dùng sản phẩm hoặc thư viện hỗ trợ.
- **Ví dụ:** DAX cho DynamoDB, Varnish được AWS Builders' Library nêu như inline cache (agent đã đọc). Cache-aside là cách **mô phỏng** read-through bằng code ứng dụng (Azure).

#### c. Write-through *(Chắc: Azure caching guidance, AWS whitepaper, ElastiCache docs, đã mở)*
**Ai ghi cache:** ghi cache **cùng lúc** ghi nguồn, trong cùng một thao tác; chỉ trả thành công khi cả hai xong (Azure).

```
GHI:  app ─► (nguồn  VÀ  cache) cùng một thao tác ─► trả thành công khi cả hai OK
ĐỌC:  app ─► cache ─hit──► trả (luôn mới)   [miss ─► thường rơi về lazy loading]
```
- **Ưu:** cache luôn tươi; người đọc thấy giá trị mới ngay sau khi ghi thành công; ít đọc DB hơn nên DB nhẹ (AWS, Azure).
- **Nhược:** **ghi chậm hơn** (hai chuyến) và cần phối hợp logic; dữ liệu ít ai đọc cũng bị ghi vào cache nên cache lớn, đắt, nhiều thứ "chết"; node cache mới (thay thế/scale) thiếu dữ liệu cho tới khi được ghi hoặc đọc (ElastiCache docs).
- **Thực tế:** thường đi **cùng lazy loading** (để miss được nạp) và **TTL** (để dọn dữ liệu chết) (AWS).
- **Dùng khi:** đường đọc nhiều và cần "đọc ngay sau ghi" mới; **không** khi dữ liệu ít được đọc sau khi ghi hoặc đổi liên tục (Azure khuyên khi đó hãy xoá khi ghi hoặc bỏ qua cache).

#### d. Write-behind (write-back) *(Vừa: Azure chỉ nhắc tên; phần mô tả còn lại là kiến thức chung, Chưa kiểm)*
**Ai ghi nguồn:** cache, **sau** khi đã ghi vào cache và trả thành công cho ứng dụng.

```
GHI:  app ─► cache (ghi ngay, trả thành công) ···sau đó, bất đồng bộ, thường gom lô···► nguồn
```
- **Ưu (chung):** ghi rất nhanh với ứng dụng; gom nhiều lần ghi thành ít lần ghi nguồn nên giảm tải ghi.
- **Nhược (chung):** **mất dữ liệu nếu cache chết trước khi kịp ghi nguồn**; nguồn sự thật tạm thời lệch cache; cần cache bền hoặc cơ chế phục hồi. Điều này **trái nguyên tắc** của Azure caching guidance: coi cache là kho tạm có thể biến mất bất cứ lúc nào, đừng chỉ giữ dữ liệu quý trong cache.
- **Khi nào hợp lý:** dữ liệu mất được hoặc tái tạo được (số đo, bộ đếm xấp xỉ); không hợp với dữ liệu nghiệp vụ như Board, Task. Chưa tìm được case thực tế có nguồn nên ghi chú này không nêu.

#### e. Write-around *(Chưa kiểm: chưa mở nguồn nào định nghĩa; mô tả là kiến thức chung)*
**Ai ghi cache:** không ai khi ghi: ghi **thẳng nguồn, bỏ qua cache**; cache chỉ được nạp khi đọc và miss.

```
GHI:  app ─► nguồn (không động tới cache; key cũ nếu có phải bị xoá hoặc hết hạn)
ĐỌC:  như cache-aside
```
- **Ưu (chung):** dữ liệu ghi nhiều nhưng ít đọc lại không làm bẩn cache.
- **Nhược (chung):** đọc ngay sau khi ghi là miss; nếu key cũ còn trong cache mà không bị xoá thì đọc ra bản cũ. Trên thực tế gần với cache-aside, khác ở chỗ nhấn mạnh việc **không nạp cache lúc ghi**.

#### f. Refresh-ahead (làm mới trước hạn) *(Tên gọi: Chưa kiểm; cơ chế tương đương có nguồn ở dưới)*
**Ai làm mới:** cache hoặc ứng dụng **chủ động nạp lại** entry sắp hết hạn **trước khi** có người hỏi, để người dùng không bao giờ gặp miss ở key nóng.
- **Cơ chế có nguồn đã mở:** (1) **soft TTL / hard TTL** (AWS Builders' Library): quá soft TTL thì thử làm mới, nguồn lỗi thì vẫn trả bản cũ tới hard TTL; (2) **làm mới sớm theo xác suất** của Cloudflare (mục 7): gần hết hạn thì mỗi request tự bốc thăm có làm mới không, không cần khoá.
- **Ưu:** key nóng không bao giờ miss đồng loạt (giảm stampede). **Nhược:** làm mới cả thứ chẳng ai hỏi nữa (tốn nguồn); phức tạp hơn.

#### So sánh nhanh

| Cơ chế | Ai nạp / ghi cache | Đường đọc | Đường ghi | Độ tươi | Rủi ro mất dữ liệu | Hợp khi |
|---|---|---|---|---|---|---|
| Cache-aside | Ứng dụng | miss → đọc nguồn → ghi cache | ghi nguồn, xoá key | có thể cũ tới lần đọc sau | Không (nguồn luôn đúng) | Phổ biến nhất; đọc nhiều, chịu hơi cũ |
| Read-through | Lớp cache | cache tự nạp khi miss | tuỳ sản phẩm | như cache-aside | Không | Muốn code đơn giản; có sản phẩm hỗ trợ |
| Write-through | Ghi cache + nguồn cùng lúc | thường hit | ghi cả hai | luôn mới | Thấp (nguồn được ghi đồng bộ) | Cần đọc-sau-ghi mới; đọc nhiều |
| Write-behind | Ghi cache, nguồn sau | hit | ghi cache trước | cache mới, nguồn trễ | **Có** nếu cache chết trước khi ghi nguồn | Dữ liệu mất được; ghi rất nhiều |
| Write-around | Không ghi cache khi ghi | miss rồi nạp | ghi thẳng nguồn | có thể cũ nếu không xoá key | Không | Ghi nhiều, ít đọc lại |
| Refresh-ahead | Làm mới trước hạn | hầu như luôn hit | – | gần mới | Không | Key nóng cần tránh stampede |

**Kết hợp thường gặp:** cache-aside + TTL + xoá khi ghi (mặc định hợp lý); write-through + lazy loading + TTL (AWS khuyên dùng chung để vừa tươi vừa gọn). Hai cơ chế không loại trừ nhau.

### 3.2 Giữ cache đúng và chống quá tải

- **TTL (hết hạn):** quá ngắn thì cache hết tác dụng vì liên tục miss; quá dài thì dữ liệu cũ (Azure). Chọn theo mức nghiệp vụ chịu được độ cũ.
- **Eviction (đẩy bớt khi đầy):** cache có kích thước giới hạn; hầu hết dùng LRU (ít dùng gần nhất) để chọn entry bỏ, một số cho tuỳ biến (Azure); entry đắt để tính nên cấu hình riêng cho ở lại lâu hơn.
- **Priming (làm nóng):** nạp sẵn dữ liệu cần dùng lúc khởi động; vẫn nên dùng cache-aside song song cho những gì hết hạn hoặc bị đẩy ra (Azure).

**Stampede, giải thích kỹ.** Hình dung key "board:42" có TTL 30 giây và 500 request/giây đọc nó. Đúng giây thứ 30 key hết hạn; trong vài chục ms trước khi request đầu tiên kịp nạp lại, hàng trăm request cùng thấy miss và cùng chạy truy vấn đắt. DB bị dồn tải đúng lúc đó, chậm đi, khiến việc nạp lại càng lâu, càng thêm request miss. Có ba cách giảm: (a) **gom request** (request coalescing): chỉ một request đi nguồn, các request khác chờ kết quả; (b) **lease/lock**: chỉ ai giữ token mới được nạp (cách Facebook, mục 7); (c) **làm mới sớm theo xác suất**: gần hết hạn thì ngẫu nhiên vài request tự làm mới trước, không cần khoá (cách Cloudflare, mục 7). Ngoài ra có thể giữ dữ liệu cũ thêm một lúc (**soft TTL / hard TTL**): quá soft TTL thì thử làm mới, nguồn lỗi thì vẫn trả bản cũ tới hard TTL.

**Invalidation, giải thích kỹ.** "Có hai việc khó trong khoa học máy tính: đặt tên và vô hiệu hoá cache." Khó vì nguồn đổi ở một nơi, bản sao nằm ở nhiều nơi, và các thao tác có thể đến **sai thứ tự** (ghi giá trị cũ vào cache sau khi đã xoá). Cách thực tế: TTL ngắn làm lưới an toàn, kèm xoá key khi ghi; chỗ cần chặt thì dùng cơ chế như lease (mục 7).

## 4. Cách dùng

**Cache-aside bằng Decorator trong .NET** (minh hoạ chung, **Chưa kiểm**: chưa biên dịch trong dự án; `IMemoryCache` thuộc nền tảng ASP.NET Core, T0):

```csharp
public interface IBoardReader
{
    Task<BoardSummary?> GetAsync(Guid id, CancellationToken ct);
}

public sealed class CachedBoardReader(IBoardReader inner, IMemoryCache cache) : IBoardReader
{
    private static readonly MemoryCacheEntryOptions Options = new()
        { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };

    public async Task<BoardSummary?> GetAsync(Guid id, CancellationToken ct)
    {
        var key = $"board:{id}";
        if (cache.TryGetValue(key, out BoardSummary? hit)) return hit;   // hit

        var value = await inner.GetAsync(id, ct);                        // miss: hỏi nguồn
        if (value is not null) cache.Set(key, value, Options);           // ghi kèm TTL
        return value;
    }
}

// Đăng ký (không cần thư viện): bọc implementation thật bằng decorator
services.AddMemoryCache();
services.AddScoped<BoardReader>();
services.AddScoped<IBoardReader>(sp => new CachedBoardReader(
    sp.GetRequiredService<BoardReader>(), sp.GetRequiredService<IMemoryCache>()));
```

Kết quả mong đợi: lần gọi đầu chạm DB, các lần sau trong 30 giây không chạm DB. **Lỗ hổng cố ý để bạn tìm:** hai request cùng miss một lúc đều chạm DB (stampede nhỏ), và ghi Board không xoá key nên người dùng thấy bản cũ tối đa 30 giây. Sửa cả hai là bài tập ở mục 10.

**Cache trong tiến trình vs ngoài tiến trình:** `IMemoryCache` nhanh nhất và không cần hạ tầng, nhưng mỗi instance một bản riêng nên instance A và B có thể trả giá trị khác nhau. Cache ngoài dùng chung ([Redis/Valkey](redis.md)) cho mọi instance thấy cùng một bản, đổi lại thêm một chuyến mạng và một thành phần phải vận hành.

**Cache HTTP (tầng trình duyệt/CDN)** (minh hoạ chung, **Chưa kiểm**: chưa chạy trong dự án; ASP.NET Core Minimal API):

```csharp
app.MapGet("/api/v1/boards/{id:guid}", async (Guid id, IBoardReader reader, HttpContext ctx, CancellationToken ct) =>
{
    var board = await reader.GetAsync(id, ct);
    if (board is null) return Results.NotFound();
    ctx.Response.Headers.CacheControl = "private, max-age=30";   // dữ liệu cá nhân hoá: không cho CDN/proxy lưu
    return Results.Ok(board);
});
```
Mong đợi: trình duyệt dùng lại phản hồi 30 giây mà không gọi server; CDN không lưu (vì `private`). Vì sao `private`: danh sách Board gắn với người dùng, nếu cache dùng chung lưu thì người khác có thể thấy (MDN: phản hồi cá nhân hoá không được lưu ở shared cache).

**Quy trình quyết định:** đo trước (truy vấn nào chậm, bao nhiêu lần/giây) → tối ưu truy vấn/index → mới cache → chọn TTL theo mức chịu cũ của nghiệp vụ → đặt giám sát hit ratio.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Giảm độ trễ và tải lên nguồn | Dữ liệu có thể cũ; invalidation khó |
| Chịu được tải đọc tăng mà không scale DB | Thêm thành phần vận hành, thêm lỗi mới (stampede, cold start) |
| Giảm chi phí (ít query, ít egress) | **Hành vi hai chế độ** (có/không cache): hệ thống quen cache có thể sập khi cache mất (AWS Builders' Library) |
| Có thể cho phép đọc bản cũ khi nguồn lỗi (soft/hard TTL) | Che khuyết điểm của truy vấn kém thay vì sửa nó |

**Nên dùng khi:** đọc nhiều, chịu được hơi cũ, truy vấn đắt, đã đo thấy nút cổ chai.
**Không nên khi:** tải thấp, dữ liệu đổi liên tục, cần nhất quán mạnh, truy vấn đã nhanh.
**Lựa chọn thay thế cache:** tối ưu truy vấn/index, read replica, materialized view/read model (CQRS).

### Bản đồ giải pháp

Cache không phải một công cụ: nó là một **họ kỹ thuật ở nhiều tầng**. Redis/Valkey chỉ là **một dòng** trong bảng dưới. Mức: Chắc nếu tôi tự mở trang; Vừa nếu chỉ agent đọc.

| Tầng / họ | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn (mức) |
|---|---|---|---|---|
| **Cache của trình duyệt (private cache)** | HTTP `Cache-Control`, `ETag` | Tài nguyên tĩnh có hash trong URL (`public, max-age=31536000, immutable`); HTML thì `no-cache` kèm ETag để revalidate (304) | Không kiểm soát được cache nằm ở máy người dùng; `no-store` mất lợi ích như bfcache | MDN HTTP caching (Chắc, đã mở); RFC 9111, RFC 5861 (Vừa) |
| **CDN / edge (shared cache)** | Cloudflare, Amazon CloudFront | Nội dung công khai, người dùng phân tán địa lý; giảm tải origin | Cache key và TTL cần thiết kế (key sai thì hit thấp hoặc lộ nội dung riêng); purge có giới hạn tốc độ, CloudFront khuyên dùng tên file có phiên bản thay cho invalidation | developers.cloudflare.com, docs.aws.amazon.com CloudFront (Vừa) |
| **Cache trong tiến trình** | `IMemoryCache` (.NET) | Một instance hoặc có sticky session; nhanh nhất, không cần hạ tầng | Mỗi instance một bản riêng, không nhất quán giữa instance, mất khi restart | learn.microsoft.com caching overview (Vừa) |
| **Cache hai tầng L1 + L2** | `HybridCache` (.NET) | Muốn nhanh như in-memory và chia sẻ qua cache phân tán; có bảo vệ stampede **trong một instance** | Thêm phụ thuộc; xoá key chỉ ảnh hưởng L1 của server hiện tại (L1 server khác không bị xoá) | learn.microsoft.com HybridCache (Vừa) |
| **Cache phân tán, kiểu cấu trúc dữ liệu** | Redis / Valkey (thực thi lệnh tuần tự một luồng, I/O có thể đa luồng từ Valkey 8.0, nhiều kiểu dữ liệu, replication/HA, pub/sub, persistence tuỳ chọn) | Nhiều instance cần chung một cache hoặc bộ đếm; cần cấu trúc dữ liệu | Thêm thành phần vận hành; Redis đổi giấy phép (xem [redis.md](redis.md)) | docs.aws.amazon.com ElastiCache (Chắc, đã mở), valkey.io |
| **Cache phân tán, key-value đơn giản** | Memcached (đa luồng; theo bảng AWS: không replication/HA, không pub/sub, không durability; kiểu dữ liệu đơn giản) | Mô hình đơn giản nhất, node lớn nhiều lõi, scale ngang thêm bớt node, cache object | Không HA/failover sẵn; không cấu trúc dữ liệu phức tạp | [ElastiCache: so sánh engine](https://docs.aws.amazon.com/AmazonElastiCache/latest/dg/SelectEngine.html) (Chắc, đã mở) |
| **Cache của tầng dữ liệu** | DynamoDB Accelerator (DAX); buffer pool của DB (PostgreSQL `shared_buffers`, SQL Server buffer pool) | DAX: đọc cực nhanh, hot key, đọc nhiều; buffer pool là sẵn có, chỉ cần cấu hình RAM | DAX không hợp đọc strongly consistent hay ghi nhiều; buffer pool quá lớn trùng với page cache của OS | docs.aws.amazon.com DAX, postgresql.org, learn.microsoft.com SQL Server (Vừa) |
| **Không cache** | Tối ưu truy vấn, index, read replica, read model (CQRS) | Tải thấp, dữ liệu đổi liên tục, cần nhất quán mạnh | Không giảm được tải đọc khi đã tối đa hoá DB | `decision-framework.md` 2.7 (Chắc) |

**Cách chọn tầng:** bắt đầu ở tầng **gần người dùng nhất** mà dữ liệu cho phép (HTTP/CDN cho nội dung công khai, trong tiến trình cho dữ liệu nhỏ ít đổi), chỉ thêm cache phân tán khi nhiều instance cần chung; mỗi tầng thêm là một nơi có thể cũ. `Cache-Control: private` bắt buộc cho phản hồi cá nhân hoá ở tầng dùng chung (CDN, proxy) kẻo người khác thấy dữ liệu của bạn (MDN).

## 6. Lỗi hay gặp và cách tránh

- **Dữ liệu cũ gây lỗi nghiệp vụ.** Triệu chứng: sửa xong vẫn thấy bản cũ. Cách tránh: TTL ngắn + xoá key khi ghi; chỗ cần chặt thì write-through hoặc lease.
- **Stampede khi key nóng hết hạn.** Cách phát hiện: đỉnh query DB trùng thời điểm hết TTL. Cách tránh: gom request, lease, làm mới sớm theo xác suất, thêm jitter vào TTL.
- **Cache mất thì sập hệ thống.** Triệu chứng: node cache restart, DB cạn kết nối (xem Discord, Honeycomb ở mục 7). Cách tránh: test tải với cache tắt, circuit breaker bảo vệ DB, khởi động cache có làm nóng, giữ bản cũ qua hard TTL.
- **Cache chứa dữ liệu nhạy cảm không kiểm soát.** Cách tránh: key gồm cả chủ thể (user/tenant); không cache response có dữ liệu riêng ở tầng dùng chung (CDN).
- **Cache che lỗi thiết kế.** Cách tránh: đo và tối ưu truy vấn trước.

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **Facebook** (2013) | Memcache look-aside trước MySQL, đọc áp đảo ghi; gặp stale set, thundering herd, DB quá tải khi server cache chết | **Lease** (token gắn key, set bị từ chối nếu key đã bị xoá), mỗi key chỉ cấp token mỗi 10 giây, **gutter pool** hứng request khi server chết, daemon `mcsqueal` đọc log commit MySQL để gửi delete invalidation | Với nhóm key dễ herd, đỉnh query DB 17K/s → 1.3K/s; gutter giảm lỗi client thấy 99% (số theo paper) | [Scaling Memcache at Facebook, NSDI 2013](https://www.usenix.org/system/files/conference/nsdi13/nsdi13-final170_update.pdf) | Chắc (đã đọc bản PDF) |
| **Cloudflare** (2024) | Stampede khi object nổi tiếng hết hạn; cache lock thêm độ trễ và một dịch vụ có thể hỏng | Không khoá: gần hết hạn, mỗi request tự bốc thăm xem có làm mới sớm không, xác suất tăng dần | Giữ tải origin ở mức kiểm soát được mà không phối hợp; không đảm bảo đúng một request. Ghi chú: ví dụ số trong bài có chỗ không khớp phép tính, nên **không trích số** | [Sometimes I cache](https://blog.cloudflare.com/sometimes-i-cache/) | Chắc (đã mở) |
| **AWS Builders' Library** (2019) | Hướng dẫn cache cho dịch vụ quy mô lớn | Phân biệt inline vs side cache, on-box vs external, soft/hard TTL, gom request, negative caching | Cảnh báo "hành vi hai chế độ": hệ thống phụ thuộc cache có thể sập khi cache mất; nên load test khi cache tắt | [Caching challenges and strategies](https://builder.aws.com/content/3K9TeXp6strMiJ4vhakpRD7BQWO/caching-challenges-and-strategies) (J. Chhabra, 05/12/2019) | Chắc (đã mở) |
| **Netflix EVCache** | Lớp cache phân tán trên Memcached cho microservice trên AWS | Consistent hashing, mỗi zone một cluster, đọc ưu tiên zone cục bộ, ghi vào mọi zone, fallback sang zone khác | Chọn sẵn sàng và độ trễ thấp thay vì nhất quán mạnh | [EVCache wiki](https://github.com/netflix/evcache/wiki) | Vừa (agent nghiên cứu đã đọc, tôi chưa tự mở lại) |
| **Cloudflare Cache Reserve** (2022) | CDN dùng LRU nên nội dung ít truy cập bị đẩy ra, hit ratio thấp | Thêm tầng lưu bền (R2) làm tầng trên cùng của tiered cache | Bài đăng ở giai đoạn closed beta nên chưa có số đo | [Introducing Cache Reserve](https://blog.cloudflare.com/introducing-cache-reserve/) | Vừa (như trên) |

**Khi cache là nguyên nhân sự cố**

| Sự cố | Điều đã xảy ra | Bài học | Nguồn | Mức |
|---|---|---|---|---|
| **Discord, 12/02/2019** | Cụm Redis cache (cài đặt push notification, partial user object) giảm thông lượng ở 4 node; lượng request dồn xuống MongoDB làm nó quá tải, chập chờn; có bug ở circuit breaker và hai bug phía sau; khoảng 4,5 giờ từ báo cáo đầu đến lúc đóng | Cache hỏng thì tải đổ thẳng xuống DB, và cơ chế bảo vệ (circuit breaker) cũng có thể lỗi | [discordstatus.com/incidents/j13tlh8g4hkq](https://discordstatus.com/incidents/j13tlh8g4hkq) | Chắc (đã mở) |
| **GitHub, 09/02/2026** | Thay đổi cấu hình cache user settings gây ghi lại cache đồng thời hàng loạt, làm quá tải hạ tầng job nền và proxy Git HTTPS; hai đợt (87 và 76 phút, tổng 163 phút theo báo cáo); khắc phục: tắt ghi lại bất đồng bộ, giảm khuếch đại ghi, tự giới hạn tốc độ cập nhật hàng loạt | Đổi cấu hình cache cũng là thay đổi rủi ro: ghi lại cache hàng loạt là một dạng stampede | [GitHub availability report, 02/2026](https://github.blog/news-insights/company-news/github-availability-report-february-2026/) | Vừa (agent đã mở) |
| **Honeycomb, 25/07/2023** | Sau khi chuyển cluster, bản ghi "last seen" mà cache của ingest dựa vào ngừng được ghi; cache yếu dần rồi ngừng, tải đọc làm DB cạn connection, dính deadlock MySQL hiếm; toàn bộ thành phần người dùng sập khoảng 1 giờ 8 phút | Cache là một phần của đường chịu tải; cần cách làm nóng/làm mới cache có kiểm soát và bảo vệ DB | [Postmortem](https://www.honeycomb.io/blog/postmortem-rds-clogs-cache-refresh-crash-loops) | Vừa (agent đã đọc, tôi chưa tự mở lại) |

**Mô hình phổ biến (Chắc, theo AWS Builders' Library và ElastiCache docs):** cache-aside với TTL cho phần đông ứng dụng; cache ngoài dùng chung khi có nhiều instance; invalidation bằng TTL kèm xoá chủ động; giám sát hit ratio và chuẩn bị cho cold start.

**Chưa tìm được nguồn gốc đã mở cho:** Twitter (Twemcache/Pelikan), Instagram, và write-behind ở quy mô lớn; vì vậy ghi chú này không nêu các case đó.

## 8. Trong TeamBoard

- **Hiện chưa có cache** trong `src/`; `board-service` tải thấp. `decision-framework.md` mục 2.7 (`.agents/skills/system-design/references/decision-framework.md`): "đo trước khi cache".
- Pattern cache-aside + Decorator nằm ở `document/architecture/patterns-catalog.md` mục 2.5 và gợi ý bài tập ở mục 7; cache nằm trong lớp Infrastructure của Clean Architecture của `board-service` (ranh giới `.agents/rules/architecture.md`).
- Công cụ cache ngoài: chính sách phụ thuộc cấm Redis server, hướng đi là Valkey hoặc không cache khi chưa cần: xem [redis.md](redis.md).
- Rate limit ở gateway đang đếm trong bộ nhớ vì chỉ 1 instance (`document/design/api-gateway.md:153`).
- Cache che dữ liệu của service khác phải theo ranh giới dữ liệu: service chỉ cache dữ liệu **của mình** hoặc dữ liệu nhận qua event/API, không đọc DB người khác (`architecture.md`).

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Mức |
|---|---|---|---|---|
| 1 | `document/architecture/patterns-catalog.md` mục 2.5 | Nội bộ | cache-aside + Decorator trong dự án | Chắc |
| 2 | `.agents/skills/system-design/references/decision-framework.md` mục 2.7 | Nội bộ | khi nào nên/không nên cache | Chắc |
| 3 | [redis.md](redis.md) | Nội bộ | công cụ cache ngoài (Valkey) | Chắc |
| 4 | https://docs.aws.amazon.com/AmazonElastiCache/latest/dg/Strategies.html | Chính thức | lazy loading, write-through, TTL | Chắc (đã mở) |
| 5 | AWS Builders' Library, Facebook NSDI 2013, Cloudflare (mục 7) | Bài gốc | stampede, invalidation, tầng cache | Chắc (đã mở) |
| 6 | https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Caching | Chính thức | HTTP caching: private/shared, ETag, no-cache vs no-store | Chắc (đã mở) |
| 7 | https://docs.aws.amazon.com/AmazonElastiCache/latest/dg/SelectEngine.html | Chính thức | Memcached vs Valkey/Redis | Chắc (đã mở) |
| 8 | learn.microsoft.com: Overview of caching in ASP.NET Core, HybridCache | Chính thức | `IMemoryCache`, `IDistributedCache`, `HybridCache` | Vừa (agent đã mở) |
| 9 | RFC 9111 (HTTP Caching), RFC 5861 (stale-while-revalidate, stale-if-error) | Chuẩn | ngữ nghĩa cache HTTP | Vừa (agent đã mở) |

## 10. Tự kiểm tra / thực hành

1. Vì sao cache-aside chịu được việc node cache chết, còn hệ thống "luôn dựa vào cache" thì dễ sập? (Gợi ý: hành vi hai chế độ, Discord.)
2. Với `CachedBoardReader` ở mục 4: viết kịch bản test cho (a) hai lần gọi liên tiếp chỉ chạm DB một lần, (b) sau khi sửa Board thì lần đọc kế không trả bản cũ. Chỉ ra dòng nào cần thêm để (b) đạt.
3. Vì sao thêm jitter vào TTL giúp giảm stampede? So sánh với lease của Facebook: mỗi cách đánh đổi gì?
4. Việc chạy được: viết test xUnit cho (a) bằng `inner` là fake đếm số lần gọi; chạy `dotnet test` và ghi kết quả thật. Nếu dự án chưa có project test thì ghi "chưa chạy được" thay vì bịa.
