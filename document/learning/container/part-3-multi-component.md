# Part 3 — Multi-component (Container)

> [!NOTE]
> **Mục tiêu:** hiểu **điều phối** làm gì và vì sao: trạng thái mong muốn so với thực tế, vòng reconcile, khởi động lại và backoff, rolling update, probe, và chọn giữa Kubernetes, ECS, Nomad hay không điều phối.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** giải thích vì sao một container chết được thay thế, tính được số bản sẵn sàng tối thiểu của một rolling update, phân biệt liveness và readiness, chọn công cụ điều phối cho 3 tình huống.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Điều phối và các khái niệm của nó

#### Điều phối (orchestrator)
**Là gì:** hệ thống lập lịch, mở rộng và tự hồi phục nhiều container trên nhiều máy theo một **mô tả mong muốn** (ví dụ "chạy 3 bản của `board-service`").
**Use case:** một node chết, các container trên đó được tạo lại ở node khác mà không ai can thiệp.

#### Trạng thái mong muốn và thực tế
Bạn khai báo **mong muốn** (số bản, phiên bản image, tài nguyên); hệ thống liên tục so với **thực tế** và sửa cho khớp.

#### Probe và sẵn sàng
Cách hệ thống biết một container còn sống (liveness) và sẵn sàng nhận lưu lượng (readiness).

#### Rolling update
Thay phiên bản mới dần dần, giữ đủ số bản sẵn sàng suốt quá trình.

## L2 Why — Vì sao cần điều phối

Chạy hai ba container bằng tay thì được. Chạy hàng chục dịch vụ trên nhiều máy thì ai thay container chết, ai phân bố lên máy còn chỗ, ai cập nhật không gián đoạn, ai biết bản nào đang khoẻ? Borg (Google, EuroSys 2015) là hệ thống như vậy ở quy mô hàng trăm nghìn job: khai báo job, kiểm soát nhận việc, nhồi task, chia sẻ máy với cô lập mức tiến trình, rút ngắn thời gian phục hồi lỗi. Điều phối đổi lấy **một hệ thống phức tạp để vận hành** (Part 7).

## L3 How — Cơ chế đáng nhớ

### Vòng reconcile (control loop)
Kubernetes mô tả controller là một **vòng lặp không kết thúc**, như bộ điều nhiệt: `spec` chứa trạng thái mong muốn; controller đưa trạng thái hiện tại về phía đó. Cụm có thể không bao giờ "yên", và điều đó bình thường miễn controller còn chạy. Nomad có cùng ý: **job** là trạng thái mong muốn; **evaluation** là sự kiện kích hoạt reconcile; **allocation** là việc đặt task lên một node. ECS: service scheduler giữ đúng số task mong muốn và khởi động task thay thế khi một task lỗi hay không qua health check.

```
vòng lặp:  quan sát thực tế ──► so với mong muốn ──► hành động (tạo, xoá, thay) ──► lặp
```
**Hệ quả:** hệ thống **tự hồi phục** (Lab 3.1), nhưng chỉ khi mong muốn được khai báo; mọi chỉnh sửa tay trái với khai báo sẽ bị vòng lặp sửa lại.

### Restart và backoff
Container chết thì khởi động lại, nhưng khởi động ngay lập tức làm quá tải khi nó chết liên tục (Lab 3.2). Kubernetes dùng **backoff mũ**: 10 giây, 20, 40... trần 300 giây (5 phút), và đặt lại sau khi container chạy 10 phút không lỗi (CrashLoopBackOff). ECS cũng giới hạn tốc độ khởi động khi task liên tục không lên được RUNNING và phát sự kiện dịch vụ.

### Probe
- **Liveness:** thất bại quá ngưỡng thì container bị **khởi động lại**.
- **Readiness:** thất bại thì container **bị bỏ khỏi danh sách nhận lưu lượng** (không khởi động lại).
- **Startup:** chạy trước; cho tới khi qua, liveness và readiness chưa chạy.
- Cơ chế: `exec`, `httpGet`, `tcpSocket`, `grpc`; chu kỳ mặc định 10 giây.
- **Bẫy:** liveness quá nhạy làm container bị giết đúng lúc quá tải; liveness phụ thuộc DB làm cả cụm khởi động lại khi DB chậm.

### Rolling update
Khi đổi image, tạo bản mới và xoá bản cũ dần, giới hạn bởi hai tham số: bao nhiêu bản **được thiếu** và bao nhiêu bản **được dư**. Kubernetes Deployment: `maxUnavailable` mặc định 25% (làm tròn xuống) và `maxSurge` mặc định 25% (làm tròn lên); với mặc định thì trong lúc cập nhật có ít nhất 75% và nhiều nhất 125% số bản mong muốn; `progressDeadlineSeconds` mặc định 600. ECS (REPLICA): `minimumHealthyPercent` mặc định 100 và `maximumPercent` mặc định 200; có **deployment circuit breaker** tự rollback về lần triển khai hoàn tất gần nhất khi quá nhiều task lỗi (ngưỡng mặc định 50% số task mong muốn, kẹp trong khoảng 3 đến 200).
- **Bản mới hỏng:** một rolling update tốt **dừng lại** thay vì thay hết (Lab 3.3); phải có rollback hay cảnh báo, nếu không nó kẹt ở trạng thái nửa cũ nửa mới.
- **Recreate:** xoá hết rồi tạo mới: đơn giản nhưng **có thời gian chết** (Lab 3.3).

### Khám phá dịch vụ
Container thay đổi địa chỉ liên tục nên cần tên ổn định: Kubernetes Service có tên DNS (`my-svc.my-namespace.svc.cluster.local`) trỏ tới ClusterIP; headless Service trả danh sách địa chỉ các Pod; ECS có Service Connect hoặc Cloud Map. Tên ngắn chỉ có phạm vi trong namespace của chính nó.

### Các công cụ điều phối (khái niệm)
| Công cụ | Đơn vị | Điểm nổi bật | Cái giá |
|---|---|---|---|
| Kubernetes | Pod | hệ sinh thái lớn, di động giữa cloud | phức tạp vận hành; control plane managed tính phí |
| Amazon ECS (EC2 hoặc Fargate) | task, service | ít vận hành, tích hợp AWS; Fargate không quản server | gắn AWS |
| Nomad | job, allocation | lập lịch cả ứng dụng không container; một binary | giấy phép BUSL-1.1 (từ 2023) |
| Docker Swarm | service | tích hợp Docker Engine, tự hồi phục, rolling update, mTLS | hệ sinh thái nhỏ hơn |

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0 nhưng **không dùng Docker**. Đây là **mô hình reconciler** (.NET, "container" là đối tượng trong bộ nhớ, thời gian mô phỏng theo tick 100 ms): nó chứng minh *cơ chế* vòng lặp, backoff và rolling update, **không** chứng minh hành vi của Kubernetes hay ECS thật.

#### Lab 3.1 — Tự hồi phục: desired = 3, một container bị giết ở tick 10
**Đầu ra thật:**
```text
không có vòng reconcile: số sẵn sàng [t9:3 t10:2 t11:2 ... t16:2] -> KHÔNG BAO GIỜ hồi phục (sau 40 tick vẫn thiếu)
có vòng reconcile:       số sẵn sàng [t9:3 t10:3 t11:3 ... t16:3] -> đủ 3 trở lại ở tick 11 (100 ms sau khi chết)
```
**Giải thích:** không có vòng lặp, thiếu một bản mãi mãi; có vòng lặp, chỗ trống được lấp ngay ở lần reconcile kế tiếp (thời gian "100 ms" là độ phân giải của mô hình, **không** phải độ trễ thật; container thay thế còn cần thời gian sẵn sàng).

#### Lab 3.2 — Container luôn chết ngay khi chạy: restart ngay và backoff mũ
Cơ sở 100 ms, trần 1600 ms, 10 giây mô phỏng (tỉ lệ thu nhỏ từ 10 giây và 300 giây của Kubernetes).
**Đầu ra thật:**
```text
restart ngay (không backoff):               1000 lần khởi động trong 10 giây; 8 lần đầu ở ms: 0, 10, 20, 30, 40, 50, 60, 70
backoff mũ 100, 200, 400, 800, 1600 (trần): 10 lần khởi động trong 10 giây;   8 lần đầu ở ms: 0, 100, 300, 700, 1500, 3100, 4700, 6300
```
**Giải thích:** không backoff thì container hỏng bị khởi động 1000 lần (tốn CPU, kéo theo dependency); backoff mũ giảm còn 10 lần, khoảng cách tăng dần rồi chạm trần. Đơn vị thời gian và trần là của mô hình.

#### Lab 3.3 — Rolling update 4 bản v1 → v2 (maxSurge = 1, maxUnavailable = 1; v2 sẵn sàng sau 3 tick)
**Đầu ra thật:**
```text
rolling, v2 tốt:   sẵn sàng thấp nhất 3/4; hoàn tất ở tick 9; cuối: v2 x4, sẵn sàng 4/4
rolling, v2 HỎNG:  sẵn sàng thấp nhất 3/4; KHÔNG hoàn tất; cuối: v1 x3, v2 x2 (v2 không sẵn sàng), sẵn sàng 3/4
recreate (xoá hết rồi tạo mới), v2 tốt: sẵn sàng thấp nhất 0/4 (3 tick); hoàn tất ở tick 4; cuối: v2 x4, sẵn sàng 4/4
```
**Giải thích:** rolling giữ ít nhất 3/4 bản sẵn sàng suốt cập nhật; khi v2 **hỏng**, nó **dừng** với ba bản v1 còn phục vụ và hai bản v2 chờ mãi, chứ không thay hết (đó là bảo vệ, nhưng cần cảnh báo hoặc rollback tự động như `progressDeadlineSeconds` và circuit breaker của ECS để không kẹt). Recreate nhanh hơn nhưng có 3 tick (300 ms mô phỏng) với 0 bản sẵn sàng, tức là thời gian chết.

## L5 Troubleshooting — Khi điều phối không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Container được tạo lại liên tục | crash loop, thoát ngay (backoff tăng dần) | số lần restart, log, mã thoát | sửa nguyên nhân; xem backoff có đang giãn |
| Rolling update kẹt ở nửa chừng | bản mới không bao giờ sẵn sàng (Lab 3.3) | trạng thái rollout, readiness | rollback; deadline và cảnh báo tự động |
| Có thời gian chết khi cập nhật | chiến lược recreate, hoặc `maxUnavailable` quá lớn | xem chiến lược và tham số | rolling với `maxUnavailable` thấp, readiness đúng |
| Lưu lượng vào bản chưa sẵn sàng | thiếu readiness, hoặc readiness sai | xem probe | readiness kiểm đúng điều kiện nhận việc |
| Cả cụm khởi động lại khi DB chậm | liveness phụ thuộc DB | xem liveness | liveness chỉ kiểm tiến trình; phụ thuộc đặt ở readiness |
| Sửa tay container bị mất | vòng reconcile sửa lại theo khai báo | so khai báo và thực tế | sửa khai báo, không sửa tay |
| Dịch vụ A không gọi được B bằng tên ngắn | khác namespace | thử tên đầy đủ | dùng tên đủ (`b.ns`) |
| Task ECS liên tục không lên được | cấu hình sai, thiếu quyền, hết tài nguyên | sự kiện dịch vụ, log task | sửa cấu hình; circuit breaker rollback |

## L7 Architecture — Chọn điều phối, và khi không cần

| Nhu cầu | Chọn | Cái giá |
|---|---|---|
| Một hai container trên một máy | chạy trực tiếp hoặc Compose | không tự hồi phục khi máy chết |
| Cụm nhỏ, đã dùng Docker, cần đơn giản | Docker Swarm | hệ sinh thái nhỏ hơn |
| Đã ở AWS, ít vận hành | ECS (Fargate) | gắn AWS (dự án chọn: ADR 0003) |
| Hệ sinh thái lớn, di động giữa cloud | Kubernetes | phức tạp; control plane managed tính phí |
| Cần lập lịch cả ứng dụng không container | Nomad | giấy phép BUSL-1.1; ghép Consul/Vault |
| Tải thất thường, xử lý sự kiện | serverless (Lambda) | giới hạn runtime |
| **Không điều phối** | PaaS, hoặc một VM chạy Compose | tự thay thế container chết |

**Cách chọn:** hỏi (1) mấy dịch vụ và mấy máy, (2) cần tự hồi phục và cập nhật không gián đoạn không, (3) đã ở nhà cung cấp nào, (4) ai vận hành. ADR 0003 chọn ECS Fargate thay vì Kubernetes: khớp lộ trình, ít thành phần hơn, chi phí thấp hơn; đánh đổi: không học Kubernetes.

## Tự kiểm tra

1. Vì sao cụm tự thay container chết, và điều đó đòi hỏi gì ở khai báo?
2. Backoff mũ giải quyết vấn đề gì (Lab 3.2)?
3. Rolling update với 4 bản, `maxUnavailable` = 1: tối thiểu bao nhiêu bản sẵn sàng? Khi bản mới hỏng thì sao?
4. Liveness và readiness khác nhau ở hệ quả nào?

<details>
<summary>Đáp án ngắn</summary>

1. Vòng reconcile so thực tế với mong muốn; cần khai báo mong muốn (số bản, image).
2. Tránh khởi động liên tục một container hỏng gây quá tải (1000 so với 10 lần ở Lab 3.2).
3. 3 bản; rolling dừng với bản cũ còn phục vụ (Lab 3.3), cần rollback hoặc cảnh báo.
4. Liveness thất bại: khởi động lại container; readiness thất bại: bỏ khỏi danh sách nhận lưu lượng, không khởi động lại.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 đến 3.3 là **mô hình** .NET 10.0.13 (đầu ra thật của mô hình, không phải của Kubernetes hay ECS).
**Đã tự mở:** Borg paper (EuroSys 2015, trang tóm tắt); các tài liệu Kubernetes, ECS, Nomad, Swarm do agent đọc (không tự mở lại).
**Chỉ qua agent (⚠):** Kubernetes `controller`, `deployment` (`maxUnavailable` và `maxSurge` 25%, `progressDeadlineSeconds` 600), `liveness-readiness-startup-probes`, `pod-lifecycle` (backoff 10s, 20s, 40s trần 300s, đặt lại sau 10 phút), `dns-pod-service`; ECS `ecs_services`, `API_DeploymentConfiguration` (100 và 200), `deployment-circuit-breaker` (50%, 3 đến 200), `fargate-tasks-services`; Nomad `scheduling` và LICENSE (BUSL-1.1); Docker Swarm `engine/swarm`.
**Chưa kiểm:** `failureThreshold`, `timeoutSeconds`, `initialDelaySeconds` mặc định của probe; hành vi Kubernetes hay ECS thật; bài "Borg, Omega, and Kubernetes" (ACM Queue 2016) chỉ đọc được trang tóm tắt; Borg ngoài trang tóm tắt chưa đọc.
