---
key: TB-008
title: Báo cáo mọi FIX agent đã sửa sau mỗi vòng review (không sửa im lặng)
status: in-progress
phase: 0
branch: feature/TB-008-review-fix-report
estimate: S
created: 2026-10-22
started: 2026-10-22
closed: null
---

# TB-008 — Báo cáo mọi FIX agent đã sửa sau mỗi vòng review

## Bối cảnh
Giao thức review (`review-loop.md`) cho agent tự sửa finding loại FIX, còn mục "Báo cáo cho người dùng" chỉ yêu cầu verdict và số lượng. Người dùng chỉ thấy "đã sửa N lỗi", không biết sửa gì, ở đâu, nên không kiểm tra lại được. Người dùng yêu cầu: sau mỗi bước review hoặc fix phải liệt kê đã sửa những gì.

## Phạm vi
- Làm: (1) `review-loop.md`: thêm quy tắc "không sửa im lặng", bảng **Fix log** trong artifact và bảng "Đã sửa" trong báo cáo cho người dùng; (2) `code-review.md` và skill `coding-review`: báo cáo và issue có mục tương ứng; (3) template issue có bảng Fix log; (4) mục lục issue.
- Không làm: không đổi cách phân loại CONFIRM/FIX; không thêm điểm dừng chờ duyệt FIX trước khi sửa (người dùng chưa chọn); không đổi các skill review khác ngoài việc trỏ về `review-loop.md`.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `review-loop.md` có quy tắc yêu cầu báo cáo từng FIX đã sửa (fingerprint, vị trí, vấn đề, cách sửa, evidence), không chỉ số lượng
- [ ] Báo cáo `code-review.md` có mục "Đã sửa trong vòng này"; issue template có bảng Fix log
- [ ] Không có liên kết chết trong các file đã sửa
- [ ] Commit có scope `TB-008`, body tiếng Anh, không có `Co-Authored-By`

## Kế hoạch
1. Sửa `review-loop.md` (quy tắc 12, mục 5, mục 7).
2. Sửa `code-review.md`, skill `coding-review`, `document/issues/_template.md`.
3. Cập nhật `document/issues/README.md`.

## Ghi chú / Nhật ký
- 2026-10-22: làm theo làn nhỏ (tài liệu). Nhánh tách từ `main` bằng git worktree để không lẫn với thay đổi dở của TB-007. Người dùng chưa chọn định dạng; dùng mặc định: bảng "Đã sửa" trong báo cáo và issue, agent sửa xong mới báo.

## Bài học (lessons learned)

- không có
