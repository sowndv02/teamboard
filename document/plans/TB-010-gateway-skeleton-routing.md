---
title: Gateway lát 1, khung YARP, route boards, health và 404 problem+json
issue: TB-010
status: approved            # draft | in-review | approved | done
owner: sondv
updated: 2026-10-23
spec: document/spec/api-gateway.md
design: document/design/api-gateway.md
services: [gateway, shared/Http]
---

# Plan: TB-010 — Gateway lát 1, khung YARP, route boards, health và 404 problem+json

Lát 1 trong [`api-gateway-slices.md`](api-gateway-slices.md) (`approved`). Làn lớn: spec và design đã `confirmed` ở TB-007 (PR 6). Ảnh chụp trạng thái branch: `feature/TB-010-gateway-skeleton-routing`, tạo từ `origin/main` `19845f1` (đã gồm TB-001), working tree sạch trước khi sửa issue. Đã có: `src/board-service`, `src/shared/ServiceDefaults` (`AddServiceDefaults`, `MapDefaultEndpoints`), compose (`docker-compose.yml`, `.observability.yml`), 2 project test (xUnit 2.9.3 + Reqnroll.xUnit 3.3.4, nhóm `docker`). Chưa có `src/gateway`, `src/shared/Http`.

## 1. Phạm vi của lát

**Làm:**
- `src/gateway`: host ASP.NET Core dùng YARP theo cấu hình tĩnh (design D0.4, D3); gọi `AddServiceDefaults("gateway")` và `MapDefaultEndpoints()` (health, telemetry dùng chung).
- Route `/api/v1/boards/{**catch-all}`: `GET, HEAD` và `POST, PUT, PATCH, DELETE` tới cluster `board-service`, giữ nguyên path (FR-001, BR-001). Địa chỉ upstream qua cấu hình (NFR-004).
- `/health/live`, `/health/ready` nằm ngoài route proxy và độc lập upstream (FR-002, BR-003); thời gian đến `ready` < 10 giây đo ở test Docker (NFR-005).
- Fallback `404 problem+json` (`ROUTE_NOT_FOUND`) cho mọi request không khớp route, gồm path có đuôi như tệp (`/foo.txt`) và method không hỗ trợ trên path boards (design 6.3); dùng `MapFallback("{**path}", ...)` vì mặc định của `MapFallback` có ràng buộc `nonfile`.
- `src/shared/Http` (`Teamboard.Shared.Http`, theo D0.4/Q-D2): kiểu `ErrorDefinition` (mã, status, message mặc định tiếng Anh), `ProblemFactory` dựng `problem+json` (`type`, `title`, `status`, `detail`, `code`, `traceId`, `correlationId`) và bảng mã dùng chung (`INTERNAL_ERROR`...) cho unit test chính sách (`backend-engineering.md` mục 1); mã riêng của service (`ROUTE_NOT_FOUND`) nằm ở bảng của gateway cùng kiểu `ErrorDefinition`, không có registry toàn cục (người dùng chốt); thêm vào `teamboard.slnx`.
- Project test `src/gateway.Tests` và `src/shared/Http.Tests`; file `.feature` ở `document/spec/features/gateway/`.
- Compose: service `gateway` (host 8080 → container 7070, **không** `depends_on` vì gateway phải sống và ready độc lập upstream, FR-002), Dockerfile, `config/services.yaml` (`local_port: 8080`, `container_port: 7070`), `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml` (YARP); sửa hai test Docker của TB-001 để chỉ `up` service cần thiết.

**Không làm (trong lát này):** correlation (`X-Correlation-Id`, lát 2), timeout và ánh xạ `502/503/504` (lát 3), rate limit, `ForwardedHeaders` (lát 4), giới hạn body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention, graceful shutdown (lát 7), smoke với `board-service` thật có endpoint boards (lát 8), kiểm JWT, retry, circuit breaker; test "cấu hình route sai thì fail khi khởi động" (design mục 7, để lát sau); Central Package Management (ghim phiên bản inline như TB-001, ghi nợ).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-001 route giữ path, chuyển nguyên status/body của upstream (kể cả lỗi), `404 problem+json` khi không khớp | D0.4, D3, 6.3 | B3a | @S01, @S02, @S06 | Host test + upstream giả; Scenario Outline theo method và path |
| FR-002 health, ready không phụ thuộc upstream | 6.3, 6.4 | B3b | @S03, @S04 | Host test, dừng upstream giả rồi gọi `/health/ready` |
| BR-001 không tên service trong path | 6.3 | B3a | @S01 | Upstream giả ghi lại path nhận được |
| BR-002 không nghiệp vụ, không DB | D0.2, D0.3 | B1, B3a | – | Không có test hành vi; kiểm bằng `config/services.yaml` (`datastore: none`) và review |
| BR-003 health ngoài versioning | 6.3 | B3b | @S03 | `/health/live` không có `/api/v1` |
| NFR-004 cấu hình, không hardcode | D3, `backend-engineering.md` mục 2 | B3a, B4 | @S01 | Test ghi đè địa chỉ upstream qua cấu hình (stub khởi động trước, địa chỉ truyền bằng `UseSetting`); test hygiene của TB-001 quét compose mới |
| NFR-005 ready < 10 giây | 2.2 | B4 | @S05 | Đo trong test Docker: từ `compose up -d` (image đã build) tới `/health/ready` 200 |
| Thư viện problem+json và mã lỗi (Q-D2) | D0.4, `backend-engineering.md` mục 1 | B2 | – (unit test) | Mỗi mã có status và message; không trùng; dựng đúng trường; `traceId` kể cả khi không có Activity |
| Compose chạy gateway (tiêu chí issue) | – | B4 | @S05 | Test nhóm `docker` gọi `docker compose` qua Process |
| Gate xanh (tiêu chí issue) | – | B1–B4 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất, tag `@TB-010 @gateway`):
- @S01 (Outline) Request `GET, HEAD, POST, PUT, PATCH, DELETE` tới `/api/v1/boards`, `/api/v1/boards/{id}` và có query string được chuyển tới upstream giả với đúng method và đúng path; response của upstream trả nguyên về client.
- @S02 (Outline) Path không khớp route (`/api/v1/unknown`, `/api/v1/boardsX`, `/foo.txt`) và method không hỗ trợ trên path boards (`TRACE`) nhận `404 problem+json` có `code` `ROUTE_NOT_FOUND`, `status` 404, `traceId`, `correlationId`; body không chứa tên host hay đường dẫn nội bộ.
- @S03 `GET /health/live` trả 200.
- @S04 `GET /health/ready` trả 200 khi upstream giả đã dừng.
- @S05 (`@docker`) Compose chạy riêng service `gateway`: `GET /health/live` ở cổng host 8080 trả 200 và `/health/ready` trả 200 trong 10 giây kể từ lệnh `up -d` (image đã build).
- @S06 Upstream trả `404 problem+json` (mã `BOARD_NOT_FOUND`) thì client nhận nguyên nội dung của upstream, gateway không thay bằng `ROUTE_NOT_FOUND` (thêm ở test-review; thuộc FR-001, nằm ở `routing.feature`, bước B3a).

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước. Mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff, không tính file sinh ra).

### B1. Khung project, YARP và project test
- **Kịch bản/test sẽ viết:** không có hành vi mới; một test rỗng trong `gateway.Tests` dựng `WebApplicationFactory<Program>` và chạy xanh, chứng minh project thấy `Program`. Không áp dụng "đỏ".
- **File/module dự kiến chạm:** `src/gateway/gateway.csproj` (net10.0, `RootNamespace` `Gateway`, tham chiếu `Teamboard.Shared.ServiceDefaults`, `Yarp.ReverseProxy`), `src/gateway/Program.cs` tối thiểu (`public partial class Program`), `src/gateway.Tests/gateway.Tests.csproj` (xUnit, Reqnroll.xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `ReqnrollUseIntermediateOutputPathForCodeBehind`, `RootNamespace` `Gateway.Tests`, `DisableTestParallelization` cho nhóm docker), `src/teamboard.slnx`, `config/dependencies.yaml` (YARP `verified: true` kèm nguồn). Ghim phiên bản inline như TB-001 (Central Package Management ghi nợ).
- **Kiểm tra phiên bản (đã tra 2026-10-23):** `Yarp.ReverseProxy` mới nhất trên NuGet là 2.3.0; B1 đọc tài liệu YARP xác nhận tương thích .NET 10, giấy phép MIT (nuspec), rồi pin chính xác.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** `config/dependencies.yaml`.
- **Kiểm chứng:** `dotnet build src/teamboard.slnx -warnaserror` và `dotnet test src/teamboard.slnx --filter "Category!=docker"` xanh (gồm test rỗng của gateway); `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` sạch.
- **Phụ thuộc:** không.

### B2. Thư viện `Teamboard.Shared.Http`: problem+json và kiểu mã lỗi
- **Kịch bản/test sẽ viết (đỏ trước):** unit test chính sách: mọi mã trong bảng dùng chung có status và message tiếng Anh không rỗng, không trùng (`backend-engineering.md` mục 1); unit test `ProblemFactory`: có `type`, `title`, `status`, `detail`, `code`, `traceId`, `correlationId`; `traceId` lấy từ `Activity.Current?.Id` (dạng W3C như ví dụ `api-endpoints.md` mục 5) và rơi về `HttpContext.TraceIdentifier` khi không có Activity; `correlationId` = `traceId` ở lát này (người dùng chốt P-01, lát 2 thay bằng id thật); không chứa chi tiết nội bộ. Đỏ vì thư viện chưa có (khung rỗng ném `NotImplementedException`).
- **File/module dự kiến chạm:** `src/shared/Http/Teamboard.Shared.Http.csproj` (`ErrorDefinition`, bảng mã dùng chung, `ProblemFactory`), `src/shared/Http.Tests/`, `src/teamboard.slnx`.
- **Hợp đồng thay đổi:** định nghĩa dạng `problem+json` chung theo `api-endpoints.md` mục 5; mã riêng gateway (`ROUTE_NOT_FOUND`...) nằm ở gateway cùng kiểu `ErrorDefinition` (design D0.4).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (hợp đồng đã có ở `api-endpoints.md`).
- **Kiểm chứng:** test thư viện xanh; `dotnet build -warnaserror`; `git diff` không chứa business logic trong `src/shared` (`PROJECT.md`).
- **Phụ thuộc:** B1.

### B3a. Routing và fallback 404 (FR-001: @S01, @S02)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/routing.feature` (@S01, @S02, @S06), step definition và upstream giả (Kestrel trong tiến trình, cổng ngẫu nhiên, ghi lại method và path nhận được; khởi động trước, địa chỉ truyền cho gateway bằng `WithWebHostBuilder(... UseSetting("ReverseProxy:Clusters:board-service:Destinations:d1:Address", ...))`). Đỏ vì gateway chưa có route hay fallback.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`AddServiceDefaults("gateway")`, `AddReverseProxy().LoadFromConfig(...)`, `MapReverseProxy()`, `MapDefaultEndpoints()`, `MapFallback("{**path}", ...)`), `src/gateway/appsettings.json` (Routes `boards-read` cho `GET, HEAD`, `boards-write` cho `POST, PUT, PATCH, DELETE`; Cluster `board-service` với địa chỉ upstream), `GatewayErrorCodes` (bảng mã riêng, `ErrorDefinition`) và hằng số route, `src/gateway.Tests/`, `document/spec/features/gateway/`.
- **Hợp đồng thay đổi:** route công khai `/api/v1/boards/**` (không tên service trong path); lỗi `404` `problem+json` mã `ROUTE_NOT_FOUND`. Thứ tự middleware đầy đủ của design 6.4 chưa cần ở lát này (chưa có correlation, CORS, rate limit); ghi thứ tự đích trong comment và cập nhật dần ở các lát sau.
- **Migration / cấu hình / biến môi trường:** khoá YARP `ReverseProxy:Clusters:board-service:Destinations:d1:Address`; địa chỉ mặc định `http://board-service:7070` trong cấu hình cho compose; biến môi trường tương ứng `ReverseProxy__Clusters__board-service__Destinations__d1__Address` (tên khoá đối chiếu tài liệu YARP ở B3a).
- **Tài liệu cần cập nhật:** chuyển sang B4.
- **Kiểm chứng:** @S01, @S02, @S06 xanh (gồm HEAD, `/api/v1/boards` không có segment cuối, path kiểu tệp, method lạ); `dotnet build -warnaserror`; `dotnet format --verify-no-changes`; chạy lặp 10 lần nhóm test nhanh để loại flaky (cổng ngẫu nhiên).
- **Phụ thuộc:** B1, B2.

### B3b. Health độc lập upstream (FR-002: @S03, @S04)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/health.feature` (@S03, @S04): gọi `/health/live`, rồi dừng upstream giả và gọi `/health/ready`. Đỏ nếu health chưa nối vào host gateway hoặc ready phụ thuộc upstream.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (đã gọi `MapDefaultEndpoints()` ở B3a; bước này kiểm chứng và chốt thứ tự để health không đi qua proxy), `src/gateway.Tests/`, `document/spec/features/gateway/health.feature`.
- **Hợp đồng thay đổi:** `/health/live`, `/health/ready` ngoài versioning (BR-003).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** chuyển sang B4.
- **Kiểm chứng:** @S03, @S04 xanh; chạy lặp 10 lần.
- **Phụ thuộc:** B3a.

### B4. Compose, Dockerfile, cấu hình, tài liệu và chỉnh test Docker của TB-001 (@S05)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/compose.feature` (@S05, `@docker`): `docker compose -p tb010-tests ... up --build -d gateway`, `GET /health/live` ở cổng host 8080, rồi đo từ lúc `up -d` (sau khi image đã build, tách bước build và bước `up`) tới `/health/ready` 200 trong 10 giây; dọn bằng `down` (không `-v`). Đỏ vì chưa có service `gateway` trong compose.
- **File/module dự kiến chạm:** `src/gateway/Dockerfile` (multi-stage theo mẫu `board-service`, copy project shared trước `restore`, chạy không root), `src/docker-compose.yml` (service `gateway`, host 8080 → 7070, env địa chỉ upstream `http://board-service:7070`, không `depends_on`), `config/services.yaml` (`status: existing`, `path: src/gateway`, `local_port: 8080`, `container_port: 7070`), `PROJECT.md` (Trạng thái hiện tại), `COMMANDS.md` (cổng gateway, lệnh), `src/.dockerignore` nếu cần, `src/docker-compose.dcproj` nếu liệt kê file; **test TB-001**: `src/board-service.Tests/Support/DockerCli.cs` (`ComposeUp` nhận danh sách service), `ServiceIdentitySteps.cs` (@S03 chỉ `up board-service`), `ObservabilitySteps.cs` (@S11 chỉ `up board-service jaeger`); `src/gateway.Tests/Support/` (bản sao `DockerCli`, `HttpWait`, `RepoRoot` với tên dự án compose `tb010-tests`, ghi nợ tách project dùng chung khi có service thứ ba cần).
- **Hợp đồng thay đổi:** cổng host của gateway 8080; gateway là điểm vào, `board-service` giữ cổng 6060 cho debug trực tiếp ở lát này.
- **Migration / cấu hình / biến môi trường:** `ASPNETCORE_URLS` và địa chỉ upstream qua env; không secret.
- **Tài liệu cần cập nhật:** `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml`.
- **Kiểm chứng:** @S05 xanh (cần Docker Desktop, cổng 8080 trống); hai test Docker của TB-001 (@S03, @S11) vẫn xanh khi chạy riêng và khi chạy cả solution (`dotnet test src/teamboard.slnx`, không tranh cổng vì 6060 và 8080 khác nhau); test hygiene của TB-001 (S14, S15) xanh trên compose mới; `docker compose -f src/docker-compose.yml config -q`; quét tên cũ và secret thô.
- **Phụ thuộc:** B3b.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Kiểu mã lỗi, dựng `problem+json` (`Http.Tests`) | Không I/O | Không mock |
| Integration (host in-process) | Routing, health, fallback (`gateway.Tests`) | `WebApplicationFactory<Program>` (TestServer cho phía client) + upstream giả bằng Kestrel thật ở cổng ngẫu nhiên; YARP gọi upstream qua socket thật | Không mock YARP; upstream giả ghi method và path |
| Integration Docker (nhóm `docker`) | Compose chạy gateway và thời gian ready (@S05) | Docker local; `docker compose -p` tên dự án riêng, `down` không `-v` | Gọi `docker compose` qua Process; không thêm phụ thuộc; tự bỏ qua khi không có Docker (`SkipException`) |

Kim tự tháp: nhiều unit/host test nhanh; một test Docker. Contract test: `problem+json` kiểm bằng unit test dùng chung từ `src/shared/Http`. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`, đã chốt ở TB-001).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Method không hỗ trợ trên path boards nhận `405` thay vì `404` fallback (hành vi định tuyến của ASP.NET Core) | Lệch design 6.3 | Test @S02 kiểm (có dòng `TRACE`); nếu lệch thì ghi Câu hỏi mở và hỏi người dùng, không tự đổi hợp đồng | Revert B3a |
| YARP 2.3.0 không như giả định (tên khoá cấu hình, hành vi `HEAD`, `MapFallback` đứng sau `MapReverseProxy`, `{**catch-all}` có khớp `/api/v1/boards`) | Route sai | Đọc tài liệu và test từng route ở B3a; không dùng hành vi chưa kiểm của design mục 16 trong lát này | Revert B3a |
| Upstream giả tranh cổng hoặc chạy chậm làm test flaky; địa chỉ upstream ghi đè không được đọc | Test đỏ oan | Cổng ngẫu nhiên, stub khởi động trước, `UseSetting` trước khi host build, chờ có điều kiện, không `sleep` cố định, chạy lặp 10 lần ở B3a, B3b | Sửa test, không nới điều kiện |
| Sửa test Docker của TB-001 làm hỏng @S03/@S11 | Hồi quy | Chạy lại hai test đó riêng và cả solution ở B4 | Revert phần sửa test |
| Cổng 8080 trùng cổng khác trên máy | @S05 đỏ sai lý do | Test báo rõ khi cổng bị chiếm | Đổi cổng trong compose và `config/services.yaml` |
| Dockerfile thiếu COPY project shared, image không build | @S05 đỏ | Build image ở B4 | Revert B4 |
| Không `depends_on` nên gateway khởi động trước `board-service` | Request đầu tới upstream lỗi (lát 3 xử lý `503`) | Chấp nhận (FR-002 yêu cầu gateway độc lập upstream); health không đi qua proxy | Không áp dụng |

Ảnh hưởng consumer (service khác): chưa có (gateway mới); `board-service` không đổi. Bảo mật: không xác thực ở lát này (spec mục 10), không đưa ra internet thật; `problem+json` không lộ host nội bộ (kiểm ở @S02); container chạy không root; không secret trong compose. Chi phí: chỉ local, không tài nguyên AWS.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `workflow`, `security`): LL-001 (không commit secret) và LL-005 (câu hỏi mở có lựa chọn, khuyến nghị) áp dụng; bài học từ review TB-001 (luật quét chỉ nhìn tên khoá, query lọt vào log) chưa được ghi thành lesson, nhưng được áp dụng ở đây: không log query string, test hygiene quét compose đã có ở `board-service.Tests`.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx` (gồm nhóm `docker`); `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ); `git status --short && git diff --stat`.
- Tiêu chí hoàn thành: theo mục "Tiêu chí hoàn thành" của issue TB-010.
- Tài liệu/config cập nhật: như B4; bảng lát `api-gateway-slices.md` (lát 1 → `done` khi merge), `document/issues/README.md`.

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở bước tương ứng): A-01 `Yarp.ReverseProxy` 2.3.0 hỗ trợ .NET 10 (B1); A-02 `MapFallback("{**path}", ...)` đặt cùng `MapReverseProxy` bắt mọi request không khớp, gồm method lạ trên path boards và path kiểu tệp (B3a, @S02); A-03 `HEAD` được route cùng nhóm `GET` như design 6.3 (B3a, @S01); A-04 YARP đọc địa chỉ upstream từ cấu hình khi host build nên `UseSetting` trong test có hiệu lực (B3a). Chưa xác minh; sai thì dừng và hỏi, không tự đổi cách làm.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú |
|---|---|---|---|---|
| P-01 | Giá trị `correlationId` của lát 1 | B2, @S02 | sondv | answered: dùng `traceId` tạm (2026-10-23) |
| P-02 | Cổng host của gateway | B4, @S05 | sondv | answered: 8080 → 7070 (2026-10-23) |
| P-03 | Tên project và vị trí | B1, B2 | sondv | answered: `Teamboard.Shared.Http`, `src/gateway` (assembly `gateway`), test cạnh project nguồn (2026-10-23) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-010:p01-correlation-id | Giá trị `correlationId` của lát 1 | Dùng `traceId` làm tạm, lát 2 thay bằng id thật | 1 | 2026-10-23 |
| 2 | plan-review:TB-010:p02-host-port | Cổng host của gateway | 8080 → container 7070 | 1 | 2026-10-23 |
| 3 | plan-review:TB-010:p03-project-names | Tên project | `Teamboard.Shared.Http`; `src/gateway` (assembly `gateway`); test cạnh project nguồn | 1 | 2026-10-23 |
| 4 | plan-review:TB-010:compose-shared-with-tb001-tests | Test Docker TB-001 up cả file compose khi thêm gateway | `up` chỉ service cần thiết ở cả hai bộ test; sửa test TB-001 trong B4 | 1 | 2026-10-23 |
| 5 | plan-review:TB-010:depends-on-vs-docker-rule | `depends_on` trơn trái `docker.md` | Bỏ `depends_on`, YARP phân giải DNS lúc gọi | 1 | 2026-10-23 |
| 6 | plan-review:TB-010:error-registry-extension-mechanism | Cơ chế thêm mã lỗi riêng của gateway | Shared chỉ có `ErrorDefinition` và `ProblemFactory`; mỗi service có bảng mã cùng kiểu | 1 | 2026-10-23 |
| 7 | plan-review:TB-010:docker-test-helper-location | Helper test Docker dùng chung hay copy | Copy sang `gateway.Tests` với tên dự án compose mới, ghi nợ | 1 | 2026-10-23 |
| 8 | plan-review:TB-010:nfr005-measurement-validity | Đo NFR-005 thế nào | Đo trong test Docker (@S05): từ `compose up -d` tới `/health/ready` 200 trong 10 giây | 1 | 2026-10-23 |

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-23 | Toàn bộ checklist A0, A–H (sub-agent độc lập) | 5 CONFIRM + 3 câu hỏi mở P-01..P-03 (đã chốt cả 8) | 6 FIX (đã sửa) | NEEDS_CONFIRM |
| 2 | 2026-10-23 | Delta: B3 tách đôi, @S05 Docker, sửa test TB-001, quyết định P-01..P-03 | 0 | 0 | PASS |

### Fix log vòng 1

| Fingerprint | Sửa gì |
|---|---|
| fallback-nonfile-constraint | Dùng `MapFallback("{**path}", ...)`; @S02 thêm dòng path kiểu tệp |
| s01-path-examples | @S01 thêm `/api/v1/boards`, `/api/v1/boards/{id}`, query string; @S02 thêm `/api/v1/boardsX` và `TRACE` |
| traceid-source-unspecified | B2 nêu `Activity.Current?.Id`, fallback `HttpContext.TraceIdentifier`, unit test khi không có Activity |
| services-yaml-fields | B4 ghi `container_port: 7070` cùng `local_port: 8080`; kiểm S14/S15 của TB-001 trên compose mới |
| dependency-pinning-cpm | B1 ghi ghim inline như TB-001; Central Package Management ghi nợ |
| b1-no-red-and-test-feasibility | B1 thêm test dựng `Program`; tách B3 thành B3a (@S01, @S02) và B3b (@S03, @S04); nêu cơ chế ghi đè địa chỉ upstream (stub khởi động trước, `UseSetting`, giả định A-04) |

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-23 | Tạo bản nháp | plan-init cho TB-010 (lát 1 của gateway) |
| 2026-10-23 | plan-review vòng 1: ghi quyết định P-01..P-03 và 5 CONFIRM; sửa 6 FIX; tách B3; gộp NFR-005 vào @S05 (test Docker); đánh số lại kịch bản (@S05 là test Docker) | plan-review TB-010 |
| 2026-10-23 | test-review: thêm @S06 (upstream trả lỗi được chuyển nguyên) vào B3a; upstream giả trả 201/204 theo method | test-review TB-010 |
