# Part 5 — Production, Security, Operations (Caching)

> [!NOTE]
> **Mục tiêu:** theo dõi cache bằng chỉ số đúng, thiết kế để cache chết không kéo sập nguồn, tránh hai lớp lỗi bảo mật điển hình (poisoning và rò dữ liệu riêng), và ước tính chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** liệt kê chỉ số và ngưỡng cảnh báo, mô tả hành vi khi cache chết, chỉ ra một lỗ hổng cache key trong một ví dụ.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Cache khi đã là một phần của hệ thống thật

#### Quan sát cache
**Là gì:** các chỉ số cho biết cache có đang giúp hay đang làm hại: hit/miss, eviction, hết hạn, bộ nhớ, độ trễ.

#### Cache chết (và cache lạnh)
**Là gì:** cache mất (node chết, restart, xoá nhầm) hoặc bắt đầu rỗng; toàn bộ tải đổ về nguồn.

#### Bảo mật của cache
**Là gì:** hai nhóm lỗi: (1) kẻ tấn công **cài** nội dung độc vào cache dùng chung (poisoning); (2) dữ liệu của người này **lọt** vào response người khác thấy (cache deception, thiếu `Vary`/key, `public` cho dữ liệu riêng).

#### Chi phí cache
**Là gì:** tiền cho RAM và instance cache chạy 24/7, thường tính theo giờ dù tải thấp.

## L2 Why — Vì sao phần này quyết định cache có an toàn không

Cache bắt đầu như một tối ưu và dần thành **thành phần bắt buộc**: nguồn được cấu hình cho tải sau cache, nên khi cache mất, nguồn không chịu nổi. Google SRE mô tả đây là một trong các nguyên nhân của sự cố dây chuyền (cascading failure) ⚠. Bảo mật khác với phần còn lại ở chỗ lỗi thường **không thấy trong chỉ số**: hit ratio vẫn đẹp trong lúc cache phục vụ nội dung độc hoặc dữ liệu của người khác.

## L3 How — Cơ chế đáng nhớ

### Chỉ số cần theo dõi

| Chỉ số | Ý nghĩa | Dấu hiệu xấu |
|---|---|---|
| hit ratio (`keyspace_hits`/`keyspace_misses` ⚠) | tỉ lệ yêu cầu được cache phục vụ | tụt đột ngột, hoặc thấp kéo dài |
| `evicted_keys` ⚠ | số key bị đẩy ra vì đầy | tăng liên tục: cache nhỏ hơn tập nóng |
| `expired_keys` ⚠ | số key hết hạn | tăng vọt đồng loạt: thiếu jitter |
| bộ nhớ dùng so với `maxmemory` | mức đầy | gần 100% kéo dài |
| độ trễ lệnh, số kết nối | sức khoẻ node | tăng, hoặc chạm giới hạn kết nối |
| **tải lên nguồn** | tác dụng thật của cache | tăng khi hit ratio tụt |
| tỉ lệ đọc cũ (nếu đo được) | độ đúng | tăng sau thay đổi triển khai |

Tên chỉ số của Valkey lấy từ lệnh `INFO` (đã chạy ở [Valkey Part 5, Lab 5.1](../valkey/part-5-production-security-ops.md)); trên AWS các chỉ số tương ứng nằm ở CloudWatch với tên riêng ⚠.

### Khi cache chết

Mục tiêu: cache lỗi **không** biến thành sập nguồn.

#### Timeout ngắn và bỏ qua cache khi lỗi
**Là gì:** gọi cache với timeout ngắn; lỗi hay quá hạn thì coi như miss, đọc nguồn.
**Use case:** cache là tối ưu, nguồn chịu được tải thường ngày.
**Nhược:** nếu nguồn không chịu được tải đầy đủ thì đây chính là đường dẫn tới sập nguồn; cần các lớp sau.

#### Circuit breaker
**Là gì:** khi cache lỗi liên tục, **ngừng gọi cache** một thời gian thay vì chờ timeout mỗi yêu cầu.
**Use case:** tránh mỗi yêu cầu chậm thêm vài trăm ms vì chờ cache chết.

#### Giới hạn tải xuống nguồn (load shedding, giới hạn đồng thời)
**Là gì:** khi cache mất, giới hạn số lần đọc nguồn đồng thời và từ chối bớt yêu cầu ưu tiên thấp.
**Use case:** nguồn chỉ chịu được ví dụ 20% tải thường ngày; thà trả lỗi cho một phần còn hơn sập cả nguồn.

#### Gom request, làm nóng, triển khai từng phần
Gom request (Part 4) giảm hệ số nhân tải; làm nóng nạp trước các key phổ biến; triển khai cache theo từng node để không mất cả cụm cùng lúc.

### Bảo mật

#### Web cache poisoning
**Là gì:** cache dùng chung khoá theo một phần của yêu cầu (đường dẫn, host) nhưng response lại phụ thuộc vào **đầu vào không nằm trong khoá** (header như `X-Forwarded-Host`). Kẻ tấn công gửi yêu cầu chứa giá trị độc; response độc được lưu và phục vụ cho người sau (PortSwigger).
**Use case để hiểu:** trang chủ nhúng `<script src="//{host}/main.js">` với `host` lấy từ header; một yêu cầu độc làm mọi người sau tải script của kẻ tấn công (Lab 5.1).
**Phòng:** đưa mọi đầu vào ảnh hưởng response vào khoá hoặc **bỏ** chúng khỏi logic; không tin header do client gửi; giới hạn những gì được cache.

#### Web cache deception
**Là gì:** lừa cache lưu **trang riêng của người dùng** như thể là tài nguyên tĩnh công khai (ví dụ yêu cầu `/account/profile.css` mà server vẫn trả trang tài khoản), rồi kẻ tấn công lấy bản đó từ cache (PortSwigger ⚠ chưa mở trang này).
**Phòng:** `Cache-Control: private` hoặc `no-store` cho dữ liệu người dùng; không để CDN đoán cache theo đuôi file; server trả 404 cho đường dẫn lạ.

#### Dữ liệu riêng trong cache dùng chung
Thiếu `userId` trong key, thiếu `Vary`, hoặc `public` cho response theo người dùng là cách phổ biến nhất để lộ dữ liệu. Output cache mặc định bỏ qua yêu cầu có `Authorization` (Lab 2.5) chính vì lý do này.

#### Truy cập vào chính dịch vụ cache
Dịch vụ cache không nên mở ra Internet; dùng mạng riêng, xác thực và mã hoá khi đi qua mạng ⚠ (theo tài liệu Valkey, chưa tự kiểm). Không cache secret hoặc token trong bản rõ ở cache dùng chung.

### Chi phí

Chi phí đến từ **RAM chạy liên tục**, không theo tải. Phép tính dưới là **ước tính** từ giá đọc qua agent, cần kiểm lại trên trang giá trước khi dùng (⚠).

| Phương án | Giá niêm yết (⚠) | Ước tính một tháng (730 giờ) |
|---|---|---|
| ElastiCache node `r7g.large` | $0,1752/giờ | khoảng $127,9 |
| ElastiCache serverless Valkey, lưu trữ tối thiểu 100 MB | $0,084/GB-giờ | khoảng $6,13 (chưa tính phí xử lý yêu cầu) |

Bài học: cache nhỏ có thể rẻ hơn nhiều khi chọn mô hình tính theo mức dùng; cache lớn ổn định thường rẻ hơn khi chọn node cố định. Luôn so với chi phí **tăng cỡ nguồn** thay thế.

## L4 Hands-on — Lab Part 5

#### Lab 5.1 — Web cache poisoning (mô phỏng)
Cache chỉ khoá theo đường dẫn, bỏ qua `X-Forwarded-Host`, trong khi origin dùng header đó để dựng URL script.
**Đầu ra thật:**
```text
-- khoá cache chỉ đường dẫn
   kẻ tấn công  GET /home  X-Forwarded-Host: evil.test -> <script src="//evil.test/main.js"></script>
   nạn nhân     GET /home  (không header)             -> <script src="//evil.test/main.js"></script>   [HIT]
-- khoá cache có header X-Forwarded-Host
   kẻ tấn công  GET /home  X-Forwarded-Host: evil.test -> <script src="//evil.test/main.js"></script>
   nạn nhân     GET /home  (không header)             -> <script src="//app.example.com/main.js"></script>
```
**Giải thích:** khoá chỉ có đường dẫn: nạn nhân nhận trúng nội dung độc (`[HIT]`). Đưa header vào khoá thì nạn nhân nhận bản sạch. Đây là **mô hình** của lỗi, chạy trên bộ nhớ, không có hệ thống thật nào bị tấn công.

#### Lab 5.2 — Yêu cầu có `Authorization` không vào output cache
Đã chạy ở [Part 2, Lab 2.5](part-2-core-skills.md); kết quả: 3 yêu cầu kèm `Authorization` chạy handler 3 lần, 3 yêu cầu ẩn danh chỉ 1 lần.

#### Lab 5.3 — Chi phí ⚠ tính tay
730 giờ × $0,1752 = $127,896 ≈ $127,9; 0,1 GB × $0,084 × 730 = $6,132 ≈ $6,13. Phép tính đúng số học nhưng **giá đầu vào chưa xác minh trên trang giá**.

## L5 Troubleshooting — Sự cố vận hành cache

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Cache restart xong, DB quá tải | khởi động lạnh | thời điểm tải DB so với restart | làm nóng, giới hạn đồng thời, triển khai từng phần |
| Mọi yêu cầu chậm khi cache lỗi | timeout dài, không có circuit breaker | log timeout và độ trễ | timeout ngắn, circuit breaker |
| `evicted_keys` tăng liên tục | cache nhỏ hơn tập nóng, hoặc key không hết hạn | xem bộ nhớ, `maxmemory`, chính sách | tăng cỡ, đặt TTL, đổi chính sách |
| Hit ratio tụt sau khi triển khai | đổi cách sinh key (đổi tiền tố, tham số) | so key trước và sau | giữ key ổn định, đặt phiên bản có chủ đích |
| Người dùng thấy nội dung lạ (script, link) | poisoning: đầu vào không nằm trong khoá | thử yêu cầu với header lạ | đưa vào khoá hoặc bỏ khỏi logic; purge cache |
| Người dùng thấy dữ liệu của người khác | `public`, thiếu `Vary` hoặc `userId` trong key | xem header và key | `private`/`no-store`, sửa key, purge |
| Hoá đơn cache cao bất thường | node quá lớn, chạy 24/7, môi trường dev bật suốt | xem loại node và giờ chạy | giảm cỡ, tắt môi trường không dùng, serverless |

## L6 Production — Danh sách kiểm trước khi bật cache

- Có chỉ số hit ratio, eviction, bộ nhớ, độ trễ **và tải nguồn** trên cùng một dashboard, kèm ngưỡng cảnh báo.
- Đã thử **tắt cache** ở môi trường thử: nguồn chịu được bao nhiêu? Nếu không chịu được, có giới hạn tải và circuit breaker.
- Timeout cache ngắn; lỗi cache không làm lỗi yêu cầu.
- Mọi key có tiền tố ứng dụng/môi trường/phiên bản; mọi key chứa các thành phần phân biệt (người dùng, ngôn ngữ).
- Dữ liệu người dùng: `private` hoặc `no-store` ở HTTP; có `userId` trong key ở cache dùng chung.
- Mọi đầu vào ảnh hưởng response nằm trong khoá hoặc bị loại bỏ khỏi logic.
- Dịch vụ cache trong mạng riêng, có xác thực; không cache secret.
- Chi phí đã ước tính và so với phương án thay thế (tăng cỡ nguồn).
- Có runbook: xoá cache, làm nóng, khôi phục sau sự cố.

## L7 Architecture — Cache là tối ưu hay thành phần bắt buộc?

| Mô hình | Khi đúng | Hệ quả thiết kế |
|---|---|---|
| Cache là **tối ưu** | nguồn chịu được tải đầy đủ khi cache mất | timeout ngắn, bỏ qua khi lỗi, đơn giản |
| Cache là **bắt buộc** | nguồn không thể chịu tải đầy đủ (quy mô lớn) | cache có sao lưu và failover, làm nóng, giới hạn tải, thử tắt cache định kỳ |
| Cache **cho phép dữ liệu cũ** khi nguồn lỗi | nội dung chịu được cũ | soft TTL, `stale-if-error` |
| Không cache dữ liệu nhạy cảm | quyền truy cập, token, thông tin cá nhân | `no-store`; đọc từ nguồn |

Câu hỏi chọn: *nếu cache mất 10 phút, hệ thống có còn sống không?* Nếu không, đừng gọi nó là "tối ưu".

## Tự kiểm tra

1. Vì sao hit ratio cao chưa đảm bảo cache an toàn?
2. Cache chết, nguồn chỉ chịu 20% tải thường ngày: nêu hai biện pháp.
3. Cache poisoning khác cache deception ở điểm nào?
4. Nêu ba chỉ số của cache và ý nghĩa.

<details>
<summary>Đáp án ngắn</summary>

1. Cache có thể phục vụ nội dung độc hoặc dữ liệu người khác vẫn tính là hit.
2. Giới hạn đồng thời đọc nguồn và circuit breaker; gom request; làm nóng.
3. Poisoning: kẻ tấn công cài nội dung vào cache cho người khác. Deception: lừa cache lưu trang riêng của nạn nhân để kẻ tấn công lấy.
4. Ví dụ: hit ratio (hiệu quả), `evicted_keys` (cache nhỏ), bộ nhớ so với `maxmemory` (mức đầy).
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 (mô phỏng, đầu ra thật); Lab 5.2 tham chiếu Lab 2.5 đã chạy.
**Đã tự mở:** PortSwigger "Web cache poisoning" (đầu vào không nằm trong khoá, cơ chế tấn công); Cloudflare incident report về Cloudbleed (ví dụ rò dữ liệu qua cache, xem Part 7).
**Chỉ qua agent (⚠):** Google SRE về cascading failure; PortSwigger "Web cache deception"; Kettle "Practical Web Cache Poisoning"; các chỉ số Valkey `INFO`; tên chỉ số CloudWatch; giá ElastiCache (nhân với 730 giờ do mình tính); khuyến nghị bảo mật dịch vụ cache.
**Chưa kiểm:** giá niêm yết hiện hành, các chỉ số trên Valkey thật, hành vi circuit breaker thật (chưa chạy lab).
