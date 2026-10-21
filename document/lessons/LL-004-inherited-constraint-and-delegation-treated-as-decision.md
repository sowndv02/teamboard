---
id: LL-004
title: Ràng buộc kế thừa và uỷ quyền của người dùng không thay cho lý do và xác nhận ở quyết định kiến trúc/DB
status: partial         # open | partial | prevented | recurred
tags: [process, agent-behavior, docs, workflow, database]
issue: TB-006
date: 2026-10-22
severity: medium        # low | medium | high (tác động thực tế hoặc tiềm tàng)
recurrence: 0           # số lần lặp lại sau khi đã ghi
---

# LL-004 — Ràng buộc kế thừa và uỷ quyền của người dùng không thay cho lý do và xác nhận ở quyết định kiến trúc/DB

## Bài học (một câu, mệnh lệnh)
Với ranh giới service, loại database, kiến trúc nội bộ và hợp đồng công khai: luôn viết lý do (kể cả khi ADR đã bắt buộc) và để người dùng chọn hoặc xác nhận kết quả cụ thể; câu trả lời "tuỳ/chọn cái thực tế" chỉ là uỷ quyền đề xuất, chưa phải xác nhận.

## Chuyện gì đã xảy ra
Design `identity-auth` vượt `design-review` (PASS vòng 2) mà không có phần lý do cho việc tách service, database riêng, PostgreSQL và Vertical Slice: các mục này được coi là ràng buộc có sẵn (ADR 0001, `PROJECT.md`) nên không được phân tích. Người dùng phát hiện ("sao trong phase system design tôi không thấy liên quan đến logic kiến trúc"; "nếu tách DB thì sẽ có thêm lý do chọn DB + chọn system design"). Cùng phiên, một quyết định (dùng `JsonWebTokenHandler`, thu hẹp phạm vi ADR 0006) được agent áp dụng ngay theo câu trả lời "cái nào sát real-world thì ưu tiên" mà không hỏi lại kết quả cụ thể.

## Tác động
Tiềm tàng: quyết định khó đảo ngược (DB, ranh giới, kiến trúc) được chốt mà người dùng không thấy lý do, mất mục tiêu học system design; một ADR có thể bị sửa phạm vi không có xác nhận rõ. Thực tế: phải mở lại design-review, thêm D0, thêm một vòng review.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao design thiếu lý do cấp service? Vì skill `system-design` nguyên tắc 8 ("tôn trọng ràng buộc đã chốt") được hiểu thành "ràng buộc thì khỏi phân tích".
2. Vì sao review không bắt được? Vì checklist `design-review` chỉ kiểm "có phá ADR không", không kiểm "ràng buộc có lý do không".
3. Vì sao agent tự chốt khi người dùng uỷ quyền? Vì quy trình không có quy tắc cho uỷ quyền; `review-loop.md` chỉ nói "reviewer không tự chốt" mà không nói uỷ quyền thì tính thế nào.
4. Vì sao người dùng không được hỏi mặc định? Vì chế độ "người dùng chọn trước" của `system-design` chỉ bật khi người dùng yêu cầu.
**Nguyên nhân gốc:** quy trình không bắt buộc có lý do cho lựa chọn bị ràng buộc, không đặt người dùng làm người chọn mặc định cho nhóm quyết định khó đảo ngược, và không phân biệt uỷ quyền với xác nhận.

## Cách sửa tức thời
Thêm D0 vào design identity-auth (thuộc issue TB-004 đang tạm dừng, chưa có trong repo) (Confirm log #8–#11); mở lại D2 để người dùng xác nhận kết quả cụ thể.

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí (file/lệnh) | Trạng thái |
|---|---|---|---|---|
| 1 | rule (skill) | Nguyên tắc 8 mở rộng (ràng buộc không thay lý do), nguyên tắc 10 (người dùng chọn/xác nhận nhóm khó đảo ngược), nguyên tắc 11 (uỷ quyền ≠ xác nhận); bước 3b D0 bắt buộc; chế độ học mặc định cho nhóm này; mục "Không làm" và "Hoàn thành khi" | `.agents/skills/system-design/SKILL.md` | done |
| 2 | checklist | Mục B2 kiểm D0, lý do cho ràng buộc, người dùng chọn/xác nhận; PASS yêu cầu D0 đã xác nhận | `.agents/skills/design-review/SKILL.md` | done |
| 3 | rule (giao thức) | Quy tắc 11 về uỷ quyền và cách ghi Confirm log "Agent chọn theo uỷ quyền … chờ xác nhận" | `.agents/skills/workflow/references/review-loop.md` | done |
| 4 | mẫu | Mục D0 trong template design | `document/design/_template.md` | done |
| 5 | kiểm chứng | Chạy `design-review` trên design service mới tiếp theo và xác nhận thiếu D0 bị chặn ở mức blocker | design của service kế tiếp | todo |

## Cách kiểm chứng biện pháp hiệu quả
Khi design service mới kế tiếp (ví dụ `task-service`) chạy `design-review`: thiếu D0 phải bị báo CONFIRM `missing` blocker; quyết định "Agent chọn theo uỷ quyền" chưa xác nhận phải chặn PASS. Chưa chạy được vì chưa có design mới; áp dụng thủ công trên `identity-auth` (D0 và D2 đang được xác nhận).

## Phạm vi áp dụng
Mọi `system-design` và `design-review`, đặc biệt service mới hoặc khi chạm lưu trữ/ranh giới. Cũng áp dụng cho `spec-review`/`plan-review` ở khái niệm uỷ quyền (`review-loop.md` quy tắc 11).

## Liên kết
design identity-auth (thuộc issue TB-004 đang tạm dừng, chưa có trong repo) (D0, Confirm log #8–#11, #2); `.agents/skills/system-design/SKILL.md`; `.agents/skills/design-review/SKILL.md`; `.agents/skills/workflow/references/review-loop.md`; `document/design/_template.md`; ADR 0001, ADR 0006.
