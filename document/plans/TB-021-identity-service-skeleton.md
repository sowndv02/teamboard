---
title: Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền
issue: TB-021
status: approved          # draft | in-review | approved | done
owner: sondv
updated: 2026-10-25
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md
services: [identity-service, shared/ServiceDefaults, shared/Http]
---

# Plan: TB-021 — Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền

Lát 1a trong [`identity-auth-slices.md`](identity-auth-slices.md) (`approved`, Confirm log #1–#4). Làn lớn: spec `confirmed`, design `confirmed` cho I1–I4, I6 (PASS vòng 6). Ảnh chụp trạng thái branch: `feature/TB-021-identity-service-skeleton` tạo từ `origin/main` `e79560d` (đã gồm TB-001, TB-010, TB-004), working tree sạch. Đã có: `src/board-service` (mẫu Program/Dockerfile/EF Core), `src/gateway`, `src/shared/ServiceDefaults` (`AddServiceDefaults`, `MapDefaultEndpoints`, check gắn tag `ready`), `src/shared/Http` (`ProblemFactory`), compose, hai project test mẫu (xUnit + Reqnroll, nhóm `docker`). Chưa có `src/identity-service`, PostgreSQL trong compose.

## 1. Phạm vi của lát

**Làm:**
- `src/identity-service`: host ASP.NET Core .NET 10, tổ chức Vertical Slice (thư mục `Features/<Slice>/`, design D0.4); gọi `AddServiceDefaults("identity-service")` và `MapDefaultEndpoints()` (health, OTel).
- `/health/live` không kiểm dependency; `/health/ready` kiểm PostgreSQL (check gắn tag `ready`); kiểm khoá ký thêm ở lát 4 (design 6.3, 7).
- PostgreSQL riêng cho identity: database và user riêng (D0.2); EF Core + Npgsql (D8), `IdentityDbContext` chưa có thực thể, migration nền; chuỗi kết nối qua biến môi trường.
- Lỗi chưa xử lý trả `500 problem+json` (`INTERNAL_ERROR`) bằng `Teamboard.Shared.Http`, không lộ chi tiết nội bộ (NFR-003).
- Project test `src/identity-service.Tests`; file `.feature` ở `document/spec/features/identity-service/`.
- Dockerfile, compose (identity-service + PostgreSQL), `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml`, `*.example` cho cấu hình có secret.

**Không làm (trong lát này):** bảng `user` và mọi bảng nghiệp vụ, đăng ký, băm, benchmark (lát 1b); rate limit (lát 3); khoá ký, Secrets Manager, JWT (lát 4+); route ở gateway (lát 19); middleware `X-Correlation-Id` (gateway lát 2 sẽ đưa vào `src/shared`, Confirm log #5); Terraform/AWS; ADR (chưa có quyết định mới trong lát này).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| NFR-001 `/health/live`, `/health/ready`, OTel | 6.3, mục 9 | B1, B3 | @S01, @S02, @S03, @S08 | Host test (live không cần DB; telemetry mang `service.name` đúng); integration với PostgreSQL thật (ready lên/xuống) |
| NFR-003 lỗi `problem+json`, không lộ nội bộ | 6.3, `backend-engineering.md` mục 1 | B2 | @S06 | Host test với endpoint ném lỗi chỉ có trong test host |
| NFR-004 `traceparent` (một phần; `X-Correlation-Id` để sau, Confirm log #5) | mục 9 | B1 | – | Kiểm `ServiceDefaults` đã gắn trace; không thêm test riêng |
| NFR-009 khôi phục DB dev bằng migration | 10 | B3 | @S04, @S07 | Áp migration lên DB trống, kiểm bảng lịch sử migration |
| Compose chạy identity-service + DB (tiêu chí issue) | 10, D0.2 | B4 | @S05 | Test nhóm `docker` gọi `docker compose` qua Process |
| Database + user riêng (D0.2) | D0.2 | B3, B4 | @S05, @S09 | Script khởi tạo tạo database và user `identity` riêng; kết nối bằng user đó (ready) và user không có quyền quản trị |
| Gate xanh (tiêu chí issue) | – | B1–B4 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất, tag `@TB-021 @identity-service`):
- @S01 `GET /health/live` trả 200 kể cả khi DB không chạy.
- @S02 `GET /health/ready` trả 200 khi PostgreSQL chạy và migration đã áp.
- @S03 `GET /health/ready` trả 503 khi PostgreSQL không kết nối được; body không chứa chuỗi kết nối hay mật khẩu.
- @S04 Migration nền áp lên PostgreSQL trống thành công và bảng lịch sử migration có bản ghi nền; áp lần hai không đổi gì.
- @S05 (`@docker`) Compose chạy identity-service và PostgreSQL: `/health/ready` ở cổng host trả 200 trong 30 giây kể từ `up -d` (image đã build).
- @S07 Khi cờ tự áp migration tắt thì service vẫn khởi động (`/health/live` 200) và không tạo lịch sử migration (từ B3: "có test cờ tắt thì không áp"; `test-init` thêm ID, `test-review` thêm kiểm live để test từng đỏ, Confirm log #10 của issue).
- @S08 Telemetry của service mang `service.name` = `identity-service` (NFR-001; thêm ở `test-review`, Confirm log #9 của issue).
- @S09 (`@docker`) Người dùng DB của identity không là superuser, không có quyền tạo role hay database (D0.2; thêm ở `test-review`, Confirm log #11 của issue).
- @S06 Lỗi chưa xử lý trả `500 problem+json` có `code` `INTERNAL_ERROR`, `traceId`; body không có stack trace, tên máy, đường dẫn nội bộ.

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

Quy ước: test thực thi sinh theo từng bước, đỏ từng bước. Mỗi bước một commit nhỏ (dưới khoảng 400 dòng diff, không tính file sinh ra).

### B1. Khung project và project test
- **Kịch bản/test sẽ viết:** không có hành vi mới; một test rỗng dựng `WebApplicationFactory<Program>` xanh, chứng minh project thấy `Program` (không áp dụng "đỏ").
- **File/module dự kiến chạm:** `src/identity-service/identity-service.csproj` (net10.0, `RootNamespace` `IdentityService`, tham chiếu `Teamboard.Shared.ServiceDefaults` và `Teamboard.Shared.Http`), `src/identity-service/Program.cs` tối thiểu (`AddServiceDefaults`, `MapDefaultEndpoints`, `public partial class Program`), `src/identity-service.Tests/` (xUnit, Reqnroll.xUnit, `Microsoft.AspNetCore.Mvc.Testing`, cùng cách cấu hình với `gateway.Tests`), `src/teamboard.slnx`.
- **Hợp đồng thay đổi:** không có.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** `dotnet build src/teamboard.slnx -warnaserror` và `dotnet test src/teamboard.slnx --filter "Category!=docker"` xanh.
- **Phụ thuộc:** không.

### B2. Health live và lỗi `500 problem+json` (@S01, @S06)
- **Kịch bản/test sẽ viết (đỏ trước):** `health.feature` @S01 chạy với host không có DB; `errors.feature` @S06 dùng endpoint ném lỗi chỉ đăng ký trong test host, kiểm status, `code`, `traceId`, không có stack trace.
- **File/module dự kiến chạm:** `src/identity-service/Program.cs` (exception handler dùng `ProblemFactory` và mã `INTERNAL_ERROR` từ `SharedErrors`), `src/identity-service.Tests/` (steps, support), `document/spec/features/identity-service/{health,errors}.feature`.
- **Hợp đồng thay đổi:** định dạng lỗi theo `api-endpoints.md` mục 5.
- **Migration / cấu hình:** không có.
- **Tài liệu cần cập nhật:** không.
- **Kiểm chứng:** `dotnet test src/identity-service.Tests` xanh; response lỗi không chứa `StackTrace`, tên máy.
- **Phụ thuộc:** B1.

### B3. PostgreSQL, EF Core, migration nền và ready kiểm DB (@S02, @S03, @S04)
- **Kịch bản/test sẽ viết (đỏ trước):** `database.feature` @S02 (ready 200 sau migration), @S03 (ready 503 khi DB dừng, không lộ chuỗi kết nối), @S04 (migration trên DB trống, chạy hai lần). PostgreSQL cho test dựng bằng `Testcontainers.PostgreSql` (Confirm log #2); các kịch bản cần Docker daemon nên gắn nhóm `docker` (loại khỏi gate nhanh, chạy bằng `--filter "Category=docker"`).
- **File/module dự kiến chạm:** `src/identity-service/Persistence/IdentityDbContext.cs`, `Persistence/Migrations/` (migration nền), `Program.cs` (đăng ký DbContext, check DB gắn tag `ReadyTag`), `src/identity-service/appsettings.json` (không chứa mật khẩu), `identity-service.csproj` (gói `Npgsql.EntityFrameworkCore.PostgreSQL` và `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, Confirm log #1; kiểm giấy phép và tương thích .NET 10 ở nguồn chính thức rồi pin chính xác), `src/identity-service.Tests/` (thêm `Testcontainers.PostgreSql`, Confirm log #2).
- **Hợp đồng thay đổi:** `/health/ready` phụ thuộc DB (design 6.3).
- **Migration / cấu hình:** migration nền không tạo bảng nghiệp vụ; cấu hình chuỗi kết nối qua `ConnectionStrings__IdentityDb` (biến môi trường); migration chỉ tự áp khi cờ `Database:MigrateOnStartup` bật (mặc định tắt; compose bật; Confirm log #3), có test cờ tắt thì không áp.
- **Tài liệu cần cập nhật:** `config/dependencies.yaml` (Npgsql EF provider, check EF; `Testcontainers` chuyển từ `watch` sang `approved` theo Confirm log #2).
- **Kiểm chứng:** `dotnet test src/identity-service.Tests --filter "Category=docker"` xanh cho @S02–@S04 (cần Docker Desktop, Testcontainers dùng cổng ngẫu nhiên); `dotnet test src/teamboard.slnx --filter "Category!=docker"` vẫn xanh; `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` sạch.
- **Phụ thuộc:** B2.

### B4. Dockerfile, compose, cấu hình và tài liệu (@S05)
- **Kịch bản/test sẽ viết (đỏ trước):** `compose.feature` @S05 (`@docker`) gọi `docker compose` qua Process (theo `gateway.Tests/ComposeSteps.cs`), kiểm ready trong ngưỡng; sửa test hygiene của TB-001 quét compose mới nếu cần.
- **File/module dự kiến chạm:** `src/identity-service/Dockerfile` (theo mẫu `gateway/Dockerfile`: `COPY` csproj của cả `shared/ServiceDefaults` và `shared/Http` trước `restore`), `src/docker-compose.yml` (service `identity-service` host 6061 → container 7070; service `postgres` dùng chung ở compose chính, không mở cổng ra host, volume, healthcheck; `identity-service` `depends_on` postgres healthy; `Database__MigrateOnStartup: "true"`; Confirm log #3, #4, #6), script khởi tạo database và user `identity` riêng, không quyền quản trị (D0.2, chạy khi volume trống; task-service dùng lại instance và thêm DB riêng sau), `src/.env.example` (tên biến mật khẩu, giá trị mẫu không phải mật khẩu thật; `.gitignore` đã cho phép `!.env.example`; compose dùng `${POSTGRES_PASSWORD:?}` và `${IDENTITY_DB_PASSWORD:?}`, Confirm log #8), `src/docker-compose.dcproj` (liệt kê file mới nếu thêm), `config/services.yaml` (`status: existing`, `path`, `local_port`, `container_port`), `PROJECT.md` (trạng thái hiện tại), `COMMANDS.md` (cổng, lệnh), `document/spec/features/identity-service/compose.feature`.
- **Hợp đồng thay đổi:** cổng host 6061 của identity-service (Confirm log #4); không route ở gateway.
- **Migration / cấu hình:** biến môi trường `ConnectionStrings__IdentityDb`, mật khẩu DB lấy từ `src/.env` local, không có giá trị mặc định trong repo (LL-001). Người dùng tạo `src/.env` từ `src/.env.example`; gate và test docker truyền `--env-file src/.env.example`.
- **Tài liệu cần cập nhật:** `config/services.yaml` (`local_port: 6061`), `PROJECT.md`, `COMMANDS.md` (cổng 6061, bước tạo `.env`, `--env-file` ở lệnh gate compose), `config/tool-versions.yaml` (phiên bản image PostgreSQL: người dùng chọn major khi làm B4, ghim theo `dependency-pinning.md`).
- **Kiểm chứng:** `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`; test `docker` (`dotnet test --filter "Category=docker"`) xanh; `git diff | grep -n -i -E "password|secret|connectionstring"` chỉ ra tên biến và file `*.example`, không có giá trị thật.
- **Phụ thuộc:** B3.

## 4. Chiến lược test
| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit/host | Health live, lỗi 500, cấu hình | `WebApplicationFactory<Program>` | Không DB; endpoint ném lỗi chỉ có trong test host |
| Integration (cần Docker) | Ready, migration nền | PostgreSQL thật bằng Testcontainers (nhóm `docker`) | Không mock DB; DB dừng thật cho @S03 |
| Docker | Compose chạy cả hai container | `docker compose` qua Process, nhóm `docker`, cổng host trống | Không chạy trong gate nhanh; chạy chung nhóm `docker` với integration DB |
| Contract/e2e | Chưa có (chưa có API nghiệp vụ) | – | Bắt đầu từ lát 1b |

Công cụ test/BDD (`config/tool-versions.yaml`): xUnit 2.9.3 và Reqnroll.xUnit 3.3.4 (đã duyệt, TB-001). `Testcontainers.PostgreSql`: đã duyệt (Confirm log #2).

## 5. Rủi ro và rollback
| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Gói Npgsql EF hoặc check EF chưa qua kiểm theo ADR 0007 (`verified: false`) | Phải đổi gói giữa chừng | Confirm log #1: kiểm nguồn chính thức và giấy phép ở B1/B3 trước khi pin | Bỏ gói, quay về B2 |
| Chuỗi kết nối hoặc mật khẩu DB lọt vào repo (LL-001) | Lộ secret, phải xoay | `.env` đã ignore, chỉ `*.example`, quét diff bằng lệnh ở COMMANDS.md mục 2 | Xoay mật khẩu, gỡ khỏi lịch sử theo AGENTS.md |
| Xung đột cổng host với `board-service` (6060) và `gateway` (8080) | Compose không lên | Cổng 6061 đã chốt (Confirm log #4), test docker yêu cầu cổng trống | Đổi cổng trong compose |
| Tự áp migration khi khởi động ghi nhầm DB khác | Hỏng dữ liệu | Cờ `MigrateOnStartup` mặc định tắt (Confirm log #3); không áp lên DB không phải local (COMMANDS.md mục 5) | Xoá volume DB dev, áp lại migration (NFR-009) |
| ready 200 giả (check DB không thật sự kết nối) | Orchestrator coi service sống khi DB chết | @S03 dùng DB dừng thật | Sửa check |

Ảnh hưởng consumer (service khác): không có (service mới, chưa được route ở gateway).

Bảo mật: không secret trong repo; `/health/ready` và lỗi `500` không lộ chuỗi kết nối, stack trace (test @S03, @S06); user DB riêng của identity, không dùng user quản trị cho ứng dụng (D0.2).

Chi phí: chỉ chạy local bằng compose (chính sách local-first, design mục 10); không tài nguyên AWS; không có bước `destroy`.

Bài học đã dùng: LL-001 (secret trong `.env`).

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, `quality-gate.md` (vùng .NET và Docker): `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test src/teamboard.slnx --filter "Category!=docker"`, `dotnet test --filter "Category=docker"`, `dotnet format --verify-no-changes`, `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q`, `dotnet list package --vulnerable --include-transitive`, quét secret thô.
- Tiêu chí hoàn thành của issue: xem TB-021.
- Tài liệu/config cập nhật: `config/services.yaml`, `config/dependencies.yaml`, `PROJECT.md`, `COMMANDS.md`, `document/plans/identity-auth-slices.md` (trạng thái, ước lượng vs thực tế).

## 7. Câu hỏi mở và giả định
Không còn câu hỏi mở: Q-1..Q-5 của bản nháp và hai điểm phát sinh khi review (vị trí PostgreSQL, mật khẩu DB) đã được người dùng chốt ở Confirm log #1–#8.

Quy ước thư mục Vertical Slice (Confirm log #7): `Features/<Slice>/` chứa endpoint, request/response, handler, truy vấn của một lát; mã dùng chung (`Passwords`, `Tokens`, `Keys`, khoá tạm) nằm ở `Security/` (shared kernel, design D0.4). Thư mục chỉ được tạo khi có tệp đầu tiên (git không giữ thư mục rỗng): `Security/` bắt đầu từ lát 1b.

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | `plan-review:TB-021:ef-packages` | Thêm `Npgsql.EntityFrameworkCore.PostgreSQL` và `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (`verified: false`) | Thêm cả hai; kiểm giấy phép và tương thích .NET 10 ở nguồn chính thức rồi pin | 1 | 2026-10-25 |
| 2 | `plan-review:TB-021:test-database` | PostgreSQL cho test tích hợp dựng bằng gì | `Testcontainers.PostgreSql` (duyệt T2); test nhóm `docker` | 1 | 2026-10-25 |
| 3 | `plan-review:TB-021:migration-policy` | Áp migration thế nào | Cờ `Database:MigrateOnStartup`, mặc định tắt, compose bật | 1 | 2026-10-25 |
| 4 | `plan-review:TB-021:ports-threshold` | Cổng host, cổng DB, ngưỡng `ready` | Identity 6061 (container 7070); DB không mở cổng ra host; ready ≤ 30 giây | 1 | 2026-10-25 |
| 5 | `plan-review:TB-021:correlation-id` | `X-Correlation-Id` khi middleware dùng chung chưa có | Để sau (gateway lát 2); lát này chỉ `traceparent`, `correlationId` hiện bằng `traceId` | 1 | 2026-10-25 |
| 6 | `plan-review:TB-021:postgres-placement` | Vị trí service PostgreSQL | Compose chính, service `postgres` dùng chung, DB và user riêng cho identity (D0.2) | 1 | 2026-10-25 |
| 7 | `plan-review:TB-021:slice-folder-convention` | Quy ước thư mục Vertical Slice | `Features/<Slice>/` + `Security/` | 1 | 2026-10-25 |
| 8 | `plan-review:TB-021:db-password-handling` | Mật khẩu DB trong compose chính và gate `config -q` | Biến bắt buộc `${VAR:?}`; `src/.env` local từ `src/.env.example`; gate và test docker truyền `--env-file src/.env.example` | 2 | 2026-10-25 |

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-25 | Toàn bộ checklist A0–H (làn lớn); đối chiếu code `gateway`, `board-service`, `ServiceDefaults`, `Shared.Http`, compose | 7 (#1–#7, đã hỏi 2 lô) | 3 (đã sửa, xem Fix log) | NEEDS_CONFIRM → đã sửa plan |
| 2 | 2026-10-25 | Delta: B3, B4, mục 4–7; kiểm dây chuyền (nhóm test `docker`, `--env-file` ở gate và COMMANDS.md, `dependencies.yaml`, tái phát FIX cũ) | 1 (#8, đã chốt) | 0 | PASS |

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | `plan-init` cho lát 1a của identity-auth |
| 2026-10-25 | Ghi Confirm log #1–#8, sửa B3, B4, mục 4–7, status `approved` | `plan-review` vòng 1–2 PASS |

## Fix log
| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|---|---|
| 1 | `plan-review:TB-021:dockerfile-copy-shared-http` | FIX | B4, file dự kiến | Dockerfile mẫu chỉ ghi `board-service`, thiếu `COPY` của `shared/Http` (identity dùng `Teamboard.Shared.Http`) nên restore sẽ lỗi | Đổi sang mẫu `gateway/Dockerfile` và ghi cần `COPY` cả hai csproj shared | `src/gateway/Dockerfile` có hai dòng `COPY shared/...` |
| 1 | `plan-review:TB-021:env-example-convention` | FIX | B4, file dự kiến | Plan ghi `src/.env.example` trong khi repo có `src/.env.develop.example` và `.env.stg.example` | Thêm `docker-compose.dcproj` liệt kê file mới; sau Confirm log #6 và #8 chọn `src/.env.example` vì compose chính tự đọc `src/.env` | `ls src/.env*`, `src/docker-compose.dcproj`, `.gitignore` dòng 369–373 |
| 1 | `plan-review:TB-021:q5-correlation-fact` | FIX | Câu hỏi Q-5 | Thiếu dữ kiện: `ProblemFactory` đang gán `correlationId = traceId` | Ghi dữ kiện vào câu hỏi trước khi hỏi | `src/shared/Http/ProblemFactory.cs` dòng 15 và 27 |
| 2 | `plan-review:TB-021:docker-category-from-testcontainers` | CONFIRM #2 | B3, mục 4, @S02–@S04 | Testcontainers cần Docker daemon, nên kịch bản DB không thể nằm trong gate nhanh `Category!=docker` | Gắn nhóm `docker` cho @S02–@S04, sửa kiểm chứng B3 và bảng test; ghi rõ gate nhanh không còn kiểm DB | Mục 3 B3, mục 4 |
| 2 | `plan-review:TB-021:gate-env-file` | CONFIRM #8 | Mục 6, B4, COMMANDS.md | Gate `docker compose config -q` sẽ lỗi vì biến bắt buộc chưa có `.env` | Thêm `--env-file src/.env.example` vào lệnh gate và kiểm chứng B4; thêm `COMMANDS.md` vào tài liệu cần cập nhật | Mục 6, B4 |
| 2 | `plan-review:TB-021:s05-threshold` | CONFIRM #4 | Kịch bản @S05 | Ngưỡng `ready` còn tham chiếu câu hỏi mở | Ghi 30 giây | Mục 2 |
