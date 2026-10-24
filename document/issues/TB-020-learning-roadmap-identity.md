---
key: TB-020
title: Lộ trình học Identity (xác thực và ủy quyền) theo Part 0–7 × L1–L8
status: in-progress
phase: 0
branch: feature/TB-020-learning-roadmap-identity
estimate: L
created: 2026-10-25
started: 2026-10-25
closed: null
---

# TB-020 — Lộ trình học Identity (xác thực và ủy quyền) theo Part 0–7 × L1–L8

## Bối cảnh
Dự án sẽ tự xây `identity-service` để học (ADR 0006, `document/architecture/identity-learning.md`, giai đoạn I1–I9). Người dùng muốn một lộ trình `learning-hub` đủ Part 0–7 cho chủ đề này: khái niệm xác thực và ủy quyền, các cơ chế (mật khẩu, JWT, JWKS, OAuth 2.0 và OIDC, refresh token, ủy quyền), các tấn công và lab chạy thật.

Nhánh cắt từ `origin/main` (PR #10 với 7 lộ trình trước đã merge).

## Phạm vi
- Làm: lộ trình `document/learning/identity/` (Part 0–7) cho khái niệm; lab .NET ngoài repo (dịch vụ giả trên `127.0.0.1`, khoá sinh ngẫu nhiên lúc chạy); tấn công thử chỉ nhằm vào dịch vụ do chính lab dựng; cập nhật `topic-map.md` và mục lục.
- Không làm: sửa `src/`, `config/`, ADR 0006, `identity-learning.md`; tự viết thuật toán mật mã (dùng BCL); thử trên hệ thống thật; lưu khoá hay mật khẩu thật; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ, mục phạm vi và cảnh báo "tự xây để học, không để chạy sản phẩm"; ánh xạ I1–I9.
- [x] Mỗi cơ chế một thẻ có Use case; Bản đồ giải pháp ≥3 lựa chọn kể cả không tự xây.
- [x] Mỗi tấn công có bằng chứng **thành công** trên bản cố ý yếu và **bị chặn** trên bản đúng.
- [x] Mọi lab có đầu ra thật hoặc gắn ⚠; không có khoá, mật khẩu hay token thật trong tài liệu và repo.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; script kiểm chứng đạt cho 8 lộ trình.

## Kế hoạch
1. Issue và nhánh (file này).
2. Nghiên cứu chuẩn và sự cố (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab thật: băm mật khẩu, JWT bằng BCL, JWKS và xoay khoá, code + PKCE, refresh token, tấn công.
4. Viết Part 0–7.
5. Mục lục, topic-map, liên kết, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue theo kế hoạch người dùng duyệt (plan mode).
- 2026-10-25: viết Part 0–7 và chạy lab thật (.NET 10, ngoài repo); cập nhật topic-map và mục lục; script kiểm chứng đạt cho 8 lộ trình; quét thô không có khoá hay token thật. Chưa commit. Còn thiếu bằng chứng I7, I8, I9 và nguồn sự cố chỉ qua agent (ghi ở Part 6, 7).

## Bài học (lessons learned)

- Chưa có.
