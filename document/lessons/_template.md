---
id: LL-000
title: <bài học một dòng>
status: open            # open | partial | prevented | recurred
tags: [<tag>, <tag>]
issue: TB-NNN
date: YYYY-MM-DD
severity: low           # low | medium | high (tác động thực tế hoặc tiềm tàng)
recurrence: 0           # số lần lặp lại sau khi đã ghi
---

# LL-000 — <bài học một dòng>

## Bài học (một câu, mệnh lệnh)
Làm/không làm điều gì, trong tình huống nào.

## Chuyện gì đã xảy ra
Triệu chứng, bối cảnh, phát hiện lúc nào và bằng cách nào. Không đổ lỗi cá nhân; không ghi secret.

## Tác động
Thực tế hoặc tiềm tàng (dữ liệu, chi phí, thời gian, bảo mật, chất lượng).

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao lỗi xảy ra?
2. Vì sao điều đó không bị phát hiện sớm?
3. Vì sao quy trình/công cụ không chặn được?
4. ...
**Nguyên nhân gốc:** (một câu, về hệ thống/quy trình)

## Cách sửa tức thời
Đã làm gì để khắc phục (link commit/issue).

## Biện pháp phòng ngừa

Ưu tiên theo độ chắc chắn: **tự động hoá > rule/gate > mẫu/checklist > ADR/spec > chỉ nhắc nhở** (loại cuối phải có lý do).

| # | Loại | Biện pháp | Vị trí (file/lệnh) | Trạng thái |
|---|---|---|---|---|
| 1 | test / gate / rule / template / ADR / spec / checklist | | | done / todo |

## Cách kiểm chứng biện pháp hiệu quả
Làm sao biết lỗi này sẽ bị chặn lần sau (test tái hiện, lệnh gate, kịch bản thử).

## Phạm vi áp dụng
Service/vùng/loại việc nào cần nhớ bài học này (khớp tag). Có áp dụng cho dự án khác không?

## Liên kết
Issue, commit, finding review, rule/ADR được cập nhật.
