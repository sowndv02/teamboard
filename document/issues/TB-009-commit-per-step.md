---
key: TB-009
title: Commit file của từng bước workflow ngay khi bước đạt
status: review
phase: 0
branch: feature/TB-009-commit-per-step
estimate: S
created: 2026-10-22
started: 2026-10-22
closed: null
---

# TB-009 — Commit file của từng bước workflow ngay khi bước đạt

## Bối cảnh
Hiện quyền commit chỉ được hỏi ở `coding-start`, nên artifact của các bước trước (spec, design, plan, test) nằm dồn trong working tree, không có mốc lịch sử riêng và agent ở worktree khác không thấy. Người dùng đề xuất: xong bước nào thì commit file của bước đó trước khi sang bước kế.

## Phạm vi
- Làm: (1) skill `workflow`: hỏi quyền commit **một lần ở đầu workflow**, mục "Commit theo bước" (khi nào, gồm file nào, message, điều kiện), sửa các câu nói commit chỉ hỏi ở `coding-start`; (2) `coding-start`: trỏ về quyền đã hỏi ở đầu; (3) `state-protocol.md`, `git-workflow.md`, `git-commit.md`: khớp quy tắc mới.
- Không làm: không cho push (vẫn chỉ qua `create-pr` và xác nhận); không đổi định dạng commit; không đổi `AGENTS.md` (rule chung, tài liệu dự án thắng khi mâu thuẫn).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `workflow/SKILL.md` có mục "Commit theo bước" và quyền commit được hỏi một lần ở đầu workflow
- [ ] Không còn chỗ nào nói quyền commit chỉ hỏi ở `coding-start` (grep `chỉ commit|commit cục bộ`)
- [ ] Commit có scope `TB-009`, body tiếng Anh, không có `Co-Authored-By`

## Kế hoạch
1. Sửa `workflow/SKILL.md`, `coding-start/SKILL.md`.
2. Sửa `state-protocol.md`, `git-workflow.md`, `git-commit.md`.
3. Cập nhật mục lục issue.

## Ghi chú / Nhật ký
- 2026-10-22: làn nhỏ (tài liệu). Nhánh tách từ `origin/main` bằng git worktree để không lẫn với thay đổi dở của TB-007. Người dùng chọn phương án "commit tự động sau mỗi bước, hỏi quyền một lần ở đầu workflow".

- 2026-10-22: mở PR https://github.com/sowndv02/teamboard/pull/5 (bằng `gh pr create`). Chờ người dùng review và merge.

## Bài học (lessons learned)

- không có
