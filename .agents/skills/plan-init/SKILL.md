---
name: plan-init
description: Khởi tạo kế hoạch triển khai cho một issue (một lát giao được) từ spec và design đã xác nhận - chia bước theo TDD/BDD, ánh xạ yêu cầu sang việc, thứ tự, phụ thuộc, kiểm chứng, rủi ro, rollback. Dùng sau design-review PASS và trước plan-review.
---

# Plan init

Tạo `document/plans/TB-NNN-<slug>.md` (mẫu `document/plans/_template.md`) cho **một issue**. Plan nói **làm gì, theo thứ tự nào, kiểm chứng ra sao**; **không** chốt lại quyết định kiến trúc (đã ở design/ADR) và **không** tự quyết điều chưa rõ: điểm chưa rõ ghi thành **Câu hỏi mở** để `plan-review` đưa cho người dùng.

Rule liên quan: `task-workflow.md`, `testing.md`, `test-review.md`, `architecture.md`, `api-endpoints.md`, `backend-engineering.md`, `frontend-engineering.md`, `quality-gate.md`, `docs-conventions.md`, `docs-update-specs.md`.

## Đầu vào và điều kiện
- Spec `confirmed`, design `confirmed` (nếu tính năng có design). Chưa → quay lại bước trước; **không** lập plan trên spec/design chưa PASS.
- Issue `TB-NNN` tồn tại và có tiêu chí hoàn thành. Chưa có → đề xuất chia issue từ spec/design (mỗi issue là một lát giao được, nêu phụ thuộc) và **chờ người dùng đồng ý** trước khi tạo; branch `feature/TB-NNN-<slug>` theo `git-workflow.md` (không push).
- Đọc spec, design, ADR liên quan, `config/services.yaml`, code hiện có trong vùng bị ảnh hưởng (không đoán cấu trúc).

## Quy trình
0. **Discovery và nền** (`../workflow/references/generation-principles.md` mục 2): tìm bằng Grep/Glob code, test, contract, ADR liên quan (không đoán tên file/API). Chụp **trạng thái branch** làm ngữ cảnh: HEAD, merge-base với `main`, working tree sạch hay có thay đổi dở. Đã có plan của issue → **cập nhật tại chỗ**, không tạo bản trùng. Phạm vi lớn hoặc thiếu spec → hỏi người dùng xác nhận phạm vi **trước** khi ghi plan.
1. **Xác định phạm vi lát:** FR/BR/NFR và quyết định design thuộc issue này; điều **không** làm trong lát (ghi rõ).
2. **Bảng truy vết:** mỗi FR/BR/NFR của lát → bước kế hoạch → kịch bản (`@Sxx`) → test dự kiến. Yêu cầu không có bước = lỗ hổng.
3. **Chia bước theo TDD/BDD**, mỗi bước là một chu kỳ nhỏ (Đỏ → Xanh → Refactor) gắn với **kịch bản** cụ thể. Sắp xếp theo giá trị và phụ thuộc: khung đi trước (contract, schema), rồi đường chính, rồi đường lỗi/biên.
4. **Với mỗi bước ghi:** mục tiêu, kịch bản/test sẽ viết, file/module dự kiến chạm (đọc code để xác định, không đoán), hợp đồng thay đổi (API/sự kiện), migration, cấu hình/biến môi trường, tài liệu cần cập nhật, tiêu chí **kiểm chứng** (lệnh/kết quả quan sát được).
5. **Chiến lược test:** tầng nào (unit/integration/contract), dữ liệu/ môi trường (Testcontainers/LocalStack), cái gì mock/không mock, **công cụ BDD/test runner** (đọc `config/tool-versions.yaml`; nếu `TODO` → ghi vào Câu hỏi mở, không tự chọn).
6. **Rủi ro và rollback:** rủi ro kỹ thuật, migration an toàn, cách quay lại, ảnh hưởng consumer (Angular, service khác).
7. **Gate và Definition of Done:** gate nào (`code-change-gate.md`, `quality-gate.md`), tiêu chí hoàn thành của issue, tài liệu/ADR/config cập nhật.
8. **Câu hỏi mở và giả định:** mọi điểm chưa rõ khi lập plan (hành vi không có trong spec, lựa chọn kỹ thuật chưa chốt, thiếu thông tin hạ tầng) → ghi vào bảng, **không tự điền**.
9. Plan `status: draft`; cập nhật bảng Workflow trong issue (`plan-init: done`); báo `workflow` mở `plan-review`.

## Quy tắc
- Plan bám spec/design: không thêm yêu cầu, không đổi hành vi. Thấy spec/design thiếu hoặc sai → ghi Câu hỏi mở và đề xuất quay lại bước tương ứng.
- Bước đủ nhỏ để một lượt coding hoàn thành và kiểm chứng (nếu quá lớn → chia).
- Mỗi bước kiểm chứng được; không có bước kiểu "hoàn thiện" mơ hồ.
- Không viết code; chỉ ví dụ ngắn nếu cần làm rõ.
- Quyết định kỹ thuật trong plan phải có **bằng chứng** (đường dẫn spec/ADR/code); không có thì là Câu hỏi mở, không ghi như đã quyết.
- Mỗi bước tuân cỡ bước (`generation-principles.md` mục 5): không khổng lồ; nếu bước cần nhiều test thực thi, nêu cách sinh (một lần hay theo từng bước).
- Không tuyên bố "sẵn sàng code" ở bước này; đó là kết quả của `plan-review` PASS.
- **Tra bài học** (`document/lessons/README.md`, theo tag của vùng/service): lesson liên quan phải được phản ánh trong plan (bước kiểm, kịch bản, rủi ro). Ghi tên lesson đã dùng vào mục Rủi ro.

## Hoàn thành khi
- [ ] Plan đủ mục mẫu, đặt đúng chỗ/tên, có trong `document/plans/README.md`
- [ ] Bảng truy vết phủ toàn bộ FR/BR/NFR của lát
- [ ] Mỗi bước có kịch bản/test, kiểm chứng, file dự kiến
- [ ] Câu hỏi mở và giả định được liệt kê, không tự chốt
- [ ] Issue có bảng Workflow cập nhật

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
