# Part 6 — Projects (Container)

> [!NOTE]
> **Mục tiêu:** đối chiếu cách `board-service` đang được đóng gói với một **checklist "sẵn sàng chạy container"** rút từ Part 1 đến 5 và từ `.agents/rules/docker.md`, và nêu chỗ cần người dùng chốt; không sửa `src/`.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** một bảng đối chiếu có bằng chứng cho từng mục, và danh sách điểm lệch kèm đề xuất.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

`board-service` có một `Dockerfile` (multi-stage) và bốn file compose trong `src/`; image `board-service:latest` đã build trên máy này. Production dự kiến chạy trên **ECS Fargate** sau ALB (ADR 0003); local dùng Docker Compose. Quy tắc dự án: `.agents/rules/docker.md` (multi-stage, ghim phiên bản base, không root, có `HEALTHCHECK` hoặc dựa vào health endpoint, không secret trong image, `.dockerignore`, compose có `depends_on` với `service_healthy`).

Lộ trình **không** sửa `src/`, `config/` hay quy tắc. Phần này chỉ **đọc** `Dockerfile`, compose và `docker image inspect` và báo lệch.

## L4 Hands-on — Ba dự án

Môi trường: như Part 0. Chỉ đọc và `inspect`; không chạy container của dự án.

#### Dự án 6.1 — Đối chiếu Dockerfile với checklist
`docker image inspect board-service:latest` và đọc `src/board-service/Dockerfile`:
**Đầu ra thật:**
```text
User=1654 | Entrypoint=["dotnet","board-service.dll"] | Cmd=null | ExposedPorts={"8080/tcp":{}} | Healthcheck=null
Size=125060811 byte | Os/Arch=linux/amd64 | 9 layer
docker history (rút gọn): COPY /app/publish (85,6 MB) · COPY /dotnet (27,4 MB) · COPY /dotnet (83,2 MB) · apt-get (45,8 MB) · USER 1654 · ENTRYPOINT ...
```
| Mục checklist | Bằng chứng | Kết quả |
|---|---|---|
| Multi-stage (build tách runtime) | Dockerfile có 4 stage: `base`, `build`, `publish`, `final`; image cuối chỉ có runtime `aspnet` | đạt |
| Chạy không root | `USER $APP_UID` trong stage `base`; image có `User=1654` | đạt |
| Ghim phiên bản base | `FROM mcr.microsoft.com/dotnet/aspnet:10.0` (tag theo phiên bản phụ, không `latest`, **không ghim digest**) | đạt một phần |
| Lệnh khởi động dạng exec | `ENTRYPOINT ["dotnet","board-service.dll"]` (dạng exec, `dotnet` là PID 1, nhận `SIGTERM`) | đạt |
| Cache layer hợp lý | `COPY *.csproj` và `dotnet restore` trước `COPY . .` | đạt |
| `HEALTHCHECK` hoặc health endpoint | `Healthcheck=null`; dự án có `/health/live` và `/health/ready`; compose chưa gắn healthcheck cho `board-service` | cần chốt |
| Không secret trong image | không `ARG` hay `ENV` chứa secret trong Dockerfile | đạt |
| `.dockerignore` | `src/.dockerignore` có `**/bin`, `**/obj`, `**/.env`, `**/.git`; chưa thấy mẫu `.env.*` hay `local/` | đạt một phần |
| `EXPOSE` khớp cổng chạy | `EXPOSE 8080` nhưng compose đặt `ASPNETCORE_URLS=http://+:7070` và map `6060:7070` | lệch (chỉ là tài liệu, nhưng gây hiểu nhầm) |

**Giải thích:** phần lớn đạt. Điểm lệch nhỏ: `EXPOSE 8080` không khớp cổng 7070 mà compose đặt (`EXPOSE` không mở cổng, chỉ là chú thích). Kích thước: `inspect` báo 125.060.811 byte, còn `docker images` liệt kê 455 MB; đây là hai cách đo khác nhau (chưa điều tra chi tiết), nên chỉ so sánh trong cùng một cách đo.

#### Dự án 6.2 — Dòng thời gian "mất dữ liệu" (bài tập, ⚠ chưa chạy)
`src/docker-compose.develop.yml` đặt SQL Server `db-dev` với volume `sqlserver_dev_data`. **Tiêu chí đạt:** dùng khái niệm Part 2 để giải thích hai kịch bản: `docker compose down` rồi `up` (volume còn), và `down -v` (mất dữ liệu); **không chạy** `down -v` (quy tắc dự án cấm khi chưa được đồng ý). Ghi bằng lời, không chạy.

#### Dự án 6.3 — Sơ đồ chuỗi từ ECS task tới process (bài tập)
Từ ADR 0003: ECS Fargate sau ALB. **Tiêu chí đạt:** vẽ chuỗi kubelet/ECS agent → containerd → shim → runc → tiến trình `dotnet` (Part 4), chỉ ra: probe và `OOMKilled` ở tầng nào, `SIGTERM` tới PID 1 thế nào khi ECS dừng task (`stopTimeout`), và bộ nhớ cấp ở Fargate (cỡ task) liên quan thế nào tới `HeapHardLimitPercent` của .NET. Phần chưa chắc ghi ⚠.

#### Điểm đối chiếu với tài liệu dự án (báo, không tự sửa)
| Tài liệu | Quy định | Quan sát |
|---|---|---|
| `.agents/rules/docker.md` | có `HEALTHCHECK` hoặc dựa vào health endpoint | image và compose của `board-service` chưa có healthcheck; cần chốt dựa vào health endpoint ở ECS (ALB, task) |
| `.agents/rules/docker.md` | ghim phiên bản base | đang ghim `10.0` theo phiên bản phụ; ghim digest là lựa chọn mạnh hơn, cần chốt |
| `.agents/rules/docker.md` | `.dockerignore` loại `.env*` và `local/` | `src/.dockerignore` có `**/.env`; mẫu `.env.*` và `local/` chưa thấy |
| ADR 0003 | ECS Fargate | Dockerfile dùng `USER $APP_UID` (hợp Fargate không root); không có cấu hình dừng êm ở compose ngoài `restart: unless-stopped` |
| `PROJECT.md` | `/health/live`, `/health/ready` | có trong yêu cầu; chưa thấy được dùng ở `HEALTHCHECK` hay compose |

## L5 Troubleshooting — Khi đối chiếu không khớp

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| `docker images` và `inspect` báo kích thước khác nhau | hai cách đo khác nhau | so từng cách | chọn một cách đo và nêu rõ |
| Dịch vụ không truy cập được ở cổng `EXPOSE` | `EXPOSE` chỉ là chú thích | xem `ASPNETCORE_URLS` | map theo cổng thật chạy |
| Compose khởi động trước khi DB sẵn sàng | thiếu `depends_on` với `service_healthy` | xem compose | healthcheck cho dependency |
| Container không dừng êm | PID 1 không xử lý tín hiệu | mã thoát 137 hay 143 | dạng exec, `stopTimeout` hợp lý |
| Mất dữ liệu DB dev | `down -v` hoặc không gắn volume | xem volume | không dùng `-v`; volume có tên |
| Image to hơn mong đợi | layer thừa, base lớn | `docker history` | multi-stage đúng, base nhỏ hơn nếu hợp |

## Tự kiểm tra

1. Mục nào trong checklist `board-service` đạt, mục nào lệch?
2. Vì sao `EXPOSE 8080` không làm cổng 8080 mở?
3. Giải thích kịch bản `down` so với `down -v` bằng khái niệm Part 2.

<details>
<summary>Đáp án ngắn</summary>

1. Đạt: multi-stage, không root, exec form, cache layer, không secret; lệch hoặc cần chốt: `HEALTHCHECK`, ghim digest, `.dockerignore`, `EXPOSE`.
2. `EXPOSE` chỉ là siêu dữ liệu; cổng thật do ứng dụng lắng nghe (`ASPNETCORE_URLS`) và do map cổng quyết định.
3. `down` xoá container nhưng volume còn nên dữ liệu còn; `down -v` xoá cả volume nên mất dữ liệu.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 (`docker image inspect` và `history` của `board-service:latest`; đầu ra thật; chỉ đọc).
**Đã tự mở:** `src/board-service/Dockerfile`, `src/docker-compose.yml`, `src/docker-compose.develop.yml` (một phần), `src/.dockerignore`, `.agents/rules/docker.md`, ADR 0003.
**Chưa kiểm:** Dự án 6.2 và 6.3 là bài tập chưa chạy; chưa đọc đủ các compose (`stg`, `observability`); chưa chạy container của dự án; chưa so với cấu hình ECS thật (chưa có Terraform).
