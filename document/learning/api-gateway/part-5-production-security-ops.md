# Part 5 — Production, Security, Operations (API Gateway)

> [!NOTE]
> **Mục tiêu:** quan sát gateway bằng chỉ số đúng, giữ nó an toàn (TLS, header, giới hạn body, lộ thông tin nội bộ), thay đổi cấu hình mà không gây sự cố, và hiểu gateway là điểm lỗi đơn có chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** liệt kê chỉ số và cảnh báo, nêu rủi ro bảo mật theo STRIDE, mô tả cách rollout cấu hình an toàn, ước tính độ trễ thêm vào và chi phí.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Gateway khi đã là cửa vào thật

#### Quan sát
Log có cấu trúc, trace (OpenTelemetry), metric theo route và status, health endpoint của chính gateway.

#### Bảo mật biên
TLS ở cửa vào, xác thực, giới hạn kích thước và tốc độ, không lộ thông tin nội bộ trong lỗi, chống giả header.

#### Thay đổi cấu hình
Route, cluster và chính sách là cấu hình; đổi cấu hình là một lần triển khai.

#### Điểm lỗi đơn và chi phí
Gateway chết là mất mọi API; nó chạy 24/7 nên có chi phí cố định.

## L2 Why — Vì sao phần này quan trọng nhất

Gateway nằm trên đường của **mọi** yêu cầu. Sự cố ở đây có phạm vi toàn hệ thống: một cấu hình sai hay một quy tắc xấu kéo sập cả nền tảng (Cloudflare 2019, 2025; Fastly 2021, Part 7). Và vì lỗi ở biên thường **không phải lỗi của gateway**, quan sát phải cho biết nhanh lỗi nằm ở gateway hay ở upstream.

## L3 How — Cơ chế đáng nhớ

### Quan sát

| Chỉ số | Ý nghĩa | Dấu hiệu xấu |
|---|---|---|
| số yêu cầu theo route và status | lưu lượng và tỉ lệ lỗi | `5xx` tăng, `4xx` tăng bất thường |
| tỉ lệ `500` do gateway tự sinh | lỗi của gateway (không phải upstream) | vượt SLO (design: 0,5% trong 5 phút) |
| `502`, `503`, `504` | upstream hỏng, chậm | kéo dài |
| `429` | rate limit | tăng đột biến |
| độ trễ thêm vào (tổng − thời gian chờ upstream) | chi phí của gateway | vượt mục tiêu (design: p95 < 20 ms) |
| `gateway_upstream_aborted` | upstream ngắt sau khi gửi header | tăng |
| số kết nối đang mở | áp lực kết nối | gần giới hạn |

Trace: span của gateway là cha của span upstream nhờ `traceparent`; tag `correlationId`, route, status. Log không ghi `Authorization`, cookie hay body; IP là dữ liệu cá nhân (design: giữ tối đa 7 ngày). Cảnh báo theo **tỉ lệ**, không theo số tuyệt đối.

### Bảo mật theo STRIDE (rút từ design TB-007)

| Mối đe doạ | Biện pháp |
|---|---|
| Giả mạo (`X-Forwarded-For` giả) | `KnownProxies` hẹp; cấm bật mọi proxy (Part 4) |
| Giả hoặc làm bẩn log (correlation id) | kiểm định dạng và độ dài, thay bằng id mới (Part 2) |
| Lộ thông tin (token, PII, host nội bộ) | không log `Authorization`/body; `problem+json` không chứa host và stack |
| Từ chối dịch vụ (spam, body lớn, slowloris) | rate limit, giới hạn body, timeout, giới hạn Kestrel |
| Nâng quyền (qua container) | chạy không root, không mount secret, cấu hình chỉ đọc |
| Sửa cấu hình trái phép | cấu hình trong repo, qua review |

#### Giới hạn body
`MaxRequestBodySize` của route (mặc định theo Kestrel, khoảng 30 MB; `-1` tắt). Vượt giới hạn thì client nhận `413`; Lab 5.2 cho thấy upstream **vẫn nhận được yêu cầu** (đã mở kết nối) trước khi luồng bị ngắt, nên muốn "không gửi xuống upstream" phải tự kiểm `Content-Length` bằng middleware đứng trước YARP.

#### TLS và mạng
Kết thúc TLS ở ALB hoặc ở gateway, mã hoá lại tới upstream nếu cần (mTLS); upstream **không** được phơi ra ngoài gateway (nếu không client bỏ qua được mọi chính sách biên); chuyển IP và Host client xuống upstream bằng `X-Forwarded-*`.

### Thay đổi cấu hình
- Cấu hình nạp lại khi file đổi, không cần khởi động lại (Lab 5.1).
- Cấu hình sai **vẫn là một lần triển khai sai**: Cloudflare 2019 (quy tắc WAF xấu đẩy toàn cầu, hết CPU, khoảng 27 phút), Fastly 2021 (cấu hình hợp lệ của một khách kích hoạt lỗi tiềm ẩn, khoảng 85% mạng lỗi), Cloudflare 11/2025 (tệp cấu hình sinh tự động gấp đôi, vượt giới hạn, proxy panic). Bài học chung: rollout **theo giai đoạn**, kiểm tra cấu hình do máy sinh như đầu vào không tin cậy, giữ bản cấu hình cuối cùng tốt, có công tắc khẩn cấp.
- Lỗi cấu hình của chính bạn: kiểm khi khởi động (design: route phải có cluster), test cấu hình, rollback bằng cấu hình cũ.

### Điểm lỗi đơn và chạy nhiều bản
Chạy ít nhất hai instance sau một load balancer, health endpoint riêng (`/health/live`, `/health/ready`; `ready` **không** phụ thuộc upstream để gateway vẫn sống khi một service chết), tắt êm (graceful shutdown ≥ thời gian timeout). Trạng thái trong bộ nhớ (rate limit, health) nằm riêng từng instance (Part 3, 4). Microsoft cũng khuyên kiểm tải gateway vì nó dễ thành nút cổ chai.

### Hiệu năng
Gateway cộng thêm một hop. Với YARP CETO của Microsoft (ingress cho trải nghiệm AI, trên .NET 8 và YARP 2.1) nhóm viết "every single millisecond at this layer counts" và đo thường xuyên; Lab 5.3 cho thấy chi phí thêm trên loopback.

### Chi phí (từ design TB-007, ước tính chưa xác minh giá)
ECS Fargate một task nhỏ khoảng 9 đến 10 USD/tháng nếu chạy 24/7; ALB dùng chung khoảng 16 đến 25 USD; tổng 24/7 khoảng 25 đến 35 USD, **vượt trần ngân sách dự án (20 USD/tháng)** nên chỉ bật trong giờ lab và `destroy` sau đó. Đây là số của design, ghi "từ trí nhớ, chưa xác minh"; kiểm lại bằng `aws-service-selection` trước khi viết Terraform.

## L4 Hands-on — Lab Part 5

Môi trường: như Part 0.

#### Lab 5.1 — Nạp lại cấu hình khi file đổi, kể cả cấu hình sai
Cấu hình trong file JSON `reloadOnChange`. Mỗi bước ghi lại file, đợi 1,5 giây, rồi thử `/a`, `/b`, `/c`, `/d`.
**Đầu ra thật:**
```text
cấu hình ban đầu (route a):               /a/x=200  /b/x=404  /c/x=404  /d/x=404
sau khi thêm route b vào file:            /a/x=200  /b/x=200  /c/x=404  /d/x=404
sau khi thêm route trỏ cluster không có (và route d hợp lệ):  /a/x=200  /b/x=200  /c/x=404  /d/x=200
sau khi sửa lại (a, b, c):                /a/x=200  /b/x=200  /c/x=200  /d/x=404
```
**Giải thích:** thêm route vào file có hiệu lực mà không khởi động lại. Với một route trỏ tới cluster không tồn tại, các route hợp lệ khác trong **cùng** file vẫn được áp dụng (`/d` thành 200). Log mức Warning của lab không bắt được dòng báo lỗi, nên chưa biết YARP có báo ở mức nào ⚠ và route sai bị bỏ hay bị từ chối ra sao (chưa thử gọi `/bad`).

#### Lab 5.2 — Giới hạn body theo route (`MaxRequestBodySize` = 1024 byte)
**Đầu ra thật:**
```text
POST 500 byte (có Content-Length)               -> 200 | thân: nhận 500 | upstream nhận 1 yêu cầu
POST 2000 byte (có Content-Length)              -> 413 | thân rỗng      | upstream nhận 1 yêu cầu
POST 2000 byte (chunked, không có Content-Length) -> 413 | thân rỗng     | upstream nhận 1 yêu cầu
```
**Giải thích:** vượt giới hạn thì client nhận `413`, nhưng **upstream vẫn thấy một yêu cầu** (kết nối đã được mở và header đã gửi) trước khi luồng bị ngắt. Design TB-007 viết "request có Content-Length lớn hơn giới hạn bị từ chối 413 ngay, không gửi xuống upstream": muốn đúng như vậy cần middleware kiểm `Content-Length` đứng **trước** YARP (design 6.4 đã có bước `BodyLimit`); cấu hình của route chưa đủ. Lab không kiểm upstream nhận bao nhiêu byte.

#### Lab 5.3 — Độ trễ thêm vào (loopback, 3000 yêu cầu tuần tự mỗi bên, sau 300 yêu cầu khởi động)
**Đầu ra thật:**
```text
gọi thẳng upstream: p50 0.136 ms, p95 0.157 ms, p99 0.202 ms
qua gateway YARP:   p50 0.307 ms, p95 0.348 ms, p99 0.418 ms
```
**Giải thích:** trên một máy, một kết nối, upstream trả ngay, gateway thêm khoảng 0,17 ms ở p50. **Không** ngoại suy sang hệ thật: không có mạng, TLS, tải đồng thời, hay logic middleware (rate limit, xác thực); mục tiêu design (p95 thêm < 20 ms) cần đo lại ở môi trường thật.

## L5 Troubleshooting — Sự cố vận hành gateway

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Toàn bộ API lỗi sau khi đổi cấu hình | cấu hình route sai hoặc quy tắc xấu | so cấu hình trước và sau, theo thời điểm | rollback cấu hình; rollout từng giai đoạn |
| Gateway CPU 100% sau khi thêm quy tắc | quy tắc tốn CPU (regex backtracking) | profile, tắt quy tắc | bộ so khớp có giới hạn tài nguyên; kiểm CPU trong test |
| Không phân biệt lỗi do gateway hay upstream | gateway nuốt hay đổi lỗi mơ hồ | xem `code` và `correlationId` trong phản hồi | `problem+json` có `code` ổn định |
| `413` mà upstream vẫn thấy yêu cầu | giới hạn body của route (Lab 5.2) | log upstream | middleware kiểm `Content-Length` trước YARP |
| Lộ host nội bộ trong thông báo lỗi | trả nguyên ngoại lệ của forwarder | đọc thân lỗi | `problem+json` không chứa host và stack |
| Upstream bị gọi thẳng, bỏ qua gateway | upstream phơi ra mạng ngoài | kiểm security group, cổng | chỉ cho phép gateway gọi upstream |
| Một instance gateway khác hành vi | cấu hình lệch giữa các bản | so cấu hình đang chạy | nguồn cấu hình duy nhất, rollout đồng bộ |
| Cảnh báo ồn hoặc muộn | cảnh báo theo số tuyệt đối | xem ngưỡng | cảnh báo theo tỉ lệ và SLO |
| Hoá đơn cao | chạy ALB và Fargate 24/7 | xem giờ chạy | `destroy` ngoài giờ lab, kiểm trần ngân sách |

## L6 Production — Danh sách kiểm trước khi đưa gateway ra ngoài

- **Xác thực:** có JWT (hoặc chính sách tương đương) trước khi ra Internet; không có thì ghi cảnh báo và giữ trong mạng riêng (design TB-007).
- **TLS:** kết thúc ở ALB hoặc gateway; upstream không phơi ra ngoài.
- **IP client:** `KnownProxies` đúng dải ALB; không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
- **Giới hạn:** rate limit, kích thước body, timeout, giới hạn Kestrel đã đặt; hành vi khi vượt đã chốt.
- **Lỗi:** `problem+json` với `code` ổn định, không lộ host nội bộ hay stack.
- **Quan sát:** dashboard và cảnh báo theo tỉ lệ (500, 502/503/504, 429, độ trễ thêm vào); log không chứa token.
- **Cấu hình:** trong repo, qua review; kiểm khi khởi động; rollout theo giai đoạn; có công tắc và bản cấu hình cũ để quay lại.
- **Khả dụng:** từ hai instance sau load balancer, health endpoint đúng nghĩa, tắt êm.
- **Chi phí:** đã ước tính và so với trần ngân sách; ngoài giờ lab `destroy`.
- **Runbook:** rollback cấu hình, tắt một route, xoay khoá JWT, xử lý tăng đột biến 429 hay 5xx.

## L7 Architecture — Tự dựng, managed hay không dùng

| Phương án | Khi chọn | Cái giá |
|---|---|---|
| Không dùng gateway | một service, một client | client dính vào service; lặp việc chéo |
| YARP tự host (dự án chọn) | đội .NET, cửa vào mỏng, cần tuỳ biến bằng code | tự lo WAF, HA, vá lỗi, quan sát |
| ALB (tầng 7) làm biên | cần TLS, WAF, health check có sẵn, route đơn giản | không có tuỳ biến bằng code; không phải API management |
| Amazon API Gateway / Azure API Management | quản lý API công khai: key, quota, portal | gắn vào cloud, chi phí, khó giống local |
| Envoy/NGINX tiến trình riêng | ngôn ngữ trung lập, hiệu năng, breaker và outlier detection sẵn | thêm công nghệ ngoài .NET |
| Mesh ingress | đã chạy mesh | yếu hơn gateway chuyên dụng ở WAF, transform |
| YARP phía sau ALB | kết hợp: ALB lo TLS và WAF, YARP lo logic | hai lớp phải cấu hình nhất quán (`KnownProxies`) |

**Câu hỏi chọn:** nếu gateway chết 10 phút thì sao, và đổi sang phương án managed có giảm rủi ro hay chỉ chuyển nó sang nhà cung cấp? Chi tiết dự án ở [Part 6](part-6-projects.md).

## Tự kiểm tra

1. Vì sao `ready` của gateway không nên phụ thuộc upstream?
2. Cấu hình route là một lần triển khai: nêu hai sự cố thật minh hoạ và hai biện pháp.
3. Lab 5.2 cho thấy điều gì về `MaxRequestBodySize`?
4. Vì sao không ngoại suy 0,17 ms ở Lab 5.3 sang production?

<details>
<summary>Đáp án ngắn</summary>

1. Nếu `ready` phụ thuộc upstream thì một service chết làm mọi instance gateway bị loại, mất cả những route còn sống.
2. Cloudflare 2019 (quy tắc WAF xấu), Fastly 2021; rollout theo giai đoạn, kiểm cấu hình tự sinh, giữ bản tốt cuối.
3. Client nhận 413 nhưng upstream vẫn nhận yêu cầu; cần middleware kiểm `Content-Length` trước YARP.
4. Chỉ một máy, loopback, không TLS hay tải đồng thời, không có middleware.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 đến 5.3 (YARP 2.3.0, đầu ra thật).
**Đã tự mở:** Cloudflare "Details of the Cloudflare outage on July 2, 2019" (regex backtracking, 13:42 đến 14:09 UTC, khoảng 27 phút, nguyên nhân và biện pháp); design TB-007 (bảo mật, quan sát, chi phí).
**Chỉ qua agent (⚠):** Cloudflare 18/11/2025 và Fastly 08/06/2021 (cả hai agent đã mở trang gốc); Microsoft "Building a scalable gateway for Microsoft AI" (17/04/2024); Azure pattern "Gateway Offloading"; YARP `config-files` (nạp lại cấu hình).
**Chưa kiểm:** TLS và mTLS thật, gateway sau ALB, cách YARP báo lỗi khi nạp cấu hình sai, hành vi `HealthyAndUnknown`, giá AWS hiện hành (số của design là "từ trí nhớ").
