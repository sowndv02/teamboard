# Part 7 — Troubleshooting & Mastery (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán sự cố từ triệu chứng, học từ các case thực tế, rồi tự thiết kế và bảo vệ phần dùng Valkey.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống, trả lời được câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: ứng dụng khựng mỗi lần chạy báo cáo đêm

Mỗi đêm báo cáo gọi `KEYS board:*` để gom Board; trong lúc đó mọi API chậm hoặc timeout.
<details>
<summary>Chẩn đoán</summary>

`KEYS` O(N), chạy trên luồng duy nhất nên chặn mọi client (Lab 2.3: 200.000 key mất ~4,6 ms, tăng theo số key). Sửa: `SCAN` theo trang, hoặc lưu danh sách Board vào một Set riêng; chạy trên replica.
</details>

#### Tình huống 2: restart xong mất hết phiên đăng nhập

Sau khi bảo trì, mọi người dùng phải đăng nhập lại. Valkey chạy trong container không gắn volume, mặc định.
<details>
<summary>Chẩn đoán</summary>

Không có volume thì file RDB mất cùng container; hoặc persistence tắt (Lab 3.1). Sửa: bật RDB/AOF và gắn volume; quyết định phiên có được phép mất không; ghi vào runbook.
</details>

#### Tình huống 3: ghi báo thành công nhưng sau failover biến mất

Primary chết, replica được promote, một số bản ghi cuối biến mất.
<details>
<summary>Chẩn đoán</summary>

Replication bất đồng bộ: bản ghi chưa tới replica thì mất (Part 3). Sửa: chấp nhận cửa sổ; `WAIT` và `min-replicas-to-write` để thu hẹp; nếu không được mất thì Valkey không là nơi duy nhất giữ dữ liệu (GitHub, case dưới).
</details>

#### Tình huống 4: lỗi OOM khi ghi, dù chỉ là cache

Ứng dụng nhận `OOM command not allowed when used memory > 'maxmemory'`.
<details>
<summary>Chẩn đoán</summary>

`maxmemory-policy` mặc định `noeviction` (Lab 4.1): server từ chối ghi khi đầy. Cache nên dùng `allkeys-lru` hoặc `allkeys-lfu`; nếu dữ liệu không được bỏ thì tăng RAM và theo dõi. Slack gặp một dạng khác: queue làm đầy RAM, cả enqueue lẫn dequeue kẹt.
</details>

#### Tình huống 5: có người xoá sạch dữ liệu

Một sáng toàn bộ key biến mất; log không có lệnh từ ứng dụng.
<details>
<summary>Chẩn đoán</summary>

Cổng Valkey mở ra ngoài và user `default` không mật khẩu (Lab 5.2): ai cũng chạy được `FLUSHALL`. Sửa: mạng riêng, ACL tối thiểu quyền cho từng ứng dụng, đặt mật khẩu, TLS, xoay mật khẩu và kiểm log truy cập.
</details>

## L7 Architecture — Trong thực tế

Mức: **đã mở** là tự mở nguồn gốc; **⚠ qua agent** là agent nghiên cứu đã đọc, chưa tự đối chiếu.

| Công ty / dự án | Bối cảnh và vấn đề | Cách làm | Kết quả, bài học | Nguồn (mức) |
|---|---|---|---|---|
| **GitHub** (2017, cập nhật 2024) | Redis giữ cả cache LRU lẫn dữ liệu bền không có bản khác | Tắt persistence; chuyển dữ liệu bền sang MySQL (GitHub::KV); feature flag, ghi đôi | Họ nói đã vượt quá Redis làm kho bền; dựa trên số đo, không phải MySQL cho mọi thứ | [Moving persistent data out of Redis](https://github.blog/engineering/infrastructure/moving-persistent-data-out-of-redis/) (đã mở) |
| **Redis Ltd.** (20/03/2024) | Doanh thu thương mại chảy qua các cloud lớn | Từ Redis 7.4: RSALv2 hoặc SSPLv1, không còn BSD-3; không hồi tố; dùng nội bộ vẫn được | Nền cho rủi ro giấy phép trong chính sách dự án | [Redis adopts dual source-available licensing](https://redis.io/blog/redis-adopts-dual-source-available-licensing/) (đã mở) |
| **Valkey / Linux Foundation** (28/03/2024) | Cộng đồng cần bản giấy phép mở | Fork Redis 7.2.4, giữ BSD-3; AWS, Google Cloud, Oracle, Ericsson, Snap hậu thuẫn | Nhánh thay thế mà chính sách dự án chỉ ra | [Linux Foundation launches Valkey](https://www.linuxfoundation.org/press/linux-foundation-launches-open-source-valkey-community) (đã mở) |
| **Redis Ltd.** (01/05/2025) | Cộng đồng phản ứng với SSPL | Thêm AGPLv3 làm lựa chọn từ Redis 8 | Bài thừa nhận quyết định trước "hurt our relationship with the Redis community"; AGPL bị cấm mặc định ở dự án | [Redis is open source again](https://redis.io/blog/agplv3/) (⚠ qua agent) |
| **Stack Overflow** (2019) | Hệ nhiều site cần cache chung | Cache hai tầng: RAM cục bộ trước, Redis khi miss; replica; pub/sub cho websocket | Số liệu của bài (08/2019): ~1,59 tỷ lệnh/ngày, đỉnh 86.982/s, CPU trung bình ~2%; một node lớn gánh tải | [How Stack Overflow caches apps](https://stackoverflow.blog/2019/08/06/how-stack-overflow-caches-apps-for-a-multi-tenant-architecture/) (⚠ qua agent) |
| **Slack** (đăng 06/12/2017) | Job queue trên Redis; DB chậm làm queue phình, Redis chạm `maxmemory` | Kafka phía trước làm bộ đệm bền; hai thành phần Go chuyển job có giới hạn tốc độ | Redis làm queue cần áp lực ngược; hết RAM thì kẹt cả enqueue và dequeue | [Scaling Slack's job queue](https://slack.engineering/scaling-slacks-job-queue/) (⚠ qua agent) |
| **Twitter** (2013, bài tóm tắt) | Timeline nhà trong Redis | Fan-out khi ghi; giữ 800 ID mới nhất mỗi timeline; nhiều TB RAM, mỗi timeline 3 bản | Ví dụ thiết kế (giới hạn kích thước danh sách), nguồn thứ cấp | [VMware case study](https://blogs.vmware.com/tanzu/case-study-staple-yourself-to-a-tweet-to-understand-30-billion-redis-updates-per-day/) (⚠ thứ cấp) |

### Khi Redis/Valkey là nguyên nhân sự cố

| Sự cố | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **Discord, 10/2017** | Google Cloud tự di chuyển một Redis primary; cụm API xử lý failover kém, phải khởi động lại toàn dịch vụ, tái kết nối hàng triệu client | Xử lý failover phía client quan trọng ngang HA phía server | [discordstatus.com/incidents/qk9cdgnqnhcn](https://discordstatus.com/incidents/qk9cdgnqnhcn) (⚠ qua agent) |
| **Slack (bài 12/2017)** | DB chậm làm queue phình, Redis đầy RAM, enqueue và dequeue kẹt, phục hồi thủ công | Đừng dựa vào RAM cho hàng đợi không có áp lực ngược | Như trên (⚠ qua agent) |
| **GitHub** | Dữ liệu bền nằm duy nhất trong Redis | Chuyển ra DB bền; đo từng loại dữ liệu | Như trên (đã mở) |

**Mô hình phổ biến:** cache với TTL + `allkeys-lru`; Valkey làm chỉ mục phụ, không là nguồn sự thật; nhiều tầng (RAM cục bộ trước, Valkey sau); HA bằng replica; mở rộng bằng cluster.
**Chưa tìm được nguồn gốc đã mở cho:** Instagram (không mở được), Pinterest, Uber, Snap; một postmortem công khai về mất dữ liệu do failover cũng chưa tìm được. Không nêu các case đó.

### Đánh đổi khi thiết kế

- **Nhanh so với bền:** RAM nhanh nhưng persistence luôn có cửa sổ mất; muốn bền hơn thì chậm hơn hoặc dùng MemoryDB/DB.
- **Đơn giản so với đúng:** khoá `SET NX PX` đơn giản nhưng không đủ cho tính đúng; fencing token tốn thêm.
- **Tự chạy so với quản lý:** rẻ ở quy mô nhỏ nhưng tự gánh vận hành, vá, sao lưu.
- **Khoá giấy phép:** Redis đổi giấy phép; Valkey BSD-3, nhưng hai bên đang tách nhau về tính năng.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Chọn kiểu dữ liệu cho bộ đếm, bảng xếp hạng, hàng đợi gần nhất, đếm duy nhất; nêu lý do.
2. Vì sao `KEYS` nguy hiểm, và thay bằng gì?
3. So sánh RDB và AOF; chọn gì cho phiên đăng nhập?
4. Vì sao replication bất đồng bộ làm mất ghi khi failover? `WAIT` giúp đến đâu?
5. Khoá `SET NX PX` an toàn đến mức nào? Khi nào cần fencing token?
6. Cluster giải quyết gì, kèm hạn chế nào?
7. Pub/sub và Streams khác nhau thế nào?
8. Khi nào **không** dùng Valkey?

### Bài mở

#### Bài 1 — Valkey cho `notification-service`
Consumer idempotent theo `eventId`, hàng đợi có DLQ. Thiết kế: dùng Valkey ở đâu (khử trùng, rate limit gửi), mức bền cần, khi Valkey mất dữ liệu thì chuyện gì xảy ra, phương án không dùng Valkey. **Tiêu chí:** nêu rõ điều gì **không** được để chỉ trong Valkey; có kế hoạch khi Valkey lỗi; ít nhất hai phương án bị loại kèm lý do.

#### Bài 2 — Quyết định có thêm Valkey cho `board-service`
Dùng số đo của chính bạn từ Dự án 6.2 và [Caching Dự án 6.1](../caching/part-6-projects.md) để quyết: `IMemoryCache`, `HybridCache` (L1 + Valkey), hay không cache. **Tiêu chí:** có số đo của bạn; nêu giới hạn của mô hình; có phương án không dùng Valkey; nêu việc cần duyệt (ADR, giấy phép, chi phí).

#### Bài 3 — Phản biện một thiết kế
Đồng nghiệp đề xuất "dùng Redis 8 làm kho chính cho Task vì nhanh, không cần backup". Viết phản biện nêu giấy phép, độ bền, mất dữ liệu khi failover, bảo mật, chi phí; đề xuất phương án thay thế. **Tiêu chí:** mỗi ý trỏ tới một Lab hoặc case cụ thể.

### Tiêu chí thành thạo

- Giải thích không nhìn tài liệu: kiểu dữ liệu và use case, RDB so với AOF, giới hạn của replication, vì sao `KEYS` nguy hiểm.
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có số đo của chính bạn và nêu giới hạn của số đo đó.
- Nói được ít nhất ba tình huống không nên dùng Valkey.

## Tự kiểm tra

1. Case nào cho thấy "đừng để Redis là nơi duy nhất giữ dữ liệu"?
2. Vì sao Redis 8 không tự động hợp lệ với dự án?
3. Slack và Discord cho hai bài học khác nhau gì?

<details>
<summary>Đáp án ngắn</summary>

1. GitHub (chuyển dữ liệu bền sang MySQL); Slack cũng cho thấy hết RAM làm kẹt hàng đợi.
2. Redis 8 có AGPLv3, bị cấm mặc định; cần người dùng duyệt và ADR.
3. Slack: áp lực ngược và bộ đệm bền cho hàng đợi. Discord: xử lý failover phía client.
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** GitHub "Moving persistent data out of Redis"; redis.io (đổi giấy phép 20/03/2024); Linux Foundation (Valkey 28/03/2024).
**Chỉ qua agent (⚠):** redis.io/blog/agplv3 (nội dung đã đọc bởi agent), Stack Overflow 06/08/2019, Slack 06/12/2017, Discord 10/2017, VMware case study về Twitter (bài tóm tắt một buổi nói).
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ cơ chế đã học, không phải sự cố đã gặp ở dự án này; chưa mở lại bài Instagram (lỗi kết nối khi tra).
