# Bài học kinh nghiệm (lessons learned)

Lý do tồn tại: lỗi đã gặp không được lặp lại. Sổ bài học ở `document/lessons/` (mục lục có tag, mẫu, quy trình trong skill `lesson-learned`). Quy tắc này nói **khi nào ghi, khi nào đọc, và thế nào là "xong"**.

## 1. Đọc trước khi làm (tra theo tag)

- Đầu task, đọc **bảng mục lục** `document/lessons/README.md` và chỉ mở các lesson có **tag khớp vùng đang làm** (service, `security`, `cost`, `terraform`/`infra`, `api`, `testing`...). Không đọc hết.
- Lesson liên quan phải được tính vào plan/test/code (ví dụ thêm kịch bản, thêm bước kiểm). Nếu lesson cho biết lỗi tương tự có thể lặp lại, nêu rõ trong báo cáo.
- Gặp lỗi: tìm xem đã có lesson cho nó chưa (`Grep` tên lỗi/tag). Có rồi mà vẫn lặp lại = **tái diễn** (xem mục 3).

## 2. Ghi khi nào (trigger)

Ghi (hoặc đề xuất ghi) một lesson khi xảy ra bất kỳ điều nào sau:

| Tình huống | Ví dụ |
|---|---|
| Review tìm ra lỗi **P0–P2** có nguyên nhân hệ thống (không phải typo) | Thiếu kiểm quyền, nuốt lỗi, hardcode |
| Test/gate fail vì **giả định sai** hoặc quy trình thiếu | Giả định tool miễn phí nhưng tính phí |
| **Phải làm lại** (rework) đáng kể | Phải viết lại spec vì hiểu sai yêu cầu |
| **Người dùng sửa agent** ("tôi đã bảo...", "đừng làm...") | Agent bỏ qua một rule/ràng buộc |
| Sự cố khi chạy/triển khai, chi phí bất ngờ, phát hiện bảo mật | Hoá đơn tăng, secret lộ |
| **Suýt xảy ra** (near-miss) có tác động tiềm ẩn lớn | Phát hiện kịp trước khi merge |

Không ghi: lỗi gõ nhầm tầm thường, việc đã có rule rõ ràng và chỉ là quên một lần không đáng kể (khi đó chỉ sửa), thông tin bí mật.

Khi nào: **ngay khi phát hiện** (bản nháp ngắn), và **khi đóng issue** (tổng kết, mục 4 của `pipeline-lifecycle.md`). Agent đề xuất; người dùng xác nhận trước khi chốt trạng thái.

## 3. Thế nào là "xong" (phòng ngừa phải mã hoá)

- Lesson **không hoàn tất** chỉ với phần mô tả. Bắt buộc có ≥ 1 **biện pháp phòng ngừa** thuộc loại sau, ưu tiên từ trên xuống: **tự động hoá** (test, lint/analyzer, script, gate) > **rule/gate trong `.agents/rules`** > **mẫu/checklist** (template, checklist review) > **ADR/spec** > **chỉ nhắc nhở** (cuối cùng, phải có lý do vì dễ quên).
- Có **cách kiểm chứng** biện pháp hiệu quả (test tái hiện hoặc kịch bản thử).
- Trạng thái: `open` → `partial` → `prevented`. `recurred` khi lỗi lặp lại: tăng `recurrence`, **nâng cấp biện pháp** (thường là tự động hoá), cập nhật lesson, không chỉ ghi thêm một dòng.
- Lesson lặp lại ≥ 2 lần hoặc `severity: high` → **thăng cấp** thành rule hoặc gate cố định (và liên kết lại).

## 4. Chất lượng

- **Không đổ lỗi**, nói về hệ thống/quy trình. Một câu bài học mệnh lệnh ở đầu file.
- **Nguyên nhân gốc** (5 câu hỏi "vì sao") dừng ở quy trình/công cụ, không dừng ở "ai quên".
- **Tránh trùng:** tìm lesson cũ trước khi tạo mới; trùng thì cập nhật/ghép.
- Ngắn (≤ 1 trang), có tag đúng từ vựng, có liên kết issue/commit/rule bị ảnh hưởng.
- Không ghi secret, dữ liệu cá nhân, chi tiết khai thác lỗ hổng có thể bị lạm dụng.
- Cập nhật mục lục `document/lessons/README.md` trong cùng thay đổi.

## 5. Phối hợp với quy trình

- `plan-init`, `coding-start`: tra lesson theo tag trước khi lập plan/code.
- Review (`code-review.md`, các skill review): finding P0–P2 có nguyên nhân hệ thống → đề xuất lesson.
- Đóng issue/PR (`workflow`): bước tổng kết bài học (hỏi "có gì sai/đáng nhớ không?").
- Biện pháp phòng ngừa chạm rule/skill/gate → làm trong một issue/commit rõ ràng theo `git-workflow.md`.
