---
title: Đổi tên board-service, thêm ServiceDefaults và Jaeger
issue: TB-001
status: approved            # draft | in-review | approved | done
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
| FR-001 (tên nhất quán) | Q-05 phạm vi đổi tên (issue) | B2, B6 | @S01, @S02, @S03 | Test tên assembly/namespace; test quét tên cũ; test `Docker` chạy compose (@S03) |
| FR-002 (health) | Q-02 ready chỉ "đã sẵn sàng" | B3 | @S04, @S05, @S06 | Unit health check + host test `/health/*` |
| FR-004 (dùng lại được) | – | B3, B4 | @S07 | Host tối thiểu gọi `AddServiceDefaults`/`MapDefaultEndpoints` |
| FR-003 (telemetry, Jaeger) | Q-02 OTLP trực tiếp; Q-03 dùng `/health/live` | B4, B5 | @S08, @S09, @S10, @S11 | In-memory exporter; thiếu endpoint vẫn chạy; test `Docker` truy vấn Jaeger (@S11) |
| NFR-001 (không secret) | – | B4, B5 | @S10 | Test cấu hình; quét secret thô |
| NFR-002 (gate xanh) | – | B1–B6 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |
| NFR-003 (pin, danh bạ) | Q-04 xUnit + Reqnroll | B1 | – | `config/dependencies.yaml` có dòng cho mọi gói mới; phiên bản cố định |
| NFR-004 (không log secret/body) | GDPR | B4 | @S10 | Test thuộc tính span không chứa giá trị `Authorization` |
| NFR-005 (Jaeger bind local, pin) | `docker.md` | B5 | @S11 | `docker compose config` kiểm `127.0.0.1` và tag |

Kịch bản (đặt trong `document/spec/features/`, tag `@TB-001`, ID `@S01…@S11`):
- @S01 Tên assembly và namespace gốc của service là `board-service`/`BoardService`.
- @S02 Không file hiện hành nào còn tên cũ (code, compose, config, `PROJECT.md`, `COMMANDS.md`, `document/` trừ thư mục lịch sử bất biến).
- @S03 Compose chạy service `board-service` nghe ở host 6060 (test tự động nhóm `Docker`, gọi `docker compose` qua Process).
- @S04 `GET /health/live` trả 200 khi service đã khởi động.
- @S05 `GET /health/ready` trả 200 khi service đã sẵn sàng.
- @S06 Check "sẵn sàng" báo không khoẻ trước khi `ApplicationStarted`.
- @S07 Host khác dùng `AddServiceDefaults`/`MapDefaultEndpoints` nhận cùng health và telemetry.
- @S08 Request tạo span có `service.name = board-service`.
- @S09 Không đặt endpoint OTLP thì service vẫn khởi động, không lỗi.
- @S10 Span/log không chứa giá trị `Authorization` hay body.
- @S11 Request qua compose hiện thành trace trong Jaeger (test tự động nhóm `Docker`, truy vấn API Jaeger).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Mỗi bước là một commit nhỏ (dưới khoảng 400 dòng diff, không tính file đổi tên thuần). Test thực thi sinh theo từng bước, đỏ từng bước (quy ước của plan này).

### B1. Công cụ, project test và danh bạ phụ thuộc
- **Kịch bản/test sẽ viết:** không có hành vi mới; một test rỗng chạy xanh chứng minh runner hoạt động. Không áp dụng "đỏ".
- **File/module dự kiến chạm:** `config/tool-versions.yaml` (xUnit, Reqnroll, test SDK), `config/dependencies.yaml` (Reqnroll T2 `approved` kèm lối thoát, xUnit, Microsoft.NET.Test.Sdk, Microsoft.AspNetCore.Mvc.Testing, OpenTelemetry và exporter, `Microsoft.Extensions.Diagnostics.HealthChecks`), `src/board-service.Tests/` (theo P-04), `src/teamboard.slnx`.
- **Kiểm tra phiên bản (đã tra ngày 2026-10-23 bằng `dotnet package search`, pin ở B1):** `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `Reqnroll.xUnit` 3.3.4, `Microsoft.NET.Test.Sdk` 18.11.0, `Microsoft.AspNetCore.Mvc.Testing` 10.0.13, `OpenTelemetry.Extensions.Hosting`/`Exporter.OpenTelemetryProtocol`/`Instrumentation.AspNetCore`/`Instrumentation.Http`/`Exporter.InMemory` 1.20.1, image Jaeger `cr.jaegertracing.io/jaegertracing/jaeger:2.22.0` (tài liệu Jaeger ghi 2.22 là mới nhất). Kiểm tương thích thực tế khi restore/build ở B1. Còn phải xác minh ở B1: `.feature` đặt ngoài project (`document/spec/features/`) liên kết được vào project test Reqnroll. Không chạy được → dừng, hỏi người dùng (không tự chuyển sang cách khác).
- **Ghi chú thứ tự:** project test ở B1 độc lập, chưa `ProjectReference` tới `teamboard-api` (sẽ đổi tên ở B2); tham chiếu tới `board-service` thêm ở B2.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** `config/tool-versions.yaml` (tên công cụ đã ghi ở plan-review: `dotnet_test_framework: xUnit`, `bdd_runner_dotnet: Reqnroll`; B1 điền phiên bản cụ thể), `config/dependencies.yaml` (đánh dấu `verified: true` kèm nguồn khi đã kiểm).
- **Kiểm chứng:** `dotnet test src/teamboard.slnx` chạy xanh (1 test mẫu); `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` không có lỗ hổng.
- **Phụ thuộc:** không.

### B2. Đổi tên teamboard-api → board-service (FR-001: @S01, @S02)
- **Kịch bản/test sẽ viết (đỏ trước):** test `@S01` (tên assembly là `board-service`, namespace gốc `BoardService`); test `@S02` duyệt hệ thống tệp từ gốc repo (tìm gốc bằng cách đi ngược lên tới `src/teamboard.slnx`; không phụ thuộc lệnh `git`, bỏ qua `bin/`, `obj/`, `.vs/`, `.git/`) trong `src/`, `config/`, `PROJECT.md`, `COMMANDS.md` tìm `teamboard-api`/`teamboard_api`, quét thêm `document/` trừ các thư mục lịch sử bất biến (`document/issues/`, `document/plans/`, `document/workflow/`, `document/lessons/`, `document/architecture/adr/`), theo P-03 (đã chốt: viết lại để không còn tên cũ). Đỏ vì tên hiện tại còn cũ (không phải lỗi biên dịch). File `.feature`: `document/spec/features/board-service/service-identity.feature` (@S01–@S03).
- **File/module dự kiến chạm:** `src/teamboard-api/**` → `src/board-service/**` (`git mv`), `board-service.csproj` (`RootNamespace`/`AssemblyName`), `Program.cs` (thêm `public partial class Program` cho host test), xoá `Controllers/WeatherForecastController.cs`, `WeatherForecast.cs`, `teamboard-api.http`; `Dockerfile` (đường dẫn, entrypoint `board-service.dll`); `src/docker-compose.yml`, `.develop.yml`, `.stg.yml` (service và image `board-service`); `src/launchSettings.json`; `src/teamboard.slnx`; `src/.dockerignore` nếu có tham chiếu.
- **Hợp đồng thay đổi:** endpoint `/weatherforecast` bị xoá (mẫu, không consumer, đã được người dùng đồng ý). Cổng host 6060 → container 7070 giữ nguyên.
- **Migration / cấu hình / biến môi trường:** không đổi biến; không chạm `.env*`.
- **Tài liệu cần cập nhật:** chuyển sang B6.
- **Kiểm chứng:** hai test xanh; `dotnet build src/teamboard.slnx -warnaserror`; `docker compose -f src/docker-compose.yml config -q` và `docker compose -f src/docker-compose.yml -f src/docker-compose.develop.yml config -q` (kiểm `.env` giả qua biến tạm, không commit); `docker compose -f src/docker-compose.yml up --build -d` rồi `curl -s -o /dev/null -w "%{http_code}" http://localhost:6060/` ra mã HTTP (404 là đúng, không phải từ chối kết nối) và `docker compose ps` hiện `board-service`; sau đó `down` (không dùng `-v`). Các lệnh này cũng là nội dung test @S03 nhóm `Docker` (viết test đỏ trước khi đổi compose; chạy được tự động khi có Docker).
- **Phụ thuộc:** B1.

### B3. ServiceDefaults: health (FR-002, FR-004: @S04–@S07)
- **Kịch bản/test sẽ viết (đỏ trước):** @S06 check "sẵn sàng" trả `Unhealthy` trước `ApplicationStarted` và `Healthy` sau; @S04/@S05 `GET /health/live`, `/health/ready` qua `WebApplicationFactory<Program>` của board-service trả 200; @S07 một host tối thiểu trong project test của ServiceDefaults gọi `AddServiceDefaults()` và `MapDefaultEndpoints()` nhận 200 ở cả hai.
- **File/module dự kiến chạm:** `src/shared/ServiceDefaults/` (project mới `Teamboard.Shared.ServiceDefaults`, theo P-04): `Extensions` (`AddServiceDefaults`, `MapDefaultEndpoints`), `StartupHealthCheck`/cờ sẵn sàng gắn `IHostApplicationLifetime`; `src/board-service/Program.cs` (gọi hai extension, giữ ngắn); `src/board-service/board-service.csproj` (ProjectReference); `Dockerfile` (COPY project shared trước `restore`); `src/teamboard.slnx`; project test ServiceDefaults; file `.feature` mới.
- **File `.feature`:** `document/spec/features/shared/health.feature` (@S04–@S07).
- **Hợp đồng thay đổi:** thêm `/health/live` (không check dependency) và `/health/ready` (check có tag `ready`, hiện chỉ "đã sẵn sàng"); nằm ngoài versioning theo `api-endpoints.md` mục 1. Dùng response mặc định của health checks (văn bản `Healthy`/`Unhealthy`, không trả chi tiết từng check) để không lộ thông tin nội bộ.
- **Migration / cấu hình:** không. Cấu hình bắt buộc qua Options có validate nếu có (`backend-engineering.md`).
- **Tài liệu cần cập nhật:** `COMMANDS.md` (health), chuyển phần còn lại sang B6.
- **Kiểm chứng:** test @S04–@S07 xanh; `dotnet build -warnaserror`; build image qua compose xanh (Dockerfile copy đủ project shared).
- **Phụ thuộc:** B2.

### B4. ServiceDefaults: OpenTelemetry (FR-003, NFR-004: @S08–@S10)
- **Kịch bản/test sẽ viết (đỏ trước):** @S08 host test với in-memory exporter, một `GET /health/live` tạo Activity có tài nguyên `service.name = board-service`; @S09 host khởi động và phục vụ health khi không đặt endpoint OTLP; @S10 request mang header `Authorization: Bearer <giá trị thử>` và body, không thuộc tính span hay bản ghi log nào chứa giá trị đó.
- **File/module dự kiến chạm:** `src/shared/ServiceDefaults/` (cấu hình OTel: trace ASP.NET Core + HttpClient, metric, log; exporter OTLP chỉ bật khi có `OTEL_EXPORTER_OTLP_ENDPOINT`; tên service lấy từ cấu hình/`OTEL_SERVICE_NAME`, board-service đặt `board-service`; Options có validate), `src/board-service/` (đặt tên service), test.
- **File `.feature`:** `document/spec/features/shared/telemetry.feature` (@S08–@S11).
- **Hợp đồng thay đổi:** không có HTTP mới.
- **Migration / cấu hình / biến môi trường:** `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME` (qua env, không secret); health không được lọc khỏi trace (Q-03).
- **Tài liệu cần cập nhật:** chuyển sang B6.
- **Kiểm chứng:** test @S08–@S10 xanh; `dotnet build -warnaserror`; quét thô `git diff | grep -i -E "password|secret|apikey"` không có kết quả thật.
- **Phụ thuộc:** B3.

### B5. Jaeger trong compose (FR-003, NFR-005: @S11)
- **Kịch bản/test sẽ viết (đỏ trước):** @S11 test tự động nhóm `Docker` (chạy `docker compose` qua Process, tự bỏ qua khi không có Docker): chạy compose có observability, gọi `GET /health/live`, truy vấn API Jaeger tìm trace của service `board-service` (có thời gian chờ và thử lại có giới hạn); `down` (không `-v`) khi xong. Đỏ vì chưa có file observability. File `.feature` đã nêu ở B4.
- **File/module dự kiến chạm:** `src/docker-compose.observability.yml` (Jaeger, tag pin, UI và OTLP chỉ bind `127.0.0.1`, cùng mạng compose với board-service; biến `OTEL_EXPORTER_OTLP_ENDPOINT` cho board-service), `src/docker-compose.yml` hoặc file observability đặt biến cho board-service, `src/docker-compose.dcproj` nếu liệt kê file.
- **Hợp đồng thay đổi:** không.
- **Migration / cấu hình:** tag image Jaeger cố định và kiểm giấy phép Apache-2.0 trước khi dùng (A-03); ghi vào `config/dependencies.yaml`.
- **Tài liệu cần cập nhật:** `COMMANDS.md` (lệnh chạy kèm observability), chuyển phần còn lại sang B6.
- **Kiểm chứng:** `docker compose -f src/docker-compose.yml -f src/docker-compose.observability.yml config -q`; `up --build -d`; `curl http://localhost:6060/health/live` (200); `curl -s "http://127.0.0.1:16686/api/traces?service=board-service&limit=1"` có ít nhất một trace; `down` (không `-v`). Kiểm cổng chỉ bind `127.0.0.1` bằng `docker compose ... config`.
- **Phụ thuộc:** B4.

### B6. Tài liệu và cấu hình đồng bộ (FR-001 phần còn lại, NFR-002)
- **Kịch bản/test sẽ viết:** @S02 mở rộng nếu cần (đã có ở B2); không thêm kịch bản mới.
- **File/module dự kiến chạm:** `config/services.yaml` (`path: src/board-service`, bỏ ghi chú "sẽ đổi tên"), `config/project.yaml` (`paths.services.boardService`), `config/tool-versions.yaml` (chú thích csproj), `config/dependencies.yaml` (ghi chú Swashbuckle), `PROJECT.md` (Trạng thái hiện tại), `COMMANDS.md` (cổng, `dotnet test` đã có project test), `document/architecture/README.md` và `document/architecture/patterns-catalog.md` (viết lại các câu nhắc tên cũ cho đúng trạng thái đã đổi tên, theo P-03), `document/issues/` (README, TB-001, TB-010 bỏ ghi chú chờ).
- **Hợp đồng thay đổi:** không.
- **Kiểm chứng:** chạy lại toàn bộ gate (mục 6); @S02 xanh; `grep` thủ công các tệp ngoại lệ.
- **Phụ thuộc:** B2–B5.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Check "sẵn sàng" và cờ khởi động | Không I/O | Dùng `IHostApplicationLifetime` giả |
| Integration (host in-process) | Health, telemetry, thiếu OTLP, không lộ `Authorization`/body | `WebApplicationFactory<Program>`, in-memory exporter | Không mock ASP.NET Core; không DB |
| Integration Docker (nhóm `Docker`) | Compose, cổng 6060, Jaeger (@S03, @S11) | Docker local; `docker compose -p` tên dự án riêng, `down` không `-v` | Gọi `docker compose` qua Process; tự bỏ qua khi không có Docker; không thêm phụ thuộc (Confirm log #4); tách khỏi vòng chạy nhanh bằng bộ lọc nhóm |

Kim tự tháp: nhiều unit/host test nhanh, không có e2e tự động (không có UI, không đủ giá trị). Contract test: không áp dụng (chưa có API nghiệp vụ).

Công cụ test/BDD (`config/tool-versions.yaml`: `dotnet_test_framework: xUnit`, `bdd_runner_dotnet: Reqnroll`): **xUnit v2 (2.9.3) + Reqnroll.xUnit 3.3.4** (người dùng chọn). Phiên bản đã tra trên NuGet ngày 2026-10-23 (xem B1), pin ở B1.

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Reqnroll chưa hỗ trợ .NET 10 / xUnit hoặc không đọc `.feature` ngoài project (A-02) | Phải đổi cách BDD | Kiểm ở B1 trước mọi bước khác; dừng và hỏi nếu không chạy | Bỏ project test, revert B1 |
| Đổi tên làm vỡ Visual Studio/launch profile hoặc Docker context | Không build/chạy được | Dùng `git mv`; build và compose sau B2; commit riêng | Revert commit B2 |
| Dockerfile thiếu COPY project shared (`restore` lỗi) | Build image lỗi | Thêm COPY ở B3; build image ở B3 | Revert B3 |
| OTLP endpoint sai hoặc Jaeger chưa lên làm app lỗi | Service không khởi động | Exporter chỉ bật khi có endpoint; test @S09 | Bỏ biến môi trường |
| Jaeger không bật OTLP mặc định trong all-in-one (A-01: tài liệu chỉ ghi cổng 4317/4318 mặc định, chưa nói rõ bật sẵn) | Không thấy trace | Pin `2.22.0`; kiểm thực tế ở B5 bằng test `Docker`; nếu cần thì thêm cấu hình receiver | Revert B5 |
| Test nhóm `Docker` chậm, kẹt cổng 6060 hoặc để sót container | Test đỏ oan, tốn tài nguyên | Tên dự án compose riêng, `down` (không `-v`) trong `finally`, bỏ qua khi thiếu Docker, tách khỏi vòng chạy nhanh | Xoá container thủ công bằng `docker compose -p ... down` |
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

Giả định A-01..A-03 của issue, đã kiểm ngày 2026-10-23: **A-03 đã xác minh** (giấy phép Jaeger là Apache-2.0, theo file `LICENSE` ở `github.com/jaegertracing/jaeger`); **A-02 xác minh một phần** (tài liệu Reqnroll liệt kê .NET 10 và xUnit; còn phải kiểm `.feature` ngoài project ở B1); **A-01 xác minh một phần** (tài liệu Jaeger ghi cổng OTLP 4317/4318 mặc định, chưa nêu rõ bật sẵn trong all-in-one; kiểm thực tế ở B5). Người dùng chấp nhận tiếp tục với phần còn lại kiểm ở B1/B5; sai thì dừng và hỏi.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-03 | Tên cũ trong `document/architecture/**`: giữ hay viết lại | @S02, B6 | sondv | answered: viết lại để không còn tên cũ (2026-10-23) |
| P-04 | Tên project ServiceDefaults và vị trí project test | B1, B3 | sondv | answered: `Teamboard.Shared.ServiceDefaults`, test cạnh project nguồn (2026-10-23) |

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
| 1 | plan-review:TB-001:p03-legacy-name | Tên cũ trong `document/architecture/**` | Viết lại để không còn tên cũ | 1 | 2026-10-23 |
| 2 | plan-review:TB-001:p04-naming | Tên ServiceDefaults và vị trí test | `Teamboard.Shared.ServiceDefaults`; test cạnh project nguồn | 1 | 2026-10-23 |
| 3 | plan-review:TB-001:assumptions-verify | Giả định A-01..A-03 | Muốn xem kết quả kiểm trước khi duyệt; đã kiểm (A-03 xác minh, A-02/A-01 một phần), tiếp tục | 1 | 2026-10-23 |
| 4 | plan-review:TB-001:docker-scenarios-automated | @S03, @S11 có test tự động không | Có; test gọi `docker compose` qua Process, nhóm `Docker`, tự bỏ qua khi không có Docker (Confirm log #4) | 1 | 2026-10-23 |
| 5 | plan-review:TB-001:xunit-version | xUnit v2 hay v3 | xUnit v2 (2.9.3) + Reqnroll.xUnit 3.3.4 | 1 | 2026-10-23 |

(Các quyết định về làn, phạm vi, telemetry, ready, test framework, đổi tên, trace nằm ở Confirm log của issue TB-001.)

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-23 | Làn vừa: A, B, D, E, F | 4 (P-03, P-04, giả định A-01..A-03, test tự động @S03/@S11) → đã chốt cả 4; thêm chọn xUnit v2 | 0 sau khi sửa 6 FIX | NEEDS_CONFIRM → PASS |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-23 | Tạo bản nháp | plan-init cho TB-001 (làn vừa) |
| 2026-10-23 | plan-review vòng 1: ghi quyết định P-03, P-04, test Docker tự động, xUnit v2; thêm phiên bản gói đã tra, kết quả kiểm giả định, đường dẫn `.feature`, ghi chú thứ tự ProjectReference, cách quét tên cũ; plan `approved` | plan-review TB-001 |
