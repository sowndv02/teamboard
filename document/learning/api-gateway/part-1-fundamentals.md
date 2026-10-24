# Part 1 — Fundamentals & Mental Models (API Gateway)

> [!NOTE]
> **Mục tiêu:** hiểu gateway là một vai trò gì, vì sao client gọi thẳng nhiều service gây vấn đề, từng cơ chế của gateway, và chọn có dùng gateway hay không.
> **Cần biết trước:** [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** phân biệt gateway với reverse proxy, load balancer, mesh, BFF; chọn giải pháp từ Bản đồ cho 3 tình huống.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Gateway và các khái niệm gần nó

#### API Gateway
**Là gì:** một điểm vào tập trung giữa client và các service: nhận request, **chuyển tới đúng service**, có thể **gộp** nhiều lời gọi, và làm các việc chung ở "biên" (xác thực, TLS, giới hạn tốc độ, ghi log). Nó là một **vai trò**, nhiều loại phần mềm đảm nhiệm được.
**Ví dụ đời thường:** quầy lễ tân toà nhà: khách chỉ biết một cửa, lễ tân kiểm tra giấy tờ rồi chỉ đúng phòng.
**Use case:** client web và mobile của TeamBoard chỉ biết `https://api.teamboard/...`; gateway chuyển `/api/v1/boards/**` tới `board-service` và `/api/v1/tasks/**` tới `task-service`.

```
Client ──► [API Gateway] ──┬─► board-service
                           ├─► identity-service
                           └─► task-service
        (bắc-nam: client vào)      (đông-tây: service gọi nhau, thường KHÔNG qua gateway)
```

#### Phân biệt các khái niệm hay bị lẫn

| Khái niệm | Nó là gì | Khác API Gateway ở đâu |
|---|---|---|
| Reverse proxy | đứng trước server, chuyển request, có thể cache và TLS | gateway = reverse proxy **cộng** việc chéo (xác thực, rate limit, aggregation) |
| Load balancer | chia tải tới nhiều instance, health check | chủ yếu phân phối tải; không quản lý API (key, quota) |
| Service mesh | proxy sidecar cạnh mỗi service; mTLS, retry, telemetry giữa **service với service** | mesh lo đông-tây; gateway lo bắc-nam |
| BFF | mỗi loại client một backend riêng, do đội frontend sở hữu | là **cách tổ chức** gateway theo client |
| Ingress (Kubernetes) | khai báo host/path tới Service, cần controller | Ingress API đã đóng băng; Kubernetes khuyên Gateway API cho triển khai mới |

#### Hướng bắc-nam và đông-tây
Bắc-nam là lưu lượng từ client vào hệ thống; đông-tây là service gọi nhau. Gateway đứng ở bắc-nam.

## L2 Why — Vì sao không để client gọi thẳng từng service

Theo Azure Architecture Center, khi client gọi thẳng nhiều service thì:
1. client phải theo dõi nhiều endpoint và tự xử lý lỗi từng cái;
2. client dính chặt vào cách chia service, đổi hay tách service là phá client;
3. một thao tác cần nhiều lượt gọi nên tăng độ trễ;
4. mỗi service công khai phải tự lo xác thực, TLS, giới hạn tốc độ;
5. giao thức phía service bị bó vào thứ client dùng được;
6. nhiều endpoint công khai làm rộng bề mặt tấn công.

Gateway gom các việc đó vào một chỗ. **Cái giá:** thêm một hop (độ trễ), thêm một thành phần phải build, vận hành, và là **điểm lỗi đơn** nếu không scale.

## L3 How — Các cơ chế của một gateway

Mỗi cơ chế là một vấn đề riêng; một gateway cụ thể làm một phần trong số này.

#### a. Routing (định tuyến tầng 7)
**Là gì:** nhận request ở một endpoint chung, khớp theo path, host, method, header rồi chuyển tới service phù hợp.
**Use case:** `/api/v1/boards/**` → board-service; khi hiện đại hoá một monolith, đặt gateway trước nó rồi đưa dần API mới sang service mà client không đổi URL.
**Ưu:** tách client khỏi cách chia service. **Nhược:** bảng route là một nơi phải cập nhật mỗi khi thêm hay đổi service; thêm một hop.

#### b. Aggregation (gộp lời gọi)
**Là gì:** client gửi **một** request; gateway gọi nhiều service rồi gộp một phản hồi.
**Use case:** màn hình Board cần Board, thành viên và task: một lượt từ mobile thay vì ba lượt qua mạng di động.
**Ưu:** giảm "chatty". **Nhược:** gateway có thêm logic (một nhánh lỗi thì sao); dễ phình. Microsoft khuyên đặt aggregation phức tạp vào **service gộp riêng** đứng sau gateway.

#### c. Offloading (đẩy việc chéo ra khỏi service)
**Là gì:** làm một lần ở gateway các việc mọi service đều cần: TLS termination, xác thực (JWT), allow/block IP, rate limit, ghi log và trace, cache, nén, chuyển giao thức.
**Use case:** thay vì mỗi service tự kiểm JWT, gateway kiểm một lần rồi chuyển tiếp.
**Ưu:** service đơn giản hơn; chính sách đồng nhất. **Nhược:** gateway thành nơi tập trung rủi ro; **không** được nhét nghiệp vụ vào đó.

#### d. Cân bằng tải và health check
**Là gì:** khi một cụm có nhiều instance, gateway chọn instance theo chính sách (round robin, random, ít yêu cầu nhất) và ngừng gửi vào instance chết (health check chủ động hoặc thụ động).
**Use case:** ba instance `board-service`; một instance chết thì lưu lượng chuyển sang hai instance còn lại. Lab ở Part 3.

#### e. Timeout, retry, circuit breaker
**Là gì:** giới hạn thời gian chờ upstream; thử lại có kiểm soát; ngắt nhanh khi upstream lỗi liên tục.
**Use case:** upstream treo thì gateway trả `504` sau 30 giây thay vì giữ kết nối mãi.
**Rủi ro:** retry nhiều tầng nhân tải (Part 4, Lab 4.3).

#### f. Rate limit và load shedding
**Rate limit:** giới hạn từng người dùng hay khoá (N yêu cầu/giây) để chống lạm dụng thường ngày. **Load shedding:** bỏ bớt yêu cầu ưu tiên thấp dựa trên **trạng thái hệ thống** khi quá tải. Stripe dùng cả hai (Part 7).
**Use case:** chặn một IP gọi 1000 lần/giây vào API đọc Board.

#### g. Quan sát: correlation id và trace
**Là gì:** gateway gắn (hoặc giữ) một id đi xuyên mọi service (`X-Correlation-Id`, `traceparent`) để lần ra cả chuỗi.
**Use case:** người dùng báo lỗi, tìm một id trong log của cả gateway lẫn service.

#### h. Cách tổ chức: một gateway hay nhiều (BFF, theo domain)
Một gateway gom hết sẽ phình thành "monolith" nếu một đội sở hữu. Hai cách tách: **BFF** (mỗi loại client một gateway, đội client sở hữu) và **gateway theo domain** (mỗi domain nghiệp vụ một điểm vào; Uber DOMA). **Cái giá:** nhiều thành phần hơn, code trùng giữa các BFF.

| Cơ chế | Giải quyết | Rủi ro chính | Part |
|---|---|---|---|
| Routing | client dính vào cách chia service | bảng route lỗi thời | 2 |
| Aggregation | quá nhiều round trip | logic phình, một nhánh lỗi | – |
| Offloading | lặp xác thực/TLS/rate limit ở từng service | tập trung rủi ro | 4, 5 |
| Cân bằng tải + health check | một instance chết kéo cả cụm | cấu hình sai chặn nhầm | 3 |
| Timeout, retry, breaker | upstream chậm giữ kết nối | retry nhân tải | 4 |
| Rate limit, load shedding | lạm dụng, quá tải | chặn nhầm người dùng thật | 4 |
| Correlation, trace | không lần ra được chuỗi lỗi | log injection nếu tin giá trị client | 2, 5 |

Giữ gateway **mỏng**: routing, biên bảo vệ, quan sát; aggregation phức tạp và nghiệp vụ đặt ở service riêng.

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0 (YARP 2.3.0, upstream giả trên `127.0.0.1`). Lab đầu tiên chỉ để **thấy** vai trò gateway; chi tiết cấu hình ở Part 2.

#### Lab 1.1 — Một cửa vào, hai đích
Gateway có hai route: `/api/v1/boards/**` (GET, HEAD) và `/legacy/**` (bỏ tiền tố `/legacy`, thêm header `X-Gateway: lab`) tới cùng một upstream giả.
**Đầu ra thật** (rút gọn):
```text
GET /api/v1/boards/1?x=1          -> 200 | upstream thấy path=/api/v1/boards/1?x=1
GET /legacy/boards/2              -> 200 | upstream thấy path=/boards/2, X-Gateway=lab
GET /khong-co-route               -> 404 | (thân rỗng)
```
**Giải thích:** client chỉ biết địa chỉ gateway; gateway đổi path và thêm header trước khi chuyển; không route thì `404` rỗng (chưa có `problem+json`, Part 6).

## L7 Architecture — Chọn giải pháp, và khi không cần gateway

### Bản đồ giải pháp

API Gateway là một **vai trò**; có nhiều họ phần mềm đảm nhiệm. **YARP chỉ là một dòng.**

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá |
|---|---|---|---|
| Thư viện proxy trong ngôn ngữ của bạn | YARP (.NET, MIT), Ocelot | đội .NET, cần tuỳ biến bằng code | tự lo WAF, HA, portal, quota |
| Reverse proxy đa năng, tiến trình riêng | nginx, Envoy, HAProxy | hiệu năng cao, ngôn ngữ trung lập | nginx không có quản lý API; Envoy cấu hình phức tạp |
| Gateway có plugin và khám phá cấu hình | Kong, Traefik | cần plugin phong phú hoặc môi trường container đổi liên tục | một phần tính năng và giấy phép thương mại (Kong) |
| Dịch vụ managed (API management) | Amazon API Gateway, Azure API Management | ít vận hành, cần key, quota, portal | gắn vào một cloud, chi phí, khó giống local |
| Load balancer L7 managed | AWS ALB, Azure Application Gateway | phân tải và route theo path/host, WAF, TLS | không phải API management |
| Kubernetes-native | Gateway API, Ingress + controller | chạy trên Kubernetes | gắn với Kubernetes; Ingress đã đóng băng |
| Mesh ingress | Istio ingress gateway | đã chạy mesh | yếu hơn gateway chuyên dụng ở WAF, transform |
| **Không dùng gateway riêng** | client gọi thẳng, hoặc ALB/CDN phía trước | một service, một loại client | client dính vào service; lặp việc chéo từng service |

**Cách chọn:** (1) có nhiều service và nhiều loại client chưa? Một service thì chưa cần gateway; (2) cần quản lý API công khai (key, quota, portal) hay chỉ routing biên?; (3) chạy ở đâu (Kubernetes, AWS, tự host)?; (4) đội mạnh ngôn ngữ nào; (5) vận hành được bao nhiêu thành phần. Dự án này chọn YARP tự host vì đội .NET, một cửa vào mỏng, chạy giống nhau ở local và ECS; đánh đổi là tự lo WAF và HA (Part 6).

### Khi KHÔNG nên có gateway
- Chỉ một service hoặc một loại client: thêm gateway chỉ thêm độ phức tạp và một hop.
- Ứng dụng web phía server nhỏ: Microsoft ghi lời gọi trực tiếp có thể đủ.
- Không có người vận hành: gateway là điểm lỗi đơn cần theo dõi.

## Tự kiểm tra

1. Gateway khác load balancer thế nào? Khác service mesh thế nào?
2. Kể sáu vấn đề khi client gọi thẳng nhiều service.
3. Vì sao một gateway gom mọi service có thể thành vấn đề, và hai cách tách?
4. Với một service và một client web, bạn chọn gì (kể cả "không dùng")?

<details>
<summary>Đáp án ngắn</summary>

1. Gateway hiểu ứng dụng (route theo path, rate limit, xác thực); load balancer chủ yếu chia tải. Gateway lo bắc-nam, mesh lo đông-tây.
2. Xem L2.
3. Phình thành monolith, một đội sở hữu thành nút thắt; tách bằng BFF hoặc gateway theo domain.
4. Không dùng gateway riêng (hoặc ALB/CDN nếu cần TLS), vì chưa có vấn đề nào ở L2.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 (YARP 2.3.0, đầu ra thật).
**Đã tự mở:** Azure Architecture Center "API gateways" (sáu vấn đề, ba pattern); Microsoft .NET Microservices guide (gateway so với gọi trực tiếp); Uber DOMA (23/07/2020); Stripe "Scaling your API with rate limiters" (30/03/2017); Istio và Envoy (tài liệu kiến trúc).
**Chỉ qua agent (⚠):** Azure patterns Gateway Routing, Aggregation, Offloading, Backends for Frontends; samnewman.io BFF; microservices.io API Gateway; nginx, Envoy, Kong, Amazon API Gateway và Azure API Management docs; Kubernetes Gateway API và Ingress.
**Chưa kiểm:** phạm vi tính năng của bản mã nguồn mở Kong; bảng so sánh các sản phẩm ngoài YARP chỉ dựa vào tài liệu, chưa chạy.
