# Part 2 — Core Engineering Skills (Docker)

> [!NOTE]
> **Mục tiêu:** tự đóng gói và chạy một dịch vụ: viết Dockerfile đúng thứ tự cache, nối mạng theo tên, giữ dữ liệu, truyền cấu hình và đọc log.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** build và chạy một image có healthcheck, giải thích vì sao build chậm hay nhanh, gỡ được 5 lỗi phổ biến ở L5.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-container.md)

## L1 What — Bộ kỹ năng lõi

#### Nhóm lệnh CLI hằng ngày
**Là gì:** `run` tạo và chạy; `ps` liệt kê; `logs` xem đầu ra; `exec` chạy lệnh trong container đang chạy; `inspect` xem cấu hình chi tiết; `stop`/`rm` dừng và xoá; `images`, `pull`, `tag`, `push` quản lý image.
**Use case:**
- Xem vì sao một container thoát ngay → `docker ps -a`, `docker logs`.
- Vào xem file trong container đang chạy → `docker exec -it <tên> sh`.
- Lấy IP, biến môi trường, trạng thái healthcheck → `docker inspect -f '<mẫu>'`.

#### Dockerfile
**Là gì:** file văn bản mô tả cách build image; mỗi chỉ thị tạo một layer.
**Ví dụ đời thường:** công thức nấu ăn: từng bước một, bước nào đổi thì phải nấu lại từ bước đó.
**Use case:** đóng gói `board-service` thành image tái lập được; chạy cùng image ở dev và CI.

| Chỉ thị | Làm gì |
|---|---|
| `FROM` | mở stage mới từ một image nền; đặt tên bằng `AS` |
| `WORKDIR` | đặt thư mục làm việc cho các lệnh sau |
| `COPY` | chép file từ build context (hoặc `--from` stage khác) |
| `RUN` | chạy lệnh lúc build, tạo layer |
| `ENV` / `ARG` | biến còn lại lúc chạy / chỉ có lúc build |
| `EXPOSE` | tài liệu về cổng, **không** mở cổng |
| `ENTRYPOINT` / `CMD` | lệnh chạy khi container khởi động |
| `USER`, `HEALTHCHECK` | user chạy ứng dụng; cách kiểm tra khoẻ |

#### Layer và build cache
**Là gì:** mỗi chỉ thị là một layer; Docker dùng lại layer cũ nếu chỉ thị và đầu vào không đổi. **Khi một layer đổi, mọi layer phía sau phải build lại.**
**Use case:** copy file khai báo phụ thuộc (`*.csproj`) và `restore` **trước**, copy mã nguồn **sau** → sửa code không phải restore lại.

#### `.dockerignore`
**Là gì:** danh sách file loại khỏi build context.
**Use case:** loại `.git`, `bin/`, `obj/`, `.env*` → context nhỏ, build nhanh, secret không lọt vào image.

#### Mạng
**Là gì:** `bridge` (mặc định), `host`, `none`; mạng do bạn tạo (`docker network create`) có DNS theo tên container.
**Use case:** app gọi `lh-db` thay vì IP trong cùng mạng tự tạo; chỉ publish cổng của app ra ngoài, không publish DB.

#### Lưu trữ: volume, bind mount, tmpfs
**Là gì:** volume do Docker quản lý; bind mount trỏ tới thư mục host; tmpfs nằm trong RAM.
**Use case:** dữ liệu DB → volume; mã nguồn khi dev → bind mount; dữ liệu tạm hoặc nhạy cảm → tmpfs.

#### Môi trường và log
**Là gì:** `-e`, `--env-file` đưa cấu hình vào lúc chạy; log của container là **stdout/stderr**, driver `json-file` ghi ra file.
**Use case:** đổi chuỗi kết nối giữa dev và stg mà không build lại image; xoay log để không đầy đĩa.

## L2 Why — Vì sao phải làm đúng từ đầu

**Tình huống:** Dockerfile đầu tiên "chạy được" nhưng: mỗi lần sửa một dòng code lại mất 3 phút restore; image nặng vì chứa cả `.git`; mật khẩu nằm trong `ENV`; `docker rm` xong mất dữ liệu; hai container không gọi được nhau.

Mỗi lỗi trên có một nguyên nhân cơ chế rõ ràng ở L3. Học đúng thứ tự cache, `.dockerignore`, mạng tự tạo và volume từ đầu rẻ hơn nhiều so với sửa khi đã lên production.

## L3 How — Cơ chế đáng nhớ

#### Cache bị vô hiệu theo chiều đi xuống
Docker kiểm từng chỉ thị theo thứ tự: nếu chỉ thị và các file nó dùng không đổi thì dùng layer cũ; **ngay khi một layer đổi, các layer sau đều build lại**. `COPY . .` đặt trước bước cài phụ thuộc nghĩa là mọi thay đổi code làm bước cài chạy lại (Lab 2.3).

#### BuildKit chỉ chuyển file được dùng
Builder hiện đại chỉ gửi những file mà `COPY` tham chiếu. Nếu Dockerfile chỉ `COPY app.txt` thì file 20 MB nằm cạnh không bị gửi dù không có `.dockerignore`; nhưng `COPY . .` thì gửi tất cả (Lab 2.4). Vì vậy `.dockerignore` quan trọng nhất khi dùng `COPY . .`.

#### `ARG` và `ENV` khác nhau ở thời điểm sống
`ENV` còn trong container lúc chạy và `-e` ghi đè được; `ARG` chỉ có lúc build, không có trong container, **nhưng vẫn ghi trong lịch sử image** (`docker history`). Đó là lý do không đưa secret qua `ARG` (Lab 2.8, Part 4).

#### Mạng: DNS theo tên chỉ có ở mạng tự tạo
Mạng `bridge` mặc định không phân giải tên container, chỉ gọi được theo IP. Mạng bạn tạo có DNS nhúng tại `127.0.0.11`, nên container gọi nhau theo tên (Lab 2.5). `localhost` trong container là chính container đó, không phải máy host hay container khác.

#### Publish cổng: `0.0.0.0` hay `127.0.0.1`
`-p 18090:80` mở cổng trên **mọi** địa chỉ (`0.0.0.0` và `[::]`); `-p 127.0.0.1:18091:80` chỉ mở cho máy local (Lab 2.6). Docker coi việc publish cổng là không an toàn theo mặc định, nên cổng nội bộ nên bind `127.0.0.1` ⚠.

#### Dữ liệu: lớp ghi tạm và volume
Ghi vào filesystem container là ghi vào lớp ghi tạm, **mất khi `docker rm`** (`docker diff` hiện các file thêm `A`, đổi `C`). Volume sống ngoài vòng đời container (Lab 2.7).

#### Dạng shell và dạng exec của `CMD`
Dạng exec `CMD ["sleep","300"]` chạy trực tiếp. Dạng shell `CMD a && b` được bọc trong `/bin/sh -c`, nên **`sh` mới là PID 1**, ứng dụng chỉ là tiến trình con và không nhận tín hiệu gửi cho container (Lab 2.10). Với một lệnh đơn trên Alpine, `sh` tự `exec` nên khác biệt không hiện ra; khác biệt hiện khi dùng lệnh ghép.

## L4 Hands-on — Lab Part 2

Môi trường: như Part 1. Thư mục lab và đầu ra đều là của lần chạy thật ngày 23/10/2026. Cổng dùng dải 18080+, bind `127.0.0.1`.

#### Lab 2.1 — CLI cơ bản và publish cổng
```bash
docker run -d --name lh-web -p 127.0.0.1:18080:80 nginx:alpine
docker ps --filter name=lh-web --format 'table {{.Names}}\t{{.Image}}\t{{.Ports}}\t{{.Status}}'
curl -s -o /dev/null -w "HTTP %{http_code}\n" http://127.0.0.1:18080/
docker exec lh-web nginx -v
docker inspect -f 'IP={{(index .NetworkSettings.Networks "bridge").IPAddress}} Cmd={{.Config.Cmd}}' lh-web
```
**Đầu ra thật:**
```text
NAMES     IMAGE          PORTS                     STATUS
lh-web    nginx:alpine   127.0.0.1:18080->80/tcp   Up 2 seconds
HTTP 200
nginx version: nginx/1.31.8
IP=172.17.0.2 Cmd=[nginx -g daemon off;]
```
**Giải thích:** `-p host:container` nối cổng 18080 của máy với cổng 80 trong container. IP `172.17.0.2` là IP trong mạng `bridge`.
**Dọn dẹp:** `docker rm -f lh-web`.

#### Lab 2.2 — Dockerfile đầu tiên có HEALTHCHECK
```dockerfile
FROM nginx:alpine
COPY index.html /usr/share/nginx/html/index.html
EXPOSE 80
HEALTHCHECK --interval=5s --timeout=2s --start-period=2s --retries=3 CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
```
```bash
docker build -t lh-site:1 .
docker run -d --name lh-site -p 127.0.0.1:18081:80 lh-site:1
docker inspect -f 'health={{.State.Health.Status}}' lh-site
```
**Đầu ra thật:** `health=healthy`, và `curl` trả `<h1>lh-site v1</h1>`.
**Giải thích:** lớp `COPY` và `HEALTHCHECK` nằm trên các lớp của `nginx:alpine` (xem `docker history lh-site:1`). `EXPOSE 80` chỉ là tài liệu; cổng chỉ mở khi có `-p`.
**Dọn dẹp:** `docker rm -f lh-site`.

#### Lab 2.3 — Thứ tự lệnh và build cache
Dockerfile tốt: `COPY deps.txt` → `RUN sleep 3` (giả lập cài phụ thuộc) → `COPY src.txt`. Dockerfile xấu: `COPY . .` trước `RUN sleep 3`.
```bash
docker build --progress=plain -t lh-cache:good .
```
**Đầu ra thật (tóm tắt thời gian và trạng thái):**

| Tình huống | Thời gian | Bước cài (`RUN sleep 3`) |
|---|---|---|
| Tốt, lần 1 | 5 giây | chạy |
| Tốt, không đổi gì | 1 giây | `CACHED` |
| Tốt, chỉ đổi `src.txt` | 1 giây | `CACHED` |
| Tốt, đổi `deps.txt` | 4 giây | chạy lại |
| Xấu, chỉ đổi `src.txt` | 4 giây | **chạy lại** |

**Giải thích:** khi chỉ đổi code, thứ tự tốt giữ nguyên layer cài phụ thuộc; thứ tự xấu buộc cài lại mỗi lần.

#### Lab 2.4 — `.dockerignore` và build context
Thư mục có `big.bin` (20 MB), `app.txt`, `Dockerfile` với `COPY . /app/`.
```bash
docker build --progress=plain --no-cache -t lh-ctx:3 .   # có .dockerignore chứa big.bin
```
**Đầu ra thật:**

| | Context chuyển đi | Kích thước image | Có `big.bin` trong image? |
|---|---|---|---|
| Có `.dockerignore` | 138 B | 13 MB | không |
| Không có | 20,98 MB (1,0 giây) | 34 MB | có |

**Giải thích:** không có `.dockerignore`, `COPY . .` đưa cả file lớn vào context và image. Với `COPY app.txt` thì BuildKit không gửi `big.bin` (context chỉ vài chục byte) nên khác biệt ẩn đi.

#### Lab 2.5 — Mạng: bridge mặc định và mạng tự tạo
```bash
docker run -d --name lh-a1 nginx:alpine
docker run --rm alpine wget -qO- -T 3 http://lh-a1          # theo tên, bridge mặc định
docker run --rm alpine wget -qO- -T 3 http://172.17.0.2     # theo IP
docker network create lh-net
docker run -d --name lh-db --network lh-net nginx:alpine
docker run --rm --network lh-net alpine wget -qO- -T 3 http://lh-db   # theo tên
docker run --rm --network lh-net alpine grep nameserver /etc/resolv.conf
```
**Đầu ra thật:**
```text
wget: bad address 'lh-a1'          (bridge mặc định, theo tên: lỗi)
<!DOCTYPE html>                    (bridge mặc định, theo IP: được)
<!DOCTYPE html>                    (mạng tự tạo, theo tên: được)
nameserver 127.0.0.11
```
**Giải thích:** DNS theo tên chỉ có ở mạng tự tạo. `wget http://localhost` trong container cho `Connection refused`, vì `localhost` là chính nó.
**Dọn dẹp:** `docker rm -f lh-a1 lh-db`, `docker network rm lh-net`.

#### Lab 2.6 — Publish cổng và xung đột cổng
```bash
docker run -d --name lh-p1 -p 18090:80 nginx:alpine
docker run -d --name lh-p2 -p 127.0.0.1:18091:80 nginx:alpine
docker port lh-p1; docker port lh-p2
docker run -d --name lh-p3 -p 18090:80 nginx:alpine     # cổng đã bị chiếm
```
**Đầu ra thật:**
```text
80/tcp -> 0.0.0.0:18090
80/tcp -> [::]:18090
80/tcp -> 127.0.0.1:18091
... Bind for 0.0.0.0:18090 failed: port is already allocated
```
**Giải thích:** cổng không chỉ định địa chỉ mở trên mọi giao diện; bản `127.0.0.1` chỉ mở cho máy local.
**Dọn dẹp:** `docker rm -f lh-p1 lh-p2 lh-p3`.

#### Lab 2.7 — Lưu trữ
```bash
docker run --name lh-w1 alpine sh -c 'echo secret-data > /data.txt'
docker diff lh-w1
docker rm lh-w1
docker run --rm alpine cat /data.txt                          # file đã mất
docker volume create lh-vol
docker run --rm -v lh-vol:/data alpine sh -c 'echo persisted > /data/note.txt'
docker run --rm -v lh-vol:/data alpine cat /data/note.txt
docker run --rm --tmpfs /scratch:size=8m alpine df -h /scratch
docker run --rm -v "<thư mục host>:/b:ro" alpine sh -c 'echo x > /b/y.txt'
```
**Đầu ra thật:**
```text
A /data.txt
cat: can't open '/data.txt': No such file or directory
persisted
tmpfs   8.0M   0   8.0M   0% /scratch
sh: can't create /b/y.txt: Read-only file system
```
**Giải thích:** dữ liệu ghi vào lớp ghi tạm mất sau `rm`; volume giữ dữ liệu; `tmpfs` nằm trong RAM (`size=8m`); bind mount thêm `:ro` thì chặn ghi.
**Dọn dẹp:** `docker volume rm lh-vol` (volume do chính lab này tạo).

#### Lab 2.8 — Biến môi trường
Dockerfile: `ENV APP_MODE=default`, `ARG BUILD_TAG=none`, `CMD` in `APP_MODE`, `BUILD_TAG`, `DB_HOST`; build với `--build-arg BUILD_TAG=ci-42`.
```bash
docker run --rm lh-env:1
docker run --rm -e APP_MODE=prod lh-env:1
docker run --rm --env-file app.env lh-env:1                  # app.env: APP_MODE=fromfile, DB_HOST=db.internal
docker run --rm --env-file app.env -e APP_MODE=cli lh-env:1
DB_HOST=from-shell docker run --rm -e DB_HOST lh-env:1
docker inspect -f '{{.Config.Env}}' lh-env:1
docker history --no-trunc lh-env:1 --format '{{.CreatedBy}}' | grep BUILD_TAG
```
**Đầu ra thật (BUILD_TAG luôn `<unset>` khi chạy):**
```text
APP_MODE=default  DB_HOST=<unset>
APP_MODE=prod
APP_MODE=fromfile DB_HOST=db.internal
APP_MODE=cli      DB_HOST=db.internal        (-e thắng --env-file)
APP_MODE=default  DB_HOST=from-shell         (-e KEY lấy từ shell)
[PATH=... APP_MODE=default]                  (không có BUILD_TAG)
ARG BUILD_TAG=ci-42                          (nhưng vẫn nằm trong history)
```
**Giải thích:** thứ tự ưu tiên: `-e` thắng `--env-file`, thắng `ENV` trong image. `ARG` không có lúc chạy nhưng **vẫn lộ trong `docker history`**.

#### Lab 2.9 — Logging và xoay log
```bash
docker run --name lh-log --log-driver json-file --log-opt max-size=1k --log-opt max-file=3 alpine sh -c 'for i in $(seq 1 300); do echo "line $i padding-padding-padding-padding"; done'
docker inspect -f '{{.HostConfig.LogConfig.Type}} {{.HostConfig.LogConfig.Config}}' lh-log
docker logs lh-log 2>&1 | wc -l
docker info --format '{{.LoggingDriver}}'
```
**Đầu ra thật:**
```text
json-file map[max-file:3 max-size:1k]
30                      (đã in 300 dòng, chỉ còn các dòng 271 đến 300)
json-file               (driver mặc định)
```
**Giải thích:** giới hạn kích thước và số file làm các dòng cũ bị xoá; **driver mặc định `json-file` không xoay log** nếu không đặt `max-size`, nên log có thể ăn hết đĩa (L5).
**Dọn dẹp:** `docker rm lh-log`.

#### Lab 2.10 — Dạng shell và dạng exec
```bash
# Dockerfile.shell2:  CMD sleep 300 && echo xong      (dạng shell, lệnh ghép)
# Dockerfile.exec2:   CMD ["sh","-c","exec sleep 300"]
docker top lh-f-s -o pid,ppid,args
docker stop -t 4 lh-f-s
```
**Đầu ra thật:**
```text
dạng shell:  10063 (ppid 10040) /bin/sh -c sleep 300 && echo xong     <- PID 1 là sh
             10078 (ppid 10063) sleep 300                              <- app chỉ là con
stop: 4,5 giây, exit=137
dạng exec:   10153 (ppid 10130) sleep 300                             <- app là PID 1
stop: 4,5 giây, exit=137   (sleep làm PID 1 vẫn không bắt SIGTERM)
app tự bắt SIGTERM (exec): stop 1,1 giây, exit=0
```
**Giải thích:** cấu trúc tiến trình khác nhau, nhưng thoát nhanh hay chậm còn phụ thuộc app có **tự bắt SIGTERM** hay không. Với lệnh đơn trên Alpine, `CMD sleep 300` dạng shell cho cây giống dạng exec (BusyBox tự `exec`).

## L5 Troubleshooting — Sự cố hay gặp

| Triệu chứng | Kiểm tra | Nguyên nhân gốc | Sửa |
|---|---|---|---|
| `failed to connect to the docker API at npipe:...dockerDesktopLinuxEngine` | `docker context ls`, `docker version` | Docker Desktop chưa chạy | mở Docker Desktop, đợi daemon |
| `wget: bad address 'lh-a1'` | `docker network inspect` | mạng `bridge` mặc định không có DNS tên | tạo và dùng mạng riêng |
| `port is already allocated` | `docker ps`, `docker port` | cổng host đã bị container/ứng dụng khác giữ | đổi cổng host hoặc dừng bên giữ |
| thoát `127`, `executable file not found` | `docker run --entrypoint sh` rồi `ls` | lệnh không có trong image | sửa lệnh hoặc image |
| thoát `125` | đọc dòng lỗi từ daemon | image không tồn tại hoặc lỗi cấu hình | kiểm tên image, quyền pull |
| mất dữ liệu sau `rm` | `docker diff <container>` | ghi vào lớp ghi tạm | dùng volume |
| build luôn cài lại phụ thuộc | `--progress=plain`, xem `CACHED` | `COPY . .` đặt trước bước cài | copy file khai báo phụ thuộc trước |
| `stop` chờ đúng timeout rồi `137` | `docker top`, xem PID 1 | PID 1 không bắt SIGTERM hoặc là `sh` bọc | dạng exec, bắt SIGTERM, `--init` |
| build chậm, context lớn | dòng `transferring context` | thiếu `.dockerignore` | loại file thừa |
| đầy đĩa vì log | `docker system df`, xem file log | `json-file` không xoay | đặt `max-size` và `max-file` |

**Sự cố đi sâu: mất dữ liệu sau `rm`.** Triệu chứng: dữ liệu ghi vào container biến mất sau khi tạo lại. Kiểm tra: `docker diff` cho thấy file chỉ nằm ở lớp ghi (`A /data.txt`). Nguyên nhân gốc: filesystem của container là lớp ghi tạm gắn vòng đời container. Sửa: mount volume vào thư mục dữ liệu (Lab 2.7).

## Tự kiểm tra

1. Vì sao copy `*.csproj` và `restore` trước khi `COPY . .`?
2. Hai container trên mạng `bridge` mặc định gọi nhau theo tên thất bại. Vì sao và sửa thế nào?
3. `ARG` khác `ENV` ở điểm nào, và vì sao `ARG` không an toàn cho secret?
4. Khi nào `.dockerignore` không làm context nhỏ đi dù bạn thêm vào?

<details>
<summary>Đáp án ngắn</summary>

1. Layer chỉ build lại từ chỗ đổi; file khai báo phụ thuộc ít đổi nên bước restore giữ cache khi bạn chỉ sửa code.
2. Mạng `bridge` mặc định không có DNS theo tên; tạo mạng riêng bằng `docker network create` và chạy container vào đó.
3. `ENV` có lúc chạy, `ARG` chỉ lúc build; nhưng giá trị `ARG` nằm trong `docker history` của image nên lộ.
4. Khi Dockerfile chỉ `COPY` các file cụ thể: BuildKit đã không gửi file thừa; `.dockerignore` chỉ có tác dụng rõ với `COPY . .`.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** toàn bộ Lab 2.1 đến 2.10 và các số đo trong bảng (thời gian build, kích thước context và image, mã thoát, đầu ra mạng/volume/log).
**Đã tự mở:** docs.docker.com/build/building/best-practices (thứ tự cache, tag và digest, exec form và PID 1, `.dockerignore`).
**Chỉ qua agent (⚠):** mô tả đầy đủ các chỉ thị Dockerfile và giá trị mặc định của HEALTHCHECK (docs.docker.com/reference/dockerfile); câu "publish cổng là không an toàn theo mặc định" (docs.docker.com/engine/network/port-publishing); khuyến nghị bind `127.0.0.1`.
**Chưa kiểm:** `docker push` lên registry thật (chỉ tag cục bộ); xoay log ở cấp `daemon.json` (chỉ dùng `--log-opt` trên một container).
