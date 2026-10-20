# .NET rules

Quy ước ngôn ngữ C#. Mã lỗi, cấu hình, hằng số, khởi động, worker và truy cập dữ liệu: xem `backend-engineering.md`.

- Target `net10.0`; `Nullable` và `ImplicitUsings` bật; warning không được bỏ qua (không `#pragma disable` để lách).
- Naming: PascalCase type/method, `_camelCase` private field, `I` prefix interface, hậu tố `Async` cho async method. Một type chính mỗi file.
- Async xuyên suốt: không `.Result`/`.Wait()`. Method async nhận `CancellationToken` và truyền xuống.
- DI qua constructor; đăng ký trong extension method `AddXxx` của từng module/slice. Không service locator.
- Cấu hình dùng Options pattern (`IOptions<T>`) + validate lúc khởi động (`ValidateOnStart`). Không đọc `Environment.GetEnvironmentVariable` rải rác.
- Không hardcode connection string, URL, secret; lấy từ config/env.
- EF Core: migration có tên rõ nghĩa, không sửa migration đã commit; query chỉ đọc dùng `AsNoTracking`; không trả entity ra API (dùng DTO).
- Validation input ở biên (endpoint). Lỗi nghiệp vụ dùng kết quả/exception có kiểu, không dùng exception cho luồng bình thường.
- Log có cấu trúc (`logger.LogInformation("Created {BoardId}", id)`), không nối chuỗi, không log PII/secret.
- Không `catch (Exception)` rồi nuốt; nếu bắt thì log và ném lại hoặc chuyển thành lỗi có nghĩa.
- Chạy `dotnet format` trước khi báo xong.
