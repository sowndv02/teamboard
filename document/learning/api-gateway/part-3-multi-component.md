# Part 3 — Multi-component (API Gateway)

> [!NOTE]
> **Mục tiêu:** cho gateway nói chuyện với nhiều instance upstream: chính sách cân bằng tải, health check chủ động và thụ động, và biết chuyện gì xảy ra khi có nhiều instance gateway.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** chọn chính sách cân bằng tải, cấu hình health check, dự đoán lưu lượng khi một instance chết.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Nhiều đích, nhiều bản gateway

#### Cân bằng tải
**Là gì:** cluster có nhiều destination; chính sách quyết định mỗi request tới đâu.
**Use case:** ba instance `board-service`; gateway rải lưu lượng.

#### Health check
**Chủ động:** gateway gửi yêu cầu thăm dò định kỳ tới đường dẫn như `/health`. **Thụ động:** gateway quan sát kết quả của chính các request thật.
**Use case:** instance chết thì ngừng gửi lưu lượng vào nó.

#### Nhiều instance gateway
Chạy nhiều bản gateway sau một load balancer để không là điểm lỗi đơn. Bộ đếm rate limit trong bộ nhớ khi đó nằm ở **từng** instance (Part 4).

## L2 Why — Vì sao cần cân bằng tải và health check

Một upstream đơn là điểm lỗi đơn; nhiều instance cho khả năng chịu lỗi và mở rộng. Nhưng nếu gateway tiếp tục gửi vào instance chết thì thêm instance không giúp được gì: một nửa yêu cầu vẫn lỗi (Lab 3.2). Và nếu mọi instance được cho tải như nhau trong khi một cái chậm, cả cụm bị kéo chậm (Lab 3.1).

## L3 How — Cơ chế đáng nhớ

### Chính sách cân bằng tải
Khoá `LoadBalancingPolicy` của cluster. **Mặc định là `PowerOfTwoChoices`.**

| Chính sách | Cách chọn | Hợp khi |
|---|---|---|
| `FirstAlphabetical` | destination đầu tiên còn dùng được | dự phòng chủ động (primary và standby) |
| `Random` | ngẫu nhiên | các instance đồng đều |
| `RoundRobin` | lần lượt | các instance đồng đều, request đồng đều |
| `PowerOfTwoChoices` | chọn ngẫu nhiên hai, lấy cái ít request đang xử lý hơn | mặc định tốt, rẻ hơn `LeastRequests` |
| `LeastRequests` | xét tất cả, lấy cái ít request đang xử lý nhất | request có thời gian xử lý rất khác nhau |

Các chính sách "ít request" dựa vào số request **đang chạy** nên tự tránh instance chậm (Lab 3.1). Chính sách tự viết cài `ILoadBalancingPolicy`.

### Health check chủ động
Cấu hình `HealthCheck:Active`: `Enabled` (mặc định false), `Interval` (15 giây), `Timeout` (10 giây), `Policy` (bắt buộc), `Path`, `Query`. Chính sách dựng sẵn duy nhất là `ConsecutiveFailures`, ngưỡng `ConsecutiveFailuresHealthPolicy.Threshold` mặc định **2** (đặt trong `Metadata` của cluster); phản hồi 2xx là khoẻ. Mỗi destination có thể có `Health` riêng. Destination khỏi lỗi khi một lần thăm dò thành công.

### Health check thụ động
Cấu hình `HealthCheck:Passive`: `Enabled`, `Policy` (bắt buộc), `ReactivationPeriod`. Chính sách dựng sẵn duy nhất là `TransportFailureRate`: tỉ lệ request lỗi **vận chuyển** (không phải lỗi ứng dụng như 500) trong cửa sổ trượt; tuỳ chọn toàn cục `DetectionWindowSize` (1 phút), `MinimalTotalCountThreshold` (10), `DefaultFailureRateLimit` (0,3). Destination bị đánh dấu không khoẻ **ngừng nhận mọi lưu lượng** cho tới khi hết `ReactivationPeriod`, rồi trở về `Unknown` và được thử lại. Cần `PassiveHealthCheckMiddleware` (mặc định của `MapReverseProxy()` đã thêm).

### Khi không còn destination khoẻ
`AvailableDestinationsPolicy`: `HealthyOrPanic` (**mặc định**): nếu không còn destination khoẻ thì coi **tất cả** là khả dụng; `HealthyAndUnknown`: không còn thì trả **503**. Vì vậy với mặc định, cụm chết hoàn toàn vẫn nhận lưu lượng và trả 502 hay 504, không phải 503 của YARP.

### Nhiều instance gateway
Mỗi instance giữ trạng thái health, bộ đếm rate limit và cache **riêng**; không có phối hợp. Bộ đếm theo IP vì vậy lỏng hơn N lần khi chạy N instance (design TB-007 chấp nhận ở một instance, đổi khi có nhiều instance).

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0; mỗi lab dựng lại gateway và các upstream giả.

#### Lab 3.1 — Chính sách cân bằng tải khi một upstream chậm
3 upstream: `s1`, `s2` nhanh, `s3` chậm 200 ms; 300 yêu cầu, đồng thời 30.
**Đầu ra thật:**
```text
RoundRobin         s1=100 s2=100 s3(chậm)=100   tổng 946 ms
Random             s1= 97 s2=100 s3(chậm)=103   tổng 852 ms
PowerOfTwoChoices  s1=145 s2=145 s3(chậm)= 10   tổng 212 ms
LeastRequests      s1=136 s2=156 s3(chậm)=  8   tổng 209 ms
```
**Giải thích:** `RoundRobin` và `Random` đưa 1/3 tải vào instance chậm và cả cụm mất ~0,9 giây; hai chính sách "ít request" gửi chỉ 8 đến 10 yêu cầu vào instance chậm và xong trong ~0,2 giây. Phép đo một lần chạy, đồng thời 30, upstream giả: chỉ có giá trị tương đối.

#### Lab 3.2 — Health check khi `s2` chết (20 yêu cầu mỗi lần, cluster `RoundRobin`)
**Đầu ra thật** (`502/503` là số lỗi):
```text
không có health check:    cả hai sống 20 OK | dừng s2: 10 OK, 10 lỗi | sau 2,5 giây: 10 OK, 10 lỗi
thụ động (TransportFailureRate; cửa sổ 10 giây, ngưỡng tối thiểu 2, giới hạn 0,3):
                          cả hai sống 20 OK | dừng s2: 15 OK, 5 lỗi  | sau 2,5 giây: 20 OK, 0 lỗi
chủ động (/health mỗi 1 giây, ConsecutiveFailures):
                          cả hai sống 20 OK | dừng s2: 18 OK, 2 lỗi  | sau 2,5 giây: 20 OK, 0 lỗi
```
**Giải thích:** không health check thì một nửa yêu cầu **mãi mãi** lỗi. Thụ động phải tốn một số yêu cầu thật làm "mồi" (ở lab này 5 lỗi) trước khi loại destination; chủ động chỉ mất 2 lỗi (hai lần thăm dò thất bại liên tiếp) nhưng thêm lưu lượng thăm dò. Số lỗi phụ thuộc nhịp thăm dò và tải; ngưỡng được hạ thấp trong lab để thấy hiệu ứng.

## L5 Troubleshooting — Khi cụm upstream không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Một nửa yêu cầu lỗi mãi | không có health check, instance chết | thử từng destination bằng tay | bật health check chủ động hoặc thụ động |
| Lỗi chỉ ở vài yêu cầu đầu rồi hết | health thụ động cần "mồi" | đếm lỗi trước khi loại | thêm health chủ động nếu không chịu được |
| Destination khoẻ lại nhưng không nhận tải | `ReactivationPeriod` còn dài | xem cấu hình và thời gian | rút thời gian, hoặc bật chủ động |
| Cả cụm chậm vì một instance chậm | `RoundRobin` hay `Random` | so thời gian từng instance | `PowerOfTwoChoices` hoặc `LeastRequests` |
| Cụm chết hoàn toàn nhưng không thấy 503 | `HealthyOrPanic` mặc định | xem `AvailableDestinationsPolicy` | `HealthyAndUnknown` nếu muốn 503 sớm |
| Health thụ động không loại instance lỗi 500 | chỉ tính lỗi vận chuyển | xem loại lỗi | health chủ động hoặc chính sách tự viết |
| Giới hạn thực lỏng hơn mong đợi | N instance gateway, bộ đếm riêng | so tổng yêu cầu qua tất cả instance | bộ đếm tập trung, hoặc chấp nhận |

## L7 Architecture — Chọn cân bằng tải và health check

| Nhu cầu | Chọn | Cái giá |
|---|---|---|
| Một upstream, tải thấp | cluster một destination, không health check | không chịu lỗi |
| Nhiều instance đồng đều | `PowerOfTwoChoices` (mặc định) | không đảm bảo công bằng tuyệt đối |
| Request có thời gian khác nhau lớn | `LeastRequests` | xét tất cả, tốn hơn một chút |
| Phát hiện lỗi nhanh, ít hy sinh request | health chủ động | lưu lượng thăm dò, cần endpoint `/health` đúng nghĩa |
| Không muốn thêm lưu lượng thăm dò | health thụ động | hy sinh một số request thật |
| Lỗi tầng ứng dụng (500) cũng phải loại | chủ động, hoặc chính sách tự viết | tự cài |
| Cân bằng tải ngoài gateway | ALB/NLB trước upstream, YARP trỏ vào một địa chỉ | thêm thành phần, mất kiểm soát từng destination |
| **Không dùng cân bằng tải ở gateway** | để service discovery hay mesh làm | gateway không biết từng instance |

Dự án hiện chỉ có **một** `board-service`; design TB-007 dùng một destination và chưa cần cân bằng tải hay health check ở gateway. Phần này là kiến thức cho giai đoạn nhiều instance.

## Tự kiểm tra

1. Chính sách cân bằng tải mặc định của YARP là gì, và vì sao nó tránh được instance chậm?
2. Health thụ động và chủ động khác nhau ở điểm nào về lỗi trong Lab 3.2?
3. Cụm không còn destination khoẻ thì YARP mặc định làm gì?
4. Chạy 3 instance gateway, rate limit 120/phút/IP trong bộ nhớ: giới hạn thực là bao nhiêu?

<details>
<summary>Đáp án ngắn</summary>

1. `PowerOfTwoChoices`: chọn hai ngẫu nhiên, lấy cái đang xử lý ít request hơn (Lab 3.1).
2. Thụ động mất vài yêu cầu thật làm mồi; chủ động mất ít hơn nhưng thêm lưu lượng thăm dò.
3. `HealthyOrPanic`: coi tất cả là khả dụng và vẫn gửi (lỗi 502 hay 504, không phải 503 của YARP).
4. Khoảng 360/phút/IP nếu lưu lượng của IP đó chia đều ba instance.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 và 3.2 (YARP 2.3.0, đầu ra thật).
**Đã tự mở:** learn.microsoft.com YARP `dests-health-checks` (khoá, mặc định, chính sách, `AvailableDestinationsPolicy`).
**Chỉ qua agent (⚠):** YARP `load-balancing` (tên chính sách, mặc định `PowerOfTwoChoices`), `session-affinity`.
**Chưa kiểm:** nhiều instance gateway thật sau một load balancer; hành vi `HealthyAndUnknown` trả 503 (chưa chạy); cân bằng tải qua service discovery.
