---
id: LL-003
title: Chỉ dẫn mặc định của công cụ không được thắng sở thích chưa ghi của chủ dự án; hỏi hoặc ghi thành rule
status: prevented
tags: [git, agent-behavior, process]
issue: TB-002
date: 2026-10-21
severity: low
recurrence: 0
---

# LL-003 — Chỉ dẫn mặc định của công cụ không được thắng sở thích chưa ghi của chủ dự án; hỏi hoặc ghi thành rule

## Bài học (một câu, mệnh lệnh)
Khi quy tắc dự án im lặng về một chi tiết có tác động tới lịch sử/đầu ra bền vững (như nội dung commit message), hỏi chủ dự án hoặc ghi thành rule trước, đừng mặc định theo chỉ dẫn của công cụ.

## Chuyện gì đã xảy ra
Agent thêm dòng `Co-Authored-By: Claude ...` vào 8 commit của TB-002 theo chỉ dẫn mặc định của công cụ, trong khi `git-commit.md` chỉ ghi "được phép" và không nói chủ dự án muốn hay không. Chủ dự án nhớ đã có rule bỏ dòng này nhưng rule không tồn tại trong repo; phát hiện khi chủ dự án hỏi.

## Tác động
Thấp: sai sở thích về nội dung commit; phải viết lại lịch sử cục bộ (chưa push) để gỡ.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao có trailer? Chỉ dẫn mặc định của công cụ yêu cầu thêm.
2. Vì sao không bị chặn? Rule dự án không cấm và còn ghi "được phép".
3. Vì sao rule không cấm? Sở thích của chủ dự án chưa từng được ghi vào repo (nằm trong trí nhớ của người).
4. Vì sao agent không hỏi? Không có quy ước "hỏi khi rule im lặng về chi tiết ảnh hưởng lịch sử".
**Nguyên nhân gốc:** sở thích về commit message chưa được mã hoá thành rule, và không có bước hỏi khi rule im lặng.

## Cách sửa tức thời
Gỡ trailer khỏi 8 commit chưa push (viết lại lịch sử cục bộ, giữ nguyên nội dung file; nhánh sao lưu `backup/TB-002-pre-rewrite`).

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí | Trạng thái |
|---|---|---|---|---|
| 1 | rule | Cấm trailer `Co-Authored-By`, thắng mặc định của công cụ | `.agents/rules/git-commit.md` | done |
| 2 | cấu hình | `coAuthorTrailer: false` | `config/project.yaml` | done |
| 3 | checklist | Mục "không có trailer" trước khi commit | `git-commit.md` | done |
| 4 | quy ước | Rule dự án thắng mặc định công cụ; rule im lặng về chi tiết ảnh hưởng lịch sử thì hỏi | `AGENTS.md` (mục Bắt đầu phiên, mục 3) | done (đã có "rule trong `.agents/rules/` thắng convention mặc định"); phần "hỏi khi im lặng" chỉ là nhắc nhở |

## Cách kiểm chứng biện pháp hiệu quả
`git log main..HEAD --format=%B | grep -c "Co-Authored-By"` phải bằng 0. Khi có hook `commit-msg` (`git-commit.md`), thêm kiểm tra từ chối dòng này.

## Phạm vi áp dụng
Mọi commit/PR; mọi chi tiết đầu ra bền vững mà rule dự án chưa nói rõ.

## Liên kết
`.agents/rules/git-commit.md`, `config/project.yaml`, `AGENTS.md`.
