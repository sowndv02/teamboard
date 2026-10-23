# Container

> Loại: Khái niệm · Công cụ trong dự án: [Docker và Docker Compose](docker-compose.md) (chỉ là một trong các lựa chọn, xem [Bản đồ giải pháp](#bản-đồ-giải-pháp))
> Làm mới lần cuối: 2026-10-23 · Trạng thái dự án: `teamboard-api` đã có Dockerfile và compose; production dự kiến ECS Fargate (ADR 0003)

## 1. Vấn đề nó giải quyết

"Chạy được trên máy tôi" nhưng lỗi trên máy khác: khác phiên bản runtime, thiếu thư viện hệ thống, khác biến môi trường. Giải pháp cũ là máy ảo (VM) cho mỗi ứng dụng: cô lập tốt nhưng nặng (mỗi VM mang cả hệ điều hành), khởi động chậm, tốn tài nguyên. Chạy nhiều ứng dụng trên cùng một máy thì lại giẫm chân nhau (xung đột phiên bản, tranh CPU/RAM).

## 2. Là gì (hiểu trong 1 phút)

**Container** là một tiến trình Linux bình thường được **bọc lại** để nó (1) chỉ **thấy** một phần hệ thống: filesystem riêng, mạng riêng, cây tiến trình riêng; và (2) chỉ **dùng được** một lượng CPU/RAM nhất định. Nó **dùng chung kernel** với máy chủ, khác VM có kernel riêng. Ứng dụng và thư viện của nó đóng gói thành một **image** bất biến; chạy image ra container.

```
VM:         [App][Libs][Guest OS + kernel]  ×N  ──► Hypervisor ──► phần cứng
Container:  [App][Libs]  ×N  ──► Container runtime ──► MỘT kernel của host ──► phần cứng
```

Ba cơ chế nền (Linux; theo man page `namespaces(7)` và `cgroups(7)`, agent đã đọc): **namespaces** quyết định tiến trình "thấy gì" (tám loại: cgroup, IPC, network, mount, PID, time, user, UTS), **cgroups** quyết định "dùng được bao nhiêu" (controller cho cpu, memory, pids, io), và **filesystem từ image** (các layer chồng lên nhau) cung cấp thư mục gốc riêng. Container là sự kết hợp của ba thứ đó, không phải một tính năng đơn lẻ của kernel.

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ |
|---|---|---|
| Image | Khuôn chỉ đọc gồm các layer: ứng dụng + thư viện + cấu hình khởi động | `mcr.microsoft.com/dotnet/aspnet` |
| Layer | Một bước thay đổi filesystem, bất biến, chia sẻ giữa image | mỗi lệnh `RUN` trong Dockerfile tạo một layer |
| Container | Instance đang chạy của image, thêm một lớp ghi riêng | `docker run ...` |
| Tag vs digest | Tag là nhãn có thể bị gán lại (mutable); digest là SHA-256 của nội dung, luôn trỏ đúng một image | `image:1.2` vs `image@sha256:...` |
| Registry | Nơi lưu và phân phối image | Docker Hub, Amazon ECR, GHCR |
| Container runtime | Phần mềm thực sự tạo và chạy container (từ cấp cao tới cấp thấp) | containerd, CRI-O, runc |
| OCI | Chuẩn mở về runtime, image, distribution | tránh phụ thuộc một công cụ |
| Điều phối (orchestrator) | Hệ thống lập lịch, mở rộng, tự hồi phục nhiều container trên nhiều máy | Kubernetes, ECS |
| Rootless | Chạy container không cần quyền root trên host (dùng user namespace) | Podman |

**OCI, giải thích kỹ.** Sau khi Docker phổ biến, mỗi công cụ có định dạng image và cách chạy riêng, nguy cơ mỗi nhà một kiểu. Open Container Initiative (22/06/2015, dưới Linux Foundation, Docker và CoreOS khởi xướng) đặt ba chuẩn: **runtime-spec** (cách chạy một "bundle": thư mục gốc + file cấu hình gồm lệnh chạy, mount, namespace, cgroup), **image-spec** (manifest, layer, config) và **distribution-spec** (API phân phối image). Hệ quả thực tế: image build bằng công cụ này chạy được trên runtime khác miễn tuân chuẩn. `runc` là chương trình cấp thấp tạo và chạy container theo runtime-spec (Apache-2.0); các runtime cấp cao như containerd gọi nó.

**Tag mutable, giải thích kỹ.** `myapp:latest` hôm nay và ngày mai có thể là hai image khác nhau, nên cùng một lệnh deploy cho kết quả khác nhau và rollback khó. Digest ghim đúng một nội dung; image đa nền tảng còn có manifest list với digest riêng cho từng kiến trúc. Registry như Amazon ECR cho bật "tag immutability" để push lại tag đã có bị từ chối.

### 3.1 Các cơ chế chính bên trong một container

Container không phải một tính năng đơn lẻ mà là **ghép** các cơ chế sau. Nguồn: man page `namespaces(7)`, `cgroups(7)` và tài liệu Docker/OCI (agent đã đọc, mức Vừa trừ khi ghi khác).

#### a. Namespaces: quyết định tiến trình "thấy gì"
Mỗi namespace cho tiến trình bên trong cảm giác có bản **riêng** của một tài nguyên toàn cục. Linux có tám loại:

| Namespace | Cô lập | Hệ quả với container |
|---|---|---|
| PID | ID tiến trình | tiến trình đầu tiên trong container là PID 1; không thấy tiến trình của host |
| Mount | điểm mount, filesystem | có thư mục gốc riêng (từ image) |
| Network | thiết bị mạng, stack, cổng | địa chỉ IP và cổng riêng; cần ánh xạ cổng ra host |
| UTS | hostname, tên miền NIS | container có hostname riêng |
| IPC | System V IPC, POSIX message queue | không chia sẻ hàng đợi với host |
| User | UID/GID | root trong container có thể ánh xạ về user thường trên host (rootless) |
| Cgroup | thư mục gốc cgroup | container chỉ thấy cây cgroup của mình |
| Time | đồng hồ boot và monotonic | hiếm dùng |

- **Giới hạn:** namespace chỉ **ẩn** chứ không **hạn chế dùng**; cô lập vẫn nằm trên cùng một kernel.

#### b. Cgroups (control groups): quyết định tiến trình "dùng được bao nhiêu"
Nhóm tiến trình theo cây để **giới hạn và theo dõi** tài nguyên; mỗi loại tài nguyên do một controller xử lý: `cpu`, `memory`, `pids` (số tiến trình), `io`.
- **cgroup v1:** mỗi controller có thể gắn vào một cây riêng. **cgroup v2:** một cây thống nhất cho mọi controller, có quy tắc "no internal processes".
- **Ví dụ:** `docker run --memory=64m` đặt giới hạn bộ nhớ; vượt thì tiến trình bị kernel dừng (OOM). Chưa chạy lại trong phiên này.
- **Giới hạn:** không giới hạn thì một container có thể ăn hết CPU/RAM của host.

#### c. Filesystem phân lớp từ image (layer + lớp ghi)
Image gồm các **layer bất biến** xếp chồng thành thư mục gốc; mỗi container thêm một lớp ghi riêng ở trên cùng; layer được chia sẻ giữa các image (Docker docs, mức Vừa).
- **Ưu:** tiết kiệm đĩa và băng thông, build nhanh nhờ cache layer. **Nhược:** dữ liệu ghi vào lớp container mất khi container bị xoá (cần volume); layer cũ vẫn chứa nội dung bạn "xoá" ở layer sau (vì vậy secret nhét vào layer vẫn lộ).

#### d. Runtime nhiều tầng (cao → thấp)
Một container chạy qua nhiều tầng: **công cụ cho người dùng** (Docker CLI, Podman, nerdctl) → **runtime cấp cao** (containerd, CRI-O: kéo image, quản vòng đời) → **runtime cấp thấp** (runc, tạo namespace/cgroup theo OCI runtime-spec). Kubernetes nói chuyện với runtime cấp cao qua giao diện **CRI**. Hệ quả: đổi công cụ phía trên không nhất thiết đổi image hay runtime phía dưới (mục 5, bản đồ).

#### e. Chuẩn OCI (ba spec) *(Chắc: opencontainers.org, đã mở)*
**runtime-spec** (cách chạy một bundle), **image-spec** (manifest, layer, config), **distribution-spec** (API phân phối image). Là lý do image portable giữa công cụ.

#### Tóm tắt

| Cơ chế | Trả lời câu hỏi | Nếu thiếu thì |
|---|---|---|
| Namespaces | Tiến trình thấy gì? | thấy và can thiệp được tiến trình/mạng của host |
| Cgroups | Dùng được bao nhiêu? | một container làm đói cả host |
| Layer + lớp ghi | Hệ thống file từ đâu? | không có môi trường đóng gói tái lập |
| Runtime nhiều tầng | Ai tạo và quản lý? | không có vòng đời, không tích hợp được điều phối |
| OCI | Có chạy được ở nơi khác? | phụ thuộc một công cụ |

## 4. Cách dùng

**Thấy tận mắt namespace và cgroup** (minh hoạ chung, **Chưa kiểm**: cần máy Linux; chưa chạy trong phiên này):

```bash
# Tiến trình trong PID namespace riêng chỉ thấy chính nó
sudo unshare --fork --pid --mount-proc bash -c 'ps aux'
# Giới hạn tài nguyên bằng cgroup thông qua một runtime (ở đây là Docker)
docker run --rm --memory=64m --cpus=0.5 alpine sh -c 'cat /sys/fs/cgroup/memory.max'
```
Mong đợi: lệnh đầu chỉ liệt kê vài tiến trình; lệnh sau in giá trị khoảng 67108864 (64 MiB) nếu máy dùng cgroup v2.

**Dùng thực tế**: viết Dockerfile (hoặc tương đương) → build image → đẩy lên registry → chạy bằng một runtime hoặc điều phối. Chi tiết với Docker và Compose nằm ở [docker-compose.md](docker-compose.md).

**Quy trình chọn công cụ:** (1) máy dev: Docker/Podman; (2) build trong CI: BuildKit hoặc Buildah; (3) production: giao cho điều phối (ECS/Kubernetes) thay vì tự chạy; (4) ghim base image theo digest, chạy không root.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Môi trường nhất quán từ dev tới production | Chung kernel nên **cô lập yếu hơn VM**: lỗ hổng kernel có thể ảnh hưởng mọi container trên host (suy ra từ việc dùng chung kernel; chưa tìm được tài liệu bảo mật chính chủ nói thẳng) |
| Nhẹ và khởi động nhanh hơn VM (theo Docker: image cỡ chục MB so với chục GB) | Cần học thêm: image, registry, mạng, volume, điều phối |
| Dùng tài nguyên hiệu quả (nhiều container trên một máy) | Dữ liệu trong container mất khi container bị xoá nếu không dùng volume |
| Đóng gói chuẩn (OCI), nhiều công cụ cùng dùng được | Bề mặt tấn công mới: image độc hại, secret nằm trong layer, chạy root |

**Nên dùng khi:** nhiều dịch vụ cần triển khai nhất quán, cần scale, CI/CD tự động.
**Không nên khi:** cần cô lập mức hypervisor giữa các bên không tin nhau (cân nhắc VM hoặc micro-VM), ứng dụng đơn giản chạy ổn một chỗ, GPU/phần cứng đặc thù chưa được hỗ trợ tốt.

### Bản đồ giải pháp

Container là một khái niệm có nhiều lớp công cụ; **Docker không phải duy nhất**. Cột "Nguồn" ghi mức tôi tự mở (Chắc) hay chỉ qua agent (Vừa).

**Chạy và quản lý container (runtime/engine):**

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn |
|---|---|---|---|---|
| Engine đầy đủ cho nhà phát triển (daemon + CLI + build) | Docker Engine / Docker Desktop | Máy dev, quen thuộc, hệ sinh thái lớn | Daemon chạy quyền root theo mặc định; Docker Desktop có điều kiện giấy phép trả phí cho tổ chức lớn (xem `dependency-policy.md` mục 7) | docs.docker.com (Vừa) |
| Daemonless, rootless | Podman | Máy dev/server cần rootless, không muốn daemon root; CLI tương thích Docker | Hệ sinh thái Compose/Desktop khác; cần kiểm tương thích từng trường hợp | github.com/containers/podman (Vừa) |
| Runtime cấp cao để nhúng | containerd (+ CLI `nerdctl`) | Làm runtime cho node Kubernetes hoặc nhúng vào nền tảng; dùng containerd trực tiếp | Không phải công cụ cuối cho người dùng; `nerdctl` là dự án phụ | containerd.io, github.com/containerd/nerdctl (Vừa) |
| Runtime tối giản chỉ cho Kubernetes | CRI-O | Node Kubernetes cần runtime gọn | Không có CLI cho dev, chỉ phục vụ kubelet | cri-o.io (Vừa) |

**Build image:**

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn |
|---|---|---|---|---|
| Builder hiện đại, cache tốt | BuildKit (backend build của Docker) | CI cần cache/ song song | Cần hiểu cấu hình cache | github.com/moby/buildkit (Vừa) |
| Build không daemon | Buildah | CI không có daemon; build từ script hoặc Containerfile | Công cụ riêng cần học | buildah.io (Vừa) |
| Build trong cluster không cần daemon | Kaniko | (từng) build ngay trong pod Kubernetes | **Repo đã archived từ 03/06/2025, không còn phát triển**; không chọn cho dự án mới | github.com/GoogleContainerTools/kaniko (Chắc, đã mở) |

**Điều phối nhiều container:**

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn |
|---|---|---|---|---|
| Điều phối mã nguồn mở đa năng | Kubernetes | Cần hệ sinh thái lớn, di động giữa cloud | Phức tạp vận hành; bản managed trên AWS (EKS) tính phí control plane, ADR 0003 nêu khoảng 73 USD/tháng chưa tính node (chưa kiểm giá hiện hành); tự dựng thì tốn công vận hành | kubernetes.io (Vừa) |
| Điều phối quản lý hoàn toàn của AWS | Amazon ECS (EC2 hoặc Fargate) | Đã ở AWS, muốn ít vận hành; Fargate không phải quản server | Gắn chặt vào AWS | docs.aws.amazon.com/AmazonECS (Vừa) |
| Điều phối tích hợp sẵn Docker | Docker Swarm | Cụm nhỏ, đã dùng Docker, cần đơn giản | Hệ sinh thái nhỏ hơn Kubernetes | docs.docker.com/engine/swarm (Vừa) |
| Điều phối linh hoạt, chạy cả không-container | HashiCorp Nomad | Cần lập lịch cả ứng dụng không container; một binary | Giấy phép BUSL-1.1 (xem `dependency-policy.md`, hạng cần phê duyệt hoặc cấm); ghép Consul/Vault để đủ chức năng | developer.hashicorp.com/nomad (Vừa) |
| **Không dùng container** | VM, PaaS, serverless functions | Ứng dụng đơn giản; cần cô lập mạnh; tải thất thường | Mất tính nhất quán đóng gói của container (VM) hoặc mất kiểm soát runtime (PaaS) | – |

## 6. Lỗi hay gặp và cách tránh

- **Dùng tag `latest`/tag mutable.** Triệu chứng: build không tái lập, rollback không chắc. Cách tránh: ghim digest (Docker docs khuyến nghị ghim bằng digest).
- **Secret trong layer.** Triệu chứng: ai có image đều đọc được token. Nguyên nhân: truyền qua `ARG`/`ENV` khi build ("persist in the final image" theo Docker docs). Cách tránh: `docker build --secret` + `RUN --mount=type=secret` (xem [docker-compose.md](docker-compose.md)).
- **Chạy root trong container.** Cách tránh: `USER` không đặc quyền; rootless nếu có thể.
- **Nhầm container với VM.** Coi cô lập như hypervisor trong khi chung kernel. Cách tránh: cập nhật kernel/host, giới hạn capability, cân nhắc VM khi cần cô lập mạnh.
- **Mất dữ liệu khi xoá container.** Cách tránh: volume hoặc dữ liệu ở dịch vụ ngoài (DB).
- **Phụ thuộc công cụ đã archive/hết hạn** (ví dụ Kaniko). Cách tránh: kiểm trạng thái dự án và giấy phép trước khi ghim vào pipeline (`dependency-policy.md`).

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (đã mở) | Mức |
|---|---|---|---|---|---|
| **Google Borg** (paper EuroSys 2015) | Chạy hàng trăm nghìn job từ hàng nghìn ứng dụng, cụm tới hàng chục nghìn máy | Cluster manager với admission control, task packing, over-commitment, chia sẻ máy với cách ly mức tiến trình, đặc tả job khai báo | Hiệu suất sử dụng cao; paper nêu kinh nghiệm vận hành. Trang tóm tắt không nhắc Kubernetes, nên quan hệ với Kubernetes là suy luận | [Borg](https://research.google/pubs/large-scale-cluster-management-at-google-with-borg/) (Verma và cộng sự, 2015) | Chắc (đã mở) |
| **OCI** (2015) | Nguy cơ phân mảnh định dạng/runtime sau khi Docker phổ biến | Docker, CoreOS và các bên lập OCI dưới Linux Foundation; ba spec runtime, image, distribution | Image tuân chuẩn chạy trên nhiều runtime, giảm lock-in | [opencontainers.org](https://opencontainers.org/about/overview/) | Chắc (đã mở) |
| **Kaniko / Google** (archive 2025) | Công cụ build trong cluster không cần daemon | Repo chính thức ghi "archived, no longer developed or maintained" từ 03/06/2025 | Bài học vòng đời công cụ: một công cụ phổ biến vẫn có thể bị ngừng; kiểm trạng thái trước khi phụ thuộc | [github.com/GoogleContainerTools/kaniko](https://github.com/GoogleContainerTools/kaniko) | Chắc (đã mở) |
| **Kubernetes bỏ dockershim** | Docker Engine ra đời trước CRI nên Kubernetes giữ một lớp adapter | Trang FAQ cho thấy việc gỡ được công bố từ v1.20 và có cập nhật theo v1.24; **lý do cụ thể và tác động lên image chưa đọc được trong phần trang đã mở**. Kiến thức chung (Chưa kiểm): kubelet chuyển sang gọi thẳng containerd/CRI-O vì duy trì adapter là gánh nặng; image build bằng Docker vẫn chạy vì theo chuẩn OCI | Bài học: tách runtime khỏi công cụ dev nhờ chuẩn mở | [Dockershim FAQ](https://kubernetes.io/blog/2022/02/17/dockershim-faq/) (17/02/2022) | Vừa |
| **Bài về Borg, Omega, Kubernetes** (ACM Queue 2016) | Bài học từ ba hệ thống quản lý container của Google | Tổng kết sau khoảng một thập kỷ | Bản đầy đủ trả 403, chỉ đọc được trang tóm tắt | [research.google](https://research.google/pubs/borg-omega-and-kubernetes/) | Vừa (agent đã mở) |

Hai case thực hành chính thức khác về secret trong build, Compose trong production và giới hạn pull Docker Hub nằm ở bảng của [docker-compose.md](docker-compose.md) mục 7.

**Mô hình phổ biến (Chắc, theo Docker docs đã mở):** cùng một image đi từ dev → CI → production; Compose cho local/test, điều phối cho production.

**Khi nó thất bại hoặc bị bỏ:** Kaniko bị archive; Docker Swarm có hệ sinh thái nhỏ hơn Kubernetes (so sánh chung, Vừa). Chưa tìm được postmortem công khai đã mở về sự cố thoát khỏi container; nên không nêu.

## 8. Trong TeamBoard

- Công cụ đang dùng: Docker + Compose cho local (`src/docker-compose.yml`), chi tiết ở [docker-compose.md](docker-compose.md).
- Production: **ECS Fargate** (ADR 0003, `document/architecture/adr/0003-ecs-fargate.md`) thay vì Kubernetes; lý do theo ADR: khớp lộ trình khoá, ít thành phần hơn Kubernetes, chi phí thấp hơn (ADR nêu control plane EKS khoảng 73 USD/tháng chưa tính node, chưa kiểm lại giá hiện hành). Đánh đổi ADR ghi rõ: không học Kubernetes. Xem thêm README kiến trúc mục 5 và skill `aws-service-selection`.
- Chính sách: Docker Desktop có thể cần gói trả phí khi tổ chức vượt ngưỡng; Docker Engine/CLI/Compose dùng Apache-2.0 (`.agents/rules/dependency-policy.md` mục 7); Nomad (BUSL) thuộc hạng cần xem xét, Kaniko không nên dùng cho CI mới.
- Quy tắc Docker: `.agents/rules/docker.md`.

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Mức |
|---|---|---|---|---|
| 1 | [docker-compose.md](docker-compose.md) | Nội bộ | Docker/Compose cụ thể, Dockerfile của dự án | Chắc |
| 2 | `.agents/rules/docker.md`, ADR 0003 | Nội bộ | luật và lựa chọn runtime | Chắc |
| 3 | https://opencontainers.org/about/overview/ | Chuẩn | OCI | Chắc (đã mở) |
| 4 | https://research.google/pubs/large-scale-cluster-management-at-google-with-borg/ | Paper | Borg | Chắc (đã mở) |
| 5 | man7.org: `namespaces(7)`, `cgroups(7)` | Chính thức | cơ chế nền Linux | Vừa (agent đã mở) |
| 6 | https://kubernetes.io/docs/concepts/overview/ | Chính thức | vì sao cần điều phối | Vừa (agent đã mở) |

## 10. Tự kiểm tra / thực hành

1. Vì sao container khởi động nhanh hơn VM, và cái giá của việc chung kernel là gì?
2. Một nhóm dùng `myapp:latest` và hôm nay deploy ra kết quả khác hôm qua. Giải thích bằng tag/digest và đề xuất cách sửa.
3. Podman không có daemon còn Docker có. Nêu một hệ quả bảo mật và một hệ quả vận hành của khác biệt này.
4. Đối chiếu bản đồ ở mục 5: nếu CI không có Docker daemon, bạn chọn công cụ build nào và vì sao không chọn Kaniko?
5. Việc chạy được: chạy lệnh `docker run --rm --memory=64m alpine cat /sys/fs/cgroup/memory.max` (nếu có Docker) và ghi kết quả thật; không có Docker thì ghi "không chạy được".
