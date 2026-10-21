---
key: TB-006
title: Cải tiến skill, rule và template từ workflow identity-auth (D0, uỷ quyền, cổng xác nhận, thuật ngữ, câu hỏi mở)
status: review
phase: 0
branch: feature/TB-006-agent-skills-rules
estimate: M
created: 2026-10-22
started: 2026-10-22
closed: null
---

# TB-006 — Cải tiến skill, rule và template từ workflow identity-auth

## Bối cảnh
Khi chạy workflow cho `identity-auth` (issue TB-004, đang tạm dừng để làm API gateway trước), người dùng chỉ ra nhiều chỗ hở của quy trình: design bỏ qua lý do cấp service, agent tự chốt khi người dùng uỷ quyền, bước vẫn `done` khi còn điểm chờ xác nhận, thuật ngữ nâng cao không được giải thích, câu hỏi mở không có lựa chọn và khuyến nghị. Năm commit sửa các chỗ hở đó ban đầu nằm trên nhánh của TB-004 và được tách ra issue riêng để merge trước khi làm gateway (các khoá TB-004 và TB-005 đã được dùng).

## Phạm vi
- Làm: (1) `system-design` và `design-review`: mục D0 (ranh giới, cô lập DB, loại DB, kiến trúc nội bộ), người dùng chọn/xác nhận nhóm quyết định khó đảo ngược, uỷ quyền không phải xác nhận, mức Gần/Xa so với thực tế cho `[Chọn để học]`; (2) `workflow`: cổng xác nhận áp cho mọi bước; (3) rule `explain-terms.md` giải thích thuật ngữ nâng cao; (4) template spec/design và các skill liên quan: câu hỏi mở có lựa chọn, khuyến nghị kèm lý do, hậu quả; (5) bài học LL-004, LL-005.
- Không làm: không đổi code ứng dụng; không đụng spec, design hay workflow state của identity-auth (nằm ngoài repo tạm thời, thuộc TB-004).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Mọi commit có scope `TB-006`, body bằng tiếng Anh, không có `Co-Authored-By`
- [x] Bài học LL-004, LL-005 không có liên kết chết
- [x] Mục lục `document/lessons/README.md` và `document/issues/README.md` đã cập nhật
- [ ] Người dùng duyệt PR và merge vào `main`
- [ ] Chạy `design-review` trên design mới (ví dụ gateway) và xác nhận thiếu D0 bị chặn (bước kiểm chứng của LL-004, LL-005)

## Kế hoạch
Đã làm: tách năm commit khỏi nhánh TB-004, đổi scope sang TB-006, sửa liên kết bài học. Còn lại: PR, merge, rồi dùng quy tắc mới cho design gateway.

## Ghi chú / Nhật ký
- 2026-10-22: làm theo làn vừa (tài liệu, không có code). Năm commit được chuyển bằng cherry-pick từ nhánh `feature/TB-004-identity-auth`, chưa push; message được đổi scope từ `TB-004` sang `TB-006`.

## Bài học (lessons learned)

- [LL-004](../lessons/LL-004-inherited-constraint-and-delegation-treated-as-decision.md): ràng buộc kế thừa và uỷ quyền không thay cho lý do và xác nhận ở quyết định kiến trúc/DB.
- [LL-005](../lessons/LL-005-open-questions-written-without-options-or-recommendation.md): câu hỏi mở phải có lựa chọn, khuyến nghị kèm lý do và hậu quả.
