---
name: coding-review
description: Review code của một issue sau khi mọi test đã pass - tìm lỗi, điểm chưa rõ, mâu thuẫn với spec/design/plan/rule, yêu cầu người dùng chốt điểm cần quyết định, sửa và review lại đến khi không còn điểm cần xác nhận. Dùng sau coding-start (test xanh hết) và trước create-pr.
---

# Coding review

Đọc `../workflow/references/review-loop.md` (vòng lặp, CONFIRM/FIX, Confirm log, PASS) và **rule `code-review.md`** (ba trụ cột Correctness/Blast Radius/Best Practices, probe F1–F8, B1–B4, BP1–BP6, guardrails, mức P0–P3, vòng review, định dạng báo cáo). Skill này là quy trình chạy review đó **trong workflow**.

## Cổng đầu vào (bắt buộc, chạy trước mọi thứ)

1. **Mọi test pass.** Chạy lại test của mọi project đã đụng (và gate theo `code-change-gate.md`, `quality-gate.md`). Có test fail, build lỗi, lint/format fail → **dừng, không review**; trả về `coding-start` (ghi nguyên nhân).
2. `coding-start: done` trong Workflow của issue; bảng kịch bản ↔ test không có gap; không `@wip`/skip.
3. Branch `feature/TB-NNN-*` khớp issue; diff so với base (`main`) xác định được.
4. Không có secret/file sinh ra trong diff (quét thô + `gitleaks` nếu đã cài).

## Quy trình
1. **Lấy diff** (`code-review.md` mục "Diff và base"); ghi branch, issue, base, vòng.
2. **Chạy review vòng 1 đầy đủ** theo `code-review.md`: mọi probe áp dụng (Risk Audit F1–F8, Blast Radius B1–B4, Best Practices BP1–BP6) và guardrails PG1–PG7. Mọi finding gắn fingerprint và mức P0–P3.
3. **Phân loại theo giao thức workflow:**
   - **CONFIRM** (người dùng quyết): hành vi chưa được spec/design/plan quy định mà code đang tự chọn; code lệch spec/design/plan (không rõ nên sửa code hay sửa spec); đánh đổi hiệu năng/bảo mật/phạm vi cần ưu tiên; rủi ro tiềm ẩn trong phạm vi cần người dùng chấp nhận; breaking change contract; P3 chưa quyết sửa hay bỏ; có nhiều cách sửa hợp lý khác nhau.
   - **FIX** (agent sửa): lỗi rõ ràng một cách sửa (bug, thiếu test cho hành vi đã quy định, vi phạm rule đã chốt, hardcode, secret, thiếu xử lý lỗi theo registry, thiếu idempotency theo contract...).
4. **Có CONFIRM mở** → hỏi người dùng theo lô ≤ 4 câu (nêu bằng chứng `file:dòng`, các lựa chọn, đánh đổi, khuyến nghị chỉ tham khảo), ghi Confirm log, **không tự chọn**. Người dùng chốt hành vi → cập nhật **spec** (và plan/`.feature`/test) **trước** rồi mới sửa code.
5. **Sửa** (theo `code-review.md` mục "Khi người dùng yêu cầu sửa finding"): lô ≤ 3 finding mỗi commit/lượt, mỗi finding có test/evidence (finding validation/policy/contract/coverage chỉ đóng khi có test mới hoặc mở rộng); **chạy lại test và gate** sau mỗi lô — **test phải xanh hết** trước khi review lại. Fix mà đổi hành vi đã được người dùng chốt trước đó → hỏi lại.
6. **Review delta** (vòng 2+): theo `code-review.md` (fingerprint, dedup, không leo mức, so khớp vòng trước, regression). Lặp bước 3–6.
7. **Cập nhật issue:** bảng **Review rounds** (vòng, commit đã review, verdict, fingerprint mở), Workflow.

## Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
Ngoài probe của `code-review.md`, báo cáo phải có bảng "Lăng kính":
- **Chất lượng:** đúng, xử lý lỗi, không nuốt lỗi, không N+1, phân trang/giới hạn, log/trace, dễ đọc, theo kiến trúc service.
- **Bảo mật:** OWASP Top 10 (kiểm soát truy cập, injection, mật mã, cấu hình sai, thành phần có lỗ hổng, SSRF...), secret trong code/log, phụ thuộc có lỗ hổng (`quality-gate.md`), CORS/CSP/header. **Kiểm cả khi diff không "chạm bề mặt bảo mật"**, ở mức nhẹ; chạm bề mặt thì kiểm sâu (F8).
- **Chi phí:** truy vấn/gọi ngoài thừa, vòng lặp tốn kém, log/dữ liệu lưu quá mức, polling không cần, tài nguyên không giải phóng, kích thước bundle.
- Lỗi bảo mật rõ ràng = FIX `blocker`; đánh đổi bảo mật/chi phí/hiệu năng không có đáp án duy nhất = CONFIRM.

## Phối hợp với verdict của `code-review.md`
| Verdict workflow | Tương ứng `code-review.md` | Điều kiện |
|---|---|---|
| `PASS` | `Sẵn sàng merge` | 0 CONFIRM mở; không P0–P2 mở; không P3 `open` (đã sửa hoặc người dùng `skipped` có lý do ≥ 10 ký tự); probe áp dụng đã kiểm chứng; rủi ro tiềm ẩn đã được người dùng chấp nhận hoặc ghi "Giới hạn đã biết"; **test xanh hết ở lần chạy cuối** |
| `NEEDS_CONFIRM` | – | ≥ 1 CONFIRM mở |
| `NEEDS_FIX` | `Sẵn sàng sau khi sửa` / `Chưa sẵn sàng` | còn FIX mở |

P0–P2 không được bỏ qua trừ khi người dùng chấp nhận tường minh.

## Review của agent và review của người thật
- Review này là **self-review có kỷ luật** của agent trước khi đến tay người. Nó không thay review của đồng đội/code owner; PR vẫn cần người thật duyệt khi đội có quy ước.
- Mức finding ánh xạ thói quen đội: P0–P2 = **must fix** (chặn merge); P3 = **nit/suggestion** (sửa hay bỏ do người dùng chốt, ghi lý do).
- Khi PR đã có comment của người thật: nhận mỗi comment là một finding (CONFIRM nếu cần quyết định, FIX nếu rõ), trả lời từng cái, không sửa im lặng; đề xuất kèm lý do khi không đồng ý nhưng quyết định thuộc về người dùng.
- Làn nhỏ (`../workflow/references/lanes.md`): một vòng, chỉ probe liên quan diff. Làn hotfix: review tập trung đúng-sai của bản sửa và rollback; phần còn lại bù sau.

## Diff có frontend
Diff chạm `src/web/**`: chạy thêm checklist của skill `frontend-review` (Angular: reactivity, bảo mật client, a11y, i18n, bundle, test hành vi) và gate FE đầy đủ; verdict PASS cần cả hai. Xem `../frontend-review/SKILL.md`.

## Review độc lập
Nên chạy review trong phiên/sub-agent khác với phiên đã viết code (`review-loop.md` mục 6): đưa diff, spec/design/plan và rule, không đưa lập luận của người viết; kết quả vẫn phải tự kiểm trước khi hỏi người dùng. Hỏi người dùng có cho dùng sub-agent không nếu chưa rõ.

## Không làm
- Không review khi test chưa pass.
- Không tự quyết hành vi/đánh đổi; không áp dụng khuyến nghị khi chưa được trả lời.
- Không sửa code mà không có test/evidence; không tắt lint/test để qua.
- Không tự sửa spec/design/ADR cũ; không sửa nội dung ngoài phạm vi issue.

## PASS khi
- Verdict `PASS` như bảng trên; lần chạy test và gate cuối **xanh**.
- Issue: Review rounds đầy đủ, Workflow `coding-review: PASS`, Confirm log ghi mọi quyết định.
- Báo `workflow` mở `create-pr`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
