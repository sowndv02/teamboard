# Part 4 — Advanced Container Engineering (Docker)

> [!NOTE]
> **Mục tiêu:** build nhanh, image gọn, không lộ secret trong layer, build được cho nhiều kiến trúc, và giới hạn tài nguyên đúng cách.
> **Cần biết trước:** [Part 2](part-2-core-skills.md) và [Part 3](part-3-multi-container.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** viết Dockerfile multi-stage dùng cache mount và build secret, build đa nền tảng, và lý giải một lần container bị OOM kill.
> [← Part 3](part-3-multi-container.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Công cụ build và vận hành nâng cao

#### BuildKit
**Là gì:** bộ máy build của Docker hiện nay: chuyển Dockerfile thành đồ thị các bước, chạy song song bước độc lập và bỏ qua bước không cần.
**Use case:** build nhiều service trong CI nhanh hơn; chỉ build stage mà `--target` cần.

#### Multi-stage build
**Là gì:** nhiều `FROM` trong một Dockerfile; image cuối chỉ lấy kết quả cần chạy (`COPY --from`).
**Ví dụ đời thường:** xưởng gỗ và phòng trưng bày: máy cưa nằm lại xưởng, chỉ bàn ghế thành phẩm ra phòng trưng bày.
**Use case:** build `board-service` bằng image SDK nặng, chạy bằng image `aspnet` nhẹ.

#### Cache nâng cao
**Là gì:** ngoài cache layer, có **cache mount** (`RUN --mount=type=cache`): thư mục cache bền giữa các lần build nhưng **không nằm trong image**; và **xuất/nhập cache** ra registry hoặc GitHub cho CI.
**Use case:** giữ gói NuGet/npm đã tải giữa các lần build; CI máy sạch vẫn dùng lại cache.

#### Build secret
**Là gì:** `--secret` truyền dữ liệu nhạy cảm vào **một bước `RUN`** dưới dạng file tạm, không ghi vào layer.
**Use case:** token đọc registry NuGet riêng khi `dotnet restore`.

#### buildx và đa nền tảng
**Là gì:** `docker buildx build --platform linux/amd64,linux/arm64` tạo một **manifest list**: một tên image trỏ tới biến thể cho từng kiến trúc; khi pull, Docker chọn biến thể khớp máy.
**Use case:** dev trên Mac ARM, server AWS amd64 (hoặc Graviton arm64), cùng một tag.

#### Giới hạn tài nguyên
**Là gì:** `--memory`, `--cpus`, `--pids-limit` đặt giới hạn qua cgroup.
**Use case:** một service rò bộ nhớ không làm chết cả máy; ngăn fork bomb.

## L2 Why — Vì sao cần

- **Build chậm và image nặng:** SDK, cache package, mã nguồn nằm lại trong image làm image lớn, kéo dài thời gian pull và tăng bề mặt tấn công.
- **Secret lộ:** truyền token bằng `ARG` rồi `docker history` đọc được (Lab 4.3).
- **Hai kiến trúc CPU:** image build trên một kiến trúc không chạy được native trên kiến trúc khác.
- **Một service lỗi làm sập máy:** không giới hạn thì một tiến trình ăn hết RAM/CPU của host.

## L3 How — Cơ chế đáng nhớ

#### BuildKit: đồ thị và chỉ build cái cần
Mỗi bước là một nút phụ thuộc nhau; stage độc lập chạy song song, stage không ai cần bị bỏ qua. `--target build` dừng ở stage `build`; kết quả là image chứa mọi thứ của stage đó (65,4 MB trong Lab 4.1).

#### Multi-stage: image cuối chỉ có thứ được `COPY --from`
Bộ công cụ nặng nằm ở stage trước và **không** vào image cuối: cùng nội dung ứng dụng nhưng 65,4 MB (một stage) so với 12,9 MB (multi-stage). Tên stage (`AS build`) an toàn hơn chỉ số thứ tự khi thêm bớt stage.

#### Cache mount: bền giữa các build, vắng mặt trong image
Thư mục `target` của cache mount sống ngoài image: lần build sau thấy dữ liệu của lần trước (Lab 4.2), nhưng container chạy ra từ image không có thư mục đó.

#### Secret mount: chỉ tồn tại trong một lệnh `RUN`
`RUN --mount=type=secret,id=tok` mount file ở `/run/secrets/tok` trong đúng bước đó. Sau bước, file biến mất; `docker history` không chứa giá trị (Lab 4.3).

#### Manifest list và nền tảng
Image đa nền tảng là một **index** trỏ tới manifest riêng cho từng nền tảng. `alpine` công bố 8 nền tảng (`386`, `amd64`, `arm/v6`, `arm/v7`, `arm64/v8`, `ppc64le`, `riscv64`, `s390x`, Lab 4.4). Khi kiến trúc không khớp máy, Docker Desktop chạy qua **QEMU** (emulation): chạy được nhưng chậm; compile nặng nên build native hoặc cross-compile ⚠.

#### Giới hạn tài nguyên
- **Bộ nhớ:** vượt `--memory` (và swap) thì kernel **OOM kill** tiến trình, container thoát **137** và `OOMKilled=true`.
- **CPU:** vượt `--cpus` thì bị **làm chậm** (throttle), không bị giết; `--cpus 0.5` giữ mức dùng quanh 50% một lõi.
- **Số tiến trình:** vượt `--pids-limit` thì `fork` thất bại với `Resource temporarily unavailable`.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 1; BuildKit v0.30.0 (Docker Desktop). Tài nguyên `lh-*` hoặc `--rm`.

#### Lab 4.1 — Multi-stage và `--target`
```dockerfile
# Dockerfile.multi
FROM alpine AS build
RUN head -c 52428800 /dev/zero > /build-toolchain.bin     # giả lập công cụ build nặng 50 MB
RUN echo "app-binary" > /app

FROM alpine AS final
COPY --from=build /app /app
CMD ["cat", "/app"]
```
```bash
docker build -f Dockerfile.single -t lh-ms:single .    # một stage, cùng nội dung
docker build -f Dockerfile.multi  -t lh-ms:multi  .
docker build -f Dockerfile.multi --target build -t lh-ms:buildstage .
docker images --format '{{.Repository}}:{{.Tag}} {{.Size}}' | grep lh-ms
docker run --rm lh-ms:single ls -l /build-toolchain.bin
docker run --rm lh-ms:multi  ls /build-toolchain.bin
```
**Đầu ra thật:**
```text
lh-ms:multi 12.9MB
lh-ms:single 65.4MB
lh-ms:buildstage 65.4MB
52428800 /build-toolchain.bin                    (image một stage còn nguyên toolchain)
ls: /build-toolchain.bin: No such file or directory   (image multi-stage không có)
```
**Giải thích:** cả hai chạy ra `app-binary`, nhưng image multi-stage nhỏ hơn ~5 lần vì không mang toolchain.
**Dọn dẹp:** `docker rmi lh-ms:single lh-ms:multi lh-ms:buildstage`.

#### Lab 4.2 — Cache mount
```dockerfile
FROM alpine
ARG REV=1
RUN --mount=type=cache,target=/pkgcache sh -c 'echo "rev=$REV" >> /pkgcache/log; echo "lần build: $(wc -l < /pkgcache/log)"; cat /pkgcache/log'
```
```bash
for r in 1 2 3; do docker build --progress=plain --build-arg REV=$r -t lh-cache:m$r -f Dockerfile.cache .; done
docker run --rm lh-cache:m3 ls /pkgcache
```
**Đầu ra thật:**
```text
REV=1 -> lần build: 1   (rev=1)
REV=2 -> lần build: 2   (rev=1, rev=2)
REV=3 -> lần build: 3   (rev=1, rev=2, rev=3)
ls: /pkgcache: No such file or directory
```
**Giải thích:** `REV` khác nhau làm layer bị build lại, nhưng thư mục cache **tích luỹ** giữa các lần build; vào container chạy thì không có.

#### Lab 4.3 — Build secret: `ARG` làm lộ, `--secret` không
```dockerfile
# Dockerfile.arg:    ARG TOKEN  +  RUN echo "... $(echo -n $TOKEN | wc -c) ..."
# Dockerfile.secret: RUN --mount=type=secret,id=tok sh -c 'echo "... $(wc -c < /run/secrets/tok) ..."'
```
```bash
docker build -f Dockerfile.arg    --build-arg TOKEN="$(cat token.txt)" -t lh-sec:arg .
docker build -f Dockerfile.secret --secret id=tok,src=token.txt        -t lh-sec:secret .
docker history --no-trunc lh-sec:arg    --format '{{.CreatedBy}}' | grep -c FAKE_TOKEN
docker history --no-trunc lh-sec:secret --format '{{.CreatedBy}}' | grep -c FAKE_TOKEN
docker run --rm lh-sec:secret ls /run/secrets
```
**Đầu ra thật** (token thử là chuỗi giả `ghp_FAKE_TOKEN…` do lab tự tạo):
```text
2      <- ARG: giá trị xuất hiện ở 2 dòng history (dòng ARG và dòng RUN)
0      <- --secret: không có
ls: /run/secrets: No such file or directory
```
**Giải thích:** `docker history` của image dùng `ARG` đọc được token. Secret mount tồn tại trong đúng bước `RUN` rồi biến mất.
**Dọn dẹp:** `docker rmi lh-sec:arg lh-sec:secret`.

#### Lab 4.4 — buildx và đa nền tảng
```bash
docker buildx ls
docker buildx imagetools inspect alpine | grep Platform: | sort -u
docker run --rm --platform linux/arm64 alpine uname -m
docker buildx build --platform linux/amd64,linux/arm64 -t lh-mp:1 --load .
docker run --rm --platform linux/amd64 lh-mp:1
docker run --rm --platform linux/arm64 lh-mp:1
```
**Đầu ra thật:**
```text
desktop-linux* docker   running   BUILDKIT v0.30.0   linux/amd64 (+3), linux/arm64, linux/arm (+2), linux/ppc64le, (2 more)
Platform: linux/386 / linux/amd64 / linux/arm/v6 / linux/arm/v7 / linux/arm64/v8 / linux/ppc64le / linux/riscv64 / linux/s390x
aarch64
build chạy trên linux/amd64, image cho amd64, uname=x86_64
build chạy trên linux/amd64, image cho arm64, uname=aarch64
```
**Giải thích:** máy là amd64 nhưng chạy được image arm64 nhờ emulation (`uname -m` ra `aarch64`). Một lần `buildx build` tạo cả hai biến thể dưới một tên.
**Dọn dẹp:** `docker rmi lh-mp:1`.

#### Lab 4.5 — Giới hạn tài nguyên
```bash
docker run --name lh-oom -m 32m --memory-swap 32m alpine sh -c 'tail /dev/zero'
docker inspect lh-oom -f 'status={{.State.Status}} exit={{.State.ExitCode}} oomKilled={{.State.OOMKilled}}'
docker run --rm --pids-limit 8 alpine sh -c 'for i in $(seq 1 30); do sleep 3 & done'
docker run -d --name lh-cpu  --cpus 0.5 alpine sh -c 'while :; do :; done'
docker run -d --name lh-cpu2        alpine sh -c 'while :; do :; done'
docker stats lh-cpu lh-cpu2 --no-stream --format '{{.Name}} CPU={{.CPUPerc}}'
```
**Đầu ra thật:**
```text
exit(docker run)=137
status=exited exit=137 oomKilled=true
sh: can't fork: Resource temporarily unavailable
lh-cpu  CPU=49.98%
lh-cpu2 CPU=99.81%
```
**Giải thích:** `tail /dev/zero` đọc vô hạn nên vượt 32 MB bị OOM kill. `--pids-limit 8` chặn `fork`. `--cpus 0.5` giữ ~50%, không giới hạn thì ~100% một lõi.
**Dọn dẹp:** `docker rm -f lh-oom lh-cpu lh-cpu2`.

## L5 Troubleshooting — Sự cố build và tài nguyên

| Triệu chứng | Kiểm tra | Nguyên nhân gốc | Sửa |
|---|---|---|---|
| Thoát `137`, restart liên tục | `docker inspect` `OOMKilled`, `docker stats` | vượt `--memory` | tăng giới hạn hoặc sửa rò bộ nhớ |
| `can't fork: Resource temporarily unavailable` | `docker stats` cột PIDS | chạm `--pids-limit` | nâng giới hạn hoặc sửa app tạo tiến trình |
| App chậm mà CPU "không cao" | `docker stats`, so với `--cpus` | bị throttle | nâng `--cpus` |
| Bước cài luôn chạy lại | `--progress=plain`, xem `CACHED` | `COPY . .` trước bước cài | sắp xếp lại (Part 2) |
| `COPY --from` không thấy file ⚠ | `--target` từng stage, `ls` | sai đường dẫn, chỉ số stage lệch | dùng tên stage, kiểm đường dẫn |
| `exec format error` ⚠ | `docker buildx imagetools inspect` | image sai kiến trúc máy | build đúng `--platform` |
| Build đa nền tảng chậm ⚠ | xem bước chạy trên nền tảng lạ | bước biên dịch chạy qua QEMU | build native hoặc cross-compile |

**Sự cố đi sâu: thoát 137 và OOM.** 137 = 128 + 9 (SIGKILL). SIGKILL đến từ hai nơi: `docker stop` hết timeout (Part 1) hoặc kernel OOM kill. Phân biệt bằng `OOMKilled`: `true` là hết bộ nhớ (Lab 4.5), `false` là bị dừng/kill. Sửa tận gốc: nếu là rò bộ nhớ thì sửa ứng dụng; nếu là giới hạn thấp quá thì tăng `--memory` và đặt `--memory-reservation` làm giới hạn mềm.

## L6 Production — Quyết định khi đưa lên thật

- **Cache trong CI:** máy CI sạch mất cache layer; xuất cache ra registry hoặc `type=gha`. `cache-to type=gha,mode=max` cache cả layer trung gian nhưng tốn dung lượng và thời gian export hơn `mode=min`. API cache v1 của GitHub đã ngừng từ 15/04/2025, chỉ còn v2 (cần Buildx 0.21.0 trở lên, BuildKit 0.20.0 trở lên).
- **Cache poisoning:** workflow ít quyền (`pull_request_target`, `issue_comment`) chỉ được đọc cache, và cấp quyền ghi cache cho workflow không tin cậy làm tăng rủi ro cache poisoning. Nên nạp cache từ workflow chạy trên nhánh mặc định.
- **Kích thước image so với khả năng debug:** `alpine` rất nhỏ nhưng dùng musl libc nên có thể khác hành vi ⚠; `distroless` không có shell nên khó `exec` vào debug ⚠. Cân bằng: slim (Debian) là mức giữa ⚠.
- **Giới hạn tài nguyên:** đặt `--memory` quá thấp gây OOM, quá cao thì không bảo vệ host. Đừng tắt OOM killer khi chưa đặt `-m`.
- **Provenance và secret:** `--provenance=mode=max` ghi giá trị build arg vào attestation ⚠; secret mount thì không vào provenance ⚠.
- **Hiệu năng bind mount trên Windows/macOS:** bind mount từ ổ host đi qua ranh giới VM nên chậm; để mã nguồn trong filesystem Linux của WSL2 để nhanh và nhận thay đổi file ⚠.

## L7 Architecture — Chọn công cụ build

#### Phạm vi và những gì Docker KHÔNG đại diện
Phần build của Docker (BuildKit) chỉ là **một lựa chọn** trong bản đồ build ở [Container, Part 4](../container/part-4-advanced.md#l7-architecture--chọn-runtime-và-cách-build):
- **BuildKit** (backend của Docker): cache tốt, build song song, chạy được không cần root.
- **Buildah**: build không cần daemon, từ Dockerfile hoặc script.
- **Kaniko**: build trong cluster không cần daemon, nhưng kho mã nguồn đã **archived từ 03/06/2025**, không còn được phát triển; không chọn cho dự án mới.
- **Chiến lược đa nền tảng:** QEMU (dễ, chậm với compile), nhiều node native (nhanh, phải quản lý), cross-compile bằng `$BUILDPLATFORM`/`TARGETARCH` (không cần emulation nhưng Dockerfile phải chuẩn bị riêng) ⚠.

## Tự kiểm tra

1. Vì sao image multi-stage nhỏ hơn dù cùng ứng dụng?
2. `--secret` khác `ARG` ở điểm nào về `docker history`?
3. Container thoát 137 và `OOMKilled=false`. Nguyên nhân có thể là gì?
4. Vì sao cache mount không làm image phình, dù dữ liệu cache lớn?

<details>
<summary>Đáp án ngắn</summary>

1. Image cuối chỉ lấy file được `COPY --from`; toolchain build nằm lại ở stage trước.
2. `ARG` ghi giá trị vào metadata/lịch sử image; `--secret` chỉ tồn tại trong bước `RUN` và không để lại dấu vết.
3. Bị SIGKILL vì `docker stop` hết timeout (hoặc `docker kill`), không phải hết bộ nhớ.
4. Thư mục cache nằm ngoài layer của image, chỉ tồn tại ở bộ nhớ đệm của builder.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** Lab 4.1 đến 4.5 (kích thước image, cache mount tích luỹ, secret trong history, manifest list và emulation arm64, OOM, pids, CPU).
**Đã tự mở:** docs.docker.com/build/ci/github-actions/cache (cache backends, API v1 ngừng 15/04/2025, cảnh báo cache poisoning); github.com/GoogleContainerTools/kaniko (archived 03/06/2025); docs.docker.com/build/building/best-practices (multi-stage, cache).
**Chỉ qua agent (⚠):** QEMU và chiến lược đa nền tảng (build/building/multi-platform); đặc tính alpine/distroless/slim và musl; provenance `mode=max` lộ build arg (build/metadata/attestations); hiệu năng bind mount trên Windows (desktop/features/wsl/best-practices).
**Chưa kiểm:** `exec format error` và `COPY --from` sai đường dẫn (chưa tái hiện); cache `type=gha`/`registry` (cần CI thật).
