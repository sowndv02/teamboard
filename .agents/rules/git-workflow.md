# Git workflow rules

Giá trị cụ thể (prefix, mẫu branch, repo) lấy từ `config/project.yaml`.

- Mọi thay đổi gắn với một issue `TB-<số>` (file ở `document/issues/`). Chưa có thì tạo trước.
- Branch: `feature/TB-001-<slug>` từ `main` (fix/chore dùng tiền tố `fix/`, `chore/`). Không làm trực tiếp trên `main`.
- Sự cố production: branch `hotfix/TB-001-<slug>` từ `main`, sửa tối thiểu + test tái hiện, cùng cổng và cách xác nhận như mọi PR (làn hotfix: `.agents/skills/workflow/references/lanes.md`).
- Merge: theo `pullRequest.mergeStrategy` trong `config/project.yaml` (squash/merge/rebase); `TODO` thì hỏi người dùng. Người dùng merge, agent không tự merge. Giữ PR nhỏ (gợi ý dưới ~400 dòng diff), quá lớn thì tách theo lát.
- Commit: định dạng, ngôn ngữ (tiếng Anh), body bắt buộc, danh sách type và ví dụ nằm ở `git-commit.md`. Tóm tắt: `type(TB-NNN): summary` + body giải thích vì sao. Commit nhỏ, mỗi commit một ý và build được.
- Chỉ commit/push/tạo PR khi người dùng yêu cầu. Không force-push, không `--no-verify`, không `reset --hard` khi chưa được đồng ý.
- PR dùng `gh`: tiêu đề `TB-001: <tóm tắt>`, mô tả gồm link file issue, thay đổi chính, cách kiểm chứng, ghi chú rủi ro/migration.
- Trước khi merge PR: chạy review theo `code-review.md` (mặc định report-only).
- Trước khi mở PR: build + test xanh, diff không có secret, file sinh ra, hay thay đổi ngoài phạm vi.
- Đổi trạng thái issue (file + bảng `document/issues/README.md`) trong cùng thay đổi.
- Cập nhật `PROJECT.md` → "Trạng thái hiện tại" khi hoàn thành một giai đoạn.
