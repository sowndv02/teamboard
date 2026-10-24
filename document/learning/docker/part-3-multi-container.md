# Part 3 — Multi-container Applications (Docker Compose)

> [!NOTE]
> **Mục tiêu:** mô tả cả hệ thống nhiều dịch vụ bằng một file, chạy bằng một lệnh, và hiểu thứ tự khởi động, DNS, cấu hình, secret.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** viết `compose.yaml` có healthcheck và `depends_on: service_healthy`, truyền cấu hình và secret đúng cách, dừng và xoá đúng cách.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Compose và các thành phần

#### Compose file và project
**Là gì:** file YAML khai báo nhiều **service** (mỗi service là một hoặc nhiều container), mạng, volume, secret. Một lần `docker compose up` là một **project** (tên mặc định lấy từ thư mục hoặc `name:`).
**Ví dụ đời thường:** bản kế hoạch dựng cả một gian hàng: ai đứng đâu, nối dây thế nào, dựng bằng một lệnh.
**Use case:** `board-service` cùng SQL Server và Jaeger chạy bằng `docker compose up`; đồng đội chỉ cần một lệnh.

#### Service và mạng mặc định
**Là gì:** `docker compose up` tự tạo mạng `<project>_default` và gắn mọi service vào; mỗi service gọi được service khác **theo tên service**.
**Use case:** worker gọi `http://api` mà không cần biết IP; IP đổi khi container tạo lại nhưng tên giữ nguyên.

#### `ports` và `expose`
**Là gì:** `ports` mở cổng ra host (`HOST:CONTAINER`); service gọi nhau bằng cổng **container**, không cần `ports`.
**Use case:** chỉ publish API (`127.0.0.1:6060:7070`); DB không publish ra host nếu chỉ app cần.

#### `depends_on` và healthcheck
**Là gì:** `depends_on` kiểm soát thứ tự. Dạng ngắn chỉ chờ container **chạy**; dạng dài `condition: service_healthy` chờ healthcheck **qua**; còn `service_started` và `service_completed_successfully` (cho job chạy xong).
**Use case:** app chờ DB nhận kết nối rồi mới chạy; job migrate chạy xong rồi app mới bật.

#### Biến môi trường, `.env`, interpolation
**Là gì:** `environment`/`env_file` đặt biến cho container; `.env` và biến shell dùng để **nội suy** `${VAR}` trong chính file Compose.
**Use case:** cùng một `compose.yaml` cho dev và stg, khác nhau ở `.env.dev`, `.env.stg`.

#### Secrets và configs
**Là gì:** `secrets` đưa dữ liệu nhạy cảm vào container dưới dạng **file** tại `/run/secrets/<tên>`; chỉ service được liệt kê mới thấy.
**Use case:** mật khẩu DB đọc từ file thay vì biến môi trường.

#### Profiles
**Là gì:** gán `profiles: [debug]` cho service để nó chỉ chạy khi bật profile; service không có profile luôn chạy.
**Use case:** Jaeger hay công cụ debug chỉ bật khi cần.

#### Restart policy
**Là gì:** `restart: no | on-failure[:N] | always | unless-stopped`.
**Use case:** service nền tự hồi phục sau crash hoặc sau khi khởi động lại máy.

## L2 Why — Vì sao cần Compose

**Tình huống:** chạy `board-service` cần SQL Server, Jaeger, đúng cổng, đúng biến môi trường, đúng thứ tự. Gõ từng lệnh `docker run` dài dòng, mỗi người một kiểu, quên `--network` hay quên biến thì hỏng.

Compose đưa tất cả vào một file có thể commit, đọc và lặp lại. Nó **không** thay thế bộ điều phối cho production nhiều máy (xem L7).

## L3 How — Cơ chế đáng nhớ

#### Mạng của project và DNS theo tên service
`up` tạo mạng `lh-compose_default` (kiểu `bridge`) và mỗi service đăng ký tên của nó với DNS nhúng; container gọi nhau bằng `api`, `db`... (Lab 3.3). Dùng `-p` hoặc `name:` khác nhau để hai project không đụng tên.

#### Thứ tự khởi động
Dạng ngắn `depends_on: [api]` chỉ đảm bảo container `api` đã **bắt đầu**, chưa chắc dịch vụ bên trong đã sẵn sàng. `service_healthy` buộc chờ healthcheck qua. Lab 3.2 đo được: `early` bắt đầu sau 1 giây và gọi thất bại; `worker` chờ ~8 giây đến khi `api` healthy mới chạy.

#### Thứ tự ưu tiên của biến khi nội suy (từ cao xuống thấp)
1. biến trong shell chạy lệnh; 2. `--env-file`; 3. `.env` mặc định; 4. giá trị mặc định `${VAR:-mặc định}`. Lab 3.1 đã kiểm: `GREETING` là `from-shell` khi đặt trong shell (kể cả khi có `--env-file`), `from-envfile` khi chỉ có `--env-file`, `from-dotenv` khi chỉ có `.env`. Docs còn mô tả thêm các nguồn dành cho biến trong container (`environment`, `env_file`, `run -e`) ⚠.

#### Quy tắc nội suy
`${VAR:-x}` dùng `x` nếu `VAR` không đặt **hoặc rỗng**; `${VAR:?lỗi}` dừng và báo lỗi nếu thiếu; `$$` là ký tự `$` thật. Lab 3.7.

#### Secret là file, không phải biến
Compose mount secret thành file chỉ đọc ở `/run/secrets/<tên>`, và chỉ vào service nào khai báo `secrets:`. `docker inspect` không hiện giá trị trong danh sách biến môi trường (Lab 3.3).

#### `down` và `down -v`
`down` xoá container và mạng, **giữ volume**; `down -v` xoá cả volume đã khai báo → mất dữ liệu (Lab 3.5).

## L4 Hands-on — Lab Part 3

Môi trường: như Part 1. Project lab tên `lh-compose`; mọi tài nguyên bắt đầu bằng `lh-`. `down -v` chỉ chạy trên chính project lab này.

File `compose.yaml` dùng chung (rút gọn phần giả lập chậm):
```yaml
name: lh-compose
services:
  api:
    image: nginx:alpine
    command: sh -c "sleep 6; exec nginx -g 'daemon off;'"   # khởi động chậm 6 giây
    environment: { GREETING: "${GREETING:-hello}" }
    secrets: [api_key]
    ports: ["127.0.0.1:18100:80"]
    volumes: ["lh-data:/data"]
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://127.0.0.1/ >/dev/null || exit 1"]
      interval: 2s
      retries: 10
      start_period: 1s
  worker:                       # chờ api healthy
    image: alpine
    depends_on: { api: { condition: service_healthy } }
    command: sh -c 'echo "[$$(date +%S)] worker start"; wget -qO- http://api | head -c 15'
  early:                        # dạng ngắn
    image: alpine
    depends_on: [api]
    command: sh -c 'echo "[$$(date +%S)] early start"; wget -qO- -T 2 http://api 2>&1 | head -c 40'
  tools:
    image: alpine
    profiles: [debug]
    command: sh -c "echo tools-ran"
secrets:
  api_key: { file: ./api_key.txt }
volumes:
  lh-data:
```

#### Lab 3.1 — `config`: xem kết quả sau nội suy và ưu tiên biến
```bash
docker compose config | grep GREETING                          # có .env: GREETING=from-dotenv
GREETING=from-shell docker compose config | grep GREETING
docker compose --env-file other.env config | grep GREETING     # other.env: GREETING=from-envfile
GREETING=from-shell docker compose --env-file other.env config | grep GREETING
docker compose config --profiles
```
**Đầu ra thật:**
```text
GREETING: from-dotenv
GREETING: from-shell
GREETING: from-envfile
GREETING: from-shell        (shell thắng cả --env-file)
debug
```
**Giải thích:** `docker compose config` là cách an toàn nhất để biết giá trị cuối cùng trước khi chạy. Biến trong shell thắng `--env-file`; `--env-file` thay thế file `.env` mặc định.

#### Lab 3.2 — `service_healthy` so với dạng ngắn
```bash
docker compose up -d --wait
docker compose logs worker early --no-log-prefix
docker compose ps --format 'table {{.Service}}\t{{.State}}'
```
**Đầu ra thật:**
```text
[54] bắt đầu up
[55] early start
wget: can't connect to remote host (172.…        <- early gọi khi api chưa sẵn sàng
[02] worker start
<!DOCTYPE html>                                  <- worker chờ ~8 giây rồi gọi thành công
api       running   (healthy)
```
**Giải thích:** `early` chỉ chờ `api` **bắt đầu**; `worker` chờ `api` **healthy**. Đây là khác biệt thực tế giữa hai dạng `depends_on`.

#### Lab 3.3 — Secret, biến và DNS
```bash
docker compose exec -T api sh -c 'ls -l /run/secrets; cat /run/secrets/api_key'
docker compose exec -T api sh -c 'echo GREETING=$GREETING'
docker inspect lh-compose-api-1 -f '{{.Config.Env}}' | tr ' ' '\n' | grep -ci super-secret
docker compose run --rm -T early sh -c 'nslookup api | tail -2'
```
**Đầu ra thật:**
```text
-rwxrwxrwx 1 root root 19 ... api_key       (nội dung: super-secret-value)
GREETING=from-dotenv
0                                           (giá trị secret không nằm trong biến môi trường)
Address: 172.19.0.2                         (tên service `api` được phân giải)
```
**Giải thích:** `worker` **không** có thư mục `/run/secrets` vì không khai báo `secrets:`. Quyền `0777` của file là đặc điểm của môi trường Docker Desktop trên Windows (file nguồn nằm ở ổ Windows); không nên coi đó là chuẩn trên Linux.

#### Lab 3.4 — Profile
```bash
docker compose ps -a --format '{{.Service}}={{.State}}'    # chưa bật profile: không có tools
docker compose --profile debug up -d tools
```
**Đầu ra thật:** `api=running early=exited worker=exited` (trước), rồi container `lh-compose-tools-1` được tạo và chạy xong (`tools=exited`).
**Giải thích:** service có `profiles` chỉ chạy khi chỉ định tên hoặc bật `--profile`.

#### Lab 3.5 — `down` và `down -v`
```bash
docker compose exec -T api sh -c 'echo kept > /data/n.txt'
docker compose down                       # giữ volume
docker volume ls --filter name=lh-compose
docker compose up -d --wait && docker compose exec -T api cat /data/n.txt
docker compose down -v                    # xoá cả volume (chỉ project lab)
docker volume ls --filter name=lh-compose | wc -l
```
**Đầu ra thật:** sau `down` volume `lh-compose_lh-data` còn; đọc lại file được `kept`; sau `down -v` còn `0` volume.
**Giải thích:** `-v` là cờ phá huỷ; chỉ dùng khi chắc chắn (xem L6 Part 5).

#### Lab 3.6 — Restart policy
```bash
docker run -d --name lh-rs --restart on-failure:3 alpine sh -c 'echo crash; exit 1'
sleep 12
docker inspect lh-rs -f 'status={{.State.Status}} exit={{.State.ExitCode}} restarts={{.RestartCount}}'
```
**Đầu ra thật:** `status=exited exit=1 restarts=3`.
**Giải thích:** `on-failure:3` thử lại tối đa 3 lần rồi dừng; `unless-stopped` đặt cho service nền, `restarts=0` khi chưa lỗi.
**Dọn dẹp:** `docker rm -f lh-rs`.

#### Lab 3.7 — Nội suy biến
```yaml
environment:
  MODE: ${MODE:-dev}
  DB_HOST: ${DB_HOST:?DB_HOST là bắt buộc}
```
**Đầu ra thật:**
```text
(thiếu DB_HOST)  error while interpolating ... required variable DB_HOST is missing a value: DB_HOST là bắt buộc
DB_HOST=db.local, MODE không đặt     -> MODE: dev
MODE= (rỗng)                         -> MODE: dev
MODE=prod                            -> MODE: prod
```
**Giải thích:** `:?` biến thiếu biến thành lỗi sớm thay vì chạy với giá trị rỗng.

## L5 Troubleshooting — Sự cố Compose

| Triệu chứng | Kiểm tra | Nguyên nhân gốc | Sửa |
|---|---|---|---|
| App crash lúc khởi động vì DB chưa sẵn sàng | `compose logs`, `ps` | `depends_on` dạng ngắn | `service_healthy` + healthcheck |
| `service_healthy` kẹt mãi | `docker inspect` mục `State.Health.Log` | healthcheck sai lệnh, thiếu công cụ trong image | sửa lệnh, thêm công cụ |
| Không resolve được tên service | `compose config`, `network inspect` | khác mạng, hoặc gọi `localhost` | dùng tên service, cùng mạng |
| Biến sai giá trị | `docker compose config` | nguồn có ưu tiên cao hơn ghi đè | kiểm thứ tự ưu tiên |
| Secret không đọc được | `compose exec <svc> ls /run/secrets` | service chưa khai báo `secrets:` | thêm vào service |
| Mất sạch dữ liệu DB | `docker volume ls` | `down -v` đã xoá volume | không dùng `-v` trên dữ liệu thật |
| Service "không chạy" | `compose ps -a` | có `profiles` mà chưa bật | `--profile` hoặc chỉ định tên |
| Sửa Dockerfile nhưng chạy bản cũ | `compose images` | không build lại | `up --build` |

**Sự cố đi sâu: lỗi thật trong repo TeamBoard.** `src/docker-compose.develop.yml` dòng 5 đặt chuỗi kết nối `Server=sqlserver,1433`, trong khi service DB tên `db-dev`. Trên mạng project, tên `sqlserver` **không được phân giải**: lab mô phỏng (một container tên `db-dev` trên mạng tự tạo) cho `nslookup db-dev` ra địa chỉ, còn `nslookup sqlserver` ra `Can't find sqlserver: No answer`. Chưa chạy chính file compose của repo (cần SQL Server và `SA_PASSWORD`). Triệu chứng: app crash khi kết nối DB. Kiểm tra: `docker compose -f ... config` và `docker compose exec board-service getent hosts sqlserver`. Nguyên nhân gốc: tên host không khớp tên service. Sửa: dùng `db-dev` hoặc đặt `networks.default.aliases`. (Ghi nhận trong lộ trình, chưa sửa code vì ngoài phạm vi; xem Part 6.)

## L7 Architecture — Compose khi nào đủ, khi nào không

#### Phạm vi và những gì Compose KHÔNG đại diện
- **Compose không phải bộ điều phối nhiều máy.** Docker docs: chạy Compose ở production được trên **một server** (bỏ bind mount mã nguồn, thêm `restart`, tách file `compose.production.yaml`); cần mở rộng nhiều máy thì dùng Swarm hoặc nền tảng khác.
- **Kubernetes, ECS Fargate** lo lập lịch, tự hồi phục, rolling update, mở rộng nhiều node. Dự án chọn **ECS Fargate** cho AWS (ADR 0003) và Compose cho local.
- **Khác Docker Swarm:** Swarm là chế độ điều phối trong Docker Engine; Compose file có thể dùng làm điểm khởi đầu nhưng chạy khác.

| Cần | Chọn |
|---|---|
| Môi trường dev/CI một máy | Compose |
| Production một máy nhỏ | Compose (có hạn chế) |
| Nhiều máy, tự hồi phục, rolling update | ECS Fargate, Kubernetes |

## Tự kiểm tra

1. `depends_on: [db]` có đảm bảo DB sẵn sàng không? Cần gì thêm?
2. Thứ tự ưu tiên giữa biến shell, `--env-file` và `.env`?
3. `docker compose down` và `down -v` khác nhau thế nào, và vì sao nguy hiểm?
4. Vì sao `worker` không đọc được `/run/secrets` trong Lab 3.3?

<details>
<summary>Đáp án ngắn</summary>

1. Không, chỉ đảm bảo container đã bắt đầu. Thêm healthcheck cho `db` và `condition: service_healthy`.
2. Shell cao nhất, rồi `--env-file` (thay `.env` mặc định), rồi `.env`, rồi giá trị mặc định `:-`.
3. `down` giữ volume, `down -v` xoá cả volume đã khai báo nên mất dữ liệu.
4. Vì chỉ service khai báo `secrets:` mới được mount secret.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** Lab 3.1 đến 3.7 (đo thời gian khởi động, DNS, secret, volume, restart, nội suy).
**Đã tự mở:** docs.docker.com/compose/how-tos/production (Compose chạy production trên một server; Swarm khi cần mở rộng; tách `compose.production.yaml`).
**Chỉ qua agent (⚠):** danh sách điều kiện `service_started`/`service_healthy`/`service_completed_successfully` và cờ `--wait` (compose/how-tos/startup-order); mô tả `profiles` (compose/how-tos/profiles); thứ tự ưu tiên đầy đủ cho biến trong container, gồm 5 nguồn (envvars-precedence); mô tả `secrets` và `configs` trong tham chiếu Compose.
**Chưa kiểm:** `configs`, `docker compose up --abort-on-container-exit`, hành vi trên Linux của quyền file secret.
