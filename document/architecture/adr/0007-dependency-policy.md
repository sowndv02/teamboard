# ADR 0007 — Chính sách phụ thuộc bên thứ ba và công cụ giả lập AWS cục bộ

Trạng thái: Chấp nhận (phần chính sách); Đề xuất (phần công cụ giả lập AWS) · Ngày: 2026-10-21
Nguồn: yêu cầu của chủ dự án; `.agents/rules/dependency-policy.md`; `config/dependencies.yaml`
Nhãn: `[Cần]`

## Bối cảnh
Nhiều thư viện .NET phổ biến (MediatR, AutoMapper, MassTransit, FluentAssertions) đã chuyển sang giấy phép thương mại; SpecFlow hết vòng đời; Redis, Terraform đổi giấy phép; LocalStack yêu cầu token; Docker Desktop trả phí với tổ chức lớn. Chủ dự án muốn hạn chế thư viện, chỉ giữ những thứ chắc chắn không thu phí (như EF Core, thuộc nền tảng của nhà cung cấp).

## Quyết định
1. **Dùng nền tảng trước** (.NET BCL, ASP.NET Core, EF Core, `Microsoft.Extensions.*`, Angular, AWS SDK).
2. **Phân hạng T0–T3** (`dependency-policy.md` mục 3); T3 cấm trừ khi người dùng duyệt tường minh kèm ADR và chi phí; T2 cần phê duyệt và bọc sau interface của mình.
3. **Tự viết phần nhỏ, an toàn** (mediator, ánh xạ, lớp nhắn tin mỏng trên AWS SDK, validation) thay vì thêm thư viện; **không tự viết mật mã/ORM**.
4. **Danh bạ** `config/dependencies.yaml` ghi hạng, giấy phép, nguồn kiểm, lối thoát cho mọi phụ thuộc; quét lỗ hổng và giấy phép trong gate.
5. **Cô lập phụ thuộc** sau interface ở project hạ tầng; không để kiểu của thư viện rò vào hợp đồng.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. Chính sách phân hạng + tự viết phần nhỏ (đã chọn) | Ít rủi ro giấy phép, hiểu sâu (hợp mục tiêu học), dễ thay thế | Tốn công hơn một chút; phải kỷ luật | Chọn |
| B. Dùng thoải mái, xử lý khi có sự cố | Nhanh | Rủi ro pháp lý/chi phí bất ngờ; nợ gỡ phụ thuộc | Không chọn |
| C. Chỉ dùng nền tảng, cấm mọi thư viện bên thứ ba | An toàn tuyệt đối về giấy phép | Phi thực tế (OpenTelemetry, xUnit, driver DB...) | Không chọn |

## Vì sao không chọn các phương án còn lại
- **B** sẽ đúng khi sản phẩm ngắn hạn, bỏ đi, không có ràng buộc pháp lý. **Tín hiệu xem lại:** không áp dụng cho dự án có mục tiêu dài hạn.
- **C** sẽ đúng khi môi trường bị kiểm soát cực chặt (ví dụ yêu cầu tuân thủ đặc biệt).

## Công cụ giả lập AWS cục bộ (chưa chốt)
LocalStack (nhắc ở ADR 0002 và nhiều tài liệu) cần auth token từ 2026.03. Lựa chọn khả dĩ:
- Đặt AWS sau interface + **fake trong bộ nhớ** cho unit test (không phụ thuộc công cụ).
- Giả lập nhẹ theo từng dịch vụ (DynamoDB Local của AWS; ElasticMQ/Moto cho SQS/SNS: cần kiểm chứng giấy phép và độ phủ).
- **Tài khoản AWS sandbox thật** cho ít test tích hợp (SQS/SNS/DynamoDB trả theo dùng, chi phí thấp; tuân `aws-service-selection` và `terraform.md`).
- LocalStack nếu điều khoản phù hợp (gói miễn phí phi thương mại).
Cần người dùng quyết (CONFIRM) khi bắt đầu giai đoạn nhắn tin (giai đoạn 2).

## Hệ quả và đánh đổi
- Thêm công sức viết một số phần nhỏ; đổi lại hiểu sâu và giảm rủi ro.
- Mọi thay đổi phụ thuộc cần cập nhật `config/dependencies.yaml` và qua kiểm tra giấy phép.
- Các tài liệu hiện nhắc LocalStack/MediatR/MassTransit như ví dụ cần đọc kèm ADR này.

## Tín hiệu xem lại
- Một phụ thuộc T1/T2 đổi giấy phép hoặc bị bỏ rơi.
- Có yêu cầu pháp lý/tuân thủ mới về giấy phép.
- Đội đủ nguồn lực để chấp nhận chi phí giấy phép cho một công cụ giá trị cao.

## Liên kết
`.agents/rules/dependency-policy.md`, `config/dependencies.yaml`, ADR 0002, ADR 0004, ADR 0006, `document/architecture/patterns-catalog.md`.
