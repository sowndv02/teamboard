# Part 7 — Troubleshooting & Mastery (Caching)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán sự cố cache từ triệu chứng, học từ các sự cố và hệ thống thật, rồi tự thiết kế và bảo vệ một lớp cache.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống dưới đây, trả lời được các câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: sửa tên Board rồi vẫn thấy tên cũ

Người dùng đổi tên Board, F5 vẫn thấy tên cũ trong khoảng nửa phút, sau đó tự đúng. `board-service` chạy 3 instance dùng `HybridCache` với TTL 5 phút; lệnh cập nhật gọi `RemoveAsync`.
<details>
<summary>Chẩn đoán</summary>

`RemoveAsync` chỉ xoá L1 của instance xử lý lệnh và L2; L1 của hai instance kia vẫn giữ bản cũ đến khi `LocalCacheExpiration` hết (Lab 3.1). Sửa: rút `LocalCacheExpiration` hoặc phát sự kiện xoá L1 (Part 3), và giữ TTL làm lưới an toàn.
</details>

#### Tình huống 2: DB tăng vọt đúng mỗi 10 phút

Đồ thị truy vấn DB có đỉnh nhọn cách đều 10 phút; trùng thời điểm TTL của nhóm key được nạp lúc khởi động.
<details>
<summary>Chẩn đoán</summary>

Hàng loạt key cùng TTL hết hạn cùng lúc (Lab 2.4). Sửa: jitter ±10–20%; với key nóng thêm gom request hoặc XFetch (Lab 4.1).
</details>

#### Tình huống 3: restart cache xong, cả hệ thống chậm rồi lỗi

Bảo trì cache lúc đông người; ngay sau đó DB cạn kết nối và các service gọi nó lỗi dây chuyền.
<details>
<summary>Chẩn đoán</summary>

Khởi động lạnh: toàn bộ tải đổ về DB không chịu nổi (Part 5; Discord và Honeycomb ở L7). Sửa: thử tắt cache ở môi trường thử, giới hạn đồng thời đọc nguồn, circuit breaker, làm nóng, bảo trì ngoài giờ cao điểm, triển khai từng node.
</details>

#### Tình huống 4: người dùng này thấy tên Board của người dùng kia

Sau khi bật output cache cho endpoint danh sách Board, một số người báo thấy Board không phải của mình.
<details>
<summary>Chẩn đoán</summary>

Endpoint theo người dùng bị cache theo URL mà không theo người dùng (khoá thiếu `userId`, hoặc `Cache-Control: public`). Output cache mặc định bỏ qua yêu cầu có `Authorization` (Lab 2.5), nên nếu vẫn lộ thì có cấu hình ghi đè hoặc cache ở tầng khác (CDN). Sửa: `private`/`no-store`, thêm `userId` vào khoá hoặc không cache endpoint này, rồi purge cache (Part 5).
</details>

#### Tình huống 5: trang chủ tải script lạ

Một số người dùng thấy trang chủ tải script từ domain lạ trong vài giờ rồi hết.
<details>
<summary>Chẩn đoán</summary>

Cache poisoning: origin dùng header (như `X-Forwarded-Host`) để dựng URL, cache không khoá theo header đó nên bản độc được phục vụ cho người khác (Lab 5.1). Sửa: bỏ logic dựa vào header client gửi hoặc đưa nó vào khoá; purge; xác thực giá trị header.
</details>

## L7 Architecture — Trong thực tế

Mỗi dòng có nguồn; **mức độ tin cậy** ghi ở cột cuối.

| Công ty / dự án | Bối cảnh và vấn đề | Cách làm | Kết quả, bài học | Nguồn (mức) |
|---|---|---|---|---|
| **Facebook** (2013) | Memcache trước MySQL; stale set, thundering herd, DB quá tải khi server cache chết | **Lease** (set bị từ chối nếu key đã bị xoá), token 10 giây mỗi key, **gutter pool**, `mcsqueal` đọc log commit để xoá key | Nhóm key dễ herd: đỉnh DB 17K/s → 1,3K/s; gutter giảm lỗi client thấy 99% (số theo paper) | [Scaling Memcache at Facebook](https://www.usenix.org/system/files/conference/nsdi13/nsdi13-final170_update.pdf) (đã đọc pdf) |
| **Cloudflare** (2024) | Stampede khi object nổi tiếng hết hạn; khoá thêm độ trễ và điểm hỏng | Không khoá: gần hết hạn, mỗi yêu cầu tự bốc thăm làm mới sớm với xác suất tăng dần | Giữ tải origin kiểm soát được mà không phối hợp. Ví dụ số trong bài có chỗ không khớp phép tính nên không trích số | [Sometimes I cache](https://blog.cloudflare.com/sometimes-i-cache/) (đã mở) |
| **Uber** (2025) | Lớp cache phía trước kho dữ liệu, quy mô hơn 150M hàng/giây ở đỉnh | TTL, cộng CDC (Flux) và xoá ở đường ghi; TTL mặc định 5 phút | Ví dụ TTL 24 giờ vẫn giữ hit ratio trên 99,9% nhờ invalidation theo sự kiện; invalidation tốt cho phép TTL dài | [How Uber serves over 150 million reads](https://www.uber.com/us/en/blog/how-uber-serves-over-150-million-reads/) (26/08/2025) (đã mở) |
| **Meta** (2022) | Cache (TAO) sai lệch với DB rất hiếm nhưng khó thấy | Polaris theo dõi từng lần ghi để phát hiện không nhất quán | Tỉ lệ nhất quán từ 99,9999% lên 99,99999999% trong cửa sổ 5 phút (theo bài) | [Cache made consistent](https://engineering.fb.com/2022/06/08/core-infra/cache-made-consistent/) (08/06/2022) (đã mở) |
| **AWS Builders' Library** (2019) | Cache cho dịch vụ quy mô lớn | Inline và side cache, on-box và external, soft/hard TTL, gom request, negative caching | Cảnh báo **hành vi hai chế độ**: phụ thuộc cache thì có thể sập khi cache mất; nên thử tải khi tắt cache | [Caching challenges and strategies](https://builder.aws.com/content/3K9TeXp6strMiJ4vhakpRD7BQWO/caching-challenges-and-strategies) (đã mở) |
| **Netflix EVCache** | Cache phân tán trên Memcached cho microservice trên AWS | Consistent hashing, một cluster mỗi zone, đọc ưu tiên zone cục bộ, ghi vào mọi zone | Chọn sẵn sàng và độ trễ thấp hơn nhất quán mạnh | [EVCache wiki](https://github.com/netflix/evcache/wiki) (⚠ qua agent) |
| **Cloudflare Cache Reserve** (2022) | CDN dùng LRU nên nội dung ít truy cập bị đẩy ra | Thêm tầng lưu bền (R2) trên cùng tiered cache | Bài ở giai đoạn closed beta nên chưa có số đo | [Introducing Cache Reserve](https://blog.cloudflare.com/introducing-cache-reserve/) (⚠ qua agent) |

### Khi cache là nguyên nhân sự cố

| Sự cố | Điều đã xảy ra | Bài học | Nguồn | Mức |
|---|---|---|---|---|
| **Discord, 12/02/2019** | Redis cache giảm thông lượng ở 4 node; tải đổ xuống MongoDB làm quá tải; circuit breaker có bug; khoảng 4,5 giờ | Cache hỏng thì tải dồn thẳng xuống DB; cơ chế bảo vệ cũng có thể lỗi | [discordstatus.com](https://discordstatus.com/incidents/j13tlh8g4hkq) | Đã mở |
| **Cloudflare, 13 đến 18/02/2017 (Cloudbleed)** | Lỗi bộ nhớ trong bộ phân tích HTML làm response lộ dữ liệu khách khác (1 trên 3,3 triệu yêu cầu); công cụ tìm kiếm lưu bản cache đó | Dữ liệu riêng lọt vào bản cache của bên thứ ba rất khó thu hồi | [incident report](https://blog.cloudflare.com/incident-report-on-memory-leak-caused-by-cloudflare-parser-bug/) | Đã mở |
| **GitHub, 02/2026** | Thay đổi cấu hình cache user settings gây ghi lại cache hàng loạt, làm quá tải job nền và proxy Git HTTPS; hai đợt (87 và 76 phút) | Đổi cấu hình cache là thay đổi rủi ro; ghi lại hàng loạt là một dạng stampede | [availability report 02/2026](https://github.blog/news-insights/company-news/github-availability-report-february-2026/) | ⚠ qua agent |
| **Honeycomb, 25/07/2023** | Sau khi chuyển cluster, bản ghi "last seen" của cache ingest ngừng được ghi; cache yếu dần, DB cạn connection; sập khoảng 1 giờ 8 phút | Cache là một phần của đường chịu tải; cần cách làm nóng/làm mới có kiểm soát và bảo vệ DB | [Postmortem](https://www.honeycomb.io/blog/postmortem-rds-clogs-cache-refresh-crash-loops) | ⚠ qua agent |

**Mô hình phổ biến:** cache-aside với TTL cho phần lớn ứng dụng; cache ngoài dùng chung khi có nhiều instance; invalidation bằng TTL kèm xoá chủ động hoặc sự kiện; giám sát hit ratio và chuẩn bị cho khởi động lạnh.
**Chưa tìm được nguồn gốc đã mở cho:** Twitter (Twemcache/Pelikan), Instagram, write-behind ở quy mô lớn; vì vậy không nêu các case đó.

### Đánh đổi khi thiết kế

- **Tươi so với nhanh:** TTL ngắn tươi hơn nhưng tải nguồn cao hơn; mỗi lần rút TTL một nửa, miss tăng gấp đôi trong điều kiện đều.
- **Nhất quán so với sẵn sàng:** phục vụ bản cũ khi nguồn lỗi là chọn sẵn sàng.
- **Đơn giản so với đúng:** chỉ TTL đơn giản nhất nhưng đọc cũ cao nhất (Dự án 6.1).
- **Tối ưu so với bắt buộc:** nếu mất cache làm sập hệ thống, cache là thành phần bắt buộc, phải đầu tư như mọi thành phần bắt buộc.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Giải thích sáu cơ chế đọc/ghi cache và cho mỗi cái một use case.
2. Vì sao cache-aside ghi nguồn trước rồi xoá key? Còn cửa sổ lỗi nào?
3. `no-cache` khác `no-store`? Khi nào dùng `private`?
4. Thiết kế chống stampede cho một key nóng có 10 instance.
5. Thêm node vào cụm cache `hash mod N` thì sao? Cách khắc phục?
6. Cache chết thì hệ thống ra sao? Làm sao biết trước?
7. Cho ví dụ cache làm lộ dữ liệu người dùng, và cách ngăn.
8. Khi nào **không** nên cache?

### Bài mở

#### Bài 1 — Thiết kế lớp cache cho `task-service`
Task đọc nhiều, được cập nhật bằng event sourcing. Thiết kế: tầng nào, key, TTL, invalidation (từ sự kiện nào), chống stampede, cách đo độ cũ. **Tiêu chí:** nêu độ cũ chấp nhận được và lý do; có kế hoạch khi cache mất; có ít nhất hai phương án bị loại kèm lý do.

#### Bài 2 — Đề xuất cache cho `board-service`
Dùng kết quả Dự án 6.1 của chính bạn để quyết: cache hay không, tầng nào, cơ chế ghi, TTL. **Tiêu chí:** có số đo của bạn; nêu rõ giới hạn của mô hình đo; có phương án "không cache".

#### Bài 3 — Phản biện một thiết kế
Một đồng nghiệp đề xuất "bọc Redis cho mọi truy vấn, TTL 1 giờ". Viết phản biện nêu ít nhất 5 rủi ro đã học (độ cũ, stampede, cache chết, rò dữ liệu, chi phí) và phương án thay thế. **Tiêu chí:** mỗi rủi ro trỏ tới một Lab hoặc case cụ thể.

### Tiêu chí thành thạo

- Giải thích mà không nhìn tài liệu: sáu cơ chế, thứ tự ghi–xoá, vì sao stampede xảy ra và hai cách chống.
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có số đo của chính bạn và nêu được giới hạn của số đo đó.
- Nói được ít nhất ba tình huống không nên cache.

## Tự kiểm tra

1. Vì sao hệ thống phụ thuộc cache có thể sập khi cache mất?
2. Cloudbleed cho thấy điều gì khác với một cache bị đầy?
3. Vì sao Uber vẫn giữ TTL dù đã có invalidation theo sự kiện?

<details>
<summary>Đáp án ngắn</summary>

1. Nguồn được cấu hình cho tải sau cache; mất cache thì tải thật đổ xuống nó (Discord, Honeycomb).
2. Dữ liệu riêng bị lưu ở bản cache của bên thứ ba và phải thu hồi cả ở đó; lỗi bảo mật chứ không phải hiệu năng.
3. Sự kiện có thể trễ hoặc mất; TTL là lưới an toàn (Part 4).
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** Facebook "Scaling Memcache" (PDF); Cloudflare "Sometimes I cache"; AWS Builders' Library; Discord status; Uber Engineering về CacheFront (26/08/2025); Meta "Cache made consistent" (08/06/2022); Cloudflare incident report về Cloudbleed (13 đến 18/02/2017).
**Chỉ qua agent (⚠):** Netflix EVCache wiki, Cloudflare Cache Reserve, GitHub availability report 02/2026, Honeycomb postmortem 25/07/2023.
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ cơ chế đã học, không phải sự cố đã gặp ở dự án này.