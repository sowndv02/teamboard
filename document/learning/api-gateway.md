# API Gateway

> Loại: Khái niệm · Công cụ trong dự án: [YARP](yarp.md) (chỉ là một trong các lựa chọn, xem [Bản đồ giải pháp](#bản-đồ-giải-pháp))
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: **mới có spec và design, chưa có code** (lát 1: [TB-010](../issues/TB-010-gateway-skeleton-routing.md), chờ TB-001)

## 1. Vấn đề nó giải quyết

Hệ thống có nhiều service (identity, board, task...). Nếu client gọi thẳng từng service thì (theo Azure Architecture Center, đã mở): client phải theo dõi nhiều endpoint và tự xử lý lỗi từng cái; client dính chặt vào cách chia service nên đổi/tách service là phá client; một thao tác cần nhiều lượt gọi nên tăng độ trễ; mỗi service công khai phải tự lo xác thực, SSL, giới hạn tốc độ; giao thức phía service bị bó vào thứ client dùng được; và nhiều endpoint công khai làm rộng bề mặt tấn công.

## 2. Là gì (hiểu trong 1 phút)

**API Gateway** là một điểm vào tập trung giữa client và các service: nhận request, **chuyển tới đúng service** (hoạt động như reverse proxy, định tuyến tầng 7), có thể **gộp** nhiều lời gọi thành một, và làm các việc chung ở "biên" (xác thực, TLS, giới hạn tốc độ, ghi log). Nó không phải một sản phẩm cụ thể mà là **vai trò** có thể do nhiều loại phần mềm đảm nhiệm.

```
Client ──► [API Gateway] ──┬─► board-service
                           ├─► identity-service
                           └─► task-service
        (bắc-nam: client vào)      (đông-tây: service gọi nhau, thường KHÔNG qua gateway)
```

**Phân biệt các khái niệm hay bị lẫn** (nguồn: Azure Architecture Center, microservices.io, tài liệu Kubernetes/Istio; chi tiết từng nguồn ở mục 7):

| Khái niệm | Nó là gì | Khác API Gateway ở đâu |
|---|---|---|
| **Reverse proxy** | Đứng trước server, chuyển request, có thể cache, TLS, load balancing | Gateway = reverse proxy **cộng** các việc chéo (xác thực, rate limit, aggregation); nhiều gateway dựng trên reverse proxy |
| **Load balancer (L4/L7)** | Chia tải tới nhiều instance, health check; ví dụ ALB là L7 route theo path/host/header | Chủ yếu phân phối tải; không quản lý API (key, quota, portal) |
| **Service mesh** (Istio, Linkerd) | Proxy sidecar cạnh mỗi service (data plane) + control plane; mTLS, retry, circuit breaking, telemetry giữa **service với service** | Mesh lo lưu lượng đông-tây; gateway lo bắc-nam. Azure ghi ingress gateway của mesh thường yếu hơn gateway chuyên dụng về WAF, productization API, transform |
| **BFF** (Backend for Frontend) | Mỗi loại client (web, mobile) có backend riêng, do đội frontend sở hữu | Là **cách tổ chức** gateway theo client; dùng khi các client cần khác nhau, không cần khi chỉ một client |
| **Ingress / Ingress controller** (Kubernetes) | Object khai báo host/path → Service, cần một controller cài riêng thực thi | Ingress API đã "frozen" (vẫn GA nhưng không phát triển thêm); Kubernetes khuyên **Gateway API** cho triển khai mới |

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ |
|---|---|---|
| Routing | Định tuyến tầng 7 (path, host, header) tới đúng service, tách client khỏi service | `/api/v1/boards/**` → board-service |
| Aggregation | Một request client → nhiều request tới service rồi gộp kết quả, giảm chatty | trang Board lấy cả thành viên và task |
| Offloading | Đẩy việc chéo khỏi từng service lên gateway | TLS termination, mTLS, xác thực, allow/block IP, rate limit, cache phản hồi, WAF, nén, tĩnh |
| Bắc-nam vs đông-tây | Lưu lượng client vào vs service gọi nhau | gateway vs mesh |
| Rate limiting vs load shedding | Chặn từng người dùng lạm dụng vs bỏ bớt request theo tình trạng hệ thống | xem Stripe ở mục 7 |
| Correlation id | Một id đi xuyên các service để lần ra cả chuỗi | `X-Correlation-Id` + `traceparent` |
| Điểm lỗi đơn | Gateway chết thì mất mọi API | cần scale ngang, health check |

### 3.1 Các cơ chế chính của một API Gateway

Theo Azure Architecture Center và .NET Microservices guide (đã mở), một gateway làm ba việc chính, mỗi việc là một pattern riêng; thêm cách **tổ chức** gateway (một hay nhiều) và hai cơ chế bảo vệ.

#### a. Routing (định tuyến, reverse proxy tầng 7) *(Chắc: Azure, Microsoft, đã mở)*
Gateway là reverse proxy: nhận request ở **một endpoint chung**, khớp theo path/host/header rồi chuyển tới service phù hợp.
```
Client ─► /api/v1/boards/**  ─► gateway ─► board-service
          /api/v1/tasks/**   ─►          └► task-service
```
- **Ưu:** tách client khỏi cách chia service (đổi/tách service không phá client); khi hiện đại hoá monolith, đặt gateway trước monolith rồi đưa dần API mới ra service mà client không đổi URL (Microsoft).
- **Nhược:** bảng route là một nơi phải được cập nhật mỗi khi thêm/đổi service (nên dùng IaC); gateway thêm một hop.
- **Dùng khi:** muốn tách client khỏi service. Đây là chức năng tối thiểu của mọi gateway.

#### b. Aggregation (gộp lời gọi) *(Chắc: Azure, Microsoft, đã mở)*
Client gửi **một** request; gateway gọi nhiều service, **gộp** kết quả rồi trả một phản hồi.
```
Client ─► gateway ─┬─► board-service ─┐
                   ├─► task-service  ─┼─► gộp ─► một phản hồi
                   └─► identity     ─┘
```
- **Ưu:** giảm "chatty": màn hình cần ba service thì không phải gọi ba lần qua Internet, quan trọng nhất cho client từ xa như mobile/SPA; gateway gọi song song trong mạng nội bộ nhanh.
- **Nhược:** gateway thêm logic (cách gộp, một trong ba lỗi thì sao); dễ phình thành nơi chứa nghiệp vụ. Microsoft nêu nhiều khi linh hoạt hơn là tạo **service gộp riêng** đứng trong phạm vi gateway thay vì cấu hình trong gateway.
- **Dùng khi:** một thao tác cần dữ liệu từ nhiều service và client ở xa; ít quan trọng với ứng dụng web phía server (độ trễ nội bộ nhỏ).

#### c. Offloading (đẩy việc chéo ra khỏi service) *(Chắc: Azure, Microsoft, đã mở)*
Các việc chung làm một lần ở gateway thay vì lặp ở mỗi service: TLS termination, mTLS, xác thực/phân quyền, danh sách IP cho/chặn, giới hạn tốc độ (throttling), ghi log/giám sát/trace, cache phản hồi, WAF, nén GZIP, phục vụ nội dung tĩnh, biến đổi header/query.
- **Ưu:** service đơn giản hơn; chính sách đồng nhất; bề mặt tấn công nhỏ hơn vì service nội bộ không phơi ra ngoài.
- **Nhược:** gateway thành nơi tập trung rủi ro (điểm lỗi đơn, nút cổ chai nếu không scale); từng sản phẩm gateway hỗ trợ khác nhau (Azure: mọi lựa chọn đều có routing L7 nhưng xác thực, rate limit, TLS khác nhau).
- **Dùng khi:** nhiều service cùng cần một chính sách biên.

#### d. Một gateway hay nhiều: BFF và gateway theo domain *(Chắc: Microsoft, Uber, đã mở)*
Một gateway duy nhất gom mọi service sẽ phình thành "monolith" và nút thắt phát triển nếu một đội sở hữu nó (Microsoft). Hai cách tách:
- **BFF (Backend for Frontend):** mỗi loại client (web, mobile) có gateway riêng, do đội của client sở hữu, API vừa cho client đó. Chỉ đáng khi các client cần khác nhau; một client thì không cần.
- **Gateway theo domain:** mỗi domain nghiệp vụ một gateway làm điểm vào duy nhất (Uber DOMA, mục 7).
- **Cái giá:** thêm số gateway phải vận hành; có thể trùng code giữa các BFF.

#### e. Rate limiting và load shedding *(Chắc: Stripe, đã mở)*
Hai cơ chế dễ lẫn: **rate limiting** giới hạn từng người dùng/khoá (ví dụ N request/giây) để chống lạm dụng thường ngày; **load shedding** bỏ bớt request ưu tiên thấp dựa trên **trạng thái hệ thống** khi quá tải, là biện pháp khẩn cấp để giữ request quan trọng sống. Stripe dùng cả hai tầng (mục 7). Chi tiết thuật toán (token bucket, sliding window) nằm ở ghi chú công cụ ([yarp.md](yarp.md)) và tài liệu của từng sản phẩm.

#### So sánh nhanh

| Cơ chế | Việc gateway làm | Giải quyết | Rủi ro chính |
|---|---|---|---|
| Routing | khớp và chuyển request | client dính vào cách chia service | bảng route lỗi thời |
| Aggregation | gọi nhiều service, gộp | quá nhiều round trip | logic phình, một nhánh lỗi |
| Offloading | làm việc chéo một lần | lặp xác thực/TLS/rate limit ở từng service | tập trung rủi ro |
| BFF / theo domain | nhiều gateway nhỏ | gateway phình, nút thắt | nhiều thành phần hơn |
| Rate limit / load shed | chặn lạm dụng, bỏ bớt khi quá tải | lạm dụng và sập dây chuyền | cấu hình sai chặn nhầm người dùng thật |

**Aggregation và gateway tập trung** là hai chỗ dễ làm gateway phình; giữ gateway mỏng (routing, biên bảo vệ, quan sát) và đặt aggregation phức tạp vào service riêng.

## 4. Cách dùng

**Quy trình chọn (rút gọn):**
1. Có thật nhiều service và nhiều loại client chưa? Một service thì không cần gateway.
2. Cần gì từ gateway: chỉ routing + vài chính sách biên (reverse proxy là đủ), hay quản lý API công khai (key, quota, portal: sản phẩm API management)?
3. Môi trường: Kubernetes (Ingress/Gateway API, mesh ingress), AWS/Azure (managed), hay tự host cạnh ứng dụng?
4. Đội mạnh về gì (.NET → YARP/Ocelot; hệ đa ngôn ngữ → Envoy/NGINX/Kong/Traefik)?
5. Giữ gateway **mỏng**: routing, biên bảo vệ, quan sát; không nghiệp vụ.

**Ví dụ tối thiểu, trung lập với công cụ** (cùng một ý "route theo path, chuyển tới đích", viết bằng NGINX; **Chưa kiểm**: chưa chạy trong dự án; YARP có bản tương đương ở [yarp.md](yarp.md)):

```nginx
location /api/v1/boards/ {
    proxy_pass http://board-service:7070;   # route theo tiền tố path tới một đích
}
```
Mong đợi: `GET /api/v1/boards/1` được chuyển tới board-service. Cùng ý này, mỗi công cụ cấu hình một kiểu (file cấu hình, JSON, annotation, CRD Kubernetes); khái niệm là route + đích + chính sách biên.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Client không phụ thuộc cách chia service; đổi/tách service không phá client | Thêm một hop (độ trễ) và một thành phần phải build, deploy, vận hành |
| Việc chung (xác thực, rate limit, log) làm một lần, nhất quán | Là điểm lỗi đơn và có thể thành nút cổ chai nếu không scale |
| Giảm round-trip nhờ aggregation, ẩn service nội bộ (giảm bề mặt tấn công) | Dễ phình thành "monolith"/coupling mọi service; một đội sở hữu thành nút thắt |
| Dịch giao thức public ↔ nội bộ | Cần chính sách đổi routing khi thêm/đổi service (nên dùng IaC) |

**Nên dùng khi:** nhiều service cần cửa vào chung và chính sách biên thống nhất, nhiều loại client.
**Không nên khi:** chỉ một service hoặc một client gọi trực tiếp là đủ (thêm gateway chỉ tăng độ phức tạp); thiếu người vận hành.

### Bản đồ giải pháp

API Gateway là một **vai trò**; có nhiều họ phần mềm đảm nhiệm. **YARP chỉ là một dòng trong bảng này.** Mức: Chắc nếu tôi tự mở trang; Vừa nếu chỉ agent đọc.

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn (mức) |
|---|---|---|---|---|
| **Thư viện proxy trong ngôn ngữ của bạn** | YARP (.NET, MIT, Microsoft duy trì), Ocelot (.NET, MIT, cấu hình aggregation/rate limit sẵn) | Đội .NET, cần tuỳ biến bằng code; chạy cùng nền tảng với service | Tự lo WAF, HA, portal, quota; ít tính năng dựng sẵn hơn sản phẩm API management | learn.microsoft.com YARP, github.com/ThreeMammals/Ocelot (Vừa) |
| **Reverse proxy đa năng chạy tiến trình riêng** | NGINX (BSD-2), Envoy (Apache-2.0; L3/L4 + L7, cấu hình động, retry, circuit breaking, dùng được cả làm edge proxy lẫn service proxy) | Hiệu năng cao, ngôn ngữ-trung lập, nền cho mesh/gateway K8s | NGINX không có quản lý API sẵn; Envoy cấu hình phức tạp, thường dùng qua lớp điều khiển khác | nginx.org; envoyproxy.io/docs (Envoy Chắc, đã mở; NGINX Vừa) |
| **Gateway có hệ plugin / tự khám phá cấu hình** | Kong Gateway (Apache-2.0 repo, có gói thương mại; phạm vi tính năng bản OSS **chưa xác minh**), Traefik (MIT; tự khám phá cấu hình từ Docker/K8s, cập nhật động) | Cần plugin phong phú hoặc môi trường container đổi liên tục | Kong có phần thương mại cần kiểm giấy phép; Traefik ít tính năng quản lý API | developer.konghq.com, doc.traefik.io (Vừa) |
| **Dịch vụ managed (API management)** | Amazon API Gateway (REST/HTTP/WebSocket, throttling token bucket nhiều tầng, 429), Azure API Management (gateway + portal + policy, có self-hosted gateway) | Muốn ít vận hành, cần quản lý vòng đời API công khai, serverless | Gắn vào một cloud; chi phí; khó giống hệt local; Azure APIM không tự làm load balancing | docs.aws.amazon.com, learn.microsoft.com (Vừa) |
| **Load balancer L7 managed** | AWS Application Load Balancer, Azure Application Gateway | Phân tải + route theo path/host/header, WAF, TLS | Không phải API management (không key/quota/portal) | docs.aws.amazon.com (Vừa) |
| **Kubernetes-native** | Gateway API (Gateway, GatewayClass, HTTPRoute), Ingress + controller | Chạy trên Kubernetes, muốn khai báo chuẩn | Gắn với Kubernetes; Ingress đã "frozen" | kubernetes.io (Vừa; ngày GA 31/10/2023 Chắc) |
| **Service mesh ingress** | Istio ingress gateway, Linkerd | Đã chạy mesh, muốn cửa vào chung mTLS/telemetry | Thường yếu hơn gateway chuyên dụng về WAF, transform, productization (Azure); mesh thêm độ phức tạp | istio.io (Chắc, đã mở), learn.microsoft.com Azure (Chắc, đã mở) |
| **Không dùng gateway riêng** | Client gọi trực tiếp, hoặc ALB/CDN đứng trước | Ít service, một loại client, ứng dụng web phía server | Coupling client-service, lặp việc chéo ở từng service (Microsoft: có thể đủ cho ứng dụng nhỏ) | learn.microsoft.com .NET Microservices (Chắc, đã mở) |

**Khi chọn, hỏi:** cần quản lý API công khai (key, quota, portal) hay chỉ routing biên? Chạy ở đâu (K8s, AWS, tự host)? Đội mạnh ngôn ngữ nào? Bao nhiêu client và service? Chi phí vận hành chấp nhận được? Dự án này chọn YARP vì lý do ở [yarp.md](yarp.md).

## 6. Lỗi hay gặp và cách tránh

1. **Nhét logic nghiệp vụ vào gateway**: phá ranh giới, thành nút thắt. Giữ mỏng; aggregation phức tạp đặt vào service riêng.
2. **Một gateway gom hết mọi thứ**: thành monolith. Tách theo client (BFF) hoặc theo domain.
3. **Gateway là điểm lỗi đơn không ai theo dõi**: cần scale ngang, health check, cảnh báo.
4. **Retry ở nhiều tầng**: gateway retry cộng với retry của service gọi nhau nhân tải khi sự cố (xem Google SRE ở mục 7).
5. **Tin `X-Forwarded-For` giả**: né rate limit theo IP. Chỉ tin proxy đã biết.
6. **Rate limit thay cho chống DDoS**: tài liệu .NET nêu rate limit không đủ chống DDoS; cần WAF/CDN (Vừa).
7. **Không đặt timeout**: một service chậm kéo cạn kết nối của gateway.

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **Microsoft (Azure Architecture Center)** | Client gọi trực tiếp nhiều service gặp 6 vấn đề (mục 1) | Gateway làm routing, aggregation, offloading; nêu các lựa chọn: reverse proxy (NGINX, HAProxy), mesh ingress gateway, Azure Application Gateway, Front Door, API Management | Khuyên ưu tiên giải pháp dựng sẵn của nền tảng; dùng gateway tuỳ biến chỉ khi thiếu linh hoạt; quản lý routing bằng IaC. Azure API Management không tự load balancing nên đi kèm LB | [API gateways](https://learn.microsoft.com/en-us/azure/architecture/microservices/design/gateway) (cập nhật 2026) | Chắc (đã mở) |
| **Microsoft (.NET Microservices guide)** | Client gọi trực tiếp: coupling, nhiều round trip, lộ bề mặt tấn công | Gateway/BFF, nhiều gateway theo client và ranh giới nghiệp vụ | Rủi ro tự nêu: phình thành monolith, điểm lỗi đơn, thêm hop, nút cổ chai, một đội sở hữu thành nút thắt | [Gateway vs direct](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/direct-client-to-microservice-communication-versus-the-api-gateway-pattern) | Chắc (đã mở) |
| **Uber (2020)** | Khoảng 2.200 microservice trọng yếu; "networked monolith"; tìm nguyên nhân gốc có khi qua khoảng 50 service của 12 đội | DOMA: khoảng 70 domain, 5 tầng, **mỗi domain một gateway** làm điểm vào duy nhất, extension thêm logic/dữ liệu không sửa lõi | Theo bài: onboarding giảm 25–50%; một tích hợp tính năng từ khoảng 3 ngày xuống 3 giờ; gateway giúp hai lần viết lại nền tảng lớn mà upstream không phải đổi. Đây là "gateway từng domain", không phải edge gateway | [DOMA](https://www.uber.com/en-GB/blog/microservice-architecture/) (23/07/2020) | Chắc (đã mở) |
| **Stripe (2017)** | Bảo vệ API khỏi lạm dụng và quá tải | 4 lớp: giới hạn tốc độ theo user (token bucket, Redis), giới hạn đồng thời, load shedder theo fleet, load shedder theo worker | Rate limit chống lạm dụng thường ngày; load shedding là biện pháp khẩn cấp theo trạng thái hệ thống | [Scaling your API with rate limiters](https://stripe.com/blog/rate-limiters) (30/03/2017) | Chắc (đã mở) |
| **Netflix Zuul** | Lưu lượng API khổng lồ từ nhiều loại thiết bị; cần lớp edge sửa nhanh khi sự cố | Filter ở edge: xác thực, định tuyến động, load shedding, static response; Zuul 2 trên Netty, quy tắc không block trong filter | Gateway là nơi gom việc chéo và công tắc vận hành. Bài blog gốc không mở được (403) | [Zuul wiki](https://github.com/Netflix/zuul/wiki) | Vừa (agent đã mở) |
| **Kubernetes Gateway API** (GA 31/10/2023) | Ingress hạn chế (HTTP, ít biểu đạt) | Gateway API: Gateway, GatewayClass, HTTPRoute theo vai trò; cho cả bắc-nam và đông-tây | Là hướng Kubernetes khuyên cho triển khai mới; Ingress đã "frozen". Trang tôi mở được phần đầu (ngày, thông báo GA), danh sách tài nguyên lên v1 chưa đọc tới | [Gateway API GA](https://kubernetes.io/blog/2023/10/31/Gateway-API-GA/) | Vừa (ngày Chắc) |
| **Google SRE** | Sự cố dây chuyền | Ví dụ sách: 3 tầng, mỗi tầng 4 lần thử → 4×4×4 = 64 lần thử lên backend quá tải | Không retry ở nhiều tầng; backoff có jitter, retry budget | [Addressing Cascading Failures](https://sre.google/sre-book/addressing-cascading-failures/) | Vừa (agent đã mở) |

**Mô hình phổ biến (Chắc, theo Microsoft và microservices.io):** một điểm vào làm routing, xác thực, rate limit, tracing; biến thể BFF cho từng loại client; ưu tiên giải pháp dựng sẵn của nền tảng.

**Khi nó thất bại hoặc bị lạm dụng:** gateway gom hết thành "monolith", điểm lỗi đơn, retry khuếch đại (Google SRE). Chưa tìm được postmortem công khai của một công ty chỉ vì gateway; ghi chú dựa vào nguyên lý có nguồn.

## 8. Trong TeamBoard

- Gateway là một trong 6 service bắt buộc, kiến trúc "API Gateway" (`PROJECT.md`, `document/architecture/README.md` mục 1); chỉ routing/authN/rate limit, **không có nghiệp vụ** (`.agents/rules/architecture.md`).
- Công cụ đã chọn: **YARP** tự host (design D0.1), lý do và chi tiết ở [yarp.md](yarp.md). Đây là quyết định của dự án, không phải khuyến nghị chung: mục 5 cho thấy các họ khác (managed, Envoy/NGINX...) phù hợp ở hoàn cảnh khác. `aws-service-selection` dùng khi chọn ALB/API Gateway ở giai đoạn 4.
- Spec/design/plan: [spec](../spec/api-gateway.md), [design](../design/api-gateway.md), [plan](../plans/api-gateway-slices.md); pattern: [patterns-catalog.md](../architecture/patterns-catalog.md) mục 3.5.
- `api-endpoints.md` bắt buộc không có tên service trong path, khác ví dụ slide tham khảo.
- BFF: chỉ cần khi có client đặc thù; hiện không có frontend (ADR 0008/0009).

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Mức |
|---|---|---|---|---|
| 1 | [yarp.md](yarp.md) | Nội bộ | công cụ dự án chọn | Chắc |
| 2 | [design/api-gateway.md](../design/api-gateway.md), [spec](../spec/api-gateway.md) | Nội bộ | quyết định và yêu cầu | Chắc |
| 3 | https://learn.microsoft.com/en-us/azure/architecture/microservices/design/gateway | Chính thức | vai trò, lựa chọn, cách chọn | Chắc (đã mở) |
| 4 | https://microservices.io/patterns/apigateway.html | Pattern | ưu/nhược | Vừa (agent đã mở) |
| 5 | https://istio.io/latest/docs/ops/deployment/architecture/ | Chính thức | mesh vs gateway | Chắc (đã mở) |
| 6 | https://www.envoyproxy.io/docs/envoy/latest/intro/what_is_envoy | Chính thức | Envoy là gì | Chắc (đã mở) |
| 7 | https://kubernetes.io/docs/concepts/services-networking/gateway/ | Chính thức | Gateway API | Vừa (agent đã mở) |

## 10. Tự kiểm tra / thực hành

1. Gateway khác load balancer ở điểm nào? Đúng khi nêu: gateway hiểu ứng dụng (route theo path, rate limit, xác thực), load balancer chủ yếu chia tải.
2. Gateway khác service mesh thế nào? Dùng "bắc-nam" và "đông-tây" để trả lời, và nêu khi nào dùng cả hai.
3. Vì sao một gateway gom mọi service có thể trở thành vấn đề, và hai cách tách?
4. Đối chiếu bản đồ giải pháp: với đội .NET một người, một service và chưa có client ngoài, bạn chọn gì (kể cả "không dùng")? Với một API công khai cần key và quota thì sao?
5. Việc chạy được: đọc `document/design/api-gateway.md` và chỉ ra quyết định D0.1 chọn họ nào trong bản đồ ở mục 5 và vì sao họ khác bị loại.
