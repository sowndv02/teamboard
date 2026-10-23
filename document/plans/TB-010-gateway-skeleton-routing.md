---
title: Gateway lát 1, khung YARP, route boards, health và 404 problem+json
issue: TB-010
status: draft            # draft | in-review | approved | done
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
- `/health/live`, `/health/ready` nằm ngoài route proxy và độc lập upstream (FR-002, BR-003); thời gian đến `ready` < 10 giây (NFR-005).
- Fallback `404 problem+json` (`ROUTE_NOT_FOUND`) cho mọi request không khớp route, gồm method không hỗ trợ trên path boards (design 6.3).
- `src/shared/Http` (`Teamboard.Shared.Http`, theo D0.4/Q-D2): dựng `problem+json` (`type`, `title`, `status`, `detail`, `code`, `traceId`, `correlationId`) và registry mã lỗi dùng chung (`ErrorCodes`, ánh xạ mã → status + message mặc định, `backend-engineering.md` mục 1); thêm vào `teamboard.slnx`.
- Project test `src/gateway.Tests` và `src/shared/Http.Tests`; file `.feature` ở `document/spec/features/gateway/`.
- Compose: service `gateway` (`depends_on: board-service`), Dockerfile, `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml` (YARP).

**Không làm (trong lát này):** correlation (`X-Correlation-Id`, lát 2), timeout và ánh xạ `502/503/504` (lát 3), rate limit, `ForwardedHeaders` (lát 4), giới hạn body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention, graceful shutdown (lát 7), smoke với `board-service` thật có endpoint boards (lát 8), kiểm JWT, retry, circuit breaker.

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-001 route giữ path, `404 problem+json` khi không khớp | D0.4, D3, 6.3 | B3 | @S01, @S02 | Host test + upstream giả; Scenario Outline theo method và path |
| FR-002 health, ready không phụ thuộc upstream | 6.3, 6.4 | B3 | @S03, @S04 | Host test, dừng upstream giả rồi gọi `/health/ready` |
| BR-001 không tên service trong path | 6.3 | B3 | @S01 | Upstream giả ghi lại path nhận được |
| BR-002 không nghiệp vụ, không DB | D0.2, D0.3 | B1, B3 | – | Không có test hành vi; kiểm bằng `config/services.yaml` (`datastore: none`) và review |
| BR-003 health ngoài versioning | 6.3 | B3 | @S03 | `/health/live` không có `/api/v1` |
| NFR-004 cấu hình, không hardcode | D3, `backend-engineering.md` mục 2 | B3, B4 | @S01 | Test ghi đè địa chỉ upstream qua cấu hình; quét secret thô |
| NFR-005 ready < 10 giây | 2.2 | B3 | @S05 | Đo thời gian từ dựng host tới `ready` 200 |
| Thư viện problem+json và registry mã lỗi (Q-D2) | D0.4, `backend-engineering.md` mục 1 | B2 | – (unit test) | Mỗi mã có status và message; không trùng; dựng đúng trường |
| Compose chạy gateway (tiêu chí issue) | – | B4 | @S06 | Test nhóm `docker` gọi `docker compose` qua Process |
| Gate xanh (tiêu chí issue) | – | B1–B4 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất, tag `@TB-010 @gateway`):
- @S01 (Outline) Request `GET, HEAD, POST, PUT, PATCH, DELETE` tới `/api/v1/boards/...` được chuyển tới upstream giả với đúng method và đúng path; response của upstream trả nguyên về client.
- @S02 (Outline) Path không khớp route (ví dụ `/api/v1/unknown`) và method không hỗ trợ trên path boards nhận `404 problem+json` có `code` `ROUTE_NOT_FOUND`, `status` 404, `traceId`.
- @S03 `GET /health/live` trả 200.
- @S04 `GET /health/ready` trả 200 khi upstream giả đã dừng.
- @S05 Gateway sẵn sàng (`/health/ready` 200) trong dưới 10 giây kể từ lúc khởi động host.
- @S06 (`@docker`) Compose chạy service `gateway` và `GET /health/live` ở cổng host của gateway trả 200.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước. Mỗi bước một commit nhỏ.

### B1. Khung project, YARP và project test
- **Kịch bản/test sẽ viết:** không có hành vi mới; một test rỗng chạy xanh chứng minh runner hoạt động. Không áp dụng "đỏ".
- **File/module dự kiến chạm:** `src/gateway/gateway.csproj` (net10.0, `RootNamespace` `Gateway`, tham chiếu `Teamboard.Shared.ServiceDefaults`, `Yarp.ReverseProxy`), `src/gateway/Program.cs` tối thiểu (`public partial class Program`), `src/gateway.Tests/gateway.Tests.csproj` (xUnit, Reqnroll.xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `ReqnrollUseIntermediateOutputPathForCodeBehind`, `RootNamespace` `Gateway.Tests`, `DisableTestParallelization` cho nhóm docker), `src/teamboard.slnx`, `config/dependencies.yaml` (YARP `verified: true` kèm nguồn).
- **Kiểm tra phiên bản (đã tra 2026-10-23):** `Yarp.ReverseProxy` mới nhất trên NuGet là 2.3.0; B1 đọc tài liệu YARP xác nhận tương thích .NET 10, giấy phép MIT (nuspec), rồi pin chính xác.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** `config/dependencies.yaml`.
- **Kiểm chứng:** `dotnet build src/teamboard.slnx -warnaserror` và `dotnet test src/teamboard.slnx --filter "Category!=docker"` xanh; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` sạch.
- **Phụ thuộc:** không.

### B2. Thư viện `Teamboard.Shared.Http`: problem+json và registry mã lỗi
- **Kịch bản/test sẽ viết (đỏ trước):** unit test cho registry: mọi `ErrorCode` có status và message mặc định không rỗng; không mã trùng; `FromHttpStatus` cho mã chưa có riêng (`backend-engineering.md` mục 1); unit test dựng `problem+json`: có `type`, `title`, `status`, `detail`, `code`, `traceId`, `correlationId`; không chứa chi tiết nội bộ. Đỏ vì thư viện chưa có (khung rỗng ném `NotImplementedException`).
- **File/module dự kiến chạm:** `src/shared/Http/Teamboard.Shared.Http.csproj` (`ErrorCodes`, bảng ánh xạ mã → status + message tiếng Anh, `ProblemFactory`), `src/shared/Http.Tests/`, `src/teamboard.slnx`.
- **Hợp đồng thay đổi:** định nghĩa dạng `problem+json` chung theo `api-endpoints.md` mục 5; chỉ chứa mã dùng chung, mã riêng gateway (`ROUTE_NOT_FOUND`...) nằm ở gateway nhưng đăng ký qua cùng cơ chế (design D0.4).
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không (hợp đồng đã có ở `api-endpoints.md`).
- **Kiểm chứng:** test thư viện xanh; `dotnet build -warnaserror`; `git diff` không chứa business logic trong `src/shared` (`PROJECT.md`).
- **Phụ thuộc:** B1. Câu hỏi mở P-01 (giá trị `correlationId` khi chưa có middleware) phải chốt trước.

### B3. Routing, health và fallback 404 (FR-001, FR-002: @S01–@S05)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/routing.feature` (@S01, @S02), `health.feature` (@S03–@S05) cùng step definition và upstream giả (Kestrel trong tiến trình, cổng ngẫu nhiên, ghi lại method và path nhận được). Đỏ vì gateway chưa có route, health thật hay fallback.
- **File/module dự kiến chạm:** `src/gateway/Program.cs` (`AddServiceDefaults("gateway")`, `AddReverseProxy().LoadFromConfig(...)`, `MapReverseProxy()`, `MapDefaultEndpoints()`, `MapFallback`), `src/gateway/appsettings.json` (Routes `boards-read` cho `GET, HEAD`, `boards-write` cho `POST, PUT, PATCH, DELETE`; Cluster `board-service` với địa chỉ upstream; địa chỉ qua biến môi trường ghi đè ở compose), hằng số route/mã lỗi riêng gateway (`GatewayErrorCodes`, `ApiRoutes`), `src/gateway.Tests/`, `document/spec/features/gateway/`.
- **Hợp đồng thay đổi:** route công khai `/api/v1/boards/**` (không tên service trong path); health `/health/*`; lỗi `404` `problem+json` mã `ROUTE_NOT_FOUND`. Thứ tự middleware đầy đủ của design 6.4 chưa cần ở lát này (chưa có correlation, CORS, rate limit); ghi chú thứ tự đích trong comment và cập nhật dần ở các lát sau.
- **Migration / cấu hình / biến môi trường:** `ReverseProxy__Clusters__board-service__Destinations__d1__Address` (hoặc tên tương đương theo schema YARP); địa chỉ mặc định `http://board-service:7070` trong cấu hình cho compose.
- **Tài liệu cần cập nhật:** chuyển sang B4.
- **Kiểm chứng:** @S01–@S05 xanh; `dotnet build -warnaserror`; `dotnet format --verify-no-changes`; chạy lặp 10 lần nhóm test nhanh để loại flaky (cổng ngẫu nhiên, thời gian khởi động).
- **Phụ thuộc:** B1, B2.

### B4. Compose, Dockerfile, cấu hình và tài liệu (@S06)
- **Kịch bản/test sẽ viết (đỏ trước):** `document/spec/features/gateway/compose.feature` (@S06, `@docker`): `docker compose -p <tên riêng> up --build -d` rồi `GET /health/live` ở cổng host của gateway, dọn bằng `down` (không `-v`). Đỏ vì chưa có service `gateway` trong compose.
- **File/module dự kiến chạm:** `src/gateway/Dockerfile` (multi-stage theo mẫu `board-service`, copy project shared trước `restore`, chạy không root), `src/docker-compose.yml` (service `gateway`, `depends_on: board-service`, cổng host theo P-02), `config/services.yaml` (`status: existing`, `path: src/gateway`, `local_port`), `PROJECT.md` (Trạng thái hiện tại), `COMMANDS.md` (cổng gateway, lệnh), `src/.dockerignore` nếu cần, `src/docker-compose.dcproj` nếu liệt kê file.
- **Hợp đồng thay đổi:** cổng host của gateway; gateway là điểm vào, `board-service` vẫn giữ cổng 6060 cho debug trực tiếp ở lát này.
- **Migration / cấu hình / biến môi trường:** `ASPNETCORE_URLS` và địa chỉ upstream qua env; không secret.
- **Tài liệu cần cập nhật:** `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml`.
- **Kiểm chứng:** @S06 xanh (cần Docker Desktop, cổng host của gateway trống); `docker compose -f src/docker-compose.yml config -q`; chạy toàn bộ test (`dotnet test src/teamboard.slnx`) xanh; quét tên cũ và secret thô.
- **Phụ thuộc:** B3.

## 4. Chiến lược test

| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit | Registry mã lỗi, dựng `problem+json` (`Http.Tests`) | Không I/O | Không mock |
| Integration (host in-process) | Routing, health, fallback, thời gian ready (`gateway.Tests`) | `WebApplicationFactory<Program>` + upstream giả bằng Kestrel thật ở cổng ngẫu nhiên | Không mock YARP; upstream giả ghi method và path |
| Integration Docker (nhóm `docker`) | Compose chạy gateway (@S06) | Docker local; `docker compose -p` tên dự án riêng, `down` không `-v` | Gọi `docker compose` qua Process; không thêm phụ thuộc; tự bỏ qua khi không có Docker (`SkipException`) |

Kim tự tháp: nhiều unit/host test nhanh; một test Docker. Contract test: `problem+json` kiểm bằng unit test dùng chung từ `src/shared/Http`. Công cụ: xUnit 2.9.3 + Reqnroll.xUnit 3.3.4 (`config/tool-versions.yaml`, đã chốt ở TB-001).

## 5. Rủi ro và rollback

| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Method không hỗ trợ trên path boards nhận `405` thay vì `404` fallback (hành vi định tuyến của ASP.NET Core) | Lệch design 6.3 | Test @S02 kiểm; nếu lệch thì ghi Câu hỏi mở và hỏi người dùng, không tự đổi hợp đồng | Revert B3 |
| YARP 2.3.0 không như giả định (tên khoá cấu hình, hành vi `HEAD`, `MapFallback` đứng sau `MapReverseProxy`) | Route sai | Đọc tài liệu và test từng route ở B3; không dùng hành vi chưa kiểm của design mục 16 trong lát này | Revert B3 |
| Upstream giả tranh cổng hoặc chạy chậm làm test flaky | Test đỏ oan | Cổng ngẫu nhiên, chờ có điều kiện, không `sleep` cố định, chạy lặp ở B3 | Sửa test, không nới điều kiện |
| Cổng host của gateway trùng cổng khác trên máy | @S06 đỏ sai lý do | Chốt P-02; test báo rõ khi cổng bị chiếm | Đổi cổng trong compose và `config/services.yaml` |
| Dockerfile thiếu COPY project shared, image không build | @S06 đỏ | Build image ở B4 | Revert B4 |
| `correlationId` trong `problem+json` ở lát 1 chưa có nguồn thật | Hợp đồng lỗi sai nghĩa | Chốt P-01 trước B2 | Đổi ở lát 2 (mã và trường không đổi) |

Ảnh hưởng consumer (service khác): chưa có (gateway mới); `board-service` không đổi. Bảo mật: không xác thực ở lát này (spec mục 10), không đưa ra internet thật; `problem+json` không lộ host nội bộ; container chạy không root; không secret trong compose. Chi phí: chỉ local, không tài nguyên AWS.

Bài học đã tra (`document/lessons/README.md`, tag `api`, `observability`, `workflow`, `security`): LL-001 (không commit secret) và LL-005 (câu hỏi mở có lựa chọn, khuyến nghị) áp dụng; bài học về luật quét bí mật và query lọt log (TB-001) chưa được ghi thành lesson, nhưng được áp dụng ở đây: không log query string, test quét compose đã có ở `board-service.Tests`.

## 6. Gate và Definition of Done

- Gate (`code-change-gate.md`, `quality-gate.md`): `dotnet build src/teamboard.slnx -warnaserror`; `dotnet test src/teamboard.slnx` (gồm nhóm `docker`); `dotnet format src/teamboard.slnx --verify-no-changes`; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive`; `docker compose -f src/docker-compose.yml config -q`; quét secret thô (`gitleaks` chưa cài, ghi rõ); `git status --short && git diff --stat`.
- Tiêu chí hoàn thành: theo mục "Tiêu chí hoàn thành" của issue TB-010.
- Tài liệu/config cập nhật: như B4; bảng lát `api-gateway-slices.md` (lát 1 → `done` khi merge), `document/issues/README.md`.

## 7. Câu hỏi mở và giả định

Giả định (kiểm ở bước tương ứng): A-01 `Yarp.ReverseProxy` 2.3.0 hỗ trợ .NET 10 (B1); A-02 `MapFallback` đặt sau `MapReverseProxy` bắt mọi request không khớp, gồm method lạ trên path boards (B3, @S02); A-03 `HEAD` được route cùng nhóm `GET` như design 6.3 (B3, @S01).

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| P-01 | Giá trị `correlationId` trong `problem+json` của lát 1, khi middleware correlation (lát 2) chưa có | B2, B3, @S02 | sondv | open |
| P-02 | Cổng host của gateway (`config/services.yaml` đang `TODO`) | B4, @S06 | sondv | open |
| P-03 | Tên project và vị trí: `Teamboard.Shared.Http` ở `src/shared/Http`, gateway ở `src/gateway` với assembly `gateway`, test cạnh project nguồn | B1, B2 | sondv | open |

### P-01 `correlationId` ở lát 1
- **Lựa chọn:** A) dùng `traceId` làm `correlationId` cho tới lát 2 (trường luôn có, đúng `api-endpoints.md` "luôn có `traceId` và `correlationId`", lát 2 thay bằng id thật); B) để `correlationId` rỗng/null ở lát 1 (vi phạm quy ước "luôn có"); C) đưa middleware correlation vào lát 1 (lát 1 phình, lệch bảng lát đã duyệt).
- **Khuyến nghị:** A, vì giữ đúng hợp đồng mà không đổi phạm vi lát đã duyệt; chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** B2 không biết điền trường nào; test dựng `problem+json` không viết được.
- **Mặc định nếu quá hạn:** không dùng mặc định (ảnh hưởng hợp đồng công khai).

### P-02 Cổng host của gateway
- **Lựa chọn:** A) 8080 → container 7070 (cổng điểm vào quen thuộc, không đụng 6060 của `board-service`); B) 6061 → 7070 (cạnh `board-service`); C) 5000 → 7070.
- **Khuyến nghị:** A, vì gateway là điểm vào duy nhất của client nên cổng dễ nhớ; `board-service` giữ 6060 để debug trực tiếp. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** B4 không ghi được `local_port` và test @S06 không biết gọi cổng nào.
- **Mặc định nếu quá hạn:** không dùng mặc định (cổng là giá trị `TODO` cần người dùng chốt).

### P-03 Tên project
- **Lựa chọn:** A) `Teamboard.Shared.Http` (`src/shared/Http`), gateway `src/gateway/gateway.csproj` (assembly `gateway`, namespace `Gateway`), test `src/gateway.Tests` và `src/shared/Http.Tests` (khớp quy ước TB-001 và design D0.4); B) `Teamboard.Shared.ProblemDetails`; C) gateway tên `Teamboard.Gateway`.
- **Khuyến nghị:** A, vì khớp tên đã nêu ở design và pattern đặt tên của `board-service` và `ServiceDefaults`. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** không tạo project ở B1 và B2.
- **Mặc định nếu quá hạn:** không dùng mặc định.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi

| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-23 | Tạo bản nháp | plan-init cho TB-010 (lát 1 của gateway) |
