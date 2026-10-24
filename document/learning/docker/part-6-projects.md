# Part 6 — Real-world Projects (Docker trên TeamBoard)

> [!NOTE]
> **Mục tiêu:** áp dụng toàn bộ Part 1 đến 5 lên repo thật: đọc và chạy image `board-service`, ghép Compose với Jaeger, gỡ lỗi thật của repo, rà Dockerfile theo rule dự án.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md) đến [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** chạy `board-service` bằng image, gọi được `/health/live` và `/health/ready`, tìm và giải thích được lỗi host DB trong compose develop, lập danh sách điểm lệch của Dockerfile.
> **Trạng thái kiểm chứng:** phần đọc cấu hình và image đã kiểm thật; **các lab chạy container ở dự án 1 và 2 chưa chạy** vì Docker Desktop đang tắt khi viết Part này (ghi ⚠, kèm lệnh để bạn chạy).
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Các dự án

Repo dùng ở trạng thái `main` sau TB-001: service `src/board-service`, mã chung `src/shared/ServiceDefaults`, bốn file compose.

| Dự án | Bạn làm | Học được |
|---|---|---|
| 1 | Đọc và chạy image `board-service` | đọc image, cổng, user, health endpoint |
| 2 | Ghép Compose chính với Jaeger | nhiều file Compose, quan sát bằng trace |
| 3 | Gỡ lỗi host DB trong compose develop | DNS theo tên service, đọc `compose config` |
| 4 | Rà Dockerfile và `.dockerignore` theo rule | đối chiếu thực hành với quy tắc dự án |
| 5 | Thiết kế CI build và đẩy image | chuỗi workflow, cache, tag theo SHA (thiết kế) |
| 6 | Triển khai ECS Fargate | thiết kế ở [Part 7](part-7-troubleshooting-mastery.md#l7-architecture--thiết-kế-và-phỏng-vấn) |

**Các file Docker của repo (`src/`):**

| File | Vai trò |
|---|---|
| `board-service/Dockerfile` | 4 stage: `base` (aspnet:10.0, `USER $APP_UID`) → `build` → `publish` → `final` |
| `docker-compose.yml` | Service `board-service`, host 6060 → container 7070 (`ASPNETCORE_URLS`) |
| `docker-compose.observability.yml` | Thêm Jaeger 2.22.0, bind `127.0.0.1`; trỏ OTLP `http://jaeger:4317` |
| `docker-compose.develop.yml` | Thêm `db-dev` (SQL Server 2022 `-latest`, healthcheck `sqlcmd`, volume `sqlserver_dev_data`) |
| `docker-compose.stg.yml` | Env `Staging`, chuỗi kết nối từ `${STG_CONNECTION_STRING}` |

**Health endpoint (`src/shared/ServiceDefaults/ServiceDefaultsExtensions.cs`):** `/health/live` không kiểm dependency (dòng 39); `/health/ready` chạy các check có tag `ready` (dòng 40), hiện là `StartupHealthCheck` báo healthy khi ứng dụng khởi động xong.

## L4 Hands-on — Lab Part 6

#### Lab 6.1 — Đọc image `board-service:latest` (đã kiểm thật)
**Mục tiêu:** biết image đang chạy ai, cổng nào, có healthcheck không.
```bash
docker image inspect board-service:latest -f 'created={{.Created}} size={{.Size}} user={{.Config.User}} entrypoint={{.Config.Entrypoint}} exposed={{.Config.ExposedPorts}} arch={{.Architecture}}'
docker history board-service:latest --format '{{.Size}}\t{{.CreatedBy}}' --no-trunc | head -8
```
**Đầu ra thật** (rút gọn một số lớp `WORKDIR` và `ENV`):
```text
created=2026-10-23T04:40:50Z size=125060811 user=1654 entrypoint=[dotnet board-service.dll] exposed=map[8080/tcp:{}] arch=amd64
0B     ENTRYPOINT ["dotnet" "board-service.dll"]
85.6MB COPY /app/publish . # buildkit
0B     EXPOSE [8080/tcp]
0B     USER 1654
27.4MB COPY /dotnet /usr/share/dotnet # buildkit
```
**Giải thích:** chạy bằng user `1654` (không root, đạt rule); tổng ~125 MB; cổng được `EXPOSE` là 8080 trong khi compose chạy app ở 7070 (xem dự án 4). `Dockerfile` không có chỉ thị `HEALTHCHECK` (đọc trực tiếp file), nên image chưa có healthcheck.

#### Lab 6.2 — Chạy image và gọi health (⚠ chưa chạy)
```bash
docker run -d --name lh-bs -p 127.0.0.1:18200:7070 -e ASPNETCORE_URLS=http://+:7070 board-service:latest
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:18200/health/live
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:18200/health/ready
docker exec lh-bs id
docker rm -f lh-bs
```
**Mong đợi (⚠ chưa xác nhận):** hai `curl` trả `200`; `id` ra `uid=1654`. Chạy lại không đặt `ASPNETCORE_URLS` để xem cổng mặc định.

#### Lab 6.3 — Compose chính + Jaeger (⚠ chưa chạy)
```bash
docker compose -p lh-board -f src/docker-compose.yml -f src/docker-compose.observability.yml up -d --no-build
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:6060/health/live
curl -s http://127.0.0.1:16686/api/v3/services
docker compose -p lh-board -f src/docker-compose.yml -f src/docker-compose.observability.yml down
```
**Mong đợi (⚠ chưa xác nhận):** `board-service` trả 200; Jaeger hiện tên service sau khi có request (`COMMANDS.md` mục 3 ghi API truy vấn chỉ có v3). `--no-build` vì image đã có; không dùng `down -v`.

#### Lab 6.4 — Gộp compose develop và đọc cấu hình (đã kiểm thật, không cần daemon)
```bash
SA_PASSWORD=lab-fake-value docker compose -p lh-cfg -f src/docker-compose.yml -f src/docker-compose.develop.yml config
```
**Đầu ra thật (rút gọn, mật khẩu là giá trị giả):**
```text
board-service:
  environment:
    ASPNETCORE_URLS: http://+:7070
    ConnectionStrings__DefaultConnection: Server=sqlserver,1433;Database=AppDb;User Id=sa;Password=<giả>;...
  depends_on: { db-dev: { condition: service_healthy } }
  ports: published 6060 -> target 7070
db-dev:
  image: mcr.microsoft.com/mssql/server:2022-latest
  ports: host_ip 127.0.0.1, 1433
  healthcheck: test: [CMD, /opt/mssql-tools18/bin/sqlcmd, -S, localhost, -U, sa, -P, lab-fake-value, -C, -Q, SELECT 1]
```
**Giải thích:** hai file được gộp: `ports` và `ASPNETCORE_URLS` của file chính cộng với `depends_on` và biến của file develop. Hai điều cần để ý trong đầu ra này: host DB trong chuỗi kết nối là `sqlserver`, và mật khẩu xuất hiện dạng văn bản trong tham số `-P` của healthcheck.

#### Lab 6.5 — Mô phỏng lỗi DNS của dự án 3 (đã kiểm thật)
```bash
docker network create lh-dns
docker run -d --name lh-db-dev --network lh-dns --network-alias db-dev nginx:alpine
docker run --rm --network lh-dns alpine nslookup db-dev
docker run --rm --network lh-dns alpine nslookup sqlserver
```
**Đầu ra thật:**
```text
Address: 172.19.0.2                     (db-dev: phân giải được)
*** Can't find sqlserver: No answer     (sqlserver: không)
```
**Giải thích:** trên mạng tự tạo chỉ tên service (và alias) được đăng ký. Mô phỏng cho thấy cơ chế; **chưa chạy** chính file compose develop (cần SQL Server và chấp nhận EULA, `ACCEPT_EULA: "Y"`).
**Dọn dẹp:** `docker rm -f lh-db-dev`, `docker network rm lh-dns` (đã dọn).

## L5 Troubleshooting — Gỡ lỗi thật của repo

#### Dự án 3 — App không kết nối được DB trong compose develop
**Triệu chứng dự kiến:** `board-service` khởi động nhưng lỗi khi mở kết nối SQL Server dù `db-dev` đã `healthy`.
**Chẩn đoán:**
1. `docker compose ... config` (Lab 6.4): host trong chuỗi kết nối là `sqlserver`.
2. Tên service DB trong cùng file là `db-dev`.
3. Lab 6.5: tên không khớp tên service hay alias thì **không phân giải**.
**Nguyên nhân gốc:** chuỗi kết nối ở `src/docker-compose.develop.yml` dòng 5 dùng host `sqlserver` thay vì `db-dev`.
**Cách sửa (đề xuất, chưa áp dụng vì ngoài phạm vi tài liệu):** đổi host thành `db-dev`, hoặc thêm `networks.default.aliases: [sqlserver]` cho `db-dev`.
**Phòng ngừa:** thêm bước kiểm `docker compose config` + `nslookup` trong CI cho compose develop.

#### Điểm lệch khác khi rà (ghi nhận, không sửa)
| Điểm lệch | Nơi | Hệ quả |
|---|---|---|
| `EXPOSE 8080` nhưng compose chạy 7070 | `Dockerfile:7`, `docker-compose.yml:8-10` | dễ hiểu nhầm cổng |
| Tag trôi `aspnet:10.0`, `sdk:10.0` | `Dockerfile:4,11` | build không tái lập hoàn toàn |
| `mssql/server:2022-latest` | `docker-compose.develop.yml:10` | trái rule "không `latest`" |
| Chưa có `HEALTHCHECK` | `Dockerfile` | phụ thuộc health endpoint ngoài |
| `.dockerignore` chỉ loại `**/.env` | `src/.dockerignore:3` | `.env.*` và `local/` có thể lọt vào context khi `COPY . .` |
| Mật khẩu qua `-P` trong healthcheck | `docker-compose.develop.yml:28-29` | hiển thị trong cấu hình container ⚠ |

## Các dự án còn lại

#### Dự án 4 — Rà Dockerfile và `.dockerignore` theo rule
Đối chiếu từng dòng của `.agents/rules/docker.md` (multi-stage, base pin, không root, healthcheck, `.dockerignore` loại `.env*` và `local/`, cổng khớp `config/services.yaml`). Đã đạt: multi-stage (4 stage), không root (`USER $APP_UID`, image chạy `1654`), tách `restore` để cache. Chưa đạt: xem bảng trên. **Đầu ra:** danh sách điểm lệch có file:dòng và mức ưu tiên (làm ở [Part 7, bài mở 3](part-7-troubleshooting-mastery.md#l8-mastery--bài-mở)).

#### Dự án 5 — Thiết kế CI build và đẩy image (thiết kế, chưa chạy ⚠)
Chuỗi workflow chuẩn theo tài liệu Docker: checkout → đăng nhập registry → setup buildx → sinh tag từ git ref (metadata) → build-push với cache `type=gha,mode=max` (cache API v1 đã ngừng từ 15/04/2025, cần Buildx 0.21.0 trở lên). Gắn tag theo git SHA để truy vết và rollback. **Lưu ý:** phiên bản cụ thể của các action chưa kiểm; theo `.agents/rules/dependency-policy.md` phải ghim phiên bản và kiểm giấy phép trước khi dùng. Workflow GitHub Actions thật cần issue riêng vì thay đổi `.github/`.

#### Dự án 6 — Triển khai ECS Fargate
Thiết kế ở [Part 7, mục L7](part-7-troubleshooting-mastery.md#l7-architecture--thiết-kế-và-phỏng-vấn). Chưa có hạ tầng (`infra/` thuộc giai đoạn 4).

## Tự kiểm tra

1. Lab 6.4 gộp hai file compose. Cái gì đến từ file nào?
2. Vì sao `EXPOSE 8080` không làm app nghe cổng 8080 khi compose đặt `ASPNETCORE_URLS=http://+:7070`?
3. Vì sao dùng `up --no-build` ở Lab 6.3, và vì sao không dùng `down -v`?
4. Liệt kê 3 điểm lệch Dockerfile/compose so với rule dự án.

<details>
<summary>Đáp án ngắn</summary>

1. File chính: `image`, `build`, `ports` 6060:7070, `ASPNETCORE_URLS`, `restart`; file develop: `ASPNETCORE_ENVIRONMENT`, chuỗi kết nối, `depends_on`, và service `db-dev` kèm volume.
2. `EXPOSE` chỉ là tài liệu về cổng; cổng app nghe do cấu hình (`ASPNETCORE_URLS`).
3. Image đã build sẵn nên không cần build lại (và không phải kéo SDK); `down -v` xoá volume, có thể mất dữ liệu.
4. Ví dụ: `latest` ở `mssql/server`, chưa có `HEALTHCHECK`, `.dockerignore` thiếu `.env*` và `local/`.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** Lab 6.1 (image có sẵn, đọc thật), Lab 6.4 (`docker compose config`, không cần daemon), Lab 6.5 (mô phỏng DNS); đọc trực tiếp `src/board-service/Dockerfile`, `src/.dockerignore`, bốn file compose, `ServiceDefaultsExtensions.cs` và `StartupHealthCheck.cs`.
**Chỉ qua tài liệu (⚠):** chuỗi workflow CI (docs.docker.com/build/ci/github-actions, đã tự mở phần cache); lưu ý quyền truy vấn Jaeger v3 (từ `COMMANDS.md`).
**Chưa chạy:** Lab 6.2 và 6.3 (cần Docker Desktop đang chạy); chính file compose develop (cần SQL Server và `ACCEPT_EULA`); build lại image từ Dockerfile (cần kéo `mcr.microsoft.com/dotnet/sdk:10.0` khoảng 1 GB, chưa được phép).
