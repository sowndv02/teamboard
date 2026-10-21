# Giao thức review lặp (dùng chung cho mọi bước review)

Áp dụng cho: `spec-review`, `design-review`, `plan-review`, `test-review`, `coding-review`. Mỗi skill review có checklist riêng; **vòng lặp, cách phân loại, cách hỏi người dùng và điều kiện dừng** theo file này.

Mục đích của review: tìm ra điểm **chưa rõ, mơ hồ, mâu thuẫn, thiếu quyết định** và để **người dùng chốt**. Reviewer **không tự chốt** và **không tự đưa quyết định** thay người dùng.

## 1. Hai loại finding

| Loại | Định nghĩa | Ai xử lý |
|---|---|---|
| **CONFIRM** | Cần người dùng quyết định hoặc cung cấp thông tin. Gồm: mơ hồ, thiếu thông tin, mâu thuẫn, quyết định chưa được đưa ra, giả định chưa xác nhận, ranh giới phạm vi chưa rõ, nhiều cách hợp lý ngang nhau | **Người dùng**. Agent không được tự chọn |
| **FIX** | Lỗi rõ ràng chỉ có **một** cách sửa hợp lý, căn cứ vào nguồn có thẩm quyền (spec/ADR/rule đã chốt): thiếu mục bắt buộc của mẫu, sai chính tả, link chết, số liệu lệch với `config/`, nội dung mâu thuẫn với quyết định người dùng đã chốt trước đó | **Agent** sửa, rồi review lại |

Nghi ngờ giữa FIX và CONFIRM → **luôn là CONFIRM**. Cách sửa chạm tới hành vi, phạm vi, đánh đổi hay công nghệ → CONFIRM.

### Phân nhóm CONFIRM (để hỏi dễ hơn)
`ambiguity` (mơ hồ), `missing` (thiếu), `conflict` (mâu thuẫn), `undecided` (chưa quyết định), `assumption` (giả định cần xác nhận), `scope` (ranh giới phạm vi).

### Lăng kính xuyên suốt
Mọi bước review còn kiểm **Chất lượng, Bảo mật, Chi phí** (xem `review-philosophy.md` để biết mỗi bước kiểm gì, cách phân loại CONFIRM/FIX/mức, và quan điểm chung từng phase). Bảo mật mặc định nghiêm hơn chi phí; đánh đổi giữa chúng là CONFIRM, không tự quyết.

## 2. Định dạng finding

Mỗi finding có:

- **ID/fingerprint** ổn định: `<bước>:<artifact>:<concern>` (ví dụ `spec-review:board-assign:fr003-vague-notify`). Không đổi giữa các vòng.
- **Loại**: CONFIRM hoặc FIX; với CONFIRM thêm nhóm.
- **Mức**: `blocker` (không thể sang bước sau nếu chưa giải quyết) hoặc `major`/`minor` (FIX).
- **Vị trí**: file + mục/dòng.
- **Vấn đề**: một đến hai câu, có bằng chứng.
- **Với CONFIRM**: câu hỏi cụ thể + 2–4 lựa chọn (nếu có) + **phương án khuyến nghị kèm lý do**. Khuyến nghị chỉ để tham khảo, **không được áp dụng khi người dùng chưa trả lời**.
- **Với FIX**: cách sửa đề xuất.

## 3. Vòng lặp

```
review (vòng N)
  ├─ có CONFIRM mở?  → hỏi người dùng (theo lô) → ghi vào "Confirm log" → sửa artifact theo quyết định → vòng N+1
  ├─ có FIX mở?      → agent sửa → vòng N+1
  └─ không còn CONFIRM mở và không còn FIX chặn → PASS → ghi trạng thái → cho phép bước tiếp
```

Quy tắc:

1. **Vòng 1 phải đầy đủ** (làn vừa/lớn): chạy hết checklist của bước. Làn nhỏ: chạy phần checklist liên quan tới diff, một vòng. Không để dành finding cho vòng sau (trừ khi nó phát sinh từ chính các thay đổi vừa sửa).
2. **Chỉ sang bước tiếp theo khi PASS.** Không được bắt đầu bước sau khi còn CONFIRM mở, kể cả khi "có vẻ nhỏ". Còn điểm chờ người dùng xác nhận (CONFIRM mở, mục "chờ xác nhận" trong Confirm log, giả định agent đặt chưa xác nhận) thì **không thể PASS/done**; đây là cổng xác nhận chung của mọi bước (`state-protocol.md` mục 3 quy tắc 8).
3. **Hỏi người dùng theo lô**: gom tất cả CONFIRM của vòng, dùng công cụ hỏi có lựa chọn (`AskUserQuestion`), **tối đa 4 câu mỗi lô**, ưu tiên theo mức chặn. Nhiều hơn 4 thì chia lô, hỏi các câu chặn nhất trước, rồi lô kế.
4. **Ghi quyết định** vào mục **Confirm log** của artifact ngay khi nhận (xem mục 5), rồi mới sửa. Sửa artifact đúng theo quyết định; không thêm điều người dùng không nói.
5. **Sau khi sửa → chạy review lại.** Vòng sau chỉ tập trung vào phần **thay đổi (delta)** và tác động của nó, nhưng vẫn kiểm các finding cũ đã đóng có bị tái phát không.
6. **Không hỏi lại điều đã chốt.** Nếu nội dung mới mâu thuẫn quyết định cũ trong Confirm log → tạo CONFIRM mới nêu rõ mâu thuẫn và trích quyết định cũ.
7. **Dedup**: cùng fingerprint không nâng mức giữa các vòng; một concern một finding.
8. **Người dùng có thể trả lời "hoãn/để sau"** hoặc "chấp nhận rủi ro": ghi vào Confirm log là `Hoãn` (kèm người chịu trách nhiệm, rủi ro còn lại). Chỉ chấp nhận cho điểm **không** phải `blocker` kỹ thuật bất khả thi; điểm `blocker` mà người dùng hoãn → bước tiếp theo vẫn cần biết giả định dùng; ghi rõ giả định đó là của người dùng chọn.
9. **Giới hạn vòng theo làn** (`lanes.md`): nhỏ 1 vòng, vừa 3, lớn 5. Nếu sau số vòng đó vẫn còn phát sinh CONFIRM mới liên tục, **dừng**, báo mô thức (ví dụ yêu cầu đổi liên tục, spec chưa ổn định) và hỏi người dùng nên quay lại bước trước hay tiếp tục.
10. **Không bịa lý do** để PASS: PASS chỉ khi mọi mục checklist áp dụng đã kiểm chứng.
11. **Uỷ quyền không phải xác nhận.** Người dùng trả lời kiểu "tuỳ", "chọn cái thực tế", "tham khảo thực tế" nghĩa là họ giao cho agent *đề xuất*, không phải đã chốt. Agent: (a) chọn theo nguyên tắc nêu rõ trong câu trả lời và trong tài liệu; (b) ghi Confirm log nguyên văn lời uỷ quyền, cột quyết định ghi **"Agent chọn theo uỷ quyền: <kết quả cụ thể>; chờ xác nhận"** kèm phương án thay thế chính; (c) hỏi lại người dùng xác nhận **kết quả cụ thể** (không hỏi lại điều họ đã chốt) trước khi PASS; khi xác nhận xong thì cập nhật cột đó. Nếu kết quả làm đổi ADR hoặc spec đã chốt, nêu rõ và đề xuất ADR/spec-review thay vì coi là xong. Đặc biệt nghiêm với quyết định khó đảo ngược (loại DB, ranh giới service, kiến trúc nội bộ, hợp đồng công khai).

## 4. Verdict

| Verdict | Điều kiện | Hành động |
|---|---|---|
| `PASS` | 0 CONFIRM mở, 0 FIX mở | Ghi trạng thái bước = đạt; báo cho `workflow` để mở bước kế |
| `NEEDS_CONFIRM` | ≥ 1 CONFIRM mở | Hỏi người dùng; dừng ở bước này |
| `NEEDS_FIX` | 0 CONFIRM mở, ≥ 1 FIX mở | Agent sửa rồi review lại |

## 5. Ghi nhận trong artifact

Mỗi artifact được review có hai bảng cuối file (nếu chưa có thì tạo):

**Confirm log**

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

**Review rounds**

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

Với quyết định do uỷ quyền (quy tắc 11), cột "Quyết định của người dùng" ghi lời uỷ quyền nguyên văn và kết quả agent chọn, đánh dấu "chờ xác nhận" cho tới khi người dùng xác nhận.

Khi `PASS`: đổi `status` trong front matter (hoặc bảng Workflow trong issue) theo từng bước (xem skill `workflow`).

Lưu ý quy tắc cập nhật tài liệu (`docs-update-specs.md`): chỉ sửa artifact thuộc phạm vi đang làm; không sửa issue/ADR cũ.

## 6. Độc lập của reviewer

Review khách quan hơn khi reviewer **không bị neo vào suy nghĩ của người viết**. Khi có thể (và người dùng cho phép dùng sub-agent), chạy review trong một phiên/sub-agent riêng: đưa **artifact + nguồn tham chiếu + checklist**, **không** đưa kết luận hay lý lẽ của người viết. Kết quả trả về vẫn phải tự kiểm trước khi hỏi người dùng. Không bắt buộc cho việc nhỏ.

## 7. Báo cáo cho người dùng

Ngắn gọn, theo thứ tự:
1. Verdict và số CONFIRM/FIX mở.
2. Với `NEEDS_CONFIRM`: danh sách câu hỏi (qua công cụ hỏi), không dán cả báo cáo dài.
3. Đường dẫn artifact và mục đã đổi.
4. Việc tiếp theo.
