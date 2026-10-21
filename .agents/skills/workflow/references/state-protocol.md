# Giao thức lưu trạng thái workflow

Mục đích: mỗi khi qua một bước, **trạng thái được ghi lại** để agent khác (phiên khác, sub-agent, người) tiếp tục, nhận việc hoặc kiểm tra mà không phải đoán. Mọi skill bước (`requirements-discovery`, `spec-review`, ..., `create-pr`) và `workflow` phải tuân giao thức này **ở làn lớn** (bắt buộc), làn vừa (tuỳ chọn, khuyến nghị khi có sub-agent). Làn nhỏ và hotfix không cần state file: bảng Workflow trong issue là đủ (`workflow/references/lanes.md`).

## 1. Nguồn trạng thái (ai là nguồn sự thật cho cái gì)

| Nơi | Lưu gì | Ai đọc |
|---|---|---|
| **State file** `document/workflow/<id>.yaml` | Trạng thái **quy trình**: bước, vòng, người nhận việc, bằng chứng, việc con (subtask), việc tiếp theo, nhật ký sự kiện | Agent/sub-agent, `workflow` (máy đọc được) |
| Front matter của artifact (`status` trong spec/design/plan) | Trạng thái **của artifact** (`draft`/`confirmed`/`approved`) | Người và agent |
| Bảng **Workflow** trong file issue | Bản **tóm tắt cho người**, sinh từ state file | Người |
| **Confirm log / Review rounds** trong artifact | Quyết định người dùng và lịch sử review (chi tiết, nguyên văn) | Người và agent |

- `<id>`: `TB-NNN` (theo issue) hoặc `feature-<ten>` (bước theo tính năng: spec/design).
- Không trùng lặp nội dung: state file chỉ **trỏ tới** artifact và bằng chứng, không chép lại nội dung spec/design/plan.
- **Mâu thuẫn giữa các nơi:** không tin ngầm bên nào. Kiểm bằng **bằng chứng thật** (đọc artifact, chạy lại test/gate, `git status`), sửa cho khớp, ghi sự kiện `reconciled` kèm lý do. Nếu không rõ → hỏi người dùng.

## 2. Khi nào ghi (bắt buộc)

| Thời điểm | Ghi gì |
|---|---|
| **Trước khi bắt đầu** một bước | `status: in_progress`, `owner`, `started`, `inputs` (hash artifact đầu vào), sự kiện `step_started` |
| **Sau mỗi vòng review** | `round`, `verdict`, số CONFIRM/FIX mở, `confirm_pending` (nếu có), sự kiện `review_round` |
| **Khi cần người dùng chốt** | `status: needs_confirm`, danh sách câu hỏi (fingerprint + câu hỏi ngắn), `next_action: người dùng trả lời`, sự kiện |
| **Khi bị chặn** | `status: blocked`, `blocked_by` (lý do cụ thể, thiếu gì), `next_action` |
| **Khi hoàn thành** | `status: done` (review: `verdict: PASS`), `finished`, `artifacts`, `evidence`, sự kiện `step_done`, cập nhật `current_step` và `next_action` |
| **Khi giao/nhận/xong việc con** | bản ghi `subtasks[]` (xem mục 5) và sự kiện |
| **Khi artifact phía trên đổi** | các bước phía dưới đã `done` → `status: invalidated`, sự kiện `invalidated` kèm nguyên nhân |
| **Khi quay lui hoặc đổi phạm vi** | sự kiện `rewind`/`scope_change` kèm quyết định người dùng |

Sau mỗi lần ghi state file, **cập nhật bảng Workflow trong file issue** (tóm tắt) trong cùng thao tác. Ghi state **trước khi** báo kết quả cho người dùng.

## 3. Quy tắc ghi

1. **Đọc trước khi ghi** (đọc lại file ngay trước khi sửa); chỉ sửa phần của mình; không ghi đè cả file.
2. Chỉ **orchestrator** (agent điều phối chính) ghi trạng thái **bước** (`steps.*.status`, `current_step`, `next_action`). **Sub-agent** chỉ ghi bản ghi `subtasks[]` của chính mình và thêm sự kiện; kết quả của sub-agent được orchestrator **xác minh** rồi mới chuyển trạng thái bước.
3. `events` là **chỉ thêm** (append-only), không sửa/xoá sự kiện cũ. Sửa sai bằng sự kiện mới.
4. Dấu thời gian ISO 8601 UTC; `owner` dạng `<vai trò>:<định danh>` (ví dụ `orchestrator:session-ab12`, `worker:coding-b3`, `reviewer:plan-1`, `human:<tên>`).
5. Ghi **bằng chứng**, không ghi lời hứa: lệnh đã chạy + kết quả (ví dụ `dotnet test: 24/24 pass`), đường dẫn artifact, hash.
6. Không ghi secret, token, dữ liệu cá nhân.
7. State file chỉ theo dõi quy trình; **quyết định** của người dùng ghi nguyên văn ở Confirm log của artifact (state file chỉ trỏ tới fingerprint).

## 4. Nhận việc và tránh giẫm chân

- **Claim:** khi bắt đầu bước hoặc subtask, đặt `owner` và `started`; agent khác thấy bước `in_progress` có owner khác **không** làm tiếp.
- **Nghi ngờ bị bỏ dở (stale):** `in_progress` quá lâu (gợi ý > 2 giờ không có sự kiện mới) → hỏi orchestrator/người dùng trước khi giành lại; ghi sự kiện `reclaimed` kèm lý do. Không tự ý giành.
- **Hai agent ghi cùng thư mục:** chỉ một agent sửa cùng tệp tại một thời điểm; sub-agent được giao **tập đường dẫn cho phép** (`allowed_paths`) không chồng lấn nhau. Hai việc cùng đụng một tệp → làm tuần tự (`depends_on`) hoặc dùng worktree riêng.
- **Phát hiện artifact đầu vào đổi** (hash khác với `inputs` đã ghi) → dừng, báo orchestrator; không tiếp tục trên đầu vào cũ.

Lệnh lấy hash (kể cả file chưa commit):

```bash
git hash-object document/spec/<ten>.md
```

## 5. Việc con (subtask) và giao việc cho sub-agent

Orchestrator có thể chia một bước thành **subtask** giao cho sub-agent (ví dụ viết test cho nhóm kịch bản, code một bước plan, review một lăng kính). Mỗi subtask một bản ghi trong `subtasks[]`:

```yaml
- id: ST-003
  step: coding-start
  title: Implement bước B2 của plan (đường lỗi tạo board)
  owner: worker:coding-b2
  status: in_progress          # pending | in_progress | done | failed | cancelled
  depends_on: [ST-002]
  allowed_paths: [src/board-service/Application/**, src/board-service.Tests/**]
  inputs: { plan: document/plans/TB-004-x.md#B2, scenarios: ["@S04", "@S05"] }
  done_when: "Test @S04,@S05 xanh; gate .NET xanh; không sửa ngoài allowed_paths"
  claimed: 2026-10-21T08:30:00Z
  result: null                 # điền khi xong (xem báo cáo bên dưới)
```

### Gói giao việc (handoff packet) — nội dung bắt buộc trong prompt cho sub-agent
1. **Mục tiêu** và **vì sao** (một đoạn), id subtask, đường dẫn state file.
2. **Đọc trước**: danh sách file (spec, design, plan, rule liên quan); không bắt sub-agent tự đoán nguồn.
3. **Phạm vi**: `allowed_paths`, những gì **không** được làm (không sửa `.agents/**`, không đổi artifact ngoài phạm vi, không commit/push nếu chưa được phép).
4. **Neo**: plan/`.feature` đã duyệt là cố định; điều chưa rõ → **dừng và báo lại**, không tự quyết.
5. **Tiêu chí xong** (`done_when`) và lệnh kiểm chứng phải chạy.
6. **Định dạng báo cáo** (bên dưới) và nơi ghi (`subtasks[].result`, thêm sự kiện).
7. Với **reviewer độc lập**: chỉ đưa artifact + nguồn tham chiếu + checklist, **không** đưa lập luận của người viết (`review-loop.md` mục 6).

### Báo cáo của sub-agent (bắt buộc)

```yaml
result:
  status: done | failed | blocked
  summary: <1-3 câu>
  files_changed: [<đường dẫn>]
  evidence: ["dotnet test: 12/12 pass", "gate format: ok"]
  open_questions:            # điều cần người dùng/orchestrator quyết, KHÔNG tự quyết
    - "<câu hỏi ngắn kèm bối cảnh>"
  deviations: []             # khác với plan (nếu có), kèm lý do
```

### Orchestrator xác minh trước khi tin
- Chạy lại lệnh kiểm chứng (build/test/gate), đọc `git status`/`git diff --stat`, đối chiếu `files_changed` với `allowed_paths` (ngoài phạm vi → trả lại).
- `open_questions` không rỗng → chuyển thành CONFIRM (hỏi người dùng), không đóng subtask.
- Kết quả đạt → `ST-xxx: done`, rồi mới cập nhật trạng thái bước.

## 6. Resume và tiếp nhận từ agent khác

Agent vào giữa chừng làm theo thứ tự:
1. Đọc state file (`document/workflow/<id>.yaml`): `current_step`, `next_action`, `blocked_by`, `confirm_pending`, `subtasks`, vài sự kiện cuối.
2. **Kiểm chứng trạng thái** thay vì tin: kiểm hash `inputs` so với artifact thật, `git status`/branch, chạy lại test/gate nếu bước là code, đối chiếu front matter artifact.
3. Có sai lệch → ghi sự kiện `reconciled`/`invalidated`, báo orchestrator hoặc người dùng.
4. Có `confirm_pending` → **đừng làm tiếp**; hỏi người dùng.
5. Claim bước/subtask (mục 4), rồi làm theo skill của bước.

## 7. Làm vô hiệu (invalidation)

Khi artifact đổi sau khi các bước phụ thuộc đã `done`:
- Spec đổi → design, plan, test, code (của các lát liên quan) → `invalidated`.
- Design đổi → plan, test, code → `invalidated`.
- Plan đổi → test, code → `invalidated`.
- `.feature`/test đổi → code review → `invalidated`.
Bước `invalidated` cần **review lại phần thay đổi (delta)**; không coi `PASS` cũ còn hiệu lực. Ghi nguyên nhân và artifact gây ra.

## 8. Định dạng và vị trí

- Mẫu: `document/workflow/_template.yaml`; mục lục: `document/workflow/README.md`.
- State file được commit **cùng** thay đổi của bước (theo `git-workflow.md`, `git-commit.md`; chỉ commit khi người dùng cho phép). Khi chưa commit, state vẫn nằm trong working tree dùng chung bởi các agent cùng thư mục làm việc. Agent ở **worktree khác** không thấy thay đổi chưa commit: cần commit hoặc chuyển giao tường minh (ghi chú trong `next_action`).
- Tuân `docs-update-specs.md`: state file theo issue chỉ sửa khi branch hiện tại khớp issue; state theo tính năng sửa khi đang làm tính năng đó.

## 9. Kiểm tra nhanh (checklist mỗi lần ghi)

- [ ] Đã đọc lại file ngay trước khi sửa; chỉ sửa phần của mình
- [ ] `status`, `owner`, thời gian, bằng chứng đã ghi; sự kiện đã thêm (append-only)
- [ ] `current_step` và `next_action` đúng và cụ thể (ai làm gì tiếp)
- [ ] Bảng Workflow trong issue đã cập nhật khớp
- [ ] Không secret; không chép nội dung artifact vào state
