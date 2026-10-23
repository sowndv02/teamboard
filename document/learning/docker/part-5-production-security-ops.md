# Part 5 — Production, Security & Operations (Docker)

> [!NOTE]
> **Mục tiêu:** đưa container vào vận hành an toàn: siết quyền, kiểm soát chuỗi cung ứng image, quan sát và gỡ lỗi, build/đẩy/lùi phiên bản.
> **Cần biết trước:** [Part 2](part-2-core-skills.md) đến [Part 4](part-4-advanced.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** chạy một container với đủ biện pháp siết quyền, đọc được SBOM/provenance, chẩn đoán một container unhealthy, và lùi phiên bản bằng tag hoặc digest.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Các mảng vận hành

#### Nguyên tắc ít quyền nhất (least privilege)
**Là gì:** cho container đúng quyền cần dùng và không hơn: user không root, filesystem chỉ đọc, bỏ capability thừa, chặn tăng quyền.
**Use case:** nếu một lỗ hổng RCE trong web service bị khai thác, kẻ tấn công không có root, không ghi được đĩa, không có capability để đổi owner hay mở cổng thấp.

#### Chuỗi cung ứng image
**Là gì:** biết image chứa gì và đến từ đâu: **quét lỗ hổng**, **SBOM** (danh sách thành phần), **provenance** (nguồn gốc build), **chữ ký**, **ghim digest**.
**Use case:** khi có lỗ hổng mới trong một thư viện, tra SBOM để biết image nào dính; build lại và deploy.

#### Quan sát và debug
**Là gì:** `docker logs`, `stats`, `events`, `inspect`, `top`, `diff`, `cp`, `exec` và HEALTHCHECK.
**Use case:** container "unhealthy" hay thoát liên tục: đọc log, trạng thái health, mã thoát và sự kiện để tìm nguyên nhân gốc.

#### CI/CD và registry
**Là gì:** quy trình build image, gắn tag, đẩy lên registry, rồi triển khai; registry lưu và phân phối image (Docker Hub, Amazon ECR, GHCR).
**Use case:** mỗi commit build một image gắn tag theo git SHA; rollback bằng cách triển khai lại tag cũ.

#### Vận hành hằng ngày
**Là gì:** restart policy, dọn dẹp tài nguyên, giấy phép công cụ.
**Use case:** service nền tự hồi phục; biết `docker system df` đang tốn gì mà không xoá nhầm dữ liệu.

## L2 Why — Vì sao không được bỏ qua

- **Mặc định là root và nhiều capability:** container chạy thường có `uid=0` và một bộ capability rộng (Lab 5.1). Lỗ hổng trong app thành quyền lớn trong container.
- **Tag có thể đổi nội dung:** `app:stable` hôm nay khác hôm qua; không có cách rollback tin cậy nếu không ghim (Lab 5.2).
- **Không biết image chứa gì:** không SBOM thì không trả lời được "có dùng thư viện lỗi không".
- **Sự cố khó chẩn đoán nếu không có tín hiệu:** container unhealthy hay crash loop cần log, health, mã thoát.

## L3 How — Cơ chế đáng nhớ

#### Capabilities và các cờ siết quyền
Linux chia quyền root thành các **capability**; container mặc định giữ một tập (`CapEff=00000000a80425fb` trong lab). `--cap-drop ALL` đưa về `0`. `--read-only` mount filesystem gốc chỉ đọc; thư mục cần ghi cấp riêng bằng `--tmpfs` hoặc volume. `--security-opt no-new-privileges` chặn tiến trình giành thêm quyền (`NoNewPrivs: 1`). `--user` đặt UID/GID. Mặc định `--user` là root (Lab 5.1).

#### Rootless và userns-remap
- **Rootless:** cả daemon lẫn container chạy bằng user thường trong user namespace; cần `newuidmap`/`newgidmap` và `/etc/subuid`, `/etc/subgid` có ít nhất 65.536 ID; cài bằng `dockerd-rootless-setuptool.sh install`; kiểm tra bằng `docker info` thấy `rootless` ở Security Options.
- **userns-remap** ⚠: ánh xạ root trong container sang user không đặc quyền trên host, nhưng daemon vẫn chạy root.

#### SBOM và provenance là attestation
Khi build với `--sbom=true --provenance=mode=max`, BuildKit đính kèm **attestation** dạng **in-toto Statement** vào image index: một cho provenance (`slsa.dev/provenance/v1`) và một cho SBOM (`spdx.dev/Document`). Lab 5.5 đọc thật các blob này từ file OCI.

#### Tag và digest
Tag là nhãn có thể gán lại; **digest** (`sha256:…`) là mã băm nội dung, luôn trỏ đúng một image. Ghim digest bảo đảm tái lập, đổi lại phải **tự cập nhật** để nhận bản vá (Dependabot hoặc công cụ tương tự).

#### HEALTHCHECK: trạng thái và hệ quả
Container có ba trạng thái health: `starting`, `healthy`, `unhealthy`. Thất bại trong `start-period` không tính vào `retries`. **Docker không tự restart container unhealthy**: trạng thái chỉ để bộ điều phối hoặc người vận hành hành động (Lab 5.3b).

#### Log và sự kiện
`json-file` mặc định không xoay log (Part 2). `docker events` phát sự kiện vòng đời (`create`, `start`, `die`, `kill`, `stop`, `health_status`, `exec_*`); mỗi lần healthcheck chạy sinh `exec_create/start/die`.

#### Giới hạn pull của Docker Hub
Người dùng chưa đăng nhập bị giới hạn **100 lượt pull mỗi 6 giờ theo IPv4 hoặc IPv6 /64**, tài khoản cá nhân đăng nhập **200**, gói trả phí không giới hạn; vượt thì HTTP 429. Runner CI dùng chung IP dễ chạm trần.

## L4 Hands-on — Lab Part 5

Môi trường: như Part 1. Không dùng `prune`, `volume rm` hay `down -v` ngoài tài nguyên lab.

#### Lab 5.1 — Siết quyền
```bash
docker run --rm alpine sh -c 'id; grep CapEff /proc/self/status'
docker run --rm --user 10001:10001 alpine sh -c 'id; touch /etc/x'
docker run --rm --cap-drop ALL alpine sh -c 'grep CapEff /proc/self/status; chown 1:1 /etc/hostname'
docker run --rm --read-only alpine touch /x
docker run --rm --read-only --tmpfs /tmp alpine sh -c 'touch /tmp/ok && echo ghi-được-/tmp'
docker run --rm --user 10001:10001 --read-only --tmpfs /tmp --cap-drop ALL --security-opt no-new-privileges alpine sh -c 'id; grep -E "CapEff|NoNewPrivs" /proc/self/status'
```
**Đầu ra thật (rút gọn):**
```text
uid=0(root) ...                       CapEff: 00000000a80425fb
uid=10001 gid=10001                   touch: /etc/x: Permission denied
CapEff: 0000000000000000              chown: /etc/hostname: Operation not permitted
touch: /x: Read-only file system
ghi-được-/tmp
uid=10001 gid=10001   CapEff: 0000000000000000   NoNewPrivs: 1
```
**Giải thích:** mặc định là root với nhiều capability. Mỗi cờ chặn một lớp; dùng đủ cả bốn thì container gần như không có quyền gì ngoài việc ghi `/tmp`.

#### Lab 5.2 — Tag đổi, digest không, và rollback
```bash
docker build -t lh-pin:stable .                      # nội dung version-A
docker tag lh-pin:stable lh-pin:rollback
docker build -t lh-pin:stable .                      # sửa Dockerfile thành version-B, cùng tên
docker run --rm lh-pin:stable
docker run --rm lh-pin:rollback
docker image inspect alpine -f '{{index .RepoDigests 0}}'
```
**Đầu ra thật:**
```text
tag stable lần 1 -> sha256:d727375f8e49…
tag stable lần 2 -> sha256:47c9932e8f75…   (cùng tên, nội dung khác)
version-B                                   (stable)
version-A                                   (rollback)
alpine@sha256:be2656061b536c1883c4034790e21d0a09c5aaaf51968b89368a3c0371ea2c9a
```
**Giải thích:** `stable` đổi nội dung nhưng `rollback` giữ bản cũ nên lùi được. Chạy `docker run alpine@sha256:…` luôn ra đúng một bản (Alpine 3.24.3 lúc chạy).

#### Lab 5.3 — Quan sát: health, top, diff, cp, stats, events
```bash
docker run -d --name lh-hc --health-cmd 'test -f /tmp/ready' --health-interval 2s --health-retries 2 --health-start-period 1s alpine sh -c 'sleep 6; touch /tmp/ready; sleep 300'
docker inspect lh-hc -f '{{.State.Health.Status}} (failing={{.State.Health.FailingStreak}})'
docker ps --filter name=lh-hc --format '{{.Status}}'
docker top lh-hc; docker diff lh-hc; docker stats lh-hc --no-stream
docker events --since 30s --until now --filter container=lh-hc --format '{{.Type}} {{.Action}}'
```
**Đầu ra thật (rút gọn):**
```text
starting (failing=0) -> starting (failing=1) -> healthy
Up 12 seconds (healthy)
sleep 300 (PID trong host 8720)       C /tmp, A /tmp/ready
CPU=0.00% MEM=1.832MiB / 7.68GiB PIDS=1
exec_create / exec_start / exec_die (6 lần, mỗi lần một lượt healthcheck), health_status: healthy, kill, stop, start
```
**Giải thích:** `FailingStreak` tăng khi `/tmp/ready` chưa có; khi file xuất hiện chuyển `healthy`. `docker events` cho thấy mỗi lượt healthcheck là một `exec`.

#### Lab 5.3b — Healthcheck không bao giờ qua
```bash
docker run -d --name lh-hc2 --health-cmd 'echo "db chưa sẵn sàng" 1>&2; exit 1' --health-interval 1s --health-retries 3 alpine sleep 300
docker inspect lh-hc2 -f 'status={{.State.Health.Status}} failing={{.State.Health.FailingStreak}}'
docker inspect lh-hc2 -f '{{with index .State.Health.Log 0}}exit={{.ExitCode}} output={{.Output}}{{end}}'
docker inspect lh-hc2 -f 'running={{.State.Running}} restarts={{.RestartCount}}'
```
**Đầu ra thật:**
```text
status=unhealthy failing=6
exit=1 output=db chưa sẵn sàng
Up 7 seconds (unhealthy)
running=true restarts=0
```
**Giải thích:** `Health.Log` giữ cả **đầu ra** của lệnh kiểm tra, nơi tìm nguyên nhân. Container vẫn `running`, `RestartCount=0`: Docker không restart vì unhealthy.

#### Lab 5.4 — Debug container thoát ngay
```bash
docker run --name lh-bad alpine sh -c 'echo "khởi động..."; echo "lỗi: thiếu DB_HOST" 1>&2; exit 3'
docker ps -a --filter name=lh-bad --format '{{.Names}} {{.Status}}'
docker logs lh-bad
docker exec lh-bad ls
docker run --rm --entrypoint sh alpine -c 'ls / | head -5'
```
**Đầu ra thật:**
```text
lh-bad Exited (3) Less than a second ago
khởi động...
lỗi: thiếu DB_HOST
Error response from daemon: container ac3856d0e3d7... is not running
bin dev etc home lib
```
**Giải thích:** mã thoát 3 là do app tự chọn; `logs` cho thông báo; `exec` không dùng được trên container đã dừng nên chạy image với `--entrypoint sh` để xem bên trong.

#### Lab 5.5 — SBOM và provenance
```bash
docker buildx build --sbom=true --provenance=mode=max --output type=oci,dest=att.tar -t lh-att:1 .
```
**Kết quả đọc thật từ `att.tar`** (giải nén và duyệt các blob):
```text
9 blob; 3 blob là attestation:
  836 B   index trỏ tới attestation (slsa.dev/provenance/v1, spdx.dev/Document)
  3507 B  in-toto Statement v0.1, predicateType slsa.dev/provenance/v1
  86645 B in-toto Statement v0.1, predicateType spdx.dev/Document   (danh sách gói: alpine-baselayout, alpine-keys, ...)
```
**Giải thích:** SBOM (SPDX) liệt kê từng gói của image; provenance (SLSA v1) ghi cách build. Cả hai nằm **cùng** image index, không phải file riêng.

#### Lab 5.6 — Quét lỗ hổng (chưa chạy)
`docker scout version` cho `v1.21.0`. **⚠ Không chạy quét** vì Docker Scout cần đăng nhập và gửi metadata image tới dịch vụ ngoài; Trivy chưa cài. Dùng `trivy image <image>` hoặc `docker scout cves` khi có chỗ chạy và được phép.

#### Lab 5.7 — Dung lượng (chỉ đọc)
```bash
docker system df
```
**Đầu ra thật (máy lab, đã có dữ liệu thật của bạn):** `Images 31 / 11,88 GB`, `Build Cache 193 / 9,823 GB (reclaimable 9,147 GB)`.
**Giải thích:** image chiếm nhiều nhất (11,88 GB) nhưng chỉ ~5% thu hồi được; build cache nhỏ hơn (9,82 GB) nhưng gần như toàn bộ thu hồi được (9,15 GB), nên là phần "đáng dọn" lớn nhất. **Không** chạy `docker system prune` ở đây: theo `COMMANDS.md` mục 5 và `.agents/rules/docker.md`, lệnh này cần người dùng đồng ý.

## L5 Troubleshooting — Sự cố vận hành

| Triệu chứng | Kiểm tra | Nguyên nhân gốc | Sửa |
|---|---|---|---|
| Container `unhealthy` | `inspect` mục `Health.Log` | lệnh health sai/thiếu công cụ, app chưa sẵn sàng | sửa lệnh, tăng `start-period` |
| `Permission denied` sau khi siết quyền | log, `docker diff` | app ghi vào chỗ chỉ đọc hoặc không có quyền | cấp `--tmpfs`/volume cho đường dẫn cụ thể |
| nginx không chạy với `--user 10001` | log `[emerg] mkdir() "/var/cache/nginx/client_temp" failed (13: Permission denied)` | image gốc ghi vào thư mục chỉ root ghi | image thiết kế cho non-root, hoặc `tmpfs` ⚠ |
| `Read-only file system` | log | `--read-only` bật mà app ghi file | `--tmpfs` cho thư mục cần ghi |
| Thoát liên tục (crash loop) | `docker logs`, mã thoát, `RestartCount` | lỗi cấu hình, thiếu phụ thuộc | sửa lỗi gốc, không chỉ tăng restart |
| Pull lỗi 429 trong CI | log pull | chạm giới hạn pull của Docker Hub | `docker login`, mirror, hoặc ECR |
| Đĩa đầy | `docker system df`, kích thước log | log không xoay, cache và image cũ | `max-size`, dọn có kiểm soát |
| Image dính lỗ hổng mới | SBOM, quét | thư viện/base cũ | cập nhật base, build lại, deploy |
| Rollback không ra bản cũ | `docker image inspect` | tag đã bị ghi đè | triển khai theo digest hoặc tag bất biến |

**Sự cố đi sâu: nginx với `--user` không root.** Lab 5.1 thử chạy `nginx:alpine` bằng `--user 10001:10001 --cap-drop ALL` và nhận `[emerg] mkdir() "/var/cache/nginx/client_temp" failed (13: Permission denied)`. Nguyên nhân gốc: image gốc ghi vào `/var/cache/nginx` chỉ root ghi được; **không phải** vì cổng thấp (nginx dừng trước khi tới bước mở cổng). Hai hướng sửa: dùng image được thiết kế chạy non-root, hoặc cấp `--tmpfs` cho các thư mục cần ghi và cấu hình cổng ≥ 1024 ⚠.

## L6 Production — Quyết định đưa lên thật

- **Siết quyền theo từng bước:** thêm một cờ rồi kiểm app còn chạy; đánh đổi là chi phí cấu hình, đổi lại giảm thiệt hại khi bị xâm nhập. Đặt **UID/GID cụ thể** trong Dockerfile vì UID tự cấp có thể đổi giữa các lần build.
- **Không dùng `--privileged`** trừ khi thật sự cần ⚠ (gần như quyền host).
- **Ghim digest và có kế hoạch cập nhật:** digest tái lập nhưng không tự nhận bản vá; dùng Dependabot (`package-ecosystem: docker`) hoặc công cụ tương tự.
- **Registry và giới hạn:** `docker login` trong CI; cân nhắc Amazon ECR cho dự án chạy trên AWS (tag immutability, lifecycle policy, quét khi push ⚠).
- **CI/CD:** chuỗi phổ biến là checkout → login registry → setup buildx → metadata (tag từ git ref) → build-push → cache `type=gha`; gắn tag theo git SHA để truy vết. Cache `type=gha` có rủi ro poisoning nếu cho workflow không tin cậy ghi (Part 4).
- **Rollback:** triển khai lại tag/digest cũ; cần tag cũ **không bị ghi đè** và chưa bị lifecycle policy xoá ⚠.
- **Quan sát:** log ra stdout/stderr; đặt xoay log; `HEALTHCHECK` và sự kiện để bộ điều phối xử lý.
- **Dọn dẹp:** `docker system prune` xoá container dừng, mạng không dùng, image dangling, build cache; `-a` xoá cả image không dùng; `--volumes` có thể **mất dữ liệu**. Trong dự án này mọi lệnh phá huỷ cần được đồng ý trước.
- **Giấy phép Docker Desktop:** miễn phí cho cá nhân, giáo dục, open source phi thương mại và doanh nghiệp dưới 250 nhân viên **và** dưới 10 triệu USD doanh thu; còn lại cần gói trả phí. Docker Engine và Moby không đổi điều khoản.
- **Chi phí:** registry tính theo dung lượng và băng thông; cache và image cũ tích luỹ nên cần chính sách dọn.

## L7 Architecture — Chọn trong vận hành

#### Phạm vi và những gì Docker KHÔNG đại diện
- **Docker Desktop không phải Docker.** Docker Engine/CLI/Compose là một thứ, Docker Desktop (có điều kiện giấy phép) là gói tiện ích dành cho Windows/macOS. Hướng thay thế: Docker Engine trực tiếp trong WSL2/Linux, Podman, Rancher Desktop, Colima ⚠ (chưa kiểm).
- **Bảo mật container không chỉ là Docker:** quét, ký và giám sát runtime là lớp công cụ riêng (Trivy, Docker Scout, cosign ⚠).
- **Registry:** Docker Hub (giới hạn pull), Amazon ECR (IAM, quét, replication), GHCR (gắn với GitHub). Chọn theo nơi chạy và quyền.

| Việc | Lựa chọn | Đánh đổi |
|---|---|---|
| Quyền ít nhất | cờ siết quyền hoặc rootless | cấu hình và tương thích |
| Tái lập | digest | phải tự cập nhật |
| Quét | Scout, Trivy | cần đăng nhập/gửi dữ liệu hoặc cài thêm |

## Tự kiểm tra

1. Vì sao `docker ps` có thể thấy container `unhealthy` mà Docker không restart nó?
2. Nêu bốn cờ siết quyền ở Lab 5.1 và mỗi cờ chặn điều gì.
3. SBOM khác provenance thế nào, và chúng nằm ở đâu trong image?
4. Vì sao ghim digest không đủ cho bảo mật nếu bỏ mặc?

<details>
<summary>Đáp án ngắn</summary>

1. Healthcheck chỉ báo trạng thái; Docker không tự hành động. Bộ điều phối hoặc người vận hành phải xử lý.
2. `--user` (không root), `--cap-drop ALL` (bỏ capability), `--read-only` (chặn ghi filesystem gốc), `--security-opt no-new-privileges` (chặn tăng quyền).
3. SBOM liệt kê thành phần; provenance ghi cách build; cả hai là attestation đính kèm trong cùng image index.
4. Digest cố định nội dung nên không nhận bản vá tự động; phải cập nhật có chủ đích.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** Lab 5.1 đến 5.5 và 5.7 (siết quyền, tag so với digest, health/events/top/diff, sự cố unhealthy, debug, SBOM và provenance đọc từ file OCI, dung lượng).
**Đã tự mở:** docs.docker.com/engine/security/rootless (điều kiện, cài đặt, kiểm tra, trang không nêu giới hạn); docs.docker.com/docker-hub/usage/pulls (100/200/không giới hạn, 6 giờ, 429); docs.docker.com/build/building/best-practices (tag/digest, UID cố định, exec form); docs.docker.com/build/ci/github-actions/cache.
**Chỉ qua agent (⚠):** userns-remap; Docker Scout và Trivy; cosign và chữ ký; ECR (tag immutability, lifecycle, quét khi push); GHCR; Podman/Rancher Desktop/Colima; `docker system prune` và `--volumes`; giấy phép Docker Desktop (đã thấy trong `.agents/rules/dependency-policy.md`).
**Chưa kiểm:** quét lỗ hổng thật (Lab 5.6), push/pull ECR và GHCR, GitHub Actions thật, rootless trên Linux.
