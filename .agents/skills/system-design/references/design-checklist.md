# Checklist thiết kế

Dùng ở bước 2, 6, 8 của skill.

## 1. Ước lượng nhanh (back-of-envelope)

Ghi **giả định** và **công thức**; làm tròn mạnh; mục tiêu là bậc độ lớn.

| Đại lượng | Cách tính | Ví dụ minh hoạ (giả định) |
|---|---|---|
| Người dùng hoạt động/ngày (DAU) | từ spec hoặc giả định | 1.000 |
| Yêu cầu/người/ngày | giả định | 50 |
| Yêu cầu/ngày | DAU × yêu cầu/người | 50.000 |
| Yêu cầu/giây trung bình (RPS) | yêu cầu/ngày ÷ 86.400 | ≈ 0,6 |
| RPS đỉnh | trung bình × hệ số đỉnh (thường 3–10) | ≈ 3–6 |
| Tỷ lệ đọc:ghi | giả định | 10:1 |
| Kích thước bản ghi | giả định | 1 KB |
| Dữ liệu mới/ngày | số ghi/ngày × kích thước | ≈ 5 MB |
| Dữ liệu/năm | × 365 (+ chỉ mục ×1,5–2) | ≈ 2–4 GB |
| Băng thông | RPS đỉnh × kích thước phản hồi | vài trăm KB/s |
| Số kết nối đồng thời | RPS × thời gian xử lý (Little's law) | vài chục |

Kết luận kiểu: "Tải này nhỏ; một instance và một DB đủ, không cần cache hay phân vùng". Nếu tải thực sự lớn, nói rõ điểm nghẽn đầu tiên. **Không** thiết kế cho con số không có trong spec.

## 2. Chế độ lỗi (failure modes)

Với mỗi phụ thuộc/bước, hỏi: *nếu nó chậm, chết, trả sai, hoặc trả trùng thì sao?*

| Tình huống | Câu hỏi | Công cụ xử lý |
|---|---|---|
| Dịch vụ phụ thuộc chậm/chết | Có timeout? Người dùng thấy gì? | timeout, retry+jitter, circuit breaker, fallback, hạ cấp chức năng |
| Message gửi trùng | Tác động có lặp lại không? | idempotency key, dedupe `eventId` |
| Message mất | Có ghi nhận gửi trước khi coi là xong? | outbox, xác nhận sau khi xử lý, DLQ |
| Sai thứ tự | Có phụ thuộc thứ tự không? | version/sequence, FIFO theo khoá, thiết kế bất biến thứ tự |
| Hỏng giữa chừng quy trình | Trạng thái nửa vời? | transaction cục bộ, saga + bồi hoàn |
| Hai bên ghi cùng lúc | Ai thắng? Mất cập nhật? | khoá lạc quan (`version`/ETag), ràng buộc duy nhất |
| Poison message | Có bị kẹt hàng đợi? | `maxReceiveCount` + DLQ + cảnh báo |
| Nóng một khoá/tài nguyên | Có nghẽn một điểm? | phân tán khoá, xếp hàng, giới hạn tốc độ |
| Cơ sở dữ liệu chết | RTO/RPO? | sao lưu, multi-AZ, kiểm thử khôi phục |
| Triển khai lỗi | Quay lại thế nào? | triển khai dần, health check, rollback, migration tương thích ngược |
| Dữ liệu xấu/độc hại | Validate ở biên? | validate, giới hạn kích thước, rate limit |

## 3. Pre-mortem

"Sáu tháng sau thiết kế này thất bại. Vì sao?" Ghi 3–5 nguyên nhân khả dĩ nhất, thường rơi vào:
- Giả định tải/NFR sai.
- Ranh giới service cắt sai, call đồng bộ chằng chịt.
- Nhất quán/idempotency không xử lý, dữ liệu trùng hoặc lệch.
- Vận hành quá phức tạp so với quy mô đội.
- Không quan sát được nên không debug nổi.
- Quyết định khó đảo ngược chọn quá sớm.

Với mỗi nguyên nhân: xác suất sơ bộ, tác động, cách giảm, tín hiệu cảnh báo sớm.

## 4. Câu hỏi thử thách mẫu (cho chế độ học)

- Nếu một message được xử lý hai lần thì điều gì xảy ra?
- Nếu DB chết 5 phút, người dùng thấy gì và dữ liệu nào có thể mất?
- Hai người sửa cùng một tài nguyên cùng lúc thì ai thắng?
- Tải tăng 10x: thành phần nào hỏng đầu tiên? Làm sao biết trước?
- Nếu service X chậm gấp 10 lần, các service gọi nó bị ảnh hưởng thế nào?
- Làm sao thêm một consumer mới mà không sửa producer?
- Làm sao đổi schema sự kiện mà consumer cũ không hỏng?
- Chi phí hằng tháng khi để chạy 24/7 là bao nhiêu? Phần nào có thể tắt?

## 5. Rà soát cuối trước khi trình bày

- [ ] Mỗi quyết định nối về một driver cụ thể trong spec
- [ ] Phương án đơn giản nhất đã được cân nhắc nghiêm túc
- [ ] Không có điểm lỗi đơn chưa được nêu
- [ ] Có chiến lược cho message trùng/mất/sai thứ tự
- [ ] Ranh giới nhất quán rõ (giao dịch cục bộ vs nhất quán cuối cùng)
- [ ] Bảo mật: authN/authZ, IDOR, dữ liệu nhạy cảm, bí mật
- [ ] Quan sát: trace/metric/log/cảnh báo cho luồng chính và luồng lỗi
- [ ] Chi phí AWS ước tính và cách tắt khi không dùng
- [ ] Lộ trình mở rộng và tín hiệu để nâng cấp
- [ ] Nhãn `[Cần]`/`[Chọn để học]` trung thực; mục `[Chọn để học]` có cách thực tế làm và mức xa thực tế (Gần/Xa); mức *Xa* có xác nhận của người dùng
