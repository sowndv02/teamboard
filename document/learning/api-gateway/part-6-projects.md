# Part 6 — Projects (API Gateway)

> [!NOTE]
> **Mục tiêu:** dựng một gateway mô phỏng theo design TB-007 cho `board-service` (route đọc/ghi, fallback 404, ánh xạ lỗi upstream sang `problem+json`) và ghi lại chỗ YARP **khác** với giả định của design để người dùng quyết; không sửa `src/`.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** một bảng kết quả theo từng tình huống lỗi, và danh sách điểm lệch với design kèm đề xuất.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

Gateway là một trong 6 service bắt buộc của TeamBoard (kiến trúc "API Gateway", chỉ routing, xác thực, rate limit, **không có nghiệp vụ**). Trạng thái tại thời điểm viết: có spec ([spec](../../spec/api-gateway.md)), design ([design](../../design/api-gateway.md)) và plan ([plan](../../plans/api-gateway-slices.md)); lát 1 là issue TB-010 (skeleton, định tuyến, health, 404) đang làm ở nhánh riêng với test BDD. Lộ trình **không** sửa `src/`, `config/`, spec, design hay plan: mọi mã chạy trong thư mục thử ngoài repo, và khi lab cho kết quả khác design thì chỉ **báo**.

Quyết định đã chốt trong design (đối chiếu khi làm dự án): tự host YARP, host mỏng không Clean Architecture, route tĩnh nạp lại khi file đổi, timeout tổng 30 giây, rate limit trong bộ nhớ (cửa sổ trượt, IPv6 gộp /64), chưa JWT, chưa retry hay circuit breaker, không đưa ra Internet trước khi có JWT, mã lỗi `ROUTE_NOT_FOUND`, `RATE_LIMIT_EXCEEDED`, `PAYLOAD_TOO_LARGE`, `UPSTREAM_UNAVAILABLE` (503), `UPSTREAM_BAD_GATEWAY` (502), `UPSTREAM_TIMEOUT` (504), `INTERNAL_ERROR`.

## L4 Hands-on — Ba dự án

Môi trường: như Part 0. Timeout đặt 1 giây thay cho 30 giây để lab chạy nhanh.

#### Dự án 6.1 — Gateway mô phỏng theo design: route, fallback, ánh xạ lỗi
Hai route trên `/api/v1/boards/**` (đọc: `GET, HEAD`; ghi: `POST, PUT, PATCH, DELETE`), `MapFallback` trả `404 problem+json`, và một middleware sau YARP đọc `IForwarderErrorFeature` để đổi lỗi sang `problem+json`.
**Đầu ra thật:**
```text
GET /api/v1/boards/1 (route đọc)                          -> 200 | {"ok":true}
POST /api/v1/boards/1 (route ghi)                         -> 200 | {"ok":true}
OPTIONS /api/v1/boards/1 (method không có route)          -> 404 application/problem+json {"code":"ROUTE_NOT_FOUND","status":404}
GET /khong-co (không khớp route)                          -> 404 application/problem+json {"code":"ROUTE_NOT_FOUND","status":404}
GET /dead/x (cổng upstream đóng)                          -> 503 {"code":"UPSTREAM_UNAVAILABLE","status":503}   2057 ms
GET /api/v1/boards/abort (upstream ngắt trước header)     -> 502 {"code":"UPSTREAM_BAD_GATEWAY","status":502}   2 ms
GET /api/v1/boards/slow (upstream chậm, route Timeout 1 s) -> 504 {"code":"UPSTREAM_TIMEOUT","status":504}       1014 ms
```
**Giải thích:**
- `503` chỉ khi lỗi là **từ chối kết nối** (`SocketException` với `ConnectionRefused`); upstream ngắt trước header cũng là `SocketException` (kết nối bị reset) nên cần phân biệt theo mã lỗi socket, nếu không `502` bị phân loại nhầm thành `503`. Lần thử đầu của lab mắc đúng lỗi này.
- Timeout của route: lỗi của forwarder là `RequestCanceled` (YARP trả 400); muốn `504` phải kiểm `IHttpRequestTimeoutFeature.RequestTimeoutToken`. Không được thêm điều kiện "client chưa huỷ" vì token `RequestAborted` cũng bị huỷ khi hết timeout.
- Cổng đóng mất ~2 giây trên Windows; đó là thời gian kết nối thất bại của máy này.

#### Điểm khác giữa YARP và giả định của design (báo cáo, không tự sửa)
| Mục design | Giả định hoặc quy định | Quan sát ở lab |
|---|---|---|
| 6.3 route, fallback | method không hỗ trợ trả `404 ROUTE_NOT_FOUND` | route hạn chế `Methods` trả `405` rỗng; cần `MapFallback` (dự án 6.1) |
| D4 timeout | quá timeout trước header là `504 UPSTREAM_TIMEOUT` | `Timeout` của route cho `400` mặc định; `ActivityTimeout` cho `504` (Part 2) |
| D4 ánh xạ | `503` khi không kết nối, `502` khi ngắt hay lỗi giao thức | đúng nếu phân biệt theo `SocketError`, không chỉ theo `SocketException` |
| 6.3 giới hạn body | `Content-Length` lớn hơn giới hạn bị `413` ngay, không gửi xuống upstream | `MaxRequestBodySize` của route trả 413 nhưng upstream vẫn thấy yêu cầu (Part 5, Lab 5.2) |
| D5 `X-Forwarded-*` | đặt lại, không nối với giá trị client | khớp: giá trị giả bị thay (Part 2, Lab 2.1; Part 4, Lab 4.4) |
| D5 `traceparent` | forwarder mặc định truyền | đúng khi client đã gửi; không có Activity thì không tự tạo (Part 2, Lab 2.3) |
| 6.4 thứ tự middleware | CORS trước rate limit | chưa chạy; bài tập 6.2 |

Mỗi dòng trên là một câu hỏi cần người dùng chốt trước hoặc khi làm lát tương ứng, không phải kết luận rằng design sai: design đã ghi một số hành vi YARP là "chưa kiểm, kiểm bằng test ở lát 2 và 3".

#### Dự án 6.2 — Thứ tự middleware: CORS trước rate limit (bài tập, ⚠ chưa chạy)
Dựng pipeline `ForwardedHeaders → Routing → Correlation → ExceptionHandler → CORS → RateLimiter → BodyLimit → YARP` như design 6.4, rate limit 3 yêu cầu/phút. **Tiêu chí đạt:** (1) preflight `OPTIONS` không bị tính vào giới hạn; (2) response `429` vẫn mang header CORS; (3) đổi thứ tự (rate limit trước CORS) làm (1) hoặc (2) sai, ghi lại đầu ra thật.

#### Dự án 6.3 — Gắn JWT vào route `/api/v1/boards/**` (bài tập, ⚠ chưa chạy trên gateway của dự án)
Dựa trên [Part 4, Lab 4.2](part-4-advanced.md): thêm `AuthorizationPolicy` cho route, kiểm bốn trường hợp (không token, hợp lệ, hết hạn, sai chữ ký) và ghi mã `401` kèm `WWW-Authenticate`. **Tiêu chí đạt:** health và fallback vẫn không cần token; yêu cầu bị từ chối không tới upstream; không log `Authorization`. Lưu ý design TB-007 **loại JWT khỏi lát hiện tại**: bài tập là để chuẩn bị cho giai đoạn identity, không phải thay đổi phạm vi.

## L5 Troubleshooting — Khi dự án không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| `502` bị gán thành `503` | phân loại theo `SocketException` chung | in `SocketErrorCode` | chỉ `ConnectionRefused` (và DNS) là `503` |
| Timeout trả `502` hoặc `400` thay vì `504` | không kiểm `RequestTimeoutToken` | in `IHttpRequestTimeoutFeature` | kiểm token trước khi phân loại; không loại trừ `RequestAborted` |
| Fallback không bao giờ chạy | route khớp path nhưng sai method trả `405` trước | thử method lạ | `MapFallback` hoặc route bắt tất cả với `Order` lớn |
| Lỗi mất header `X-Correlation-Id` | `Response.Clear()` xoá header đã đặt | đọc response lỗi | đặt header trong `OnStarting` hoặc sau `Clear()` |
| Middleware lỗi không bắt được lỗi YARP | đặt trước `MapReverseProxy` mà không đọc `IForwarderErrorFeature` sau `next()` | in feature | đọc feature sau `await next()` khi response chưa bắt đầu |
| Số đo mỗi lần chạy một khác | máy ồn, một lần chạy | chạy lại 3 lần | lặp lại, báo khoảng giá trị |
| Cổng đóng mất ~2 giây | thời gian lỗi kết nối của hệ điều hành | đo trực tiếp bằng `curl` | không chỉnh trong gateway; timeout kết nối là `ConnectTimeout` |

## Tự kiểm tra

1. Vì sao route chỉ cho `GET` mà nhận `POST` ra 405, và làm sao ra 404 `ROUTE_NOT_FOUND`?
2. Phân biệt `503` và `502` trong ánh xạ lỗi dựa vào điều gì?
3. Vì sao không thêm điều kiện "client chưa huỷ" khi nhận diện timeout?
4. Kể hai điểm lệch giữa YARP và design cần người dùng chốt.

<details>
<summary>Đáp án ngắn</summary>

1. Routing của ASP.NET Core trả 405 khi path khớp mà method không; dùng `MapFallback`.
2. `503` khi từ chối kết nối (`ConnectionRefused`); `502` cho ngắt hay lỗi giao thức.
3. `RequestAborted` cũng bị huỷ khi hết timeout nên điều kiện đó loại nhầm timeout.
4. Ví dụ: 405 thay vì 404; timeout của route cho 400; body 413 nhưng upstream vẫn nhận yêu cầu.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 (YARP 2.3.0, đầu ra thật, môi trường ở Part 0). Các dòng "quan sát ở lab" trong bảng tham chiếu đến Lab 2.1, 2.3, 4.4 và 5.2 đã chạy.
**Đã tự mở:** design TB-007 (D4, D5, 6.3, 6.4, STRIDE); spec và plan của gateway chỉ đọc mục tiêu, không đối chiếu từng yêu cầu.
**Chưa kiểm:** Dự án 6.2 và 6.3 là bài tập chưa chạy; gateway thật của TB-010 (nhánh riêng, chưa đọc mã); số đo là của mô hình trên một máy, không phải đo hệ thống của dự án.
