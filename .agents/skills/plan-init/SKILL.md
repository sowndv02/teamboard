---
name: plan-init
description: Chia spec/design lớn thành các lát nhỏ giao được (chế độ slices) rồi khởi tạo kế hoạch triển khai cho một issue (một lát giao được) từ spec và design đã xác nhận - chia bước theo TDD/BDD, ánh xạ yêu cầu sang việc, thứ tự, phụ thuộc, kiểm chứng, rủi ro, rollback. Dùng sau design-review PASS và trước plan-review.
---

# Plan init

Tạo `document/plans/TB-NNN-<slug>.md` (mẫu `document/plans/_template.md`) cho **một issue**. Plan nói **làm gì, theo thứ tự nào, kiểm chứng ra sao**; **không** chốt lại quyết định kiến trúc (đã ở design/ADR) và **không** tự quyết điều chưa rõ: điểm chưa rõ ghi thành **Câu hỏi mở** để `plan-review` đưa cho người dùng.

Rule liên quan: `task-workflow.md`, `testing.md`, `test-review.md`, `architecture.md`, `api-endpoints.md`, `backend-engineering.md`, `quality-gate.md`, `docs-conventions.md`, `docs-update-specs.md`.

## Hai chế độ

- **`slices`** (bước 4b, theo tính năng): chia spec/design lớn thành bảng lát `document/plans/<feature>-slices.md` theo `../workflow/references/slicing.md` (tiêu chí lát, mẫu bảng, quy trình, hỏi người dùng duyệt). Không viết plan chi tiết ở chế độ này. Xong khi bảng `approved`.
- **`plan`** (bước 5, theo issue): mô tả dưới đây; đầu vào là **một hàng của bảng lát**.

## Đầu vào và điều kiện
- Spec `confirmed`, design `confirmed` (nếu tính năng có design). Chưa → quay lại bước trước; **không** lập plan trên spec/design chưa PASS.
- Tính năng lớn: bảng lát `approved` và lát này là **lát kế tiếp** (các lát trước `done` hoặc người dùng cho làm song song). Chưa có bảng → chạy chế độ `slices` trước; **không** lập một plan phủ cả spec. Plan vượt cỡ lát (nhiều hơn ~3 ngày hoặc ~5 kịch bản chính) → dừng, đề xuất chia lát lại và hỏi người dùng.
- Issue `TB-NNN` tồn tại và có tiêu chí hoàn thành. Chưa có → tạo từ hàng của bảng lát (hoặc, với việc nhỏ không có bảng, đề xuất một issue) và **chờ người dùng đồng ý** trước khi tạo; branch `feature/TB-NNN-<slug>` theo `git-workflow.md` (không push).
- Đọc spec, design, ADR liên quan, `config/services.yaml`, code hiện có trong vùng bị ảnh hưởng (không đoán cấu trúc).

## Quy trình
0. **Discovery và nền** (`../workflow/references/generation-principles.md` mục 2): tìm bằng Grep/Glob code, test, contract, ADR liên quan (không đoán tên file/API). Chụp **trạng thái branch** làm ngữ cảnh: HEAD, merge-base với `main`, working tree sạch hay có thay đổi dở. Đã có plan của issue → **cập nhật tại chỗ**, không tạo bản trùng. Phạm vi lớn hoặc thiếu spec → hỏi người dùng xác nhận phạm vi **trước** khi ghi plan.
1. **Xác định phạm vi lát:** FR/BR/NFR và quyết định design thuộc issue này; điều **không** làm trong lát (ghi rõ).
2. **Bảng truy vết:** mỗi FR/BR/NFR của lát → bước kế hoạch → kịch bản (`@Sxx`) → test dự kiến. Yêu cầu không có bước = lỗ hổng.
3. **Chia bước theo TDD/BDD**, mỗi bước là một chu kỳ nhỏ (Đỏ → Xanh → Refactor) gắn với **kịch bản** cụ thể. Chia theo **lát dọc (vertical slice)** giao được giá trị chạy end-to-end (một hành vi người dùng đi từ API tới DB), không chia theo lớp (tất cả entity, rồi tất cả repository). Sắp xếp theo giá trị và phụ thuộc: khung tối thiểu (contract, schema) đi trước, rồi đường chính, rồi đường lỗi/biên. Việc chưa muốn lộ ra người dùng đặt sau feature flag để merge sớm được.
4. **Với mỗi bước ghi:** mục tiêu, kịch bản/test sẽ viết, file/module dự kiến chạm (đọc code để xác định, không đoán), hợp đồng thay đổi (API/sự kiện), migration, cấu hình/biến môi trường, tài liệu cần cập nhật, **tiêu chí hoàn thành của bước** và tiêu chí **kiểm chứng** (lệnh/kết quả quan sát được). Mỗi bước nên là một commit/PR nhỏ có thể review độc lập (gợi ý dưới ~400 dòng diff); bước lớn hơn thì chia.
5. **Chiến lược test:** theo **kim tự tháp test**: nhiều unit nhanh cho logic, ít integration cho biên (DB, hàng đợi), **contract test** cho API/event giữa service, rất ít e2e cho luồng sống còn; dữ liệu/môi trường (Testcontainers/LocalStack), cái gì mock/không mock, **công cụ BDD/test runner** (đọc `config/tool-versions.yaml`; nếu `TODO` → ghi vào Câu hỏi mở, không tự chọn).
6. **Rủi ro, rollout và rollback:** rủi ro kỹ thuật, ảnh hưởng consumer (service khác). **Bắt buộc** khi plan đổi DB hoặc contract: migration theo expand/contract (từng pha là một bước deploy riêng, quay lại được), **thứ tự deploy producer/consumer**, feature flag/canary nếu có, cách rollback và tín hiệu để quyết định rollback.
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

## Làn và ngoài đời thực
- Làn vừa: plan ngắn (một bảng bước + truy vết + rủi ro/rollback, có thể nằm trong issue); làn nhỏ không có plan riêng (`../workflow/references/lanes.md`).
- Ngoài đời thực: đội thường không viết plan dài; họ chia ticket thành các lát nhỏ merge được mỗi ngày, kèm tiêu chí chấp nhận, và nêu sớm phụ thuộc/thứ tự deploy. Plan tốt = người khác đọc 5 phút biết làm gì trước, kiểm chứng ra sao, hỏng thì lùi thế nào.

## Hoàn thành khi
- [ ] Plan đủ mục mẫu, đặt đúng chỗ/tên, có trong `document/plans/README.md`
- [ ] Bảng truy vết phủ toàn bộ FR/BR/NFR của lát
- [ ] Mỗi bước có kịch bản/test, kiểm chứng, file dự kiến
- [ ] Câu hỏi mở và giả định được liệt kê, không tự chốt
- [ ] Issue có bảng Workflow cập nhật

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
