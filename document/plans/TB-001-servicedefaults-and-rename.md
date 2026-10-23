---
title: Đổi tên board-service, thêm ServiceDefaults và Jaeger
issue: TB-001
status: draft            # draft | in-review | approved | done
owner: sondv
updated: 2026-10-23
spec: document/issues/TB-001-servicedefaults-and-rename.md   # làn vừa: spec ngắn nằm trong issue
design: null             # làn vừa, không có quyết định kiến trúc mới (OTel, health đã quy định ở observability.md)
services: [board-service, shared/ServiceDefaults]
---

# Plan: TB-001 — Đổi tên board-service, thêm ServiceDefaults và Jaeger

Ảnh chụp trạng thái branch: nhánh `feature/TB-001-servicedefaults-and-rename`, HEAD `0198cee` (trên `origin/main` `28a7792`), working tree sạch. Công cụ có sẵn: .NET SDK 10.0.402, Docker 29.5.3. Chưa có project test, `Directory.Packages.props`, hay thư mục `src/shared`.

## 1. Phạm vi của lát

**Làm:**
- Đổi tên `src/teamboard-api` → `src/board-service` (thư mục, csproj, namespace `BoardService`, Dockerfile, 3 file compose, image, `launchSettings.json`, `teamboard.slnx`), xoá `WeatherForecast` và file `.http` mẫu.
- `src/shared/ServiceDefaults`: OpenTelemetry (trace, metric, log), `/health/live`, `/health/ready`; gắn vào `board-service`.
- `src/docker-compose.observability.yml` với Jaeger; service gửi OTLP trực tiếp.
- Hai project test (xUnit + Reqnroll) và các file `.feature`.
- Cập nhật `config/services.yaml`, `config/project.yaml`, `config/tool-versions.yaml`, `config/dependencies.yaml`, `PROJECT.md`, `COMMANDS.md`.

**Không làm (trong lát này):** Collector, Prometheus/Grafana/log tập trung, `correlationId` và `problem+json` dùng chung (TB-010), check database ở `ready`, sửa `.env*`, sửa `Dockerfile` ngoài đổi tên và copy project shared, các endpoint nghiệp vụ.

## 2. Bảng truy vết

| Yêu cầu | Quyết định | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-001 (tên nhất quán) | Q-05 phạm vi đổi tên (issue) | B2, B6 | @S01, @S02, @S03 | Test tên assembly/namespace; test quét tên cũ; `docker compose config` |
| FR-002 (health) | Q-02 ready chỉ "đã sẵn sàng" | B3 | @S04, @S05, @S06 | Unit health check + host test `/health/*` |
| FR-004 (dùng lại được) | – | B3, B4 | @S07 | Host tối thiểu gọi `AddServiceDefaults`/`MapDefaultEndpoints` |
| FR-003 (telemetry, Jaeger) | Q-02 OTLP trực tiếp; Q-03 dùng `/health/live` | B4, B5 | @S08, @S09, @S10, @S11 | In-memory exporter; thiếu endpoint vẫn chạy; kiểm Jaeger bằng lệnh |
| NFR-001 (không secret) | – | B4, B5 | @S10 | Test cấu hình; quét secret thô |
| NFR-002 (gate xanh) | – | B1–B6 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |
| NFR-003 (pin, danh bạ) | Q-04 xUnit + Reqnroll | B1 | – | `config/dependencies.yaml` có dòng cho mọi gói mới; phiên bản cố định |
| NFR-004 (không log secret/body) | GDPR | B4 | @S10 | Test thuộc tính span không chứa giá trị `Authorization` |
| NFR-005 (Jaeger bind local, pin) | `docker.md` | B5 | @S11 | `docker compose config` kiểm `127.0.0.1` và tag |

Kịch bản (đặt trong `document/spec/features/`, tag `@TB-001`, ID `@S01…@S11`):
- @S01 Tên assembly và namespace gốc của service là `board-service`/`BoardService`.
- @S02 Không file hiện hành nào còn tên cũ (ngoại lệ ở câu hỏi P-03).
- @S03 Compose chạy service `board-service` nghe ở host 6060 (kiểm bằng lệnh).
- @S04 `GET /health/live` trả 200 khi service đã khởi động.
- @S05 `GET /health/ready` trả 200 khi service đã sẵn sàng.
- @S06 Check "sẵn sàng" báo không khoẻ trước khi `ApplicationStarted`.
- @S07 Host khác dùng `AddServiceDefaults`/`MapDefaultEndpoints` nhận cùng health và telemetry.
- @S08 Request tạo span có `service.name = board-service`.
- @S09 Không đặt endpoint OTLP thì service vẫn khởi động, không lỗi.
- @S10 Span/log không chứa giá trị `Authorization` hay body.
- @S11 Request qua compose hiện thành trace trong Jaeger (kiểm bằng lệnh).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Mỗi bước là một commit nhỏ (dưới khoảng 400 dòng diff, không tính file đổi tên thuần). Test thực thi sinh theo từng bước, đỏ từng bước (quy ước của plan này).

### B1. Công cụ, project test và danh bạ phụ thuộc
- **Kịch bản/test sẽ viết:** không có hành vi mới; một test rỗng chạy xanh chứng minh runner hoạt động. Không áp dụng "đỏ".
- **File/module dự kiến chạm:** `config/tool-versions.yaml` (xUnit, Reqnroll, test SDK), `config/dependencies.yaml` (Reqnroll T2 `approved` kèm lối thoát, xUnit, Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing, OpenTelemetry và exporter, `Microsoft.Extensions.Diagnostics.HealthChecks`), `src/board-service.Tests/` (tên theo câu hỏi P-04), `src/teamboard.slnx`.
- **Kiểm tra phiên bản:** tra phiên bản ổn định mới nhất tương thích .NET 10 cho từng gói bằng `dotnet package search`/trang chính thức, rồi pin chính xác. Xác minh giả định A-02 (Reqnroll chạy trên .NET 10 + xUnit, `.feature` đặt ngoài project trỏ về `document/spec/features/`). Không chạy được → dừng, hỏi người dùng (không tự chuyển sang cách khác).
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** `config/tool-versions.yaml`, `config/dependencies.yaml` (đánh dấu `verified: true` kèm nguồn khi đã kiểm).
- **Kiểm chứng:** `dotnet test src/teamboard.slnx` chạy xanh (1 test mẫu); `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` không có lỗ hổng.
- **Phụ thuộc:** không.

### B2. Đổi tên teamboard-api → board-service (FR-001: @S01, @S02)
- **Kịch bản/test sẽ viết (đỏ trước):** test `@S01` (tên assembly là `board-service`, namespace gốc `BoardService`); test `@S02` quét tệp theo dõi bởi git (`git ls-files`) trong `src/`, `config/`, `PROJECT.md`, `COMMANDS.md` tìm `teamboard-api`/`teamboard_api`. Đỏ vì tên hiện tại còn cũ (không phải lỗi biên dịch).
- **File/module dự kiến chạm:** `src/teamboard-api/**` → `src/board-service/**` (`git mv`), `board-service.csproj` (`RootNamespace`/`AssemblyName`), `Program.cs` (thêm `public partial class Program` cho host test), xoá `Controllers/WeatherForecastController.cs`, `WeatherForecast.cs`, `teamboard-api.http`; `Dockerfile` (đường dẫn, entrypoint `board-service.dll`); `src/docker-compose.yml`, `.develop.yml`, `.stg.yml` (service và image `board-service`); `src/launchSettings.json`; `src/teamboard.slnx`; `src/.dockerignore` nếu có tham chiếu.
- **Hợp đồng thay đổi:** endpoint `/weatherforecast` bị xoá (mẫu, không consumer, đã được người dùng đồng ý). Cổng host 6060 → container 7070 giữ nguyên.
- **Migration / cấu hình / biến môi trường:** không đổi biến; không chạm `.env*`.
- **Tài liệu cần cập nhật:** chuyển sang B6.
- **Kiểm chứng:** hai test xanh; `dotnet build src/teamboard.slnx -warnaserror`; `docker compose -f src/docker-compose.yml config -q` và `docker compose -f src/docker-compose.yml -f src/docker-compose.develop.yml config -q` (kiểm `.env` giả qua biến tạm, không commit); `docker compose -f src/docker-compose.yml up --build -d` rồi `curl -s -o /dev/null -w "%{http_code}" http://localhost:6060/` ra mã HTTP (404 là đúng, không phải từ chối kết nối) và `docker compose ps` hiện `board-service`; sau đó `down` (không dùng `-v`). Đây là kiểm @S03.
- **Phụ thuộc:** B1.

### B3. ServiceDefaults: health (FR-002, FR-004: @S04–@S07)
- **Kịch bản/test sẽ viết (đỏ trước):** @S06 check "sẵn sàng" trả `Unhealthy` trước `ApplicationStarted` và `Healthy` sau; @S04/@S05 `GET /health/live`, `/health/ready` qua `WebApplicationFactory<Program>` của board-service trả 200; @S07 một host tối thiểu trong project test của ServiceDefaults gọi `AddServiceDefaults()` và `MapDefaultEndpoints()` nhận 200 ở cả hai.
- **File/module dự kiến chạm:** `src/shared/ServiceDefaults/` (project mới, tên theo P-04): `Extensions` (`AddServiceDefaults`, `MapDefaultEndpoints`), `StartupHealthCheck`/cờ sẵn sàng gắn `IHostApplicationLifetime`; `src/board-service/Program.cs` (gọi hai extension, giữ ngắn); `src/board-service/board-service.csproj` (ProjectReference); `Dockerfile` (COPY project shared trước `restore`); `src/teamboard.slnx`; project test ServiceDefaults; file `.feature` mới.
- **Hợp đồng thay đổi:** thêm `/health/live` (không check dependency) và `/health/ready` (check có tag `ready`, hiện chỉ "đã sẵn sàng"); nằm ngoài versioning theo `api-endpoints.md` mục 1.
- **Migration / cấu hình:** không. Cấu hình bắt buộc qua Options có validate nếu có (`backend-engineering.md`).
- **Tài liệu cần cập nhật:** `COMMANDS.md` (health), chuyển phần còn lại sang B6.
- **Kiểm chứng:** test @S04–@S07 xanh; `dotnet build -warnaserror`; build image qua compose xanh (Dockerfile copy đủ project shared).
- **Phụ thuộc:** B2.

### B4. ServiceDefaults: OpenTelemetry (FR-003, NFR-004: @S08–@S10)
- **Kịch bản/test sẽ viết (đỏ trước):** @S08 host test với in-memory exporter, một `GET /health/live` tạo Activity có tài nguyên `service.name = board-service`; @S09 host khởi động và phục vụ health khi không đặt endpoint OTLP; @S10 request mang header `Authorization: Bearer <giá trị thử>` và body, không thuộc tính span hay bản ghi log nào chứa giá trị đó.
- **File/module dự kiến chạm:** `src/shared/ServiceDefaults/` (cấu hình OTel: trace ASP.NET Core + HttpClient, metric, log; exporter OTLP chỉ bật khi có `OTEL_EXPORTER_OTLP_ENDPOINT`; tên service lấy từ cấu hình/`OTEL_SERVICE_NAME`, board-service đặt `board-service`; Options có validate), `src/board-service/` (đặt tên service), test.
- **Hợp đồng thay đổi:** không có HTTP mới.
- **Migration / cấu hình / biến môi trường:** `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME` (qua env, không secret); health không được lọc khỏi trace (Q-03).
- **Tài liệu cần cập nhật:** chuyển sang B6.
- **Kiểm chứng:** test @S08–@S10 xanh; `dotnet build -warnaserror`; quét thô `git diff | grep -i -E "password|secret|apikey"` không có kết quả thật.
- **Phụ thuộc:** B3.

### B5. Jaeger trong compose (FR-003, NFR-005: @S11)
- **Kịch bản/test sẽ viết:** @S11 kiểm bằng lệnh (không có test tự động; ghi rõ trong TDD log): chạy compose, gọi `GET /health/live`, truy vấn API của Jaeger tìm trace của service `board-service`.
- **File/module dự kiến chạm:** `src/docker-compose.observability.yml` (Jaeger, tag pin, UI và OTLP chỉ bind `127.0.0.1`, cùng mạng compose với board-service; biến `OTEL_EXPORTER_OTLP_ENDPOINT` cho board-service), `src/docker-compose.yml` hoặc file observability đặt biến cho board-service, `src/docker-compose.dcproj` nếu liệt kê file.
- **Hợp đồng thay đổi:** không.
- **Migration / cấu hình:** tag image Jaeger cố định và kiểm giấy phép Apache-2.0 trước khi dùng (A-03); ghi vào `config/dependencies.yaml`.
- **Tài liệu cần cập nhật:** `COMMANDS.md` (lệnh chạy kèm observability), chuyển phần còn lại sang B6.
- **Kiểm chứng:** `docker compose -f src/docker-compose.yml -f src/docker-compose.observability.yml config -q`; `up --build -d`; `curl http://localhost:6060/health/live` (200); `curl -s "http://127.0.0.1:16686/api/traces?service=board-service&limit=1"` có ít nhất một trace; `down` (không `-v`). Kiểm cổng chỉ bind `127.0.0.1` bằng `docker compose ... config`.
- **Phụ thuộc:** B4.

### B6. Tài liệu và cấu hình đồng bộ (FR-001 phần còn lại, NFR-002)
- **Kịch bản/test sẽ viết:** @S02 mở rộng nếu cần (đã có ở B2); không thêm kịch bản mới.
- **File/module dự kiến chạm:** `config/services.yaml` (`path: src/board-service`, bỏ ghi chú "sẽ đổi tên"), `config/project.yaml` (`paths.services.boardService`), `config/tool-versions.yaml` (chú thích csproj), `config/dependencies.yaml` (ghi chú Swashbuckle), `PROJECT.md` (Trạng thái hiện tại), `COMMANDS.md` (cổng, `dotnet test` đã có project test), `document/architecture/README.md` (đánh dấu đã đổi tên nếu đúng trạng thái, tuỳ P-03), `document/issues/` (README, TB-001, TB-010 bỏ ghi chú chờ).
- **Hợp đồng thay đổi:** không.
- **Kiểm chứng:** chạy lại toàn bộ gate (mục 6); @S02 xanh; `grep` thủ công các tệp ngoại lệ.
- **Phụ thuộc:** B2–B5.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Check "sẵn sàng" và cờ khởi động | Không I/O | Dùng `IHostApplicationLifetime` giả |
| Integration (host in-process) | Health, telemetry, thiếu OTLP, không lộ `Authorization`/body | `WebApplicationFactory<Program>`, in-memory exporter | Không mock ASP.NET Core; không DB |
| Kiểm bằng lệnh | Compose, đổi tên, Jaeger (@S03, @S11) | Docker local | Không có test tự động; ghi lệnh và kết quả vào TDD log |

Kim tự tháp: nhiều unit/host test nhanh, không có e2e tự động (không có UI, không đủ giá trị). Contract test: không áp dụng (chưa có API nghiệp vụ).

Công cụ test/BDD (từ `config/tool-versions.yaml`, hiện còn `TODO`): **xUnit + Reqnroll** (người dùng chọn Q-04); ghi giá trị cụ thể vào file đó ở B1 sau khi kiểm phiên bản.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Reqnroll chưa hỗ trợ .NET 10 / xUnit hoặc không đọc `.feature` ngoài project (A-02) | Phải đổi cách BDD | Kiểm ở B1 trước mọi bước khác; dừng và hỏi nếu không chạy | Bỏ project test, revert B1 |
| Đổi tên làm vỡ Visual Studio/launch profile hoặc Docker context | Không build/chạy được | Dùng `git mv`; build và compose sau B2; commit riêng | Revert commit B2 |
| Dockerfile thiếu COPY project shared (`restore` lỗi) | Build image lỗi | Thêm COPY ở B3; build image ở B3 | Revert B3 |
| OTLP endpoint sai hoặc Jaeger chưa lên làm app lỗi | Service không khởi động | Exporter chỉ bật khi có endpoint; test @S09 | Bỏ biến môi trường |
| Jaeger image giấy phép/cổng không như giả định (A-01, A-03) | Sai cấu hình | Đọc trang chính thức, pin tag, ghi `verified` | Revert B5 |
| Telemetry lộ `Authorization`/body (GDPR) | Lộ dữ liệu | Test @S10; không bật capture header/body | Tắt instrumentation liên quan |
| Xoá `WeatherForecast` làm vỡ consumer ẩn | Request cũ 404 | Chỉ là mẫu, không consumer; người dùng đồng ý | Khôi phục từ git |

Ảnh hưởng consumer: không có (chưa service nào gọi `board-service`); gateway TB-010 sẽ dùng tên mới.

Bảo mật: kiểm `Authorization`/body không vào telemetry (@S10); Jaeger và OTLP chỉ bind `127.0.0.1`; không secret trong compose; quét thô diff. Chi phí: chỉ local, không tài nguyên AWS; không có bước `destroy`.

Bài học đã tra (`document/lessons/README.md`, tag `observability`, `backend`, `board-service`, `workflow`): không có lesson khớp trực tiếp; LL-002 (kiểm giấy phép trước khi dùng công cụ) áp dụng cho Jaeger, OpenTelemetry, Reqnroll ở B1/B5.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx`; `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml config -q` (và bản có observability); quét secret thô (`gitleaks` chưa cài, ghi rõ); `git status --short && git diff --stat`.
- Tiêu chí hoàn thành: theo mục "Tiêu chí hoàn thành" của issue TB-001.
- Tài liệu/config cập nhật: như B6.

## 7. Câu hỏi mở và giả định

Giả định: A-01, A-02, A-03 của issue (Jaeger nhận OTLP gốc; Reqnroll chạy trên .NET 10 + xUnit; giấy phép Jaeger). Chưa xác minh, kiểm ở B1/B5.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-03 | Các câu nhắc tên cũ mang tính giải thích trong `document/architecture/` (ví dụ "đổi tên từ teamboard-api", ví dụ Strangler Fig) giữ hay viết lại? | @S02, B6 | sondv | open |
| P-04 | Tên project và namespace của ServiceDefaults, và vị trí project test | B1, B3 | sondv | open |

### P-03 Tên cũ trong tài liệu kiến trúc
- **Lựa chọn:** A) giữ các câu giải thích lịch sử trong `document/architecture/**` và loại thư mục này khỏi kiểm @S02 (chỉ kiểm code, compose, config, PROJECT, COMMANDS); B) viết lại mọi câu để không còn tên cũ (mất ngữ cảnh lịch sử, đụng nhiều file tài liệu ngoài phạm vi).
- **Khuyến nghị:** A, vì các câu đó giải thích nguồn gốc `board-service` và đổi chúng là việc ngoài phạm vi rename; chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** @S02 không có danh sách ngoại lệ rõ, test quét dễ đỏ oan hoặc lọt.
- **Mặc định nếu quá hạn:** không dùng mặc định (ảnh hưởng tiêu chí test).

### P-04 Tên ServiceDefaults và vị trí test
- **Lựa chọn:** A) project `Teamboard.ServiceDefaults` trong `src/shared/ServiceDefaults/`, test ở `src/board-service.Tests/` và `src/shared/ServiceDefaults.Tests/`; B) project `Teamboard.Shared.ServiceDefaults` (khớp tên `Teamboard.Shared.Http` đã nêu ở design gateway), cùng vị trí test; C) thư mục `tests/` riêng ở gốc `src/` cho mọi project test.
- **Khuyến nghị:** B, vì thống nhất tiền tố `Teamboard.Shared.*` với thư viện dùng chung của gateway; project test đặt cạnh project nguồn (quy ước phổ biến của .NET, dễ tìm). Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** không thể tạo project ở B1/B3 mà không tự chọn tên.
- **Mặc định nếu quá hạn:** không dùng mặc định.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

(Các quyết định về làn, phạm vi, telemetry, ready, test framework, đổi tên, trace nằm ở Confirm log của issue TB-001.)

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-23 | Tạo bản nháp | plan-init cho TB-001 (làn vừa) |
