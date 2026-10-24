# Part 6 — Projects (Caching)

> [!NOTE]
> **Mục tiêu:** áp các kỹ thuật của Part 1 đến 5 lên đường đọc Board của TeamBoard, đo lợi ích, độ cũ và rủi ro bằng số, thay vì tin cảm giác.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** một bảng so sánh các biến thể cache trên cùng một tải, và một đề xuất có lý do cho `board-service`.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

`board-service` (Clean Architecture, SQL Server) hiện **chưa có cache**; tải thấp. Quyết định nền của dự án là "đo trước khi cache". Phần này **không sửa `src/`** (issue TB-014 chỉ tài liệu): mọi mã chạy trong thư mục thử ngoài repo. Muốn đưa cache vào `board-service` thật thì tạo issue riêng, theo ranh giới Clean Architecture: lớp bọc cache (Decorator) nằm ở Infrastructure, handler không biết cache.

Bài toán mô phỏng: đường đọc Board, 100 Board, truy cập lệch (Zipf: vài Board rất nóng), 98% đọc và 2% cập nhật, nguồn mất 20 ms mỗi lần đọc.

## L4 Hands-on — Ba dự án

#### Dự án 6.1 — So sánh bốn biến thể trên cùng một tải (mô phỏng)

Tải: 10.000 thao tác trong 10 giây (1.000 thao tác/giây), TTL 1 giây. "Đọc cũ" = bản trả về có phiên bản thấp hơn nguồn lúc trả về. Biến thể: không cache; cache-aside chỉ TTL; cache-aside TTL + xoá khi ghi; `HybridCache` TTL + xoá khi ghi.
**Đầu ra thật:**
```text
không cache                        chạm nguồn   9805 | hit ratio   0.0% | p50  30.9 ms | p95  31.8 ms | đọc cũ    1 (0.0%) | 10043 ms
cache-aside, chỉ TTL               chạm nguồn   1100 | hit ratio  88.8% | p50   0.0 ms | p95  30.9 ms | đọc cũ 2640 (26.9%) | 10021 ms
cache-aside, TTL + xoá khi ghi     chạm nguồn   1329 | hit ratio  86.4% | p50   0.0 ms | p95  31.0 ms | đọc cũ   13 (0.1%) | 10038 ms
HybridCache, TTL + xoá khi ghi     chạm nguồn    914 | hit ratio  90.7% | p50   0.0 ms | p95  31.0 ms | đọc cũ   16 (0.2%) | 10013 ms
```
**Giải thích:**
- Không cache: 9.805 lần chạm nguồn, mọi lần đọc ~31 ms (20 ms của nguồn cộng khoảng 10 ms nhiễu bộ định giờ trên máy này; chỉ so tương đối).
- Cache-aside chỉ TTL: nguồn giảm hơn 8 lần (1.100) nhưng **26,9% lượt đọc trả bản cũ**: xoá khi ghi là điều kiện để dữ liệu đáng tin.
- Thêm xoá khi ghi: đọc cũ còn 0,1% (13 lượt, đúng là cửa sổ race hiếm của Lab 1.5), đổi lại chạm nguồn tăng từ 1.100 lên 1.329 vì key bị xoá phải nạp lại.
- `HybridCache`: ít chạm nguồn nhất (914) nhờ gom request đồng thời; đọc cũ 0,2%.
- p95 vẫn ~31 ms ở mọi biến thể có cache vì luôn có phần miss (khoảng 10% lượt đọc); p50 gần 0.

**Giới hạn:** mô hình trong một tiến trình, không có mạng, không có nhiều instance, nguồn là giả lập; chạy một lần, không lặp để có khoảng tin cậy. Chỉ dùng để so hướng chứ không dự đoán con số thật.

**Phát hiện đáng nhớ:** lần chạy đầu của mô phỏng này (đồng thời 50, 20.000 thao tác, thời gian không khống chế) đã cho kết quả khác hẳn (đọc cũ 76,9% với chỉ TTL) vì thao tác dồn lại trong vài trăm ms thay vì trải đều 10 giây. Kết luận về độ cũ phụ thuộc mạnh vào cách dựng tải; cần nêu rõ mô hình tải khi báo số.

#### Dự án 6.2 — Decorator cache-aside cho `IBoardReader` (bài tập, ⚠ chưa chạy trên `board-service`)

Đã chạy ở dạng thu nhỏ trong [Lab 2.3](part-2-core-skills.md) (100 lượt đọc, 5 lần chạm nguồn). Bài tập: dựng lại trong dự án thử, thêm ba thứ mà Lab 2.3 cố ý bỏ:
1. **Xoá khi ghi** ở lệnh cập nhật Board, theo thứ tự ghi nguồn rồi xoá key.
2. **Gom request** bằng `HybridCache` hoặc `Lazy<Task>`, kiểm bằng 50 yêu cầu đồng thời.
3. **Test** (xUnit, `inner` là fake đếm số lần gọi): hai lần đọc chỉ chạm nguồn một lần; sau khi cập nhật, lần đọc kế không trả bản cũ.
**Tiêu chí đạt:** ba test xanh; ghi lại số lần chạm nguồn của kịch bản 50 yêu cầu.

#### Dự án 6.3 — Header HTTP cho endpoint Board (bài tập, ⚠ chưa chạy trên `board-service`)

Dựa trên [Lab 2.1](part-2-core-skills.md): với `GET /boards/{id}` trả dữ liệu theo người dùng, đặt `Cache-Control: private, max-age=…` và `ETag`, tự so `If-None-Match` để trả `304`. Kiểm bằng `curl -i` ghi lại ba lần gọi (200, 304, 200 sau khi đổi).
**Tiêu chí đạt:** có đủ ba lượt gọi với đầu ra thật; có test chứng minh `private` (không `public`) cho dữ liệu theo người dùng.

## L5 Troubleshooting — Khi số đo không như kỳ vọng

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Số đo giữa hai lần chạy khác nhau nhiều | tải dồn cục, bộ định giờ thô, chạy một lần | chạy lại 3 lần, so trải đều thời gian | khống chế thời điểm gửi, lặp lại, báo khoảng giá trị |
| Hit ratio cao nhưng tỉ lệ đọc cũ cao | chỉ dùng TTL | đo đọc cũ cùng hit ratio (Dự án 6.1) | xoá khi ghi, TTL ngắn hơn |
| `p95` không giảm dù hit ratio cao | phần miss vẫn chậm | xem phân phối độ trễ, không chỉ trung bình | gom request, làm mới sớm, giảm độ trễ nguồn |
| Chạm nguồn tăng sau khi thêm xoá khi ghi | key bị xoá phải nạp lại | đếm lần xoá và lần nạp | chấp nhận, hoặc cập nhật thay vì xoá (write-through) |
| Test cache chập chờn | phụ thuộc thời gian thật | xem test dùng `Task.Delay` hay đồng hồ | dùng `TimeProvider` giả, không ngủ thật |

## Tự kiểm tra

1. Vì sao "chỉ TTL" có hit ratio đẹp mà vẫn nguy hiểm?
2. Vì sao thêm xoá khi ghi làm số lần chạm nguồn tăng?
3. Hạn chế nào khiến kết quả Dự án 6.1 không dùng để dự đoán số thật?

<details>
<summary>Đáp án ngắn</summary>

1. 26,9% lượt đọc trả bản cũ ở Dự án 6.1; hit ratio không đo độ đúng.
2. Key bị xoá thì lần đọc sau phải nạp lại từ nguồn.
3. Một tiến trình, nguồn giả lập, không mạng, không nhiều instance, chạy một lần, kết quả phụ thuộc cách dựng tải.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 (mô phỏng, đầu ra thật, môi trường ở Part 0, chạy một lần).
**Chưa kiểm:** Dự án 6.2 và 6.3 là bài tập chưa chạy trên `board-service`; mọi số liệu ở Dự án 6.1 là của mô hình, không phải đo `board-service` thật. Quyết định "đo trước khi cache" lấy từ tài liệu thiết kế nội bộ (`decision-framework.md` mục 2.7).
