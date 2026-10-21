---
title: <tên tính năng>
status: draft            # draft | confirmed | superseded
owner: <người chịu trách nhiệm>
updated: YYYY-MM-DD
services: [<service liên quan>]
issues: []               # TB-NNN liên quan
---

# <Tên tính năng>

## 1. Yêu cầu gốc

> Dán **nguyên văn** yêu cầu của khách/người dùng. Không sửa, không diễn giải.

Nguồn: <ai, khi nào, kênh nào>

## 2. Bối cảnh và mục tiêu

- Vấn đề cần giải:
- Giá trị/kết quả mong muốn:
- Thước đo thành công:

## 3. Người dùng và vai trò

| Vai trò | Mô tả | Mục đích sử dụng |
|---|---|---|

## 4. Phạm vi

**Trong phạm vi (lần này):**
-

**Ngoài phạm vi:**
-

## 5. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|

## 6. Yêu cầu chức năng

| ID | Mô tả | Ưu tiên (M/S/C/W) | Tiêu chí chấp nhận | Nguồn |
|---|---|---|---|---|
| FR-001 | | Must | | Lời khách / Giả định |

## 7. Quy tắc nghiệp vụ

| ID | Quy tắc | Ví dụ | Nguồn |
|---|---|---|---|
| BR-001 | | | |

## 8. Luồng nghiệp vụ

### 8.1 Luồng chính
1.

### 8.2 Luồng thay thế
-

### 8.3 Luồng lỗi và trường hợp biên
-

## 9. Dữ liệu

| Thực thể | Thuộc tính chính | Ràng buộc |
|---|---|---|

## 10. Phân quyền và bảo mật

| Hành động | Ai được phép | Ghi chú (dữ liệu nhạy cảm, audit) |
|---|---|---|

## 11. Tích hợp và sự kiện

| Với service/hệ thống | Cách giao tiếp (REST/sự kiện) | Dữ liệu/sự kiện |
|---|---|---|

## 12. Yêu cầu phi chức năng

| ID | Nội dung (kèm số đo) |
|---|---|
| NFR-001 | |

## 13. Giao diện (chưa áp dụng)

Giai đoạn hiện tại không phát triển frontend (ADR 0008). Yêu cầu về giao diện nếu có thì ghi vào mục "Ngoài phạm vi" hoặc "Câu hỏi mở" để xử lý khi có frontend.

## 14. Giả định

| # | Giả định | Người xác nhận | Trạng thái |
|---|---|---|---|
| A-01 | | | chưa xác nhận |

## 15. Câu hỏi mở

Mỗi câu hỏi mở phải có **lựa chọn, khuyến nghị kèm lý do và hậu quả nếu chưa chốt**; câu hỏi trần (chỉ có câu hỏi) là chưa đủ. Bảng liệt kê, chi tiết ngay dưới.

| # | Câu hỏi | Ảnh hưởng tới | Người trả lời | Hạn | Trạng thái (`open`/`answered`/`deferred`) |
|---|---|---|---|---|---|
| Q-01 | | | | | open |

### Q-01 <tên ngắn>
- **Lựa chọn:** A) … B) … (2–4 lựa chọn, mỗi cái kèm đánh đổi chính)
- **Khuyến nghị:** A, vì … (lý do nối về mục tiêu hoặc ràng buộc cụ thể; chỉ để tham khảo, không áp dụng khi người dùng chưa trả lời)
- **Hậu quả nếu chưa chốt:** … (cái gì bị chặn hoặc phải đoán)
- **Mặc định nếu quá hạn:** … (không dùng mặc định cho điều `blocker`)

## 16. Rủi ro và quyết định cần ADR

| Mục | Mô tả | Hướng xử lý |
|---|---|---|

## 17. Truy vết

| Yêu cầu | Issue | Kịch bản (`.feature`) | ADR |
|---|---|---|---|
| FR-001 | | | |

## 18. Lịch sử thay đổi

| Ngày | Thay đổi | Nguồn |
|---|---|---|
| YYYY-MM-DD | Tạo bản nháp | |
