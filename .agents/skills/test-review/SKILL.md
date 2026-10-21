---
name: test-review
description: Review bộ test BDD/TDD (file .feature và test thực thi, trạng thái đỏ) để tìm điểm chưa rõ, mơ hồ, thiếu kịch bản, mâu thuẫn với spec/plan, yêu cầu người dùng chốt, sửa và review lại đến khi không còn điểm cần xác nhận. Dùng sau test-init và trước coding-start.
---

# Test review

Đọc `../workflow/references/review-loop.md` trước (vòng lặp, CONFIRM/FIX, Confirm log, PASS). Chuẩn chi tiết cho `.feature` và test thực thi nằm ở **rule `test-review.md`** (mục 1–7): skill này là quy trình chạy bước review đó **trong workflow**, thêm cách phân loại CONFIRM/FIX.

**Đầu vào:** `.feature` + test thực thi + kết quả chạy đỏ (từ `test-init`), plan `approved`, spec, issue. **Đầu ra khi PASS:** Workflow ghi `test-review: PASS`; tag `@wip` không còn; bảng kịch bản ↔ test đầy đủ.

**Nguồn đối chiếu:** spec (tiêu chí chấp nhận), plan (bảng truy vết), design (chế độ lỗi/hợp đồng), `api-endpoints.md`, `messaging.md`, `testing.md`, `quality-gate.md`.

## Checklist (vòng 1 chạy hết)

### A. Chất lượng `.feature` (theo `test-review.md` mục 1–3)
- [ ] Khai báo, ngôn ngữ nghiệp vụ, không lộ chi tiết kỹ thuật; một `When`; `Given` chỉ lập trạng thái.
- [ ] Tag đúng (`@TB-NNN`, `@Sxx`, service); không `@wip`.
- [ ] Dữ liệu mẫu cụ thể; dùng `Background`/`Scenario Outline` hợp lý; step nhất quán.
- Lỗi hình thức/style rõ ràng → FIX. Kịch bản mơ hồ về *hành vi mong đợi* → CONFIRM `ambiguity`.

### B. Phủ yêu cầu
- [ ] Mỗi FR/BR/NFR của lát có ≥ 1 kịch bản; đường lỗi và biên: validation, quyền/IDOR, trùng lặp/idempotency, đồng thời, hết hạn, lỗi phụ thuộc, dữ liệu rỗng/lớn.
- [ ] Không có kịch bản "tự thêm" ngoài spec (→ CONFIRM `scope`: giữ, bỏ hay cập nhật spec?).
- [ ] Kịch bản thiếu mà spec không đủ để suy ra hành vi → CONFIRM `missing` (hỏi hành vi mong đợi), **không tự viết hành vi**.

### C. Đúng với spec/plan (→ CONFIRM `conflict`)
- [ ] Kịch bản không mâu thuẫn spec, plan, design, quy ước API/sự kiện (mã lỗi, status, trường, thông điệp).
- [ ] Hai kịch bản không mâu thuẫn nhau.

### D. Test thực thi và trạng thái đỏ (`test-review.md` mục 5)
- [ ] Mỗi kịch bản có test thực thi tương ứng (bảng reconcile); gap → FIX nếu thiếu đơn thuần, CONFIRM nếu không rõ nên test thế nào.
- [ ] **Chạy lại test**: đỏ **đúng lý do** (assertion/hành vi chưa có), không đỏ vì lỗi biên dịch, thiếu cấu hình hay môi trường. Đỏ sai lý do → FIX.
- [ ] Assert trạng thái thật (DB, thông điệp phát ra), không chỉ status code; integration dùng DB thật; mock chỉ ở biên ngoài; test độc lập, deterministic; không skip/ignore/assert rỗng.
- [ ] Test không chứa logic production; stub rỗng không có hành vi.
- [ ] Gate tĩnh của phần test (format/lint) xanh theo `quality-gate.md`.

### E. Công cụ và phạm vi
- [ ] Test/BDD runner đã chốt trong `config/tool-versions.yaml` (nếu còn `TODO` → CONFIRM `undecided`, blocker).
- [ ] Phạm vi test (targeted/full) và lệnh chạy ghi rõ trong issue.

### F. Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
- [ ] **Chất lượng:** assert trạng thái thật, đường lỗi/biên, test ổn định/độc lập, đỏ đúng lý do.
- [ ] **Bảo mật:** có kịch bản/test cho authZ và IDOR, validation/biên đầu vào, không rò rỉ thông tin trong lỗi/log, quyền theo vai trò, token hết hạn/giả. Thiếu khi spec/design có yêu cầu bảo mật → FIX/CONFIRM `blocker`.
- [ ] **Chi phí:** thời gian chạy test và chi phí hạ tầng test hợp lý; dùng LocalStack/container thay dịch vụ AWS thật khi đủ.

## Cách hỏi người dùng
Gom CONFIRM theo lô ≤ 4 câu; ưu tiên câu về **hành vi mong đợi** và **công cụ**. Với mỗi câu: nêu kịch bản/spec liên quan, các hành vi có thể, khuyến nghị (chỉ tham khảo). Hành vi chốt xong → cập nhật **spec trước** (nguồn hiện hành; `docs-update-specs.md`), rồi `.feature`/test.

## Sau khi người dùng trả lời
Ghi Confirm log → cập nhật spec (nếu là hành vi), plan (nếu đổi bước), `.feature`, test; chạy test lại để xác nhận vẫn **đỏ đúng lý do** → review lại delta. Ghi vòng vào mục **Test review rounds** của issue (theo `test-review.md` mục 4, 7).

## PASS khi
- 0 CONFIRM mở, 0 FIX mở; P3 theo cổng disposition của `test-review.md` mục 4 (sửa hoặc bỏ qua có lý do do người dùng quyết định).
- Mọi kịch bản có test thực thi; test **đỏ đúng lý do**; không `@wip`/skip.
- Issue: bảng Kịch bản và TDD log đủ; Workflow `test-review: PASS`.
- Báo `workflow` mở `coding-start`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
