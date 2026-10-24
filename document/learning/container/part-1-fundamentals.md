# Part 1 — Fundamentals & Mental Models (Container)

> [!NOTE]
> **Mục tiêu:** hiểu container là gì (và không là gì), vì sao nó nhẹ hơn máy ảo và cái giá của việc chung kernel, ba cơ chế nền, và chọn có dùng container hay không.
> **Cần biết trước:** [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** giải thích container không dùng "máy ảo nhỏ", đọc được bằng chứng cô lập từ `/proc`, chọn giữa VM, container, serverless cho 3 tình huống.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Container và các khái niệm nền

#### Container
**Là gì:** một **tiến trình Linux bình thường** được bọc lại để nó (1) chỉ **thấy** một phần hệ thống (filesystem, mạng, cây tiến trình riêng) và (2) chỉ **dùng được** một lượng tài nguyên nhất định. Nó **dùng chung kernel** với máy chủ; ứng dụng và thư viện đóng gói thành **image** bất biến, chạy image ra container.
**Ví dụ đời thường:** phòng họp có kính mờ và hạn giờ: người trong phòng không thấy phòng khác và chỉ được dùng một khung giờ, nhưng cả toà nhà vẫn chung một hệ thống điện (kernel).
**Use case:** chạy `board-service` và `task-service` trên cùng một máy với phiên bản .NET và thư viện khác nhau mà không xung đột.

```
VM:         [App][Libs][Guest OS + kernel]  ×N  ──► Hypervisor ──► phần cứng
Container:  [App][Libs]  ×N  ──► Container runtime ──► MỘT kernel của host ──► phần cứng
```

#### Image, layer, registry
**Image:** khuôn chỉ đọc gồm các **layer** (mỗi layer là một bộ thay đổi filesystem) cộng cấu hình khởi động. **Registry:** nơi lưu và phân phối image. Chi tiết cấu trúc ở [Part 4](part-4-advanced.md).
**Use case:** build một lần, chạy giống nhau ở máy dev, CI và production.

#### Namespace và cgroup
**Namespace:** quyết định tiến trình "thấy gì". **Cgroup:** quyết định "dùng được bao nhiêu".

#### Runtime và điều phối
**Runtime:** phần mềm thực sự tạo và chạy container (containerd, runc). **Điều phối (orchestrator):** hệ thống lập lịch, mở rộng, tự hồi phục nhiều container trên nhiều máy (Kubernetes, ECS). Ở [Part 3](part-3-multi-component.md).

## L2 Why — Vì sao container

"Chạy được trên máy tôi" nhưng lỗi trên máy khác: khác phiên bản runtime, thiếu thư viện hệ thống, khác biến môi trường. Máy ảo giải được nhưng nặng: mỗi VM mang cả một hệ điều hành, khởi động chậm, tốn tài nguyên. Chạy nhiều ứng dụng trên cùng máy thì giẫm chân nhau.

Container giữ được **môi trường đóng gói nhất quán** với chi phí nhỏ vì không có kernel riêng: nó chỉ là một tiến trình có tầm nhìn và hạn mức riêng (Lab 1.1). **Cái giá:** cô lập **yếu hơn** VM vì chung kernel (một lỗ hổng kernel có thể ảnh hưởng mọi container trên host, Part 5); cần học thêm image, registry, mạng, volume, điều phối.

## L3 How — Các cơ chế nền

Container không phải một tính năng đơn lẻ của kernel mà là **ghép** các cơ chế sau.

#### a. Namespaces: tiến trình "thấy gì"
**Là gì:** mỗi namespace cho tiến trình bên trong cảm giác có bản **riêng** của một tài nguyên toàn cục. Linux có tám loại.
**Use case:** PID namespace làm tiến trình đầu tiên của container là PID 1 và không thấy tiến trình của host.

| Namespace | Cô lập | Hệ quả với container |
|---|---|---|
| PID | ID tiến trình | tiến trình đầu tiên là PID 1; không thấy tiến trình của host |
| Mount | điểm mount | có thư mục gốc riêng (từ image) |
| Network | thiết bị mạng, stack, cổng | IP và cổng riêng; cần ánh xạ cổng ra host |
| UTS | hostname | container có hostname riêng |
| IPC | System V IPC, POSIX message queue | không chia sẻ hàng đợi với host |
| User | UID và GID | root trong container có thể ánh xạ về user thường trên host (rootless) |
| Cgroup | gốc cây cgroup | container chỉ thấy cây cgroup của mình |
| Time | đồng hồ boot và monotonic | hiếm dùng |

Mỗi tiến trình có các tệp `/proc/<pid>/ns/*`; `clone` tạo tiến trình mới trong namespace mới, `unshare` đưa tiến trình hiện tại vào namespace mới, `setns` vào namespace có sẵn. Tạo hầu hết namespace cần `CAP_SYS_ADMIN`; riêng user namespace không cần đặc quyền (từ kernel 3.8). **Giới hạn:** namespace chỉ **ẩn**, không **hạn chế dùng**.

#### b. Cgroups: tiến trình "dùng được bao nhiêu"
**Là gì:** nhóm tiến trình theo cây để **giới hạn và đo** tài nguyên qua các controller (`cpu`, `memory`, `pids`, `io`).
**Use case:** đặt `memory.max` để một container rò bộ nhớ không làm sập cả máy.
- **v1:** mỗi controller có thể gắn vào một cây riêng; **v2:** một cây thống nhất, và quy tắc "no internal process": ngoài gốc, tiến trình chỉ nằm ở cgroup lá.
- `memory.max` là giới hạn cứng: vượt mà không thu hồi được thì OOM killer chạy **trong chính cgroup đó**, không giết tiến trình ngoài; `memory.high` chỉ làm chậm và không gọi OOM killer.
- **Giới hạn:** không giới hạn thì một container có thể ăn hết CPU và RAM của host.

#### c. Filesystem phân lớp từ image
**Là gì:** image là các layer bất biến xếp chồng thành thư mục gốc; mỗi container thêm một lớp ghi riêng ở trên cùng; layer được chia sẻ giữa các image.
**Use case:** hai image cùng base chỉ lưu base một lần.
- **Ưu:** tiết kiệm đĩa và băng thông, build nhanh nhờ cache layer. **Nhược:** dữ liệu ghi vào lớp container mất khi container bị xoá; layer cũ vẫn chứa nội dung bạn "xoá" ở layer sau (Lab 4.1).

#### d. Container chạy qua nhiều tầng
Công cụ cho người dùng (Docker CLI, Podman, nerdctl) → runtime cấp cao (containerd, CRI-O) → runtime cấp thấp (runc theo OCI runtime-spec). Chi tiết ở [Part 4](part-4-advanced.md).

| Cơ chế | Trả lời câu hỏi | Nếu thiếu thì |
|---|---|---|
| Namespaces | tiến trình thấy gì? | thấy và can thiệp được tiến trình, mạng của host |
| Cgroups | dùng được bao nhiêu? | một container làm đói cả host |
| Layer và lớp ghi | hệ thống file từ đâu? | không có môi trường đóng gói tái lập |
| Runtime nhiều tầng | ai tạo và quản lý? | không có vòng đời, không tích hợp điều phối |
| Chuẩn OCI | chạy được ở nơi khác không? | phụ thuộc một công cụ |

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0 (Docker 29.5.3, `alpine:latest`, container tên `lh-*`). Lab Docker cho namespaces, cgroups và vòng đời đã chạy ở lộ trình Docker: [Lab 1.3 (namespaces)](../docker/part-1-fundamentals.md), [Lab 1.4 (cgroups)](../docker/part-1-fundamentals.md), [Lab 1.5 (vòng đời)](../docker/part-1-fundamentals.md); không chạy lại. Lab dưới đây đọc `/proc` từ **bên trong** container.

#### Lab 1.1 — Container là một tiến trình có tầm nhìn riêng
`docker run --rm --name lh-proc alpine sh -c '...'` đọc `/proc`:
**Đầu ra thật** (rút gọn):
```text
hostname: c3f951de60f7                     (hostname = ID container, do UTS namespace)
PID 1: sh -c <chính lệnh được truyền vào>   (tiến trình đầu tiên là lệnh của bạn, không phải init của hệ điều hành)
tiến trình thấy được:  PID 1 (sh -c ...), PID 11 (ps)     (chỉ hai tiến trình, không thấy tiến trình của host)
/proc/self/status:     Name: grep   Pid: 12   NSpid: 12
namespace:  cgroup, ipc, mnt, net, pid, pid_for_children, time, time_for_children, user, uts  (mỗi cái một số inode)
cgroup: 0::/                               (cgroup v2; gốc cây của riêng container nhờ cgroup namespace)
kernel: 6.18.33.1-microsoft-standard-WSL2
rootfs: NAME="Alpine Linux", VERSION_ID=3.24.3
```
**Giải thích:** container chỉ thấy hai tiến trình; PID 1 chính là lệnh được truyền vào, nên cũng là tiến trình nhận tín hiệu dừng (Part 2). Namespace `user` có số `4026531837` (số thấp, là namespace gốc của hệ thống): container này **không** dùng user namespace riêng, nghĩa là root trong container là root thật trong máy ảo Linux (Part 5). `cgroup: 0::/` cho biết cgroup v2.

#### Lab 1.2 — Container chung kernel với container khác
```text
docker run --rm alpine uname -r            -> 6.18.33.1-microsoft-standard-WSL2
(lặp lại lần thứ hai)                       -> 6.18.33.1-microsoft-standard-WSL2
docker run --rm --pid=host alpine ...       -> thấy 15 tiến trình (của máy ảo Linux của Docker Desktop, không phải của Windows)
```
**Giải thích:** mọi container trên máy này chạy trên **một** kernel Linux (của máy ảo WSL2), không có kernel riêng mỗi container. Trên Windows, "container Linux" nghĩa là tiến trình Linux trong một máy ảo, nên khởi động engine chậm nhưng mỗi container sau đó nhẹ.

## L7 Architecture — Chọn container, VM, serverless, hay không

### Bản đồ giải pháp

Container là **một lựa chọn** trong dải cách đóng gói và chạy ứng dụng; **Docker không phải duy nhất**.

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá |
|---|---|---|---|
| Chạy trực tiếp trên máy | cài runtime, systemd | ứng dụng đơn giản, một máy, ít thay đổi | xung đột phiên bản, môi trường không tái lập |
| Máy ảo (VM) | VMware, EC2, Hyper-V | cần cô lập mức hypervisor giữa các bên không tin nhau | nặng, khởi động chậm, tốn tài nguyên |
| Micro-VM | Firecracker | cô lập mạnh như VM nhưng khởi động nhanh (Lambda, Fargate) | thêm lớp ảo hoá; ít công cụ sẵn |
| Container, engine cho dev | Docker, Podman, nerdctl | máy dev, CI, môi trường nhất quán | cô lập yếu hơn VM; cần học image, mạng, volume |
| Container + điều phối | Kubernetes, ECS (EC2 hay Fargate), Nomad | nhiều dịch vụ, cần scale, tự hồi phục | phức tạp vận hành; chi phí chạy nền |
| Serverless functions | AWS Lambda | tải thất thường, xử lý theo sự kiện | giới hạn runtime, khó debug cục bộ |
| PaaS | App Service, Heroku | ít vận hành, ứng dụng web chuẩn | mất kiểm soát runtime, gắn nhà cung cấp |
| **Không dùng container** | cài trực tiếp hoặc PaaS | ứng dụng đơn giản chạy ổn một chỗ | mất tính nhất quán đóng gói |

**Cách chọn:** hỏi (1) cần cô lập đến mức nào (giữa các nhóm cùng tin cậy hay các bên không tin nhau), (2) có nhiều dịch vụ cần triển khai nhất quán không, (3) tải đều hay thất thường, (4) ai vận hành. Dự án chọn container và ECS Fargate (ADR 0003) vì cần triển khai nhất quán nhiều service và ít vận hành hơn Kubernetes.

### Khi KHÔNG nên dùng container
- Cần cô lập mức hypervisor giữa các bên không tin nhau: VM hoặc micro-VM.
- Ứng dụng đơn giản chạy ổn một chỗ: thêm container chỉ thêm độ phức tạp.
- GPU hoặc phần cứng đặc thù chưa được hỗ trợ tốt.
- Chưa có ai vận hành image, registry, vá lỗi base image.

## Tự kiểm tra

1. Vì sao container khởi động nhanh hơn VM, và cái giá của việc chung kernel là gì?
2. Lab 1.1: vì sao `ps` chỉ thấy hai tiến trình?
3. Namespace và cgroup khác nhau ở điểm nào?
4. Docker Desktop trên Windows: kernel của container là kernel nào?

<details>
<summary>Đáp án ngắn</summary>

1. Không có kernel riêng cần khởi động; cái giá: cô lập yếu hơn VM, lỗ hổng kernel ảnh hưởng mọi container.
2. PID namespace: container chỉ thấy tiến trình trong namespace của nó.
3. Namespace quyết định tiến trình thấy gì (ẩn); cgroup quyết định dùng được bao nhiêu (giới hạn).
4. Kernel Linux của máy ảo WSL2 mà Docker Desktop chạy (Lab 1.2), không phải Windows.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 và 1.2 (Docker 29.5.3, `alpine:latest`; đầu ra thật).
**Đã tự mở:** opencontainers.org (OCI ra mắt 22/06/2015); Borg paper (EuroSys 2015); nhiều tài liệu khác ở các Part sau.
**Chỉ qua agent (⚠):** man7.org `namespaces(7)` và `cgroups(7)`, kernel `cgroup-v2` (tám namespace, `memory.max` và OOM trong cgroup, `memory.high`, quy tắc no-internal-process, kernel 3.8 cho user namespace); các cột "Khi chọn" của Bản đồ ngoài ADR 0003 là tổng hợp của mình.
**Chưa kiểm:** Firecracker và micro-VM (chưa mở nguồn); hành vi trên máy Linux thật (lab chạy trong WSL2).
