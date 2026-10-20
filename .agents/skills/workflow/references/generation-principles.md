# Nguyên tắc khi sinh artifact (plan, test, code)

Đọc cùng `review-philosophy.md`. Review hỏi "artifact này đã đúng chưa"; file này nói **agent sinh artifact như thế nào để không vibe tự do**: sinh trong biên đã chốt, dựa trên bằng chứng, không bịa, không tự chốt.

## 1. Nguyên tắc chung

| Nguyên tắc | Ý nghĩa |
|---|---|
| **Plan of record** | Plan `approved` và `.feature` `ready` là **neo**. Test và code không "nghĩ lại" tiêu chí chấp nhận; thấy sai/thiếu thì dừng và hỏi (quay lui), không tự đổi |
| **Discovery, không bịa** | Sinh dựa trên thứ đọc được: spec → design → ADR → contract → code hiện có (tìm bằng Grep/Glob). Không đoán cấu trúc, tên file, API; không "nhớ" thay vì kiểm |
| **Chưa chốt thì không sinh như đã chốt** | Điều mơ hồ/chưa quyết → ghi **Câu hỏi mở** và hỏi người dùng; cấm viết vào plan/test/code như thể đã quyết |
| **Tách artifact** | spec → design → plan → `.feature` → test thực thi → code production. Mỗi lớp một skill, một người chịu trách nhiệm, không nhảy lớp |
| **Thứ tự khoá** | Test đỏ trước production; test review trước coding; plan `approved` trước test; review PASS trước bước kế |
| **Agent sinh, issue neo** | Issue + branch gắn phạm vi; sinh trong biên lát, không ôm cả tính năng |
| **Sinh ≠ chốt** | Agent sinh bản nháp; "xong/đã duyệt" chỉ sau review PASS và người dùng chốt các CONFIRM |

```
Sinh PLAN  = dịch spec + design → lộ trình bước/file (chưa code)
Sinh TEST  = dịch spec/plan → hành vi Gherkin (living doc) + test thực thi đỏ
Sinh CODE  = implement theo bước plan để test xanh (không thêm hành vi ngoài test)
```

## 2. Sinh plan (`plan-init`)

- **Đầu vào:** issue/branch, spec và design `confirmed`, ADR/contract liên quan, plan cha (nếu có), code hiện có (đã discovery).
- **Ảnh chụp trạng thái branch** (chỉ làm ngữ cảnh, không đánh giá chất lượng): HEAD, merge-base với `main`, working tree sạch hay có thay đổi dở.
- **Đầu ra:** plan theo mẫu: mục tiêu, trong/ngoài phạm vi, bảng truy vết, các bước (mỗi bước một chu kỳ TDD), rủi ro/rollback, câu hỏi mở. Quyết định kỹ thuật chỉ ghi khi có **bằng chứng** (đường dẫn spec/ADR/code); không có thì là câu hỏi mở.
- **Cỡ bước** (xem mục 5): bước không được khổng lồ.
- **Resume:** đã có plan của issue → cập nhật tại chỗ, không tạo bản trùng.
- **Scope lớn hoặc thiếu spec** → hỏi người dùng xác nhận phạm vi **trước** khi ghi plan.
- **Không làm:** không implement; không thay spec bằng suy diễn; không tuyên bố "sẵn sàng code" ở bước init (đó là sau `plan-review` PASS).
- **Bàn giao:** plan `approved` → `test-init` đọc plan + spec để sinh test.

## 3. Sinh test (`test-init`, hai tầng)

### A. Living doc `.feature` (trước coding)
- Nguồn: spec (tiêu chí chấp nhận), plan (bảng truy vết), design (hợp đồng, chế độ lỗi).
- Bố cục theo service/tính năng; tag issue + ID kịch bản (`@S01`).
- **Tự kiểm trước khi mời review:** chạy checklist chất lượng `.feature` (`test-review.md` mục 1–3), kiểm cơ học (tag đủ, không `@wip`, step nhất quán, mọi FR/BR/NFR có kịch bản, mọi kịch bản truy được về yêu cầu). Còn lỗi → sửa, **không** chuyển `test-review`.
- Sinh `.feature` không có nghĩa test chạy đúng; không coi test thực thi đã đúng chỉ vì có `.feature`.

### B. Test thực thi (TDD)
```
.feature (ready) → viết test thực thi → chạy ĐỎ đúng lý do (thiếu hành vi) → code tối thiểu → XANH → refactor
```
- Cấm sửa production trước khi đỏ được xác nhận; đỏ vì lỗi biên dịch/import/cấu hình/môi trường **không tính**.
- Nếu issue lớn, có thể sinh test thực thi **theo từng bước plan** (đỏ từng bước) thay vì toàn bộ một lần; quy ước này ghi trong plan.

## 4. Sinh code (`coding-start`)

- **Cổng vào:** plan `approved`, test `ready`, test đỏ đúng lý do.
- **Không một cục:** vòng lặp theo bước plan; mỗi bước nhỏ, kiểm chứng ngay.
- **Hợp đồng phạm vi:** chỉ sửa đường dẫn/module có trong plan; **cấm** sửa `.agents/**`, `.claude/**`, CI, cấu hình dùng chung hay file ngoài phạm vi trừ khi plan nêu. Chạm ngoài → dừng và hỏi.
- **Sinh code = điền hành vi để test xanh**, không viết tính năng ngoài plan/`.feature`.
- **Commit:** theo quyết định đã chốt (hỏi một lần; không push). Commit đúng `git-commit.md`.
- **Đổi phạm vi giữa chừng:** xem `workflow/SKILL.md` (mục đổi phạm vi), không lách plan.
- Có thể giao một bước plan cho sub-agent (ngữ cảnh sạch) khi bước độc lập; orchestrator giữ vòng đỏ/xanh và tự kiểm kết quả.

## 5. Cỡ bước (phase sizing) — hướng dẫn

Mục đích: mỗi bước vừa trong một lượt làm việc và review được. Đây là **gợi ý**, không phải con số cứng; vượt thì chia hoặc nêu lý do trong plan:

| Dấu hiệu bước quá lớn | Hành động |
|---|---|
| Chạm nhiều service hoặc nhiều tầng không liên quan | Chia theo service/tầng |
| Nhiều hơn khoảng 3–5 kịch bản cho một bước | Chia theo nhóm kịch bản |
| Khó nêu một tiêu chí kiểm chứng duy nhất | Chia nhỏ tới khi có |
| Gồm cả đổi contract lẫn dùng nó | Tách: contract trước, rồi dùng |
| Cần quyết định chưa chốt giữa chừng | Tách phần đã chốt, hỏi người dùng phần còn lại |

## 6. Sinh và review (cùng pipeline)

| Bước | Sinh (agent làm gì) | Review kiểm gì |
|---|---|---|
| Plan | Plan từ discovery + mẫu + bước | Khớp thứ bậc nguồn thẩm quyền, cỡ bước, quyết định có bằng chứng, 3 lăng kính |
| Test | `.feature` từ spec/plan + tự kiểm cơ học; test thực thi đỏ | Chuẩn Gherkin, phủ yêu cầu, đỏ đúng lý do, bảo mật/chất lượng |
| Code | Production theo bước plan sau khi đỏ | Diff vs plan/spec, failure, bảo mật, chất lượng, chi phí |
| Reconcile | – (không sinh) | Bảng kịch bản ↔ test thực thi |

Reconcile (đối chiếu kịch bản ↔ test) chạy ở `coding-start` trước khi bàn giao và được kiểm lại ở `coding-review`.
