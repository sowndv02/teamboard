---
name: lesson-learned
description: Ghi lại bài học kinh nghiệm sau lỗi/sự cố/sai sót (nguyên nhân gốc, biện pháp phòng ngừa đã mã hoá thành rule/test/gate), tra bài học liên quan trước khi làm việc, và rà soát định kỳ xem bài học đã thật sự ngăn được lỗi lặp lại chưa. Dùng khi review phát hiện lỗi hệ thống, khi người dùng sửa cách làm của agent, khi sự cố xảy ra, khi đóng issue/PR để tổng kết, hoặc khi muốn tra bài học theo chủ đề.
---

# Lesson learned

Biến sai sót thành **thay đổi hệ thống** để lần sau không lặp lại. Sổ bài học: `document/lessons/` (mục lục có tag, mẫu `_template.md`). Quy tắc ngắn: `.agents/rules/lessons-learned.md`. Skill này là quy trình.

Rule liên quan: `docs-conventions.md`, `docs-update-specs.md`, `agent-conduct.md` (trung thực khi nhận lỗi), `code-review.md`, `git-workflow.md`.

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `capture` (mặc định) | Vừa có lỗi/sự cố/sai sót; người dùng sửa agent; tổng kết khi đóng issue | Lesson mới hoặc cập nhật lesson cũ + biện pháp phòng ngừa |
| `search` | Bắt đầu task trong một vùng, hoặc hỏi "đã gặp lỗi này chưa" | Danh sách lesson liên quan (tag) kèm điều cần nhớ |
| `audit` | Rà định kỳ (ví dụ cuối giai đoạn) | Báo cáo: lesson nào `open/partial`, lặp lại, biện pháp nào chưa hiệu quả; đề xuất thăng cấp |

Không cần gõ lệnh; suy ra từ ngữ cảnh, không rõ thì hỏi.

## `capture`

### 1. Xác định có đáng ghi không
Theo trigger ở `lessons-learned.md` mục 2. Lỗi tầm thường (gõ nhầm, đã có rule rõ ràng và chỉ quên một lần) → chỉ sửa, không ghi. Không chắc → hỏi người dùng một câu.

### 2. Thu thập sự thật (không suy diễn)
- Chuyện gì xảy ra, phát hiện lúc nào, bằng cách nào; tác động thực tế/tiềm tàng.
- Bằng chứng: issue, commit, finding review, log, đoạn người dùng sửa. Đọc lại chúng, **không dựng lại từ trí nhớ**.
- **Hỏi người dùng** nếu thiếu thông tin quan trọng (ví dụ nguyên nhân thật, mức độ nghiêm trọng); không bịa.

### 3. Tìm trùng
`Grep` tag/từ khoá trong `document/lessons/`. Đã có lesson: **cập nhật** (tăng `recurrence`, trạng thái `recurred`, nâng cấp biện pháp) thay vì tạo mới.

### 4. Nguyên nhân gốc (5 câu hỏi "vì sao")
Đi từ triệu chứng xuống **quy trình/công cụ/mẫu đã để lỗi lọt qua**, dừng ở nguyên nhân hệ thống. Cách diễn đạt **không đổ lỗi cá nhân** ("quy trình không có bước kiểm X", không "A quên X"). Nghi ngờ nguyên nhân thật → ghi là giả thuyết và hỏi.

### 5. Thiết kế biện pháp phòng ngừa (phần quan trọng nhất)
Với mỗi nguyên nhân gốc, chọn biện pháp **chắc nhất khả thi**:
1. **Tự động hoá:** test tái hiện lỗi, lint/analyzer rule, script/lệnh trong gate, hook.
2. **Rule/gate:** thêm hoặc sửa mục trong `.agents/rules/*` hoặc `code-change-gate.md`/`quality-gate.md`.
3. **Mẫu/checklist:** thêm dòng vào template (issue/spec/design/plan) hoặc checklist review.
4. **ADR/spec:** nếu thực chất là quyết định thiết kế.
5. **Chỉ nhắc nhở:** cuối cùng, chỉ kèm lý do vì sao không làm được cách mạnh hơn.
Mỗi biện pháp ghi: loại, nội dung, **vị trí cụ thể**, trạng thái (`done`/`todo`). Biện pháp chạm rule/skill/gate là thay đổi thật: **đề xuất cho người dùng**, làm trong issue/commit rõ ràng (không tự ý sửa ngầm), theo workflow thường lệ.

### 6. Cách kiểm chứng
Ghi cách chứng minh biện pháp hiệu quả (viết test tái hiện, lệnh gate, kịch bản thử). Có thể thì **chạy thử** và ghi kết quả; nếu chưa chạy được, ghi rõ.

### 7. Ghi lesson
- Tạo/cập nhật `document/lessons/LL-NNN-<slug>.md` từ mẫu (số kế tiếp trong mục lục), đủ front matter (`status`, `tags`, `severity`, `issue`, `date`, `recurrence`).
- Cập nhật bảng trong `document/lessons/README.md` cùng thay đổi.
- Trạng thái: `prevented` chỉ khi biện pháp đã `done` **và** đã kiểm chứng; có việc dở → `partial`; chưa có → `open`.
- Liên kết lesson trong mục "Bài học" của file issue (chỉ khi branch khớp issue, `docs-update-specs.md`).

### 8. Xác nhận với người dùng
Trình bày ngắn: bài học một câu, nguyên nhân gốc, biện pháp đề xuất (và cái nào cần quyết định), trạng thái. Người dùng chỉnh/duyệt **trước khi** chốt trạng thái hoặc thay đổi rule/gate. Lỗi do chính agent gây ra: **nói thẳng và nhận**, không biện minh (`agent-conduct.md`).

## `search`

1. Nhận ngữ cảnh: vùng/tag đang làm (từ issue, plan, file đã đổi).
2. Đọc mục lục `document/lessons/README.md`; lọc theo tag; mở các lesson khớp (không mở hết).
3. Trả về: với mỗi lesson, **điều cần nhớ + biện pháp đã có + việc cần làm trong lát này** (ví dụ "thêm kịch bản test về X", "chạy gitleaks trước commit").
4. Nếu lesson cho thấy rủi ro lặp lại mà biện pháp còn `partial/open`, nêu rõ.
Không liệt kê lesson không liên quan.

## `audit`

Rà toàn bộ `document/lessons/`:
- Lesson `open/partial` quá lâu: liệt kê việc dở và đề xuất ưu tiên.
- Lesson `recurred` hoặc có `recurrence ≥ 2`: đề xuất **thăng cấp** biện pháp (tự động hoá, rule cố định).
- Lesson `prevented` nhưng biện pháp không còn tồn tại (rule bị xoá, test bị skip): đánh dấu `partial`.
- Nhóm lesson theo tag, tìm **mô thức** (nhiều lỗi cùng nguyên nhân gốc) và đề xuất cải tiến quy trình ở mức cao hơn.
- Lesson cũ không còn áp dụng (công nghệ đã đổi): đề xuất lưu trữ (không xoá lịch sử).
Báo cáo ngắn, có bằng chứng; thay đổi thật cần người dùng duyệt.

## Không làm
- Không ghi secret, dữ liệu cá nhân, chi tiết khai thác có thể bị lạm dụng.
- Không đổ lỗi cá nhân; không bịa nguyên nhân hay tác động.
- Không đánh dấu `prevented` khi biện pháp chưa mã hoá và kiểm chứng.
- Không tự ý sửa rule/gate/skill để "phòng ngừa" mà không đề xuất và được duyệt.
- Không tạo lesson trùng; không sửa lesson cũ để xoá dấu vết (cập nhật có lịch sử rõ ràng).

## Hoàn thành khi (`capture`)
- [ ] Có bài học một câu mệnh lệnh; tag đúng; liên kết bằng chứng
- [ ] Nguyên nhân gốc dừng ở hệ thống/quy trình
- [ ] ≥ 1 biện pháp phòng ngừa **mã hoá** (ưu tiên tự động hoá), có vị trí cụ thể
- [ ] Có cách kiểm chứng; trạng thái phản ánh đúng thực tế
- [ ] Mục lục cập nhật; người dùng đã xem

## Ghi trạng thái workflow
Nếu chạy trong workflow (tổng kết sau PR): theo `../workflow/references/state-protocol.md`, ghi sự kiện `lessons_recorded` (danh sách LL-ID, hoặc "không có") vào state file.
