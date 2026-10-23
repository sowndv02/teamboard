# Part 1 — Fundamentals & Mental Models (Docker)

> [!NOTE]
> **Mục tiêu:** hiểu container và Docker bên dưới đang làm gì, đủ để không còn coi chúng là "máy ảo nhẹ".
> **Cần biết trước:** [Part 0](README.md). Chưa cần biết Linux.
> **Thời lượng:** ~3 giờ · **Đầu ra kiểm được:** giải thích luồng `docker run`, nêu vai trò namespaces và cgroups, đọc được trạng thái và mã thoát của một container.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Container, image và Docker

#### Container
**Là gì:** một tiến trình Linux chạy trong một "hộp" cô lập: nó chỉ thấy filesystem, mạng, danh sách tiến trình của riêng nó, và chỉ dùng được lượng CPU/RAM được cấp.
**Ví dụ đời thường:** một phòng khách sạn: dùng chung toà nhà (kernel) với các phòng khác nhưng có khoá, nội thất và công tơ điện riêng.
**Use case:**
- Chạy `board-service` và SQL Server trên cùng một máy mà không xung đột phiên bản → mỗi thứ một container.
- Chạy thử một công cụ cài phức tạp (một bản `nginx` cụ thể) rồi xoá sạch → `docker run --rm`.

**Lưu ý:** container không có hệ điều hành riêng, chỉ có "userland" riêng (thư viện, công cụ). Kernel là kernel của máy chủ (Lab 1.2).

#### Image
**Là gì:** khuôn **chỉ đọc**, gồm các lớp (layer) chứa ứng dụng, thư viện và cấu hình khởi động. Container là một lần chạy của image, thêm một lớp ghi tạm ở trên.
**Ví dụ đời thường:** image là bản in gốc của một cuốn sách; container là một bản photo bạn được phép viết nháp lên.
**Use case:**
- Build một lần ở CI, chạy y nguyên ở dev, staging, production → cùng một image.
- Quay lại phiên bản cũ khi bản mới lỗi → chạy lại image cũ.

#### Docker (client, daemon, registry)
**Là gì:** bộ công cụ gồm **client** `docker` (bạn gõ lệnh), **daemon** `dockerd` (làm việc thật: quản lý image, container, mạng, volume) và **registry** (nơi lưu và chia sẻ image, mặc định Docker Hub).
**Use case:**
- `docker pull` / `docker push` đưa image từ và lên registry (Docker Hub, Amazon ECR, GHCR).
- Docker Desktop chạy daemon trong một máy ảo Linux nhỏ trên Windows/macOS, nên "container Linux" thực ra chạy trong VM đó.

| | Container | Máy ảo (VM) |
|---|---|---|
| Kernel | dùng chung với host | mỗi VM một kernel |
| Khởi động | dưới 1 giây (Lab 1.5) | thường lâu hơn nhiều ⚠ |
| Kích thước | image `alpine` 13 MB | thường hàng GB ⚠ |
| Cô lập | yếu hơn (chung kernel) | mạnh hơn |

## L2 Why — Vì sao cần container

**Tình huống:** `board-service` chạy tốt trên máy bạn nhưng lỗi trên máy đồng nghiệp: khác phiên bản .NET, thiếu thư viện, khác biến môi trường. Thêm SQL Server và Jaeger thì mỗi người cài một kiểu.

- **Trước container:** hướng dẫn cài dài, "chạy trên máy tôi", mỗi môi trường một lệch.
- **Dùng VM:** cô lập tốt nhưng nặng (mỗi VM mang cả hệ điều hành), chậm bật, tốn RAM.
- **Dùng container:** đóng gói ứng dụng cùng thứ nó cần vào một image; chạy ở đâu có Docker là giống nhau; khởi động nhanh; nhiều container chung một máy.

**Cái giá:** thêm một lớp cần học (image, registry, mạng, volume) và cô lập yếu hơn VM. Mục L7 nói khi nào đáng.

## L3 How — Bên trong một container

#### Chuỗi từ lệnh tới tiến trình
```
docker run ...        (client)
   │  REST API qua socket/pipe
dockerd               (daemon: image, mạng, volume, API)
   │
containerd            (runtime cấp cao: kéo image, quản vòng đời)
   │
runc                  (runtime cấp thấp: tạo namespace, cgroup rồi chạy tiến trình)
   ▼
tiến trình của bạn (PID 1 trong container)
```
Phiên bản thật trên máy chạy lab: Engine 29.5.3, containerd v2.2.4, runc 1.3.5, docker-init 0.19.0 (Lab 1.1). Vai trò của **shim** (giữ container sống khi runc đã thoát) là kiến thức chung ⚠.
**Use case:** biết chuỗi này giúp hiểu lỗi `failed to create shim task: OCI runtime create failed` khi lệnh không tìm thấy (Lab 1.5): lỗi nằm ở runc, không phải ở app.

#### Namespaces: tiến trình "thấy gì"
**Là gì:** mỗi namespace cho tiến trình một bản riêng của một tài nguyên hệ thống. Linux có 8 loại:

| Namespace | Cô lập | Thấy trong lab |
|---|---|---|
| PID | cây tiến trình | `ps` chỉ thấy chính nó, PID 1 |
| UTS | hostname | `--hostname lh-host` |
| Network | thiết bị mạng, cổng | `--net=none` chỉ còn `lo` |
| Mount | điểm mount | thư mục gốc riêng từ image |
| IPC, User, Cgroup, Time | hàng đợi IPC; UID/GID; gốc cgroup; đồng hồ | có đủ trong `/proc/self/ns` |

**Use case:** hai container cùng nghe cổng 80 mà không đụng nhau (mỗi cái một network namespace); container chạy `root` bên trong nhưng không phải root thật nhờ user namespace (khi bật rootless/userns-remap ở Part 5).
**Lưu ý:** namespace chỉ **ẩn**, không **giới hạn**.

#### Cgroups: tiến trình "dùng được bao nhiêu"
**Là gì:** nhóm tiến trình theo cây để giới hạn và đo tài nguyên (CPU, bộ nhớ, số tiến trình, I/O). Máy chạy lab dùng **cgroup v2** (Lab 1.1).
**Use case:** `-m 64m` đặt `memory.max=67108864`; `--cpus 0.5` đặt `cpu.max=50000 100000` (50 ms CPU mỗi 100 ms) → một service lỗi không ăn hết máy.
**Lưu ý:** vượt bộ nhớ thì kernel **OOM kill** tiến trình; vượt CPU thì chỉ bị làm chậm (Part 4).

#### Lớp image và lớp ghi
Image gồm các layer chỉ đọc xếp chồng; container thêm một lớp ghi. **Ghi vào đó thì mất khi xoá container** (Lab 2.7); muốn giữ dữ liệu phải dùng volume. Storage driver trên máy lab là `overlayfs` (containerd image store), không phải `overlay2` như nhiều tài liệu cũ.

#### Vòng đời và mã thoát
```
create ─► created ─start─► running ─pause─► paused
                              │  ▲             │
                       stop/kill  └─unpause────┘
                              ▼
                           exited ─rm─► (biến mất)
```
Ngoài ra còn `restarting` (đang được restart theo policy) và `dead` (lỗi, chỉ xoá được) ⚠.

| Mã thoát | Nghĩa |
|---|---|
| 0 | thoát bình thường |
| 125 | lỗi từ daemon, ví dụ image không tồn tại (Lab 1.5) |
| 127 | lệnh không tìm thấy trong container (Lab 1.5) |
| 137 | 128+9: bị SIGKILL (hết hạn `stop`, hoặc OOM) |
| 143 | 128+15: thoát vì SIGTERM |

#### PID 1 và tín hiệu
**Là gì:** tiến trình đầu tiên trong container là PID 1. Kernel không áp xử lý mặc định cho tín hiệu của PID 1: nếu app không tự bắt `SIGTERM` thì nó **bỏ qua**.
**Use case:** `docker stop` gửi SIGTERM, chờ hết timeout rồi gửi SIGKILL. Với `sleep` làm PID 1, lệnh `stop -t 3` mất đúng ~3,6 giây rồi thoát 137; thêm `--init` thì dừng trong ~0,5 giây, thoát 143 (Lab 1.5).
**Lưu ý:** bắt SIGTERM trong app hoặc dùng `--init`; dùng dạng exec cho `ENTRYPOINT`/`CMD` (Part 2).

## L4 Hands-on — Lab Part 1

Môi trường: Docker Desktop 4.77.0, Engine 29.5.3, Windows 11 (WSL2), ngày 23/10/2026. Mọi tài nguyên có tiền tố `lh-` hoặc dùng `--rm`.

#### Lab 1.1 — Xem kiến trúc và phiên bản
**Mục tiêu:** biết daemon đang dùng runtime nào, cgroup nào, storage nào.
```bash
docker info --format 'Server={{.ServerVersion}} | Kernel={{.KernelVersion}} | Cgroup={{.CgroupVersion}}/{{.CgroupDriver}} | Storage={{.Driver}}'
docker version --format '{{range .Server.Components}}{{.Name}} {{.Version}}{{"\n"}}{{end}}'
```
**Đầu ra thật:**
```text
Server=29.5.3 | Kernel=6.18.33.1-microsoft-standard-WSL2 | Cgroup=2/cgroupfs | Storage=overlayfs
Engine 29.5.3
containerd v2.2.4
runc 1.3.5
docker-init 0.19.0
```
**Giải thích:** kernel là kernel của VM WSL2, không phải của Windows. Có đủ Engine, containerd, runc và `docker-init` (dùng cho `--init`).

#### Lab 1.2 — Container dùng kernel của host
```bash
docker run --rm alpine uname -sr
```
**Đầu ra thật:** `Linux 6.18.33.1-microsoft-standard-WSL2`
**Giải thích:** đúng kernel ở Lab 1.1: container không có kernel riêng.

#### Lab 1.3 — Namespaces
```bash
docker run --rm alpine ps
docker run --rm --hostname lh-host alpine hostname
docker run --rm --net=none alpine ip -o addr
docker run --rm alpine sh -c 'ls /proc/self/ns | tr "\n" " "'
```
**Đầu ra thật (rút gọn):**
```text
PID   USER     TIME  COMMAND
    1 root      0:00 ps
lh-host
1: lo    inet 127.0.0.1/8 scope host lo
cgroup ipc mnt net pid pid_for_children time time_for_children user uts
```
**Giải thích:** `ps` thấy mỗi chính nó với PID 1; hostname và mạng riêng; thư mục `ns` liệt kê đủ 8 loại (`pid_for_children`, `time_for_children` là bản phụ).

#### Lab 1.4 — Cgroups
```bash
docker run --rm -m 64m --cpus 0.5 alpine sh -c 'echo memory.max=$(cat /sys/fs/cgroup/memory.max); echo cpu.max=$(cat /sys/fs/cgroup/cpu.max)'
docker run --rm alpine sh -c 'echo memory.max=$(cat /sys/fs/cgroup/memory.max); echo cpu.max=$(cat /sys/fs/cgroup/cpu.max)'
```
**Đầu ra thật:**
```text
memory.max=67108864
cpu.max=50000 100000
memory.max=max
cpu.max=max 100000
```
**Giải thích:** cờ `-m` và `--cpus` chỉ là cách nói; kernel thực thi qua file cgroup. Không đặt giới hạn thì là `max`.

#### Lab 1.5 — Vòng đời, `stop` và mã thoát
```bash
docker create --name lh-life alpine sleep 300     # created
docker start lh-life                              # running
docker pause lh-life                              # paused
docker unpause lh-life
docker stop -t 3 lh-life                          # sleep không bắt SIGTERM
docker run -d --init --name lh-life2 alpine sleep 300
docker stop -t 10 lh-life2
docker run --rm alpine nosuchcmd; echo "exit=$?"
docker run --rm nosuchimage_lh; echo "exit=$?"
```
**Đầu ra thật (rút gọn):**
```text
created / Up Less than a second / Up 1 second (Paused)
stop (không --init): 3,58 giây -> state=exited exit=137
stop (--init):       0,49 giây -> state=exited exit=143
docker: Error response from daemon: ... OCI runtime create failed: ... exec: "nosuchcmd": executable file not found in $PATH
exit=127
(image không tồn tại)
exit=125
```
**Giải thích:** `sleep` làm PID 1 không bắt SIGTERM nên phải đợi hết timeout rồi bị SIGKILL (137). Với `--init`, `docker-init` nhận SIGTERM, chuyển cho `sleep`, `sleep` chết vì SIGTERM (143) ngay. Lệnh không có trong image cho 127; image không tồn tại cho 125.
**Dọn dẹp:** `docker rm -f lh-life lh-life2` (đã dọn).

## L7 Architecture — Docker là một lựa chọn, không phải "container"

#### Phạm vi và những gì Docker KHÔNG đại diện
Docker là **một họ** trong [bản đồ giải pháp Container](../container.md#bản-đồ-giải-pháp): "engine đầy đủ cho nhà phát triển". Nó không đại diện cho cả khái niệm.

- **Container là cơ chế của kernel** (namespaces, cgroups) cộng chuẩn OCI. Docker là cách tiện dụng để dùng chúng.
- **Image build bằng Docker chạy được trên runtime khác** tuân chuẩn OCI (containerd, CRI-O, Podman).
- **Podman** chạy không cần daemon và mặc định rootless; **containerd + nerdctl** dùng runtime trực tiếp; **CRI-O** chỉ phục vụ Kubernetes. Chi tiết ở [container.md](../container.md).
- **Docker Desktop** có điều kiện giấy phép trả phí với tổ chức lớn; Docker Engine thì không (Part 5).

#### Khi nào KHÔNG nên dùng container
- Cần cô lập mạnh giữa các bên không tin nhau → VM hoặc micro-VM, vì container chung kernel.
- Ứng dụng đơn giản chạy ổn một chỗ, không cần tái lập môi trường.
- Cần phần cứng đặc thù chưa được hỗ trợ tốt trong container.

## Tự kiểm tra

1. Container khác VM ở đâu, và hệ quả bảo mật là gì?
2. `docker stop` mất đúng thời gian timeout rồi thoát 137. Nguyên nhân thường gặp và hai cách sửa?
3. Namespaces và cgroups khác nhau thế nào? Mỗi cái trả lời câu hỏi gì?
4. Vì sao một image build bằng Docker vẫn chạy được trên containerd mà không cần Docker?

<details>
<summary>Đáp án ngắn</summary>

1. VM có kernel riêng, container dùng chung kernel host. Hệ quả: lỗi kernel hoặc cấu hình sai có thể ảnh hưởng mọi container; cô lập yếu hơn VM.
2. App làm PID 1 không bắt SIGTERM nên kernel bỏ qua tín hiệu; hết timeout bị SIGKILL (137). Sửa: để app bắt SIGTERM (và dùng dạng exec) hoặc chạy với `--init`.
3. Namespaces quyết định tiến trình **thấy gì**; cgroups quyết định **dùng được bao nhiêu**.
4. Cả hai theo chuẩn OCI về định dạng image và cách chạy, nên runtime nào tuân chuẩn cũng chạy được.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** toàn bộ đầu ra ở L4 (Lab 1.1 đến 1.5, môi trường nêu ở đầu mục); mã thoát và hành vi `--init`; giới hạn cgroup.
**Đã tự mở:** OCI overview (opencontainers.org/about/overview), Borg paper (research.google), tài liệu thực hành tốt về exec form và PID 1 (docs.docker.com/build/building/best-practices).
**Chỉ qua agent hoặc ước lượng chung (⚠):** cột VM trong bảng so sánh (Docker chỉ nêu "chục MB so với chục GB" và container khởi động nhanh hơn, qua agent); các trạng thái `restarting` và `dead` (docs.docker.com/reference/cli/docker/container/ls); man page `namespaces(7)` và `cgroups(7)` về tám loại namespace và điều kiện quyền; vai trò shim.
**Chưa kiểm:** quan hệ chính xác dockerd → containerd → shim → runc không có trang nào nối thẳng ba thành phần trong tài liệu đã đọc.
