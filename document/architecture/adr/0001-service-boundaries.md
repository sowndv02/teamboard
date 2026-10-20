# ADR 0001 — Chia service & chọn kiến trúc từng service

Trạng thái: Đề xuất · Ngày: 2026-10-20

## Bối cảnh
Dự án học tập; mục tiêu là trải nghiệm nhiều kiểu kiến trúc, không phải tối ưu cho domain.

## Quyết định
Sáu service (gateway, identity, board, task, notification, activity), mỗi service sở hữu data riêng và dùng một kiến trúc nội bộ khác nhau (xem README §1).

## Đánh đổi
- Domain nhỏ bị chia hơi "quá tay" so với thực tế (over-engineering có chủ đích để luyện tập).
- Chi phí vận hành tăng → bù bằng observability và IaC ngay từ đầu.
- Ranh giới Board/Task có thể gây chatty call; chấp nhận để luyện xử lý consistency (eventual consistency, outbox).

## Khi nào xem lại
Nếu một ranh giới gây quá nhiều coupling sync, cân nhắc gộp lại và ghi ADR mới.
