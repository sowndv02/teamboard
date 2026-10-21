---
name: design-review
description: Review một design doc (system design) để tìm điểm chưa rõ, mơ hồ, mâu thuẫn, quyết định chưa chốt, thiếu phương án hay thiếu lý do không chọn, rồi yêu cầu người dùng chốt, sửa và review lại đến khi không còn điểm cần xác nhận. Dùng sau system-design và trước plan.
---

# Design review

Đọc `../workflow/references/review-loop.md` trước: giao thức vòng lặp, CONFIRM/FIX, Confirm log, điều kiện PASS. Skill này định nghĩa **đối tượng và checklist** của bước review design.

**Đầu vào:** `document/design/<ten>.md` (`draft`) + spec `confirmed` tương ứng. **Đầu ra khi PASS:** design `status: confirmed`, Confirm log và Review rounds đầy đủ.

**Nguồn đối chiếu:** spec, `document/architecture/README.md`, mọi ADR, `config/services.yaml`, rule `architecture.md`, `api-endpoints.md`, `messaging.md`, `backend-engineering.md`, `observability.md`, `terraform.md`; `system-design/references/decision-framework.md` và `design-checklist.md`.

## Checklist (vòng 1 chạy hết)

### A. Phủ yêu cầu (→ CONFIRM `missing`/FIX)
- [ ] Mỗi FR then chốt và **mọi NFR** của spec được thiết kế xử lý (bảng truy vết FR/NFR → thành phần/quyết định). Yêu cầu không được phủ → CONFIRM.
- [ ] NFR trong design khớp số của spec; số do design tự đặt phải ghi là **giả định** và chờ xác nhận (CONFIRM `assumption`).
- [ ] Design không thêm yêu cầu mà spec không có (CONFIRM `scope`).

### B. Chất lượng quyết định
- [ ] Mỗi quyết định lớn có **≥ 2 phương án thật** (gồm phương án đơn giản nhất) → thiếu: FIX nếu chỉ thiếu trình bày, CONFIRM nếu phương án thay thế có thể đổi hướng.
- [ ] Có **bảng so sánh theo tiêu chí lấy từ NFR**, khuyến nghị, **"Vì sao không chọn B/C" và điều kiện B/C đúng**, đánh đổi, khả năng đảo ngược, **tín hiệu xem lại đo được**.
- [ ] Nhãn `[Cần]`/`[Chọn để học]` trung thực; không biến lựa chọn "để học" thành lý do kỹ thuật giả (CONFIRM nếu không rõ).
- [ ] **Bám thực tế:** mỗi mục `[Chọn để học]` ghi cách dự án thực tế thường làm và mức xa thực tế (Gần/Xa). Mức *Xa* (tự viết thứ đã có thư viện chuẩn như mật mã/giao thức/JWT, hạ tầng nặng hơn nhiều so với tải) mà chưa có xác nhận rõ của người dùng, hoặc nằm trên đường chính của sản phẩm → CONFIRM `scope` (đề xuất phương án *Gần*). Mục đích học không phải lý do chấp nhận phương án xa thực tế.
- [ ] **Quyết định chưa được đưa ra** hoặc "tuỳ" ("có thể dùng A hoặc B") → CONFIRM `undecided`.
- [ ] Lựa chọn phụ thuộc vào ưu tiên của người dùng (chi phí vs độ trễ, đơn giản vs mở rộng) chưa hỏi → CONFIRM.

### B2. Quyết định cấp service (D0)
- [ ] Có D0 (ranh giới, cô lập database, loại database, kiến trúc nội bộ) khi design service mới hoặc chạm lưu trữ/ranh giới; mỗi mục ≥ 2 phương án thật và "vì sao không chọn". Thiếu D0 → CONFIRM `missing` mức `blocker` (không phải FIX).
- [ ] Ràng buộc kế thừa (ADR, `PROJECT.md`) **không thay cho lý do**. ADR chỉ ghi kết quả mà không có lý do → design ghi "ràng buộc chưa có lý do" và đề xuất bổ sung ADR; tự bịa lý do cho hợp thức hoá → CONFIRM.
- [ ] Loại database: tiêu chí rút từ yêu cầu dữ liệu (unique, giao dịch/CAS, truy vấn, hết hạn, chi phí biên, chạy local); phần yếu và phương án thay thế nghiêm túc nhất được nêu; "đa dạng công nghệ" hay sở thích không được dùng làm lý do kỹ thuật (nếu là lý do học thì phải gắn `[Chọn để học]`).
- [ ] Kiến trúc nội bộ: so với ≥ 2 kiến trúc khác, nêu điểm yếu và cách giảm.
- [ ] Nhóm khó đảo ngược (loại DB, ranh giới service, kiến trúc nội bộ, hợp đồng công khai): Confirm log có **người dùng chọn hoặc xác nhận**. Mục ghi "Agent chọn theo uỷ quyền" mà chưa có xác nhận kết quả cụ thể → CONFIRM `assumption`; quyết định đó làm đổi ADR/spec đã chốt thì tạo đề xuất ADR.

### C. Nhất quán và ràng buộc (→ CONFIRM `conflict`)
- [ ] Không mâu thuẫn ADR đã chấp nhận, kiến trúc bắt buộc từng service (`architecture.md`), tech stack đã chốt. Muốn phá ràng buộc → CONFIRM (đề xuất ADR mới).
- [ ] Quyền sở hữu dữ liệu: mỗi service chỉ truy cập DB của mình; không truy cập chéo.
- [ ] Hợp đồng API/sự kiện theo `api-endpoints.md` và `messaging.md` (outbox, idempotent, DLQ, envelope, `correlationId`).
- [ ] Ranh giới giao dịch và nhất quán cuối cùng rõ ràng; không dual write.
- [ ] Hình dạng sơ đồ, bảng và văn bản khớp nhau (tên service, tên sự kiện, đường dẫn).

### D. Độ tin cậy, an toàn, vận hành
- [ ] Chế độ lỗi: phụ thuộc chậm/chết, message trùng/mất/sai thứ tự, hỏng giữa chừng, hai bên ghi cùng lúc, poison message.
- [ ] Không có điểm lỗi đơn chưa được nêu; có retry/timeout/circuit breaker cho call ngoài.
- [ ] Bảo mật: authN/authZ, IDOR, dữ liệu nhạy cảm, bí mật.
- [ ] Quan sát (trace/metric/log/cảnh báo) cho luồng chính và lỗi; chỉ số cho từng revisit trigger.
- [ ] NFR có SLI/SLO (và error budget) cho luồng chính; cảnh báo gắn SLO. Thiếu → CONFIRM `missing`.
- [ ] Triển khai, migration, rollback; chi phí AWS ước tính và cách tắt khi không dùng.
- [ ] **Tương thích ngược:** đổi API/event có versioning/chính sách bỏ dùng; đổi schema theo expand/contract (từng pha deploy độc lập, quay lại được); thứ tự deploy producer/consumer rõ.
- [ ] **Rollout/rollback:** thay đổi rủi ro cao có feature flag/canary, điều kiện và cách rollback; phiên bản cũ đọc được dữ liệu mới.
- [ ] Lộ trình mở rộng 10x/100x và những gì chủ ý chưa làm.

### E. Over/under-engineering và tính trung thực
- [ ] Độ phức tạp tương xứng tải ước lượng; phần thừa không có nhãn `[Chọn để học]` → CONFIRM.
- [ ] Use case tương tự: không bịa chi tiết/số liệu công ty; có mức tin cậy; nguồn ghi rõ mức xác minh.
- [ ] Ước lượng (back-of-envelope) có công thức, giả định, và kết luận bậc độ lớn hợp lý.
- [ ] Có pre-mortem; rủi ro lớn có hướng giảm thiểu.

### F. Lỗi hình thức (FIX)
Thiếu mục của `_template.md`, sơ đồ Mermaid lỗi cú pháp/thiếu chú thích, link chết, tên file sai quy ước, ADR dự kiến chưa liệt kê, thuật ngữ nâng cao xuất hiện lần đầu mà chưa giải thích (`.agents/rules/explain-terms.md`).

### G. Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
- [ ] **Chất lượng:** chế độ lỗi, không điểm lỗi đơn, timeout/retry/circuit breaker, idempotency, nhất quán, quan sát, lộ trình mở rộng.
- [ ] **Bảo mật:** có **mô hình đe doạ nhẹ (STRIDE)** theo ranh giới tin cậy; authN/authZ từng tầng; chống IDOR; quản lý bí mật; mã hoá truyền/lưu; least privilege IAM; thiếu → CONFIRM/FIX `blocker`.
- [ ] **Chi phí:** có **ước tính chi phí AWS hằng tháng theo thành phần** kèm giả định và nguồn giá; tài nguyên 24/7 vs theo nhu cầu; cấu hình nhỏ cho dev/stg; cách tắt/`destroy`; chi phí của độ phức tạp; có phương án rẻ hơn đã cân nhắc; so với `quality.monthlyBudgetUsd` (nếu còn `TODO` → CONFIRM `missing`).
- Đánh đổi bảo mật ↔ chi phí chưa được người dùng quyết → CONFIRM, không tự chọn.

## Làn và độ sâu
Làn vừa: chỉ kiểm A, B (quyết định thực sự có mặt), C, D (failure modes, tương thích ngược, rollout) trên phần design ngắn, tối đa 3 vòng; làn lớn: đủ A–G, tối đa 5 vòng (`../workflow/references/lanes.md`, `review-loop.md`).

## Ngoài đời thực
Review design ở công ty là một buổi đọc + comment của đồng đội và người sở hữu hệ thống bị ảnh hưởng; họ hỏi nhiều nhất "nếu cái này hỏng/đổi thì ai chịu, quay lại thế nào?". Giữ câu hỏi đó khi review; nhờ người thật đọc lại design lớn nếu có.

## Cách hỏi người dùng
Gom CONFIRM theo lô ≤ 4 câu; ưu tiên quyết định khó đảo ngược (mô hình dữ liệu, ranh giới service, hợp đồng công khai, công nghệ lưu trữ). Mỗi câu nêu phương án, đánh đổi chính của từng phương án, và khuyến nghị (chỉ tham khảo). Người dùng muốn luyện thì có thể để họ **chọn trước** rồi mới đối chiếu (xem chế độ học của `system-design`).

## Sau khi người dùng trả lời
Ghi Confirm log → cập nhật design (quyết định, bảng so sánh, "vì sao không chọn", sơ đồ, chế độ lỗi nếu đổi) → review lại delta, kiểm tra tác động dây chuyền (một quyết định đổi kéo theo dữ liệu, hợp đồng, chi phí, test).

## PASS khi
- 0 CONFIRM mở, 0 FIX mở; checklist A–F đã kiểm chứng.
- Mọi quyết định lớn đã chốt; mọi giả định đã được xác nhận hoặc `Hoãn` có người chịu trách nhiệm.
- D0 đầy đủ và người dùng đã chọn/xác nhận từng mục; không còn mục "Agent chọn theo uỷ quyền" chờ xác nhận.
- Design `status: confirmed`.
- Đề xuất danh sách **ADR** cần ghi cho các quyết định lớn (tạo khi người dùng đồng ý) và chia **issue** theo lát giao được; báo `workflow` mở `plan-init`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
