# Test review (TDD + BDD)

Lý do tồn tại: dự án làm theo **BDD cho hành vi** (file `.feature` là tài liệu sống) và **TDD cho code** (đỏ → xanh → refactor). Rule này quy định cách review hai thứ đó và cổng "sẵn sàng code" trước khi viết production code. Quy tắc viết test nói chung nằm ở `testing.md`; phạm vi test targeted/full ở `quality-gate.md`.

Tuân `agent-conduct.md` trong mọi review: không workaround, không giấu rủi ro đã biết.

## Hai đối tượng, hai mục đích

| Đối tượng | Review để biết | Rule |
|---|---|---|
| **`.feature` (living doc)** | Hành vi mô tả đúng nghiệp vụ/spec, đủ kịch bản, đọc được bởi người không code | Mục 1–4 |
| **Test thực thi** (unit/integration, step definition) | Test có thật sự kiểm chứng kịch bản, đủ sâu, không giả | Mục 5 + `testing.md` + `code-review.md` (F3) |

Review `.feature` **không** đánh giá chi tiết cài đặt test; đối chiếu kịch bản ↔ test thực thi dùng quy trình ở mục 6.

## Quy trình TDD/BDD (outside-in)

1. **Kịch bản trước**: viết/cập nhật `.feature` theo spec/tiêu chí issue. Chạy test review `.feature` (rule này) → verdict `Sẵn sàng code`.
2. **Đỏ**: viết step definition + test; chạy, **thấy fail đúng lý do** (không phải lỗi biên dịch hay setup). Ghi vào nhật ký issue: lệnh, kết quả đỏ.
3. **Xanh**: viết code production tối thiểu cho test pass. Ghi kết quả xanh.
4. **Refactor**: dọn code, giữ xanh. Không đổi hành vi.
5. Lặp theo từng kịch bản. Hoàn tất khi mọi kịch bản của issue xanh + gate (`code-change-gate.md`).

Không viết production code cho hành vi chưa có kịch bản/test đỏ. Sửa bug: viết test tái hiện (đỏ) trước.

## 1. Cấu trúc và vị trí `.feature`

- Vị trí: `document/spec/features/<service-hoac-feature>/<ten>.feature` (kebab-case), cùng thư mục với spec; runner trỏ tới đây (chọn runner: `config/tool-versions.yaml`, hiện TODO). Kịch bản cho luồng người dùng thấy trên UI Angular chạy bằng runner web (`bdd_runner_web`, đề xuất Playwright; `frontend-testing.md`).
- Một file = một khả năng nghiệp vụ (capability). `Feature:` có mô tả "As a… I want… So that…" ngắn.
- **Toàn bộ test BDD viết bằng tiếng Anh**: `.feature` (từ khoá và nội dung), step definition, test xUnit, tên test và comment trong code test. Không dùng ký tự ngoài ASCII (tránh lỗi unicode khi sinh code Reqnroll); dữ liệu test cố ý ngoài ASCII viết bằng escape `\uXXXX` trong code. Thuật ngữ/ID/enum giữ nguyên theo contract.
- **Tag**: issue key (`@TB-001`), ID kịch bản duy nhất trong file (`@S01`, `@S02`), service (`@board-service`), loại (`@smoke`, `@wip`). Kịch bản có tag `@wip` **không** được có trong nhánh chờ merge.
- Mỗi kịch bản truy ngược được tới một dòng của spec hoặc tiêu chí hoàn thành trong issue.

## 2. Chất lượng kịch bản

- **Khai báo (declarative)**, ngôn ngữ nghiệp vụ: mô tả *điều gì*, không mô tả *thế nào*. Không có tên class, method, bảng DB, đường dẫn URL, selector CSS, mã HTTP trong phần nghiệp vụ (mã lỗi chỉ xuất hiện khi chính hợp đồng API là hành vi cần kiểm).
- Một kịch bản = một hành vi; một `When` duy nhất; `Then` kiểm kết quả quan sát được.
- `Given` lập trạng thái ban đầu, không chứa hành động của chính hành vi được kiểm.
- Dùng `Background` cho tiền điều kiện chung; `Scenario Outline` + `Examples` cho dữ liệu biến thể (không copy-paste kịch bản).
- Dữ liệu mẫu cụ thể, thực tế; không dùng `foo/bar/test123`.
- Kịch bản phải gồm cả **đường lỗi và biên** (dữ liệu sai, không đủ quyền, trùng, hết hạn, đồng thời), không chỉ happy path.
- Bước (step) nhất quán trong toàn bộ file: cùng cách diễn đạt cho cùng hành động, để tái sử dụng step.
- Không kịch bản mô tả chi tiết kỹ thuật nội bộ (outbox, retry…) trừ khi chính là hành vi nghiệp vụ cần chứng minh (ví dụ "event chỉ phát một lần khi nhận trùng") và ghi ở mức quan sát được.

## 3. Mức độ (P0–P3) và finding

Fingerprint: `test:<service>:<concern>` (không trùng fingerprint của `code-review.md`).

| Mức | Ví dụ |
|---|---|
| **P0** | Thiếu kịch bản bắt buộc của spec/tiêu chí issue; kịch bản sai contract (mã lỗi, trường, trạng thái) |
| **P1** | Kịch bản mâu thuẫn spec; thiếu tag/ID khi quy tắc yêu cầu; step định nghĩa lỗi thời (không khớp code); thiếu đường lỗi quan trọng (auth, validation, trùng lặp) |
| **P2** | Lỗ hổng coverage so với spec; rò rỉ chi tiết kỹ thuật vào kịch bản nghiệp vụ; step không nhất quán; `When` nhiều hành động; copy-paste thay vì Outline |
| **P3** | Diễn đạt, tiêu đề, định dạng, smell Gherkin nhẹ |

## 4. Verdict và cổng disposition P3

| Verdict | Điều kiện |
|---|---|
| `Sẵn sàng code` | Không còn finding P0–P2 chặn **và** không còn P3 chặn |
| `Cần sửa feature` | Còn P0–P2, hoặc P3 chưa xử lý khi agent muốn kết thúc vòng |

- Trạng thái finding: `open` → `resolved` (đã sửa, chờ review lại) → `closed` (review xác nhận), hoặc `skipped` (bỏ qua có lý do), `re-open` nếu sửa chưa đạt.
- **P3 còn `open`/`resolved`/`re-open` chặn verdict "Sẵn sàng code".** Agent **dừng**, liệt kê P3 chặn, **hỏi người dùng** (không tự bỏ qua):
  - **Sửa** → sửa `.feature`, review lại để `closed`.
  - **Bỏ qua** → ghi `skipped` kèm lý do ≥ 10 ký tự.
- P0–P2 **không** được bỏ qua (chỉ sửa, hoặc người dùng chấp nhận tường minh).
- Rủi ro tiềm ẩn trong phạm vi đã biết: cấm verdict "Sẵn sàng code" trừ khi người dùng đã xác nhận, và báo cáo ghi mục "Giới hạn đã biết" (xem `agent-conduct.md`).

Trạng thái giữa các vòng ghi trong mục **"Test review rounds"** ở cuối file issue (không dựa vào trí nhớ chat). Chỉ sửa issue của branch hiện tại (`docs-update-specs.md`).

## 5. Review test thực thi (TDD)

Khi review test code (unit, integration, step definition), kiểm:

- [ ] Test **đã từng đỏ đúng lý do** (nhật ký issue có bằng chứng đỏ → xanh), không phải test luôn xanh.
- [ ] Assert trên **trạng thái/hành vi thật** (DB, message phát ra), không chỉ status code hay "không ném lỗi".
- [ ] Mỗi kịch bản `.feature` có test thực thi tương ứng, tên/tag trỏ lại được ID kịch bản (`S01`).
- [ ] Không mock quá mức che mất hành vi thật (không mock DB cho integration); mock chỉ ở biên ngoài (mạng bên thứ ba).
- [ ] Test độc lập, không phụ thuộc thứ tự/thời gian thực/dữ liệu chung; deterministic (clock, id giả khi cần).
- [ ] Đủ đường lỗi và biên, đặc biệt: idempotency, outbox, consumer nhận trùng, auth/IDOR, hết hạn, đồng thời.
- [ ] Không có `Skip`/`Ignore`/`it.skip`/assert rỗng để lách; step definition không stale.
- [ ] Tên test mô tả hành vi (`Method_Condition_ExpectedResult` hoặc câu mô tả).

Phát hiện ở mục này ghi vào findings với fingerprint `test:<service>:<concern>` và dùng cùng thang P0–P3.

## 6. Đối chiếu kịch bản ↔ test (reconcile, thủ công)

Không có công cụ tự động; thực hiện bằng grep trước khi báo xong issue và trước khi mở PR:

1. Liệt kê ID kịch bản: `grep -rhoE "@S[0-9]+" document/spec/features/<service>/`.
2. Với mỗi ID, tìm test thực thi tương ứng (tag/tên test/step) và chạy nó.
3. Ghi bảng vào nhật ký issue:

```
| Kịch bản | Test thực thi | Kết quả |
|---|---|---|
| @S01 Tạo board hợp lệ | BoardTests.Create_Valid_ReturnsCreated | ✅ |
| @S02 Tiêu đề rỗng bị từ chối | (chưa có) | ❌ gap → P1 |
```

Kịch bản không có test = finding P1 (hoặc P0 nếu là tiêu chí bắt buộc). Test không có kịch bản = hỏi: thiếu kịch bản hay test thừa?

## 7. Định dạng báo cáo

Vòng 1 đầy đủ; vòng 2+ ưu tiên delta (như `code-review.md`).

1. **Tóm tắt**: issue, file `.feature` đã review, vòng, commit.
2. **Coverage**: phần đã review, spec/tiêu chí đã đối chiếu.
3. **So khớp vòng trước** (vòng 2+): bảng `Fingerprint | Vòng trước | Vòng này`.
4. **Findings**: bảng `Fingerprint | Mức | Vị trí | Vấn đề | Gợi ý` — **mọi** finding, kể cả P3, phải nằm trong bảng này. **Cấm** mục riêng kiểu "Ghi chú P3 (không chặn)" để né cổng.
5. **Bảng reconcile** (mục 6).
6. **Verdict** theo mục 4, kèm "Giới hạn đã biết" nếu có.

Sau báo cáo: cập nhật bảng "Test review rounds" trong issue.

## Khi người dùng yêu cầu sửa finding

Sửa theo lô nhỏ; mỗi lô giữ test xanh và cập nhật kịch bản/test liên quan; review lại delta để `closed`. Finding mà cách sửa là hoàn tác yêu cầu người dùng đã đưa → hỏi lại trước.
