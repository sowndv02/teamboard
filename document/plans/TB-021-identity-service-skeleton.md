---
title: Identity lát 1a, khung identity-service, health, PostgreSQL và migration nền
issue: TB-021
status: draft            # draft | in-review | approved | done
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

**Không làm (trong lát này):** bảng `user` và mọi bảng nghiệp vụ, đăng ký, băm, benchmark (lát 1b); rate limit (lát 3); khoá ký, Secrets Manager, JWT (lát 4+); route ở gateway (lát 19); middleware `X-Correlation-Id` (gateway lát 2 sẽ đưa vào `src/shared`, xem Q-5); Terraform/AWS; ADR (chưa có quyết định mới trong lát này).

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| NFR-001 `/health/live`, `/health/ready`, OTel | 6.3, mục 9 | B1, B3 | @S01, @S02, @S03 | Host test (live không cần DB); integration với PostgreSQL thật (ready lên/xuống) |
| NFR-003 lỗi `problem+json`, không lộ nội bộ | 6.3, `backend-engineering.md` mục 1 | B2 | @S06 | Host test với endpoint ném lỗi chỉ có trong test host |
| NFR-004 `traceparent` (một phần; `X-Correlation-Id` xem Q-5) | mục 9 | B1 | – | Kiểm `ServiceDefaults` đã gắn trace; không thêm test riêng |
| NFR-009 khôi phục DB dev bằng migration | 10 | B3 | @S04 | Áp migration lên DB trống, kiểm bảng lịch sử migration |
| Compose chạy identity-service + DB (tiêu chí issue) | 10, D0.2 | B4 | @S05 | Test nhóm `docker` gọi `docker compose` qua Process |
| Database + user riêng (D0.2) | D0.2 | B3, B4 | @S05 | Script khởi tạo tạo DB và user riêng; kiểm kết nối bằng user của identity |
| Gate xanh (tiêu chí issue) | – | B1–B4 | – | `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, `docker compose config -q` |

Kịch bản (ID duy nhất, tag `@TB-021 @identity-service`):
- @S01 `GET /health/live` trả 200 kể cả khi DB không chạy.
- @S02 `GET /health/ready` trả 200 khi PostgreSQL chạy và migration đã áp.
- @S03 `GET /health/ready` trả 503 khi PostgreSQL không kết nối được; body không chứa chuỗi kết nối hay mật khẩu.
- @S04 Migration nền áp lên PostgreSQL trống thành công và bảng lịch sử migration có bản ghi nền; áp lần hai không đổi gì.
- @S05 (`@docker`) Compose chạy identity-service và PostgreSQL: `/health/ready` ở cổng host trả 200 trong ngưỡng ở Q-4 kể từ `up -d` (image đã build).
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
- **Kịch bản/test sẽ viết (đỏ trước):** `database.feature` @S02 (ready 200 sau migration), @S03 (ready 503 khi DB dừng, không lộ chuỗi kết nối), @S04 (migration trên DB trống, chạy hai lần). Cách dựng PostgreSQL cho test: Q-2.
- **File/module dự kiến chạm:** `src/identity-service/Persistence/IdentityDbContext.cs`, `Persistence/Migrations/` (migration nền), `Program.cs` (đăng ký DbContext, check DB gắn tag `ReadyTag`), `src/identity-service/appsettings.json` (không chứa mật khẩu), `identity-service.csproj` (gói Npgsql EF, health check EF; xem Q-1), `src/identity-service.Tests/`.
- **Hợp đồng thay đổi:** `/health/ready` phụ thuộc DB (design 6.3).
- **Migration / cấu hình:** migration nền không tạo bảng nghiệp vụ; cấu hình chuỗi kết nối qua `ConnectionStrings__IdentityDb` (biến môi trường); cách áp migration: Q-3.
- **Tài liệu cần cập nhật:** `config/dependencies.yaml` (Npgsql EF provider, check EF).
- **Kiểm chứng:** `dotnet test src/identity-service.Tests --filter "Category!=docker"` xanh (gồm test PostgreSQL thật theo Q-2); `dotnet list src/teamboard.slnx package --vulnerable --include-transitive` sạch.
- **Phụ thuộc:** B2, Q-1, Q-2, Q-3.

### B4. Dockerfile, compose, cấu hình và tài liệu (@S05)
- **Kịch bản/test sẽ viết (đỏ trước):** `compose.feature` @S05 (`@docker`) gọi `docker compose` qua Process (theo `gateway.Tests/ComposeSteps.cs`), kiểm ready trong ngưỡng; sửa test hygiene của TB-001 quét compose mới nếu cần.
- **File/module dự kiến chạm:** `src/identity-service/Dockerfile` (theo mẫu `board-service/Dockerfile`), `src/docker-compose.yml` (service `identity-service`, service PostgreSQL, volume, biến môi trường), script khởi tạo DB và user riêng cho identity (D0.2), `src/.env.example` (biến mật khẩu DB, không giá trị thật; `.gitignore` đã chặn `.env`), `config/services.yaml` (`status: existing`, `path`, `local_port`, `container_port`), `PROJECT.md` (trạng thái hiện tại), `COMMANDS.md` (cổng, lệnh), `document/spec/features/identity-service/compose.feature`.
- **Hợp đồng thay đổi:** cổng host của identity-service (Q-4); không route ở gateway.
- **Migration / cấu hình:** biến môi trường `ConnectionStrings__IdentityDb`, mật khẩu DB lấy từ `.env` local (LL-001).
- **Tài liệu cần cập nhật:** `config/services.yaml`, `PROJECT.md`, `COMMANDS.md`, `config/tool-versions.yaml` (nếu chốt phiên bản Docker/PostgreSQL, Q-4).
- **Kiểm chứng:** `docker compose -f src/docker-compose.yml config -q`; test `docker` (`dotnet test --filter "Category=docker"`) xanh; `git diff | grep -n -i -E "password|secret|connectionstring"` chỉ ra tên biến và file `*.example`, không có giá trị thật.
- **Phụ thuộc:** B3, Q-4.

## 4. Chiến lược test
| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|
| Unit/host | Health live, lỗi 500, cấu hình | `WebApplicationFactory<Program>` | Không DB; endpoint ném lỗi chỉ có trong test host |
| Integration | Ready, migration nền | PostgreSQL thật (cách dựng: Q-2) | Không mock DB; DB dừng thật cho @S03 |
| Docker | Compose chạy cả hai container | `docker compose` qua Process, nhóm `docker`, cổng host trống | Không chạy trong gate nhanh |
| Contract/e2e | Chưa có (chưa có API nghiệp vụ) | – | Bắt đầu từ lát 1b |

Công cụ test/BDD (`config/tool-versions.yaml`): xUnit 2.9.3 và Reqnroll.xUnit 3.3.4 (đã duyệt, TB-001). Testcontainers: Q-2.

## 5. Rủi ro và rollback
| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|
| Gói Npgsql EF hoặc check EF chưa qua kiểm theo ADR 0007 (`verified: false`) | Phải đổi gói giữa chừng | Q-1: kiểm nguồn chính thức và giấy phép ở B1/B3 trước khi pin | Bỏ gói, quay về B2 |
| Chuỗi kết nối hoặc mật khẩu DB lọt vào repo (LL-001) | Lộ secret, phải xoay | `.env` đã ignore, chỉ `*.example`, quét diff bằng lệnh ở COMMANDS.md mục 2 | Xoay mật khẩu, gỡ khỏi lịch sử theo AGENTS.md |
| Xung đột cổng host với `board-service` (6060) và `gateway` (8080) | Compose không lên | Q-4 chốt cổng, test docker yêu cầu cổng trống | Đổi cổng trong compose |
| Tự áp migration khi khởi động ghi nhầm DB khác | Hỏng dữ liệu | Q-3 chốt cách áp; không áp lên DB không phải local (COMMANDS.md mục 5) | Xoá volume DB dev, áp lại migration (NFR-009) |
| ready 200 giả (check DB không thật sự kết nối) | Orchestrator coi service sống khi DB chết | @S03 dùng DB dừng thật | Sửa check |

Ảnh hưởng consumer (service khác): không có (service mới, chưa được route ở gateway).

Bảo mật: không secret trong repo; `/health/ready` và lỗi `500` không lộ chuỗi kết nối, stack trace (test @S03, @S06); user DB riêng của identity, không dùng user quản trị cho ứng dụng (D0.2).

Chi phí: chỉ chạy local bằng compose (chính sách local-first, design mục 10); không tài nguyên AWS; không có bước `destroy`.

Bài học đã dùng: LL-001 (secret trong `.env`).

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, `quality-gate.md` (vùng .NET và Docker): `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test src/teamboard.slnx --filter "Category!=docker"`, `dotnet test --filter "Category=docker"`, `dotnet format --verify-no-changes`, `docker compose -f src/docker-compose.yml config -q`, `dotnet list package --vulnerable --include-transitive`, quét secret thô.
- Tiêu chí hoàn thành của issue: xem TB-021.
- Tài liệu/config cập nhật: `config/services.yaml`, `config/dependencies.yaml`, `PROJECT.md`, `COMMANDS.md`, `document/plans/identity-auth-slices.md` (trạng thái, ước lượng vs thực tế).

## 7. Câu hỏi mở và giả định
Mỗi câu có lựa chọn, khuyến nghị và hậu quả nếu chưa chốt (`plan-review` đưa cho người dùng); chưa tự điền.

| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|
| Q-1 | Gói mới: `Npgsql.EntityFrameworkCore.PostgreSQL` (Npgsql đã `approved` T1, nhưng gói EF `verified: false`) và `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` có được thêm không | B3 | sondv | Lựa chọn: A) thêm cả hai sau khi kiểm giấy phép và tương thích .NET 10 ở nguồn chính thức; B) chỉ gói Npgsql EF, tự viết check DB (nhiều mã hơn). Khuyến nghị A: cùng hệ EF Core first-party với board-service, ít mã tự viết. Hậu quả nếu chưa chốt: B3 không bắt đầu được. |
| Q-2 | PostgreSQL cho test tích hợp dựng bằng gì | B3, @S02–@S04 | sondv | Lựa chọn: A) Testcontainers.PostgreSql (T2, đang `watch`, cần duyệt; design mục 10 và 11 đã nêu); B) dùng container PostgreSQL của compose, test nhóm `docker`. Khuyến nghị A: DB sạch cho từng test, không cần cổng cố định, khớp design mục 11; B chậm hơn và không cô lập. Hậu quả nếu chưa chốt: test B3 không có chỗ chạy, gate nhanh không kiểm DB. |
| Q-3 | Áp migration thế nào | B3, B4 | sondv | Lựa chọn: A) tự áp khi khởi động, bật bằng cờ `Database:MigrateOnStartup` (compose bật, mặc định tắt); B) lệnh riêng (`dotnet ef database update` hoặc bước/container chạy một lần). Khuyến nghị A với cờ mặc định tắt: tiện cho compose, an toàn khi nhiều instance nhờ mặc định tắt; B đúng hơn cho môi trường thật, tính ở lát hạ tầng sau. Hậu quả nếu chưa chốt: B3 và B4 chưa biết ready đợi migration hay không. |
| Q-4 | Cổng host của identity-service, cổng DB và ngưỡng thời gian `ready` (`local_port: TODO` trong `config/services.yaml`; `docker` ở `config/tool-versions.yaml` còn `TODO`) | B4, @S05 | sondv | Lựa chọn: A) identity-service host 6061 (container 7070 như service khác), PostgreSQL không mở cổng ra host, ready ≤ 30 giây; B) mở thêm cổng PostgreSQL ra `127.0.0.1` để debug. Khuyến nghị A; phiên bản image PostgreSQL ghim theo `dependency-pinning.md`, người dùng chọn major. Hậu quả nếu chưa chốt: không viết được compose và @S05. |
| Q-5 | `X-Correlation-Id` (NFR-004 phần còn lại) khi middleware dùng chung chưa có | B2 | sondv | Lựa chọn: A) để sau, lát này chỉ `traceparent` qua OTel, `correlationId` trong `problem+json` theo `ProblemFactory` hiện có; B) viết middleware tạm trong identity. Khuyến nghị A: gateway lát 2 sẽ đưa middleware vào `src/shared`, viết tạm rồi bỏ là làm hai lần. Hậu quả nếu chưa chốt: @S06 chưa rõ có kiểm `correlationId` hay không. |

Giả định: sơ đồ `Features/<Slice>/` là quy ước thư mục chưa có ví dụ trong repo (identity là service Vertical Slice đầu tiên); chốt cụ thể khi viết B1, ghi lại ở đây.

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| 2026-10-25 | Tạo bản nháp | `plan-init` cho lát 1a của identity-auth |
