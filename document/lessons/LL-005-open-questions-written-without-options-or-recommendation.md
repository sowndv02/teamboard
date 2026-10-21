---
id: LL-005
title: Câu hỏi mở trong tài liệu phải có lựa chọn, khuyến nghị kèm lý do và hậu quả, và phải được cập nhật khi đã chốt
status: partial         # open | partial | prevented | recurred
tags: [process, agent-behavior, docs, workflow]
issue: TB-006
date: 2026-10-22
severity: low           # low | medium | high (tác động thực tế hoặc tiềm tàng)
recurrence: 0           # số lần lặp lại sau khi đã ghi
---

# LL-005 — Câu hỏi mở trong tài liệu phải có lựa chọn, khuyến nghị kèm lý do và hậu quả, và phải được cập nhật khi đã chốt

## Bài học (một câu, mệnh lệnh)
Khi một quy tắc về cách hỏi hay ghi áp cho chat, hãy kiểm xem tài liệu và template có phủ cùng quy tắc không; mỗi câu hỏi mở trong tài liệu phải có 2–4 lựa chọn, khuyến nghị kèm lý do và hậu quả nếu chưa chốt, và chuyển `answered` ngay khi đã được chốt.

## Chuyện gì đã xảy ra
Người dùng đọc bảng "Câu hỏi mở" trong spec và design `identity-auth` và thấy câu hỏi trần ("Thuật toán ký mặc định (RSA hay ECDSA)?"), trong khi họ kỳ vọng có lựa chọn, khuyến nghị và lý do như khi agent hỏi qua công cụ hỏi. Ngoài ra bảng trong spec còn ghi "hoãn" cho Q-02, Q-03, Q-11 dù design đã chốt chúng (D1, D7, D2). Rà soát còn thấy skill đòi câu hỏi mở có "hạn" nhưng template spec không có cột này.

## Tác động
Chất lượng tài liệu và khả năng học: người dùng không thấy phương án và lý do khi đọc tài liệu, và đọc phải câu hỏi đã được trả lời. Không mất dữ liệu hay quyết định sai.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao câu hỏi mở thiếu lựa chọn và khuyến nghị? Vì agent điền đúng cột của template, mà template chỉ có `Câu hỏi | Ảnh hưởng | Người trả lời | Mặc định`.
2. Vì sao template thiếu? Vì quy tắc "lựa chọn + khuyến nghị kèm lý do" (`agent-conduct.md`, `review-loop.md`, `spec-review`) được viết cho việc hỏi trong chat, không cho câu hỏi lưu trong tài liệu.
3. Vì sao review không bắt được? Vì checklist `spec-review`/`design-review` chỉ kiểm owner, hạn, mặc định, không kiểm nội dung câu hỏi; template và skill còn lệch nhau (skill đòi "hạn", template không có).
4. Vì sao câu đã chốt vẫn ghi `open`? Vì không có bước nào yêu cầu cập nhật trạng thái câu hỏi mở khi bước sau (design, ADR) trả lời nó.
**Nguyên nhân gốc:** quy tắc về cách hỏi chỉ phủ kênh chat, template và checklist không đồng bộ với skill, và không có bước giữ câu hỏi mở cập nhật theo các quyết định về sau.

## Cách sửa tức thời
Cập nhật bảng câu hỏi mở của spec và design `identity-auth`: đánh dấu câu đã chốt kèm link, bổ sung lựa chọn, khuyến nghị và hậu quả cho câu còn mở.

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí (file/lệnh) | Trạng thái |
|---|---|---|---|---|
| 1 | mẫu | Mục câu hỏi mở có lựa chọn, khuyến nghị kèm lý do, hậu quả nếu chưa chốt, mặc định nếu quá hạn; thêm cột hạn và trạng thái | `document/spec/_template.md` mục 15, `document/design/_template.md` mục 14 | done |
| 2 | rule (skill) | Quy tắc "mỗi câu hỏi mở phải có đủ" và "chuyển `answered` khi đã chốt" | `.agents/skills/requirements-discovery/SKILL.md` (bước 5, checklist), `.agents/skills/system-design/SKILL.md` (không làm, hoàn thành khi, FE) | done |
| 3 | checklist | Review báo FIX khi câu hỏi mở thiếu lựa chọn/khuyến nghị/hậu quả hoặc đã chốt mà còn `open`; CONFIRM `missing` khi cần thông tin từ người dùng | `.agents/skills/spec-review/SKILL.md`, `.agents/skills/design-review/SKILL.md` | done |
| 4 | kiểm chứng | Chạy spec-review/design-review trên spec và design kế tiếp, xác nhận câu hỏi trần bị báo | tài liệu kế tiếp | todo |

## Cách kiểm chứng biện pháp hiệu quả
Ở spec hoặc design mới: một câu hỏi mở chỉ có câu hỏi phải bị review báo FIX; một câu đã nằm trong Confirm log hay D1…Dn mà vẫn `open` cũng bị báo. Chưa chạy được vì chưa có tài liệu mới; đã áp dụng thủ công trên `identity-auth`.

## Phạm vi áp dụng
Mọi spec và design (mục "Câu hỏi mở"), cũng đáng áp dụng cho plan và ADR có phần câu hỏi mở. Liên quan LL-004 (cùng họ: quy tắc có nhưng phạm vi hẹp hơn nơi nó cần áp dụng).

## Liên kết
spec identity-auth (thuộc issue TB-004 đang tạm dừng, chưa có trong repo) (mục 15), design identity-auth (thuộc issue TB-004 đang tạm dừng, chưa có trong repo) (mục 14), `document/spec/_template.md`, `document/design/_template.md`, `.agents/skills/requirements-discovery/SKILL.md`, `.agents/skills/system-design/SKILL.md`, `.agents/skills/spec-review/SKILL.md`, `.agents/skills/design-review/SKILL.md`, `.agents/rules/agent-conduct.md`, LL-004.
