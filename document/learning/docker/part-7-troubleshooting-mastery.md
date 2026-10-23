# Part 7 — Troubleshooting, Interview & Mastery (Docker)

> [!NOTE]
> **Mục tiêu:** tự chẩn đoán sự cố không có gợi ý, giải thích đánh đổi thiết kế, và kiểm tra tổng hợp mức thành thạo.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md) đến [Part 6](part-6-projects.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** gỡ đúng 6 trên 8 kịch bản dưới đây không nhìn đáp án, trả lời được câu hỏi thiết kế về triển khai lên ECS Fargate, hoàn thành một bài mở ở L8.
> [← Part 6](part-6-projects.md) · [Part 0 →](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi kịch bản có triệu chứng và câu hỏi chẩn đoán; hãy tự trả lời trước, rồi mở đáp án. Kịch bản 1 đến 5 và 8 dựa trên lab đã chạy ở Part 1 đến 5; kịch bản 6 dựa trên nghiên cứu công bố; kịch bản 7 là lỗi thật trong repo.

#### Kịch bản 1 — Build hôm qua chạy, hôm nay lỗi
**Triệu chứng:** pipeline CI không đổi mã nhưng build lỗi hoặc hành vi khác. Dockerfile dùng `FROM mcr.microsoft.com/dotnet/aspnet:10.0` và `mssql/server:2022-latest`.
**Câu hỏi chẩn đoán:** thứ gì không nằm trong commit mà vẫn đổi được kết quả build? Lệnh nào so sánh?
<details>
<summary>Đáp án</summary>

Tag là nhãn có thể gán lại; nhà xuất bản đổi nội dung `10.0` hay `latest` thì build đổi theo (Part 5, Lab 5.2: cùng tên `stable` nhưng ID khác). So sánh bằng `docker image inspect <image> -f '{{.Id}}'` hoặc `docker buildx imagetools inspect`. Sửa: ghim digest (`image:tag@sha256:…`) và có quy trình cập nhật (Dependabot), vì ghim thì không tự nhận bản vá.
</details>

#### Kịch bản 2 — Container tự thoát liên tục với mã 137
**Triệu chứng:** `docker ps` thấy `Restarting`/`Exited (137)`.
**Câu hỏi chẩn đoán:** 137 có hai nguồn; làm sao phân biệt?
<details>
<summary>Đáp án</summary>

137 = 128 + 9 (SIGKILL), từ `docker stop` hết timeout/`docker kill`, hoặc từ kernel OOM kill. Phân biệt bằng `docker inspect -f '{{.State.OOMKilled}}'`: `true` là hết bộ nhớ (Part 4, Lab 4.5), `false` là bị dừng/kill. Nếu OOM: sửa rò bộ nhớ hoặc tăng `--memory`; nếu do `stop`: app làm PID 1 không bắt SIGTERM (Part 1, Lab 1.5), dùng exec form hoặc `--init`.
</details>

#### Kịch bản 3 — Container "unhealthy" mãi
**Triệu chứng:** `docker ps` hiện `(unhealthy)`, dịch vụ phụ thuộc không bao giờ khởi động (`service_healthy` kẹt).
**Câu hỏi chẩn đoán:** thông tin nào cho biết vì sao healthcheck thất bại?
<details>
<summary>Đáp án</summary>

`docker inspect <c> -f '{{range .State.Health.Log}}{{.ExitCode}} {{.Output}}{{end}}'` cho mã thoát và **đầu ra** của lệnh kiểm tra (Part 5, Lab 5.3b: `exit=1 output=db chưa sẵn sàng`). Nguyên nhân thường gặp: lệnh sai, image không có công cụ (ví dụ thiếu `curl`/`wget`), app chưa sẵn sàng nên cần `start-period` dài hơn. Nhớ: Docker **không** tự restart container unhealthy.
</details>

#### Kịch bản 4 — Pull lỗi trong CI, local thì được
**Triệu chứng:** job CI báo lỗi giới hạn khi `docker pull`; máy bạn kéo bình thường.
**Câu hỏi chẩn đoán:** vì sao chỉ CI bị, và có cách nào giảm phụ thuộc?
<details>
<summary>Đáp án</summary>

Docker Hub giới hạn người dùng chưa đăng nhập ở 100 lượt pull mỗi 6 giờ theo IPv4 hoặc IPv6 /64; runner dùng chung IP nên chạm trần; tài khoản cá nhân đăng nhập là 200, gói trả phí không giới hạn; vượt thì 429. Sửa: `docker login` trong workflow, dùng registry mirror hoặc ECR/MCR (repo này dùng `mcr.microsoft.com` nên ít bị ảnh hưởng).
</details>

#### Kịch bản 5 — Đĩa đầy dù không ghi gì
**Triệu chứng:** host báo đầy đĩa; container dịch vụ đã chạy nhiều tuần.
**Câu hỏi chẩn đoán:** hai nơi nào Docker tích luỹ dữ liệu ngầm, và lệnh nào xem mà không xoá?
<details>
<summary>Đáp án</summary>

Log `json-file` không xoay theo mặc định (Part 2, Lab 2.9: 300 dòng chỉ còn 30 khi đặt `max-size`, nhưng không đặt thì giữ hết), và image/cache build tích luỹ (Part 5, Lab 5.7: build cache là phần thu hồi được lớn nhất, 9,15 GB trong 9,82 GB). Xem bằng `docker system df` (chỉ đọc). Sửa: đặt `max-size`/`max-file` ở cấp daemon; dọn có kiểm soát và **được đồng ý** vì `prune` có thể xoá thứ cần cho rollback.
</details>

#### Kịch bản 6 — Token xuất hiện trong lịch sử image
**Triệu chứng:** quét image phát hiện token/khoá ở layer; image đã đẩy lên registry.
**Câu hỏi chẩn đoán:** xoá file token ở layer sau có đủ không? Cần làm gì?
<details>
<summary>Đáp án</summary>

Không đủ: layer cũ vẫn chứa dữ liệu, và `ARG`/`ENV` nằm trong lịch sử (Part 4, Lab 4.3: `docker history` của image dùng `ARG` lộ token, của `--secret` thì không). Làm ngay: **coi token đã lộ và xoay (rotate) nó** (dự án đã có bài học tương tự: [LL-001](../../lessons/LL-001-secrets-committed-in-env-files.md)), gỡ image đã đẩy, build lại bằng `RUN --mount=type=secret`, loại `.env*` khỏi context bằng `.dockerignore`. Về quy mô: nghiên cứu của Dahlmanns và cộng sự (arXiv 2307.03958, 2023) phân tích 337.171 image Docker Hub cùng 8.076 registry riêng và thấy 8,5% image chứa secret, gồm 52.107 khoá riêng và 3.158 API secret.
</details>

#### Kịch bản 7 — App không kết nối được DB trong compose của repo
**Triệu chứng:** chạy `docker-compose.develop.yml`, `board-service` crash khi mở kết nối DB dù `db-dev` đã healthy.
**Câu hỏi chẩn đoán:** đọc dòng 5 và tên service DB; điểm gì không khớp?
<details>
<summary>Đáp án</summary>

Chuỗi kết nối dùng `Server=sqlserver,1433` nhưng service DB tên `db-dev`; tên `sqlserver` không được phân giải (Part 3: `nslookup` tên sai trả `Can't find sqlserver: No answer`). Sửa: dùng `db-dev` hoặc thêm alias `sqlserver` cho service DB. Nơi sửa: `src/docker-compose.develop.yml`. (Chỉ ghi nhận trong lộ trình, chưa sửa code.)
</details>

#### Kịch bản 8 — Sau khi siết quyền, app báo `Read-only file system`
**Triệu chứng:** thêm `--read-only` rồi app lỗi khi ghi file tạm.
**Câu hỏi chẩn đoán:** làm sao biết app ghi vào đâu, và sửa mà không bỏ `--read-only`?
<details>
<summary>Đáp án</summary>

Đọc đường dẫn trong lỗi; `docker diff` trên một container chạy không `--read-only` cho biết các đường dẫn app ghi (`A`/`C`). Giữ `--read-only` và cấp `--tmpfs` hoặc volume đúng cho các đường dẫn đó (Part 5, Lab 5.1: `--read-only --tmpfs /tmp` ghi được `/tmp`).
</details>

## L7 Architecture — Thiết kế và phỏng vấn

#### Thiết kế: đóng gói và triển khai TeamBoard lên AWS
**Bài toán:** `board-service` chạy local bằng Compose; production dự kiến ECS Fargate sau ALB (ADR 0003). Trình bày các quyết định đóng gói và triển khai.

| Quyết định | Phương án | Đánh đổi |
|---|---|---|
| Dockerfile | multi-stage, user không root, ghim digest | digest phải tự cập nhật |
| Registry | Amazon ECR | gắn AWS; có tag immutability ⚠ |
| Chạy trên | ECS Fargate (task `awsvpc`) | không học Kubernetes (ADR 0003) |
| Cổng/health | healthcheck ở `/health/ready`, ALB target group loại `ip` ⚠ | cần endpoint health thật |
| Secret | SSM/Secrets Manager qua `secrets` trong container definition ⚠ | đổi secret phải **force new deployment** ⚠ |
| Lùi phiên bản | circuit breaker bật `rollback` | chỉ cho rolling update (controller `ECS`) |

**Circuit breaker (đã kiểm với tài liệu ECS):** hai giai đoạn: task có lên `RUNNING` không, rồi health check (ELB, Cloud Map, container). Ngưỡng mặc định `BOUNDED_PERCENT` giá trị 50, kẹp từ 3 đến 200; khi vượt thì deployment `FAILED` và có thể tự lùi về bản `COMPLETED` gần nhất. Chỉ hỗ trợ dịch vụ dùng controller `ECS` (rolling update).

#### Docker, Compose, ECS, Kubernetes: chọn khi nào
| Tình huống | Chọn |
|---|---|
| Phát triển, CI một máy | Docker + Compose |
| Production một máy, ít phức tạp | Compose (có hạn chế) |
| Production AWS, ít vận hành | ECS Fargate |
| Cần hệ sinh thái lớn, đa cloud | Kubernetes |
| Không muốn daemon root | Podman hoặc Docker rootless |

#### Case thực tế

#### Secret nằm trong image công khai (nghiên cứu RWTH Aachen, 2023)
- **Vấn đề:** người dùng đẩy image lên registry nhưng quên rằng token, khoá nằm trong layer, `ENV` hoặc file bị `COPY`.
- **Cách làm (của nghiên cứu):** quét 337.171 image trên Docker Hub và 8.076 registry riêng.
- **Bài học:** 8,5% image chứa secret, gồm 52.107 khoá riêng và 3.158 API secret; xoá file ở layer sau không đủ. Dùng secret mount, `.dockerignore`, và xoay secret đã lộ.
- Nguồn: [Secrets Revealed in Container Images](https://arxiv.org/abs/2307.03958) (Dahlmanns và cộng sự, 2023).

#### Kaniko bị ngừng phát triển (Google, 2025)
- **Vấn đề:** công cụ build image không cần daemon được dùng trong nhiều pipeline.
- **Cách làm:** kho mã nguồn chính thức chuyển sang trạng thái archived từ 03/06/2025, ghi rõ không còn phát triển hay bảo trì.
- **Bài học:** kiểm vòng đời công cụ build trước khi ghim vào pipeline; ưu tiên công cụ còn được duy trì (BuildKit, Buildah).
- Nguồn: [github.com/GoogleContainerTools/kaniko](https://github.com/GoogleContainerTools/kaniko).

#### Cache API v1 của GitHub bị tắt (2025)
- **Vấn đề:** workflow dùng `type=gha` với phiên bản Buildx cũ đột nhiên không còn cache.
- **Cách làm:** GitHub tắt API cache v1 ngày 15/04/2025; chỉ còn v2, cần Buildx 0.21.0, BuildKit 0.20.0 trở lên.
- **Bài học:** ghim và theo dõi phiên bản công cụ CI; đọc ghi chú ngừng hỗ trợ.
- Nguồn: [Cache management with GitHub Actions](https://docs.docker.com/build/ci/github-actions/cache/).

#### Docker Hub giới hạn pull cho người dùng chưa đăng nhập
- **Vấn đề:** pipeline CI dùng chung IP chạm giới hạn pull.
- **Cách làm:** Docker Hub giới hạn 100 lượt pull mỗi 6 giờ cho mỗi IPv4 hoặc IPv6 /64 khi chưa đăng nhập, 200 với tài khoản cá nhân, không giới hạn với gói trả phí.
- **Bài học:** đăng nhập trong CI hoặc dùng registry khác (ECR, MCR).
- Nguồn: [Docker Hub pull usage](https://docs.docker.com/docker-hub/usage/pulls/).

**Chưa có case kể lại việc một công ty vận hành Docker ở quy mô lớn từ nguồn gốc đã mở** (Spotify, Shopify, Airbnb chưa tìm được bài gốc mở được); Part 0 ghi trạng thái real-world là "đạt một phần".

#### 10 câu phỏng vấn thường gặp
1. **Vì sao multi-stage?** Tách môi trường build khỏi runtime; image cuối chỉ chứa thứ cần chạy; stage độc lập chạy song song (Lab 4.1: 65,4 MB so với 12,9 MB).
2. **Tag và digest, khi nào ghim?** Tag đổi được, digest cố định nội dung; ghim cho tái lập nhưng phải tự cập nhật.
3. **Vì sao copy file phụ thuộc rồi restore trước `COPY . .`?** Một layer đổi làm mọi layer sau build lại; để bước tốn kém giữ cache (Lab 2.3).
4. **Vì sao không truyền secret bằng ARG/ENV?** Giá trị nằm trong lịch sử image; dùng secret mount (Lab 4.3).
5. **Vì sao chạy non-root với UID cố định?** Giảm quyền khi bị khai thác; UID tự cấp có thể đổi giữa các lần build.
6. **Dạng exec và PID 1 để làm gì?** App làm PID 1 nhận tín hiệu dừng; dạng shell bọc `sh` thì app là con (Lab 2.10).
7. **Mặc định của HEALTHCHECK?** interval 30s, timeout 30s, start-period 0s, retries 3; exit 0 healthy, 1 unhealthy (Lab 2.2, 5.3).
8. **`depends_on` có đảm bảo DB sẵn sàng không?** Mặc định chỉ chờ container chạy; cần `condition: service_healthy` (Lab 3.2).
9. **Vì sao log container có thể làm đầy đĩa?** `json-file` không xoay mặc định; đặt `max-size`/`max-file`, chỉ áp dụng container mới (Lab 2.9).
10. **Vì sao target group của ECS Fargate phải là `ip`?** Task `awsvpc` gắn với ENI thay vì EC2 instance ⚠.

## L8 Mastery — Bài mở

Mỗi bài không có đáp án duy nhất; chấm theo tiêu chí.

#### Bài 1 — Đóng gói `notification-service` (Hexagonal, consumer SQS idempotent)
Thiết kế Dockerfile, Compose local và cách triển khai AWS cho một service **không có HTTP** (chỉ đọc hàng đợi). **Tiêu chí:** (a) healthcheck phù hợp service không có HTTP; (b) xử lý SIGTERM để hoàn tất message đang xử lý; (c) secret không nằm trong image; (d) giải thích vì sao chọn restart policy và ECS service thay vì task một lần.

#### Bài 2 — Chẩn đoán "chạy chậm trên Windows"
Dev báo hot reload không nhận thay đổi và build chậm. **Tiêu chí:** nêu giả thuyết có thể kiểm (mã nguồn nằm ở ổ Windows và đi qua ranh giới VM, bind mount, file watcher), cách đo, và cách sửa mà không đổi kiến trúc ⚠ (đề xuất để mã trong filesystem WSL2).

#### Bài 3 — Hardening Dockerfile của repo
So sánh `src/board-service/Dockerfile` và `.dockerignore` với `.agents/rules/docker.md` và lập danh sách điểm lệch kèm mức ưu tiên. **Tiêu chí:** nhận ra tối thiểu: tag `10.0` chưa ghim, chưa có `HEALTHCHECK`, `.dockerignore` mới loại `**/.env` chứ chưa `.env*` và `local/`, `EXPOSE 8080` lệch cổng 7070 của compose, `latest` ở `mssql/server:2022-latest`, mật khẩu truyền qua `-P` trong healthcheck (lộ qua `docker inspect`).

#### Bài 4 — Chiến lược lùi phiên bản
Thiết kế cách rollback khi bản mới cần **migration DB không tương thích ngược**. **Tiêu chí:** phân biệt lùi image với lùi dữ liệu; nêu mở rộng–thu hẹp (expand/contract) migration; vai trò circuit breaker và giới hạn của nó (không lùi được dữ liệu).

## Tự kiểm tra tổng hợp

1. Vẽ luồng `docker run` từ client tới runc và nêu vai trò namespaces, cgroups.
2. Container thoát 137: nêu hai nguồn và cách phân biệt.
3. Giải thích vì sao `docker rm` mất dữ liệu và ba cách giữ dữ liệu.
4. Vì sao `COPY . .` đặt trước bước cài làm build chậm?
5. Khác nhau giữa `depends_on` dạng ngắn và `service_healthy`?
6. `down` và `down -v` khác nhau thế nào?
7. Image multi-stage nhỏ hơn vì sao?
8. `ARG` và `--secret` khác nhau thế nào về `docker history`?
9. Nêu bốn cờ siết quyền.
10. SBOM khác provenance; chúng nằm ở đâu?
11. Khi nào dùng Compose, ECS Fargate, Kubernetes?
12. Docker Desktop khác Docker Engine ở điểm giấy phép nào?

<details>
<summary>Đáp án ngắn</summary>

1. client → `dockerd` → `containerd` → `runc` → tiến trình (PID 1); namespaces quyết định thấy gì, cgroups quyết định dùng bao nhiêu.
2. SIGKILL do `docker stop` hết timeout/`kill`, hoặc OOM kill; phân biệt bằng `OOMKilled`.
3. Dữ liệu ở lớp ghi tạm; giữ bằng volume, bind mount hoặc dịch vụ ngoài.
4. Mọi thay đổi code làm layer cài phụ thuộc phía sau bị build lại.
5. Dạng ngắn chỉ chờ container chạy; `service_healthy` chờ healthcheck qua.
6. `down -v` xoá cả volume đã khai báo.
7. Toolchain build nằm ở stage trước, không vào image cuối.
8. `ARG` lộ trong history; `--secret` không để lại dấu vết.
9. `--user`, `--cap-drop ALL`, `--read-only`, `--security-opt no-new-privileges`.
10. SBOM là danh sách thành phần, provenance là nguồn gốc build; cả hai là attestation trong image index.
11. Compose cho một máy; ECS Fargate cho AWS ít vận hành; Kubernetes khi cần hệ sinh thái lớn/đa cloud.
12. Desktop có điều kiện trả phí với tổ chức lớn; Engine/CLI/Compose không.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy và đối chiếu:** các lab Part 1 đến 5 được dẫn lại trong kịch bản 1 đến 5, 8 và câu phỏng vấn.
**Đã tự mở:** arXiv 2307.03958 (337.171 image, 8,5%, 52.107 khoá riêng, 3.158 API secret); docs.aws.amazon.com/AmazonECS/latest/developerguide/deployment-circuit-breaker (hai giai đoạn, ngưỡng, rollback, chỉ rolling update); docs.docker.com/docker-hub/usage/pulls (giới hạn pull).
**Chỉ qua agent (⚠):** target group loại `ip` và `awsvpc`, secret qua `secrets` và việc phải force new deployment (docs.aws.amazon.com/AmazonECS/…/specifying-sensitive-data, service-load-balancing); ECR tag immutability; đề xuất để mã trong WSL2 (docs.docker.com/desktop/features/wsl/best-practices).
**Chưa kiểm:** kịch bản 7 chưa chạy chính file compose của repo (cần SQL Server và chấp nhận EULA); bài mở L8 chưa có đáp án mẫu thực thi.
