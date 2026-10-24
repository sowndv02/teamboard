# Part 2 — Core Skills (Container)

> [!NOTE]
> **Mục tiêu:** dùng container đúng: phân biệt image và container, tag và digest, biết dữ liệu nằm ở đâu và khi nào mất, cấu hình một image bằng môi trường, và hiểu vòng đời (PID 1, tín hiệu dừng, mã thoát).
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** giải thích vì sao file trong container biến mất mà file trong volume thì không, đọc được mã thoát 137 và 143, chọn tag hay digest cho môi trường production.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Bộ khái niệm dùng hằng ngày

#### Image và container
**Image:** khuôn bất biến. **Container:** một lần chạy của image, thêm một lớp ghi riêng ở trên. Một image chạy ra nhiều container.
**Use case:** một image `board-service` chạy ba container ở ba môi trường.

#### Tag và digest
**Tag:** nhãn có thể bị gán lại (`app:1.2`, `app:latest`). **Digest:** SHA-256 của nội dung, luôn trỏ đúng một image (`app@sha256:...`).
**Use case:** production ghim digest để rollback và kiểm tra được đúng bản đã chạy.

#### Dữ liệu: lớp ghi, volume, bind mount
**Lớp ghi:** mất khi container bị xoá. **Volume:** nơi lưu do engine quản, sống độc lập với container. **Bind mount:** gắn một thư mục của host.
**Use case:** thư mục dữ liệu của DB phải nằm trên volume.

#### Cấu hình bằng môi trường
Cùng một image chạy ở nhiều môi trường nhờ tham số hoá qua biến môi trường hoặc file gắn vào, không build image riêng cho từng môi trường.

#### Vòng đời, PID 1, mã thoát
Container sống đúng bằng **tiến trình đầu tiên (PID 1)**; PID 1 thoát thì container thoát; tín hiệu dừng gửi tới PID 1.

## L2 Why — Vì sao các khái niệm này là tối thiểu

Container "dùng một lần" là chủ ý: nhờ vậy thay thế, mở rộng, rollback dễ. Nhưng điều đó đòi hỏi bạn tách ba thứ ra khỏi container: **mã** (nằm trong image bất biến), **dữ liệu** (nằm ngoài, ở volume hay dịch vụ ngoài) và **cấu hình** (truyền vào lúc chạy). Nhầm một trong ba thì mất dữ liệu, hay deploy ra kết quả khác nhau cùng một lệnh.

## L3 How — Cơ chế đáng nhớ

### Image và container
Image không đổi khi chạy; mọi thứ container ghi nằm ở lớp ghi trên cùng. `docker diff` liệt kê lớp này (Lab 2.1). Xoá container là xoá lớp ghi.

### Tag và digest
Tag là con trỏ có thể di chuyển: `latest` hôm nay và ngày mai có thể là hai image khác nhau, nên cùng lệnh deploy cho kết quả khác và rollback khó. Digest cố định một nội dung; image đa nền tảng có một **index** với digest riêng cho từng kiến trúc (Part 4). Registry có thể bật "tag immutability" để push lại tag đã có bị từ chối. Lab liên quan đã có: [Docker Lab 5.2 (tag đổi, digest không)](../docker/part-5-production-security-ops.md).

### Dữ liệu
- **Lớp ghi:** nhanh nhưng bị xoá cùng container và không chia sẻ được.
- **Volume:** engine quản; sống sau khi container xoá; dùng cho dữ liệu cần giữ.
- **Bind mount:** thư mục host; tiện cho dev, nhưng gắn chặt vào cấu trúc host và quyền file.
- **Dịch vụ ngoài:** dữ liệu quan trọng nằm ở DB hay lưu trữ ngoài, container không trạng thái.

### Cấu hình
Biến môi trường cho cấu hình nhỏ (Lab 2.2); file hoặc secret gắn vào cho cấu hình lớn và bí mật. **Không** nhét secret vào image (layer giữ lại mọi thứ, Part 4) hay vào `ENV` của Dockerfile (ai có image đều đọc được). Chi tiết với Compose và secret: [Docker Part 3](../docker/part-3-multi-container.md) và [Part 4 (Lab 4.3)](../docker/part-4-advanced.md).

### Vòng đời, PID 1 và tín hiệu
- Container chạy **đúng một lệnh chính** là PID 1; thoát thì container thoát (mã thoát của nó là mã của container).
- `stop` gửi `SIGTERM` tới PID 1, đợi một thời gian rồi `SIGKILL`. PID 1 trong Linux **không có hành vi mặc định** với tín hiệu: nếu chương trình không tự bắt `SIGTERM` thì nó bị bỏ qua và phải chờ `SIGKILL`.
- **Mã thoát:** 0 là bình thường; **143 = 128 + 15 (SIGTERM)**; **137 = 128 + 9 (SIGKILL)**, thường là bị dừng cứng hay **OOM killer** (Part 5).
- Chạy lệnh bằng `sh -c` làm shell là PID 1 và có thể không chuyển tiếp tín hiệu cho tiến trình con; dùng dạng `exec` hoặc một init nhỏ (`--init`). Lab đã có: [Docker Lab 1.5 (`--init`, 143)](../docker/part-1-fundamentals.md).

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0. Container và volume `lh-*`, đã dọn sau lab.

#### Lab 2.1 — Lớp ghi mất, volume còn
Container `lh-w1` ghi `/tmp/a.txt` (lớp ghi) và `/data/b.txt` (volume `lh-vol`), rồi bị xoá; container mới `lh-w2` cùng volume.
**Đầu ra thật:**
```text
docker diff lh-w1:   A /data   C /tmp   A /tmp/a.txt
container mới, cùng volume:
   ls /tmp/a.txt  -> ls: /tmp/a.txt: No such file or directory
   ls /data/b.txt -> /data/b.txt      cat -> trong volume
```
**Giải thích:** `docker diff` cho thấy lớp ghi chứa `/tmp/a.txt` (A là thêm, C là đổi); sau khi xoá container nó mất, còn file trong volume vẫn đọc được từ container mới. (`A /data` là điểm gắn volume.)

#### Lab 2.2 — Cùng một image, hai cấu hình
```text
docker run --rm -e ASPNETCORE_ENVIRONMENT=Staging -e LOG_LEVEL=debug alpine ...  -> ENV=Staging LOG=debug
docker run --rm alpine ...                                                        -> ENV=(không đặt)
```
**Giải thích:** một image, hành vi khác nhau theo môi trường mà không build lại.

#### Lab 2.3 — Tag và digest
```text
alpine:latest -> ID sha256:be2656061b53…
RepoDigests: ["alpine@sha256:be2656061b536c1883c4034790e21d0a09c5aaaf51968b89368a3c0371ea2c9a"]
```
**Giải thích:** ở máy này, tag `latest` và digest đang trỏ cùng một nội dung; ngày mai tag có thể trỏ nơi khác, còn `@sha256:...` thì không. Lab đổi tag rồi quan sát nằm ở [Docker Lab 5.2](../docker/part-5-production-security-ops.md).

## L5 Troubleshooting — Khi container không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Mất dữ liệu sau khi tạo lại container | ghi vào lớp ghi (Lab 2.1) | `docker diff`, xem mount | volume hoặc dịch vụ ngoài |
| Deploy hôm nay khác hôm qua | tag đổi (`latest`) | so ID và digest | ghim digest hoặc tag bất biến |
| `docker stop` chờ rất lâu rồi mã 137 | PID 1 không xử lý `SIGTERM` | xem lệnh chạy và mã thoát | bắt tín hiệu, dạng `exec`, `--init` |
| Container thoát ngay | PID 1 kết thúc hoặc lỗi cấu hình | `docker logs`, mã thoát | đọc log; chạy lệnh debug với entrypoint khác |
| Cùng image, môi trường khác chạy sai | thiếu biến môi trường (Lab 2.2) | `docker inspect` phần `Env` | đặt đủ biến; giá trị mặc định rõ |
| Bind mount báo quyền bị từ chối | UID trong container khác chủ thư mục host | `ls -ln` trong và ngoài container | chạy bằng UID khớp; ưu tiên volume |
| Secret xuất hiện trong `docker history` | truyền qua `ARG` hay `ENV` | `docker history`, `inspect` | `--secret` lúc build, secret lúc chạy |
| Ổ đĩa đầy | image, container và volume không dọn | `docker system df` | xoá có chọn lọc đúng tên; không `prune` bừa |

## Tự kiểm tra

1. Một file ghi vào `/tmp` trong container có còn sau khi xoá container không? Vì sao?
2. Vì sao production nên ghim digest?
3. Mã thoát 137 và 143 khác nhau thế nào?
4. Cùng một image cần chạy ở ba môi trường: tham số hoá bằng cách nào?

<details>
<summary>Đáp án ngắn</summary>

1. Không: nó nằm ở lớp ghi của container, bị xoá cùng container (Lab 2.1).
2. Tag có thể đổi sang image khác; digest luôn trỏ đúng một nội dung nên tái lập và rollback được.
3. 143 là `SIGTERM` (dừng êm); 137 là `SIGKILL` (dừng cứng hoặc OOM killer).
4. Biến môi trường hoặc file và secret gắn vào lúc chạy, không build image riêng.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.3 (Docker 29.5.3, `alpine:latest`; đầu ra thật). Các lab liên kết (Docker Lab 1.5, 5.2, 4.3) đã chạy ở lộ trình Docker.
**Đã tự mở:** OCI distribution-spec (tag so với digest) qua agent; Docker Hub rate limits (Part 5).
**Chỉ qua agent (⚠):** distribution-spec (tag khớp mẫu `[a-zA-Z0-9_][a-zA-Z0-9._-]{0,127}`; client phải kiểm digest của manifest); hành vi PID 1 với tín hiệu là kiến thức chung, đã quan sát ở lộ trình Docker.
**Chưa kiểm:** bind mount trên Windows/macOS (qua lớp chia sẻ file của Docker Desktop); tag immutability của registry cụ thể (ECR).
