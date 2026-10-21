---
name: spec-review
description: Review một spec để tìm điểm chưa rõ, mơ hồ, mâu thuẫn, thiếu thông tin và thiếu quyết định, yêu cầu người dùng chốt, rồi sửa và review lại đến khi không còn điểm cần xác nhận. Dùng sau khi spec đã soạn (requirements-discovery) và trước system design.
---

# Spec review

Đọc `../workflow/references/review-loop.md` trước: giao thức vòng lặp, phân loại CONFIRM/FIX, cách hỏi người dùng, Confirm log, điều kiện PASS. Skill này chỉ định nghĩa **đối tượng và checklist** của bước review spec.

**Đầu vào:** `document/spec/<ten>.md` (trạng thái `draft`). **Đầu ra khi PASS:** `status: confirmed` trong front matter, bảng Confirm log và Review rounds đầy đủ.

**Nguồn đối chiếu (đọc trước):** yêu cầu gốc trong spec, `references/gap-checklist.md` của `requirements-discovery`, các spec liên quan, `document/architecture/README.md` và ADR, `config/services.yaml`, `api-endpoints.md` / `architecture.md` nếu spec chạm API hay ranh giới service.

**Theo làn** (`../workflow/references/lanes.md`): làn **lớn** review đủ checklist, tối đa 5 vòng. Làn **vừa**: spec ngắn trong issue là đủ (không bắt buộc đủ mẫu `_template.md`, mục F bỏ phần "đủ mục của mẫu"); chỉ kiểm A (mục tiêu, phạm vi trong/ngoài, luồng lỗi chính), B, D, D2 và phần bảo mật của G; tối đa 3 vòng. Làn nhỏ không có bước này.

## Checklist (vòng 1 chạy hết, trừ khi làn vừa như trên)

### A. Đầy đủ (mỗi mục thiếu/không rõ → CONFIRM `missing`)
- [ ] Đủ các mục của mẫu `document/spec/_template.md`; mục không áp dụng có lý do.
- [ ] 16 chiều của gap-checklist đều được trả lời hoặc ghi giả định/câu hỏi mở có người xác nhận.
- [ ] Mục tiêu, thước đo thành công, người dùng/vai trò, phạm vi **trong và ngoài**.
- [ ] Luồng chính, luồng thay thế, luồng lỗi và trường hợp biên (đồng thời, trùng, rỗng, hết hạn, huỷ giữa chừng, quyền).
- [ ] Phân quyền và dữ liệu nhạy cảm; tích hợp/sự kiện; NFR **có số đo**.

### B. Rõ ràng (→ CONFIRM `ambiguity`)
- [ ] Từ định tính không có số đo ("nhanh", "dễ dùng", "nhiều", "linh hoạt").
- [ ] Từ bao quát ("tất cả", "luôn", "không bao giờ").
- [ ] Chủ thể ẩn ("hệ thống tự động...": ai/khi nào/theo quy tắc nào?).
- [ ] Thuật ngữ dùng không nhất quán hoặc chưa định nghĩa.
- [ ] Yêu cầu có thể hiểu theo hai cách; ví dụ minh hoạ thiếu hoặc mâu thuẫn với mô tả.
- [ ] Yêu cầu thực chất là giải pháp kỹ thuật ("dùng Redis") thay vì nhu cầu.

### C. Nhất quán (→ CONFIRM `conflict`)
- [ ] FR, BR, NFR, phạm vi và luồng không mâu thuẫn nhau (ví dụ "xoá được" và "giữ đầy đủ lịch sử").
- [ ] Không mâu thuẫn spec khác, ADR, kiến trúc hiện có, ràng buộc đã chốt (ADR 0005, quy ước API).
- [ ] Ưu tiên MoSCoW hợp lý (không quá nhiều Must); phụ thuộc giữa các FR không vòng tròn.

### D. Kiểm chứng được và truy vết
- [ ] Mỗi FR/BR/NFR có ID, ưu tiên và **tiêu chí chấp nhận kiểm chứng được** (có thể thành kịch bản Given/When/Then). Thiếu/không kiểm chứng được → CONFIRM `missing`.
- [ ] Mỗi yêu cầu truy được về lời khách/người dùng hoặc được đánh dấu giả định; yêu cầu "tự nhiên có" mà không có nguồn → CONFIRM `assumption`.
- [ ] "Yêu cầu gốc" còn nguyên văn.
- [ ] **Truy vết yêu cầu → test:** mỗi FR/BR/NFR `Must`/`Should` có tiêu chí Given/When/Then (ít nhất một ca chính và một ca lỗi/biên) đủ để thành `.feature`/test sau này; tiêu chí không có ví dụ số liệu cụ thể → CONFIRM `missing`. Ghi vào báo cáo bảng "yêu cầu → tiêu chí" (không cần tạo file mới).
- [ ] Yêu cầu / giả định / ràng buộc tách bạch; mỗi câu hỏi mở và giả định có owner, hạn, mặc định (thiếu → CONFIRM `missing`). Mỗi câu hỏi mở còn phải có **2–4 lựa chọn, khuyến nghị kèm lý do, hậu quả nếu chưa chốt**: thiếu mà agent tự bổ sung được từ nguồn đã có (spec, ADR, design) → FIX; cần thông tin từ người dùng để có lựa chọn → CONFIRM `missing`. Câu hỏi đã được chốt ở bước sau mà vẫn ghi `open` → FIX (chuyển `answered` kèm link).

### D2. Sẵn sàng để ước lượng (DoR, `../workflow/SKILL.md` mục DoR/DoD)
- [ ] Đủ rõ để người làm **ước lượng được dạng khoảng**: phạm vi trong/ngoài, FR Must có tiêu chí, phụ thuộc/ tích hợp đã biết, không còn câu hỏi mở `blocker` chưa có owner/hạn.
- [ ] Chưa đạt → CONFIRM `undecided`/`missing` nêu rõ cái gì cản trở estimate; không PASS chỉ để "có spec".

### E. Phạm vi và rủi ro
- [ ] Không phình phạm vi so với yêu cầu gốc; phần "tiện thể thêm" → CONFIRM `scope`.
- [ ] Giả định lớn chưa xác nhận, câu hỏi mở chặn thiết kế → CONFIRM `assumption`/`undecided`.
- [ ] Quyết định kiến trúc/công nghệ ẩn trong spec → CONFIRM `undecided` (chuyển sang system design/ADR).

### F. Lỗi hình thức (FIX)
Thiếu mục bắt buộc của mẫu, ID trùng/nhảy, link chết, front matter thiếu, chính tả, định dạng bảng, tên file sai quy ước (`docs-conventions.md`).

### G. Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
- [ ] **Chất lượng:** NFR có số đo (độ trễ, tải, sẵn sàng, khôi phục, độ bền dữ liệu); tiêu chí chấp nhận kiểm chứng được.
- [ ] **Bảo mật:** vai trò/quyền, dữ liệu nhạy cảm/PII, yêu cầu audit và tuân thủ (`config/project.yaml` → `quality.compliance`), yêu cầu xác thực.
- [ ] **Chi phí:** trần ngân sách (`quality.monthlyBudgetUsd`); yêu cầu có chi phí vận hành lớn (độ sẵn sàng cao, đa vùng, lưu trữ dài) có thật sự cần không.
- Thiếu mục nào → CONFIRM `missing`; bảo mật mặc định `blocker`.

## Ngoài đời thực
Review spec trong dự án thật là buổi "backlog refinement"/three amigos (PO, dev, tester) trước khi đưa việc vào sprint: dev hỏi "làm sao biết xong?", tester hỏi "ca lỗi đâu?", cả nhóm kiểm "đủ để estimate chưa?". Skill này mô phỏng ba góc nhìn đó; câu nào agent không trả lời được từ spec thì là CONFIRM cho người dùng (đóng vai khách/PO).

## Cách hỏi người dùng
Gom CONFIRM theo lô ≤ 4 câu. Mỗi câu: nêu bằng chứng ngắn (trích spec), câu hỏi cụ thể, 2–4 lựa chọn, khuyến nghị kèm lý do (chỉ tham khảo). Ưu tiên câu chặn thiết kế trước (mục tiêu, phạm vi, quy tắc cốt lõi, NFR), rồi mâu thuẫn, rồi chi tiết.

## Sau khi người dùng trả lời
Ghi Confirm log → sửa spec đúng quyết định (cập nhật FR/BR/NFR, giả định, câu hỏi mở, lịch sử thay đổi; giữ ID) → review lại delta. Quyết định làm đổi hành vi spec đã confirm trước đó → hỏi lại tác động (issue/design đã có).

## PASS khi
- 0 CONFIRM mở, 0 FIX mở; mọi mục A–F áp dụng đã kiểm chứng.
- `status: confirmed`; Review rounds ghi vòng cuối `PASS`.
- Giả định còn lại đều đã được người dùng xác nhận hoặc ghi `Hoãn` có người chịu trách nhiệm.
- Báo `workflow` mở bước `system-design`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
