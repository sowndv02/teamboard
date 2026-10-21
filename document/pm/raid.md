# RAID: Rủi ro, Giả định, Vấn đề, Phụ thuộc

Cập nhật: 2026-10-21 · Xem lại: hằng tuần. Cách đánh giá: xác suất (T/V/C) × tác động (T/V/C) → ưu tiên. Chiến lược: tránh / giảm / chuyển / chấp nhận. Skill: `project-manager` (chế độ `risks`).

## R: Rủi ro

| ID | Rủi ro (nguyên nhân → sự kiện → hậu quả) | Xác suất | Tác động | Ưu tiên | Chiến lược và hành động | Chủ | Dấu hiệu cảnh báo sớm | Xem lại |
|---|---|---|---|---|---|---|---|---|
| R1 | Phạm vi quá rộng (6 service + Terraform + observability) cho một người học → không hoàn thành, mất động lực | Cao | Cao | Cao | **Giảm:** làm theo mốc dọc (roadmap), mỗi mốc chạy được đầu-cuối; chấp nhận Won't; xem xét cắt `activity-service` hoặc làm modular monolith đối chứng | Chủ dự án | Mốc M0 kéo dài quá dự kiến; WIP > giới hạn | Hằng tuần |
| R2 | Ngân sách AWS chưa chốt (`TODO`) → hoá đơn bất ngờ khi tạo hạ tầng | Vừa | Cao | Cao | **Tránh:** chốt D1 trước M4; Budgets bật qua Terraform; môi trường tắt ngoài giờ học; ước tính chi phí (`aws-service-selection`) | Chủ dự án | Tạo tài nguyên tính giờ chưa có ước tính | Trước M4 |
| R3 | Secret đã nằm trong lịch sử git (LL-001) → lộ mật khẩu nếu repo được chia sẻ/đẩy lên | Vừa | Cao | Cao | **Giảm:** xoay mật khẩu; cân nhắc viết lại lịch sử trước khi push; cài `gitleaks` | Chủ dự án | Chuẩn bị `git push` lên remote | Trước khi push |
| R4 | Công cụ/thư viện đổi giấy phép (LL-002) → chi phí hoặc rủi ro pháp lý, phải sửa lại thiết kế | Vừa | Vừa | Vừa | **Giảm:** `dependency-policy.md`, danh bạ phụ thuộc, bọc sau interface; chưa chốt công cụ giả lập AWS (D2) | Chủ dự án | Thêm phụ thuộc T2/T3; thông báo giấy phép từ nhà cung cấp | Hằng tháng |
| R5 | Các quyết định chặn còn `TODO` (D1–D4) → mốc bị đứng | Cao | Vừa | Cao | **Giảm:** chốt theo thứ tự chặn nhiều việc nhất (D3, D2, D1); ghi hạn quyết định | Chủ dự án | Một issue không bắt đầu được vì `TODO` | Hằng tuần |
| R6 | Quy trình agent chỉ dựa kỷ luật, không có công cụ ép cổng → agent lách bước/cổng | Vừa | Vừa | Vừa | **Giảm:** chạy thử `workflow` trên tính năng nhỏ; nếu lách, thêm script `workflow-check` (backlog Later) | Chủ dự án | Phát hiện bước bị bỏ qua hoặc state không cập nhật | Sau lần chạy thử đầu |
| R7 | Tải quy trình nặng (nhiều bước review, tài liệu) cho dự án nhỏ → chậm, nản | Vừa | Vừa | Vừa | **Giảm:** dùng **làn nhẹ** (`pipeline-lifecycle.md`) cho việc nhỏ; theo dõi số vòng review/issue; rút gọn khi quá nặng | Chủ dự án | Một issue nhỏ tốn nhiều hơn công sức giá trị | Sau 3 issue đầu |
| R8 | Kiến thức tổng hợp từ AI (giá, giấy phép, quota, API) lỗi thời hoặc sai → quyết định sai | Vừa | Vừa | Vừa | **Giảm:** đánh dấu `verified`, kiểm tra nguồn chính thức trước khi chốt; ghi nguồn và ngày | Chủ dự án | Con số/giấy phép không có nguồn trong tài liệu | Mỗi quyết định lớn |
| R9 | Tự xây identity (ADR 0006) → lỗ hổng bảo mật tinh vi, mất nhiều thời gian | Vừa | Vừa | Vừa | **Chấp nhận có chủ ý** (`[Chọn để học]`) + giảm: test tấn công từng bước, không dùng cho dữ liệu thật; có lối thoát sang Cognito | Chủ dự án | Phát hiện lỗ hổng nghiêm trọng; dự án có người dùng thật | Mỗi giai đoạn I |
| R10 | Docker Desktop/LocalStack/Terraform có điều khoản thương mại ở môi trường công ty | Thấp–Vừa | Vừa | Thấp | **Giảm:** kiểm điều khoản khi dùng cho việc ở công ty; có phương án thay (Rancher Desktop/Podman, OpenTofu) | Chủ dự án | Dùng dự án này trên máy/tài khoản công ty | Trước khi dùng ở công ty |

## A: Giả định (cần xác nhận)

| ID | Giả định | Xác nhận bởi | Hạn | Trạng thái |
|---|---|---|---|---|
| A1 | Người làm chủ yếu một mình; thời gian khả dụng mỗi tuần chưa biết (`pm.capacityHoursPerWeek`) | Chủ dự án | Trước sprint đầu | Chưa xác nhận |
| A2 | Không có hạn chót cứng bên ngoài | Chủ dự án | Trước khi chốt roadmap | Chưa xác nhận |
| A3 | Dùng tài khoản AWS sandbox riêng cho học tập và thực hành bảo mật | Chủ dự án | Trước M4 | Chưa xác nhận |
| A4 | Máy phát triển chạy được Docker và .NET 10 | Chủ dự án | Trước TB-001 | Chưa xác nhận |
| A5 | Repo `sowndv02/teamboard` là nơi duy nhất lưu mã nguồn (PR qua GitHub) | Chủ dự án | – | Đã có remote `origin` |

## I: Vấn đề (rủi ro đã xảy ra)

| ID | Vấn đề | Tác động | Hành động | Chủ | Trạng thái | Bài học |
|---|---|---|---|---|---|---|
| I1 | `.env` chứa mật khẩu đã commit | Secret trong lịch sử git | Đã gỡ khỏi index, thêm `*.example`; **chờ xoay mật khẩu** | Chủ dự án | Đang xử lý | LL-001 |
| I2 | Tài liệu mặc định công cụ có thể tính phí (LocalStack...) | Có thể phải sửa thiết kế | Đã thêm chính sách và danh bạ; chờ chốt công cụ giả lập (D2) | Chủ dự án | Giảm (chờ D2) | LL-002 |

## D: Phụ thuộc

| ID | Phụ thuộc | Loại | Cần bởi | Ai cung cấp | Nếu không có |
|---|---|---|---|---|---|
| D1 | Quyết định ngân sách, region, tài khoản AWS | Ngoài (chủ dự án) | M4 | Chủ dự án | Không tạo hạ tầng; `aws-service-selection` dừng |
| D2 | Hồ sơ AWS chỉ đọc cho agent chạy `plan` | Ngoài | M4 | Chủ dự án | Chỉ `fmt/validate`, không `plan` thật |
| D3 | BDD runner, test framework | Kỹ thuật | `test-init` | Chủ dự án (ADR/`tool-versions.yaml`) | Không viết được test thực thi |
| D4 | Nhà cung cấp công cụ (giấy phép, điều khoản): Docker, LocalStack, Terraform | Ngoài | Mọi mốc | Nhà cung cấp | Dùng phương án thay thế (xem R4, R10) |
