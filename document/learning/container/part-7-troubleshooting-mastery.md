# Part 7 — Troubleshooting & Mastery (Container)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán sự cố container từ triệu chứng, học từ các case thực tế (Borg, OCI, dockershim, các lỗ hổng runc), rồi tự thiết kế và bảo vệ một cách đóng gói và chạy dịch vụ.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống, trả lời được câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: container chạy một lúc rồi biến mất, mã thoát 137

Dịch vụ .NET chạy ổn vài giờ rồi tự khởi động lại; `docker inspect` báo mã 137.
<details>
<summary>Chẩn đoán</summary>

OOM killer của cgroup (`memory.max`) hoặc bị `SIGKILL` (Part 5). Kiểm `OOMKilled` và `memory.events`; với .NET, GC heap mặc định chỉ phủ 75% giới hạn container, bộ nhớ native và stack nằm ngoài nên tổng vẫn có thể vượt. Sửa: tăng giới hạn hay giảm `HeapHardLimitPercent`, tìm rò bộ nhớ, đặt cảnh báo trên số lần restart.
</details>

#### Tình huống 2: `docker stop` mất đúng 10 giây rồi mã 137

Mỗi lần dừng container đều chờ lâu và kết thúc bằng 137, không bao giờ 143.
<details>
<summary>Chẩn đoán</summary>

PID 1 không xử lý `SIGTERM` (Linux không có hành vi mặc định cho PID 1), nên phải chờ `SIGKILL` (Part 2). Hay gặp khi lệnh chạy qua `sh -c` làm shell là PID 1. Sửa: dạng `exec` (như `ENTRYPOINT ["dotnet", ...]`), bắt `SIGTERM` trong ứng dụng, hoặc `--init`.
</details>

#### Tình huống 3: deploy hôm nay khác hôm qua

Cùng lệnh `docker pull app:latest` và `run`, nhưng hành vi khác; rollback không biết quay về bản nào.
<details>
<summary>Chẩn đoán</summary>

Tag là con trỏ có thể di chuyển (Part 2). Sửa: ghim digest, tag bất biến ở registry, ghi digest đã triển khai.
</details>

#### Tình huống 4: image phình, và secret vẫn lộ dù đã `rm`

Một Dockerfile `COPY` token rồi `RUN rm`, nhưng ai tải image cũng đọc được token.
<details>
<summary>Chẩn đoán</summary>

Layer là changeset: `rm` chỉ thêm tệp whiteout, nội dung vẫn nằm trong layer dưới (Lab 4.1). Sửa: không đưa secret vào layer (`--secret` lúc build), xoay token đã lộ, dùng multi-stage để loại file tạm.
</details>

#### Tình huống 5: rolling update kẹt nửa chừng

Sau một lần phát hành, hai bản mới chờ mãi, ba bản cũ vẫn phục vụ; không ai được báo.
<details>
<summary>Chẩn đoán</summary>

Bản mới không bao giờ sẵn sàng (readiness lỗi); rolling update dừng để bảo vệ (Lab 3.3), nhưng thiếu deadline và cảnh báo nên kẹt. Sửa: kiểm lý do không sẵn sàng, rollback; bật deployment circuit breaker (ECS) hoặc `progressDeadlineSeconds` (Kubernetes) và cảnh báo.
</details>

## L7 Architecture — Trong thực tế

Mức: **đã mở** là tự mở nguồn gốc; **⚠ qua agent** là agent nghiên cứu đã đọc, chưa tự đối chiếu.

| Công ty / dự án | Bối cảnh và vấn đề | Cách làm | Kết quả, bài học | Nguồn (mức) |
|---|---|---|---|---|
| **Google Borg (2015)** | hàng trăm nghìn job từ hàng nghìn ứng dụng; cụm tới hàng chục nghìn máy | cluster manager: admission control, nhồi task, over-commitment, chia sẻ máy với cô lập mức tiến trình, job khai báo | hiệu suất sử dụng cao; paper có kinh nghiệm khoảng một thập kỷ; quan hệ với Kubernetes là suy luận | [Borg](https://research.google/pubs/large-scale-cluster-management-at-google-with-borg/) (EuroSys 2015, đã mở trang tóm tắt) |
| **OCI (22/06/2015)** | nguy cơ phân mảnh định dạng và runtime sau khi Docker phổ biến | Docker, CoreOS và các bên lập OCI dưới Linux Foundation; ba spec runtime, image, distribution | image tuân chuẩn chạy trên nhiều runtime, giảm lock-in | [opencontainers.org](https://opencontainers.org/about/overview/) (đã mở) |
| **Kubernetes bỏ dockershim** | Docker Engine ra đời trước CRI nên Kubernetes giữ một adapter | công bố gỡ ở v1.20 (02/12/2020); gỡ ở v1.24 (phát hành 03/05/2022) | image build bằng Docker vẫn chạy vì theo OCI; kubelet gọi thẳng containerd hay CRI-O | [Don't Panic](https://kubernetes.io/blog/2020/12/02/dont-panic-kubernetes-and-docker/), [Dockershim FAQ](https://kubernetes.io/blog/2022/02/17/dockershim-faq/) (đã mở một phần) |
| **Kaniko (archive 03/06/2025)** | công cụ build trong cluster không daemon | repo chính thức ghi "archived, no longer developed or maintained" | công cụ phổ biến vẫn có thể bị ngừng; kiểm trạng thái trước khi phụ thuộc | [github.com/GoogleContainerTools/kaniko](https://github.com/GoogleContainerTools/kaniko) (đã mở) |
| **Docker Hub rate limits** | pull không giới hạn gây tải | 100 lượt mỗi 6 giờ (chưa đăng nhập), 200 (miễn phí đăng nhập), không giới hạn (gói trả phí) | CI cần đăng nhập, mirror hay cache | [docs.docker.com/docker-hub/usage](https://docs.docker.com/docker-hub/usage/) (⚠ qua agent) |
| **Google, "Borg, Omega, Kubernetes" (2016)** | bài học từ ba hệ thống của Google | tổng kết sau khoảng một thập kỷ | chỉ đọc được trang tóm tắt; bản đầy đủ (ACM Queue) trả 403 | [research.google](https://research.google/pubs/borg-omega-and-kubernetes/) (đã mở trang tóm tắt) |

### Khi container là nơi xảy ra sự cố bảo mật

| Sự cố | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **runc CVE-2019-5736 (công bố 11 và 12/02/2019)** | container độc hại ghi đè `runc` của host, thành root host; cần chạy root image của kẻ tấn công hay `docker exec` vào container nó từng ghi | không chạy image không tin cậy bằng root; user namespace đúng cách chặn được; cập nhật runc | [oss-security (A. Sarai)](https://seclists.org/oss-sec/2019/q1/119) (đã mở) |
| **Leaky Vessels (31/01/2024)** | bốn CVE: runc `CVE-2024-21626` (rò file descriptor, thoát ra host; ảnh hưởng runc 1.0.0-rc93 đến 1.1.11, vá ở 1.1.12) và ba lỗi BuildKit | runtime và công cụ build là bề mặt tấn công; theo dõi và vá nhanh | [GHSA-xr7r-f8xq-vfvv](https://github.com/advisories/GHSA-xr7r-f8xq-vfvv) (đã mở); [Snyk](https://labs.snyk.io/resources/leaky-vessels-docker-runc-container-breakout-vulnerabilities/) (đã mở) |
| **OOMKilled (thiết kế, không phải sự cố có ngày)** | container vượt giới hạn bộ nhớ bị dừng với `exitCode: 137` và bị khởi động lại liên tục | đặt giới hạn, hiểu bộ nhớ ngoài GC heap | [Kubernetes: assign memory](https://kubernetes.io/docs/tasks/configure-pod-container/assign-memory-resource/) (đã mở qua agent) |

**Mô hình phổ biến:** cùng một image đi từ dev tới CI tới production; Compose cho local, điều phối cho production; runtime cấp cao tách khỏi công cụ dev nhờ OCI.
**Chưa tìm được nguồn gốc đã mở cho:** số liệu "2 tỷ container mỗi tuần" của Google (chỉ thấy qua tìm kiếm, bài HPCwire chưa mở), các số về Spotify, và Docker, Netflix, Shopify (không có nguồn chính); không nêu các con số đó. Phiên bản runc được vá cho CVE-2019-5736 chưa kiểm được (nguồn đã mở không nêu số phát hành).

### Đánh đổi khi thiết kế

- **Nhẹ so với cô lập:** container nhẹ vì chung kernel; cái giá là cô lập yếu hơn VM; mã không tin cậy cần micro-VM hay VM.
- **Một công cụ so với chuẩn mở:** Docker tiện, nhưng image và runtime theo OCI giúp đổi công cụ.
- **Tự vận hành so với managed:** Kubernetes cho hệ sinh thái nhưng phức tạp; ECS Fargate ít vận hành nhưng gắn AWS; PaaS đỡ hơn nữa nhưng mất kiểm soát.
- **Bất biến so với gỡ lỗi:** container dùng một lần dễ thay thế, khó "vào sửa tay"; phải dựa vào log, trace và `exec` có kiểm soát.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Container khác máy ảo ở đâu? Vì sao khởi động nhanh? Cái giá của việc chung kernel?
2. Namespace và cgroup khác nhau thế nào? Kể ba loại namespace và hệ quả.
3. Một image gồm những gì? Digest khác DiffID thế nào?
4. Vì sao `rm` ở layer sau không bảo vệ secret?
5. Mã thoát 137 và 143 nghĩa là gì, và liên quan thế nào tới PID 1?
6. Điều phối làm gì? Giải thích vòng reconcile và rolling update.
7. Liveness và readiness khác nhau thế nào, và bẫy của liveness?
8. Khi nào **không** nên dùng container?

### Bài mở

#### Bài 1 — Thiết kế cách đóng gói và chạy `task-service` trên ECS Fargate
Một service .NET dùng PostgreSQL, nhận event từ SQS. Thiết kế: Dockerfile (base, user, healthcheck), cấu hình bằng môi trường và secret, giới hạn tài nguyên (cỡ task Fargate, `HeapHardLimitPercent`), tín hiệu dừng, probe, rolling update và rollback, ghim digest, quét và SBOM. **Tiêu chí:** mỗi quyết định trỏ tới một Lab hoặc case; nêu ít nhất hai phương án bị loại kèm lý do; nêu phần suy luận chưa kiểm.

#### Bài 2 — Đánh giá mô hình bảo mật của một container cho trước
Cho một container chạy root, `--privileged`, gắn `docker.sock`, mở mọi cổng. Dùng Part 5 và các CVE ở trên để liệt kê rủi ro, mức độ, và cách sửa từng cái; đề xuất khi nào cần micro-VM thay vì container. **Tiêu chí:** mỗi rủi ro trỏ tới Lab 5.1 đến 5.3 hoặc một CVE; nêu cái nào sửa rẻ nhất.

#### Bài 3 — Phản biện một thiết kế
Đồng nghiệp đề xuất "dùng tag `latest`, lưu dữ liệu trong container cho đỡ phức tạp, bỏ giới hạn bộ nhớ để khỏi bị OOM, chạy root cho khỏi lỗi quyền, và chạy luôn Kubernetes cho hai service". Viết phản biện nêu ít nhất 5 rủi ro đã học và phương án thay thế. **Tiêu chí:** mỗi rủi ro trỏ tới một Lab hay case.

### Tiêu chí thành thạo

- Giải thích không nhìn tài liệu: container không phải VM nhỏ, tám namespace, cgroup v2 và OOM, cấu trúc image và whiteout, PID 1 và mã thoát, vòng reconcile, rolling update.
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có số đo hoặc lab của chính bạn và nêu được giới hạn của nó.
- Nói được ít nhất ba tình huống không nên dùng container (hoặc phải dùng VM hay micro-VM).

## Tự kiểm tra

1. Vì sao tắt seccomp không đủ để `unshare` chạy trong container (Lab 5.3)?
2. CVE-2019-5736 cần điều kiện nào để bị khai thác, và biện pháp nào chặn được?
3. Rolling update dừng ở nửa chừng khi bản mới hỏng: đó là lỗi hay bảo vệ? Cần thêm gì?

<details>
<summary>Đáp án ngắn</summary>

1. Container không có `CAP_SYS_ADMIN`; capability chặn độc lập với seccomp.
2. Chạy lệnh bằng root trong container từ image do kẻ tấn công kiểm soát (hoặc `docker exec` vào container nó từng ghi); user namespace đúng cách chặn được; cập nhật runc.
3. Bảo vệ; cần deadline, cảnh báo và rollback tự động (circuit breaker của ECS, `progressDeadlineSeconds`).
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** Borg (trang tóm tắt), OCI overview, Dockershim FAQ (17/02/2022), Kaniko (archive 03/06/2025), advisory runc GHSA-xr7r-f8xq-vfvv (31/01/2024), oss-security về CVE-2019-5736 (12/02/2019), Snyk Leaky Vessels. (Các trang này do agent đọc trong phiên này; những trang đã mở ở các phiên trước được ghi ở Part 1.)
**Chỉ qua agent (⚠):** Docker Hub usage, "Don't Panic" (02/12/2020), Kubernetes 1.24 release (03/05/2022), kubernetes.io `assign-memory-resource`, `Borg, Omega, Kubernetes` (trang tóm tắt).
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ cơ chế đã học và từ lab, không phải sự cố đã gặp ở dự án này.
