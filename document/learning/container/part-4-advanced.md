# Part 4 — Advanced (Container)

> [!NOTE]
> **Mục tiêu:** hiểu image **bên trong** theo chuẩn OCI (manifest, config, layer, digest, whiteout), chuỗi runtime từ công cụ tới runc, runtime-spec, và rootless.
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** mở một image bằng tay và kiểm digest, giải thích vì sao xoá file ở layer sau không xoá nó khỏi image, nêu `config.json` của OCI chứa gì, nêu giới hạn của rootless.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Bên trong image và runtime

#### Image theo chuẩn OCI
**Là gì:** một tập **blob** đánh địa chỉ theo nội dung (SHA-256): một **manifest** liệt kê một **config** và các **layer**; một **index** trỏ tới nhiều manifest theo nền tảng.
**Use case:** biết vì sao hai image cùng base chỉ lưu base một lần và vì sao kiểm được image đã tải đúng.

#### Layer là một bộ thay đổi (changeset)
Mỗi layer là một tệp tar chứa **chỉ những thay đổi** so với layer dưới; xoá file được ghi bằng một tệp **whiteout**.
**Use case:** hiểu vì sao secret xoá ở lệnh `RUN` sau vẫn nằm trong image.

#### Runtime theo OCI
**runtime-spec:** một **bundle** (thư mục `config.json` và `rootfs`) và vòng đời `create`, `start`, `kill`, `delete`. **runc:** chương trình cấp thấp thực thi bundle.

#### Rootless
Chạy cả engine lẫn container không cần root thật trên host, nhờ user namespace.

## L2 Why — Vì sao nên đọc bên trong

Mọi lệnh như `docker pull`, `docker history`, quét lỗ hổng, SBOM đều làm việc trên cấu trúc này. Hiểu nó trả lời được: vì sao pull nhanh khi đã có layer, vì sao digest đảm bảo toàn vẹn, vì sao `docker history` lộ secret, vì sao image đa nền tảng có nhiều manifest, và vì sao runtime nào cũng chạy được image nào (chuẩn OCI).

## L3 How — Cơ chế đáng nhớ

### Cấu trúc image (OCI image-spec)
- **Descriptor:** `mediaType`, `digest` (`sha256:` cộng 64 ký tự hex), `size`; người dùng nên tính lại băm và so sánh; sai kích thước thì không tin nội dung.
- **Manifest** (`application/vnd.oci.image.manifest.v1+json`): `schemaVersion` bằng 2, một `config` (bắt buộc) và `layers` theo thứ tự từ **layer nền (chỉ số 0)** lên.
- **Config** (`application/vnd.oci.image.config.v1+json`): `rootfs.diff_ids` (một mục cho mỗi layer), `history` (mỗi mục có `created_by`; mục `empty_layer` không có layer), và cấu hình chạy (`Cmd`, `Env`, `User`, `WorkingDir`, `ExposedPorts`...).
- **Index** (`application/vnd.oci.image.index.v1+json`): danh sách manifest, mỗi mục có `platform` (`os`, `architecture`, `variant`); đây là cách một tên image chạy được trên nhiều kiến trúc.
- **Layer** (`...layer.v1.tar`, `+gzip`, `+zstd`): một tệp tar.
- **Hai loại băm của layer:** **digest** trong manifest là băm của blob **như lưu** (đã nén); **DiffID** trong config là băm của tar **chưa nén**. Config tham chiếu DiffID nên băm của config phụ thuộc nội dung filesystem. Điều này khớp Lab 4.1; câu "layer.md không nói rõ" là suy luận của agent, kết quả lab xác nhận.

### Layer là changeset, whiteout
Layer được **áp dụng**, không chỉ giải nén: đường dẫn đã có thì xoá và tạo lại theo mục trong layer. Xoá file ở layer trên được ghi bằng một tệp rỗng tên `.wh.<tên file>` cùng thư mục; `.wh..wh..opq` che mọi con của thư mục từ layer dưới. **Hệ quả:** nội dung file bị xoá **vẫn nằm trong layer dưới** và ai tải image đều đọc được (Lab 4.1).

### Phân phối (OCI distribution-spec)
Pull là `GET /v2/<name>/manifests/<reference>` rồi `GET /v2/<name>/blobs/<digest>`; `<reference>` là tag hoặc digest; client gửi `Accept` liệt kê loại manifest hiểu được; manifest trả về phải băm đúng digest nếu truy cập theo digest. Push tải blob trước, manifest sau cùng.

### Runtime theo OCI (runtime-spec)
- **Bundle:** thư mục có `config.json` (bắt buộc ở gốc) và rootfs mà `root.path` chỉ tới.
- **`config.json`:** `ociVersion`; `root` (`path`, `readonly`); `mounts`; `process` (`args`, `env`, `cwd`, `user`, `capabilities` năm tập); `hostname`; `hooks`; phần `linux`: `namespaces` (8 loại; có thể gắn `path` để vào namespace có sẵn), `uidMappings` và `gidMappings`, `resources` (`memory.limit`, `cpu`, `pids.limit`), `cgroupsPath`, `seccomp`.
- **Vòng đời:** `create` dựng môi trường nhưng **chưa chạy** chương trình; `start` chạy; `kill` gửi tín hiệu; `delete` dọn. Trạng thái: `creating`, `created`, `running`, `stopped`.
- **Điểm hay:** vì `create` và `start` tách nhau, có thể cấu hình mạng **giữa hai bước**.

### Chuỗi từ công cụ tới process
```
Docker CLI / Podman / nerdctl / kubelet
   └─► runtime cấp cao: containerd (daemon, gRPC) hoặc CRI-O
         ├─ content store + snapshotter  (kéo image, giải layer thành rootfs)
         └─► shim v2 (containerd-shim-runc-v2)  ── giữ vòng đời, thu dọn tiến trình mồ côi
               └─► runc (runtime cấp thấp, theo OCI runtime-spec)  ──►  tiến trình container
```
containerd có plugin **CRI** để kubelet gọi trực tiếp (kéo image, mạng bằng CNI, tạo "pause" sandbox cho Pod). **Shim** là tiến trình riêng nên runc có thể thoát sau khi khởi tạo. Chuỗi này lắp từ nhiều trang tài liệu, không có một sơ đồ chính thức duy nhất. Kubernetes gỡ `dockershim` ở v1.24 (Part 7) vì kubelet chuyển sang gọi thẳng runtime cấp cao qua CRI; image build bằng Docker vẫn chạy vì theo OCI.

### Rootless
Daemon và container cùng chạy trong user namespace; cần dải `/etc/subuid` và `/etc/subgid` có ít nhất 65.536 ID và `newuidmap`, `newgidmap`. **Giới hạn** (tài liệu rootless của Docker): giới hạn tài nguyên cần cgroup v2 và systemd; chỉ một số storage driver; không có AppArmor, checkpoint/restore, overlay network; cổng dưới 1024 cần cấu hình thêm; `ping` mặc định không chạy; mạng qua user-mode nên chậm hơn và không giữ IP nguồn. Khác `userns-remap` (daemon vẫn chạy root).

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0. Image dựng từ `alpine:latest` đã có (không kéo thêm); `docker save` và một script Node đọc **bằng tay** (không dùng công cụ của Docker để phân tích): đọc `index.json`, manifest, config, tar của từng layer, và tự tính SHA-256.

#### Lab 4.1 — Mở một image, kiểm digest, tìm whiteout
Dockerfile của lab (image `lh-whiteout:1`):
```dockerfile
FROM alpine:latest
RUN echo "token-123" > /secret.txt
RUN rm /secret.txt
```
**Đầu ra thật:**
```text
index.json: schemaVersion 2 | mediaType application/vnd.oci.image.index.v1+json | manifests 1
index con -> amd64 sha256:440f10eb5abd…
manifest: config sha256:9529fa6dc9df… application/vnd.oci.image.config.v1+json | layers 3
config: os/arch linux/amd64 | rootfs.type layers | diff_ids 3
history:
  0 (tạo layer)       ADD alpine-minirootfs-3.24.3-x86_64.tar.gz / # buildkit
  1 (không tạo layer) CMD ["/bin/sh"]
  2 (tạo layer)       RUN /bin/sh -c echo "token-123" > /secret.txt # buildkit
  3 (tạo layer)       RUN /bin/sh -c rm /secret.txt # buildkit
layer 0: tar+gzip 3853109 byte | digest khớp: true | diffID khớp config: true | 515 mục | whiteout: -
layer 1: tar+gzip 118 byte     | digest khớp: true | diffID khớp config: true | 1 mục   | whiteout: -
   mục chứa secret/whiteout: secret.txt (10 byte)
layer 2: tar+gzip 80 byte      | digest khớp: true | diffID khớp config: true | 1 mục   | whiteout: .wh.secret.txt
   mục chứa secret/whiteout: .wh.secret.txt (0 byte)
```
**Giải thích:**
- Cấu trúc đúng như spec: index → manifest → config + 3 layer; **ba lần kiểm đều khớp**: SHA-256 của file blob bằng tên của nó (digest), và SHA-256 của tar giải nén bằng `diff_id` trong config (DiffID).
- `history` có 4 mục nhưng chỉ **3 layer**: `CMD` là `empty_layer`, không tạo layer.
- **Bài học bảo mật:** `secret.txt` (10 byte) **nằm nguyên trong layer 1**; lệnh `rm` ở layer 2 chỉ thêm một tệp rỗng `.wh.secret.txt` che nó khi áp dụng chồng layer. Ai tải image, đọc layer 1 là thấy `token-123`. Lab chỉ đọc kích thước (10 byte) chứ không in nội dung, nhưng "10 byte" khớp với `token-123\n`; muốn thấy nội dung chỉ cần giải nén layer 1.
- Docker 29.5.3 lưu image theo bố cục OCI (`index.json`, `blobs/sha256`, `oci-layout`), đúng như spec; lab này không đọc `manifest.json` kiểu cũ của Docker.

#### Lab 4.2 — `docker history` và build secret
Đã có ở lộ trình Docker: [Lab 4.3 (`ARG` làm lộ, `--secret` không)](../docker/part-4-advanced.md). Lab 4.1 ở trên giải thích **vì sao** (layer giữ nội dung).

## L5 Troubleshooting — Image và runtime

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Image phình dù đã xoá file | file xoá ở layer sau vẫn nằm ở layer dưới (Lab 4.1) | `docker history`, kích thước từng layer | xoá trong **cùng** lệnh `RUN`, hoặc multi-stage |
| Secret lộ dù đã `rm` | secret nằm trong một layer | đọc layer như Lab 4.1 | không đưa secret vào layer; `--secret` |
| Pull lỗi "digest mismatch" hoặc manifest không khớp | blob hỏng, registry hay proxy can thiệp | tính lại SHA-256, so với digest | pull lại; kiểm đường truyền và registry |
| Image chạy trên máy này mà không chạy ở máy kia | sai kiến trúc (amd64 so với arm64) | xem index và `platform` | image đa nền tảng, hoặc build đúng nền tảng |
| Pull chậm dù chỉ đổi một dòng code | thứ tự lệnh làm vô hiệu cache layer | xem layer nào đổi | đặt lệnh ít đổi lên trước |
| Container không khởi động ở bước runtime | config.json sai (mount, user, capability) | `inspect`, log của runtime | sửa cấu hình theo OCI |
| Rootless chạy chậm hoặc không kết nối | mạng user-mode, cổng dưới 1024 | xem cấu hình mạng | tăng cổng, cấu hình `sysctl`, hoặc dùng rootful có giới hạn |
| Giới hạn tài nguyên không có tác dụng ở rootless | cgroup v1 hay thiếu systemd | `docker info` | cgroup v2 và systemd |

## L6 Production — Quyết định khi đưa lên thật

- **Ghim digest** cho image chạy production và base image; ghi digest đã triển khai để rollback đúng.
- **Image nhỏ, ít lớp thừa:** multi-stage, xoá cache trong cùng lệnh; nhưng **không** coi "xoá" là bảo mật.
- **Secret:** không ở layer, không ở `ENV`; dùng cơ chế secret của nền tảng.
- **Kiểm toàn vẹn:** verify digest, ký image (cosign hoặc tương đương ⚠ chưa dùng), SBOM ([Docker Lab 5.5](../docker/part-5-production-security-ops.md)).
- **Đa nền tảng:** build index nếu chạy cả amd64 và arm64 (Fargate, Graviton).
- **Runtime:** biết chuỗi containerd, shim, runc để biết vá lỗi ở đâu (CVE của runc, Part 7).

## L7 Architecture — Chọn runtime và cách build

| Bài toán | Chọn | Cái giá |
|---|---|---|
| Máy dev, quen thuộc | Docker (engine đầy đủ) | daemon root theo mặc định; Desktop có điều kiện giấy phép |
| Không muốn daemon root, rootless | Podman | hệ sinh thái Compose/Desktop khác |
| Node Kubernetes | containerd hoặc CRI-O | không có công cụ cuối cho dev |
| Build trong CI không daemon | Buildah, hoặc BuildKit | công cụ riêng cần học; Kaniko đã archive (03/06/2025) |
| Cô lập mạnh hơn container | micro-VM hoặc VM | thêm lớp ảo hoá |
| Chạy trực tiếp bundle OCI | runc | cấp thấp, thường chỉ cho công cụ gọi |
| **Không dùng runtime riêng** | PaaS hoặc serverless | mất kiểm soát runtime |

**Điểm mấu chốt:** vì image và runtime theo chuẩn OCI, **đổi công cụ phía trên không cần build lại image**; chọn theo cách vận hành, không theo định dạng.

## Tự kiểm tra

1. Digest trong manifest và DiffID trong config khác nhau thế nào? Lab 4.1 kiểm cả hai ra sao?
2. Vì sao `rm` ở layer sau không bảo vệ secret?
3. `history` có 4 mục nhưng image có 3 layer: vì sao?
4. `create` và `start` tách nhau mang lại lợi ích gì?

<details>
<summary>Đáp án ngắn</summary>

1. Digest là SHA-256 của blob nén như lưu; DiffID là SHA-256 của tar chưa nén; lab tự tính cả hai và so (đều khớp).
2. `rm` chỉ thêm tệp `.wh.` ở layer trên; nội dung vẫn nằm nguyên trong layer dưới.
3. Mục `CMD` là `empty_layer`, không tạo layer.
4. Có thể cấu hình mạng (hoặc làm việc khác) giữa hai bước.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 (Docker 29.5.3, `alpine:latest` đã có; script Node tự đọc `index.json`, manifest, config, tar và tự tính SHA-256; đầu ra thật). Lab 4.2 liên kết sang lộ trình Docker.
**Chỉ qua agent (⚠):** OCI image-spec (`manifest`, `image-index`, `layer`, `config`, `descriptor`: whiteout `.wh.`, opaque `.wh..wh..opq`, diffID), runtime-spec (`bundle`, `config`, `config-linux`, `runtime`), distribution-spec; containerd (`getting-started`, CRI `architecture`, `runtime-v2`), runc README; Docker rootless và `troubleshoot` (giới hạn rootless).
**Chưa kiểm:** việc container sống sót khi containerd khởi động lại (docs không nói); `runc` chạy trực tiếp (không có trên máy); ký image (cosign); Podman và rootless thật; ChainID.
