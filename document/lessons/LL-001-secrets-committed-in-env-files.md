---
id: LL-001
title: Không commit file .env chứa mật khẩu; ignore và dùng *.example ngay từ đầu
status: partial
tags: [security, git]
issue: TB-002
date: 2026-10-21
severity: high
recurrence: 0
---

# LL-001 — Không commit file `.env` chứa mật khẩu; ignore và dùng `*.example` ngay từ đầu

## Bài học (một câu, mệnh lệnh)
Mọi file cấu hình chứa secret phải được `.gitignore` **trước** commit đầu tiên; chỉ commit file mẫu (`*.example`) chỉ có tên biến.

## Chuyện gì đã xảy ra
Hai file `src/.env.develop` và `src/.env.stg` (chứa `SA_PASSWORD` và connection string) đã được commit vào repo ở những commit đầu (`Add project files.`). `.gitignore` là mẫu Visual Studio chuẩn nên không chứa quy tắc `.env`. Phát hiện khi rà soát repo lúc thiết lập quy tắc (TB-002).

## Tác động
Giá trị bí mật nằm trong **lịch sử git** (gỡ file khỏi nhánh hiện tại không xoá chúng khỏi lịch sử). Nếu repo được đẩy lên máy chủ hoặc chia sẻ, lộ mật khẩu cơ sở dữ liệu.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao secret vào git? File env nằm trong thư mục dự án và không bị ignore.
2. Vì sao không bị phát hiện? Không có quét secret (chưa có `gitleaks`/hook) và không có bước kiểm tra diff tìm secret.
3. Vì sao quy trình không chặn? Mẫu `.gitignore` mặc định của Visual Studio không có `.env`; không có checklist "secret" khi khởi tạo dự án.
4. Vì sao không có checklist? Dự án khởi tạo từ template chưa có rule bảo mật.
**Nguyên nhân gốc:** thiết lập dự án thiếu bước "bảo vệ secret" (ignore + quét tự động) trước commit đầu tiên.

## Cách sửa tức thời
- Thêm quy tắc `.env*` (giữ `!*.example`) vào `.gitignore`; gỡ hai file khỏi index (`git rm --cached`), giữ bản trên đĩa; tạo `src/.env.develop.example`, `src/.env.stg.example` (commit `e51a058`).

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí | Trạng thái |
|---|---|---|---|---|
| 1 | rule | Secret không vào repo; cung cấp `*.example` | `AGENTS.md` (mục Bảo mật), `.agents/rules/` | done |
| 2 | template | `.gitignore` có `.env*` | `.gitignore` | done |
| 3 | gate tự động | Quét secret trước commit/PR (`gitleaks`) | `quality-gate.md`, `COMMANDS.md` | todo (chưa cài công cụ) |
| 4 | gate | Quét diff thô tìm `password/secret/connectionstring` | `COMMANDS.md` mục 2 | done (thủ công) |
| 5 | hành động | **Xoay (đổi) mật khẩu** nếu là mật khẩu thật | Việc của chủ dự án | todo |
| 6 | (tuỳ chọn) | Viết lại lịch sử git để xoá secret | cần quyết định riêng | chưa quyết định |

## Cách kiểm chứng biện pháp hiệu quả
- Tạo thử file `.env.test` có giá trị, `git add -A`: Git không nhận file (bị ignore).
- Khi `gitleaks` được cài: thử commit một chuỗi giống secret vào nhánh thử, hook/gate phải chặn.

## Phạm vi áp dụng
Mọi dự án và mọi loại secret (mật khẩu, token, khoá, connection string). Áp dụng khi khởi tạo repo và khi thêm file cấu hình mới.

## Liên kết
Commit `e51a058` (gỡ env, thêm example), `PROJECT.md` mục "Vấn đề đã biết", `.agents/rules/quality-gate.md`, `document/security/infra-attack-scenarios.md` (A3).
