# Part 2 — Core Skills (Caching)

> [!NOTE]
> **Mục tiêu:** đặt cache đúng ở hai tầng rẻ nhất: HTTP (trình duyệt, proxy) và trong tiến trình (`IMemoryCache`), bọc cache quanh code bằng Decorator, thiết kế key và TTL, biết output cache phía server.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** đặt đúng header cho 4 loại nội dung, viết được Decorator cache-aside, giải thích vì sao `no-cache` khác `no-store`.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Hai tầng cache gần nhất

#### HTTP caching
**Là gì:** server gắn header vào response để **trình duyệt và proxy/CDN** quyết định giữ bản sao bao lâu và có cần hỏi lại không. Quy tắc chung nằm ở RFC 9111 ⚠.
**Use case:** file JS/CSS có hash trong tên giữ một năm; trang HTML luôn hỏi lại; trang tài khoản không bao giờ lưu.

#### Cache trong tiến trình (`IMemoryCache`)
**Là gì:** từ điển trong RAM của ứng dụng, có hết hạn và giới hạn kích thước.
**Use case:** kết quả tra cứu nhỏ, ít đổi, một instance (danh sách quốc gia, cấu hình, Board vừa đọc).

#### Decorator cache
**Là gì:** lớp bọc cùng interface với repository thật; kiểm cache trước, miss thì gọi lớp thật rồi ghi cache. Code nghiệp vụ không biết có cache.
**Use case:** thêm cache cho `IBoardReader` mà không sửa handler hay repository.

#### Output caching (ASP.NET Core)
**Là gì:** middleware lưu **toàn bộ response** của endpoint ở server và trả lại cho yêu cầu khớp, kèm tag để xoá theo nhóm.
**Use case:** endpoint danh sách công khai đắt tính toán, nhiều người gọi giống nhau.

## L2 Why — Vì sao bắt đầu từ tầng gần người dùng

Mỗi tầng xa hơn thêm một chặng mạng và một thành phần phải vận hành. Header HTTP **không tốn thêm hạ tầng** và có thể tránh luôn cả yêu cầu đến server; `IMemoryCache` không qua mạng. Chỉ khi nhiều instance cần chung cache mới đi tiếp ở [Part 3](part-3-multi-component.md).

Decorator giữ cache tách khỏi nghiệp vụ: bỏ hoặc đổi cache chỉ sửa một chỗ đăng ký DI, test nghiệp vụ không cần cache.

## L3 How — Cơ chế đáng nhớ

### HTTP caching

Hai pha: **tươi** (còn hạn, dùng ngay, không hỏi server) và **kiểm tra lại** (hết hạn, hỏi server "bản của tôi còn đúng không?").

```
GET /boards/1 ─► 200 + ETag "v1" + max-age=30   (client lưu)
... trong 30 giây: dùng thẳng bản đã lưu, KHÔNG có yêu cầu mạng
... sau 30 giây:   GET + If-None-Match: "v1"
      server: bản vẫn v1 ─► 304, thân rỗng (client dùng lại bản cũ)
      server: đã v2     ─► 200 + ETag "v2" + thân mới
```

#### `max-age` và `s-maxage`
**Là gì:** số giây còn tươi; `s-maxage` chỉ áp cho cache dùng chung (CDN, proxy).
**Use case:** `max-age=60` cho dữ liệu hơi cũ vẫn chấp nhận được; `s-maxage` dài hơn cho CDN để giảm tải server gốc.

#### `ETag` và `If-None-Match` (validator)
**Là gì:** `ETag` là dấu vân tay của bản response; client gửi lại qua `If-None-Match`, server trả `304` nếu trùng.
**Use case:** tài nguyên lớn đổi hiếm: hết hạn thì chỉ tốn một chuyến hỏi nhỏ thay vì tải lại.
**Lưu ý:** server **phải tự so sánh** ETag; chỉ đặt header `ETag` không tạo ra 304 (Lab 2.1, `/index`).

#### `private` và `public`
**`private`:** chỉ cache của người dùng (trình duyệt) được lưu; **`public`:** proxy/CDN cũng được lưu.
**Use case:** dữ liệu theo người dùng (danh sách Board của tôi) phải `private`; nội dung giống nhau với mọi người mới `public`.

#### `no-cache` và `no-store`
Tên dễ gây hiểu sai. **`no-cache`:** được lưu nhưng **phải kiểm tra lại** trước khi dùng. **`no-store`:** **không được lưu** ở đâu cả.
**Use case:** `no-cache` cho trang HTML chính (luôn mới, vẫn tận dụng 304); `no-store` cho dữ liệu nhạy cảm (trang tài khoản).

#### `immutable`
**Là gì:** báo tài nguyên không bao giờ đổi trong thời gian tươi, trình duyệt không cần kiểm tra lại kể cả khi người dùng tải lại.
**Use case:** file có hash trong tên (`app.abc123.js`): đổi nội dung thì đổi tên, nên giữ cả năm an toàn.

#### `Vary`
**Là gì:** liệt kê header yêu cầu làm **thay đổi response**; cache dùng chúng làm một phần của khoá.
**Use case:** `Vary: Accept-Language` để người dùng `vi` và `en` không lấy nhầm bản của nhau. Thiếu `Vary` khi nội dung thật sự phụ thuộc header là nguồn lỗi và rò dữ liệu (Part 5).

### `IMemoryCache`

#### Hết hạn tuyệt đối và trượt
Tuyệt đối: hạn cứng từ lúc tạo. Trượt: gia hạn mỗi lần đọc, **không bao giờ hết nếu luôn có người đọc**. Kết hợp cả hai để có trần (Lab 1.2).

#### `SizeLimit`
**Là gì:** khi đặt `SizeLimit`, **mỗi mục phải khai `Size`**; tổng vượt giới hạn thì mục mới bị **từ chối** (không bị ghi) thay vì đẩy mục cũ ra. Đúng như quan sát ở Lab 2.2.
**Use case:** chặn cache trong RAM phình vô hạn do key sinh ra từ đầu vào người dùng.
**Cái giá:** cache có thể "đầy cứng" và ngừng nhận mục mới; cần TTL để dọn.

### Thiết kế key
- Key phải chứa **mọi thứ làm kết quả khác đi**: `board:{id}`, `boards:user:{userId}:page:{n}`. Thiếu một thành phần (ví dụ `userId`) là trả nhầm dữ liệu người này cho người kia.
- Có tiền tố loại và phiên bản schema (`v2:board:{id}`) để đổi cấu trúc dữ liệu không đọc nhầm bản cũ.
- Đừng nhét đầu vào tự do của người dùng vào key khi không giới hạn: key vô hạn làm cache phình.

### TTL, và jitter
Nạp hàng loạt cùng lúc với **cùng TTL** thì cùng hết hạn cùng lúc, cả đàn đổ về nguồn một lượt. **Jitter** cộng ngẫu nhiên vào TTL (±10–20%) để trải đều (Lab 2.4).

### Negative caching
Cache cả kết quả "không có" kèm TTL ngắn để id không tồn tại không đổ hết xuống nguồn (Lab 1.3). TTL ngắn vì bản ghi có thể được tạo sau đó.

### Output caching
Lưu response theo **policy** (thời hạn, tag, biến theo query/header). Có **khoá chống stampede**: nhiều yêu cầu đồng thời cho cùng key chỉ chạy handler một lần (Lab 2.5). Mặc định **không lưu yêu cầu có `Authorization`** (Lab 2.5), tránh trả response của người này cho người khác.

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0; server Kestrel tự dựng trong tiến trình lab, gọi bằng `HttpClient`, chỉ bind `127.0.0.1`; không lưu gì ngoài thư mục thử.

#### Lab 2.1 — Header HTTP caching
Các endpoint: `/boards/1` (`private, max-age=30`, ETag, có so sánh `If-None-Match`), `/assets/app.abc123.js`, `/index` (`no-cache`, ETag nhưng **không** so sánh), `/account` (`no-store`), `/greeting` (Vary).
**Đầu ra thật:**
```text
GET /boards/1
   200 OK | Cache-Control: max-age=30, private | ETag: "v1" | body: board 1 version 1
GET /boards/1 + If-None-Match: "v1"
   304 NotModified | Cache-Control: max-age=30, private | ETag: "v1" | body: (rỗng)
sau khi dữ liệu đổi (v2), gửi lại If-None-Match: "v1"
   200 OK | Cache-Control: max-age=30, private | ETag: "v2" | body: board 1 version 2
GET /assets/app.abc123.js
   200 OK | Cache-Control: public, max-age=31536000, immutable | body: console.log('app')
GET /index + If-None-Match: "idx1"
   200 OK | Cache-Control: no-cache | ETag: "idx1" | body: <h1>index</h1>
GET /account
   200 OK | Cache-Control: no-store | body: sensitive
GET /greeting (Accept-Language: vi)
   200 OK | Cache-Control: public, max-age=60 | Vary: Accept-Language | body: xin chào
GET /greeting (Accept-Language: en)
   200 OK | Cache-Control: public, max-age=60 | Vary: Accept-Language | body: hello
```
**Giải thích:** đúng ETag thì `304` thân rỗng; đổi dữ liệu thì `200` với ETag mới. `/index` có ETag nhưng handler không so sánh nên vẫn `200`: header một mình không tạo 304. Hai ngôn ngữ cho hai thân khác nhau dưới cùng URL, nên `Vary` là bắt buộc để cache dùng chung không lẫn.

**Giới hạn:** lab gọi bằng `HttpClient` không có cache của trình duyệt, nên chỉ chứng minh **header server gửi** và hành vi 304; hành vi lưu trữ thật của trình duyệt/CDN theo RFC 9111 ⚠ chưa quan sát.

#### Lab 2.2 — `SizeLimit` từ chối mục thay vì đẩy mục cũ
`SizeLimit = 5`, thêm 8 mục `Size = 1`.
**Đầu ra thật:**
```text
sau khi thêm 8 mục (Size=1, SizeLimit=5): còn 5 mục: k1,k2,k3,k4,k5
sau 300 ms và thêm k9..k12: còn 5 mục: k1,k2,k3,k4,k5
```
**Giải thích:** k6 đến k8 **không được ghi**, và k9 đến k12 cũng không vào vì chỗ chưa trống. Trong lần chạy này không thấy dọn tự động mục cũ khi đầy; muốn cache thoáng phải đặt TTL ngắn hoặc dọn chủ động.

#### Lab 2.3 — Decorator cache-aside
```csharp
sealed class CachedBoardReader(IBoardReader inner, IMemoryCache cache) : IBoardReader
{
    private static readonly MemoryCacheEntryOptions Options =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(300) };

    public async Task<Board?> GetAsync(Guid id, CancellationToken ct)
    {
        var key = $"board:{id}";
        if (cache.TryGetValue(key, out Board? hit)) return hit;
        var value = await inner.GetAsync(id, ct);
        if (value is not null) cache.Set(key, value, Options);
        return value;
    }
}
```
**Đầu ra thật** (nguồn giả mất 20 ms mỗi lần):
```text
100 lượt đọc trên 5 board: chạm nguồn 5 lần
sau khi hết TTL, đọc lại 5 board: chạm nguồn tổng 10 lần
```
**Giải thích:** 5 board đọc 20 vòng chỉ chạm nguồn 5 lần (mỗi board một lần miss); hết TTL thì 5 board miss lại. Lưu ý code này **không chống stampede**: hai luồng cùng miss đều gọi nguồn (Part 4).

#### Lab 2.4 — TTL đồng loạt so với TTL có jitter (mô phỏng)
1000 key được nạp cùng lúc, TTL 20 giây, mô phỏng 120 giây; đếm số lần nạp lại mỗi giây (lần nạp đầu tiên tính, giai đoạn khởi động lạnh loại ra).
**Đầu ra thật:**
```text
jitter ±  0%: đỉnh 1000 lần nạp lại/giây, trung bình 41.7, số giây có nạp lại > 100: 5
jitter ± 20%: đỉnh  139 lần nạp lại/giây, trung bình 46.0, số giây có nạp lại > 100: 12
```
**Giải thích:** không jitter, cả 1000 key hết hạn trong cùng một giây (đỉnh 1000/s). Có jitter ±20% đỉnh chỉ còn 139/s dù tổng tải trung bình tương đương. Đây là **mô hình tự viết**, không đo hệ thống thật.

#### Lab 2.5 — Output caching: tag, thời hạn, khoá, `Authorization`
Policy `board`: hết hạn 3 giây, tag `boards`; handler mất 200 ms.
**Đầu ra thật:**
```text
GET /output/boards (lần 1)                  200 | body: danh sách board, lần tính thứ 1
GET /output/boards (lần 2, trong 3 giây)    200 | Age: 0 | body: lần tính thứ 1   (handler chạy 1 lần)
sau POST update (evict tag):                200 | body: lần tính thứ 2             (handler chạy 2 lần)
sau 3,5 giây (hết hạn 3 giây):              200 | body: lần tính thứ 3             (handler chạy 3 lần)
20 yêu cầu đồng thời, /output/boards (có khoá):      handler chạy 1 lần
20 yêu cầu đồng thời, /output/nolock (SetLocking(false)): handler chạy 20 lần
3 yêu cầu kèm Authorization:  handler chạy 3 lần (không được lưu)
3 yêu cầu ẩn danh:            handler chạy 1 lần (lần 2, 3 có Age: 0)
```
**Giải thích:** lần hit có header `Age`. Evict theo tag hoặc hết hạn thì tính lại. Khoá mặc định gom 20 yêu cầu thành 1 lần chạy. Yêu cầu có `Authorization` bị bỏ qua để tránh trả dữ liệu của người này cho người khác; endpoint theo người dùng cần policy riêng và key có `userId` nếu muốn cache.

## L5 Troubleshooting — Khi cache không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Có `ETag` mà không bao giờ thấy 304 | handler không so sánh `If-None-Match` | gửi `If-None-Match` bằng `curl -i`, xem mã trả về | so sánh ETag trong code hoặc dùng middleware hỗ trợ |
| Người dùng thấy dữ liệu của người khác | key hoặc `Vary` thiếu thành phần phân biệt; `public` cho dữ liệu riêng | xem `Cache-Control`, `Vary` và key | thêm `userId` vào key, dùng `private`; xem Part 5 |
| Sửa dữ liệu mà trang vẫn cũ | `max-age` dài, không có chiến lược xoá; hoặc output cache chưa evict | so thời điểm ghi với `Age` và `max-age` | rút ngắn TTL, dùng `no-cache` cho HTML, evict theo tag khi ghi |
| Bộ nhớ ứng dụng tăng mãi | không có `SizeLimit` hoặc TTL, key theo đầu vào tự do | đếm số mục, xem thống kê của cache | `SizeLimit` + `Size`, TTL, giới hạn dạng key |
| Cache đầy nhưng mục mới không vào | `SizeLimit` từ chối mục mới (Lab 2.2) | xem số mục hiện có và `Size` | TTL ngắn hơn, tăng giới hạn, dọn chủ động |
| Tải DB tăng vọt đúng lúc hết hạn | nhiều key cùng TTL, hoặc nhiều luồng cùng miss một key | xem đồ thị lần nạp theo giây | jitter (Lab 2.4), gom request (Part 4) |
| Output cache không hoạt động với người đã đăng nhập | yêu cầu có `Authorization` bị bỏ qua (Lab 2.5) | gửi cùng yêu cầu có và không có header | policy riêng có key theo người dùng, hoặc cache ở tầng dữ liệu |

## Tự kiểm tra

1. Chọn header cho: (a) `app.8f3a.js`, (b) trang HTML chính, (c) trang tài khoản, (d) danh sách Board của người dùng.
2. `no-cache` khác `no-store` thế nào?
3. Vì sao có `ETag` chưa chắc có 304?
4. Vì sao nên thêm jitter vào TTL?

<details>
<summary>Đáp án ngắn</summary>

1. (a) `public, max-age=31536000, immutable`; (b) `no-cache` + ETag; (c) `no-store`; (d) `private` với `max-age` ngắn, key/`Vary` theo người dùng.
2. `no-cache` lưu được nhưng phải kiểm tra lại; `no-store` không được lưu.
3. Server phải tự so sánh `If-None-Match` và trả 304 (Lab 2.1, `/index`).
4. Tránh cả đàn key hết hạn cùng lúc rồi cùng đổ về nguồn (Lab 2.4).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.5 (đầu ra thật, môi trường ở Part 0). Lab 2.4 là mô hình tự viết.
**Đã tự mở:** MDN "HTTP caching" (freshness, validation, `no-cache` so với `no-store`, `Vary`, `immutable`).
**Chỉ qua agent (⚠):** RFC 9111 (HTTP Caching); tài liệu Microsoft Learn về `IMemoryCache` (`SizeLimit`, `Size`, hết hạn trượt), Output caching và Response caching middleware.
**Chưa kiểm:** hành vi của trình duyệt và CDN thật với các header trên; lab chỉ chứng minh header phía server và logic 304.
