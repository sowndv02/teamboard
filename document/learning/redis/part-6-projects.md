# Part 6 — Projects (Redis)

> [!NOTE]
> **Mục tiêu:** đưa ra quyết định có bằng chứng cho TeamBoard: vì sao chính sách chọn Valkey, và nếu một ngày phải chuyển giữa Redis và Valkey thì dữ liệu và lệnh nào di chuyển được; không sửa `src/`, `config/` hay chính sách.
> **Cần biết trước:** [Part 5](part-5-production-security-ops.md), [Valkey Part 6](../valkey/part-6-projects.md).
> **Thời lượng:** ~4 giờ · **Đầu ra kiểm được:** bảng "nạp được hay không" giữa các phiên bản, danh sách kiểm khi chuyển engine, và đề xuất kèm điều kiện đổi quyết định.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)

## L1 What — Bối cảnh dự án

TeamBoard hiện **không dùng** Redis hay Valkey trong `src/` hay compose. Chính sách: Redis server `banned` (T3), Valkey `watch` (T1); `dependency-policy.md` ghi "dùng Valkey hoặc không cache khi chưa cần" và cấm AGPL theo mặc định, kèm ghi chú Redis 8 thêm AGPLv3 nên **không tự động hợp lệ** (cần người dùng duyệt và ADR). Lộ trình này **không** thay đổi chính sách; các dự án dưới đây chỉ cung cấp bằng chứng để người dùng quyết.

## L4 Hands-on — Ba dự án

Môi trường: như Part 0. Container `lh-*` dựng từ image đã kéo, dọn sau mỗi lần thử.

#### Dự án 6.1 — Tệp RDB di chuyển được giữa các engine không?
Tạo cùng một tập dữ liệu (string, hash, list, set, sorted set, 5 key) trên Redis 7.2.16, Redis 8.10.2 và Valkey 9.1.2, `SAVE`, đọc tiêu đề tệp, rồi nạp tệp vào server khác (copy `dump.rdb` vào `/data` trước khi khởi động).
**Đầu ra thật:**
```text
tiêu đề tệp: Redis 7.2 REDIS0011 | Redis 8.10.2 REDIS0015 | Valkey 9.1.2 VALKEY080
Redis 7.2 (REDIS0011)  -> Valkey 9.1.2:   nạp được, DBSIZE = 5
Redis 7.2 (REDIS0011)  -> Redis 8.10.2:   nạp được, DBSIZE = 5
Redis 8.10.2 (REDIS0015, chỉ kiểu cơ bản) -> Valkey 9.1.2:  LỖI "Can't handle RDB format version 15", server thoát mã 1
Redis 8.10.2 (có hash field expiration)   -> Valkey 9.1.2:  cùng lỗi
Redis 8.10.2 (có khoá JSON)               -> Valkey 9.1.2:  cùng lỗi
Redis 8.10.2 (REDIS0015) -> Redis 7.2.16:  LỖI "Can't handle RDB format version 15"
Valkey 9.1.2 (VALKEY080) -> Redis 8.10.2:  LỖI "Fatal error loading the DB"
Valkey 9.1.2 (VALKEY080) -> Redis 7.2.16:  LỖI "Fatal error loading the DB"
```
**Giải thích:** khớp với trang migration của Valkey ("RDB từ Redis CE 7.4 trở lên không tương thích"), nhưng lab cho thấy điều chặt hơn: **cả tệp chỉ chứa kiểu cơ bản** từ Redis 8.10.2 cũng không nạp được bằng Valkey 9.1.2 (định dạng RDB phiên bản 15), và **Valkey 9.1.2 ghi tệp với tiêu đề `VALKEY080`** mà Redis (cả 8.10.2 lẫn 7.2.16) không đọc được. Chỉ **Redis 7.2 → Valkey** (và Redis 7.2 → Redis 8) nạp được. Hệ quả: di chuyển sau khi đã dùng Redis ≥ 7.4 hay sau khi ghi bằng Valkey phải đi bằng **tầng ứng dụng** (quét key và ghi lại, hoặc `DUMP`/`RESTORE` có kiểm chứng, ⚠ chưa thử).

#### Dự án 6.2 — Mã ứng dụng chạy được trên cả hai engine không?
Chạy bộ lab .NET (khoá, rate limit, `IDistributedCache`, `HybridCache`, pub/sub) lần lượt trên Valkey 9.1.2 và Redis 8.10.2 ([Redis Part 2, Lab 2.3](part-2-core-skills.md)): **kết quả giống nhau**. **Tiêu chí đạt:** liệt kê những lệnh của mã dự án (nếu một ngày có) và kiểm từng lệnh bằng `COMMAND INFO` trên engine đích (Lab 2.2). Quy tắc rút ra: giữ ứng dụng ở **tập lệnh chung** (string, hash, list, set, sorted set, stream, `SET NX PX`, Lua, pub/sub) thì đổi engine không cần sửa mã; dùng lệnh riêng (`JSON.*`, `DELEX`, `FT.*`, `VADD`...) là tự khoá vào một bên.

#### Dự án 6.3 — Quyết định cho TeamBoard (bài tập, có điều kiện đổi)
Viết đề xuất một trang cho hai câu hỏi: (1) **hiện tại** dùng gì (không cache, `IMemoryCache`/`HybridCache`, Valkey, Redis); (2) **khi nào** xem lại. Dùng số đo của chính bạn từ [Caching Dự án 6.1](../caching/part-6-projects.md) và [Valkey Dự án 6.2](../valkey/part-6-projects.md). **Tiêu chí đạt:** nêu giấy phép, tính năng cần (có cần JSON hay vector không), chi phí, phụ thuộc nhà cung cấp, và **điều kiện đổi**: ví dụ "chuyển sang Redis chỉ nếu cần mô-đun mà Valkey chưa có và người dùng duyệt AGPL hoặc dịch vụ quản lý của Redis"; "xem lại Valkey nếu cloud đang dùng ngừng hỗ trợ".

#### Danh sách kiểm khi chuyển engine (rút từ các lab)
| Mục | Kiểm thế nào | Kết quả ở lab |
|---|---|---|
| Phiên bản nguồn | `INFO server`; đọc `server_name` và `valkey_version` | Valkey báo `redis_version:7.2.4` (Part 1) |
| Định dạng dữ liệu | tiêu đề RDB (`head -c 9`) | 7.2 → Valkey nạp được; 8.x và `VALKEY080` thì không (Dự án 6.1) |
| Lệnh đang dùng | `COMMAND INFO` từng lệnh trên đích | khác nhau ở `DELEX`, mô-đun, `DELIFEQ` (Lab 2.2) |
| Mô-đun | `MODULE LIST` | chỉ Redis 8 có sẵn (Lab 3.1) |
| ACL | `ACL CAT`, `ACL GETUSER` | danh mục khác nhau (Lab 3.3) |
| Client | chạy bộ test với cả hai | giống nhau (Lab 2.3) |
| Cluster, replication | thử di chuyển slot ở môi trường thử | tên lệnh khác nhau (Lab 4.2); chưa chạy |

## L5 Troubleshooting — Khi chuyển engine không thuận

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Server không khởi động, log "Can't handle RDB format version 15" | nạp RDB Redis 8 vào Valkey hoặc Redis 7.2 (Dự án 6.1) | `head -c 9 dump.rdb`, log | di chuyển ở tầng ứng dụng; không copy file |
| "Fatal error loading the DB" với tệp từ Valkey | tiêu đề `VALKEY080` Redis không đọc | `head -c 9 dump.rdb` | không đổi từ Valkey sang Redis bằng file |
| Ứng dụng báo `unknown command` sau khi chuyển | lệnh riêng của một bên | `COMMAND INFO` trên đích | thay bằng tập lệnh chung hoặc Lua |
| Mất dữ liệu mô-đun | dữ liệu mô-đun không đi theo RDB sang Valkey | `TYPE` của key; thử nạp | xuất ở tầng ứng dụng |
| Mã kiểm phiên bản sai | chỉ đọc `redis_version` | `INFO server` | kiểm `server_name` |
| ACL khác sau chuyển | danh mục khác nhau | `ACL CAT` | viết lại ACL cho engine đích |
| Công cụ vận hành không chạy | lệnh cluster khác tên | `COMMAND INFO` | runbook riêng |

## Tự kiểm tra

1. Tệp RDB nào nạp được vào Valkey 9.1.2 và tệp nào không?
2. Vì sao dữ liệu chỉ có kiểu cơ bản từ Redis 8.10.2 vẫn không nạp được vào Valkey?
3. Làm sao giữ ứng dụng chuyển được giữa hai engine?
4. Hãy nêu một điều kiện hợp lý để xem lại quyết định dùng Valkey ở TeamBoard.

<details>
<summary>Đáp án ngắn</summary>

1. Redis 7.2 (REDIS0011) nạp được; Redis 8.10.2 (REDIS0015) thì không (Dự án 6.1).
2. Định dạng RDB phiên bản 15 của Redis 8 mà Valkey 9.1.2 không đọc, bất kể nội dung.
3. Giữ ở tập lệnh chung; kiểm từng lệnh bằng `COMMAND INFO`; tránh lệnh riêng như `JSON.*`, `DELEX`.
4. Ví dụ: cần mô-đun mà Valkey chưa có và giấy phép của Redis được duyệt; hoặc cloud đang dùng ngừng hỗ trợ Valkey.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Dự án 6.1 (Redis 7.2.16, Redis 8.10.2, Valkey 9.1.2; nạp RDB thật; đầu ra thật) và 6.2 (tham chiếu Lab 2.3 đã chạy).
**Đã tự mở:** valkey.io `/topics/migration/` (RDB từ Redis CE 7.4 trở lên không tương thích; Valkey tương thích Redis OSS 7.2) qua agent; `dependency-policy.md` của dự án.
**Chưa kiểm:** `DUMP`/`RESTORE` và di chuyển ở tầng ứng dụng giữa hai engine; sao lưu AOF giữa hai engine (chỉ thử RDB); Dự án 6.3 là bài tập; lab chỉ trên hai phiên bản Redis (7.2.16 và 8.10.2) và một phiên bản Valkey (9.1.2).
