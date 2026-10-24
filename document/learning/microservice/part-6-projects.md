# Part 6 — Projects (Microservice)

> [!NOTE]
> **Mục tiêu:** áp các cơ chế của Part 1 đến 5 lên TeamBoard: Board → Notification đầu-cuối bằng outbox và consumer idempotent, saga "Xoá Board", và đối chiếu với ADR 0001, ADR 0002, `resilience.md`; không sửa `src/`.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** chứng minh bằng số rằng một chuỗi outbox, broker giao trùng và consumer idempotent cho kết quả đúng, và nêu chỗ thiết kế dự án cần chốt thêm.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

TeamBoard là hệ có chủ ý chia "quá tay" để học (ADR 0001): sáu service (`gateway`, `identity-service`, `board-service`, `task-service`, `notification-service`, `activity-service`), mỗi service một kiến trúc nội bộ khác nhau và dữ liệu riêng. Giao tiếp (ADR 0002): REST qua gateway; event qua SNS fan-out tới SQS mỗi consumer kèm DLQ; producer dùng Transactional Outbox, consumer idempotent theo `eventId`; mọi request và event mang `correlationId` và `traceparent`; call đồng bộ giữa service có timeout, retry có jitter và circuit breaker. Hiện chỉ có `board-service` và `ServiceDefaults` trong `src/`; các service còn lại chưa có code.

Lộ trình **không** sửa `src/`, `config/`, ADR hay design. Mọi mã chạy trong thư mục thử ngoài repo; trạng thái của hai service giả là hai DB SQLite riêng (đúng nguyên tắc database-per-service).

## L4 Hands-on — Ba dự án

Môi trường: như Part 0. Giao dịch SQLite thật; sự cố được tiêm có chủ ý.

#### Dự án 6.1 — Board → Notification đầu-cuối
`board-service` tạo Board và dòng outbox trong cùng một giao dịch (20% lần chết trước commit); relay đọc outbox và publish (10% lần chết giữa publish và đánh dấu); broker mô phỏng giao mỗi message một hoặc hai lần và xáo trộn; `notification-service` ghi `inbox` và thông báo trong **một** giao dịch (khoá chính trên `eventId`).
**Đầu ra thật:**
```text
Board được tạo (đã commit): 811; sự cố trước commit: 189
relay gửi 898 message (chết 87 lần giữa publish và đánh dấu); broker giao 1333 lượt
thông báo ghi nhận: 811 (bằng số Board đã commit: True); Board được báo ít nhất một lần: 811; trùng: 0
```
**Giải thích:** bốn loại sự cố (chết trước commit, relay gửi trùng, broker giao trùng, thứ tự xáo trộn) và kết quả vẫn đúng: **mỗi Board đã commit có đúng một thông báo**; 189 lần chết trước commit không để lại Board hay event mồ côi. Đây là mô hình trong một tiến trình: không có SQS, nhiều consumer song song hay visibility timeout thật.

#### Dự án 6.2 — Saga "Xoá Board" (đối chiếu ADR 0001)
Dựa trên [Part 4, Lab 4.2](part-4-advanced.md): huỷ Task (`task-service`), xoá feed (`activity-service`), xoá Board (`board-service`), có bồi hoàn.
**Quyết định cần người dùng chốt trước khi làm thật** (tài liệu hiện chưa nêu): (1) choreography hay orchestration; (2) ai sở hữu trạng thái saga và lưu ở đâu; (3) Board có trạng thái "đang xoá" để người đọc không thấy trạng thái nửa vời không; (4) khi bồi hoàn thất bại thì ai xử lý.
**Tiêu chí đạt (bài tập, ⚠ chưa chạy dưới dạng đa tiến trình):** mô phỏng lại với bước bồi hoàn **tự lỗi** 20% và kiểm rằng có cách chạy lại; ghi đầu ra thật.

#### Dự án 6.3 — Gộp lại để so sánh (bài tập, ⚠ chưa chạy)
Dựng phiên bản modular monolith của luồng Dự án 6.1 (một DB, một giao dịch, hai module) và so: số dòng code, số cơ chế cần có (outbox, inbox, relay, DLQ), số kiểu sự cố phải xử lý. **Tiêu chí đạt:** bảng so sánh có số đếm của chính bạn; kết luận nêu rõ cái gì chỉ có ở phiên bản microservice (deploy và scale độc lập, cô lập lỗi) và giá phải trả. Đây là bài tập đối chiếu cho ADR 0001 ("domain bị chia quá tay có chủ đích").

#### Điểm đối chiếu với tài liệu dự án (báo, không tự sửa)
| Tài liệu | Quy định | Quan sát từ lộ trình |
|---|---|---|
| ADR 0002 | consumer idempotent theo `eventId` | khớp Lab 3.1; cần quyết định thời hạn lưu `eventId` lớn hơn cửa sổ giao lại và DLQ redrive |
| ADR 0002 | outbox ở producer | Lab 4.1 cho thấy relay vẫn gửi trùng; ADR đã đi kèm idempotent nên khớp |
| ADR 0002 | SQS FIFO "nơi cần" thứ tự | FIFO khử trùng chỉ ở phía gửi (5 phút) nên vẫn cần idempotent; chưa nêu chọn nhóm `MessageGroupId` |
| ADR 0001 | Board/Task chatty gọi, chấp nhận để luyện eventual consistency | Lab 1.1: chuỗi đồng bộ làm tích độ sẵn sàng; nên đo độ dài chuỗi Board → Task |
| `resilience.md` | retry bọc ngoài circuit breaker, timeout từng lần thử trong cùng | khớp Azure Retry và Circuit Breaker; chưa chạy breaker thật (⚠) |
| Saga | patterns-catalog mục 3.4 liệt kê saga, chưa có thiết kế cụ thể | cần chốt bốn điểm ở Dự án 6.2 |

## L5 Troubleshooting — Khi dự án không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Số thông báo khác số Board đã commit | thiếu inbox, hay đánh dấu và tác động tách giao dịch | đếm theo `eventId` | khoá chính inbox, một giao dịch |
| Có event cho Board không tồn tại | publish trước commit | so thứ tự | outbox |
| Relay gửi mãi cùng một dòng | không đánh dấu `sent`, hoặc lỗi lặp | tuổi dòng, số lần gửi | đánh dấu sau publish, giới hạn lần thử, cảnh báo |
| Saga để lại dữ liệu nửa vời | thiếu bồi hoàn hoặc bồi hoàn không idempotent | trạng thái từng bước | bước bồi hoàn idempotent, ghi tiến trình |
| Reader thấy trạng thái trung gian | thiếu isolation | đọc trong lúc saga chạy | trạng thái "đang xoá", khoá ngữ nghĩa |
| Số đo mỗi lần chạy một khác | hạt giống ngẫu nhiên, máy ồn | chạy lại ba lần | cố định seed, báo khoảng giá trị |
| Kết luận "chạy được" nhưng chưa thử ở nhiều tiến trình | lab chỉ trong một tiến trình | xem phạm vi lab | thử với broker thật hoặc LocalStack |

## Tự kiểm tra

1. Trong Dự án 6.1, bốn loại sự cố là gì và cơ chế nào chặn từng loại?
2. Vì sao 811 thông báo bằng đúng số Board đã commit nói lên điều gì và chưa nói lên điều gì?
3. Nêu hai quyết định cần chốt trước khi làm saga "Xoá Board" thật.

<details>
<summary>Đáp án ngắn</summary>

1. Chết trước commit (giao dịch outbox); relay chết giữa chừng (gửi lại, consumer idempotent); broker giao trùng (inbox có khoá chính); thứ tự xáo trộn (consumer không phụ thuộc thứ tự).
2. Nói rằng mô hình giữ đúng bất biến "mỗi Board đã commit đúng một thông báo" dưới sự cố được tiêm; chưa nói gì về SQS thật, nhiều consumer song song hay độ trễ.
3. Ví dụ: choreography hay orchestration; trạng thái saga lưu ở đâu; có trạng thái "đang xoá" không; ai xử lý khi bồi hoàn thất bại.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 (giao dịch SQLite thật cho hai DB, broker mô phỏng; đầu ra thật, môi trường ở Part 0).
**Đã tự mở:** ADR 0001, ADR 0002, `resilience.md`, `patterns-catalog.md` (mục 2.7 và 3.4) của dự án.
**Chưa kiểm:** Dự án 6.2 (chạy dạng đa tiến trình) và 6.3 là bài tập chưa chạy; không đọc mã `board-service`; mọi số liệu là của mô hình, không phải đo hệ thống của dự án.
