# Backend engineering

Lý do tồn tại: các service backend (ASP.NET Core) có kiến trúc nội bộ khác nhau nhưng phải thống nhất về **mã lỗi, cấu hình, hằng số, khởi động, background worker và truy cập dữ liệu**. Áp dụng khi sửa `src/**` backend (mọi service, `src/shared`).

Bổ sung cho: `dotnet.md` (quy ước C#), `api-endpoints.md` (hình dạng HTTP), `architecture.md` (ranh giới và kiến trúc từng service), `messaging.md`, `observability.md`.

## 1. Mã lỗi và thông báo (registry tập trung)

- Lỗi API theo `application/problem+json` (xem `api-endpoints.md` mục 5). Trường `code` là **mã ổn định**, client xử lý theo `code`, **không** rẽ nhánh theo `message`.
- `message`/`detail` mặc định bằng **tiếng Anh**, ngắn, không lộ chi tiết nội bộ; bản tiếng Việt do client dịch theo `code`.
- Registry nằm ở `src/shared` (contract, không logic nghiệp vụ):
  - `ErrorCodes` (const string, UPPER_SNAKE): `VALIDATION_FAILED`, `NOT_FOUND`, `CONFLICT`, `INTERNAL_ERROR`, …
  - Ánh xạ `ErrorCode → HTTP status + message mặc định`; hàm `FromHttpStatus` cho lỗi chưa có mã riêng.
  - Mã lỗi riêng của service đặt trong `<Service>.Contracts` với tiền tố miền (`TASK_ALREADY_ASSIGNED`), vẫn đăng ký ở một chỗ.
- Code nghiệp vụ ném lỗi có kiểu (`DomainException`/`ApplicationException(ErrorCodes.X)`), **không** `throw new Exception("chuỗi")`, **không** hardcode literal mã hoặc message.
- Một middleware/exception handler chung (từ `src/shared`) chuyển ngoại lệ thành `problem+json`; endpoint không tự dựng lỗi.
- Test chính sách: mọi `ErrorCode` có status và message mặc định; không mã trùng; không message rỗng.
- Mã log vận hành tách riêng khỏi mã lỗi API (`OperationalEventIds`/`LogEvents`), không dùng chung.

## 2. Cấu hình (config)

- Cấu hình đọc qua **Options pattern**: lớp `XxxOptions` + `services.AddOptions<XxxOptions>().BindConfiguration("Xxx").ValidateDataAnnotations().ValidateOnStart()`. Sai/thiếu cấu hình bắt buộc → **fail khi khởi động**, không để lỗi lúc chạy.
- Không `Environment.GetEnvironmentVariable`/`IConfiguration["..."]` rải rác trong code nghiệp vụ; chỉ bind ở tầng composition root vào Options.
- **Secret** (mật khẩu, token, API key, connection string thật): **không** trong `src/`, `appsettings*.json` commit, compose, hay log. Lấy từ biến môi trường, User Secrets (dev), SSM/Secrets Manager (AWS). Tên biến nằm ở `*.example` và `config/`.
- Giá trị mặc định hợp lý và **giới hạn** (timeout, page size tối đa, kích thước body) đặt trong `<Domain>Defaults`/Options có validate, không literal rải rác.
- Một thiết lập, một tên: tên cấu hình/biến môi trường khớp giữa code, compose, Terraform và `config/environments.yaml`.

## 3. Hằng số và chống hardcode

| Loại | Quy tắc |
|---|---|
| **Secret** | Cấm hardcode (xem mục 2). |
| **Config** (host, port, URL, timeout, đường dẫn) | Qua Options, không literal trong code. |
| **Magic number / magic string** | Từ lần xuất hiện thứ 2 trở đi (hoặc khi mang ý nghĩa nghiệp vụ) → `const`/`static readonly`/enum đặt tên rõ nghĩa trong file `<Domain>Constants.cs` hoặc lớp `static` cùng miền. |
| **Message log vận hành** | Dùng template log có tên trong `LoggerMessage` (source-generated) hoặc `LogMessages`, không nối chuỗi và không lặp literal. |
| **Route** | Hằng số/`MapGroup` theo tài nguyên (`ApiRoutes.Boards`), không lặp chuỗi đường dẫn ở nhiều nơi. |
| **Thoát process** | Worker/host dùng `ExitCodes` có tên; không `Environment.Exit(1)` literal. |

Ngoại lệ chấp nhận được: số `0`, `1`, `-1`, chuỗi rỗng, và literal trong test mô tả dữ liệu mẫu.

## 4. Cấu trúc project và import

- Mỗi service theo kiến trúc bắt buộc của nó (`architecture.md`); thư mục/namespace phản ánh kiến trúc (Clean: `Domain/Application/Infrastructure/Api`; Vertical Slice: `Features/<UseCase>`).
- Namespace khớp thư mục; một type chính mỗi file; tên project `<Service>.<Layer>` hoặc `<Service>` (thống nhất trong repo).
- **Tham chiếu project**: theo chiều phụ thuộc của kiến trúc (vd. Domain không tham chiếu Infrastructure). Service **không** tham chiếu project của service khác; chia sẻ qua `src/shared` (contract/cross-cutting) hoặc event/HTTP.
- `global using` chỉ cho namespace dùng khắp project và đặt trong **một** file `GlobalUsings.cs`; không dùng để che phụ thuộc tầng.
- Kiểm soát ranh giới bằng test kiến trúc (ví dụ NetArchTest/ArchUnitNET 🚧): Domain không phụ thuộc framework/hạ tầng, service không tham chiếu chéo.

## 5. Khởi động (`Program.cs`) và testability

- `Program.cs` là **entry duy nhất**, ngắn: dựng host qua các extension `AddXxx`/`UseXxx` của từng module/slice; không logic nghiệp vụ.
- Cho phép test dựng ứng dụng: `public partial class Program { }` hoặc factory (`CreateApp`) để `WebApplicationFactory<Program>` dựng host trong test. Test **không** chạy executable thật, không bind cổng cố định.
- Test thay dependency bên ngoài (hàng đợi, email, đồng hồ) bằng fake ở composition root, **không** sửa code production cho test.
- Thứ tự middleware cố định và có chú thích lý do: correlation → exception handler → xác thực → phân quyền → endpoint. Health `/health/live`, `/health/ready` luôn có (`observability.md`).
- Tắt máy êm (graceful shutdown): hoàn tất request/message đang xử lý, không nhận mới; timeout shutdown cấu hình qua Options.

## 6. Background worker, consumer, scheduled job

- Dùng `BackgroundService`/`IHostedService`; vòng lặp tôn trọng `CancellationToken`; **không** `Task.Run` fire-and-forget không giám sát.
- Lỗi trong một lượt xử lý không làm chết cả worker: bắt, log có `correlationId`, backoff có giới hạn, rồi tiếp tục; lỗi vĩnh viễn → DLQ (`messaging.md`).
- Mỗi worker có metric/health riêng (độ trễ hàng đợi, số lần retry, lỗi).
- Job chạy định kỳ phải **idempotent** và an toàn khi chạy chồng (khoá phân tán hoặc điều kiện cập nhật có điều kiện).
- Worker độc lập không phải HTTP: thoát bằng `ExitCodes` có tên khi lỗi khởi tạo.

## 7. Truy cập dữ liệu

- Mỗi service chỉ truy cập database **của mình** (`architecture.md`). Chuỗi kết nối qua Options, không literal.
- EF Core: một `DbContext` cho mỗi service/bounded context; migration có tên rõ nghĩa, không sửa migration đã áp dụng; `AsNoTracking` cho truy vấn chỉ đọc; không để entity lọt ra ngoài tầng truy cập dữ liệu.
- Truy vấn: tránh N+1 (`Include`/projection), luôn phân trang, giới hạn kích thước kết quả. Giao dịch ngắn, phạm vi rõ (cùng transaction với ghi outbox).
- Migration chạy có kiểm soát (bước riêng khi deploy), **không** tự `Migrate()` lúc khởi động ở stg/prod; `dotnet ef database update` trên DB không phải local cần người dùng đồng ý (`COMMANDS.md`).
- Dữ liệu seed/fixture dùng cho test nằm trong test project, không lẫn vào migration production.

## 8. Chịu lỗi khi gọi dịch vụ ngoài

- Mọi call ra ngoài (HTTP service khác, AWS) có **timeout rõ ràng**, retry có jitter chỉ cho thao tác idempotent, circuit breaker (Polly/`Microsoft.Extensions.Http.Resilience`). Không retry vô hạn.
- Dùng `IHttpClientFactory` (typed client), không `new HttpClient()`.
- Truyền `CancellationToken`, `correlationId` và `traceparent` theo từng call.
- Lỗi từ dịch vụ ngoài chuyển thành lỗi miền có mã, không rò chi tiết nội bộ ra response.

## 9. Checklist trước khi báo xong thay đổi backend

- [ ] Lỗi ném bằng mã trong registry; không literal mã/message
- [ ] Cấu hình qua Options có validate; không secret/URL/timeout hardcode
- [ ] Magic number/string đã đặt tên; route dùng hằng/`MapGroup`
- [ ] Không tham chiếu project service khác; ranh giới kiến trúc giữ nguyên
- [ ] `Program.cs` vẫn mỏng và test dựng được host qua factory
- [ ] Worker/consumer idempotent, tôn trọng cancellation, có DLQ/metric
- [ ] Truy vấn không N+1, có phân trang; migration an toàn
- [ ] Call ra ngoài có timeout/retry/breaker; mang `correlationId`
- [ ] Gate đã chạy (`code-change-gate.md`, `quality-gate.md`)

## Thư viện bên thứ ba

Phụ thuộc mới: theo `dependency-policy.md` (nền tảng trước; T3 cấm; T2 phải bọc sau interface). Không dùng MediatR/AutoMapper/MassTransit/FluentAssertions bản thương mại; xem bảng thay thế ở mục 5 của policy.
