# Messaging rules (SNS/SQS, outbox, consumer)

## Event

- Schema đặt trong `src/shared` (contract). Trường bắt buộc: `eventId` (GUID), `type`, `occurredAt` (UTC), `correlationId`, `version`.
- Tên event là quá khứ, dạng `<Aggregate><Action>` (`TaskAssigned`). Đổi schema theo hướng tương thích ngược; breaking change = tăng `version` + ADR.
- Event mang dữ liệu đủ cho consumer, không bắt consumer gọi ngược service phát.

## Producer

- Ghi event vào bảng **outbox trong cùng transaction** với thay đổi dữ liệu; publisher nền đọc outbox và gửi SNS. Cấm publish trực tiếp trong handler sau khi commit ("dual write").
- Publisher phải chịu được retry (có thể gửi trùng).

## Consumer

- **Idempotent**: dedupe theo `eventId` (bảng/khoá lưu event đã xử lý) trước khi tác động phụ.
- Không giả định thứ tự message. Cần thứ tự thì dùng FIFO và ghi lý do trong ADR.
- Mỗi consumer có queue riêng + DLQ; `maxReceiveCount` cấu hình rõ. Lỗi tạm thời → để retry; lỗi vĩnh viễn (poison) → DLQ, không nuốt.
- Xử lý xong mới xoá message; visibility timeout > thời gian xử lý tối đa.
- Truyền tiếp `correlationId` và `traceparent` từ message vào log/trace.

## Test bắt buộc

Outbox (ghi cùng transaction), consumer idempotent (nhận trùng 2 lần → tác động 1 lần), poison message vào DLQ.
