---
key: TB-010
title: Gateway lát 1, khung YARP, route boards, health và 404 problem+json
status: in-progress
phase: 1
branch: feature/TB-010-gateway-skeleton-routing
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-23
started: 2026-10-23
closed: null            # ngày done/dropped
---

# TB-010 — Gateway lát 1, khung YARP, route boards, health và 404 problem+json

## Bối cảnh
Lát 1 của `document/plans/api-gateway-slices.md` (feature api-gateway; spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`, issue tài liệu TB-007). Xương sống để các lát sau (correlation, lỗi upstream, rate limit, body, CORS, quan sát) cắm vào.

**Phụ thuộc:** TB-001 đã merge (PR 8, `board-service`, `src/shared/ServiceDefaults`, Jaeger). Branch `feature/TB-010-gateway-skeleton-routing` tạo từ `main` ngày 2026-10-23.

## Phạm vi
- Làm:
  - Dự án `src/gateway` (host ASP.NET Core, YARP theo cấu hình tĩnh) và project test host in-process với upstream giả.
  - Route `GET, HEAD` và `POST, PUT, PATCH, DELETE` của `/api/v1/boards/**` giữ nguyên path tới upstream (FR-001).
  - `/health/live`, `/health/ready` độc lập upstream (FR-002); thời gian đến `ready` < 10 giây (NFR-005).
  - Fallback `404 problem+json` (`ROUTE_NOT_FOUND`) cho mọi request không khớp route, gồm method không hỗ trợ.
  - Thư viện nhỏ trong `src/shared` cho `problem+json` (Q-D2), thêm vào `teamboard.slnx`.
  - Compose service `gateway` và cập nhật `config/services.yaml` (cổng host 8080 → container 7070), cấu hình qua env, không secret (NFR-004).
- Không làm: correlation/trace, lỗi upstream và timeout, rate limit, giới hạn body, CORS, metric/log đặc thù, smoke với board-service thật (các lát 2–8).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] `GET /api/v1/boards` qua gateway tới upstream giả trả đúng status/body, path không đổi.
- [x] Path không khớp route trả `404 problem+json` có `code: ROUTE_NOT_FOUND`, `traceId`, `correlationId`.
- [x] `/health/live` và `/health/ready` trả `200`; `ready` vẫn `200` khi upstream chết.
- [x] Gateway sẵn sàng trong dưới 10 giây (đo trong test Docker, từ `compose up -d`).
- [x] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.
- [x] `config/services.yaml`, compose và `PROJECT.md` khớp.

## Kế hoạch
Plan chi tiết: `document/plans/TB-010-gateway-skeleton-routing.md` (4 bước: khung và YARP, thư viện problem+json, routing và health, compose).

## Ghi chú / Nhật ký
- 2026-10-23: tạo từ lát 1 của bảng lát api-gateway (người dùng duyệt bảng 2026-10-23).
- 2026-10-23: TB-001 đã merge; tạo branch từ `main` và lập plan draft.

## Workflow

Làn: lớn (service mới). Spec/design/bảng lát dùng chung ở TB-007; bước bắt đầu từ plan-init của lát.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-23 | `document/plans/TB-010-gateway-skeleton-routing.md` (draft), 4 bước B1–B4, 3 câu hỏi mở P-01..P-03 |
| plan-review | PASS | 2 | 2026-10-23 | plan `approved`; 8 quyết định (xem plan, Confirm log), 6 FIX; review độc lập (sub-agent) |
| test-init | done | – | 2026-10-23 | đỏ: 24 test nhanh đúng lý do (17 gateway.Tests + 7 Http.Tests) và @S05 Docker đỏ khi chạy thật; khung biên dịch: `Teamboard.Shared.Http` (stub), `gateway` (Program rỗng), `GatewayErrorCodes` rỗng |
| test-review | PASS | 2 | 2026-10-23 | `.feature` sẵn sàng code; thêm @S06; 1 CONFIRM và 4 FIX đã xử lý |
| coding-start | done | – | 2026-10-24 | xanh 59/59 (7 Http + 11 ServiceDefaults + 18 gateway + 23 board-service, gồm 3 test Docker); gate: build -warnaserror, format, vulnerable, compose config xanh; 4 commit B1–B4 |
| coding-review | PASS | 2 | 2026-10-24 | review độc lập (sub-agent) vòng 1: 4 P3 đã xử lý; vòng 2 delta: Sẵn sàng merge; 59 test xanh |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 Route boards giữ method, path, status và body của upstream (Outline, 8 dòng; upstream giả trả 201 cho POST, 204 cho DELETE) | `GatewaySteps` (routing.feature) + upstream giả `UpstreamStub` | đỏ: `Assert.Single` — upstream giả không nhận request nào (gateway chưa có route) | xanh (B3) |
| @S02 404 problem+json và không tới upstream (Outline, 4 dòng: `/api/v1/unknown`, `/api/v1/boardsX`, `/foo.txt`, `TRACE /api/v1/boards`) | `GatewaySteps` | đỏ: `Assert.Equal` expected `application/problem+json`, actual null (404 rỗng mặc định) | xanh (B3) |
| @S03 Live trả 200 | `GatewaySteps` (health.feature) | đỏ: expected OK, actual NotFound | xanh (B3) |
| @S04 Ready trả 200 khi upstream đã dừng | `GatewaySteps` | đỏ: expected OK, actual NotFound | xanh (B3) |
| @S05 Compose chạy riêng gateway, ready trong 10 giây (`@docker`) | `ComposeSteps` + `DockerCli` (bản sao) | đỏ: `docker compose build gateway`: no such service: gateway (chạy thật với Docker Desktop) | xanh (B4) |
| @S06 Upstream trả 404 problem+json được chuyển nguyên (thêm ở test-review) | `GatewaySteps` + `UpstreamStub` (404 theo đường dẫn) | đỏ: expected `application/problem+json`, actual null (gateway chưa có route) | xanh (B3) |
| Unit: `ProblemFactory` (4 ca) | `Teamboard.Shared.Http.Tests` | đỏ: `NotImplementedException` (khung rỗng) | xanh (B2) |
| Unit: `SharedErrors` (3 ca) | `Teamboard.Shared.Http.Tests` | đỏ: bảng mã trống, không có `INTERNAL_ERROR` | xanh (B2) |
| Unit: `GatewayErrorCodes` (2 ca) | `gateway.Tests` | đỏ: bảng mã trống, không có `ROUTE_NOT_FOUND` | xanh (B3) |

Lệnh chạy test (phạm vi theo `quality-gate.md`):

- Nhanh (không Docker): `dotnet test src/teamboard.slnx --filter "Category!=docker"`.
- Docker (@S05 của gateway; @S03, @S11 của board-service): `dotnet test src/gateway.Tests/gateway.Tests.csproj --filter "Category=docker"`; cần Docker Desktop chạy và cổng 8080 trống; không có Docker thì scenario bị bỏ qua (`SkipException`), không phải đỏ.
- Đầy đủ: `dotnet test src/teamboard.slnx` (hai assembly chạy tuần tự trong từng assembly, `DisableTestParallelization`).

Giới hạn đã biết: @S05 đo thời gian khởi động trên Docker Desktop nên có thể chậm hơn máy khác (ngưỡng 10 giây theo NFR-005, không nới); giả định A-04 (ghi đè địa chỉ upstream bằng `UseSetting`) đã kiểm ở B3 (hiệu lực); bản sao helper Docker là nợ kỹ thuật đã được người dùng chấp nhận.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-23 | `7f86b6b` | Cần sửa feature | 1 CONFIRM (upstream trả lỗi, đã chốt: thêm @S06) và 4 FIX: kiểm status/body upstream thay vì chỉ 200, @S03 thiếu kiểm upstream không nhận request, bảng mã gateway thiếu kiểm UPPER_SNAKE, thiếu lệnh chạy test |
| 2 | 2026-10-23 | delta sau `7f86b6b` | Sẵn sàng code | không còn finding mở; 17 test nhanh đỏ đúng lý do, @S05 đỏ khi chạy thật; format xanh |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-24 | `8645d4d` | Sẵn sàng sau khi sửa | 0 P0–P2; 4 P3 (1 CONFIRM đã chốt, 3 FIX); gate xanh |
| 2 | 2026-10-24 | delta `8645d4d..HEAD` | Sẵn sàng merge | không còn finding mở; 59 test xanh (gồm 3 Docker) sau sửa |


## Fix log (coding-review)

| Fingerprint | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|
| gateway.Tests:GatewaySteps.ThenNoInternalNames:weak-assert (P3) | `GatewaySteps.cs`, `routing.feature` @S02 | Test không kiểm `type`, `title` và body có thể chứa path của request | Thêm bước kiểm `type` = `https://teamboard/errors/route-not-found`, `title` = `Not Found`; body không chứa path request | @S02 xanh (17/17 test nhanh) |
| gateway:Dockerfile:expose-vs-default-port (P3) | `src/gateway/Dockerfile` | `EXPOSE 7070` nhưng image mặc định nghe 8080 | Thêm `ENV ASPNETCORE_URLS=http://+:7070` | build xanh; @S05 Docker xanh |
| gateway.Tests:HttpWait:first-response-wins (P3) | `Support/HttpWait.cs`, `ComposeSteps.cs` | Trả ngay mã đầu tiên kể cả không phải 200, vòng poll 1 giây | Thêm `GetStatusUntilAsync` thử lại tới khi đúng mã, bước 250 ms; @S05 dùng nó | @S05 xanh khi chạy thật |
| gateway:INTERNAL_ERROR-handler:unassigned-slice (P3, CONFIRM) | `api-gateway-slices.md` | `500 INTERNAL_ERROR` và ExceptionHandler chưa thuộc lát nào | Người dùng chốt: gán vào lát 2; cập nhật bảng lát và Confirm log của bảng | commit sửa tài liệu |

Giới hạn đã biết (ghi nhận):
- `/health/*` trả 200 cho mọi method (do `MapDefaultEndpoints` của ServiceDefaults), không chỉ `GET` như design 6.3; chưa có test, để lát 7 xem xét.
- YARP mặc định tự thêm `X-Forwarded-*`; xử lý ở lát 4 theo DA-05.
- `ForwardedHeaders`, CORS, rate limit, giới hạn body, correlation thật, ExceptionHandler, lỗi upstream (`502/503/504`) chưa có: đúng thứ tự lát 2–7.
- Bản sao helper Docker giữa hai project test là nợ kỹ thuật đã được chấp nhận.

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
