---
key: TB-021
title: Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền
status: review
phase: 1
branch: feature/TB-021-identity-service-skeleton
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25          # ngày chuyển in-progress
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
- [x] `GET /health/live` trả 200; `GET /health/ready` trả 200 khi PostgreSQL chạy và lỗi (không phải 200) khi DB dừng.
- [x] Migration nền áp dụng được trên PostgreSQL trống và tạo bảng lịch sử migration.
- [x] Compose chạy identity-service cùng PostgreSQL, `ready` đạt trong thời gian quy định ở plan.
- [x] Lỗi chưa xử lý trả `500 problem+json` có `code`, `traceId`, không có stack trace.
- [x] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` xanh.
- [x] `config/services.yaml`, compose, `PROJECT.md` khớp; không secret trong repo (có `*.example`).

## Kế hoạch
Plan chi tiết: `document/plans/TB-021-identity-service-skeleton.md` (`approved`, plan-review PASS vòng 2).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ hàng 1a của bảng lát; nhánh từ `main` `e79560d`.
- 2026-10-25: mở PR [sowndv02/teamboard#14](https://github.com/sowndv02/teamboard/pull/14); issue chuyển `review`.

## Kịch bản và TDD log

Lệnh: `dotnet test src/identity-service.Tests --no-build` (bản đầu 7 test, sau `test-review` 9 test, 2026-10-25, Docker 29.5.3, image `postgres:17-alpine`). Khung biên dịch: `src/identity-service/Program.cs` rỗng (không logic), project test và `src/teamboard.slnx`.

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 live 200 khi DB không chạy (`health.feature`) | `Live trả 200 khi cơ sở dữ liệu không chạy` | Đỏ: mong 200, nhận 404 (chưa có endpoint) | Xanh (coding-start) |
| @S02 ready 200 khi DB chạy (`database.feature`) | `Ready trả 200 khi cơ sở dữ liệu chạy` | Đỏ: mong 200, nhận 404 | Xanh (coding-start) |
| @S03 ready 503 khi DB dừng, không lộ chuỗi kết nối | `Ready trả 503 khi cơ sở dữ liệu đã dừng` | Đỏ: mong 503, nhận 404 | Xanh (coding-start) |
| @S04 migration nền áp trên DB trống, áp hai lần không đổi | `Migration nền áp lên cơ sở dữ liệu trống và áp lần hai không đổi gì` | Đỏ: "Bảng lịch sử migration chưa được tạo" | Xanh (coding-start) |
| @S05 compose chạy identity-service + PostgreSQL, ready ≤ 30 giây (`compose.feature`) | `Compose chạy identity-service cùng PostgreSQL và ready đạt trong 30 giây` | Đỏ: `docker compose build` báo `no such service: identity-service` | Xanh (coding-start) |
| @S06 lỗi chưa xử lý trả `500 problem+json` (`errors.feature`) | `Lỗi chưa xử lý trả lỗi chuẩn không lộ chi tiết nội bộ` | Đỏ: yêu cầu ném lỗi chưa xử lý thay vì trả 500 (chưa có bộ xử lý lỗi) | Xanh (coding-start) |
| @S07 cờ tự áp tắt thì service vẫn sống và không áp migration (`database.feature`) | `Không tự áp migration khi cờ tự áp tắt` | Bản đầu xanh ngay (không từng đỏ); sau `test-review` (Confirm log 10) thêm kiểm `/health/live` 200 nên đỏ: mong 200, nhận 404 | Xanh (coding-start) |
| @S08 telemetry mang `service.name` đúng (`health.feature`) | `Telemetry của identity-service mang đúng tên service` | Đỏ: "identity-service chưa đăng ký OpenTelemetry" (thêm ở `test-review`) | Xanh (coding-start) |
| @S09 user DB của identity không có quyền quản trị (`compose.feature`) | `Người dùng cơ sở dữ liệu của identity không có quyền quản trị` | Đỏ: `no such service: identity-service` (thêm ở `test-review`) | Xanh (coding-start) |
| @S10 role khác không kết nối được vào database identity (`compose.feature`) | `Người dùng cơ sở dữ liệu khác không kết nối được vào database của identity` | Đỏ: "Role khác vẫn kết nối được vào database identity" (thêm ở `coding-review`, Confirm log 12) | Xanh (sau `REVOKE CONNECT`) |

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
| @S01 | `Live trả 200 khi cơ sở dữ liệu không chạy` | ✅ xanh (coding-start) |
| @S02 | `Ready trả 200 khi cơ sở dữ liệu chạy` | ✅ xanh (coding-start) |
| @S03 | `Ready trả 503 khi cơ sở dữ liệu đã dừng` | ✅ xanh (coding-start) |
| @S04 | `Migration nền áp lên cơ sở dữ liệu trống và áp lần hai không đổi gì` | ✅ xanh (coding-start) |
| @S05 | `Compose chạy identity-service cùng PostgreSQL và ready đạt trong 30 giây` | ✅ xanh (coding-start) |
| @S06 | `Lỗi chưa xử lý trả lỗi chuẩn không lộ chi tiết nội bộ` | ✅ xanh (coding-start) |
| @S07 | `Không tự áp migration khi cờ tự áp tắt` | ✅ xanh (coding-start) |
| @S08 | `Telemetry của identity-service mang đúng tên service` | ✅ xanh (coding-start) |
| @S09 | `Người dùng cơ sở dữ liệu của identity không có quyền quản trị` | ✅ xanh (coding-start) |

Lệnh: `dotnet test src/identity-service.Tests --no-build` cho 9 test, 9 đỏ, không lỗi biên dịch hay môi trường; `dotnet build src/teamboard.slnx -warnaserror` xanh; `dotnet format --verify-no-changes` không báo lỗi.

**Verdict:** `Sẵn sàng code` (0 CONFIRM mở, 0 FIX mở, không P3).

**Giới hạn đã biết:**
- Test nhóm `docker` bỏ qua (skip) khi máy không có Docker daemon; gate nhanh `Category!=docker` chỉ gồm @S01, @S06, @S08 (đều đỏ) và không kiểm ready hay migration.
- @S09 chỉ kiểm user không có quyền quản trị; chưa kiểm user bị chặn vào database khác (cần `REVOKE CONNECT`, ngoài plan).
- Không có unit test riêng ở lát này (logic mỏng, phần lớn là cấu hình); không có contract test vì chưa có API nghiệp vụ.
- @S08 chỉ xanh khi B1 gọi `AddServiceDefaults("identity-service")` thay cho `Program.cs` rỗng.

## Coding log

Đỏ xác nhận trước khi viết code: 9/9 đỏ đúng lý do (commit `1880761`). Mỗi bước chạy lại test của bước, rồi gate.

| Bước | Thay đổi chính | Test xanh |
|---|---|---|
| B1 | `identity-service.csproj` (tham chiếu `ServiceDefaults`, `Shared.Http`), `Program.cs` gọi `AddServiceDefaults("identity-service")` và `MapDefaultEndpoints()` | @S01, @S08 |
| B2 | `UseExceptionHandler` trả `INTERNAL_ERROR` bằng `ProblemFactory` (không đưa ngoại lệ vào response) | @S06 |
| B3 | `Persistence/` (`IdentityDbContext`, `DatabaseOptions`, `PersistenceExtensions`), migration nền `InitialBaseline`, check DB gắn tag `ready`, cờ `Database:MigrateOnStartup` mặc định tắt; gói `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.13`, `Microsoft.EntityFrameworkCore.Design 10.0.13` | @S02, @S03, @S04, @S07 |
| B4 | `Dockerfile`, compose chính (`identity-service` host 6061, `postgres` dùng chung không mở cổng, volume, healthcheck qua TCP), `src/postgres/init/01-identity.sh` (database và user `identity` không quyền quản trị), `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/dependencies.yaml`, `docker-compose.dcproj` | @S05, @S09 |

Lệnh và kết quả (2026-10-25, Docker 29.5.3):
- `dotnet test src/identity-service.Tests`: 9/9 xanh (3 test nhanh, 6 test nhóm `docker`; @S05 ready trong 7 giây, ngưỡng 30 giây).
- `dotnet build src/teamboard.slnx -warnaserror` xanh; `dotnet format --verify-no-changes` không báo lỗi; `dotnet list package --vulnerable --include-transitive` không có gói lỗ hổng; `docker compose -f src/docker-compose.yml --env-file src/.env.example config -q` ok.
- `dotnet test src/teamboard.slnx --filter "Category!=docker"`: xanh ở cả 5 project test (7, 3, 11, 17, 21 test).
- Hồi quy docker: `gateway.Tests` (1 test) và `board-service.Tests` (2 test) nhóm `docker` xanh sau khi truyền `--env-file`.
- Quét secret thô (`git diff | grep`): chỉ tên biến và `*.example`, không có giá trị thật. `gitleaks` chưa cài nên chưa chạy.

Điểm đáng nhớ và việc ngoài plan (người dùng đồng ý hoặc cần `coding-review` xem):
- **Hồi quy compose:** biến mật khẩu bắt buộc (Confirm log 8 của plan) làm mọi lệnh `docker compose` trên file chính lỗi nếu thiếu `src/.env`. Người dùng chọn sửa test của gateway và board truyền `--env-file src/.env.example` (hai file `Support/DockerCli.cs`) và cập nhật `COMMANDS.md`; người chạy local phải tạo `src/.env` một lần.
- `.gitattributes` thêm `*.sh text eol=lf`: script khởi tạo DB chạy trong container Linux, CRLF của checkout Windows sẽ làm nó lỗi.
- Exception handler nằm trong `Program.cs` của identity, chưa ở `src/shared` như `backend-engineering.md` mục 1 mong muốn; gateway lát 2 sẽ đưa middleware dùng chung vào `src/shared`, lúc đó chuyển sang dùng chung.
- Migration nền sinh bằng `dotnet-ef 10.0.13` cài vào thư mục tạm (không cài toàn cục, không thêm vào repo).
- `Microsoft.EntityFrameworkCore.Design` giữ `PrivateAssets=all`, giống `board-service`.

## Review rounds (coding-review)

Base: `main` (`e79560d`); branch `feature/TB-021-identity-service-skeleton`; review tự thân của agent (không dùng phiên độc lập), đọc `code-review.md` và `backend-engineering.md`, `dotnet.md`, `docker.md`.

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-25 | `83089c9` | Sẵn sàng sau khi sửa (4 finding, 1 cần người dùng chốt) | `identity-service:compose-overlays:env-examples-missing-vars` (P2), `identity-service:postgres-init:public-connect-open` (P2), `identity-service:PersistenceExtensions:migrate-no-cancellation` (P3), `identity-service:env-example:password-special-chars` (P3) |
| 2 | 2026-10-25 | `9edbaa7`, `3efde2e` (delta) | Sẵn sàng merge | Không |

**Confirm log (coding-review):** #12 `identity-service:postgres-init:public-connect-open`: người dùng chọn "Thêm REVOKE và test ngay" (2026-10-25).

**Fix log:**

| Fingerprint | Vị trí | Vấn đề | Đã sửa thế nào | Evidence |
|---|---|---|---|---|
| `identity-service:compose-overlays:env-examples-missing-vars` | `src/.env.develop.example`, `src/.env.stg.example`, `config/environments.yaml` | Biến mật khẩu bắt buộc của compose chính làm `docker compose -f main -f develop|stg` lỗi vì hai file mẫu không có biến | Thêm `POSTGRES_PASSWORD`, `IDENTITY_DB_PASSWORD` vào cả hai file mẫu; ghi `src/.env` vào `environments.yaml` | `9edbaa7`; `docker compose ... -f develop|stg --env-file <giá trị tạm> config -q` ok |
| `identity-service:PersistenceExtensions:migrate-no-cancellation` | `Persistence/PersistenceExtensions.cs` | `MigrateAsync()` không nhận token, trái `dotnet.md` | Truyền `app.Lifetime.ApplicationStopping` | `9edbaa7`; build `-warnaserror`, test xanh |
| `identity-service:env-example:password-special-chars` | `src/.env.example` | Mật khẩu ghép nguyên văn vào chuỗi kết nối, ký tự `;`, nháy, `$` làm hỏng hoặc chèn tham số | Thêm cảnh báo vào file mẫu | `9edbaa7` |
| `identity-service:postgres-init:public-connect-open` | `src/postgres/init/01-identity.sh` | Database `identity` mở CONNECT cho mọi role cùng instance, trái D0.2 khi task-service thêm user | `REVOKE CONNECT ON DATABASE identity FROM PUBLIC`; thêm @S10 (đỏ trước, xanh sau) | `3efde2e`; test `Người dùng cơ sở dữ liệu khác không kết nối được...` |

**Tiêu chí issue (F6):** cả 6 tiêu chí đã có evidence ở "Coding log" và "Kịch bản và TDD log" (health, migration, compose ≤ 30 giây, lỗi 500, gate, `config`/`PROJECT.md`/compose khớp, không secret).

**Risk Audit:**

| Probe | Áp dụng? | Kết quả |
|---|---|---|
| F1 transaction và lỗi | Có (migration) | Migration lỗi làm host dừng ngay (fail fast, có log), không nuốt lỗi; Npgsql giữ khoá migration khi nhiều instance |
| F2 validation | Có (cấu hình) | Thiếu chuỗi kết nối thì dừng khi khởi động, thông báo không chứa giá trị |
| F3 độ sâu test | Có | Kiểm trạng thái thật (bảng lịch sử migration, role trong `pg_roles`, kết nối bị từ chối); ready kiểm DB dừng thật |
| F4 idempotency | Có (migration) | Áp hai lần chỉ một bản ghi nền (@S04) |
| F5 đối chiếu chéo | Có | Tên `identity`, `IdentityDb`, cổng 6061, biến mật khẩu khớp giữa code, compose, test, `config`; sửa env mẫu (finding 1) |
| F6 tiêu chí issue | Có | Xem trên |
| F7 persistence | Có | Migration nền rỗng, an toàn; không có trường server-managed |
| F8 bảo mật | Có | Không secret trong repo, `.dockerignore` chặn `**/.env`, user DB không quản trị, CONNECT thu hồi, lỗi không lộ nội bộ; xem Giới hạn đã biết |

**Blast Radius:** B1: compose chính dùng chung (biến bắt buộc ảnh hưởng mọi lệnh compose; đã sửa test gateway/board, env mẫu develop/stg, `COMMANDS.md`); B2: không có API hay event, chỉ thêm health; B3: không có tác động chéo; B4: API/contract không đổi, database mới `identity` ở `postgres` dùng chung, các service khác không bị ảnh hưởng (chạy lại docker test gateway 1/1, board 2/2).

**Best Practices:** BP1 đạt (không hardcode cấu hình trong code, `shared/` không có logic nghiệp vụ, không tham chiếu service khác); BP2 đạt sau sửa token huỷ; BP3 đạt; BP4 không áp dụng (tải rất nhỏ); BP5 đạt; BP6 đạt (exception handler của ASP.NET ghi log lỗi kèm stack ở server, không vào response).

**Giới hạn đã biết:**
- Exception handler nằm trong `Program.cs`, chưa ở `src/shared` (chờ gateway lát 2 đưa middleware dùng chung); `correlationId` hiện bằng `traceId` (Confirm log 5 của plan).
- Migration tự áp ở compose: `Database__MigrateOnStartup` bật; chưa có bước migration riêng cho môi trường thật (lát hạ tầng sau).
- `gitleaks` chưa cài nên chưa chạy; quét secret thô sạch.
- Chưa có test cho hành vi "cờ migration bật mà DB không kết nối được lúc khởi động" (hiện dừng host); spec chưa quy định.
- `identity-service` publish ra cổng host 6061 trên mọi interface như `board-service` (6060) và `gateway`; chỉ để debug local.
- Review do chính agent đã viết code thực hiện, không phải phiên độc lập; PR vẫn cần người duyệt.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | Plan `draft`, có câu hỏi mở chờ `plan-review` |
| plan-review | done | 2 | 2026-10-25 | PASS; plan `approved`, Confirm log #1–#8 |
| test-init | done | – | 2026-10-25 | Đỏ: 6/7 test (bản đầu), xanh 1; xem "Kịch bản và TDD log" |
| test-review | done | 1 | 2026-10-25 | PASS (Sẵn sàng code); 9 test đỏ đúng lý do; xem "Test review rounds" |
| coding-start | done | – | 2026-10-25 | Xanh 9/9; gate đầy đủ xanh (trừ gitleaks chưa cài); xem "Coding log" |
| coding-review | done | 2 | 2026-10-25 | PASS (Sẵn sàng merge); 4 finding đã sửa, 1 CONFIRM đã chốt; xem "Review rounds (coding-review)" |
| create-pr | done | – | 2026-10-25 | PR [#14](https://github.com/sowndv02/teamboard/pull/14) |
