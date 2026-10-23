# Part 5 — Production, Security, Operations (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** quan sát Valkey bằng chỉ số đúng, khoá truy cập bằng ACL, sao lưu và khôi phục, chọn dịch vụ quản lý, và ước tính chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** đọc `INFO` để tính hit ratio, tạo user ACL hạn chế, nêu danh sách kiểm trước khi bật Valkey ở production.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

## L1 What — Valkey khi đã là một phần của hệ thống thật

#### Quan sát
**Là gì:** `INFO`, `SLOWLOG`, `LATENCY` và chỉ số CloudWatch cho biết server khoẻ hay đang bị đẩy tới giới hạn.

#### ACL, TLS
**ACL:** user có quyền riêng (lệnh, nhóm lệnh, mẫu key). **TLS:** mã hoá đường truyền (kể cả replication và cluster bus).

#### Sao lưu và khôi phục
RDB/AOF là phương tiện sao lưu; khôi phục là nạp lại file đó vào server.

#### Dịch vụ quản lý
ElastiCache (cache, dữ liệu dựng lại được) và MemoryDB (Valkey/Redis tương thích, bền, có thể làm kho chính) trên AWS.

## L2 Why — Vì sao cần phần này

Valkey được thiết kế cho **client đáng tin trong mạng đáng tin**: người dùng mặc định không cần mật khẩu, một lệnh `FLUSHALL` xoá mọi thứ. Mở ra Internet hoặc cấp quyền rộng là rủi ro chết người (Part 7). Quan sát sai chỉ số (chỉ nhìn "còn sống") bỏ lỡ eviction, độ trễ, và nguồn bị dội tải khi hit ratio tụt.

## L3 How — Cơ chế đáng nhớ

### Chỉ số cần theo dõi

| Chỉ số | Nguồn | Ý nghĩa | Dấu hiệu xấu |
|---|---|---|---|
| `keyspace_hits`, `keyspace_misses` | `INFO stats` | tính hit ratio | hit ratio tụt đột ngột |
| `evicted_keys` | `INFO stats` | số key bị đẩy ra | tăng liên tục: RAM nhỏ hơn tập nóng |
| `expired_keys` | `INFO stats` | số key hết hạn | tăng vọt: TTL đồng loạt |
| `used_memory` so với `maxmemory` | `INFO memory` | mức đầy | gần giới hạn kéo dài |
| `SLOWLOG` | `SLOWLOG GET` | lệnh chạy lâu | `KEYS`, `DEL` big key, Lua dài |
| `connected_slaves`, `master_link_status`, offset | `INFO replication` | tình trạng sao chép | `link down`, chênh offset lớn |
| `total_commands_processed` | `INFO stats` | thông lượng | thay đổi bất thường |

Tên chỉ số trên AWS (CloudWatch) khác với `INFO`; tra tài liệu dịch vụ ⚠.

### Bảo mật

#### Mô hình tin cậy
Tài liệu bảo mật của Valkey: thiết kế cho client đáng tin trong môi trường đáng tin; **không mở ra Internet**; chặn cổng bằng tường lửa; `bind 127.0.0.1` hoặc giao diện cụ thể. **Protected mode** (không mật khẩu): chỉ nhận loopback và trả lời giải thích cho client khác.

#### ACL
Người dùng `default` có `on nopass ~* &* +@all`: ai kết nối cũng thành `default`, toàn quyền. Tạo user ứng dụng với quyền tối thiểu:
`ACL SETUSER app on >mật-khẩu ~board:* +get +set` (Lab 5.2). Mật khẩu lưu dạng băm SHA-256; `ACL GENPASS` sinh mật khẩu mạnh; user định nghĩa trong `valkey.conf` **hoặc** file ACL, không cả hai; `CONFIG REWRITE` không lưu ACL. `requirepass` là cơ chế cũ một mật khẩu chung.

#### TLS
Mật khẩu `AUTH` đi bản rõ nếu không có TLS. Valkey hỗ trợ TLS cho client, replication và cluster bus ⚠ (chưa cấu hình trong lab).

#### Nguyên tắc
Valkey trong mạng riêng; ACL tối thiểu quyền cho từng ứng dụng; không đặt secret hay token ở dạng rõ trong cache dùng chung; mật khẩu lấy từ secret manager, không vào repo hay log.

### Sao lưu và khôi phục
- Sao lưu: sao chép file RDB hoặc thư mục `appendonlydir`; file RDB gọn và hợp để sao lưu định kỳ.
- Khôi phục: đặt file vào thư mục dữ liệu, khởi động server; nếu có AOF thì server dùng AOF.
- **Kiểm tra khôi phục định kỳ**: bản sao lưu chưa thử khôi phục chưa phải sao lưu.
- AOF bị cắt cụt ở đuôi được nạp và bỏ lệnh cuối hỏng (`aof-load-truncated yes` mặc định); hỏng giữa file thì dừng khởi động, dùng `valkey-check-aof` (thử không `--fix` trước).

### Dịch vụ quản lý trên AWS

| Dịch vụ | Mục đích | Bền | Chọn khi |
|---|---|---|---|
| ElastiCache | cache, kho tạm | dữ liệu có thể dựng lại | cache, phiên, bộ đếm |
| MemoryDB | Valkey/Redis tương thích, **bền** (nhật ký giao dịch Multi-AZ) | có | cần làm kho chính |
| Tự chạy trên EC2/ECS | kiểm soát tối đa | tuỳ bạn | cần cấu hình riêng, chấp nhận vận hành |

Chính sách dự án: dùng Valkey; **ElastiCache chưa cần ở tải thấp** và thuộc nhóm chạy rỗng 24/7 (`aws-service-catalog.md`, `cost-model.md`); chọn qua skill `aws-service-selection` trước khi viết Terraform.

### Chi phí
Giá niêm yết đã tra là **số lấy từ ví dụ trên trang giá**, chưa phải bảng giá gốc ⚠. Phép tính một tháng 730 giờ:

| Phương án | Giá (⚠) | Ước tính một tháng |
|---|---|---|
| ElastiCache Valkey `cache.r7g.large`, US East | $0,1752/giờ | $127,9 |
| ElastiCache Serverless Valkey, tối thiểu 100 MB lưu trữ | $0,084/GB-giờ | $6,13 (chưa gồm ECPU) |

AWS cho biết Valkey rẻ hơn Redis OSS: node khoảng 20%, serverless khoảng 33%, MemoryDB khoảng 30% (số của AWS, tiếp thị); serverless có mức tối thiểu 100 MB thay vì 1 GB. Luôn so với chi phí **tăng cỡ nguồn** thay thế và kiểm Pricing Calculator.

## L4 Hands-on — Lab Part 5

#### Lab 5.1 — Đọc `INFO` và tính hit ratio
`CONFIG RESETSTAT`, rồi `SET k1`, hai `GET k1`, một `GET nokey`.
```text
total_commands_processed:5     expired_keys:0     evicted_keys:0
keyspace_hits:2     keyspace_misses:1
> INFO keyspace → db0:keys=1,expires=0,avg_ttl=0,keys_with_volatile_items=0
```
**Giải thích:** hit ratio = 2 ÷ (2 + 1) = 66,7%. `INFO keyspace` cho số key và số key có hạn mỗi database.

#### Lab 5.2 — ACL với quyền tối thiểu
User `app` chỉ đọc/ghi `board:*` và chỉ lệnh `GET`, `SET`:
```text
> ACL SETUSER app on >(mật khẩu lab) ~board:* +get +set → OK
SET board:1 v           → OK            GET board:1 → v
GET other:1             → NOPERM No permissions to access a key
FLUSHALL                → NOPERM User app has no permissions to run the 'flushall' command
(mật khẩu sai)          → AUTH failed: WRONGPASS invalid username-password pair or user is disabled.
> ACL GETUSER default   → on, nopass, +@all
```
**Giải thích:** `app` bị chặn ở key ngoài mẫu và lệnh nguy hiểm. Sau `AUTH` thất bại, lệnh `GET board:1` trong cùng lần gọi `valkey-cli` **vẫn chạy được** (trả `v`), vì kết nối rơi về user `default` không mật khẩu. **Bài học:** `nopass` ở `default` làm mọi lớp ACL của user khác vô nghĩa nếu cổng còn mở; phải đặt mật khẩu hoặc tắt `default`.

#### Lab 5.3 — Các giá trị mặc định đã quan sát
```text
CONFIG GET save → 3600 1 300 100 60 10000        CONFIG GET lazyfree-lazy-user-del → yes
```
Các giá trị khác ở Part 3 và 4 (`appendfsync`, eviction) lấy từ tài liệu ⚠.

## L5 Troubleshooting — Sự cố vận hành

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Ai cũng kết nối và `FLUSHALL` được | `default` nopass, cổng mở | `ACL GETUSER default`, `bind`, `protected-mode` | mạng riêng, đặt mật khẩu, ACL, bỏ lệnh nguy hiểm |
| Hit ratio tụt, DB bị dội tải | eviction, đổi key, restart rỗng | `evicted_keys`, thời điểm triển khai | tăng RAM, giữ key ổn định, làm nóng |
| Dữ liệu không khôi phục được | chưa bao giờ thử khôi phục, AOF hỏng | thử khôi phục ở môi trường thử | kiểm khôi phục định kỳ; `valkey-check-aof` |
| Độ trễ tăng đột ngột | lệnh chậm, fork cho RDB/rewrite, swap | `SLOWLOG`, `LATENCY`, RAM máy | tránh lệnh chậm, dư RAM, không để swap |
| Replica lệch xa | mạng, primary quá tải | `INFO replication` offset | kiểm mạng và tải, backlog đủ lớn |
| Hoá đơn cao bất thường | node quá lớn, chạy 24/7, môi trường dev bật suốt | loại node, giờ chạy | giảm cỡ, tắt môi trường không dùng, serverless |
| Lộ mật khẩu Valkey | để trong chuỗi kết nối trong repo hoặc log | tìm trong repo và log | secret manager, xoay mật khẩu, file `*.example` |

## L6 Production — Danh sách kiểm trước khi bật

- Valkey nằm trong mạng riêng, không cổng ra Internet; `bind` đúng; không dùng `default` nopass.
- Mỗi ứng dụng một user ACL quyền tối thiểu; mật khẩu từ secret manager; TLS khi qua mạng.
- Có dashboard hit ratio, `evicted_keys`, bộ nhớ, `SLOWLOG`, trạng thái replication, và **tải nguồn**.
- `maxmemory` và chính sách đã đặt; dư RAM cho fork, bộ đệm replication và AOF.
- Persistence khớp mức mất dữ liệu chấp nhận được; sao lưu có lịch và **đã thử khôi phục**.
- Ứng dụng có timeout ngắn và đường lui khi Valkey lỗi ([Caching Part 5](../caching/part-5-production-security-ops.md)).
- Phiên bản và giấy phép đã được duyệt (Valkey; Redis cần ADR); image ghim tag, không `latest`.
- Chi phí đã ước tính và so với phương án thay thế; môi trường dev tắt khi không dùng.
- Có runbook: restart, khôi phục, đổi mật khẩu, xoá cache, promote replica.

## L7 Architecture — Tự chạy hay dịch vụ quản lý

| Phương án | Khi chọn | Cái giá |
|---|---|---|
| Không dùng | tải thấp, một instance | không chia sẻ |
| Valkey tự chạy (container/VM) | học, cấu hình riêng, tiết kiệm ở quy mô nhỏ | tự lo vá lỗi, sao lưu, HA, bảo mật |
| ElastiCache (node) | cache ổn định, quy mô vừa | chạy 24/7, phải chọn cỡ node |
| ElastiCache Serverless | tải thay đổi, bắt đầu nhỏ | phí theo mức dùng, khó đoán nếu tải cao |
| MemoryDB | cần Valkey làm kho chính, bền | đắt hơn cache; ít lý do nếu chỉ cần cache |
| Dịch vụ khác (DB, broker) | dữ liệu quan trọng, hàng đợi bền | thêm hạ tầng |

Câu hỏi chọn: *nếu Valkey mất toàn bộ dữ liệu, hệ thống có còn đúng không?* Nếu không, đó không phải cache và cần mức bền khác.

## Tự kiểm tra

1. Hit ratio trong Lab 5.1 là bao nhiêu và tính thế nào?
2. Vì sao user `default` nopass phá giá trị của ACL cho user khác?
3. Nêu ba chỉ số của Valkey và dấu hiệu xấu.
4. Khi nào MemoryDB, khi nào ElastiCache?

<details>
<summary>Đáp án ngắn</summary>

1. 2 ÷ (2 + 1) ≈ 66,7% từ `keyspace_hits` và `keyspace_misses`.
2. Kết nối không xác thực (hoặc xác thực lỗi) rơi về `default` toàn quyền (Lab 5.2).
3. Ví dụ `evicted_keys` tăng liên tục, `used_memory` gần `maxmemory`, `SLOWLOG` có lệnh lâu.
4. MemoryDB khi cần kho chính bền; ElastiCache khi chỉ cần cache dựng lại được.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1 đến 5.3 (đầu ra thật).
**Đã tự mở:** aws.amazon.com/elasticache, /memorydb (định vị dịch vụ), blog AWS Database 08/10/2024 (hỗ trợ Valkey, các mức rẻ hơn); trang giá ElastiCache và MemoryDB đã mở nhưng bảng giá không đọc được đầy đủ, các giá chỉ lấy từ ví dụ trên trang.
**Chỉ qua agent (⚠):** valkey.io `/topics/security/`, `/topics/acl/`, `/topics/persistence/`; chỉ số CloudWatch; TLS.
**Chưa kiểm:** giá niêm yết hiện hành (cần Pricing Calculator); TLS thật; hành vi trên ElastiCache và MemoryDB thật.
