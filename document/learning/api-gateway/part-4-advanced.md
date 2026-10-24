# Part 4 — Advanced (API Gateway)

> [!NOTE]
> **Mục tiêu:** bảo vệ hệ thống ở biên: rate limit đúng, timeout và retry không nhân tải, xác thực JWT ở gateway, và không để `X-Forwarded-For` giả làm mất tác dụng giới hạn.
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chọn thuật toán rate limit và khoá phân vùng, giải thích vì sao retry nhiều tầng nguy hiểm, cấu hình xác thực JWT cho một route, cấu hình `ForwardedHeaders` an toàn.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Bốn lớp bảo vệ

#### Rate limit
Giới hạn số yêu cầu mỗi khoảng thời gian theo một khoá (IP, người dùng, API key).
**Use case:** đọc Board tối đa 120 lần/phút/IP, ghi 30 lần/phút/IP.

#### Timeout, retry, circuit breaker
Timeout: giới hạn thời gian chờ. Retry: thử lại có kiểm soát. Circuit breaker: ngắt nhanh khi upstream lỗi liên tục.
**Use case:** upstream treo thì không giữ kết nối mãi; lỗi thoáng qua của một yêu cầu `GET` được thử lại.

#### Xác thực ở gateway (JWT)
Gateway kiểm chữ ký, hạn, người cấp và đối tượng của token trước khi chuyển tiếp.
**Use case:** mọi route `/api/v1/**` yêu cầu đăng nhập, service phía sau không phải kiểm lại từ đầu.

#### Xác định IP client sau proxy
`X-Forwarded-For` do client hoặc proxy ghi; chỉ tin khi nó đến từ proxy đã biết.
**Use case:** rate limit theo IP thật của người dùng sau ALB.

## L2 Why — Vì sao bốn việc này dễ sai

- Rate limit sai khoá thì chặn nhầm người dùng thật hoặc để kẻ tấn công né.
- Mỗi tầng đều retry thì tải nhân lên đúng lúc hệ thống yếu nhất.
- Xác thực lỏng ở biên là cửa vào của mọi service phía sau.
- IP tin sai làm rate limit và log sai.

## L3 How — Cơ chế đáng nhớ

### Rate limit trong YARP
YARP dùng middleware `RateLimiter` của ASP.NET Core: định nghĩa policy trong `AddRateLimiter`, gán cho route bằng khoá `RateLimiterPolicy` (`disable` bỏ qua cả limiter toàn cục), gọi `app.UseRateLimiter()` **trước** `MapReverseProxy()`. Cần .NET 7 trở lên. `GlobalLimiter` áp cho mọi route không cần gán.

#### Cửa sổ cố định
Đếm trong từng cửa sổ thời gian, reset khi hết cửa sổ. Đơn giản nhất. **Nhược:** quanh ranh giới cửa sổ cho qua gần **gấp đôi** giới hạn (Lab 4.1).

#### Cửa sổ trượt
Chia cửa sổ thành các đoạn nhỏ, tính tổng các đoạn còn trong cửa sổ trượt; mượt hơn ở ranh giới (Lab 4.1). Cloudflare dùng một xấp xỉ khác: lấy số đếm của cửa sổ trước nhân tỉ lệ phần còn chồng, cộng số đếm cửa sổ hiện tại (ví dụ 42 × 0,75 + 18 = 49,5 so với giới hạn 50), đo trên 400 triệu yêu cầu thì sai 0,003%.

#### Token bucket
Nạp token đều, cho bùng nổ có kiểm soát; hợp khi muốn cho phép đỉnh ngắn có chủ đích. Cũng có sẵn trong RateLimiter (`AddTokenBucketLimiter`) ⚠ chưa chạy.

#### Khoá phân vùng và mã trả về
Policy phải **phân vùng** (ví dụ theo IP): policy đăng ký theo tên không phân vùng sẽ là một bộ đếm chung cho mọi người. Mặc định `RejectionStatusCode` không phải 429: đặt `429` và `OnRejected` để ghi `Retry-After`. Lab 4.1: cửa sổ cố định có metadata `RetryAfter` (giá trị `1`), cửa sổ trượt **không** có (không có header); khi thiếu metadata, đặt giá trị từ độ dài cửa sổ.

#### Đếm cục bộ so với tập trung
Đếm trong bộ nhớ từng instance rẻ nhưng lỏng hơn N lần khi chạy N instance và mất khi khởi động lại; đếm tập trung (Redis/Valkey, [lộ trình Valkey](../valkey/README.md)) chính xác hơn nhưng thêm thành phần. Design TB-007 chọn bộ nhớ vì một instance.

#### Rate limit không thay chống DDoS
Chống DDoS cần WAF/CDN ở trước; rate limit ở gateway chỉ là một lớp.

### Timeout, retry, circuit breaker
- **Timeout:** `ActivityTimeout` của cluster (100 giây mặc định) ngắt khi upstream im lặng; `Timeout` của route giới hạn tổng thời gian (cần `UseRequestTimeouts`). Mã trả về xem Part 2.
- **Retry:** YARP **không retry** mặc định (Lab 4.3). Retry an toàn chỉ cho request **idempotent** (GET); gateway không biết request nào idempotent nếu không được cấu hình.
- **Nhân retry nhiều tầng:** nếu gateway và từng service đều retry thì số lần thử nhân lên (Lab 4.3: 3 tầng × 3 lần = 27; 3 tầng × 4 lần = 64, đúng ví dụ sách Google SRE). AWS Builders' Library: ngăn xếp 5 tầng, 3 lần mỗi tầng là 243 lần (ví dụ minh hoạ). Khuyến nghị: retry **một chỗ** trong ngăn xếp, backoff có jitter, giới hạn bằng ngân sách retry.
- **Circuit breaker:** YARP không dựng sẵn; health check thụ động với `ReactivationPeriod` hoạt động như một breaker thô. Muốn breaker thật phải thêm thư viện (Polly) hoặc cấu hình proxy khác (Envoy có breaker và outlier detection dựng sẵn) ⚠ chưa chạy.

### JWT ở gateway
`AddAuthentication().AddJwtBearer(...)` kiểm chữ ký, issuer, audience, hạn (`ClockSkew` mặc định 5 phút; lab đặt 0 để kiểm hết hạn chính xác); route dùng khoá `AuthorizationPolicy` (`default` yêu cầu đã xác thực, `anonymous` cho qua); không đặt khoá thì áp `FallbackPolicy` của ASP.NET Core, mặc định **không** có xác thực. Cần `UseAuthentication()` và `UseAuthorization()` trước `MapReverseProxy()`. Token Bearer được chuyển xuống upstream mặc định. Lab 4.2.

### `X-Forwarded-For` và `ForwardedHeaders`
- Mặc định YARP đặt `X-Forwarded-For` từ `RemoteIpAddress` của kết nối (Part 2). Nếu có `UseForwardedHeaders()` trước đó, `RemoteIpAddress` đã bị thay bằng IP trong header **nếu proxy gửi đến được tin**.
- Mặc định `ForwardedHeaders` chỉ tin loopback; `ForwardLimit` mặc định 1 (chỉ lấy giá trị ngoài cùng bên phải).
- **Nguy hiểm:** đặt `ForwardLimit = null` và xoá `KnownProxies`/`KnownNetworks` (hoặc biến môi trường `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` thường có sẵn trong image) là tin mọi proxy: kẻ tấn công đổi IP mỗi yêu cầu để né rate limit (Lab 4.4), và IP giả còn được chuyển xuống upstream.

### Session affinity ⚠ chưa chạy
Cluster có thể gắn yêu cầu của một client vào cùng một destination (`SessionAffinity`: `Policy` `HashCookie` mặc định, `FailurePolicy` `Redistribute` hay `Return503Error`, `AffinityKeyName` bắt buộc và duy nhất giữa các cluster). Hợp khi upstream giữ trạng thái phiên; mỗi bản YARP với `Cookie`/`CustomHeader` cần chia sẻ khoá Data Protection. Không thay thế cho thiết kế service không trạng thái.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0.

#### Lab 4.1 — Rate limit 5 yêu cầu mỗi giây: cố định và trượt
```text
-- cố định
   8 yêu cầu liên tiếp: 200 200 200 200 200 429 429 429   (Retry-After của 429: 1)
   1 yêu cầu lúc t=0, 4 lúc t=0,9 s, 5 lúc t=1,05 s: được qua 10/10
-- trượt (4 đoạn)
   8 yêu cầu liên tiếp: 200 200 200 200 200 429 429 429   (Retry-After của 429: không có)
   cùng chuỗi thời gian: được qua 5/10
```
**Giải thích:** cả hai chặn đúng khi dồn liên tiếp; ở ranh giới cửa sổ, cửa sổ cố định cho qua 10 yêu cầu (9 yêu cầu trong khoảng 0,15 giây), cửa sổ trượt chỉ cho 5. Phụ thuộc thời gian thực; chạy lại có thể lệch vài chục ms.

#### Lab 4.2 — JWT ở gateway (khoá ký ngẫu nhiên tạo trong lab)
```text
route /secure, không có token       -> 401 | WWW-Authenticate: Bearer                 | upstream không thấy yêu cầu
route /secure, token hợp lệ         -> 200 | upstream thấy Authorization: có
route /secure, token hết hạn        -> 401 | Bearer error="invalid_token", ... The token expired at ...
route /secure, chữ ký sai           -> 401 | Bearer error="invalid_token", ... The signature key was not found
route /open (không gắn policy)      -> 200 | không cần token
```
**Giải thích:** yêu cầu bị từ chối **không tới upstream**; token hợp lệ được chuyển tiếp nguyên header. Route không gắn policy vẫn mở vì `FallbackPolicy` mặc định là null: thiếu cấu hình là mở. Lab chỉ có xác thực khoá đối xứng; khoá bất đối xứng, JWKS và vòng đời token không có trong lab.

#### Lab 4.3 — YARP có retry không? Nhân retry nhiều tầng
```text
upstream lỗi (503) 2 lần đầu rồi hồi phục; client gọi 1 lần qua YARP -> 503, upstream nhận 1 lần
-- mô hình (không phải đo hệ thống thật): backend luôn lỗi, các tầng gọi nối tiếp, mỗi tầng retry tối đa N lần
   1 tầng, mỗi tầng 3 lần thử -> backend nhận 3 lần cho 1 yêu cầu của người dùng
   2 tầng, mỗi tầng 3 lần thử -> backend nhận 9 lần
   3 tầng, mỗi tầng 3 lần thử -> backend nhận 27 lần
   3 tầng, mỗi tầng 4 lần thử -> backend nhận 64 lần
```
**Giải thích:** YARP chuyển nguyên mã 503 của upstream và không thử lại. Phần mô hình là phép tính nhân, đúng như ví dụ Google SRE, không phải đo hệ thống thật.

#### Lab 4.4 — `X-Forwarded-For` giả để né rate limit (5 yêu cầu mỗi 10 giây theo IP)
20 yêu cầu từ một client, mỗi lần một `X-Forwarded-For` khác.
```text
-- tin mọi proxy (ForwardLimit=null, xoá KnownProxies/KnownNetworks)
   được qua 20, bị 429: 0;    upstream thấy X-Forwarded-For: 203.0.113.1  (giá trị giả)
-- chỉ tin proxy 10.0.0.1 (client loopback không được tin)
   được qua 5, bị 429: 15;    upstream thấy X-Forwarded-For: 127.0.0.1    (IP thật của kết nối)
```
**Giải thích:** tin mọi proxy thì rate limit vô tác dụng và upstream còn nhận IP giả. Chỉ tin proxy đã biết thì giới hạn giữ nguyên. Lab giả lập "client không được tin" bằng danh sách proxy khác; không có ALB thật.

## L5 Troubleshooting — Bảo vệ không hoạt động

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Rate limit không bao giờ chặn | policy không gán cho route, hoặc thiếu `UseRateLimiter()` | xem `RateLimiterPolicy` của route và pipeline | gán policy, đặt `UseRateLimiter()` trước `MapReverseProxy()` |
| Mọi người bị chặn cùng lúc | policy không phân vùng, một bộ đếm chung | xem khoá phân vùng | `RateLimitPartition` theo IP |
| 429 không có `Retry-After` | cửa sổ trượt không có metadata (Lab 4.1) | xem header 429 | tính từ độ dài cửa sổ trong `OnRejected` |
| Rate limit bị né bằng đổi IP | tin mọi `X-Forwarded-For` (Lab 4.4) | thử gửi header giả | `KnownProxies` hẹp, `ForwardLimit` mặc định |
| Gateway chặn preflight CORS | rate limit đứng trước CORS | xem thứ tự middleware | CORS trước rate limit (design 6.4) |
| Route mở dù tưởng đã bảo vệ | không gắn `AuthorizationPolicy`, `FallbackPolicy` null | thử gọi không token (Lab 4.2) | gắn policy hoặc đặt `FallbackPolicy` |
| Token hết hạn vẫn được qua một lúc | `ClockSkew` mặc định 5 phút | so hạn token với thời điểm | đặt `ClockSkew` nhỏ nếu cần |
| Tải upstream nhân lên khi sự cố | retry ở nhiều tầng (Lab 4.3) | đếm số lần upstream nhận | retry một chỗ, ngân sách retry, backoff có jitter |
| 401 không giải thích | thiếu `WWW-Authenticate` ở client | đọc header phản hồi | đọc `error_description` |

## L6 Production — Quyết định khi chạy thật

- **Rate limit:** ghi rõ khoá (IP, người dùng), ngưỡng đọc/ghi, hành vi khi vượt (`429` + `Retry-After`), và **hành vi khi bộ đếm lỗi** (mở: cho qua; đóng: chặn). Với IPv6, gộp theo `/64` để kẻ tấn công không xoay địa chỉ trong một dải (design TB-007). Chạy N instance thì ngưỡng thực lỏng N lần.
- **Timeout:** đặt `ActivityTimeout` và `Timeout` theo p99 của upstream, không để mặc định 100 giây; chấp nhận cắt response streaming dài hơn timeout.
- **Retry:** chỉ cho request idempotent, một tầng, có backoff và jitter, có ngân sách; trước khi thêm retry, đo tỉ lệ 502/503 (design TB-007: xem lại khi vượt 1% kéo dài).
- **JWT:** kiểm issuer, audience, hạn, thuật toán; không tin claim chưa kiểm; khoá và JWKS xoay vòng; **không đưa gateway ra Internet khi chưa có xác thực** (design TB-007).
- **IP client:** `KnownProxies` chỉ chứa proxy thật (dải ALB); cấm `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` trong image không kiểm.
- **Log:** không log token, không tin giá trị correlation id của client.

## L7 Architecture — Chọn cách bảo vệ

| Bài toán | Chọn | Cái giá |
|---|---|---|
| Một instance gateway, tải vừa | rate limit trong bộ nhớ | lỏng hơn N lần khi nhân bản |
| Nhiều instance, cần chính xác | bộ đếm tập trung (Valkey) hoặc rate limit ở ALB/WAF | thêm thành phần |
| Cần đếm theo khoá API và quota | sản phẩm API management | gắn vào một cloud |
| Cho phép bùng nổ ngắn có chủ đích | token bucket | khó giải thích ngưỡng "N/phút" |
| Tránh bùng nổ ở ranh giới | cửa sổ trượt | tốn bộ nhớ hơn cửa sổ cố định |
| Chống DDoS | WAF/CDN phía trước | chi phí, thêm hạ tầng |
| Lỗi thoáng qua của GET | retry một tầng, có ngân sách | nhân tải nếu thêm tầng khác |
| Cần circuit breaker dựng sẵn | Envoy hoặc mesh | thêm thành phần, cấu hình phức tạp |
| Xác thực tập trung | JWT ở gateway + service tự kiểm quyền | gateway không thay phân quyền nghiệp vụ |
| **Không dùng rate limit ở gateway** | dựa vào WAF/CDN hay API management | mất kiểm soát từng route |

## Tự kiểm tra

1. Vì sao cửa sổ cố định cho qua 10 yêu cầu ở Lab 4.1, còn cửa sổ trượt chỉ 5?
2. Vì sao tin mọi `X-Forwarded-For` làm rate limit vô dụng?
3. YARP có retry mặc định không? 3 tầng, mỗi tầng 4 lần thử thì backend nhận bao nhiêu lần?
4. Route không gắn `AuthorizationPolicy` thì mở hay đóng? Vì sao?

<details>
<summary>Đáp án ngắn</summary>

1. Chuỗi yêu cầu rơi vào hai cửa sổ cố định liền nhau; cửa sổ trượt đếm cả đoạn trước.
2. Kẻ tấn công đổi IP mỗi yêu cầu nên mỗi lần là một khoá mới (Lab 4.4).
3. Không; 64 lần (4 × 4 × 4).
4. Mở, vì `FallbackPolicy` mặc định là null (Lab 4.2).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 đến 4.4 (YARP 2.3.0, JwtBearer 10.0.13; đầu ra thật). Lab 4.3 phần "mô hình" là phép tính.
**Đã tự mở:** Cloudflare "How we built rate limiting capable of scaling to millions of domains" (07/06/2017: xấp xỉ cửa sổ trượt, số đo độ chính xác, vì sao không đếm chính xác hay leaky bucket); Stripe rate limiters (Part 7); Google SRE "Addressing Cascading Failures" (ví dụ 4×4×4 = 64).
**Chỉ qua agent (⚠):** YARP `rate-limiting`, `authn-authz`, `session-affinity`, `timeouts`; AWS Builders' Library "Timeouts, retries and backoff with jitter" (243 lần là ví dụ minh hoạ); ASP.NET Core `proxy-load-balancer` (mặc định `ForwardedHeaders`, `ForwardLimit`); Envoy circuit breaking và outlier detection.
**Chưa kiểm:** token bucket, rate limit tập trung, JWKS và khoá bất đối xứng, session affinity, ngân sách retry, Polly; hành vi `ForwardedHeaders` sau ALB thật.
