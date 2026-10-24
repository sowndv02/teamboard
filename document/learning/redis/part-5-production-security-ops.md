# Part 5 — Production, Security, Operations (Redis)

> [!NOTE]
> **Mục tiêu:** đánh giá rủi ro khi chạy Redis ở production: nghĩa vụ giấy phép, vá lỗi thường xuyên, ACL khi có mô-đun, dịch vụ quản lý nào hỗ trợ engine nào, và chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md), [Valkey Part 5](../valkey/part-5-production-security-ops.md) (quan sát, ACL, backup chung).
> **Thời lượng:** ~4 giờ · **Đầu ra kiểm được:** nêu nghĩa vụ giấy phép theo kiểu triển khai, theo dõi và vá bản Redis đúng cách, viết ACL cho user có mô-đun, so sánh dịch vụ quản lý.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Redis khi chạy thật

#### Nghĩa vụ giấy phép khi triển khai
Dùng nội bộ (ứng dụng của bạn gọi Redis) khác với **cung cấp Redis như một dịch vụ cho bên thứ ba**; hai loại này chịu điều khoản khác nhau (Part 1).

#### Vá lỗi
Redis ≥ 8 có nhiều bản vá bảo mật; đây là phần của công việc vận hành.

#### ACL và mô-đun
Mô-đun thêm danh mục ACL mới và gộp lệnh vào `@read`, `@write` (Part 3).

#### Dịch vụ quản lý
ElastiCache, MemoryDB, Memorystore, Azure Managed Redis, Redis Cloud: mỗi dịch vụ hỗ trợ engine và phiên bản khác nhau.

## L2 Why — Vì sao phần này quyết định việc chọn Redis

Hai việc làm Redis khác Valkey khi vận hành: **giấy phép** (Part 1) và **nhịp vá lỗi** của các bản 8.x (dưới). Chọn engine mà không xét hai việc này là bỏ sót rủi ro lớn nhất, không phải tính năng.

## L3 How — Cơ chế đáng nhớ

### Giấy phép khi triển khai
| Cách dùng | RSALv2 | SSPLv1 | AGPLv3 (Redis ≥ 8) |
|---|---|---|---|
| Ứng dụng nội bộ gọi Redis | được | được | được |
| Cung cấp Redis như dịch vụ cho bên thứ ba | **cấm** (thương mại hoá, dịch vụ quản lý lộ chức năng) | phải công bố mã nguồn của sửa đổi **và lớp quản lý** | phải cung cấp mã nguồn bản đã sửa đổi nếu dùng qua mạng |
| Nhúng hay phân phối lại | theo điều khoản | theo điều khoản | theo điều khoản copyleft |

Bảng chỉ tóm lược điều trang giấy phép của Redis nói (đã mở); **không phải tư vấn pháp lý**, cần luật sư với tình huống thật. Với dự án này: dùng nội bộ, nhưng chính sách `dependency-policy.md` cấm AGPL theo mặc định và đặt Redis server ở T3.

### Vá lỗi
Từ release notes (đã mở): lỗ hổng RCE qua Lua (CVE-2025-49844 và các CVE khác) được vá ở 8.0.4 và 8.2.2 (tháng 10/2025); nhiều lỗi RCE liên quan `RESTORE` và Lua được vá ở 8.2.6 và 8.4.3 (tháng 5/2026); lỗi bỏ qua xác thực chứng chỉ TLS do ký tự NUL trong CN được vá ở 8.10.1 (tháng 8/2026). Nhiều nhánh được vá song song (8.4.6 và 8.2.9 cuối tháng 8/2026). **Hệ quả:** ghim tag **đã vá** (không dùng `latest`, không dùng bản cũ), có quy trình cập nhật định kỳ và theo dõi thông báo bảo mật. Với Valkey cũng phải vá, nhưng bài này chỉ nêu điều đã đọc cho Redis.

### ACL với mô-đun
Quy tắc cũ như `+@read` giờ có thể gồm `JSON.GET`, `TS.RANGE`, `FT.SEARCH`, `BF.EXISTS` (Lab 3.3). Kiểm lại ACL sau khi lên Redis 8: liệt kê lệnh tường minh hoặc dùng danh mục hẹp (`+@json`...). Phần ACL chung (user `default` không mật khẩu, `NOPERM`...) xem [Valkey Part 5](../valkey/part-5-production-security-ops.md).

### Dịch vụ quản lý và engine
| Dịch vụ | Engine được hỗ trợ (theo nguồn đã đọc) |
|---|---|
| Amazon ElastiCache | Valkey, Memcached, Redis OSS cũ (Serverless 4.0.10 đến 7.1; 4.0 và 5.0 EOL 31/01/2029); OSS 7.2+ chưa xác nhận ⚠ |
| Amazon MemoryDB | Valkey và Redis OSS tương thích, bền (nhật ký giao dịch Multi-AZ) |
| Google Memorystore | Memorystore for Valkey GA 18/04/2025 (kết quả tìm kiếm, ⚠) |
| Azure Managed Redis | dựa trên Redis Enterprise, hỗ trợ Redis 7.4.x, có mô-đun JSON, Search, Bloom, TimeSeries (trang 28/05/2026); trang không nhắc Valkey |
| Redis Cloud, Redis Software | do Redis cung cấp, thương mại |

AWS nêu Valkey rẻ hơn Redis OSS: node khoảng 20%, Serverless khoảng 33%, MemoryDB khoảng 30% (FAQ và blog của AWS, số của nhà cung cấp, chưa kiểm bằng Pricing Calculator).

### Chi phí
Phụ thuộc kiểu triển khai: tự chạy (chi phí vận hành, vá lỗi), dịch vụ quản lý (giờ máy hay mức dùng), Redis Cloud/Software (hợp đồng thương mại, chưa đọc giá ⚠). Cùng chi phí khung với [Valkey Part 5](../valkey/part-5-production-security-ops.md); khác biệt chính là giấy phép và nhà cung cấp.

## L4 Hands-on — Lab Part 5

Môi trường: như Part 0. Lab đọc **thông tin image**, không chạy dịch vụ.

#### Lab 5.1 — Image: kích thước, người dùng, nhãn, công cụ
**Đầu ra thật** (`docker image inspect`):
```text
redis:8.10.2-alpine   Size=39.126.067  User=""  Labels=null   Entrypoint docker-entrypoint.sh  Cmd redis-server
redis:7.2.16-alpine   Size=15.865.398  User=""  Labels=null   (cùng Entrypoint và Cmd)
valkey/valkey:9.1.2-alpine  Size=18.809.321  User=""  Labels={"org.opencontainers.image.source":".../valkey-io/valkey"}  Entrypoint tini -- docker-entrypoint.sh
công cụ trong image: cả ba có redis-benchmark, redis-check-aof, redis-check-rdb, redis-cli, redis-sentinel, redis-server;
                     image Valkey có thêm valkey-benchmark, valkey-check-aof, valkey-check-rdb, valkey-cli, valkey-sentinel, valkey-server
```
**Giải thích:** image Redis 8.10.2 lớn gấp khoảng 2,5 lần Redis 7.2.16 (mô-đun hợp nhất; hai số cùng một cách đo của `inspect`, khác với cột SIZE của `docker images`). **Không** image nào có nhãn giấy phép: giấy phép đi theo phiên bản (Part 1), phải tự theo dõi. Cả ba chạy **root** mặc định (`User` rỗng): trong Kubernetes hay ECS nên đặt `USER` hoặc `securityContext` không root ([Container Part 5](../container/part-5-production-security-ops.md)). Valkey dùng `tini` làm PID 1; image Valkey giữ tên `redis-*` để tương thích với công cụ cũ.

#### Lab 5.2 — ACL mô-đun
Đã chạy ở [Lab 3.3](part-3-multi-component.md): user `+@read` đọc được `JSON.GET` nhưng bị chặn `JSON.SET`; `ACL CAT` ở Redis 8.10.2 có thêm danh mục mô-đun, hai server kia không có.

## L5 Troubleshooting — Sự cố vận hành riêng của Redis

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Bị quét và khai thác lỗ hổng Lua hay `RESTORE` | chạy bản chưa vá | so tag với thông báo bảo mật | nâng lên bản đã vá; hạn chế mạng; ACL chặn `EVAL`, `RESTORE` nếu không cần |
| Xác thực TLS bằng chứng chỉ bị bỏ qua | lỗi CN/NUL ở bản trước 8.10.1 | phiên bản và cấu hình TLS | nâng lên 8.10.1 trở lên |
| Hỏi về nghĩa vụ giấy phép khi bán dịch vụ | cung cấp Redis cho bên thứ ba | đọc trang giấy phép; hỏi luật sư | chọn Valkey hoặc giấy phép phù hợp |
| ACL cho phép nhiều lệnh hơn sau khi lên Redis 8 | `@read`/`@write` gộp lệnh mô-đun | `ACL CAT read` | liệt kê lệnh tường minh |
| Tag `latest` đổi sang phiên bản mới | không ghim tag (`latest` hiện là 8.10.2) | `docker image inspect` | ghim tag, kiểm trước khi nâng |
| Image chạy root | mặc định image | `User` rỗng (Lab 5.1) | `USER`/`securityContext` không root |
| Dịch vụ quản lý không có phiên bản Redis cần | ElastiCache chưa hỗ trợ bản mới của Redis ⚠ | trang phiên bản của dịch vụ | chọn engine dịch vụ hỗ trợ (Valkey) hoặc tự chạy |
| Hoá đơn cao | chạy 24/7, bản Enterprise | loại và giờ chạy | chọn Valkey rẻ hơn theo AWS, tắt môi trường không dùng |

## L6 Production — Danh sách kiểm khi chọn và chạy Redis

- **Giấy phép:** đã đọc trang giấy phép; biết dùng nội bộ hay cung cấp dịch vụ; AGPL cần phê duyệt ở dự án này; có ADR.
- **Phiên bản:** ghim tag đã vá; nhịp cập nhật; theo dõi thông báo bảo mật; kế hoạch khi một nhánh hết vá.
- **Mạng và xác thực:** không mở ra Internet; ACL với danh sách lệnh tường minh; TLS đã vá; user `default` không để `nopass` (xem Valkey Part 5).
- **Mô-đun:** chỉ bật cái cần; kiểm ACL sau khi nâng cấp; biết dữ liệu mô-đun không di chuyển sang engine khác (Part 6).
- **Dịch vụ quản lý:** kiểm engine và phiên bản dịch vụ hỗ trợ, ngày hết hỗ trợ.
- **Chi phí và nhà cung cấp:** so Redis với Valkey; không lấy số giá của hãng làm kết luận.
- **Image:** chạy không root; `docker run` có giới hạn tài nguyên ([Container Part 5](../container/part-5-production-security-ops.md)).

## L7 Architecture — Chọn cách triển khai

| Bối cảnh | Chọn | Cái giá |
|---|---|---|
| Ứng dụng nội bộ, cần giấy phép mở | Valkey (BSD-3), tự chạy hoặc dịch vụ quản lý | không có mô-đun Redis 8 mặc định |
| Cần mô-đun Redis 8 và chấp nhận giấy phép | Redis 8 (chọn AGPL, RSAL hoặc SSPL), tự chạy | giấy phép; vá lỗi nhanh |
| Muốn Redis vận hành giúp | Redis Cloud hoặc Redis Software | thương mại, gắn nhà cung cấp |
| Ở AWS, ít vận hành | ElastiCache for Valkey | gắn AWS; không có mô-đun Redis 8 |
| Cần bền, là kho chính | MemoryDB | đắt hơn cache |
| Ở Azure | Azure Managed Redis (Redis Enterprise, mô-đun) | gắn Azure; trang không nêu Valkey |
| **Không dùng Redis** | cache trong tiến trình, HybridCache, CDN | không chia sẻ giữa instance |

**Câu hỏi quyết định:** *nếu giấy phép đổi lần nữa, chi phí chuyển đi là bao nhiêu?* Chạy qua giao diện và client chuẩn, tránh phụ thuộc lệnh độc quyền, và giữ dữ liệu ở dạng xuất được (Part 6).

## Tự kiểm tra

1. Dùng Redis trong ứng dụng nội bộ và cung cấp Redis cho khách hàng khác nhau thế nào về giấy phép?
2. Vì sao phải ghim tag Redis đã vá?
3. Sau khi lên Redis 8, ACL `+@read` thay đổi nghĩa thế nào?
4. Các image trong Lab 5.1 chạy bằng user nào?

<details>
<summary>Đáp án ngắn</summary>

1. Dùng nội bộ được ở cả ba giấy phép; cung cấp như dịch vụ bị RSALv2 cấm và SSPLv1/AGPLv3 đòi công bố mã nguồn.
2. Các bản 8.x liên tục vá RCE và lỗi xác thực; `latest` đổi theo nhánh mới nhất và bản cũ có thể chưa vá.
3. Nó gộp thêm lệnh đọc của mô-đun (`JSON.GET`, `TS.RANGE`, `FT.SEARCH`...).
4. Root (`User` rỗng); cần đặt user không root.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 (`docker image inspect`, đầu ra thật); Lab 5.2 tham chiếu Lab 3.3 đã chạy.
**Đã tự mở:** redis.io `/legal/licenses/` (điều khoản cạnh tranh), release notes Redis Open Source 8.0 đến 8.10 (các CVE và bản vá), AWS ElastiCache FAQ và blog Valkey/MemoryDB (giá chênh lệch, phiên bản), Azure `azure/redis/overview` (28/05/2026), qua agent.
**Chỉ qua agent (⚠):** Google Memorystore for Valkey GA 18/04/2025 (snippet); danh sách phiên bản Redis OSS hiện hành trên ElastiCache (FAQ có thể cũ); giá Redis Cloud và Redis Software (chưa đọc).
**Chưa kiểm:** nghĩa vụ pháp lý (không phải tư vấn pháp lý); giá hiện hành bằng Pricing Calculator; từng bản vá có mặt trong tag cụ thể ngoài 8.10.2.
