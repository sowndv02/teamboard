---
name: requirements-discovery
description: Khai phá yêu cầu mơ hồ/chung chung của khách hàng thành file spec có cấu trúc. Dùng khi người dùng đưa một yêu cầu thô ("khách muốn có chức năng X", ý tưởng, email, ghi chú họp) và cần làm rõ, đặt câu hỏi, rồi viết spec trong document/spec/. Cũng dùng để tinh chỉnh spec đã có hoặc soạn danh sách câu hỏi gửi khách hàng.
---

# Khai phá yêu cầu → spec

Biến một yêu cầu chung chung thành **spec kiểm chứng được** bằng cách hỏi đúng câu, ghi rõ cái đã biết/giả định/chưa biết, và **không bịa**. Đầu ra là file `document/spec/<ten>.md` (từ `document/spec/_template.md`) và danh sách issue gợi ý. Skill này **không viết code** và **không chốt công nghệ** (quyết định kiến trúc → ADR).

Rule liên quan: `docs-conventions.md` (đặt tên, nội dung), `docs-update-specs.md` (spec là nơi ghi hành vi hiện hành), `agent-conduct.md` (trung thực, không bịa), `architecture.md` (ánh xạ vào service), `test-review.md` (tiêu chí → kịch bản).

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `new` (mặc định) | Có yêu cầu thô, chưa có spec | Spec mới trạng thái `draft` |
| `refine` | Đã có spec, có yêu cầu bổ sung/đổi | Spec cập nhật + mục Lịch sử thay đổi |
| `questions` | Cần gửi khách hàng một danh sách câu hỏi | Bản câu hỏi không kỹ thuật, tiếng Việt, dễ trả lời |

Người dùng không cần gõ lệnh; suy ra chế độ từ ngữ cảnh, không rõ thì hỏi.

**Theo làn** (`../workflow/references/lanes.md`): làn **lớn** dùng đủ mẫu `document/spec/_template.md`. Làn **vừa** chỉ cần spec ngắn ngay trong issue (mục tiêu, phạm vi trong/ngoài, FR kèm tiêu chí Given/When/Then, giả định, câu hỏi mở), không bắt buộc file spec đủ mẫu; vẫn giữ nguyên văn yêu cầu gốc và ID. Làn nhỏ không cần bước này.

## Nguyên tắc

1. **Không bịa.** Mọi yêu cầu trong spec phải truy được về lời khách/người dùng hoặc được đánh dấu là **giả định** cần xác nhận.
2. **Hỏi trước khi viết** những điểm ảnh hưởng lớn; điểm nhỏ thì đề xuất mặc định hợp lý, đánh dấu giả định.
3. **Ưu tiên giá trị nghiệp vụ**, không bàn công nghệ. Nếu câu trả lời buộc phải chọn công nghệ/kiến trúc → ghi "Quyết định cần ADR", không tự chốt trong spec.
4. **Mỗi yêu cầu kiểm chứng được**: có tiêu chí chấp nhận cụ thể (có thể thành kịch bản Given/When/Then sau này).
5. **Giữ phạm vi:** nêu rõ cái nằm **ngoài** phạm vi; chống phình yêu cầu.
6. **Tôn trọng thời gian người dùng:** hỏi theo lô, ưu tiên theo tác động, kèm lựa chọn và mặc định đề xuất.

## Quy trình

### 1. Tiếp nhận
- Lưu **nguyên văn** yêu cầu gốc (không diễn giải) vào mục "Yêu cầu gốc" của spec.
- Nếu người dùng đưa tài liệu/ảnh/link: đọc hết; trích ý, ghi nguồn.
- Xác nhận ngắn gọn bằng lời của mình: "Tôi hiểu là ... đúng không?" (một đến hai câu).

### 2. Định vị
- Hệ thống hiện có gì liên quan? Đọc `document/architecture/README.md`, `config/services.yaml`, spec đã có (`document/spec/README.md`). Không đoán.
- Yêu cầu thuộc service nào / cần service mới / chạm nhiều service? Ghi giả thuyết ban đầu (xác nhận ở bước sau).
- Đã có spec/issue trùng hoặc gần giống? Báo người dùng, đề xuất `refine` thay vì tạo mới.

### 3. Phân tích khoảng trống
Đối chiếu yêu cầu với bảng ở `references/gap-checklist.md` (đối tượng, mục tiêu, phạm vi, dữ liệu, quy tắc nghiệp vụ, luồng, quyền, tích hợp, phi chức năng, vận hành, di chuyển dữ liệu, ràng buộc, tiêu chí chấp nhận). Mỗi mục đánh dấu: **Biết** / **Giả định** / **Chưa biết**. Chỉ những mục **Chưa biết** có tác động lớn mới thành câu hỏi.

### 4. Hỏi (vòng lặp)
- Dùng công cụ hỏi có lựa chọn (`AskUserQuestion`), **tối đa 4 câu mỗi lô**, 2–4 lựa chọn, đặt phương án khuyến nghị đầu tiên kèm lý do ngắn. Cho phép người dùng nhập tự do.
- Thứ tự ưu tiên: (1) mục tiêu/giá trị và người dùng, (2) phạm vi trong/ngoài, (3) quy tắc nghiệp vụ cốt lõi và trạng thái, (4) luồng lỗi/biên, (5) quyền và dữ liệu nhạy cảm, (6) phi chức năng.
- Câu hỏi: cụ thể, một ý, tránh câu hỏi dẫn dắt, có ví dụ khi trừu tượng ("khi hai người cùng sửa một task thì ai thắng?").
- Sau mỗi lô: cập nhật bảng Biết/Giả định/Chưa biết, quyết định có cần lô tiếp. **Dừng hỏi** khi không còn "Chưa biết" mức cao; phần còn lại thành giả định/câu hỏi mở.
- Người dùng nói "không biết/hỏi khách sau" → ghi vào **Câu hỏi mở** (kèm giả định mặc định và người cần trả lời), không chặn việc viết spec.
- Khi cần hỏi **khách hàng** thay vì người dùng này: chuyển sang chế độ `questions`.

### 5. Viết spec
- Copy `document/spec/_template.md` → `document/spec/<ten-kebab>.md` (đặt tên theo `docs-conventions.md`). Trạng thái `draft`.
- Điền đủ các mục; mục không áp dụng ghi "Không áp dụng" + lý do, không xoá.
- **ID ổn định** cho từng yêu cầu: `FR-001` (chức năng), `BR-001` (quy tắc nghiệp vụ), `NFR-001` (phi chức năng). Không đánh lại số khi sửa; yêu cầu bị bỏ thì đánh dấu "Đã bỏ", không tái sử dụng ID.
- Mỗi `FR` viết dạng **user story** "Là <vai trò>, tôi muốn <việc> để <giá trị>" (một câu), kèm: ưu tiên MoSCoW (Must/Should/Could/Won't; Must ≤ khoảng một nửa số FR, còn lại phải biện minh), nguồn (lời khách / giả định), và **tiêu chí chấp nhận** ngắn dạng Given/When/Then (1–3 ví dụ số liệu cụ thể, gồm một ca lỗi/biên):
  ```
  FR-003 Là thành viên board, tôi muốn gán task cho người khác để họ biết việc cần làm. [Must · nguồn: email khách 12/10]
  - Given task chưa có người nhận, When tôi gán cho thành viên A, Then A thấy task trong danh sách của mình.
  - Given A không thuộc board, When tôi gán, Then bị từ chối với lỗi rõ ràng.
  ```
- **Phân loại rõ** để không lẫn: **yêu cầu** (khách muốn, có nguồn), **giả định** (ta tin nhưng chưa xác nhận, có người xác nhận + mặc định), **ràng buộc** (hạn, ngân sách, quy định, công nghệ bắt buộc, không thương lượng). Mỗi mục thuộc đúng một nhóm.
- Mục **Ngoài phạm vi** liệt kê tường minh những thứ khách có thể mong nhưng lần này không làm (kèm lý do/giai đoạn sau); yêu cầu "Won't" ghi ở đây.
- Mô tả luồng chính, luồng thay thế, luồng lỗi theo từng use case chính.
- Giả định và câu hỏi mở nằm trong mục riêng, mỗi mục có **owner** (người trả lời), **hạn** (ngày cần có câu trả lời để không trễ tiến độ), **mặc định** sẽ dùng nếu quá hạn, và **trạng thái** (`open`/`answered`/`deferred`). Câu hỏi quá hạn chưa trả lời → nhắc người dùng, không tự coi là đã chốt mặc định cho điều `blocker`.
- Mermaid (nếu có) theo `docs-conventions.md`.
- Dùng thuật ngữ nhất quán; mục "Thuật ngữ" cho từ nghiệp vụ.

### 6. Tự phản biện (trước khi trình bày)
Rà lại spec và ghi vào mục Rủi ro/Giả định:
- Có hai yêu cầu mâu thuẫn nhau? Yêu cầu nào không kiểm chứng được?
- Trường hợp biên bị bỏ sót: đồng thời, trùng lặp, dữ liệu rỗng/rất lớn, hết hạn, huỷ giữa chừng, quyền khác nhau, múi giờ.
- Có yêu cầu thực chất là giải pháp kỹ thuật đội lốt yêu cầu? Đưa về nhu cầu gốc.
- Có phần phình phạm vi ("tiện thể thêm")? Chuyển sang "Ngoài phạm vi" hoặc "Could".
- Tác động tới service/contract hiện có (Blast radius sơ bộ).

### 7. Xác nhận và bàn giao
- Trình bày cho người dùng: tóm tắt ≤ 10 dòng (mục tiêu, phạm vi, FR Must, giả định lớn, câu hỏi mở quan trọng) và đường dẫn spec. **Không dán cả spec vào chat.**
- Chờ xác nhận. Đã xác nhận → trạng thái `confirmed` (hoặc giữ `draft` nếu còn câu hỏi mở chặn).
- Đề xuất (không tự tạo khi chưa được đồng ý):
  - **Chia lát** sau khi spec và design `confirmed` (`../workflow/references/slicing.md`): mỗi lát thành một issue `TB-NNN` trỏ về `FR-xxx` và có tiêu chí hoàn thành. Ở bước này chỉ gợi ý hướng chia thô, bảng lát chính thức do `plan-init` (`slices`) lập.
  - **ADR cần viết** cho quyết định kiến trúc/công nghệ phát sinh.
  - **Kịch bản `.feature`**: các tiêu chí chấp nhận là hạt giống (xem `test-review.md`).
- Thêm spec vào `document/spec/README.md`.

## Chế độ `refine`
1. Đọc spec hiện tại và Lịch sử thay đổi.
2. Phân loại thay đổi: làm rõ, thêm mới, đổi hành vi, bỏ. Thay đổi hành vi hoặc bỏ yêu cầu → hỏi xác nhận, nêu tác động tới issue/test đã có.
3. Cập nhật spec tại chỗ (giữ ID), thêm dòng vào Lịch sử thay đổi (ngày, nội dung, nguồn).
4. Nếu spec đã có issue `in-progress`/`done` bị ảnh hưởng: báo; không sửa issue cũ (`docs-update-specs.md`), đề xuất issue mới.

## Chế độ `questions` (gửi khách hàng)
- Viết bằng **tiếng Việt thường ngày, không thuật ngữ kỹ thuật**; mỗi câu một ý; kèm ví dụ hoặc lựa chọn.
- Nhóm theo chủ đề; đánh số; câu quan trọng nhất đánh dấu ⭐ và đặt lên đầu; tối đa khoảng 10–15 câu mỗi lần.
- Mỗi câu ghi (cho đội nội bộ, không gửi khách) lý do hỏi và spec mục nào bị ảnh hưởng.
- Lưu bản câu hỏi trong `local/` (không commit) hoặc cuối spec mục "Câu hỏi mở" tuỳ người dùng.
- **Mẫu email gửi khách** (soạn sẵn cho người dùng gửi, agent không tự gửi):
  ```
  Chủ đề: [Tên dự án] - Cần xác nhận một số điểm cho <tính năng> (trả lời trước <ngày>)
  Chào <tên>,
  Để bắt đầu <tính năng>, chúng tôi cần anh/chị xác nhận <N> điểm dưới đây. Điểm ⭐ ảnh hưởng trực tiếp tới tiến độ.
  1. ⭐ <câu hỏi> (gợi ý: A / B)
  2. ...
  Nếu chưa nhận được phản hồi trước <ngày>, chúng tôi sẽ tạm làm theo phương án mặc định <...> và cập nhật lại khi có thông tin.
  Cảm ơn anh/chị.
  ```
- **Biên bản họp với khách** (gửi lại sau họp trong ngày để chốt hiểu biết chung): ngày/người dự; **quyết định** đã chốt; **việc cần làm** (ai, hạn); **câu hỏi còn mở** (owner, hạn); **thay đổi so với yêu cầu trước** (nếu có, chuyển qua kiểm soát thay đổi của `project-manager`). Yêu cầu khách im lặng không có nghĩa là đồng ý: ghi là giả định cho tới khi xác nhận bằng chữ.

## Mẫu câu hỏi hay dùng
- Ai dùng chức năng này, vào lúc nào, để làm gì? Họ đang làm việc đó bằng cách nào hiện nay?
- Thế nào là "xong/thành công"? Làm sao biết chức năng đạt mục đích? (số đo, tình huống mẫu)
- Cái gì **không** nằm trong lần này?
- Khi có lỗi/sai dữ liệu/xung đột thì mong muốn hành vi nào?
- Ai được xem/sửa/xoá? Có dữ liệu nhạy cảm không?
- Khối lượng (số người dùng, bản ghi, tần suất) và mức chấp nhận chậm/ngừng?
- Có hệ thống/quy định/thời hạn nào ràng buộc không?
- Cho tôi một ví dụ thật của trường hợp phức tạp nhất.

## Ngoài đời thực
Business analyst/PO thật không hỏi một lần rồi xong: yêu cầu đổi liên tục, khách trả lời chậm, nhiều bên liên quan bất đồng. Thói quen cần tập: ghi nguyên văn, viết lại bằng user story + ví dụ cụ thể, đánh dấu giả định thay vì đoán, theo dõi câu hỏi mở có owner/hạn, gửi biên bản sau họp, và để mọi đổi yêu cầu đi qua spec (không qua lời nói). Ở dự án học tập, người dùng đóng vai khách/PO: hãy hỏi như thể bạn không thể hỏi lại ngay.

## Không làm
- Không viết code, không tạo project/skeleton.
- Không chốt công nghệ, không tự thêm tính năng khách không nhắc (ghi "đề xuất thêm" riêng, ưu tiên Could).
- Không xoá hay sửa nguyên văn "Yêu cầu gốc".
- Không tự tạo issue/ADR/`.feature` khi người dùng chưa đồng ý.
- Không sửa spec của issue `done` cho khớp code; dùng spec hiện hành và issue mới.

## Hoàn thành khi
- [ ] "Yêu cầu gốc" lưu nguyên văn
- [ ] Mọi mục Chưa biết mức cao đã được hỏi; phần còn lại là giả định/câu hỏi mở có người xác nhận
- [ ] Mọi FR/BR/NFR có ID, ưu tiên, tiêu chí chấp nhận kiểm chứng được (FR dạng user story + Given/When/Then)
- [ ] Yêu cầu / giả định / ràng buộc tách bạch; câu hỏi mở có owner, hạn, mặc định
- [ ] Có phạm vi trong và ngoài; có luồng lỗi/biên
- [ ] Đã tự phản biện (mâu thuẫn, biên, phình phạm vi, tác động)
- [ ] Spec nằm đúng chỗ, đúng tên, có trong `document/spec/README.md`
- [ ] Người dùng đã xem tóm tắt và xác nhận (hoặc ghi rõ còn chờ gì)

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
