# config/

Nơi agent (và người) tra cứu **cấu hình không bí mật** của dự án. Được commit.

| File | Nội dung | Đọc khi |
|---|---|---|
| `project.yaml` | Workflow: issue (prefix, đường dẫn), branch, commit, PR, danh sách tài liệu bắt buộc theo tier R0/R1/R2 | Đầu phiên; tạo branch/commit/PR; tìm tài liệu cần đọc |
| `services.yaml` | Danh sách service: cổng, health path, DB, kiến trúc, trạng thái | Thêm/sửa service, viết compose/Terraform, gọi service-to-service |
| `environments.yaml` | Môi trường local/dev/stg: region, tên resource, cách deploy | Làm infra, CI/CD, đổi cấu hình theo môi trường |
| `dependencies.yaml` | Danh bạ phụ thuộc bên thứ ba đã xét: hạng T0–T3, giấy phép, nguồn kiểm, lối thoát | Thêm/đổi/nâng cấp phụ thuộc; chọn thư viện; review |
| `tool-versions.yaml` | Phiên bản công cụ yêu cầu (.NET, Terraform, Docker...) | Setup máy, viết CI, kiểm tra phiên bản |

## Quy tắc

- **Không đặt secret** (mật khẩu, key, connection string thật) trong thư mục này. Secret nằm ở:
  - Local: `local/` hoặc file `.env*` (đã gitignore; mẫu là `*.example`).
  - AWS: SSM Parameter Store / Secrets Manager. File ở đây chỉ ghi **tên/đường dẫn** của secret, không ghi giá trị.
- Giá trị `TODO` nghĩa là chưa quyết định; agent không tự điền, hãy hỏi người dùng.
- Đây là nguồn sự thật cho cổng, tên service, tên môi trường. Compose, Terraform, CI phải khớp với file này; khi đổi một nơi thì cập nhật các nơi còn lại.
- Cấu hình runtime của từng service (appsettings, env vars) vẫn nằm trong service đó; file ở đây chỉ là bản tổng hợp để tra cứu.
