---
name: plan-review
description: Review kế hoạch triển khai (plan) để tìm điểm chưa rõ, mơ hồ, mâu thuẫn với spec/design/ADR/rule, thiếu bước hoặc thiếu quyết định, yêu cầu người dùng chốt, sửa và review lại đến khi không còn điểm cần xác nhận. Dùng sau plan-init và trước test-init.
---

# Plan review

Đọc `../workflow/references/review-loop.md` trước. Skill này định nghĩa **đối tượng và checklist** của bước review plan.

**Đầu vào:** `document/plans/TB-NNN-*.md` (`draft`) + issue + spec/design `confirmed`. **Đầu ra khi PASS:** plan `status: approved`; Confirm log và Review rounds đầy đủ.

**Nguồn đối chiếu:** spec, design, ADR, issue, `config/*.yaml`, mọi rule kỹ thuật liên quan (`architecture.md`, `api-endpoints.md`, `messaging.md`, `testing.md`, `test-review.md`, `quality-gate.md`), và **code hiện có** trong vùng bị ảnh hưởng (kiểm tra file/module plan nêu có thật).

## Checklist (vòng 1 chạy hết)

### A. Phủ và truy vết
- [ ] Mọi FR/BR/NFR của lát có trong bảng truy vết → bước → kịch bản → test. Thiếu → FIX nếu chỉ chưa điền; CONFIRM `missing` nếu không rõ yêu cầu thuộc lát nào.
- [ ] Không có bước nào không truy được về yêu cầu/quyết định design (việc "tự thêm") → CONFIRM `scope`.
- [ ] Phạm vi "không làm" rõ; không chồng lấn issue khác.

### B. Chất lượng bước
- [ ] Mỗi bước nhỏ, có mục tiêu, kịch bản/test, file dự kiến, tiêu chí kiểm chứng **cụ thể** (lệnh/kết quả).
- [ ] Thứ tự và phụ thuộc đúng; không vòng; khung đi trước, đường lỗi sau đường chính.
- [ ] Bước mơ hồ ("hoàn thiện", "tối ưu", "xử lý lỗi") hoặc quá lớn → CONFIRM `ambiguity` nếu cần người dùng làm rõ hành vi, FIX nếu chỉ cần chia nhỏ/diễn đạt.
- [ ] File/module dự kiến **tồn tại thật** và đúng vị trí theo kiến trúc service (kiểm bằng đọc code, `architecture.md`).

### C. Nhất quán (→ CONFIRM `conflict`)
- [ ] Không mâu thuẫn spec, design, ADR, rule kỹ thuật; không phá kiến trúc bắt buộc của service.
- [ ] Hợp đồng API/sự kiện trong plan khớp `api-endpoints.md`, `messaging.md` và design.
- [ ] Plan và issue (tiêu chí, phạm vi) nhất quán; issue key trùng tên branch/file plan.

### D. Kiểm thử (TDD/BDD)
- [ ] Mỗi bước gắn kịch bản `.feature`; có đường lỗi/biên (idempotency, quyền, trùng, hết hạn).
- [ ] Tầng test và dữ liệu/môi trường hợp lý; không mock che hành vi cần chứng minh.
- [ ] **Công cụ BDD/test runner đã chốt** (`config/tool-versions.yaml`); còn `TODO` → CONFIRM `undecided` (blocker cho test-init).
- [ ] Gate và DoD đầy đủ (`code-change-gate.md`, `quality-gate.md`).

### E. Rủi ro, an toàn, vận hành
- [ ] Migration an toàn, có rollback; không phá consumer (Angular, service khác); thay đổi contract có kế hoạch.
- [ ] Bảo mật, cấu hình/secret, biến môi trường, quan sát được xét đến.
- [ ] Tài liệu/ADR/config cần cập nhật được liệt kê.
- [ ] Hạ tầng (nếu có): `plan` Terraform, chi phí; `apply` cần người dùng đồng ý.

### F. Điểm chưa chốt
- [ ] Mọi mục trong "Câu hỏi mở" và "Giả định" của plan được chuyển thành CONFIRM (không để plan `approved` với câu hỏi mở chưa trả lời).
- [ ] Bất kỳ chỗ plan **tự quyết** hành vi/kỹ thuật không có nguồn → CONFIRM.

### G. Lỗi hình thức (FIX)
Thiếu mục mẫu, bảng sai định dạng, link/tên file sai quy ước, thiếu mục lục.

### H. Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
- [ ] **Chất lượng:** thứ tự theo rủi ro, rollback, migration an toàn, bước kiểm chứng cụ thể, quan sát cho bước mới.
- [ ] **Bảo mật:** có bước bảo mật cụ thể (validate input, kiểm quyền theo tài nguyên, secret từ nguồn đúng) và test tương ứng; cấu hình không lộ.
- [ ] **Chi phí:** môi trường dev/test cấu hình nhỏ; có bước dọn/`destroy`; không tạo tài nguyên tốn tiền ngoài Terraform; tag chi phí; thay đổi hạ tầng có ước tính (`terraform plan`).
- Thiếu → FIX nếu chỉ chưa điền; CONFIRM nếu phải chọn đánh đổi.

## Cách hỏi người dùng
Gom CONFIRM theo lô ≤ 4 câu; câu chặn test-init/coding trước (công cụ, hành vi chưa rõ, phạm vi). Mỗi câu kèm lựa chọn và khuyến nghị (chỉ tham khảo). Nếu vấn đề thực chất thuộc spec/design → nêu rõ và hỏi người dùng có quay lại bước đó không (`workflow`, cổng quay lui).

## Sau khi người dùng trả lời
Ghi Confirm log → cập nhật plan (bước, truy vết, câu hỏi mở đóng lại) → review lại delta, gồm kiểm tra dây chuyền (đổi một bước kéo theo test, hợp đồng, thứ tự).

## PASS khi
- 0 CONFIRM mở, 0 FIX mở, checklist A–G đã kiểm chứng; không còn "Câu hỏi mở" chưa trả lời.
- Plan `status: approved`; Workflow trong issue: `plan-review: PASS`.
- Báo `workflow` mở `test-init`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
