# Bài toán kinh điển để đối chiếu và đọc thêm

Dùng ở bước 5 (use case tương tự) và chế độ `explain` của skill `system-design`. Mỗi khối là **điểm dẫn dắt**, không phải lời giải đầy đủ: nêu thách thức cốt lõi, pattern thường dùng, và nó khớp chỗ nào của TeamBoard. Muốn học sâu thì đọc nguồn ở cuối từng bài.

**Quy tắc dùng:**
- Mô tả ở mức pattern công khai, không bịa chi tiết nội bộ hay số liệu của công ty cụ thể.
- Mức tin cậy của nguồn đọc thêm: `Chắc` (sách/tài liệu nổi tiếng, tồn tại chắc chắn) / `Vừa` (nhớ tên bài nhưng nên tự tra lại) / `Cần xác minh`. Không đưa đường dẫn chi tiết khi chưa kiểm tra được; chỉ ghi nơi để tìm.
- Luôn nói **điểm khác** giữa bài toán kinh điển và trường hợp của ta (thường tải của ta nhỏ hơn nhiều, nên lời giải kinh điển có thể là quá mức).

## Khuôn mỗi bài

**Bài toán** → **thách thức cốt lõi** → **pattern thường dùng và vì sao hợp** → **khớp với TeamBoard** → **câu hỏi tự thử** → **đọc thêm**.

---

## 1. Tranh chấp tài nguyên (đặt chỗ, bán vé, giữ hàng)

- **Bài toán:** nhiều người tranh cùng một tài nguyên giới hạn; không được bán trùng.
- **Thách thức:** đồng thời, ghi đè, giữ chỗ tạm thời rồi hết hạn.
- **Pattern:** khoá lạc quan (cột `version`/ETag), ràng buộc duy nhất ở DB, giữ chỗ có TTL, hàng đợi tuần tự hoá theo khoá, khoá phân tán (chỉ khi cần, vì khó đúng).
- **Khớp TeamBoard:** hai người cùng giao/đổi trạng thái một Task (`task-service`); `If-Match`/`409`/`412` trong `api-endpoints.md`.
- **Tự thử:** hai request đến cùng lúc thì ai thắng? Người thua thấy gì? Giữ chỗ hết hạn xử lý thế nào?
- **Đọc thêm:** *Designing Data-Intensive Applications* (M. Kleppmann), phần giao dịch và cô lập (`Chắc`).

## 2. Thanh toán và thao tác không được lặp (idempotency)

- **Bài toán:** client retry khi timeout; không được trừ tiền/tạo bản ghi hai lần.
- **Thách thức:** không biết lần gửi trước thành công hay chưa; "exactly-once" không thực tế trong mạng.
- **Pattern:** idempotency key + lưu kết quả theo key, at-least-once kèm xử lý idempotent, ghi trạng thái trước rồi mới tác động ngoài, bồi hoàn (saga).
- **Khớp TeamBoard:** `Idempotency-Key` ở `POST` tạo Task; consumer dedupe theo `eventId` (`messaging.md`).
- **Tự thử:** cùng key nhưng body khác thì sao? Key lưu bao lâu? Lưu key và ghi dữ liệu có cùng giao dịch không?
- **Đọc thêm:** AWS Builders' Library, bài về làm retry an toàn bằng API idempotent (`Vừa`, tìm theo tên); *Microservices Patterns* (C. Richardson) về saga (`Chắc`).

## 3. Thông báo và fan-out (một sự kiện, nhiều bên nhận)

- **Bài toán:** một hành động kích hoạt thông báo cho nhiều người/kênh.
- **Thách thức:** số người nhận lớn, kênh chậm/lỗi không được kéo chậm hành động gốc, gửi trùng/mất.
- **Pattern:** pub/sub (SNS) + hàng đợi riêng mỗi consumer (SQS) + DLQ; fan-out khi ghi hoặc khi đọc; giới hạn tốc độ gửi; retry có backoff; ưu tiên kênh.
- **Khớp TeamBoard:** `notification-service` nhận từ `teamboard-events`.
- **Tự thử:** email provider chết 1 giờ thì hàng đợi ra sao? Người dùng nhận hai thông báo thì lỗi ở đâu?
- **Đọc thêm:** *System Design Interview* (A. Xu), phần hệ thống thông báo (`Chắc`, kiểm tra tập/chương).

## 4. Feed hoạt động (đọc nhiều, ghi theo kiểu nối thêm)

- **Bài toán:** hiển thị dòng hoạt động theo thời gian cho từng người/board.
- **Thách thức:** đọc nhiều hơn ghi nhiều lần, thứ tự thời gian, phân trang ổn định, người "nóng" có nhiều người theo dõi.
- **Pattern:** fan-out khi ghi (dựng sẵn feed) so với fan-out khi đọc (gộp lúc truy vấn); cursor pagination; kho khoá-giá trị theo khoá phân vùng + sort key thời gian.
- **Khớp TeamBoard:** `activity-service` (DynamoDB), phân trang cursor.
- **Tự thử:** board có 10.000 hoạt động thì trang đầu lấy thế nào? Sửa/xoá hoạt động cũ thì feed đã dựng sẵn xử lý ra sao?
- **Đọc thêm:** *System Design Interview*, phần news feed (`Chắc`); *Designing Data-Intensive Applications*, phần mô hình dữ liệu và mô tả tải (`Chắc`).

## 5. Hàng đợi tác vụ nền và xử lý bất đồng bộ

- **Bài toán:** việc chậm (gửi mail, xuất báo cáo) không được chặn request.
- **Thách thức:** retry, tác vụ độc, thứ tự, theo dõi trạng thái, quá tải ngược (backpressure).
- **Pattern:** hàng đợi + worker, DLQ, visibility timeout, trả `202` + địa chỉ kiểm tra trạng thái, giới hạn đồng thời, job idempotent.
- **Khớp TeamBoard:** consumer SQS, API `202` trong `api-endpoints.md`, `backend-engineering.md` mục 6.
- **Tự thử:** worker chết giữa chừng thì message đi đâu? Việc chạy hai lần có hại không?
- **Đọc thêm:** *Release It!* (M. Nygard) về ổn định hệ thống (`Chắc`); tài liệu Amazon SQS về DLQ và visibility timeout (`Chắc`, tìm trong tài liệu AWS).

## 6. Giới hạn tốc độ (rate limiter)

- **Bài toán:** bảo vệ dịch vụ khỏi quá tải/lạm dụng.
- **Thách thức:** đếm chính xác trong hệ phân tán, công bằng giữa người dùng, hành vi ở rìa cửa sổ.
- **Pattern:** token bucket, leaky bucket, sliding window; đếm cục bộ (xấp xỉ) hay tập trung (Redis); đặt ở gateway; trả `429` + `Retry-After`.
- **Khớp TeamBoard:** gateway (YARP); `api-endpoints.md` mục 7.
- **Tự thử:** hai instance gateway thì đếm chung hay riêng? Chấp nhận sai lệch bao nhiêu?
- **Đọc thêm:** *System Design Interview*, phần rate limiter (`Chắc`).

## 7. Chịu lỗi khi gọi dịch vụ ngoài (timeout, retry, circuit breaker)

- **Bài toán:** phụ thuộc chậm/chết không được kéo sập cả chuỗi.
- **Thách thức:** retry gây bão, thác đổ lỗi (cascading failure), timeout đặt sai.
- **Pattern:** timeout rõ ràng, retry có backoff + jitter chỉ cho thao tác idempotent, circuit breaker, bulkhead, hạ cấp chức năng.
- **Khớp TeamBoard:** call đồng bộ giữa service; `backend-engineering.md` mục 8.
- **Tự thử:** A gọi B gọi C, C chậm 10 lần thì A thế nào? Retry ở cả ba tầng thì nhân lên bao nhiêu?
- **Đọc thêm:** AWS Builders' Library, bài về timeout, retry và backoff với jitter (`Vừa`, tìm theo tên); *Release It!* (`Chắc`).

## 8. Nhất quán xuyên service (giao dịch phân tán)

- **Bài toán:** một nghiệp vụ chạm nhiều service (xoá Board → huỷ Task → dọn feed).
- **Thách thức:** không có giao dịch chung; hỏng giữa chừng; bồi hoàn.
- **Pattern:** outbox, saga (choreography/orchestration), nhất quán cuối cùng, trạng thái trung gian rõ ràng.
- **Khớp TeamBoard:** `task-service` outbox; saga "xoá Board" (bài tập trong kiến trúc).
- **Tự thử:** bước 2 thất bại sau khi bước 1 đã xong thì làm gì? Người dùng thấy trạng thái nào ở giữa?
- **Đọc thêm:** *Microservices Patterns* (C. Richardson), phần saga và giao dịch (`Chắc`); trang microservices.io của cùng tác giả (`Vừa`).

## 9. Mô hình đọc/ghi tách biệt (CQRS) và event sourcing

- **Bài toán:** nhu cầu đọc và ghi rất khác nhau, hoặc cần lịch sử đầy đủ/kiểm toán.
- **Thách thức:** độ trễ giữa ghi và đọc, tiến hoá schema sự kiện, dựng lại trạng thái.
- **Pattern:** event store, projection tạo read model, snapshot, versioning sự kiện, chấp nhận nhất quán cuối cùng ở phía đọc.
- **Khớp TeamBoard:** `task-service` (CQRS + Event Sourcing) `[Chọn để học]`.
- **Tự thử:** người dùng ghi xong rồi đọc ngay mà chưa thấy thì UI xử lý thế nào? Đổi trường của một sự kiện đã lưu thì sao?
- **Đọc thêm:** Azure Architecture Center, các cloud design patterns về CQRS và Event Sourcing (`Vừa`, tìm theo tên); *Designing Data-Intensive Applications*, phần log và dòng sự kiện (`Chắc`).

## 10. Rút gọn URL / tạo ID duy nhất (đọc rất nhiều, ID phân tán)

- **Bài toán:** sinh mã ngắn duy nhất, đọc với tải cao.
- **Thách thức:** sinh ID không đụng nhau khi nhiều node, phân vùng dữ liệu, cache nóng.
- **Pattern:** ID tăng dần phân tán (kiểu snowflake), UUID/ULID, băm + xử lý va chạm, cache cho đường đọc.
- **Khớp TeamBoard:** chọn kiểu `{id}` (GUID chuỗi) trong `api-endpoints.md`; sắp xếp theo thời gian.
- **Tự thử:** ID có cần sắp theo thời gian được không? Lộ ID tuần tự có rủi ro gì (đoán ID)?
- **Đọc thêm:** *System Design Interview*, phần rút gọn URL và sinh ID duy nhất (`Chắc`).

## 11. Tìm kiếm và lọc phức tạp

- **Bài toán:** tìm theo từ khoá/lọc nhiều chiều trên lượng dữ liệu lớn.
- **Thách thức:** SQL `LIKE` không đủ, đồng bộ dữ liệu sang chỉ mục tìm kiếm, độ trễ cập nhật.
- **Pattern:** chỉ mục đảo (OpenSearch/Elasticsearch), đồng bộ qua sự kiện, đánh chỉ mục không đồng bộ, ranh giới giữa nguồn sự thật và bản sao.
- **Khớp TeamBoard:** chưa cần ở giai đoạn này; chỉ cân nhắc nếu spec có yêu cầu tìm kiếm văn bản. Bắt đầu bằng chỉ mục SQL và lọc theo trường.
- **Tự thử:** sao chỉ mục tìm kiếm lệch dữ liệu gốc thì phát hiện và sửa thế nào?
- **Đọc thêm:** tài liệu chính thức của OpenSearch/Elasticsearch (`Chắc`); *Designing Data-Intensive Applications*, phần dữ liệu dẫn xuất (`Chắc`).

## 12. Bộ đệm (cache) và đọc nóng

- **Bài toán:** giảm tải/độ trễ cho dữ liệu đọc nhiều.
- **Thách thức:** vô hiệu hoá cache, cache stampede, độ cũ chấp nhận được.
- **Pattern:** cache-aside, write-through, TTL + jitter, request coalescing, cache cục bộ so với tập trung.
- **Khớp TeamBoard:** chưa cần (tải thấp); xem `decision-framework.md` mục 2.7, đo trước khi thêm.
- **Tự thử:** dữ liệu sai trong cache bao lâu thì chấp nhận được? Khi cache chết, DB có chịu nổi không?
- **Đọc thêm:** *System Design Interview* (`Chắc`); tài liệu AWS về chiến lược caching (`Vừa`).

---

## Sách và nguồn tổng hợp (tra cứu chung)

| Nguồn | Dùng để học | Tin cậy |
|---|---|---|
| *Designing Data-Intensive Applications* — Martin Kleppmann | Nền tảng dữ liệu phân tán: sao chép, phân vùng, giao dịch, nhất quán, luồng | Chắc |
| *System Design Interview* (tập 1, 2) — Alex Xu | Các bài toán phỏng vấn kinh điển, cách trình bày | Chắc |
| *Microservices Patterns* — Chris Richardson | Saga, outbox, CQRS, phân rã service | Chắc |
| *Building Microservices* — Sam Newman | Ranh giới service, giao tiếp, vận hành | Chắc |
| *Release It!* — Michael Nygard | Ổn định hệ thống, mẫu chịu lỗi | Chắc |
| Google *Site Reliability Engineering* | Vận hành, SLO, xử lý sự cố | Chắc |
| AWS Builders' Library | Bài kỹ thuật về cách Amazon vận hành hệ phân tán (retry, timeout, idempotency...) | Chắc về sự tồn tại; tên bài cụ thể: Vừa |
| Azure Architecture Center — Cloud Design Patterns | Danh mục pattern (CQRS, saga, circuit breaker...) | Chắc về sự tồn tại; tên pattern cụ thể: Vừa |
| microservices.io | Danh mục pattern của C. Richardson | Vừa |

Khi một lựa chọn trong thiết kế dựa vào nguồn nào, ghi tên nguồn và mức xác minh vào mục "Liên kết" của design doc; chưa mở đọc thật thì ghi `chưa xác minh nguồn`.
