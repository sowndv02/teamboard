---
name: workflow
description: Điều phối quy trình từ yêu cầu đến Pull Request, chọn làn theo cỡ thay đổi (nhỏ/vừa/lớn/hotfix) - spec, review spec, system design, review design, plan, review plan, test (TDD/BDD), review test, coding, review code, tạo PR - với vòng lặp review/confirm/fix cho đến khi không còn điểm cần người dùng chốt. Dùng khi người dùng muốn làm một tính năng từ đầu đến cuối, tiếp tục workflow dở, hoặc hỏi đang ở bước nào.
---

# Workflow: từ yêu cầu đến PR

Orchestrator: xác định **đang ở bước nào**, chạy skill của bước đó, thực thi **cổng chuyển bước**, và đảm bảo mọi quyết định thuộc về **người dùng**. Không tự chốt, không tự đưa quyết định thay người dùng.

**Bước 0 - phân làn:** trước mọi việc, chọn làn theo `references/lanes.md` (nhỏ/vừa/lớn/hotfix) và ghi vào issue. Bảng bước dưới đây là **làn lớn đầy đủ**; làn nhỏ/vừa bỏ hoặc gộp bước theo `lanes.md`, nhưng không bỏ TDD, gate và xác nhận của người dùng. Việc phình ra giữa chừng thì nâng làn.

Rule liên quan: `pipeline-lifecycle.md` (bản đồ giai đoạn tổng quát), `agent-conduct.md`, `docs-update-specs.md`, `git-workflow.md`, `git-commit.md`. Giao thức review: `references/review-loop.md` (đọc trước khi chạy bất kỳ bước review nào); quan điểm review từng phase và lăng kính Chất lượng/Bảo mật/Chi phí: `references/review-philosophy.md`; nguyên tắc sinh artifact (plan of record, discovery, không bịa, cỡ bước): `references/generation-principles.md`.

## Các bước (làn lớn)

| # | Bước | Skill | Phạm vi | Đầu ra | Điều kiện sang bước sau |
|---|---|---|---|---|---|
| 1 | Clear spec | `requirements-discovery` | Theo tính năng | `document/spec/<f>.md` (`draft`) | Spec đầy đủ mẫu, người dùng đã trả lời các câu hỏi chặn |
| 2 | Review spec | `spec-review` | Theo tính năng | Spec `confirmed` | **PASS** (0 CONFIRM mở) |
| 3 | System design | `system-design` | Theo tính năng | `document/design/<f>.md` (`draft`) | Design đầy đủ mẫu |
| 4 | Review design | `design-review` | Theo tính năng | Design `confirmed` | **PASS** |
| 4b | Chia lát (slicing) | `plan-init` (chế độ `slices`) | Theo tính năng | `document/plans/<f>-slices.md` (`approved`), issue cho lát đầu | Người dùng duyệt bảng lát; mọi FR/NFR Must/Should có lát (`references/slicing.md`) |
| 5 | Plan init | `plan-init` | Theo issue (lát) | `document/plans/TB-NNN-*.md` (`draft`) | Plan đầy đủ mẫu |
| 6 | Review plan | `plan-review` | Theo issue | Plan `approved` | **PASS** |
| 7 | Test init | `test-init` | Theo issue | `.feature` + test thực thi, chạy ra **đỏ đúng lý do** | Test đỏ đúng lý do |
| 8 | Review test | `test-review` | Theo issue | Test `ready` | **PASS** |
| 9 | Coding start | `coding-start` | Theo issue | Code làm test **xanh toàn bộ** + gate xanh | **Mọi test pass** |
| 10 | Review code | `coding-review` | Theo issue | Verdict merge-ready | **PASS** |
| 11 | Create PR | `create-pr` | Theo issue | PR mở | Người dùng xác nhận trước khi push/mở PR |
| 12 | Demo/UAT và bàn giao (tuỳ chọn: làn lớn có khách) | `project-manager` (`handover`) | Theo tính năng | Demo cho khách, UAT được duyệt, tài liệu bàn giao/runbook | Khách (hoặc người dùng thay mặt) chấp nhận |

Làn **vừa**: gộp bước 1–2 thành spec ngắn trong issue, chỉ chạy 3–4 khi chạm contract/kiến trúc. Làn **nhỏ**: chỉ test tái hiện (nếu là bug) → code → gate → PR. Chi tiết và hotfix: `references/lanes.md`.

Mặc định: bước 1–4b làm **theo tính năng** (một spec/design chia thành nhiều lát/issue), bước 5–11 làm **theo issue** (một lát giao được). Một tính năng nhỏ có thể chỉ có một issue và bỏ 4b.

**Spec lớn không implement một lần.** Sau bước 4, bước 4b chia thành các lát nhỏ (`references/slicing.md`), người dùng duyệt, rồi bước 5–11 chạy cho **từng lát một**; plan của lát N+1 chỉ lập sau khi lát N merge (hoặc khi người dùng cho làm song song lát độc lập). Hết lát này thì quay lại bước 5 cho lát kế, không quay lại bước 1.

## Quyết định đã chốt với người dùng (2026-10-21)

- **Đơn vị:** spec/design theo tính năng; plan → PR theo từng issue (lát).
- **Plan:** file riêng `document/plans/TB-NNN-<slug>.md`.
- **Commit cục bộ:** hỏi người dùng một lần ở `coding-start` rồi nhớ trong phiên; không bao giờ push khi chưa được xác nhận (`create-pr`).
- **Review độc lập:** chạy bước review lớn (design, plan, coding) trong sub-agent riêng; hỏi người dùng nếu chưa rõ; việc nhỏ có thể chạy trong phiên hiện tại.

## Sẵn sàng (DoR) và Xong (DoD)

- **DoR** trước bước 5 (plan): spec `confirmed` có tiêu chí chấp nhận kiểm chứng được; phụ thuộc và rủi ro chính đã biết; ước lượng dạng khoảng kèm giả định (`project-manager`); không còn câu hỏi mở mức `blocker`. Chưa đạt → quay lại spec, không ép plan.
- **DoD** của issue (sau merge, người dùng thực hiện hoặc xác nhận): CI xanh trên nhánh chính; deploy môi trường dev và smoke test (`/health/ready`, kịch bản chính); có log/trace/metric để quan sát; tài liệu, runbook, ADR cập nhật; issue `done`; tổng kết bài học. Chưa deploy được thì ghi rõ "chưa kiểm chứng trên môi trường chạy".

## Tracker ngoài và người review thật

Mặc định issue là file Markdown (`issueTracker.provider: markdown`). Dự án thật thường dùng Jira/GitHub Issues. Khi `provider` khác `markdown`: giữ key trong tên branch/commit/PR, đặt link tracker ở đầu file issue và cập nhật trạng thái ở tracker theo quy ước đội. Review của **người thật** (đồng đội, code owner) bổ sung hoặc thay review của agent; comment của họ xử lý như CONFIRM/FIX trong `coding-review`.

## Cổng cứng

1. **Không bắt đầu bước N+1 khi bước N chưa đạt điều kiện.** Bước review chỉ đạt khi verdict `PASS`.
2. **Bước review còn CONFIRM mở → dừng và hỏi người dùng.** Không tự chọn phương án khuyến nghị, không "tạm giả định rồi đi tiếp".
3. **Sau khi người dùng chốt → sửa artifact → chạy lại review**, lặp đến khi không còn CONFIRM mở (`references/review-loop.md`).
4. **Trước bước 10 (review code), mọi test phải pass**: chạy test (liên quan hoặc đầy đủ theo `quality-gate.md`) và gate; có test fail → quay lại bước 9, không review.
5. **Test sinh ra theo TDD/BDD**: kịch bản `.feature` và test đỏ có **trước** code production (bước 7–8 trước bước 9). Không viết code cho hành vi chưa có test đỏ.
6. **Bước 11 cần người dùng xác nhận** trước khi push hay tạo PR (hành động ra ngoài); không tự làm.
7. **Quay lui**: review phát hiện vấn đề thuộc artifact của bước trước (ví dụ coding-review thấy hành vi spec không nói) → ghi rõ, hỏi người dùng có quay lại bước đó không; artifact bước trước sửa xong thì chạy lại review của nó, rồi các bước sau bị ảnh hưởng.
8. **Cổng xác nhận (áp cho mọi bước, không chỉ review): còn điểm cần người dùng xác nhận thì bước KHÔNG được `done`/PASS/`confirmed`/`approved`.** Điểm cần xác nhận gồm: mục `confirm_pending` của bước; mục Confirm log ghi "chờ xác nhận" (kể cả "Agent chọn theo uỷ quyền"); giả định hoặc câu hỏi mở do agent đặt mà người dùng chưa xác nhận; đề xuất cần người dùng duyệt (ADR, issue, đổi rule/skill) chưa được duyệt. Khi có: bước ở `needs_confirm`, hỏi người dùng (`AskUserQuestion`), ghi `next_action`; không "tạm giả định rồi đánh dấu xong", không coi im lặng là đồng ý. Ngoại lệ duy nhất: người dùng nói rõ "hoãn/chấp nhận rủi ro" cho điểm đó (`review-loop.md` quy tắc 8), ghi người chịu trách nhiệm. Chi tiết và cách kiểm trước khi ghi `done`: `references/state-protocol.md` mục 3 quy tắc 8.

## Khi người dùng đổi phạm vi giữa chừng

Không lách plan. Quy trình:
1. **Dừng** bước đang làm; ghi yêu cầu đổi nguyên văn vào Confirm log của artifact bị ảnh hưởng.
2. Xác định **artifact cao nhất** bị đổi (spec? design? plan?) và các artifact phụ thuộc phía sau (test, code).
3. Cập nhật artifact cao nhất trước (`docs-update-specs.md`: spec là nguồn hiện hành; không sửa issue/ADR cũ, đổi quyết định kiến trúc = ADR mới), rồi **chạy lại review của nó** đến PASS.
4. Cập nhật lần lượt các artifact thấp hơn (plan → `.feature`/test → code), mỗi cái qua review của nó. Đánh dấu bước đã `PASS` nhưng bị ảnh hưởng là cần kiểm lại trong Workflow.
5. Nếu thay đổi **hoàn tác** một quyết định người dùng chốt trước đó, hỏi xác nhận tác động (công sức đã làm, test/code phải bỏ) trước khi tiến hành.
6. Phạm vi mới lớn → đề xuất **issue mới** thay vì nhồi vào lát hiện tại.

## Lưu trạng thái (để agent khác và sub-agent theo dõi)

Bắt buộc ở làn lớn; tuỳ chọn ở làn vừa; làn nhỏ/hotfix ghi thẳng vào issue. Mỗi lần qua một bước, ghi state vào `document/workflow/<id>.yaml` (mẫu `_template.yaml`) theo `references/state-protocol.md`: trạng thái bước, vòng, `owner`, bằng chứng, `confirm_pending`, `subtasks`, `next_action`, và nhật ký sự kiện chỉ-thêm. Chỉ orchestrator ghi trạng thái bước; sub-agent ghi bản ghi subtask của mình và báo cáo theo định dạng bắt buộc, orchestrator **xác minh rồi mới tin**. Giao việc cho sub-agent dùng **gói giao việc** trong giao thức (mục tiêu, file đọc trước, `allowed_paths`, tiêu chí xong, nơi báo cáo). Artifact đổi thì các bước phía sau `invalidated` cho đến khi review lại.

## Xác định bước hiện tại (resume)

Đọc **state file trước**, rồi **kiểm chứng bằng bằng chứng thật** (hash `inputs`, `git status`, chạy lại test/gate) thay vì tin; có sai lệch thì `reconciled`/`invalidated`. Nếu chưa có state file, suy ra từ artifact như bảng dưới rồi **tạo state file**. **Bước đầu tiên chưa đạt** là bước hiện tại:

| Bước | Nơi ghi trạng thái | Đạt khi |
|---|---|---|
| 1–2 | `document/spec/<f>.md`, front matter `status` | `confirmed` |
| 3–4 | `document/design/<f>.md`, `status` | `confirmed` |
| 4b | `document/plans/<f>-slices.md`, `status` + cột Trạng thái (lát đầu tiên chưa `done` là lát hiện tại) | `approved` |
| 5–6 | `document/plans/TB-NNN-*.md`, `status` | `approved` |
| 7–8 | Issue `TB-NNN`, mục **Workflow** (bảng bước), và `.feature` + test | Bảng ghi `test-review: PASS` |
| 9–10 | Issue, mục **Workflow** | `coding-start: tests xanh`, `coding-review: PASS` |
| 11 | Issue (`status: review`, link PR) | PR mở |

Mục **Workflow** trong file issue (tạo nếu chưa có, chỉ khi branch hiện tại khớp key issue, theo `docs-update-specs.md`):

```
| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | | |
| plan-review | PASS | 3 | | |
| test-init | done | – | | đỏ: 12 test |
| test-review | PASS | 2 | | |
| coding-start | done | – | | xanh: 12/12 |
| coding-review | PASS | 2 | | |
| create-pr | pending | – | | |
```

Không chắc đang ở đâu (trạng thái mâu thuẫn, artifact thiếu) → **hỏi người dùng**, không đoán.

## Cách chạy

1. **Khởi động:** xác định tính năng/issue (hỏi nếu mơ hồ), đọc trạng thái, báo ngắn: "Đang ở bước X; sẽ làm Y".
2. **Chạy bước hiện tại** bằng skill tương ứng. Với bước review, chạy `references/review-loop.md` đến khi PASS hoặc cần hỏi người dùng.
3. **Khi bước đạt:** ghi trạng thái, báo ngắn kết quả (artifact, số vòng, quyết định đã chốt), rồi **tự động sang bước kế** nếu không có điểm cần người dùng; dừng khi:
   - Có CONFIRM chờ người dùng.
   - Hết lô công việc theo yêu cầu của người dùng (ví dụ "chỉ làm đến design").
   - Gặp hành động cần xác nhận (commit/push/PR, thao tác rủi ro).
   - Gate cứng không đạt và không tự sửa được sau 2 lần thử khác nhau.
4. **Báo cáo cuối lượt:** bước đang ở, việc đã làm, việc đang chờ người dùng.

## Chế độ

| Yêu cầu của người dùng | Hành động |
|---|---|
| "Làm tính năng X từ đầu" | Bắt đầu từ bước 1 |
| "Tiếp tục" / "đang ở đâu?" | Xác định bước hiện tại và tiếp tục / báo trạng thái |
| "Làm đến hết design" | Chạy bước 1–4 rồi dừng |
| "Chỉ chạy review ..." | Chạy riêng skill review đó với vòng lặp |
| "Quay lại sửa spec/design" | Sửa artifact, chạy lại review của nó, rồi đánh dấu các bước sau cần kiểm lại |

## Bài học (lessons learned) trong workflow

- **Trước khi làm** (`plan-init`, `coding-start`): tra `document/lessons/README.md` theo tag của vùng/service đang làm; đưa lesson liên quan vào plan/test/code (skill `lesson-learned`, chế độ `search`).
- **Trong khi làm:** finding review P0–P2 có nguyên nhân hệ thống, người dùng sửa cách làm của agent, test/gate fail do giả định sai → đề xuất ghi bài học ngay (bản nháp ngắn).
- **Sau `create-pr` (tổng kết):** hỏi "có gì sai/đáng nhớ trong lát này không?"; nếu có, chạy `lesson-learned` `capture`; nếu không, ghi sự kiện `lessons_recorded: none` vào state. Biện pháp phòng ngừa chạm rule/skill/gate là thay đổi thật: đề xuất cho người dùng, làm trong issue/commit rõ ràng.
- Lesson lặp lại → nâng cấp biện pháp (ưu tiên tự động hoá), không chỉ ghi thêm.

## Phối hợp với rule dự án

- Một issue một branch (`feature/TB-NNN-<slug>`); bước 5 trở đi cần issue và branch (tạo theo `git-workflow.md`, không `push` khi chưa được yêu cầu).
- Commit chỉ khi người dùng cho phép (xem mục xác nhận ở `coding-start`); định dạng `git-commit.md`.
- Mỗi bước cập nhật file issue (Workflow, Kịch bản và TDD log, Review rounds) trong cùng thay đổi.
- Vi phạm rule/ADR phát hiện giữa chừng → ghi finding, không lách.

## Không làm

- Không bỏ bước **của làn đã chọn**, không đảo thứ tự, không gộp bước review vào bước làm. Không tự hạ làn để né bước; làn mơ hồ thì chốt với người dùng.
- Không chốt thay người dùng; không áp dụng phương án "khuyến nghị" khi chưa được trả lời.
- Không sửa artifact của bước trước một cách im lặng; mọi thay đổi ghi vào Confirm log/lịch sử.
- Không sang bước 10 khi còn test fail; không tạo PR khi chưa được xác nhận.
