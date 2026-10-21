---
name: aws-service-selection
description: Chọn dịch vụ AWS (compute, dữ liệu, hàng đợi, mạng, bảo mật, quan sát) cho từng service của hệ thống, đặt chi phí lên đầu rồi cân theo workload, hiệu năng/quy mô, dữ liệu/nhất quán, sẵn sàng/xử lý lỗi, bảo mật/tuân thủ, độ phức tạp vận hành; có giải thích vì sao chọn/không chọn. Dùng khi cần quyết định hạ tầng AWS cho một service trước khi viết Terraform, so sánh hai dịch vụ AWS, ước tính chi phí hoặc review stack AWS đã chọn.
---

# Chọn dịch vụ AWS cho từng service (chi phí đi đầu)

Với **một service**, quyết định dịch vụ AWS nào dùng cho từng năng lực (chạy mã, lưu dữ liệu, truyền sự kiện, vào/ra mạng, bảo mật, quan sát), có **chi phí là cổng đầu tiên**. Đầu ra là tài liệu `document/infra/<service>.md` (mẫu `document/infra/_template.md`) và **danh sách tài nguyên đã đề xuất duyệt** làm đầu vào cho Terraform (`terraform.md` mục 2). Skill này **không viết Terraform** và **không `apply`**.

Nguyên tắc nền: `agent-conduct.md` (trung thực, không bịa), `architecture.md`, `terraform.md` (mục 3 bảo mật, mục 4 chi phí), `api-endpoints.md`, `messaging.md`, `observability.md`, ADR hiện có (stack, ECS Fargate). Khung cân nhắc chung: `../system-design/references/decision-framework.md`.

Tài liệu tham chiếu của skill: `references/cost-model.md` (cách ước tính chi phí), `references/aws-service-catalog.md` (danh mục dịch vụ: dùng khi/tránh khi/dạng chi phí/bẫy).

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `select` (mặc định) | Cần chọn stack AWS cho một service | `document/infra/<service>.md` + danh sách tài nguyên đề xuất + ADR nháp |
| `compare` | Phân vân giữa hai dịch vụ AWS (ví dụ SQS vs EventBridge, Fargate vs Lambda, RDS vs DynamoDB) | Bảng so sánh, cost trước, khuyến nghị, vì sao không chọn |
| `cost` | Chỉ cần ước tính/what-if chi phí của một stack | Bảng chi phí theo môi trường và kịch bản tải |
| `review` | Đã có stack/Terraform, cần soát | Danh sách vấn đề: chi phí chạy rỗng, bảo mật, độ tin cậy, vận hành, kèm đề xuất |

Không cần gõ lệnh; suy ra chế độ từ lời người dùng, không rõ thì hỏi.

## Nguyên tắc

1. **Chi phí đi đầu.** Trước khi so tính năng, phải có **trần chi phí** và **ước tính chi phí từng ứng viên**. Ứng viên vượt trần hoặc có **chi phí chạy rỗng (idle)** không biện minh được → loại sớm. Rẻ nhất mà **đủ dùng** (cheapest viable) là phương án nền; chọn đắt hơn phải có lý do gắn với tiêu chí cụ thể và được người dùng chấp nhận.
2. **Đánh đổi là của người dùng.** Đánh đổi chi phí ↔ độ sẵn sàng/hiệu năng/bảo mật không có đáp án duy nhất → hỏi người dùng (CONFIRM), kèm số liệu. Không tự chốt.
3. **Không bịa số.** Giá lấy từ trang giá chính thức hoặc AWS Pricing Calculator; không lấy được thì để tham số (`<giá/giờ>`) và ghi `chưa xác minh`. Mọi con số có công thức, đơn vị, giả định, vùng (region), ngày.
4. **Phù hợp quy mô thật.** Chọn theo tải trong spec/design, có dự phòng hợp lý; không thiết kế cho tải không có. Nêu tín hiệu để nâng cấp.
5. **Tôn trọng ràng buộc đã chốt** (ADR: tech stack, ECS Fargate, AWS, kiến trúc từng service). Muốn phá → nêu thành đề xuất ADR, không tự phá.
6. **Học và thực tế:** dự án này dùng nhiều kiểu kiến trúc để luyện. Dịch vụ chọn **vì mục tiêu học** mà đắt/phức tạp hơn mức cần phải gắn nhãn `[Chọn để học]` kèm "nếu làm thật, phương án rẻ/đơn giản hơn là …" và được duyệt về chi phí.
7. **Bảo mật không bị hy sinh vì chi phí** khi chưa có quyết định tường minh của người dùng (`review-philosophy.md`).

## Quy trình `select`

### 0. Đầu vào và cổng
- Đọc: spec/design của service, `config/services.yaml`, `config/environments.yaml`, ADR, `document/architecture/README.md`, `config/project.yaml` → `quality`.
- **Trần chi phí:** `quality.monthlyBudgetUsd` (cả dự án). Còn `TODO` → **hỏi người dùng**, không bắt đầu chọn. Chia phần của service này (ví dụ % ngân sách) và theo môi trường (dev/stg/prod); đề xuất phân chia, người dùng chốt.
- Region/account còn `TODO` → hỏi (ảnh hưởng giá, độ trễ, data residency).
- Thiếu thông tin tải/dữ liệu → hỏi theo lô (≤ 4 câu, kèm mặc định đề xuất); người dùng không biết → giả định ghi rõ, đánh dấu cần xác nhận.

### 1. Chi phí trước (cổng chi phí)
Theo `references/cost-model.md`:
1. Chia service thành các **năng lực** (compute, dữ liệu, hàng đợi/sự kiện, vào-ra mạng/edge, bảo mật, quan sát).
2. Với mỗi năng lực chọn **2–4 ứng viên thật**, luôn có **rẻ nhất đủ dùng**.
3. Ước tính chi phí **cố định (idle), theo mức dùng, và ẩn** (truyền dữ liệu, NAT, log, KMS/API calls, backup, snapshot) cho dev, stg, prod và **3 kịch bản tải** (thấp, kỳ vọng, đỉnh).
4. Quy đổi thành **% trần ngân sách**; loại ứng viên vượt trần hoặc chạy rỗng đáng kể mà không có lý do; ghi lý do loại.
5. Nêu chi phí vận hành của con người (vá lỗi, nâng cấp, trực sự cố) như một dòng chi phí, không chỉ hoá đơn AWS.
Trình bày **bảng xếp hạng chi phí** trước khi bàn tiếp.

### 2. Bảy tiêu chí cho các ứng viên còn lại
Chi phí đã xét ở bước 1; với ứng viên còn lại, đánh giá lần lượt (mỗi mục kết luận Tốt/Vừa/Kém kèm lý do một dòng, nối về spec/design):

1. **Workload:** thực sự cần giải bài toán gì (API, nền, lưu file, dữ liệu quan hệ, stream sự kiện, batch, thời gian thực)?
2. **Hiệu năng và quy mô:** request/giây, người dùng đồng thời, thông lượng, độ trễ (p95), kích thước payload, đột biến, tăng trưởng.
3. **Dữ liệu và nhất quán:** quan hệ, giao dịch, nhất quán, mẫu truy vấn, schema, retention, backup, khôi phục, khả năng di chuyển.
4. **Sẵn sàng và xử lý lỗi:** SLA, Multi-AZ, RTO/RPO, retry, timeout, DLQ, idempotency, phục hồi khi phụ thuộc lỗi.
5. **Bảo mật và tuân thủ:** public/private, IAM, mã hoá, bí mật, cô lập mạng, data residency, audit (`quality.compliance`).
6. **Chi phí:** đã xét ở bước 1; **kiểm lại sau khi chọn** (bước 4) vì lựa chọn kéo theo chi phí khác.
7. **Độ phức tạp vận hành và ràng buộc:** ai quản OS/runtime, vá lỗi, scale, deploy, debug; lock-in; giới hạn dịch vụ (quota, kích thước, thời gian); kỹ năng của đội.

Dùng `references/aws-service-catalog.md` để gợi ý ứng viên và bẫy; **không** coi là đáp án: luôn đối chiếu với drivers của service.

### 3. Chọn và giải thích
Cho **mỗi năng lực**: bảng ứng viên (chi phí/tháng và % trần, 6 tiêu chí còn lại), **khuyến nghị**, **vì sao không chọn từng ứng viên còn lại và điều kiện nó trở nên đúng**, đánh đổi, khả năng đảo ngược, **tín hiệu xem lại** (chi phí, tải), nhãn `[Cần]`/`[Chọn để học]`. Use case tương tự ngắn (có mức tin cậy; xem `system-design` bước 5).

### 4. Kiểm lại chi phí sau khi chọn
Cộng chi phí toàn stack (cả phần ẩn: NAT, log, truyền dữ liệu giữa các thành phần đã chọn), so với phần ngân sách; vượt → quay lại bước 1–3 hoặc hỏi người dùng chấp nhận/điều chỉnh. Ghi cách kiểm soát (Budgets, `max_capacity`, retention log, lịch tắt, `destroy`).

### 5. Danh sách tài nguyên đề xuất duyệt
Bảng: dịch vụ AWS, mục đích, kích thước/cấu hình theo môi trường, khác biệt dev/stg/prod, chi phí ước tính, tag, bí mật cần (tên, không giá trị), ghi chú bảo mật. Đây là **danh sách tài nguyên được phép** cho Terraform (tài nguyên ngoài danh sách = không tạo).

### 6. Tự phản biện
Pre-mortem về chi phí và vận hành ("sáu tháng sau hoá đơn vượt gấp 3 vì …?"), điểm lỗi đơn, quota/giới hạn dịch vụ, lock-in, kỹ năng đội, thành phần nào quên tắt.

### 7. Xác nhận và bàn giao
Tóm tắt ≤ 15 dòng (trần, bảng chi phí dự kiến, các quyết định kèm lý do, rủi ro, câu hỏi mở); chờ người dùng xác nhận/phản biện. Sau xác nhận (khi được đồng ý): ADR cho quyết định lớn (`adr/_template.md`), issue hạ tầng (làn hạ tầng, `pipeline-lifecycle.md`), cập nhật `config/services.yaml`/`environments.yaml` nếu cần. Tài liệu đi qua `design-review` (lăng kính chi phí/bảo mật) trước khi dùng cho plan/Terraform.

## Chế độ `compare`
Một năng lực: nêu bài toán và tiêu chí (hỏi ưu tiên nếu chưa rõ), **chi phí trước** (kèm công thức), rồi sáu tiêu chí còn lại, khuyến nghị, vì sao không chọn cái kia, điều kiện đảo ngược, bẫy chi phí/giới hạn. Hỏi 1–2 câu bối cảnh nếu thiếu (tải, ngân sách).

## Chế độ `cost`
Chỉ ước tính: bảng theo thành phần × môi trường × kịch bản (thấp/kỳ vọng/đỉnh), công thức và giả định, phần cố định vs theo mức dùng, chi phí ẩn, chi phí vận hành con người, và 3–5 cách giảm chi phí kèm tác động (chi phí ↔ chất lượng). Không bịa giá.

## Chế độ `review`
Đọc stack/Terraform: (1) chi phí chạy rỗng và tài nguyên tính giờ không cần, (2) thiếu giới hạn (`max_capacity`, retention, lifecycle), (3) bảo mật (public, IAM rộng, mã hoá), (4) độ tin cậy (điểm lỗi đơn, DLQ, timeout, backup), (5) vận hành (ai vá, deploy, debug), (6) dịch vụ đắt khi có phương án rẻ đủ dùng, (7) khoảng trống giữa tài nguyên thực tế và danh sách đã duyệt. Mỗi điểm: mức, bằng chứng, đề xuất; phân biệt chắc chắn với nghi ngờ.

## Không làm
- Không chọn khi chưa có trần chi phí và thông tin tải tối thiểu (hỏi trước).
- Không đưa con số giá không có nguồn; không nói "rẻ/đắt" mà không có công thức.
- Không tự chốt đánh đổi chi phí ↔ độ sẵn sàng/bảo mật; không hy sinh bảo mật vì chi phí khi chưa được quyết.
- Không viết Terraform, không `apply`/`destroy`, không tạo tài nguyên AWS.
- Không tự phá ràng buộc đã chốt (ADR); không tự tạo ADR/issue khi chưa được đồng ý.

## Hoàn thành khi
- [ ] Trần chi phí và phần của service đã chốt (không `TODO`)
- [ ] Mỗi năng lực: ≥ 2 ứng viên có ước tính chi phí (công thức, giả định, nguồn giá hoặc `chưa xác minh`), xếp theo % trần
- [ ] Bảy tiêu chí đã đánh giá, mỗi quyết định có "vì sao không chọn" và tín hiệu xem lại
- [ ] Chi phí đã kiểm lại sau khi chọn, có cách kiểm soát và cách tắt
- [ ] Có danh sách tài nguyên đề xuất duyệt (kèm khác biệt theo môi trường)
- [ ] Đã pre-mortem; nhãn `[Cần]`/`[Chọn để học]` trung thực
- [ ] Người dùng xác nhận hoặc ghi rõ còn chờ gì; tài liệu đúng chỗ, có trong mục lục

## Ghi trạng thái workflow
Nếu chạy trong workflow (bước `system-design`): theo `../workflow/references/state-protocol.md`, ghi state trước khi bắt đầu, sau khi cần người dùng chốt, và khi hoàn thành.
