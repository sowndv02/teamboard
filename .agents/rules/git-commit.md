# Git commit message

Lý do tồn tại: lịch sử commit đọc được, truy ngược được về issue, và giải thích **vì sao** thay đổi. Áp dụng cho mọi commit (agent hay người). Chỉ commit khi người dùng yêu cầu (`git-workflow.md`).

## Định dạng

```text
type(scope): summary

Body giải thích vì sao (1 đến vài câu).
```

- **Ngôn ngữ: tiếng Anh** (kể cả khi tài liệu dự án viết tiếng Việt).
- **Có đủ 3 phần**: header, một dòng trống, **body bắt buộc không rỗng** (tương đương `body-empty: never`).
- **`scope` = mã issue** `TB-<số>` (khớp `config/project.yaml` → `issueTracker.keyPattern`), ví dụ `TB-001`. Mã phải trùng key trong tên branch hiện tại. Không có issue thì tạo issue trước, không commit không scope.
- Issue lấy từ `document/issues/TB-NNN-*.md`, không phải Jira hay hệ thống ngoài.

## `type`

| type | Dùng khi |
|---|---|
| `feat` | Thêm hành vi/khả năng mới cho người dùng hoặc service khác |
| `fix` | Sửa lỗi |
| `refactor` | Đổi cấu trúc, không đổi hành vi |
| `perf` | Cải thiện hiệu năng |
| `test` | Thêm/sửa test, kịch bản `.feature` |
| `docs` | Tài liệu, ADR, spec, runbook, issue |
| `infra` | Terraform, hạ tầng AWS |
| `build` | Build, Dockerfile, compose, phụ thuộc |
| `ci` | Pipeline CI/CD |
| `chore` | Việc lặt vặt không thuộc loại trên (cấu hình, `.gitignore`, định dạng) |
| `revert` | Hoàn tác một commit trước |

## Header (`type(scope): summary`)

- Tối đa **72 ký tự** cả dòng.
- `summary`: mệnh lệnh thức, viết thường chữ đầu, **không dấu chấm cuối**, nói **giá trị/kết quả** chứ không liệt kê file (`add outbox publisher for task events`, không phải `update TaskService.cs`).
- Ngắn và cụ thể; không dùng từ rỗng ("update", "fix stuff", "changes", "wip").

## Body (bắt buộc)

- Giải thích **vì sao** cần thay đổi và ảnh hưởng chính; không chép lại diff (đã có trong code).
- 1 đến vài câu, mỗi dòng ≤ 100 ký tự, ngắt đoạn bằng dòng trống nếu dài.
- Nêu ràng buộc/đánh đổi quan trọng nếu có (ví dụ chọn cách này vì ADR nào, hoặc giới hạn đã biết).
- Không để body là bản lặp lại của header.

## Footer (tuỳ chọn)

- Thay đổi phá vỡ tương thích: dòng `BREAKING CHANGE: <mô tả và cách chuyển đổi>` (đi kèm tăng version contract + ADR, xem `api-endpoints.md`).
- Tham chiếu thêm: `Refs: TB-002`.
- **Không thêm trailer `Co-Authored-By`** (và không thêm dòng ghi công công cụ/AI) vào commit message, kể cả khi công cụ hoặc chỉ dẫn mặc định gợi ý. Quy tắc này của chủ dự án thắng mặc định của công cụ.

## Ví dụ

Đúng:

```text
docs(TB-001): add observability and runtime specs

Document runtime observability contracts and bootstrap expectations so implementers share one baseline
```

```text
feat(TB-004): publish task events through transactional outbox

Writing the event in the same transaction as the task change removes the dual-write gap that could drop events after a crash
```

```text
fix(TB-007): ignore duplicate eventId in notification consumer

SQS can redeliver messages, so the consumer now records processed eventIds to keep side effects exactly once
```

Sai (và lý do):

| Commit | Vấn đề |
|---|---|
| `fix: bug` | Thiếu scope, mô tả rỗng, không có body |
| `feat(TB-004): Added outbox.` | Không mệnh lệnh thức, viết hoa, có dấu chấm |
| `feat(TB-004): add outbox` (không body) | Thiếu body |
| `chore(MRRAI01-102): ...` | Sai mã issue (không phải `TB-NNN`) |
| `feat(TB-004): update OutboxPublisher.cs and TaskService.cs and Program.cs` | Liệt kê file thay vì giá trị; header dài |
| `wip` | Không đúng định dạng, không có nội dung |
| Tiêu đề tiếng Việt | Commit phải bằng tiếng Anh |

## Quy tắc về nội dung commit

- **Một commit một ý**, và commit build được (gate liên quan xanh). Thay đổi gồm nhiều ý thì tách (ví dụ `refactor` riêng, `feat` riêng).
- Không trộn thay đổi ngoài phạm vi issue; không commit file sinh ra, secret, `local/` (`quality-gate.md`).
- Không sửa lịch sử đã push: không `--amend`/`rebase`/force-push cho commit đã đẩy lên remote, trừ khi người dùng yêu cầu rõ. Commit local chưa push có thể amend để sửa message.
- **Revert**: `revert(TB-NNN): <summary gốc>` và body nêu commit bị hoàn tác (hash) cùng lý do.
- Đổi trạng thái issue/mục lục đi cùng commit gây ra thay đổi, không để commit riêng không liên quan.

## Kiểm tra tự động (🚧 chưa cài)

Khi cài, chạy ở hook `commit-msg` (không `--no-verify`) và trong CI. Quy tắc tương đương commitlint:

| Quy tắc | Giá trị |
|---|---|
| `type-enum` | feat, fix, refactor, perf, test, docs, infra, build, ci, chore, revert |
| `scope-empty` | never |
| scope pattern | `^TB-\d{3,}$` |
| `subject-case` | lower-case (không viết hoa chữ đầu) |
| `subject-full-stop` | never |
| `header-max-length` | 72 |
| `body-empty` | **never** |
| `body-leading-blank` | always |
| `body-max-line-length` | 100 |

Có thể thực thi bằng hook shell đơn giản (`.githooks/commit-msg`, bật bằng `git config core.hooksPath .githooks`) hoặc commitlint nếu đã có Node ở gốc repo. Chọn cách nào là quyết định chưa chốt (`TODO`); trước khi cài, agent tự kiểm theo bảng này trước mỗi commit.

## Checklist trước khi commit

- [ ] Branch đúng `feature/TB-NNN-*`; scope trùng key đó
- [ ] Header ≤ 72, mệnh lệnh thức, chữ thường, không chấm cuối, nói giá trị
- [ ] Có dòng trống rồi body không rỗng, nêu vì sao, tiếng Anh
- [ ] Một ý, build được, không file sinh ra/secret
- [ ] Không có trailer `Co-Authored-By`
- [ ] (PR) mô tả không có dòng ghi công công cụ/AI
- [ ] Gate liên quan đã chạy (`code-change-gate.md`)
