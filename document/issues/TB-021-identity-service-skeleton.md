---
key: TB-021
title: Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền
status: todo
phase: 1
branch: feature/TB-021-identity-service-skeleton
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: null           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-021 — Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền

## Bối cảnh
Lát 1a của `document/plans/identity-auth-slices.md` (`approved`; spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md` `confirmed` cho I1–I4, I6; issue tài liệu TB-004, PR 12). Xương sống để các lát sau (đăng ký, đăng nhập, khoá tạm, token) cắm vào. Gateway đã có lát 1 (TB-010), identity chưa được route; chạy và test trực tiếp ở local (design D0.5, Q-G).

**Phụ thuộc:** TB-001 (`src/shared/ServiceDefaults`) và TB-010 (`src/shared/Http`) đã merge. Branch tạo từ `main` ngày 2026-10-25.

## Phạm vi
- Làm:
  - Dự án `src/identity-service` (ASP.NET Core .NET 10, Vertical Slice theo design D0.4) và project test.
  - `/health/live`, `/health/ready` (ready kiểm PostgreSQL; kiểm khoá ký thêm ở lát 4) và OpenTelemetry qua `ServiceDefaults` (NFR-001).
  - PostgreSQL riêng cho identity (database + user riêng, D0.2), EF Core + Npgsql, migration nền, cấu hình qua biến môi trường, không secret trong repo.
  - Lỗi chưa xử lý trả `500 problem+json` không lộ chi tiết nội bộ (NFR-003, dùng `Teamboard.Shared.Http`).
  - Dockerfile và compose (identity-service + PostgreSQL), `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`.
- Không làm: đăng ký, băm mật khẩu, bảng `user` và các bảng nghiệp vụ (lát 1b trở đi), khoá ký và Secrets Manager (lát 4), rate limit (lát 3), route ở gateway (lát 19), `X-Correlation-Id` (chờ gateway lát 2).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `GET /health/live` trả 200; `GET /health/ready` trả 200 khi PostgreSQL chạy và lỗi (không phải 200) khi DB dừng.
- [ ] Migration nền áp dụng được trên PostgreSQL trống và tạo bảng lịch sử migration.
- [ ] Compose chạy identity-service cùng PostgreSQL, `ready` đạt trong thời gian quy định ở plan.
- [ ] Lỗi chưa xử lý trả `500 problem+json` có `code`, `traceId`, không có stack trace.
- [ ] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` xanh.
- [ ] `config/services.yaml`, compose, `PROJECT.md` khớp; không secret trong repo (có `*.example`).

## Kế hoạch
Plan chi tiết: `document/plans/TB-021-identity-service-skeleton.md` (`approved`, plan-review PASS vòng 2).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ hàng 1a của bảng lát; nhánh từ `main` `e79560d`.

## Kịch bản và TDD log

Lệnh: `dotnet test src/identity-service.Tests --no-build` (bản đầu 7 test, sau `test-review` 9 test, 2026-10-25, Docker 29.5.3, image `postgres:17-alpine`). Khung biên dịch: `src/identity-service/Program.cs` rỗng (không logic), project test và `src/teamboard.slnx`.

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 live 200 khi DB không chạy (`health.feature`) | `Live trả 200 khi cơ sở dữ liệu không chạy` | Đỏ: mong 200, nhận 404 (chưa có endpoint) | – |
| @S02 ready 200 khi DB chạy (`database.feature`) | `Ready trả 200 khi cơ sở dữ liệu chạy` | Đỏ: mong 200, nhận 404 | – |
| @S03 ready 503 khi DB dừng, không lộ chuỗi kết nối | `Ready trả 503 khi cơ sở dữ liệu đã dừng` | Đỏ: mong 503, nhận 404 | – |
| @S04 migration nền áp trên DB trống, áp hai lần không đổi | `Migration nền áp lên cơ sở dữ liệu trống và áp lần hai không đổi gì` | Đỏ: "Bảng lịch sử migration chưa được tạo" | – |
| @S05 compose chạy identity-service + PostgreSQL, ready ≤ 30 giây (`compose.feature`) | `Compose chạy identity-service cùng PostgreSQL và ready đạt trong 30 giây` | Đỏ: `docker compose build` báo `no such service: identity-service` | – |
| @S06 lỗi chưa xử lý trả `500 problem+json` (`errors.feature`) | `Lỗi chưa xử lý trả lỗi chuẩn không lộ chi tiết nội bộ` | Đỏ: yêu cầu ném lỗi chưa xử lý thay vì trả 500 (chưa có bộ xử lý lỗi) | – |
| @S07 cờ tự áp tắt thì service vẫn sống và không áp migration (`database.feature`) | `Không tự áp migration khi cờ tự áp tắt` | Bản đầu xanh ngay (không từng đỏ); sau `test-review` (Confirm log 10) thêm kiểm `/health/live` 200 nên đỏ: mong 200, nhận 404 | – |
| @S08 telemetry mang `service.name` đúng (`health.feature`) | `Telemetry của identity-service mang đúng tên service` | Đỏ: "identity-service chưa đăng ký OpenTelemetry" (thêm ở `test-review`) | – |
| @S09 user DB của identity không có quyền quản trị (`compose.feature`) | `Người dùng cơ sở dữ liệu của identity không có quyền quản trị` | Đỏ: `no such service: identity-service` (thêm ở `test-review`) | – |

Gate nhanh `Category!=docker`: 2 test (@S01, @S06) đỏ; 5 test còn lại thuộc nhóm `docker` (Testcontainers cần Docker daemon). `dotnet build src/teamboard.slnx -warnaserror` xanh; `dotnet format --verify-no-changes` không báo lỗi.

Điểm lệch so với plan, để `test-review` xem:
- Thêm @S07 (có trong chữ của B3, chưa có ID); đã ghi vào plan mục 2.
- `src/.env.example` (mẫu mật khẩu, không phải mật khẩu thật) tạo ở `test-init` thay vì B4, vì @S05 truyền `--env-file src/.env.example`; thiếu file này test đỏ vì lý do sai.
- Chọn image `postgres:17-alpine` (người dùng chọn 2026-10-25); ghi `config/tool-versions.yaml`; `Testcontainers.PostgreSql 4.16.0` và `Npgsql 10.0.3` ghi `config/dependencies.yaml` (giấy phép MIT và PostgreSQL, đối chiếu nuspec).
- Test @S06 chạy ở môi trường `Production` (ở Development ASP.NET thêm trang lỗi có dấu vết ngăn xếp). Điểm cuối ném lỗi gắn bằng `IStartupFilter` sau pipeline của app.

## Test review rounds

Chuẩn: `.agents/rules/test-review.md`, skill `test-review`. Làn lớn: đủ checklist A–F.

### Vòng 1 (2026-10-25)

**Tóm tắt:** issue TB-021; 4 file `.feature` trong `document/spec/features/identity-service/` (`health`, `database`, `errors`, `compose`); nhánh `feature/TB-021-identity-service-skeleton`, thay đổi chưa commit lúc review.

**Coverage:** đối chiếu spec NFR-001, NFR-003, NFR-004, NFR-009, BR-007; design D0.2, 6.3, 7; plan bảng truy vết và Confirm log #1–#8; chạy lại toàn bộ test.

| Fingerprint | Mức | Vị trí | Vấn đề | Xử lý |
|---|---|---|---|---|
| `test:identity-service:s06-correlation-id-unchecked` | P2 | `errors.feature` @S06 | Plan (Confirm log 5) yêu cầu @S06 kiểm trường `correlationId` có mặt, test chưa kiểm | FIX: thêm bước "lỗi có mã tương quan"; đỏ vì chưa có handler |
| `test:identity-service:compose-down-keeps-volume` | P2 | `Support/DockerCli.cs` `ComposeDown` | Bản sao từ gateway không dùng `-v`; volume PostgreSQL còn sót làm lệch mật khẩu và dữ liệu ở lần chạy sau (flaky ẩn) | FIX: `down -v --remove-orphans` (tên dự án compose riêng nên an toàn) |
| `test:identity-service:otel-uncovered` | P2 | NFR-001 | Plan không có kịch bản OpenTelemetry của identity (board có); sai tên service hay quên `AddServiceDefaults` không ai bắt | CONFIRM 9: thêm @S08 |
| `test:identity-service:s07-never-red` | P2 | `database.feature` @S07 | Kịch bản xanh ngay, trái quy tắc "test từng đỏ đúng lý do" | CONFIRM 10: thêm kiểm `/health/live` 200 để đỏ |
| `test:identity-service:db-isolation-unproven` | P2 | D0.2, plan dòng "Database + user riêng" | @S05 chỉ chứng minh kết nối được, không chứng minh user khác quản trị | CONFIRM 11: thêm @S09 |

**Confirm log (test-review):**

| # | Điểm cần chốt | Quyết định của người dùng | Ngày |
|---|---|---|---|
| 9 | Thêm kịch bản OpenTelemetry cho identity | Thêm @S08 kiểm `service.name` | 2026-10-25 |
| 10 | @S07 xanh ngay | Thêm kiểm `/health/live` 200 để đỏ | 2026-10-25 |
| 11 | Chứng minh user DB không phải quản trị | Thêm @S09 (không superuser, không tạo role hay database) | 2026-10-25 |

**Fix log:**
1. Thêm `And lỗi có mã tương quan` ở `errors.feature` và step `ThenHasCorrelationId`.
2. `ComposeDown` dùng `down -v --remove-orphans`.
3. @S07 thêm bước kiểm live 200 (`ThenStillAlive`).
4. Thêm @S08 (`health.feature`) và @S09 (`compose.feature`), step `ThenTelemetryServiceName`, `ThenDatabaseUserIsNotAdmin`, `DockerCli.ComposeExec`.
5. Thêm gói test `OpenTelemetry.Extensions.Hosting 1.20.1` (cùng phiên bản `ServiceDefaults`).
6. Plan mục 2 và B4 đồng bộ (@S07 đổi nghĩa, @S08, @S09, tên database và user `identity`).

**Bảng reconcile (kịch bản ↔ test):**

| Kịch bản | Test thực thi | Kết quả chạy lại |
|---|---|---|
| @S01 | `Live trả 200 khi cơ sở dữ liệu không chạy` | ❌ đỏ đúng lý do (404) |
| @S02 | `Ready trả 200 khi cơ sở dữ liệu chạy` | ❌ đỏ đúng lý do (404) |
| @S03 | `Ready trả 503 khi cơ sở dữ liệu đã dừng` | ❌ đỏ đúng lý do (404) |
| @S04 | `Migration nền áp lên cơ sở dữ liệu trống và áp lần hai không đổi gì` | ❌ đỏ đúng lý do (bảng chưa tạo) |
| @S05 | `Compose chạy identity-service cùng PostgreSQL và ready đạt trong 30 giây` | ❌ đỏ đúng lý do (`no such service`) |
| @S06 | `Lỗi chưa xử lý trả lỗi chuẩn không lộ chi tiết nội bộ` | ❌ đỏ đúng lý do (lỗi chưa xử lý) |
| @S07 | `Không tự áp migration khi cờ tự áp tắt` | ❌ đỏ đúng lý do (404) |
| @S08 | `Telemetry của identity-service mang đúng tên service` | ❌ đỏ đúng lý do (chưa đăng ký OpenTelemetry) |
| @S09 | `Người dùng cơ sở dữ liệu của identity không có quyền quản trị` | ❌ đỏ đúng lý do (`no such service`) |

Lệnh: `dotnet test src/identity-service.Tests --no-build` cho 9 test, 9 đỏ, không lỗi biên dịch hay môi trường; `dotnet build src/teamboard.slnx -warnaserror` xanh; `dotnet format --verify-no-changes` không báo lỗi.

**Verdict:** `Sẵn sàng code` (0 CONFIRM mở, 0 FIX mở, không P3).

**Giới hạn đã biết:**
- Test nhóm `docker` bỏ qua (skip) khi máy không có Docker daemon; gate nhanh `Category!=docker` chỉ gồm @S01, @S06, @S08 (đều đỏ) và không kiểm ready hay migration.
- @S09 chỉ kiểm user không có quyền quản trị; chưa kiểm user bị chặn vào database khác (cần `REVOKE CONNECT`, ngoài plan).
- Không có unit test riêng ở lát này (logic mỏng, phần lớn là cấu hình); không có contract test vì chưa có API nghiệp vụ.
- @S08 chỉ xanh khi B1 gọi `AddServiceDefaults("identity-service")` thay cho `Program.cs` rỗng.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | Plan `draft`, có câu hỏi mở chờ `plan-review` |
| plan-review | done | 2 | 2026-10-25 | PASS; plan `approved`, Confirm log #1–#8 |
| test-init | done | – | 2026-10-25 | Đỏ: 6/7 test (bản đầu), xanh 1; xem "Kịch bản và TDD log" |
| test-review | done | 1 | 2026-10-25 | PASS (Sẵn sàng code); 9 test đỏ đúng lý do; xem "Test review rounds" |
| coding-start | pending | – | | |
