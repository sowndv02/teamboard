# Part 5 — Production, Security, Operations (Container)

> [!NOTE]
> **Mục tiêu:** hiểu **mô hình bảo mật** của container (chung kernel, capabilities, seccomp, user namespace), giới hạn tài nguyên và OOM, quan sát, chuỗi cung ứng image, và chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** giải thích vì sao root trong container nguy hiểm hơn tưởng, giải mã được tập capabilities mặc định, nêu đúng điều seccomp và capabilities làm và không làm, đặt giới hạn tài nguyên.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Container khi đã chạy thật

#### Mô hình bảo mật
Container **không** là ranh giới bảo mật mạnh như VM: nó là tiến trình trên cùng kernel, được cô lập bằng nhiều lớp (namespace, capabilities, seccomp, MAC như AppArmor/SELinux, user namespace).

#### Capabilities và seccomp
**Capabilities:** chia quyền root thành các quyền nhỏ (`CAP_NET_ADMIN`, `CAP_SYS_ADMIN`...). **Seccomp:** bộ lọc **lời gọi hệ thống** mà tiến trình được phép.

#### Giới hạn tài nguyên và OOM
CPU, bộ nhớ, số tiến trình; vượt giới hạn bộ nhớ thì bị OOM killer dừng (mã 137).

#### Chuỗi cung ứng image
Image đến từ đâu, có bị sửa không, chứa gì (SBOM), có lỗ hổng không.

## L2 Why — Vì sao không được bỏ qua

Vì chung kernel, một lỗ hổng kernel hay runtime có thể cho tiến trình "thoát" ra host; vì mặc định chạy **root** với nhiều capability, một ứng dụng bị chiếm có nhiều quyền hơn cần thiết (Lab 5.1). Và image là mã chạy trong hạ tầng của bạn: image độc hại hay chứa lỗ hổng là rủi ro trực tiếp (runc CVE-2019-5736 và Leaky Vessels, Part 7).

## L3 How — Cơ chế đáng nhớ

### Các lớp cô lập (và giới hạn của từng lớp)
| Lớp | Chặn gì | Không chặn |
|---|---|---|
| Namespaces | tiến trình thấy tài nguyên của host | không hạn chế dùng; cùng kernel |
| Cgroups | dùng quá hạn mức | không ngăn khai thác kernel |
| Capabilities | các thao tác đặc quyền (đổi hostname, mount...) | root vẫn mạnh với các quyền còn lại (`CAP_DAC_OVERRIDE`...) |
| Seccomp | lời gọi hệ thống nguy hiểm | chỉ lọc theo tên lời gọi và tham số, không thay cho các lớp khác |
| AppArmor/SELinux | truy cập file và tài nguyên theo chính sách | cần được bật và cấu hình |
| User namespace (rootless) | root trong container thành user thường trên host | một số tính năng bị giới hạn |

### Capabilities mặc định
Docker giữ 14 capability mặc định: `CHOWN`, `DAC_OVERRIDE`, `FOWNER`, `FSETID`, `KILL`, `SETGID`, `SETUID`, `SETPCAP`, `NET_BIND_SERVICE`, `NET_RAW`, `SYS_CHROOT`, `MKNOD`, `AUDIT_WRITE`, `SETFCAP`. Không có mặc định: `NET_ADMIN`, `SYS_ADMIN`, `SYS_PTRACE`, `SYS_TIME`, `SYS_MODULE`. `--privileged` cấp **mọi** capability và thiết bị và làm yếu AppArmor/SELinux; `--cap-drop ALL` rồi `--cap-add` đúng cái cần là nguyên tắc ít quyền.

### Seccomp
Hồ sơ mặc định của Docker là danh sách **cho phép** (hành động mặc định là từ chối), tắt khoảng 44 trong hơn 300 lời gọi hệ thống: ví dụ `init_module`, `mount`, `setns`, `unshare` kèm cờ namespace, `keyctl`, `ptrace`, `perf_event_open`, `clock_settime`, `reboot`, `io_uring_*`. Ghi đè bằng `--security-opt seccomp=<file>` hay tắt bằng `seccomp=unconfined` (không nên).

### User namespace và root
Mặc định container **không** dùng user namespace riêng: UID 0 trong container là UID 0 thật trong máy ảo Linux (Lab 5.1, `uid_map`). Hệ quả: chạy bằng người dùng không phải root (`USER`) là biện pháp rẻ và hiệu quả nhất; rootless (Part 4) đẩy thêm một lớp.

### Giới hạn tài nguyên và OOM
`memory.max` của cgroup v2 là giới hạn cứng; vượt mà không thu hồi được thì OOM killer chạy trong cgroup đó, tiến trình bị dừng với mã **137**. Đặt giới hạn bộ nhớ cho **mọi** container. Đặc biệt với .NET trong container: nếu không đặt giới hạn cứng cho GC heap, mặc định là lớn hơn giữa 20 MB và **75% giới hạn bộ nhớ của container** (`System.GC.HeapHardLimitPercent`); giới hạn này chỉ phủ **GC heap**, nên bộ nhớ native và stack vẫn có thể làm container bị OOM kill (suy luận, trang tài liệu không nói rõ). K8s: một container có thể vượt `request` nhưng không vượt `limit`; vượt là bị `OOMKilled` với `exitCode: 137`. Lab giới hạn tài nguyên đã có: [Docker Lab 4.5](../docker/part-4-advanced.md).

### Quan sát
`docker stats`, `top`, `diff`, `events`, `inspect`, `logs` (đã chạy ở [Docker Lab 5.3](../docker/part-5-production-security-ops.md)); trong điều phối: số lần restart, trạng thái probe, `OOMKilled`, sự kiện lập lịch; log ra `stdout` và `stderr` để nền tảng thu gom.

### Chuỗi cung ứng
Ghim digest; quét lỗ hổng (⚠ chưa chạy, [Docker Lab 5.6](../docker/part-5-production-security-ops.md)); SBOM và provenance ([Docker Lab 5.5](../docker/part-5-production-security-ops.md)); kéo từ registry tin cậy; giới hạn pull của Docker Hub: **100 lượt mỗi 6 giờ** cho người dùng chưa đăng nhập (theo IPv4 hoặc IPv6 /64), **200** cho tài khoản miễn phí đăng nhập; gói trả phí không giới hạn (trang docs.docker.com/docker-hub/usage đã mở).

### Chi phí và mật độ
Container cho phép **nhồi nhiều** tiến trình trên một máy (mật độ), nhưng điều phối chạy 24/7 có chi phí cố định (control plane, node, Fargate theo vCPU-giờ); Fargate có các cỡ task cố định (ví dụ 0,25 vCPU với 512 MiB đến 2 GB, 1 vCPU với 2 đến 8 GB). Với dự án: ADR 0003 ghi EKS control plane khoảng 73 USD/tháng chưa tính node (chưa kiểm giá hiện hành) và chọn ECS Fargate; chọn qua skill `aws-service-selection` trước khi viết Terraform.

## L4 Hands-on — Lab Part 5

Môi trường: như Part 0. Các lab siết quyền, health, SBOM đã có ở [lộ trình Docker (Part 5)](../docker/part-5-production-security-ops.md); không chạy lại. Lab dưới đây **khác**: đọc và giải mã trạng thái bảo mật từ `/proc` bên trong container.

#### Lab 5.1 — Capabilities, seccomp, user namespace nhìn từ `/proc`
**Đầu ra thật** (container `alpine`, mặc định):
```text
uid_map:   0  0  4294967295            (UID 0 trong container = UID 0 ở ngoài, không có user namespace riêng)
CapEff:    00000000a80425fb   CapBnd: 00000000a80425fb
NoNewPrivs: 0     Seccomp: 2     Seccomp_filters: 2
```
Giải mã `CapEff` bằng một script Node theo bảng capability của Linux:
```text
CapEff 0xa80425fb -> CHOWN, DAC_OVERRIDE, FOWNER, FSETID, KILL, SETGID, SETUID, SETPCAP, NET_BIND_SERVICE, NET_RAW, SYS_CHROOT, MKNOD, AUDIT_WRITE, SETFCAP | số lượng 14
```
**Giải thích:** đúng 14 capability mặc định như tài liệu Docker; `uid_map` cho thấy không có ánh xạ UID. `Seccomp: 2` nghĩa là có bộ lọc seccomp (chế độ filter); mặc định có **2** bộ lọc.

#### Lab 5.2 — `--privileged` và `seccomp=unconfined`
```text
--privileged:                 CapEff: 000001ffffffffff   Seccomp: 2   Seccomp_filters: 1
--security-opt seccomp=unconfined:                       Seccomp: 2   Seccomp_filters: 1
```
**Giải thích:** `--privileged` bật **toàn bộ** capability (mặt nạ `0x1ffffffffff`). Điểm cần lưu ý: dù `unconfined` hay `--privileged`, vẫn còn **một** bộ lọc seccomp (`Seccomp_filters: 1`), còn mặc định có **hai**; bộ lọc còn lại không đến từ hồ sơ của Docker (nhiều khả năng do môi trường Docker Desktop và WSL2; chưa tìm nguồn nên ⚠). Đừng đọc "unconfined" là "không có bộ lọc nào" trong môi trường này.

#### Lab 5.3 — Thiếu capability thì thao tác đặc quyền bị từ chối
```text
(mặc định) hostname x           -> hostname: sethostname: Operation not permitted
(mặc định) unshare --uts ...    -> unshare: unshare(0x4000000): Operation not permitted
(seccomp=unconfined) unshare --uts ... -> unshare: unshare(0x4000000): Operation not permitted
```
**Giải thích:** `sethostname` và `unshare` (cờ `0x4000000` là `CLONE_NEWUTS`) bị từ chối vì container **không có** `CAP_SYS_ADMIN`; tắt seccomp (`unconfined`) **không** làm `unshare` chạy được, vì capability, không phải seccomp, là lớp đang chặn ở đây. Mỗi lớp độc lập: bỏ một lớp không mở được thao tác nếu lớp khác vẫn chặn.

## L5 Troubleshooting — Sự cố vận hành

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Container thoát với mã 137 | OOM killer hoặc bị `SIGKILL` | `docker inspect`: `OOMKilled`; `memory.events` | tăng giới hạn, sửa rò bộ nhớ; kiểm bộ nhớ native |
| .NET bị OOM dù GC heap nhỏ hơn giới hạn | bộ nhớ ngoài GC heap (native, stack) | so tổng bộ nhớ tiến trình với heap | hạ `HeapHardLimitPercent`, tăng giới hạn |
| Thao tác báo "Operation not permitted" trong container | thiếu capability hoặc bị seccomp chặn (Lab 5.3) | xem `CapEff` và lời gọi hệ thống bị từ chối | `--cap-add` đúng cái cần; tránh `--privileged` |
| Ứng dụng chạy root và bị chiếm | mặc định root, không `USER` | `id`, `CapEff` | `USER` không root, `--cap-drop ALL` |
| Pull thất bại "too many requests" | vượt giới hạn Docker Hub | thông báo lỗi, đã đăng nhập chưa | đăng nhập, mirror nội bộ, ghim và lưu cache |
| Chi phí nền tăng | node hay task chạy 24/7 | số node, giờ chạy | tắt môi trường không dùng, `destroy` |
| Image có lỗ hổng đã biết | base image cũ | quét, SBOM | cập nhật base, build lại, ghim digest mới |
| Nghi ngờ image bị sửa | tag đổi hay registry bị can thiệp | tính lại digest | ghim digest, ký và kiểm chữ ký |

## L6 Production — Danh sách kiểm khi chạy container

- **Không root:** `USER` không đặc quyền; `--cap-drop ALL` rồi chỉ thêm cái cần; `no-new-privileges`; `--read-only` với `tmpfs` cho thư mục cần ghi ([Docker Lab 5.1](../docker/part-5-production-security-ops.md)).
- **Không `--privileged`**, không gắn `docker.sock`, không dùng `seccomp=unconfined` nếu không có lý do ghi lại.
- **Giới hạn tài nguyên** cho mọi container (bộ nhớ, CPU, số tiến trình); .NET: hiểu `HeapHardLimitPercent`.
- **Image:** ghim digest; base image cập nhật định kỳ; quét lỗ hổng trong CI; SBOM; không secret trong layer.
- **Mạng:** không mở cổng không cần; gắn vào mạng riêng; không bind `0.0.0.0` cho DB dev.
- **Quan sát:** log ra stdout, cảnh báo theo số lần restart và `OOMKilled`, health và readiness.
- **Vá runtime:** biết phiên bản containerd và runc đang chạy và theo dõi CVE (Part 7).
- **Chi phí:** ước tính, tắt ngoài giờ, so với phương án khác (PaaS).

## L7 Architecture — Mức cô lập nên dùng

| Tình huống | Chọn | Cái giá |
|---|---|---|
| Các thành phần cùng nhóm tin cậy | container với `USER` không root, `cap-drop ALL`, seccomp mặc định | cô lập mức tiến trình, chung kernel |
| Chạy mã của bên không tin cậy | micro-VM hoặc VM, không chỉ container | thêm lớp ảo hoá |
| Giảm tác hại khi bị chiếm | rootless hoặc user namespace | một số tính năng bị giới hạn |
| Cần quyền đặc biệt (mạng, hệ thống) | `--cap-add` đúng quyền, không `--privileged` | mở rộng bề mặt tấn công có kiểm soát |
| Không muốn tự vận hành kernel và runtime | Fargate, Cloud Run, PaaS | gắn nhà cung cấp; chi phí |
| **Không dùng container** | VM chuyên dụng hoặc PaaS | mất tính nhất quán đóng gói |

## Tự kiểm tra

1. Vì sao root trong container nguy hiểm hơn tưởng khi không có user namespace?
2. Lab 5.3: vì sao tắt seccomp không làm `unshare` chạy được?
3. Nêu bốn capability mặc định của Docker và hai capability không có mặc định.
4. .NET trong container có giới hạn bộ nhớ 512 MB: GC heap mặc định tối đa bao nhiêu và vì sao vẫn có thể bị OOM kill?

<details>
<summary>Đáp án ngắn</summary>

1. UID 0 trong container là UID 0 thật trong máy ảo Linux (`uid_map` 0 0 4294967295), nên một lỗ hổng thoát ra có quyền root thật.
2. Container không có `CAP_SYS_ADMIN`; capability chặn độc lập với seccomp.
3. Ví dụ: `CHOWN`, `KILL`, `SETUID`, `NET_BIND_SERVICE`; không có: `SYS_ADMIN`, `NET_ADMIN`.
4. 75% (384 MB); GC heap không phủ bộ nhớ native và stack nên tổng vẫn có thể vượt giới hạn.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 đến 5.3 (Docker 29.5.3, `alpine`, WSL2; đầu ra thật; giải mã `CapEff` bằng script Node). Các lab liên kết đã chạy ở lộ trình Docker.
**Đã tự mở:** docs.docker.com/docker-hub/usage (100, 200 lượt mỗi 6 giờ), learn.microsoft.com .NET GC settings (75% giới hạn container), kubernetes.io `assign-memory-resource` (`OOMKilled`, 137), qua agent.
**Chỉ qua agent (⚠):** docs.docker.com `containers/run` và `security/seccomp` (14 capability mặc định, khoảng 44 lời gọi bị tắt trên hơn 300); kernel cgroup-v2 (`memory.max`, OOM trong cgroup); ECS Fargate task sizes.
**Chưa kiểm:** nguồn của bộ lọc seccomp thứ hai trong Docker Desktop (Lab 5.2); AppArmor và SELinux; quét lỗ hổng; ký image; giá hiện hành (EKS 73 USD/tháng theo ADR).
