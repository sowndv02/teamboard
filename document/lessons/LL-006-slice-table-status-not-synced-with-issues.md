---
id: LL-006
title: Bảng lát phải đổi trạng thái cùng lúc với issue của lát, không chỉ ghi trong tài liệu tham khảo
status: recurred        # open | partial | prevented | recurred
tags: [process, workflow, docs]
issue: TB-022
date: 2026-10-25
severity: low           # low | medium | high (tác động thực tế hoặc tiềm tàng)
recurrence: 1           # số lần lặp lại sau khi đã ghi
---

# LL-006 — Bảng lát phải đổi trạng thái cùng lúc với issue của lát, không chỉ ghi trong tài liệu tham khảo

## Bài học (một câu, mệnh lệnh)
Khi một tài liệu là "điểm resume" của workflow, hãy gắn việc cập nhật nó vào checklist của đúng skill đổi trạng thái (không chỉ ghi một câu trong tài liệu tham khảo).

## Chuyện gì đã xảy ra
Người dùng hỏi vì sao hoàn thành một lát mà bảng lát không được cập nhật. Bảng `api-gateway-slices.md` ghi lát 1 (TB-010, PR đã merge) và lát 2 (TB-022, đang code) đều là `todo`; bảng `identity-auth-slices.md` ghi TB-021 `in-progress` trong khi issue đã `review`.

## Tác động
`workflow` coi "lát đầu tiên chưa `done` là lát hiện tại", nên điểm resume và báo cáo tiến độ sai. Không mất dữ liệu.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao bảng lệch? Vì không ai cập nhật nó khi issue đổi trạng thái.
2. Vì sao không ai cập nhật? Vì `create-pr`, `coding-start`, `plan-init` và `git-workflow.md` chỉ nhắc đổi trạng thái issue và `document/issues/README.md`.
3. Vì sao quy tắc có mà vẫn bị bỏ sót? Vì `slicing.md` mục 3 bước 6 chỉ nói "sau mỗi lát xong: cập nhật bảng", nằm trong tài liệu tham khảo, không nằm trong checklist "Hoàn thành khi" của skill nào.
**Nguyên nhân gốc:** quy tắc cập nhật bảng lát không được nối vào các bước đổi trạng thái thực tế.

## Cách sửa tức thời
Đồng bộ hai bảng lát với trạng thái issue hiện tại (TB-010 `done`, TB-022 `in-progress`, TB-021 `review`).

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí (file/lệnh) | Trạng thái |
|---|---|---|---|---|
| 1 | rule | Hàng lát đổi trạng thái cùng commit với issue | `.agents/rules/git-workflow.md`, `.agents/skills/workflow/references/slicing.md` | done |
| 2 | checklist | Mục "hàng lát đổi `planning`/`in-progress`/`review`" trong "Hoàn thành khi" | `.agents/skills/plan-init/SKILL.md`, `coding-start/SKILL.md`, `create-pr/SKILL.md` | done |
| 3 | kiểm chứng | Lát kế tiếp (lát 3 gateway): bảng đổi đúng ở từng bước | issue lát 3 | done: `planning` và `in-progress` đúng, nhưng bước `done` sau merge của lát trước (TB-022) bị bỏ sót; coding-review TB-023 bắt được (`gateway:slice-table:TB-022-status-not-synced`) |
| 4 | checklist | Đầu `plan-init` (hoặc `coding-start`) của lát kế tiếp kiểm lát trước đã merge chưa và đổi `done` (issue, `document/issues/README.md`, bảng lát) | `.agents/skills/plan-init/SKILL.md`, `.agents/skills/coding-start/SKILL.md` | todo: cần người dùng đồng ý sửa `.agents` |

## Cách kiểm chứng biện pháp hiệu quả
Ở lát kế tiếp, so cột Trạng thái của bảng lát với trạng thái issue sau mỗi bước; hai bên phải khớp ở mọi commit đổi trạng thái.

## Phạm vi áp dụng
Mọi tính năng có bảng `<feature>-slices.md`. Liên quan LL-004, LL-005 (quy tắc có nhưng không nối vào bước thực thi).

## Liên kết
`.agents/skills/workflow/references/slicing.md`, `document/plans/api-gateway-slices.md`, `document/plans/identity-auth-slices.md`, TB-022.
