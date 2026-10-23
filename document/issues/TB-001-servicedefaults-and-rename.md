---
key: TB-001
title: Đổi tên board-service, thêm ServiceDefaults và Jaeger
status: in-progress
phase: 0
branch: feature/TB-001-servicedefaults-and-rename
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-20
started: 2026-10-23     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-001 — Đổi tên board-service, thêm ServiceDefaults và Jaeger

## Bối cảnh
Giai đoạn 0 của lộ trình (`document/architecture/README.md` §6): chuẩn bị nền để mọi service sau dùng chung cách quan sát và health check. Gateway (TB-007, lát 1 = TB-010) phụ thuộc issue này theo quyết định Q-D1 của design gateway.

**Yêu cầu gốc (giữ nguyên):** "Đổi tên `src/teamboard-api` thành `src/board-service` (project, Dockerfile, compose, solution, `config/services.yaml`). Tạo `src/shared/ServiceDefaults` (OpenTelemetry trace/metric/log, `/health/live`, `/health/ready`). Thêm Jaeger vào compose (file riêng cho observability)."

## Phạm vi
- Làm:
  - Đổi tên `src/teamboard-api` → `src/board-service` (thư mục, csproj, namespace `BoardService`, Dockerfile, compose, image, `teamboard.slnx`, `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, các nơi tham chiếu khác). Giữ cổng host 6060 → container 7070 và tên solution `teamboard.slnx`.
  - Xoá controller mẫu `WeatherForecast` (người dùng đồng ý 2026-10-23).
  - Tạo `src/shared/ServiceDefaults` (OpenTelemetry trace, metric, log; `/health/live`, `/health/ready`) và gắn vào `board-service`.
  - Thêm Jaeger vào compose (file riêng `src/docker-compose.observability.yml`); service gửi OTLP trực tiếp tới Jaeger.
  - Test: xUnit + Reqnroll (BDD) cho health và cấu hình telemetry; ghi công cụ vào `config/tool-versions.yaml` và `config/dependencies.yaml`.
- Không làm: thêm service mới, đổi nghiệp vụ, Terraform, OpenTelemetry Collector, Prometheus/Grafana/log tập trung, `correlationId`/`problem+json` dùng chung (thuộc gateway TB-010), check database ở `/health/ready`, sửa `.env*`.

## Spec ngắn (làn vừa)

**FR-001** Là lập trình viên, tôi muốn service tên `board-service` nhất quán ở mọi nơi để các issue sau (gateway, task) tham chiếu đúng tên. [Must · nguồn: yêu cầu gốc]
- Given repo sau thay đổi, When tìm tên cũ `teamboard-api`/`teamboard_api` trong code, compose, config, tài liệu hiện hành, Then không còn tham chiếu (trừ lịch sử git, issue/ADR cũ bất biến và file `bin/obj`).
- Given `docker compose up --build`, When service khởi động, Then service tên `board-service` nghe ở host `6060`.

**FR-002** Là vận hành, tôi muốn `/health/live` và `/health/ready` để orchestrator kiểm tra. [Must · nguồn: yêu cầu gốc, `PROJECT.md`]
- Given service đã khởi động xong, When `GET /health/live` và `/health/ready`, Then cả hai `200`.
- Given `ready` ở lát này chỉ kiểm "ứng dụng đã sẵn sàng" (Q-02 đã chốt), When service chưa khởi động xong, Then `ready` không trả `200` (kịch bản kiểm bằng host test).

**FR-003** Là lập trình viên, tôi muốn trace, metric, log của service xuất bằng OpenTelemetry để debug nhiều service. [Must · nguồn: yêu cầu gốc, `observability.md`]
- Given compose có Jaeger, When gọi một request tới `board-service`, Then trace của request hiện trong Jaeger với tên service `board-service`.
- Given biến môi trường endpoint OTLP không đặt, When service chạy, Then service vẫn khởi động bình thường (không crash vì thiếu collector).

**FR-004** Là lập trình viên, tôi muốn ServiceDefaults dùng lại được cho service khác. [Should · nguồn: yêu cầu gốc]
- Given một service khác gọi `AddServiceDefaults()` và `MapDefaultEndpoints()`, Then nhận cùng OpenTelemetry và health mà không copy code.

**NFR**
- NFR-001: không secret trong code/compose; endpoint OTLP nằm trong cấu hình/env.
- NFR-002: `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` xanh.
- NFR-003: phiên bản gói pin chính xác (`dependency-pinning.md`); mọi phụ thuộc mới có dòng trong `config/dependencies.yaml` (OpenTelemetry T1; Reqnroll T2 cần phê duyệt, bọc sau interface).
- NFR-004: telemetry và log không chứa secret, `Authorization`, cookie hay body request (GDPR, `config/project.yaml`).
- NFR-005: Jaeger và cổng quản trị chỉ bind `127.0.0.1` ở local; image pin phiên bản, không `latest` (`docker.md`).

**Giả định:** A-01 Jaeger all-in-one nhận OTLP gốc (cổng 4317/4318) ở phiên bản pin; A-02 Reqnroll chạy được trên .NET 10 và xUnit (chưa kiểm, kiểm ở bước plan); A-03 image Jaeger có giấy phép Apache-2.0 (CNCF, `verified: false` ở `config/dependencies.yaml`, kiểm trước khi dùng).

**Câu hỏi mở:**

| # | Câu hỏi | Trạng thái |
|---|---|---|
| Q-03 | Request nào tạo ra trace để kiểm chứng khi đã xoá controller mẫu, và health có bị lọc khỏi trace không? | answered |

### Q-03 Trace nào để kiểm chứng
- **Lựa chọn:** A) không lọc health khỏi trace ở lát này, dùng chính `GET /health/live` làm request kiểm trace (lọc sau khi có endpoint thật); B) lọc health khỏi trace ngay và thêm một endpoint tạm để kiểm; C) giữ một endpoint mẫu tối thiểu.
- **Khuyến nghị:** A, vì không cần endpoint thừa và kiểm chứng được ngay; B/C thêm code tạm. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** tiêu chí "request hiện trong Jaeger" không có request cụ thể để kiểm.
- **Quyết định (2026-10-23):** A, không lọc health khỏi trace, dùng `GET /health/live`.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `dotnet build src/teamboard.slnx -warnaserror` thành công; không còn tham chiếu tên cũ (FR-001).
- [ ] `docker compose` (file chính + observability) chạy `board-service` và Jaeger.
- [ ] `GET /health/live` và `/health/ready` trả 200 (FR-002).
- [ ] Một request tới `board-service` hiện thành trace trong Jaeger (FR-003, theo Q-03).
- [ ] `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/tool-versions.yaml`, `config/dependencies.yaml` khớp.
- [ ] Test (xUnit + Reqnroll) xanh; gate theo NFR-002.

## Kế hoạch
Plan chi tiết: `document/plans/TB-001-servicedefaults-and-rename.md` (6 bước: công cụ, đổi tên, health, OpenTelemetry, Jaeger, đồng bộ tài liệu).

## Ghi chú / Nhật ký
- 2026-10-23: chọn làn vừa, phạm vi trọn gói (đổi tên + ServiceDefaults + Jaeger); nhánh tạo từ `origin/main` (`28a7792`).

## Workflow

Làn: vừa (một service cộng một thư viện dùng chung, không nghiệp vụ, không migration; spec ngắn trong issue, không có system-design riêng vì không có quyết định kiến trúc mới). State: không bắt buộc ở làn vừa, ghi thẳng vào issue.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| spec ngắn trong issue | done | – | 2026-10-23 | FR-001..004, NFR-001..005, Q-03 đã chốt |
| spec-review | PASS | 1 | 2026-10-23 | kiểm A, B, D, D2, G (làn vừa); 2 FIX: thêm NFR-004 (không log secret/body) và NFR-005 (Jaeger bind 127.0.0.1, pin image); 0 CONFIRM mới |
| system-design | n/a (làn vừa) | | | không có quyết định kiến trúc mới (OTel, health đã quy định ở `observability.md`) |
| plan-init | done | – | 2026-10-23 | `document/plans/TB-001-servicedefaults-and-rename.md` (draft), 6 bước, 2 câu hỏi mở P-03, P-04 |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | tb001:lane-scope | Làn và phạm vi | Làn vừa, phạm vi trọn gói (đổi tên + ServiceDefaults + Jaeger) | 0 | 2026-10-23 |
| 2 | tb001:telemetry-export | Telemetry gửi đi đâu | OTLP trực tiếp tới Jaeger, chưa có Collector | 0 | 2026-10-23 |
| 3 | tb001:ready-content | `/health/ready` kiểm gì | Chỉ kiểm "ứng dụng đã sẵn sàng", chưa kiểm DB | 0 | 2026-10-23 |
| 4 | tb001:test-framework | Test framework | xUnit + Reqnroll (BDD) ngay (Reqnroll T2: ghi phê duyệt vào `config/dependencies.yaml`, bọc sau interface) | 0 | 2026-10-23 |
| 5 | tb001:rename-scope | Phạm vi đổi tên | Đổi mọi tên theo service, gồm cả xoá controller mẫu `WeatherForecast`; giữ `teamboard.slnx` và cổng 6060 | 0 | 2026-10-23 |
| 6 | tb001:trace-request | Request kiểm trace sau khi xoá controller mẫu | A: không lọc health khỏi trace, dùng `GET /health/live` | 1 | 2026-10-23 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

-
