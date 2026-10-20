# Pipeline lifecycle (từ issue đến merge)

Lý do tồn tại: một thứ tự và các cổng chuyển giai đoạn thống nhất để mọi issue đi qua cùng một đường, không nhảy bước và không "xong" mà thiếu kiểm chứng. File này là **bản đồ**: mỗi giai đoạn trỏ tới rule chi tiết, không lặp lại nội dung.

Không có công cụ điều phối tự động (không CLI session, không skill). Agent tự theo bảng này và ghi trạng thái vào **file issue** (nguồn sự thật duy nhất, xem `docs-update-specs.md`). Người dùng chỉ cần nói "làm TB-002", "review TB-002", "mở PR cho TB-002"; agent suy ra giai đoạn từ trạng thái trong issue.

## Điều phối bằng skill `workflow`

Chuỗi skill thực thi quy trình này: `requirements-discovery` → `spec-review` → `system-design` → `design-review` → `plan-init` → `plan-review` → `test-init` → `test-review` → `coding-start` → `coding-review` → `create-pr`, do skill `workflow` điều phối. Mỗi bước review lặp (hỏi người dùng chốt → sửa → review lại) đến khi không còn điểm cần xác nhận mới sang bước sau; test phải pass hết trước `coding-review`. Chi tiết: `.agents/skills/workflow/SKILL.md` và `references/review-loop.md`.

## Các giai đoạn

| # | Giai đoạn | Việc làm | Rule chi tiết | Đầu ra / cổng ra |
|---|---|---|---|---|
| 0 | **Sẵn sàng phiên** | Đọc `PROJECT.md`, `config/project.yaml`, rule R0; chạy lệnh đầu phiên | `AGENTS.md`, `COMMANDS.md` §1 | Biết đang ở branch/giai đoạn nào; môi trường chạy được |
| 1 | **Issue** | Tạo hoặc chọn issue `TB-NNN`; tiêu chí hoàn thành kiểm chứng được; phạm vi. Yêu cầu còn mơ hồ → dùng skill `requirements-discovery` để ra spec trước, rồi chia issue từ spec | `docs-conventions.md`, `document/issues/` | Issue có tiêu chí; trạng thái `todo` |
| 2 | **Branch** | Tạo `feature/TB-NNN-<slug>` từ `main`; issue → `in-progress` | `git-workflow.md` | Branch đúng key; issue `in-progress` |
| 3 | **Thiết kế & kế hoạch** (khi việc lớn) | Spec đã rõ mà có quyết định kiến trúc lớn → skill `system-design` (design doc + ADR, giải thích đánh đổi); chọn dịch vụ AWS cho từng service → skill `aws-service-selection` (chi phí đi đầu, tài liệu `document/infra/<service>.md`). Kế hoạch ngắn, chờ người dùng duyệt (plan mode) | `task-workflow.md`, `architecture.md` | Kế hoạch được duyệt; ADR (nếu cần) |
| 4 | **Spec & kịch bản** | Cập nhật spec/contract; viết `.feature` theo tiêu chí | `docs-update-specs.md`, `api-endpoints.md`, `test-review.md` | `.feature` + spec cập nhật |
| 5 | **Test review** | Review `.feature` | `test-review.md` | Verdict `Sẵn sàng code` (không P0–P2, P3 đã xử lý) |
| 6 | **Code (TDD/BDD)** | Đỏ → xanh → refactor từng kịch bản, theo lát nhỏ | `task-workflow.md`, `testing.md`, rule theo vùng (`dotnet.md`, `backend-engineering.md`, `angular.md`, …) | Mọi kịch bản xanh; nhật ký đỏ/xanh trong issue |
| 7 | **Gate** | Chạy gate theo vùng, đối chiếu kịch bản ↔ test, tự review diff | `code-change-gate.md`, `quality-gate.md`, `test-review.md` mục 6 | Gate xanh; `Sẵn sàng review` |
| 8 | **Code review** | Review vòng 1 (đầy đủ) | `code-review.md` | Verdict + bảng findings; ghi vào issue |
| 9 | **Sửa finding** | Sửa theo lô nhỏ, có test; chạy lại gate | `code-review.md` (mục sửa finding) | Finding `resolved` |
| 10 | **Review delta** | Review lại phần thay đổi; đóng finding | `code-review.md` (vòng 2+) | `Sẵn sàng merge`; không P0–P2 mở, không P3 `open` |
| 11 | **PR** | Mở PR (chỉ khi người dùng yêu cầu); issue → `review` | `git-workflow.md` | PR mở, mô tả có link issue + cách kiểm chứng |
| 12 | **Merge & đóng** | Người dùng merge; cập nhật issue → `done`, mục lục issue, `PROJECT.md` (nếu xong giai đoạn), runbook/ADR nếu phát sinh | `docs-update-specs.md`, `git-workflow.md` | Issue `done`; tài liệu khớp |

Thứ tự mặc định: 0 → 1 → 2 → (3) → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12. Giai đoạn 8–10 lặp đến khi hết finding chặn. Không nhảy cóc; không bắt đầu giai đoạn sau khi cổng ra của giai đoạn trước chưa đạt (trừ khi người dùng duyệt tường minh, và ghi lý do vào issue).

## Cổng chuyển giai đoạn

| Cổng | Điều kiện | Nếu chưa đạt |
|---|---|---|
| **G-start** (vào giai đoạn 2+) | Issue tồn tại, có tiêu chí hoàn thành, key khớp tên branch | Quay lại giai đoạn 1; hỏi người dùng nếu yêu cầu mơ hồ |
| **G-code** (vào giai đoạn 6) | `.feature` đã `Sẵn sàng code`; spec/contract liên quan đã cập nhật | Quay lại giai đoạn 4–5. Ngoại lệ "light lane" bên dưới |
| **G-review** (vào giai đoạn 8) | Gate xanh (`code-change-gate.md`); bảng kịch bản ↔ test không có gap; `.feature` không còn `@wip` | Sửa tại giai đoạn 6–7 |
| **G-merge** (verdict `Sẵn sàng merge`) | Không P0–P2 mở; không P3 `open` (đã `closed`/`skipped` có lý do); probe áp dụng đã kiểm chứng; không rủi ro tiềm ẩn chưa được người dùng chấp nhận | Quay lại giai đoạn 9–10 |
| **G-PR** (vào giai đoạn 11) | `Sẵn sàng merge`; người dùng yêu cầu mở PR; diff sạch (không secret/file sinh ra) | Dừng, báo lý do |

Ghi đè cổng (`--force` kiểu) chỉ khi người dùng duyệt tường minh; ghi vào nhật ký issue: ai duyệt, bỏ cổng nào, vì sao.

## Trạng thái issue

`todo` → `in-progress` (giai đoạn 2) → `review` (giai đoạn 11, PR mở) → `done` (giai đoạn 12, đã merge). `dropped` khi huỷ (ghi lý do). Đổi trạng thái thì cập nhật cả file issue và bảng `document/issues/README.md` trong cùng thay đổi.

## Làn nhẹ (light lane)

Không phải việc nào cũng cần đủ 12 bước. Việc thuộc nhóm sau đi **làn nhẹ**: bỏ giai đoạn 3–5 (kế hoạch, kịch bản, test review) nhưng **vẫn** có issue, branch, gate, review diff, và PR/commit theo quy ước:

- Chỉ sửa tài liệu, comment, định dạng, đổi tên không đổi hành vi.
- Sửa cấu hình không ảnh hưởng hành vi chạy (ví dụ cập nhật `config/*.yaml` cho khớp).
- Sửa lỗi nhỏ một file đã có test bao phủ, kèm test tái hiện nếu chưa có.

**Không** vào làn nhẹ nếu: đổi hành vi hay contract (API, event), đổi kiến trúc/ranh giới service, đụng bảo mật, migration, hạ tầng, hoặc người dùng yêu cầu đủ quy trình. Không chắc → dùng làn đầy đủ.

## Làn hạ tầng (Terraform / AWS)

Thay giai đoạn 4–6 bằng: viết/sửa Terraform → `fmt`, `validate`, `plan` → trình bày kết quả `plan` (tài nguyên thay đổi, chi phí) cho người dùng. **`apply`/`destroy` chỉ khi người dùng đồng ý tại thời điểm đó** (`terraform.md`). Sau apply: kiểm chứng (health, truy cập) và ghi vào issue. Gate/review/PR giữ nguyên.

## Review độc lập (khuyến nghị)

Review (giai đoạn 8, 10) hiệu quả hơn khi do một phiên/sub-agent **khác** với phiên đã viết code (không bị neo vào cách đã làm). Khi dùng sub-agent: đưa **code/diff và rule**, không đưa kết luận của mình; kết quả trả về là bằng chứng, vẫn phải tự kiểm trước khi tin (`agent-conduct.md`). Không bắt buộc cho việc nhỏ.

## Khi bị chặn hoặc có sự cố

- Gate/test fail không tự sửa được sau 2 lần thử khác nhau → dừng, báo cáo đã thử gì, hỏi người dùng (`task-workflow.md`).
- Phát hiện yêu cầu mâu thuẫn tài liệu, hoặc spec/ADR sai → hỏi người dùng; không tự sửa tài liệu lịch sử (`docs-update-specs.md`).
- Phát hiện việc ngoài phạm vi → ghi vào mục Ghi chú của issue hoặc đề xuất issue mới; không làm trong cùng branch.
- Sự cố khi vận hành/debug → theo `observability.md`, ghi runbook ở giai đoạn 12.

## Làm việc song song nhiều issue

- Mỗi issue một branch; không trộn thay đổi giữa các issue trong một commit/PR.
- Hai issue sửa cùng file hoặc cùng service → làm tuần tự, hoặc dùng worktree riêng; không để hai phiên ghi cùng tệp.
- Issue phụ thuộc nhau → ghi phụ thuộc trong issue ("Chặn bởi TB-NNN"), merge theo thứ tự.

## Ngoài vòng đời

- **Commit/push ngoài vòng đời** chỉ khi người dùng yêu cầu rõ; không thay thế các cổng trên.
- **Tư vấn/hỏi đáp/đọc code** không cần issue; không sinh thay đổi.
- **Thử nghiệm local** ở `local/` không cần issue (không commit).

## Tiêu chí "xong" của một issue

Issue chỉ `done` khi: PR đã merge; mọi tiêu chí hoàn thành đã tick **kèm bằng chứng**; spec/contract/ADR/runbook cập nhật; bảng "Kịch bản và TDD log" và "Review rounds" đầy đủ; không finding chặn mở; issue và mục lục khớp trạng thái.
