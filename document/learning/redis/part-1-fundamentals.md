# Part 1 — Fundamentals & Mental Models (Redis)

> [!NOTE]
> **Mục tiêu:** hiểu Redis là gì ở thời điểm này (nhiều giấy phép, nhiều phiên bản, nhiều sản phẩm), nói đúng phiên bản nào dùng giấy phép nào, và so với Valkey.
> **Cần biết trước:** [Part 0](README.md), [Valkey Part 1](../valkey/part-1-fundamentals.md).
> **Thời lượng:** ~3 giờ · **Đầu ra kiểm được:** điền đúng giấy phép cho 7.2, 7.4, 8.0; phân biệt Redis Open Source, Redis Software, Redis Cloud, Redis Insight; đọc `INFO server` của hai server và chỉ ra khác biệt.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Redis ngày nay

#### Redis
**Là gì:** server key-value trong RAM với cấu trúc dữ liệu phong phú, chung phần lõi với Valkey (kiểu dữ liệu cơ bản, TTL, replication, persistence, cluster; xem [Valkey Part 1](../valkey/part-1-fundamentals.md)). Do Redis Ltd. phát triển; ở bản 8 hợp nhất thêm các mô-đun trước đây nằm trong Redis Stack.
**Use case:** cache, bộ đếm, phiên, hàng đợi nhẹ như Valkey; thêm JSON, chuỗi thời gian, tìm kiếm và vector khi dùng bản 8 (Part 3).

#### Ba giấy phép theo phiên bản
Theo trang giấy phép của Redis (đã mở):

| Phiên bản | Giấy phép |
|---|---|
| Redis ≤ 7.2 | BSD-3-Clause |
| Redis 7.4 đến 7.8 | RSALv2 **hoặc** SSPLv1 (tuỳ bạn chọn) |
| Redis Open Source ≥ 8.0 | RSALv2, SSPLv1 **hoặc** AGPLv3 (tuỳ bạn chọn) |

Các mô-đun đi kèm (RediSearch, RedisJSON, RedisTimeSeries, RedisBloom) mang ba giấy phép như trên từ Redis 8. Redis Insight ≥ 2.0 là SSPLv1; Redis Software và Redis Data Integration là thương mại; Redis Cloud theo điều khoản dịch vụ. Thư viện client phần lớn MIT, BSD hoặc Apache 2.0.

#### Các sản phẩm mang tên Redis
**Redis Open Source** (trước đây Redis Community Edition): server mã nguồn có sẵn, chính là thứ trong image `redis`. **Redis Software:** bản tự vận hành cấp doanh nghiệp (Active-Active, hỗ trợ 24/7). **Redis Cloud:** dịch vụ do Redis vận hành. **Redis Insight:** công cụ giao diện.

#### Quan hệ với Valkey
Valkey fork từ **Redis 7.2.4** (28/03/2024, Linux Foundation, BSD-3). Từ đó hai bên phát triển độc lập (Part 2 và [ghi chú so sánh](../redis-vs-valkey.md)).

## L2 Why — Vì sao phải biết phiên bản và giấy phép

Cùng một cái tên "Redis" hôm nay có ba giấy phép khác nhau tuỳ phiên bản, và tính năng thay đổi mạnh giữa 7.2 và 8. Chọn nhầm phiên bản đồng nghĩa với rủi ro pháp lý (AGPL bị cấm mặc định ở dự án này; RSALv2 và SSPLv1 hạn chế **cung cấp dịch vụ cạnh tranh**) hoặc dùng lệnh không có ở bản mình chạy (Lab 2.1). Với dự án: chính sách vẫn là Valkey (Part 6).

## L3 How — Cách đọc phiên bản, giấy phép, sản phẩm

### Nhịp phát hành
Theo trang release notes của Redis Open Source (đã mở): 7.4.0 tháng 7/2024; 8.0.0 tháng 5/2025; 8.2.0 tháng 8/2025; 8.4.0 tháng 11/2025; 8.6.0 tháng 2/2026; 8.8.0 tháng 5/2026; 8.10.0 tháng 7/2026; 8.12 đang RC (10/2026). Bản vá bảo mật ra thường xuyên cho nhiều nhánh (Part 5). Docker Hub `latest` luôn trỏ nhánh mới nhất nên **phải ghim tag** (ví dụ `8.10.2`).

### Điều khoản "dịch vụ cạnh tranh"
- **RSALv2:** không được thương mại hoá phần mềm hoặc cung cấp nó như dịch vụ được quản lý, lộ ra chức năng cho bên thứ ba.
- **SSPLv1:** nếu bạn cung cấp nó như một dịch vụ, phải công bố mã nguồn của các sửa đổi **và các lớp quản lý** theo SSPL.
- **AGPLv3:** không hạn chế lĩnh vực sử dụng; dùng qua mạng với bản đã sửa đổi thì phải cung cấp mã nguồn.
- Tháng 3/2024 Redis nêu rõ: tổ chức cung cấp dịch vụ cạnh tranh không còn được dùng bản mới miễn phí theo hai giấy phép đầu. (Nội dung trên lấy từ trang giấy phép; không phải tư vấn pháp lý.)

### Server tự khai báo
`INFO server` của Redis có `redis_version`, `redis_mode`, `io_threads_active`...; **Valkey** thêm `server_name:valkey` và `valkey_version`, trong khi vẫn báo `redis_version:7.2.4` để tương thích client (Lab 1.1).

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0. Ba container (`lh-r8` Redis 8.10.2, `lh-r72` Redis 7.2.16, `lh-vk` Valkey 9.1.2), đã dọn sau lab.

#### Lab 1.1 — `INFO server` của ba server
**Đầu ra thật:**
```text
Redis 8.10.2:  redis_version:8.10.2   redis_mode:standalone   io_threads_active:0
Redis 7.2.16:  redis_version:7.2.16   redis_mode:standalone   io_threads_active:0
Valkey 9.1.2:  redis_version:7.2.4    server_name:valkey      valkey_version:9.1.2   io_threads_active:0
```
**Giải thích:** Valkey giữ `redis_version:7.2.4` (phiên bản Redis mà nó fork từ) để client Redis nhận ra nó; muốn biết đúng phiên bản Valkey phải đọc `valkey_version`. Đoạn mã kiểm phiên bản trong ứng dụng đừng chỉ dựa vào `redis_version`.

#### Lab 1.2 — Phiên bản trong image
```text
redis:8.10.2-alpine -> Redis server v=8.10.2
redis:7.2.16-alpine -> Redis server v=7.2.16
```
**Giải thích:** tag `8.10.2` và `7.2.16` ghim đúng phiên bản; giấy phép đi theo phiên bản (Bảng L1), không ghi trong chính `--version`.

## L7 Architecture — Redis so với Valkey so với lựa chọn khác

| Lựa chọn | Khi chọn | Cái giá |
|---|---|---|
| Valkey (BSD-3) | cần giấy phép mở, tương thích Redis 7.2, cloud lớn hỗ trợ | không có mô-đun Redis 8 mặc định; chi tiết tương lai khác Redis |
| Redis ≤ 7.2 (BSD-3) | cần đúng 7.2, giấy phép mở | **không còn nhánh phát triển** của Redis Ltd. cho 7.2; ít tính năng mới |
| Redis 7.4 đến 7.8 | cần hash field expiration và chấp nhận RSALv2 hoặc SSPLv1 | giấy phép source-available |
| Redis ≥ 8 (chọn AGPLv3, RSALv2 hoặc SSPLv1) | cần JSON, time series, tìm kiếm, vector tích hợp sẵn | giấy phép; AGPL bị cấm mặc định ở dự án này |
| Redis Software / Cloud | muốn Redis vận hành giúp, Active-Active | thương mại, gắn nhà cung cấp |
| Memcached | key-value thuần, không cần cấu trúc dữ liệu | không có replication, persistence sẵn |
| **Không dùng server cache riêng** | một instance, tải thấp | không chia sẻ giữa instance ([Caching](../caching/README.md)) |

**Cách chọn:** hỏi (1) cần tính năng nào chỉ có ở Redis 8, (2) giấy phép nào chấp nhận được (AGPL, RSAL, SSPL, BSD), (3) có cung cấp Redis như dịch vụ cho bên thứ ba không, (4) cloud nào và họ hỗ trợ engine nào. Với dự án này chính sách mặc định là Valkey; chi tiết ở Part 6.

## Tự kiểm tra

1. Redis 7.2.x và Redis 7.4.x khác nhau về giấy phép thế nào?
2. Vì sao `redis_version` không đủ để biết bạn đang nói chuyện với Redis hay Valkey?
3. SSPLv1 đòi gì khi bạn cung cấp Redis như một dịch vụ?
4. Vì sao phải ghim tag image thay vì dùng `latest`?

<details>
<summary>Đáp án ngắn</summary>

1. 7.2.x là BSD-3; 7.4.x là RSALv2 hoặc SSPLv1 (tuỳ chọn).
2. Valkey cũng báo `redis_version:7.2.4`; phải đọc `server_name` và `valkey_version` (Lab 1.1).
3. Công bố mã nguồn của các sửa đổi và các lớp quản lý theo SSPL.
4. `latest` luôn đổi sang nhánh mới nhất (hiện 8.10.2) nên giấy phép và tính năng đổi theo.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 và 1.2 (image đã kéo, đầu ra thật).
**Đã tự mở:** hub.docker.com/_/redis (`latest` = 8.10.2; 7.2.16 là bản 7.2 mới nhất), redis.io `/legal/licenses/` (giấy phép theo phiên bản, điều khoản cạnh tranh), redis.io `/blog/agplv3/` (01/05/2025), release notes Redis Open Source (lịch phát hành), Linux Foundation (Valkey 28/03/2024), qua agent.
**Chỉ qua agent (⚠):** `LICENSE.txt` của repo redis/redis (nhánh `unstable`); tên "Redis Open Source" và "Redis Software" là theo docs, việc "Redis Enterprise đổi tên thành Redis Software" chỉ suy luận.
**Chưa kiểm:** Redis Software, Cloud, Insight (chưa chạy); ý nghĩa pháp lý chi tiết của từng giấy phép (không phải tư vấn pháp lý).
