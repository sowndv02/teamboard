# Docker và Docker Compose

> Làm mới lần cuối: 2026-10-23 · Máy hiện có: Docker 29.5.3, Compose v5.1.4 (`config/tool-versions.yaml`: `docker: TODO`, `docker_compose: v2`)

## 1. Vấn đề nó giải quyết

"Chạy được trên máy tôi" nhưng lỗi trên máy khác: khác phiên bản .NET, thiếu SQL Server, khác biến môi trường. Với nhiều service (API, database, Jaeger...) việc cài và nối từng thứ bằng tay càng dễ sai và khó lặp lại.

## 2. Là gì (hiểu trong 1 phút)

- **Docker**: đóng gói ứng dụng cùng runtime và thư viện của nó thành một **image** (khuôn, bất biến). Chạy image ra **container** (một tiến trình bị cô lập). Giống container vận chuyển hàng: bên trong thế nào, cảng nào cũng bốc dỡ được như nhau.
- **Docker Compose**: một file YAML mô tả cả cụm container (API + DB + ...) và nối chúng chung một mạng; `up` là cả cụm chạy.

```
 máy bạn ──6060──► [teamboard-api :7070] ──► [db-dev :1433]
                        mạng compose (gọi nhau bằng tên service)
```

Khác máy ảo (VM): container dùng chung nhân hệ điều hành của máy chủ nên khởi động trong giây và nhẹ hơn nhiều, đổi lại cô lập yếu hơn VM.

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ trong dự án |
|---|---|---|
| Image / container | Khuôn / một lần chạy từ khuôn | `image: teamboard-api` |
| Layer + cache | Mỗi `COPY/RUN` là một lớp; đầu vào không đổi thì dùng lại | `restore` đứng trước `COPY . .` |
| Multi-stage build | Nhiều `FROM` trong một Dockerfile; image cuối chỉ lấy kết quả | SDK để build, `aspnet` để chạy |
| Port mapping `host:container` | Mở cổng máy bạn, nối vào cổng trong container | `6060:7070` |
| Network + DNS compose | Service gọi nhau bằng **tên service**, không `localhost` | `db-dev:1433` |
| Volume | Dữ liệu sống ngoài container, còn sau khi container xoá | `sqlserver_dev_data` |
| Healthcheck + `depends_on: service_healthy` | `depends_on` thường chỉ chờ container *chạy*; healthcheck mới biết DB *sẵn sàng* | DB dev chờ `sqlcmd SELECT 1` |
| `.dockerignore` | Loại file khỏi build context (nhỏ hơn, không lọt secret) | `src/.dockerignore` |
| Biến `${VAR}` | Đọc từ shell hoặc `--env-file`, không ghi secret vào file compose | `${SA_PASSWORD}` |

## 4. Cách dùng

Dockerfile tối thiểu cho .NET (rút gọn từ `src/teamboard-api/Dockerfile`):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY teamboard-api/teamboard-api.csproj teamboard-api/   # chỉ copy csproj trước
RUN dotnet restore teamboard-api/teamboard-api.csproj    # lớp này được cache
COPY . .
RUN dotnet publish teamboard-api -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0                # runtime nhỏ, không có SDK
WORKDIR /app
COPY --from=build /out .
USER $APP_UID                                            # không chạy bằng root
ENTRYPOINT ["dotnet", "teamboard-api.dll"]
```

Compose: gộp nhiều file bằng `-f`, file sau ghi đè/bổ sung file trước.

```bash
docker compose -f src/docker-compose.yml config -q     # kiểm cú pháp, im lặng nếu đúng
docker compose -f src/docker-compose.yml up --build -d # build + chạy nền
docker compose -f src/docker-compose.yml ps            # xem trạng thái
docker compose -f src/docker-compose.yml logs -f teamboard-api
docker compose -f src/docker-compose.yml down          # dừng; KHÔNG thêm -v (xoá volume = mất dữ liệu)
```

Chạy kèm DB dev: thêm `-f src/docker-compose.develop.yml --env-file src/.env.develop`. Sau `up`, mong đợi `ps` thấy container `running`/`healthy` và API trả lời ở http://localhost:6060.

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Môi trường giống nhau giữa dev, CI và production | Học thêm khái niệm và lỗi mạng/volume mới; debug qua lớp container |
| Dựng cả hệ thống nhiều service bằng một lệnh | Chạy nhiều container tốn RAM/đĩa trên máy dev |
| Cách ly phụ thuộc (mỗi service một runtime riêng) | Cô lập yếu hơn VM (dùng chung nhân); cấu hình sai (root, mount rộng) mở lỗ hổng |
| Image bất biến, dễ rollback, hợp CI/CD | Image dễ phình và chứa lỗ hổng cũ nếu không cập nhật base image |
| Compose rất hợp học/dev local | Compose không phải công cụ vận hành production quy mô lớn |

**Nên dùng khi:** nhiều service cần chạy cùng nhau trên máy dev/CI; muốn môi trường tái lập được; triển khai lên dịch vụ chạy container (như ECS Fargate, ADR 0003).
**Không nên / chưa cần khi:** script một file không phụ thuộc gì; cần hiệu năng đĩa/mạng cực cao; nhu cầu scale, tự phục hồi, rolling update nhiều node thì compose không đủ.

**Lựa chọn thay thế:**
- **Podman**: tương tự Docker nhưng không cần daemon chạy nền, chạy rootless mặc định.
- **Kubernetes (EKS)**: điều phối container nhiều node, scale/tự phục hồi; phức tạp và tốn kém hơn (ADR 0003 đã cân và không chọn cho dự án này).
- **ECS Fargate**: chạy container trên AWS không phải quản lý máy chủ; là đích triển khai của dự án.
- **Chạy thẳng trên máy**: nhẹ hơn nhưng không tái lập môi trường.

## 6. Lỗi hay gặp và cách tránh

1. **API không kết nối được DB**: gọi `localhost` hoặc sai tên host. Trong compose phải dùng tên service (xem điểm lệch mục 8).
2. **API khởi động trước khi DB sẵn sàng**: chỉ dùng `depends_on` trơn. Thêm `healthcheck` + `condition: service_healthy`.
3. **Build chậm, không cache**: `COPY . .` đặt trước `restore`, hoặc `.dockerignore` thiếu nên context phình. Copy `.csproj` trước, restore, rồi mới copy code.
4. **Mất dữ liệu DB**: chạy `down -v` hoặc `volume rm`. Dự án cấm các lệnh này khi chưa hỏi.
5. **Secret lọt vào image/lịch sử git**: `COPY` file `.env` hoặc ghi giá trị vào compose; xem [LL-001](../lessons/LL-001-secrets-committed-in-env-files.md). Inject lúc chạy qua `${VAR}`.

## 7. Trong thực tế (real-world)

- **Mô hình phổ biến (Vừa):** cùng một image đi từ máy dev → CI → môi trường chạy; mỗi môi trường chỉ khác cấu hình (biến môi trường), không build lại image. Compose dùng cho local và test tích hợp trong CI; production thường dùng nền tảng điều phối (ECS, Kubernetes).
- **Thực hành tốt (Chắc, theo tài liệu Docker/Microsoft):** multi-stage build, base image pin phiên bản chứ không `latest`, chạy không root, không nhét secret vào image, quét lỗ hổng image trong CI, `.dockerignore` đầy đủ.
- **Dễ sai ở production (Vừa):** dùng tag `latest` làm build không tái lập; image chạy root; thiếu healthcheck nên bộ điều phối không biết container hỏng; log ghi ra file trong container thay vì stdout.
- **Với .NET (Chắc):** Microsoft cung cấp image `sdk` (build) và `aspnet` (runtime) tách riêng và khuyến khích multi-stage, đúng như Dockerfile của dự án.

## 8. Trong TeamBoard

| File | Vai trò |
|---|---|
| `src/teamboard-api/Dockerfile` | 4 stage: `base` (aspnet, `USER $APP_UID`) → `build` → `publish` → `final` |
| `src/docker-compose.yml` | Service `teamboard-api`, host 6060 → container 7070 (`ASPNETCORE_URLS`) |
| `src/docker-compose.develop.yml` | Thêm `db-dev` (SQL Server 2022, healthcheck `sqlcmd`, volume `sqlserver_dev_data`), bind `127.0.0.1:1433` |
| `src/docker-compose.stg.yml` | Env `Staging`, connection string từ `${STG_CONNECTION_STRING}` |
| `src/.env.*.example` | Mẫu biến; file thật bị gitignore (không đọc/in giá trị) |

Rule: [docker.md](../../.agents/rules/docker.md). Lệnh: [COMMANDS.md](../../COMMANDS.md) mục 3. Compose local ↔ ECS Fargate trên AWS: [ADR 0003](../architecture/adr/0003-ecs-fargate.md). Sắp thêm: Jaeger ([plan TB-001 B5](../plans/TB-001-servicedefaults-and-rename.md)), gateway ([design](../design/api-gateway.md)).

Đã kiểm ngày làm mới: `config -q` thành công cho `docker-compose.yml` và cho cặp `yml + develop.yml` (với `SA_PASSWORD` giả); chưa chạy `up`.

**Điểm lệch tìm thấy (chưa sửa, ngoài phạm vi):**
1. `docker-compose.develop.yml` dòng 5 trỏ `Server=sqlserver` nhưng service DB tên `db-dev`: API trong compose sẽ không phân giải được host.
2. `Dockerfile` ghi `EXPOSE 8080`, compose dùng cổng 7070: chạy được nhưng dễ hiểu nhầm.
3. Rule yêu cầu pin phiên bản và health endpoint: `mssql/server:2022-latest` là tag trôi; API chưa có health endpoint (TB-001 sẽ thêm).
4. `.dockerignore` chỉ loại `**/.env`, thiếu `.env.*` và `local/` như rule yêu cầu.

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết | Xác minh |
|---|---|---|---|---|
| 1 | [docker.md](../../.agents/rules/docker.md) | Rule | Quy ước bắt buộc của dự án | Chắc |
| 2 | [observability.md](../architecture/observability.md) mục 6 | Thiết kế | Stack Jaeger/Prometheus/Grafana vào compose | Chắc |
| 3 | [Dockerfile reference](https://docs.docker.com/reference/dockerfile/) | Chính thức | Mọi lệnh Dockerfile | Vừa |
| 4 | [Multi-stage builds](https://docs.docker.com/build/building/multi-stage/) | Chính thức | Cách tách stage | Vừa |
| 5 | [Compose: startup order](https://docs.docker.com/compose/how-tos/startup-order/) | Chính thức | `depends_on` + healthcheck | Vừa |
| 6 | [Compose: biến môi trường](https://docs.docker.com/compose/how-tos/environment-variables/) | Chính thức | `${VAR}`, `--env-file` | Vừa |
| 7 | [Compose: gộp nhiều file](https://docs.docker.com/compose/how-tos/multiple-compose-files/merge/) | Chính thức | Cách `-f` ghi đè | Vừa |
| 8 | [Compose: networking](https://docs.docker.com/compose/how-tos/networking/) | Chính thức | DNS theo tên service | Vừa |
| 9 | [Build image .NET](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images) | Chính thức | Chuẩn của Microsoft cho .NET | Vừa |

(Link "Vừa": truy cập được, chưa đọc kỹ nội dung.)

## 10. Tự kiểm tra / thực hành

1. Chạy `docker compose -f src/docker-compose.yml config` và chỉ ra `ports` sau khi gộp. Đúng khi thấy `6060:7070`.
2. Vì sao `dotnet restore` đứng trước `COPY . .`? Đúng khi nêu được cache layer.
3. Chạy `config` với cả `develop.yml` và tìm lỗi tên host DB ở mục 8.1.
4. Vì sao DB bind `127.0.0.1:1433` chứ không `1433:1433`? Đúng khi nêu: chặn máy khác trong mạng truy cập DB.
5. Vì sao cùng image chạy được ở dev và production mà không build lại? Đúng khi nêu: khác biệt chỉ nằm ở cấu hình runtime.
