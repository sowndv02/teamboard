---
name: create-pr
description: Chuẩn bị và tạo Pull Request GitHub cho một issue đã qua coding-review PASS - kiểm tra lần cuối, soạn tiêu đề và mô tả, xin người dùng xác nhận rồi mới push và mở PR. Dùng ở cuối workflow.
---

# Create PR

Bước cuối của workflow. Hành động **ra ngoài** (push, tạo PR trên GitHub) nên **luôn cần người dùng xác nhận tại thời điểm đó**; không tự làm chỉ vì đến bước này.

Rule liên quan: `git-workflow.md`, `git-commit.md`, `code-change-gate.md`, `quality-gate.md`, `agent-conduct.md` (không tạo issue/PR thử nghiệm trên hệ thống thật).

## Cổng đầu vào
1. `coding-review: PASS` trong Workflow của issue; test và gate **xanh ở lần chạy cuối** (chạy lại ngay trước khi tạo PR).
2. Branch `feature/TB-NNN-*` khớp issue; working tree sạch (không thay đổi chưa commit ngoài ý muốn).
3. Mọi commit theo `git-commit.md` (tiếng Anh, `type(TB-NNN): summary`, body bắt buộc). Có commit sai định dạng → báo người dùng; sửa message (rebase/amend) **chỉ khi** commit chưa push và người dùng đồng ý.
4. `config/project.yaml` → `pullRequest.repository` **không** phải `TODO`. Còn `TODO` → hỏi người dùng `owner/name` (và remote đã cấu hình), không đoán.
5. Tài liệu khớp: spec/design/plan/ADR/runbook/`config/` đã cập nhật cho thay đổi; issue ghi đủ TDD log, Review rounds, Confirm log.

## Quy trình
1. **Kiểm tra lần cuối** (chỉ đọc/chạy kiểm tra): gate đầy đủ, `git status`, `git diff <base>...HEAD --stat`, quét secret thô; chắc chắn diff chỉ gồm phạm vi issue; không có file sinh ra, `local/`, secret.
2. **Soạn PR** (hiển thị cho người dùng duyệt, chưa tạo):
   - **Tiêu đề:** `TB-NNN: <tóm tắt ngắn>` (theo `config/project.yaml`).
   - **Mô tả:** mục tiêu và vì sao; link file issue/spec/design/plan; tóm tắt thay đổi chính theo service; **cách kiểm chứng** (lệnh test, kịch bản đã chạy, kết quả); tác động (API/contract, database/migration, consumer: service khác); rủi ro/giới hạn đã biết; các quyết định đã được người dùng chốt (từ Confirm log, tóm tắt); checklist gate đã chạy.
   - Nhãn/reviewer nếu repo có quy ước (không đoán).
3. **Xin xác nhận** người dùng (công cụ hỏi): tiêu đề, mô tả, nhánh đích (mặc định `main`), repo. Chỉ sau khi được xác nhận mới tiếp tục.
4. **Push và tạo PR:** `git push -u origin <branch>` rồi `gh pr create` (`--base main`, tiêu đề, mô tả). Không `--force`, không `--no-verify`. Nếu đã có PR mở cho branch → **cập nhật** thay vì tạo trùng; báo người dùng.
5. **Sau khi tạo:** ghi URL PR vào file issue, đổi `status: review`, cập nhật bảng `document/issues/README.md` và Workflow (`create-pr: done`). Nếu môi trường có công cụ theo dõi PR/CI thì liên kết PR với phiên; báo trạng thái CI hiện có, không tự thăm dò lặp.
6. **Báo người dùng:** URL PR, tóm tắt, việc còn lại (chờ review/CI, merge do người dùng).

## Không làm
- Không push/tạo PR khi chưa được xác nhận ở bước 3; không tự merge, không bật auto-merge.
- Không tạo PR nháp hay PR thử nghiệm để kiểm tra công cụ.
- Không bỏ qua cổng đầu vào, kể cả khi người dùng thúc giục (nêu lý do và hỏi).
- Không sửa lịch sử đã push; không force-push.

## Hoàn thành khi
- [ ] Gate và test xanh ngay trước khi tạo
- [ ] Người dùng đã xác nhận nội dung và nhánh đích
- [ ] PR mở (hoặc cập nhật) đúng repo/nhánh; URL ghi vào issue; trạng thái issue `review`
- [ ] Workflow `create-pr: done`; báo cáo cho người dùng

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
