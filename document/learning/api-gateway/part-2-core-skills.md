# Part 2 — Core Skills (API Gateway với YARP)

> [!NOTE]
> **Mục tiêu:** dựng gateway bằng YARP: route, cluster, transform, header `X-Forwarded-*`, correlation id, và hiểu mã lỗi mặc định khi upstream hỏng.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** viết được cấu hình route và cluster, đoán đúng status khi upstream đóng cổng, treo, ngắt, giải thích vì sao `X-Forwarded-For` của client không tới upstream.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Các khối của YARP

#### Route
**Là gì:** quy tắc khớp request (path, host, method, header, query) và chỉ một cluster.
**Use case:** `/api/v1/boards/{**catch-all}` với `Methods: ["GET","HEAD"]` → cluster `board`.

#### Cluster và destination
**Là gì:** cluster là nhóm đích; mỗi destination có địa chỉ. Cluster mang chính sách cân bằng tải, health check, cấu hình HTTP.
**Use case:** cluster `board` có một destination `http://board-service:7070/`.

#### Transform
**Là gì:** biến đổi request hay response trên đường đi (đổi path, thêm hay bớt header, đổi method).
**Use case:** bỏ tiền tố `/legacy` trước khi chuyển; thêm header `X-Gateway`.

#### Middleware của ASP.NET Core
YARP chạy trong pipeline ASP.NET Core nên mọi middleware (CORS, rate limit, xác thực, correlation) cắm vào được trước `MapReverseProxy()`.

## L2 Why — Vì sao cấu hình theo route và cluster

Tách "khớp request" khỏi "gửi tới đâu" cho phép đổi địa chỉ upstream, thêm instance hay đổi chính sách mà không đụng route; cấu hình nạp lại khi file đổi mà không cần khởi động lại. Transform và middleware cho phép giữ gateway mỏng nhưng vẫn tuỳ biến bằng C#.

## L3 How — Cơ chế đáng nhớ

#### Cấu hình tối thiểu
```csharp
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapReverseProxy();
```
```json
"ReverseProxy": {
  "Routes": { "boards-read": { "ClusterId": "board",
      "Match": { "Path": "/api/v1/boards/{**catch-all}", "Methods": ["GET", "HEAD"] } } },
  "Clusters": { "board": { "Destinations": { "d1": { "Address": "http://board-service:7070/" } } } }
}
```
Route cần `RouteId`, `ClusterId` và `Match` (có `Path` hoặc `Hosts`). Route cụ thể nhất thắng; `Order` nhỏ hơn thắng. Có thể dựng bằng code với `LoadFromMemory(routes, clusters)` (các lab dùng cách này).

#### Mặc định của mỗi route
Theo tài liệu YARP và Lab 2.1: Host của upstream lấy từ địa chỉ destination (không giữ Host của client); `X-Forwarded-For`, `-Proto`, `-Host`, `-Prefix` được **đặt lại** từ kết nối thật; mọi header khác được sao chép. Với `Set`, header client gửi bị xoá để chống giả mạo.

#### Các transform thường dùng
`PathPrefix`, `PathRemovePrefix` (khớp theo ranh giới đoạn nên `/prefix2` không bị đụng), `PathSet`, `PathPattern`; `RequestHeader` (Set hay Append), `RequestHeaderRemove`, `RequestHeadersAllowed` (danh sách cho phép); `X-Forwarded` (Set, Append, Remove, Off). Transform áp theo thứ tự khai báo và chỉ đổi request gửi đi, không đổi request gốc; không có transform thân request (cần middleware).

#### Mã lỗi mặc định khi upstream hỏng
Theo mã nguồn `HttpForwarder` (agent đọc) và Lab 2.2:

| Tình huống | Status YARP | Ghi chú |
|---|---|---|
| connect lỗi, từ chối kết nối, DNS | 502 | lab: 2,1 giây cho cổng đóng trên Windows |
| upstream ngắt trước khi trả header | 502 | |
| `ActivityTimeout` của cluster hết | 504 | mặc định 100 giây |
| `Timeout` của route hết | 400 | YARP coi là request bị huỷ; gateway tự đổi sang 504 nếu muốn |
| upstream trả 500, 503... | giữ nguyên | YARP không đổi mã của upstream |
| header đã gửi rồi upstream ngắt | không đổi được status | client thấy kết nối bị đóng |

Lỗi nằm trong `IForwarderErrorFeature` (có `.Error` và `.Exception`); một middleware sau YARP đọc nó để đổi sang `problem+json` (Part 6).

#### Route có `Timeout` cần middleware riêng
`Timeout` của route cần `AddRequestTimeouts()` và `UseRequestTimeouts()`; thiếu thì request tới route đó lỗi 500 với thông báo yêu cầu gọi `UseRequestTimeouts()` (Lab 2.2). Giá trị `ActivityTimeout` mặc định 100 giây; `ConnectTimeout` mặc định 15 giây.

#### Method không khớp
Route chỉ cho `GET, HEAD` mà client gửi `POST` thì ASP.NET Core trả **405**, không phải 404 (Lab 2.1). Muốn 404 `ROUTE_NOT_FOUND` như design TB-007 thì cần route hoặc fallback bắt phần còn lại (Part 6).

#### Correlation id và `traceparent`
Gateway nhận `X-Correlation-Id` nếu hợp lệ, ngược lại tạo mới, **ghi đè** header gửi xuống upstream và đặt vào response (middleware tự viết, Lab 2.3). `traceparent` của client được chuyển nguyên nếu có; khi không có `Activity` nào thì YARP **không tự tạo** (Lab 2.3), cần OpenTelemetry hoặc `ActivitySource` đăng ký để có trace.

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0. Upstream giả in ra những gì nó nhận được.

#### Lab 2.1 — Route, transform, `X-Forwarded-*`
**Đầu ra thật** (rút gọn, JSON của upstream):
```text
GET /api/v1/boards/1?x=1
   -> 200 | path=/api/v1/boards/1?x=1 host=127.0.0.1:18501 xff=127.0.0.1 xfp=http xfh=127.0.0.1:18500
GET /api/v1/boards/1  kèm X-Forwarded-For: 9.9.9.9 (client giả)
   -> 200 | xff=127.0.0.1                  (giá trị giả bị thay)
GET /legacy/boards/2  (PathRemovePrefix + RequestHeader X-Gateway=lab)
   -> 200 | path=/boards/2  X-Gateway=lab
GET /khong-co-route                 -> 404 | (thân rỗng)
POST /api/v1/boards/1 (route chỉ GET, HEAD)  -> 405 | (thân rỗng)
```
**Giải thích:** Host upstream là địa chỉ destination; `X-Forwarded-Host` giữ Host của client; `X-Forwarded-For` bị thay bằng IP kết nối thật (127.0.0.1) nên giá trị 9.9.9.9 không lọt xuống upstream; POST trên route GET-only ra 405, không phải 404.

#### Lab 2.2 — Mã lỗi khi upstream hỏng
**Đầu ra thật:**
```text
Cổng upstream đóng (connection refused)            -> 502, thân rỗng, 2124 ms
Upstream trả 500                                   -> 500, thân của upstream, 17 ms
Upstream ngắt trước khi gửi header                 -> 502, thân rỗng, 2 ms
Route Timeout 1 giây, upstream chậm 3 giây         -> 400, thân rỗng, 1006 ms
ActivityTimeout 1 giây (cluster), upstream chậm    -> 504, thân rỗng, 1010 ms
Upstream đã gửi header rồi ngắt giữa chừng         -> client: HttpRequestException (kết nối bị đóng), 520 ms
```
Khi **chưa** gọi `AddRequestTimeouts`/`UseRequestTimeouts`, route có `Timeout` trả `500` kèm thông báo `The timeout was not applied for route 't1', ensure IApplicationBuilder.UseRequestTimeouts() is called ...` (ở môi trường Development, thân là trang lỗi).
**Giải thích:** hai kiểu timeout cho hai mã khác nhau (400 và 504); lỗi sau khi đã gửi header không có status mới. Design TB-007 ghi mọi timeout trước header là `504 UPSTREAM_TIMEOUT`: cần middleware đổi mã (Lab 6.1).

#### Lab 2.3 — Correlation id và `traceparent`
**Đầu ra thật:**
```text
không gửi header                    -> response X-Correlation-Id: <GUID mới>; upstream thấy corr=<cùng GUID>, traceparent=-
gửi X-Correlation-Id: abc-123       -> response và upstream: abc-123
gửi id có ký tự lạ: a<x>b           -> bị thay bằng GUID mới (không lọt xuống upstream)
gửi traceparent của client          -> upstream thấy traceparent nguyên văn của client
```
**Giải thích:** kiểm định dạng (chữ, số, `.`, `_`, `-`, tối đa 64 ký tự) chặn log injection; khi client không gửi `traceparent` và không có Activity, upstream không có trace.

## L5 Troubleshooting — Khi gateway không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| 404 thân rỗng cho mọi request | không route nào khớp (path, host, method) | xem `Match` của route và `Order` | sửa path, thêm `{**catch-all}` |
| 405 thay vì 404 | route có `Methods` hạn chế | thử bằng đúng method | fallback route nếu muốn 404 |
| 502 sau vài giây | upstream đóng cổng hoặc DNS sai | gọi thẳng upstream, xem `IForwarderErrorFeature` | kiểm địa chỉ cluster, mạng, container |
| 504 sau đúng N giây | `ActivityTimeout` hết (mặc định 100 giây) | so thời gian với cấu hình | rút hoặc kéo dài; sửa upstream chậm |
| 400 sau đúng N giây, không giải thích | `Timeout` của route hết | xem route có `Timeout` | đổi sang 504 bằng middleware |
| 500 "timeout was not applied" | thiếu `UseRequestTimeouts()` | đọc thông báo lỗi | `AddRequestTimeouts()` và `UseRequestTimeouts()` trước `MapReverseProxy()` |
| Upstream thấy sai IP client | tin header giả hoặc proxy phía trước | xem `X-Forwarded-For` ở upstream | `ForwardedHeaders` đúng `KnownProxies` (Part 4) |
| Upstream nhận path sai | thiếu `PathRemovePrefix` hay `PathPrefix` | in path ở upstream | thêm transform |
| Client thấy kết nối đóng giữa chừng | upstream ngắt sau khi đã gửi header | log upstream | không đổi được status; log và metric |

## Tự kiểm tra

1. YARP trả status nào khi cổng upstream đóng? Khi upstream chậm quá `ActivityTimeout`? Khi route `Timeout` hết?
2. Vì sao `X-Forwarded-For` giả của client không tới upstream?
3. Route chỉ cho `GET` nhận `POST`: status gì, và sửa thành 404 thế nào?
4. Vì sao `traceparent` có thể vắng ở upstream dù client không gửi?

<details>
<summary>Đáp án ngắn</summary>

1. 502; 504; 400 (Lab 2.2).
2. Transform mặc định đặt lại `X-Forwarded-For` từ kết nối thật (Lab 2.1).
3. 405; thêm fallback route.
4. Không có `Activity` nào để YARP truyền; cần OpenTelemetry (Lab 2.3).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.3 (YARP 2.3.0, đầu ra thật).
**Đã tự mở:** YARP "Destination health checks" (cho Part 3); các khối config và transform lấy từ tài liệu YARP do agent đọc.
**Chỉ qua agent (⚠):** learn.microsoft.com YARP: `config-files`, `transforms`, `transforms-request`, `timeouts`, `http-client-config`, `direct-forwarding`; bảng ánh xạ `ForwarderError` sang status đọc từ `HttpForwarder.cs` qua bộ tóm tắt (không phải trích dẫn nguyên văn).
**Chưa kiểm:** `Forwarded` (RFC 7239), `ResponseHeader` transform, header routing; hành vi 2,1 giây khi cổng đóng có thể chỉ có trên Windows; `traceparent` khi có OpenTelemetry.
