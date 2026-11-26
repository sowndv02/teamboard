# Part 3 — Multi-component Applications (Testing)

> [!NOTE]
> **Mục tiêu:** test đúng ranh giới giữa thành phần: HTTP, DB, message bus, service khác; chọn mức test phù hợp thay vì test mọi thứ end-to-end.
> **Cần biết trước:** [Part 2](part-2-core-skills.md).
> **Thời lượng:** TODO · **Đầu ra kiểm được:** một integration test chạy với DB thật trong container và một contract test giữa hai service.
> **Part trước / sau:** [←](part-2-core-skills.md) · [→](part-4-advanced.md)

## L1 What — Các mức test nhiều thành phần
TODO. Thẻ: integration test, contract test (consumer-driven), component test, end-to-end, test với container, fake clock.

## L2 Why — Vì sao unit test chưa đủ
TODO. Tình huống đau: mock nói "ổn" nhưng ghép lại hỏng.

## L3 How — Cơ chế
TODO. `WebApplicationFactory`, Testcontainers, consumer-driven contract, kiểm tính idempotent của consumer.

## L4 Hands-on — Lab
TODO. Lab 3.1 integration test board-service với SQL Server container; Lab 3.2 contract giữa gateway và board-service. ⚠ chưa chạy. Ràng buộc: nhóm test Docker chạy mỗi lần một assembly (xem `COMMANDS.md`).

## L5 Troubleshooting — Test tích hợp hỏng
TODO. Cổng bận, container chưa sẵn sàng, dữ liệu rò giữa test, thời gian.

## L7 Architecture — Chọn mức test
TODO. So sánh unit, integration, contract, e2e theo tiêu chí; khi nào không cần e2e.

## Tự kiểm tra
TODO.

## Nguồn và độ tin cậy
- **Đã tự mở và đối chiếu:** chưa có.
- **Chỉ qua nguồn thứ cấp hoặc agent (⚠):** chưa có.
- **Chưa kiểm hoặc chưa tìm được:** toàn bộ nội dung Part này.
