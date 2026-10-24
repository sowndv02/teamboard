# Part 6 — Projects (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** áp Valkey lên ba bài toán của TeamBoard (cache đọc Board, rate limit gateway, khử trùng sự kiện cho consumer) và đo kết quả, không sửa code `src/`.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** một dự án chạy được với đầu ra của chính bạn, kèm lý do vì sao dùng (hoặc không dùng) Valkey cho từng bài toán.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

TeamBoard hiện **không có** Valkey trong `src/` hay compose; lưu trữ là SQL Server, PostgreSQL, DynamoDB. Chính sách: Redis server `banned`, Valkey `watch`, và "không cache khi chưa cần" (`dependency-policy.md`). Phần này **không sửa `src/`, `config/`** (issue TB-015 chỉ tài liệu): mọi mã chạy trong thư mục thử ngoài repo. Muốn đưa Valkey vào repo thật cần issue riêng, ADR cho dependency (client .NET chưa có trong `config/dependencies.yaml`) và chọn dịch vụ qua `aws-service-selection`.

Ba bài toán gắn với tài liệu có sẵn:
- **Cache đọc Board** của `board-service` (cache-aside, [Caching Part 2](../caching/part-2-core-skills.md)).
- **Rate limit ở gateway**: hiện đếm trong bộ nhớ vì chỉ 1 instance (`document/design/api-gateway.md`).
- **Consumer idempotent** của `notification-service`: khử trùng theo `eventId` (`PROJECT.md`).

## L4 Hands-on — Ba dự án

Môi trường: như Part 0; Valkey 9.1.2 trong `lh-valkey`, dự án .NET ngoài repo.

#### Dự án 6.1 — Khử trùng sự kiện bằng `SET NX` (consumer idempotent)
Cho 3 sự kiện, mỗi sự kiện được giao 4 lần song song; mỗi lần gọi `SET evt:{eventId} done NX EX 600`, chỉ lần thành công mới xử lý.
**Đầu ra thật:**
```text
12 lượt giao cho 3 sự kiện: xử lý thật 3 lần (khử trùng theo eventId bằng SET NX)
```
**Giải thích:** 12 lượt giao, 3 lần xử lý. Giới hạn: khoá chỉ sống 10 phút; bản giao trùng đến **sau** đó sẽ bị xử lý lại. Nếu thao tác có tác dụng phụ không lặp được thì thêm khoá ở kho dữ liệu chính (ví dụ ràng buộc duy nhất trong DB), đừng chỉ dựa vào Valkey.

#### Dự án 6.2 — Cache-aside đọc Board bằng `IDistributedCache`
1000 lượt đọc tuần tự trên 20 Board, nguồn giả mất 20 ms, TTL 30 giây.
**Đầu ra thật:**
```text
1000 lượt đọc 20 Board qua IDistributedCache (Valkey): chạm nguồn 20 lần, tổng 1231 ms (gồm 20 lần nguồn giả 20 ms)
```
**Giải thích:** 20 lần miss rồi toàn bộ hit; phần còn lại (~830 ms) nghĩa là khoảng 0,8 ms mỗi lượt đọc qua loopback của máy này, chỉ có giá trị tương đối. So với `HybridCache` ở [Part 3, Lab 3.5](part-3-multi-component.md): `IDistributedCache` trần **không gom request** (hai luồng cùng miss đều chạm nguồn) và mỗi lượt đọc đều đi qua mạng.

#### Dự án 6.3 — Rate limit cho gateway (bài tập, ⚠ chưa chạy trên gateway)
Dựa trên [Lab 4.5](part-4-advanced.md): bộ đếm tập trung cho nhiều instance gateway bằng Lua `INCR` + `PEXPIRE`, khoá theo `clientId:cửa-sổ`.
**Tiêu chí đạt:** (1) 3 instance giả lập cùng bộ đếm, tổng được cho đúng giới hạn; (2) hành vi khi Valkey không phản hồi (mở: cho qua có ghi log; đóng: chặn) được chọn có lý do; (3) so với bộ đếm trong bộ nhớ hiện tại: nêu **khi nào mới đáng** thêm Valkey (nhiều instance, cần giới hạn toàn cục).
**Quyết định đã có sẵn trong tài liệu:** với 1 instance, bộ đếm trong bộ nhớ là đủ; chưa cần Valkey.

## L5 Troubleshooting — Khi kết quả không như kỳ vọng

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Sự kiện xử lý hai lần | khoá khử trùng hết hạn, hoặc Valkey mất dữ liệu | `TTL` của `evt:*`, có persistence không | TTL dài hơn cửa sổ giao lại, ràng buộc duy nhất ở DB |
| Sự kiện bị bỏ qua mà chưa xử lý xong | `SET NX` thành công rồi tiến trình chết trước khi xử lý | log, xem trạng thái | đặt khoá "đang xử lý" có hạn ngắn, ghi "xong" sau khi hoàn tất |
| Hit ratio thấp dù có cache | key đổi giữa các lần gọi, TTL quá ngắn | key thật trong Valkey | key ổn định, TTL hợp lý |
| Đọc chậm hơn kỳ vọng | mỗi lượt qua mạng, tuần tự | đo độ trễ từng lệnh | `HybridCache` để có L1, gom lệnh |
| Rate limit sai khi Valkey lỗi | không có quy ước mở hoặc đóng | thử tắt Valkey | quyết định mở/đóng, timeout ngắn, log |
| Số đo mỗi lần chạy một khác | máy ồn, một lần chạy | chạy lại 3 lần | báo khoảng giá trị, nêu giới hạn |

## Tự kiểm tra

1. Vì sao khử trùng bằng `SET NX` có thể vẫn xử lý lại một sự kiện?
2. Khi nào bộ đếm trong bộ nhớ của gateway là đủ, khi nào cần Valkey?
3. Hạn chế nào làm số đo ở Dự án 6.2 chỉ mang tính tương đối?

<details>
<summary>Đáp án ngắn</summary>

1. Khoá hết hạn trước khi bản giao trùng đến, hoặc Valkey mất dữ liệu.
2. Một instance: bộ nhớ là đủ; nhiều instance cần giới hạn toàn cục: Valkey.
3. Một máy, loopback, một lần chạy, nguồn giả lập.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 và 6.2 (đầu ra thật).
**Chưa kiểm:** Dự án 6.3 là bài tập chưa chạy trên gateway thật. Quyết định "không cache và không dùng Valkey khi chưa cần" lấy từ `dependency-policy.md`, `config/dependencies.yaml` và `document/design/api-gateway.md`. Mọi số đo là của mô hình trên một máy, không phải đo `board-service` thật.
