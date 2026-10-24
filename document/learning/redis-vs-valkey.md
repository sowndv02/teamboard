# Redis và Valkey: ghi chú so sánh

> Loại: so sánh công cụ · Lộ trình: [Redis](redis/README.md) và [Valkey](valkey/README.md) · Cơ sở: lab trên Redis 8.10.2, Redis 7.2.16 và Valkey 9.1.2 (24/10/2026) và nguồn chính thức đã mở
> **Cách đọc mức xác minh:** **chạy** = tự chạy trên image thật; **mở** = tự mở nguồn chính thức; **⚠** = chỉ qua agent hoặc kết quả tìm kiếm, chưa tự đối chiếu. Số hiệu năng của hãng không được dùng để so sánh.

## Tóm tắt một đoạn

Valkey fork từ Redis 7.2.4 (28/03/2024) và giữ BSD-3; Redis ≥ 7.4 đổi sang giấy phép source-available (RSALv2 hoặc SSPLv1), Redis ≥ 8.0 thêm AGPLv3. Lõi (kiểu dữ liệu cơ bản, `SET NX PX`, Lua, pub/sub, replication) và client như `StackExchange.Redis` chạy giống nhau ở cả hai (đã chạy). Từ đó hai bên khác nhau ở **giấy phép, mô-đun, một số lệnh, định dạng tệp RDB và lệnh vận hành cluster**. Chính sách dự án này: Valkey, không tự động chấp nhận Redis 8.

## So sánh

| Mục | Redis (8.10.2 / 7.2.16) | Valkey (9.1.2) | Mức |
|---|---|---|---|
| Giấy phép | ≤ 7.2: BSD-3; 7.4 đến 7.8: RSALv2 hoặc SSPLv1; ≥ 8: RSALv2, SSPLv1 hoặc AGPLv3 | BSD-3-Clause, Linux Foundation | mở |
| Nguồn gốc | Redis Ltd. | fork từ Redis 7.2.4 (28/03/2024); AWS, Google Cloud, Oracle, Ericsson, Snap hậu thuẫn | mở |
| `INFO server` | `redis_version:8.10.2` | `redis_version:7.2.4` kèm `server_name:valkey`, `valkey_version:9.1.2` | chạy |
| Kiểu dữ liệu cơ bản, `SET NX PX`, Lua, pub/sub | có | có; kết quả lab .NET giống nhau | chạy |
| Hash field expiration (`HEXPIRE`, `HTTL`, `HPERSIST`) | có từ 7.4; **không** ở 7.2.16 | có (Valkey 9.0); chạy trên 9.1.2 | chạy |
| `HSETEX`, `HGETEX`, `HGETDEL`, `MSETEX` | có ở 8.10.2; không ở 7.2.16 | có ở 9.1.2 | chạy |
| `SET ... IFEQ` | có ở 8.10.2; không ở 7.2.16 | có ở 9.1.2 | chạy |
| Xoá khoá nếu đúng chủ | `DELEX` (và `DIGEST`) | `DELIFEQ` (tồn tại, chưa chạy thử) | chạy (`COMMAND INFO`) |
| `XDELEX`, `XACKDEL` | có | không | chạy |
| JSON, time series, Bloom/Cuckoo/CMS/Top-K/t-digest, vector set, query engine (`FT.*`) | nạp sẵn ở Redis 8.10.2 | không có mặc định; mô-đun riêng valkey-json, valkey-bloom, valkey-search | chạy (Redis); ⚠ (mô-đun Valkey) |
| `COMMANDLOG`, `CLIENT IMPORT-SOURCE`, `CLUSTERSCAN`, `SCRIPT SHOW` | không | có | chạy |
| Di chuyển slot nguyên tử | `CLUSTER MIGRATION` (8.4) | `CLUSTER MIGRATESLOTS` (9.0) và các lệnh liên quan | chạy (lệnh tồn tại); ⚠ (chưa dựng cluster) |
| Danh mục ACL | thêm `json`, `timeseries`, `bloom`, `cuckoo`, `cms`, `topk`, `tdigest`, `search`, `array` | không có các danh mục đó | chạy |
| Tiêu đề tệp RDB | `REDIS0011` (7.2), `REDIS0015` (8.10.2) | `VALKEY080` | chạy |
| Nạp RDB chéo | 7.2 nạp được vào Valkey và Redis 8; Redis 8 không nạp được vào Valkey hay Redis 7.2 | Valkey không nạp được RDB Redis 8; Redis không nạp được RDB Valkey | chạy |
| Client | `StackExchange.Redis` 3.4.20 chạy lab giống nhau | như vậy; có client riêng Valkey GLIDE | chạy; mở (GLIDE) |
| Số lệnh `COMMAND COUNT` | 447 (8.10.2), 241 (7.2.16) | 257 | chạy (không nên so trực tiếp: 447 gồm lệnh mô-đun và nội bộ) |
| Kích thước image alpine (`inspect`) | 39,1 MB (8.10.2), 15,9 MB (7.2.16) | 18,8 MB | chạy |
| Dịch vụ AWS | ElastiCache giữ các bản Redis OSS cũ; chưa xác nhận Redis OSS 7.2+ ⚠ | ElastiCache và MemoryDB hỗ trợ; AWS nêu rẻ hơn: node ~20%, Serverless ~33%, MemoryDB ~30% (số của AWS) | mở; ⚠ |
| Dịch vụ khác | Azure Managed Redis: Redis Enterprise, Redis 7.4.x, có mô-đun | Google Memorystore for Valkey GA 18/04/2025 | mở (Azure); ⚠ (Google) |
| Hiệu năng | tuyên bố của hãng (ví dụ "hơn 30% cho tải cache", 4 lõi) | tuyên bố của hãng (ví dụ 1,2 triệu QPS, tăng tới 40% so với 8.1) | **không dùng để so sánh** |

## Điều ghi chú này **không** nói

- Không nói bên nào nhanh hơn, ổn định hơn hay an toàn hơn: chưa có phép đo cùng điều kiện, và hai bên đều vá lỗi.
- Không nói Valkey là "bản thay thế hoàn toàn": tập lệnh Valkey 9.1.2 bao gồm tập lệnh Redis 7.2.16 (lab: không thiếu lệnh nào, thêm 29) nhưng **không** có các mô-đun và lệnh riêng của Redis 8.
- Không phải tư vấn pháp lý về giấy phép.
- Không cập nhật theo thời gian: mọi mục đúng cho các phiên bản trong tiêu đề; kiểm lại bằng `COMMAND INFO` và release notes khi dùng phiên bản khác.

## Quy tắc rút ra

1. **Ghim phiên bản và tag**; giấy phép đi theo phiên bản (Redis).
2. **Giữ ứng dụng ở tập lệnh chung** nếu muốn đổi được engine; lệnh riêng là khoá chặt vào một bên.
3. **Kiểm bằng lệnh** (`INFO server`, `COMMAND INFO`, `MODULE LIST`, tiêu đề RDB), không dựa vào bài viết.
4. **Không đổi engine bằng cách copy tệp dữ liệu** từ Redis ≥ 7.4 hay từ Valkey; di chuyển ở tầng ứng dụng.
5. **Với dự án này:** Valkey (chính sách `dependency-policy.md`); Redis 8 chỉ khi có lý do (mô-đun) và người dùng duyệt giấy phép cùng ADR.

## Nguồn

- Lab: [Redis Part 1 đến 6](redis/README.md) (Lab 1.1, 2.1 đến 2.3, 3.1 đến 3.3, 4.1, 5.1, Dự án 6.1).
- Mở: redis.io `/legal/licenses/`, hub.docker.com/_/redis, Linux Foundation (Valkey 28/03/2024), AWS Database Blog (08/10/2024), AWS ElastiCache FAQ, Azure `azure/redis/overview` (28/05/2026), valkey.io `/topics/migration/`, release notes Redis Open Source 8.0 đến 8.10.
- ⚠: valkey-json, valkey-bloom, valkey-search; Google Memorystore for Valkey; danh sách phiên bản Redis OSS trên ElastiCache.
