---
name: coding-start
description: Bắt đầu viết code theo plan và test đã duyệt - lặp Đỏ-Xanh-Refactor từng kịch bản cho đến khi mọi test pass và gate xanh, không tự quyết hành vi spec chưa nói. Dùng sau test-review PASS và trước coding-review.
---

# Coding start (TDD)

Viết code production để **làm test đỏ thành xanh**, từng lát nhỏ, theo plan. Kết thúc khi **toàn bộ test pass** và gate xanh; chỉ khi đó mới chuyển `coding-review`.

Rule liên quan: `task-workflow.md`, `testing.md`, `quality-gate.md`, `code-change-gate.md`, `architecture.md`, `api-endpoints.md`, `backend-engineering.md`, `dotnet.md`, `messaging.md`, `observability.md`, `dependency-pinning.md`, `agent-conduct.md`.

## Đầu vào và điều kiện
- `test-review: PASS` trong Workflow của issue; plan `approved`; spec/design `confirmed`.
- Đang ở branch `feature/TB-NNN-*` khớp issue; working tree sạch hoặc chỉ có thay đổi của issue này.
- Test hiện tại **đỏ đúng lý do** (chạy lại để chắc). Đã xanh sẵn hoặc đỏ sai lý do → dừng, báo `test-review`.

## Quy trình (lặp theo từng bước của plan)
1. **Chọn bước kế** theo plan (thứ tự, phụ thuộc). Đọc code liên quan.
2. **Đỏ**: chạy test của bước, xác nhận đang đỏ đúng lý do (thiếu hành vi, không phải lỗi biên dịch/import/cấu hình). **Kiểm `git status`: nếu đã có thay đổi production cho bước này trước khi đỏ được xác nhận → dừng**, đưa thay đổi về trạng thái đúng thứ tự (không viết production trước đỏ).
3. **Xanh**: viết **code tối thiểu** để test pass; theo kiến trúc bắt buộc của service và rule kỹ thuật (lỗi qua registry, cấu hình qua Options, route theo `api-endpoints.md`, outbox/idempotent consumer...).
4. **Refactor**: dọn code giữ xanh, không đổi hành vi, không thêm tính năng.
5. **Gate nhanh** của bước: build, lint/format, test liên quan (targeted, `quality-gate.md`).
6. **Ghi nhật ký** vào issue (Kịch bản và TDD log: cột Xanh; Ghi chú nếu có điều đáng nhớ).
7. Lặp đến hết plan.

Sau bước cuối:
- Chạy **toàn bộ test** của mọi project đã đụng và **gate đầy đủ** theo `code-change-gate.md` + `quality-gate.md` (secret, phụ thuộc có lỗ hổng, format...).
- Đối chiếu bảng kịch bản ↔ test (`test-review.md` mục 6): mọi kịch bản xanh.
- **Tất cả test phải pass** thì mới đánh dấu xong (`coding-start: done, xanh N/N`). Còn test fail → tiếp tục sửa; không bàn giao review.

## Plan of record và hợp đồng phạm vi (`../workflow/references/generation-principles.md` mục 4)
- Plan `approved` và `.feature` `ready` là **neo**: không "nghĩ lại" tiêu chí chấp nhận; sinh code = điền hành vi để test xanh, **không** thêm tính năng ngoài plan/`.feature`.
- Chỉ sửa đường dẫn/module có trong plan. **Cấm** sửa `.agents/**`, `.claude/**`, cấu hình CI/dùng chung hoặc file ngoài phạm vi trừ khi plan nêu rõ; cần chạm → dừng và hỏi người dùng.
- Bước độc lập có thể giao sub-agent (ngữ cảnh sạch); orchestrator giữ vòng đỏ/xanh và tự kiểm lại kết quả (build/test/diff) trước khi tin.

## Khi gặp điều chưa rõ (không tự quyết)
Dừng và **hỏi người dùng** (công cụ hỏi, kèm lựa chọn và ngữ cảnh) khi:
- Hành vi spec/plan/test không quy định (giá trị mặc định, thông điệp lỗi, thứ tự, giới hạn, quyền).
- Cần đổi hành vi so với spec, thêm phụ thuộc/công nghệ, hoặc đổi kiến trúc.
- Test sai hoặc mâu thuẫn spec (không tự sửa test cho xanh).
- Cần đụng ngoài phạm vi issue hoặc service khác.

Hành vi chốt xong → cập nhật **spec** (và plan/`.feature`/test nếu đổi) **trước**, rồi mới code (`docs-update-specs.md`). Test cần đổi vì hành vi đổi: sửa theo spec mới và chạy lại để thấy đỏ đúng lý do rồi mới xanh.

## Bài học liên quan
Trước khi code, tra `document/lessons/README.md` theo tag của vùng đang làm (service, `security`, `api`, `database`...) và áp dụng các biện pháp phòng ngừa đã ghi (ví dụ chạy quét secret, thêm test đường lỗi). Gặp lỗi lặp lại với lesson đã có → báo `recurred` và đề xuất nâng cấp biện pháp (skill `lesson-learned`).

## Commit
- Chỉ commit khi người dùng cho phép; nếu chưa được hỏi trong phiên này thì **hỏi một lần** ("cho phép tôi commit cục bộ trên branch này theo từng lát?"). Được phép → commit nhỏ theo `git-commit.md` (tiếng Anh, `type(TB-NNN): summary` + body), mỗi commit build được và test xanh. **Không push.**
- Không commit file sinh ra, secret, `local/`.

## Không làm
- Không viết code cho hành vi chưa có test đỏ; không thêm tính năng ngoài plan.
- Không skip/xoá/nới test, tắt lint/analyzer, `--no-verify`, hay workaround để qua gate.
- Không sửa spec/design/ADR cũ ngầm; không sửa issue không thuộc branch.
- Không chuyển `coding-review` khi còn test fail.

## Hoàn thành khi
- [ ] Mọi bước plan xong; **mọi test pass** (test của issue + test các project bị ảnh hưởng)
- [ ] Gate đầy đủ xanh (hoặc nêu rõ phần không chạy được và vì sao)
- [ ] Bảng kịch bản ↔ test không còn gap; không `@wip`/skip
- [ ] Issue: TDD log (đỏ/xanh), Workflow `coding-start: done, xanh N/N`
- [ ] Không có thay đổi ngoài phạm vi; diff sạch (không secret/file sinh ra)
- Báo `workflow` mở `coding-review`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
