# Part 2 — Core Skills (Redis)

> [!NOTE]
> **Mục tiêu:** biết lệnh nào có ở Redis, ở Valkey, hay ở cả hai: hash field expiration, `SET IFEQ`, `DELEX`, `HGETEX`; kiểm bằng `COMMAND INFO` thay vì đoán; và xác nhận client .NET chạy được trên cả hai.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md), [Valkey Part 2](../valkey/part-2-core-skills.md).
> **Thời lượng:** ~4 giờ · **Đầu ra kiểm được:** kiểm một lệnh có tồn tại trên server đang chạy, chọn cách "xoá khoá nếu đúng chủ" phù hợp từng server, giải thích vì sao đếm số lệnh giữa Redis và Valkey không so sánh được.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Lệnh riêng và lệnh khác nhau

#### Hash field expiration
**Là gì:** đặt thời hạn cho **từng field** của một hash (`HEXPIRE`, `HTTL`, `HPERSIST`), không chỉ cho cả key. Redis có từ 7.4; Redis 8.0 thêm `HSETEX`, `HGETEX`, `HGETDEL`.
**Use case:** một hash `session:u1` chứa nhiều token con, mỗi token hết hạn riêng.

#### `SET ... IFEQ`
**Là gì:** ghi khoá **chỉ khi giá trị hiện tại bằng** giá trị mong đợi (compare-and-set).
**Use case:** gia hạn khoá chỉ khi mình vẫn là chủ.

#### `DELEX` (Redis) và `DELIFEQ` (Valkey)
**Là gì:** xoá khoá **chỉ khi giá trị đúng** (compare-and-delete): thay cho script Lua mở khoá của Part 2 Valkey. Redis đặt tên `DELEX key IFEQ giá-trị`; Valkey có lệnh `DELIFEQ`.
**Use case:** mở khoá phân tán an toàn không cần Lua.

#### `COMMAND INFO` và `COMMAND LIST`
Hỏi chính server xem nó có lệnh nào; đây là cách kiểm khẳng định "lệnh X có ở Redis/Valkey" thay vì tin bài viết.

## L2 Why — Vì sao không được đoán

Từ 7.2.4 hai bên rẽ đôi: cùng một chức năng có khi có ở cả hai nhưng tên khác (`DELEX` và `DELIFEQ`), có khi chỉ có một bên (`XDELEX` chỉ ở Redis, `COMMANDLOG` chỉ ở Valkey). Viết mã dựa trên lệnh không có ở server thật là lỗi chạy lúc production (Lab 2.2).

## L3 How — Cơ chế đáng nhớ

### Hash field expiration
`HEXPIRE key giây FIELDS số-field field...` đặt hạn; `HTTL key FIELDS n field...` đọc; `HPERSIST` bỏ hạn. Redis 7.4: `HEXPIRE`, `HPEXPIRE`, `HEXPIREAT`, `HPEXPIREAT`, `HEXPIRETIME`, `HPEXPIRETIME`, `HTTL`, `HPTTL`, `HPERSIST`; Redis 8.0: `HSETEX`, `HGETEX`, `HGETDEL`. Valkey có hash field expiration từ 9.0 (thông cáo của Linux Foundation) và Lab 2.2 cho thấy Valkey 9.1.2 có cả `HSETEX`, `HGETEX`, `HGETDEL`.

### Compare-and-set, compare-and-delete
- Redis 8.4 thêm `DIGEST`, `DELEX` và các tuỳ chọn `SET IF*` (release notes); Lab 2.2 xác nhận `SET ... IFEQ` chạy ở Redis 8.10.2 và Valkey 9.1.2 nhưng **không** ở Redis 7.2.16 (`ERR syntax error`).
- Valkey 8.1 thêm `SET IFEQ`; Valkey có `DELIFEQ` (Lab 2.2: lệnh tồn tại; **chưa chạy** thử).
- Cách an toàn cho cả hai: Lua so chủ rồi xoá ([Valkey Part 2, Lab 2.1](../valkey/part-2-core-skills.md)), vì chạy được ở mọi phiên bản.

### Tương thích client
Client Redis hiện có chạy với cả hai server (Lab 2.3): `StackExchange.Redis` cho cùng kết quả lab khoá, rate limit, `IDistributedCache`, `HybridCache`, pub/sub trên Redis 8.10.2 và Valkey 9.1.2. Valkey còn có client riêng (Valkey GLIDE). Chú ý: tính năng mới của Redis 8 (mô-đun, `DELEX`) cần client biết gọi lệnh đó (thường qua `Execute` hoặc thư viện hỗ trợ).

### Số lệnh không so sánh được
`COMMAND COUNT`: Redis 8.10.2 là 447, Redis 7.2.16 là 241, Valkey 9.1.2 là 257. Con số 447 **đã gồm** lệnh của các mô-đun hợp nhất và hàng chục lệnh nội bộ có tiền tố `_FT.` (Lab 2.1), nên không có nghĩa "Redis nhiều tính năng gấp đôi".

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0. Ba container `lh-r8`, `lh-r72`, `lh-vk`, đã dọn sau lab.

#### Lab 2.1 — Số lệnh và tập lệnh
**Đầu ra thật:**
```text
COMMAND COUNT:  Redis 8.10.2 = 447 | Redis 7.2.16 = 241 | Valkey 9.1.2 = 257
Chỉ có ở Redis 8.10.2 so với Valkey 9.1.2: 284 mục (gồm FT.*, _FT.* nội bộ, VADD..., JSON.*, TS.*, BF.*, CF.*, CMS.*, TDIGEST.*, TOPK.*,
   delex, digest, xdelex, xackdel, increx, himport, hotkeys, backup, lmovem, blmovem, sdiffcard, sunioncard, arset/arget (nhóm ar*), cluster|migration, trimslots...)
Chỉ có ở Valkey 9.1.2 so với Redis 8.10.2: 14 mục (client|import-source, clusterscan, cluster|migrateslots, cluster|cancelslotmigrations,
   cluster|getslotmigrations, cluster|flushslot, commandlog (+ lệnh con), delifeq, script|show, client|capa)
Chỉ có ở Redis 7.2.16 so với Valkey 9.1.2: 0 mục; ngược lại Valkey có thêm 29 mục
```
**Giải thích:** tập lệnh của Valkey 9.1.2 là **tập con rộng hơn** Redis 7.2.16 (không thiếu lệnh nào của 7.2.16, thêm 29 lệnh), còn Redis 8.10.2 có **284** mục mà Valkey không có, phần lớn là mô-đun hợp nhất và lệnh nội bộ. Hai bên đều có lệnh bên kia không có. Các tên viết hoa hay thường (`FT.CREATE` và `delex`) chỉ là cách mỗi server báo.

#### Lab 2.2 — Một lệnh có tồn tại không (`COMMAND INFO`)
**Đầu ra thật** (Redis 8.10.2 | Redis 7.2.16 | Valkey 9.1.2):
```text
HEXPIRE, HTTL, HPERSIST, HGETEX, HSETEX, HGETDEL, MSETEX:   có | không | có
DELEX, DIGEST, XDELEX, XACKDEL:                              có | không | không
DELIFEQ, COMMANDLOG:                                         không | không | có
JSON.SET, TS.ADD, BF.ADD, VADD, FT.CREATE, TOPK.ADD, CMS.INITBYDIM, TDIGEST.CREATE:   có | không | không
SET k v2 IFEQ v:                                             OK | ERR syntax error | OK
HEXPIRE h 100 FIELDS 1 f rồi HTTL:                           1, 100 | lỗi "unknown command" | 1, 100
```
**Giải thích:** Redis 7.2.16 không có hash field expiration (vì 7.4 mới thêm); Valkey 9.1.2 có hash field expiration, `HSETEX`, `HGETEX`, `HGETDEL`, `MSETEX`, `SET IFEQ` giống Redis 8, **nhưng không có** `DELEX`, `XDELEX`, `XACKDEL` và các mô-đun mặc định. Điều này sửa một giả định hay gặp: "Valkey thiếu mọi lệnh mới của Redis" là **sai** với hash field expiration và `SET IFEQ` ở các phiên bản này.

#### Lab 2.3 — Client .NET trên cả hai server
`StackExchange.Redis` 3.4.20 chạy bộ lab khoá `SET NX PX`, rate limit Lua, `IDistributedCache`, `HybridCache` hai instance, pub/sub (cùng chương trình đã dùng cho Valkey), nay trỏ vào Redis 8.10.2.
**Đầu ra thật** (rút gọn; so với Valkey Part 3 và Part 4 thì **giống nhau**):
```text
# server: 8.10.2, StackExchange.Redis 3.0.0.0
khoá: A lấy True, B bị từ chối False; sau 600 ms B lấy được True; A mở khoá bằng Lua xoá 0 key, B xoá 1 key
rate limit: 8 yêu cầu liên tiếp -> cho cho cho cho cho chặn chặn chặn; 10 yêu cầu qua ranh giới cửa sổ cố định
IDistributedCache: key teamboard:board:1, kiểu Hash, field absexp/sldexp/data, TTL 30 giây
HybridCache: B đọc từ L2 (chạm DB 1); sau khi A xoá, B vẫn trả v1 từ L1; sau 1200 ms trả v2; 40 yêu cầu trên 2 instance chạm DB 2 lần
pub/sub: publish lần 1 trả 0 người nhận, lần 2 trả 1
```
**Giải thích:** lớp truy cập của ứng dụng .NET không thay đổi khi đổi giữa Redis 8.10.2 và Valkey 9.1.2 (nhãn "Valkey" trong chuỗi in của chương trình là chữ cũ của lab). Số "gói 3.0.0.0" là phiên bản assembly; gói NuGet là 3.4.20.

## L5 Troubleshooting — Khi lệnh không như mong đợi

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| `unknown command 'HEXPIRE'` | server là Redis 7.2 (hoặc Valkey < 9.0) | `INFO server`, `COMMAND INFO HEXPIRE` (Lab 2.2) | nâng phiên bản hoặc bỏ tính năng |
| `ERR syntax error` ở `SET ... IFEQ` | server cũ (Redis 7.2) | thử tuỳ chọn trên đúng server | dùng Lua so chủ |
| `DELEX` báo không tồn tại trên Valkey | chỉ có ở Redis; Valkey dùng `DELIFEQ` | `COMMAND INFO DELEX` và `DELIFEQ` | chọn lệnh theo server, hoặc dùng Lua chạy mọi nơi |
| Mã kiểm phiên bản sai trên Valkey | chỉ đọc `redis_version` (7.2.4) | đọc `server_name` và `valkey_version` (Lab 1.1) | kiểm cả hai trường |
| `JSON.SET` báo không có lệnh | image Valkey mặc định hoặc Redis 7.2 không kèm mô-đun | `MODULE LIST` | dùng Redis 8 hoặc mô-đun valkey-json (⚠ chưa chạy) |
| Dùng "số lệnh" để so tính năng | `COMMAND COUNT` gồm lệnh nội bộ và mô-đun (Lab 2.1) | xem danh sách | so từng lệnh cần dùng bằng `COMMAND INFO` |
| Ứng dụng chạy ở Redis nhưng hỏng ở Valkey | dùng lệnh chỉ có ở Redis 8 | `COMMAND INFO` trên Valkey | kiểm tính tương thích trước khi đổi engine |

## Tự kiểm tra

1. Cách nào kiểm một lệnh có tồn tại trên server đang chạy mà không tra tài liệu?
2. `DELEX` và `DELIFEQ` khác nhau ở đâu, và cách xoá khoá-đúng-chủ nào chạy mọi nơi?
3. Valkey 9.1.2 có `HSETEX` không? Redis 7.2.16 thì sao?
4. Vì sao 447 so với 257 không nói Redis 8 "nhiều tính năng gấp đôi"?

<details>
<summary>Đáp án ngắn</summary>

1. `COMMAND INFO <lệnh>` (rỗng nếu không có) hoặc `COMMAND LIST` (Lab 2.1, 2.2).
2. `DELEX` chỉ ở Redis; `DELIFEQ` chỉ ở Valkey; Lua so chủ rồi xoá chạy ở mọi phiên bản.
3. Valkey 9.1.2 có; Redis 7.2.16 không (Lab 2.2).
4. 447 gồm lệnh mô-đun và lệnh nội bộ (`_FT.*`); đó không phải tính năng độc lập.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.3 (Redis 8.10.2, Redis 7.2.16, Valkey 9.1.2; đầu ra thật).
**Đã tự mở:** redis.io `/docs/latest/develop/data-types/hashes/` (lệnh 7.4 và 8.0), release notes Redis 8.0, 8.2, 8.4 và 8.10 (lệnh mới), Linux Foundation thông cáo Valkey 9.0 (hash field expiration).
**Chỉ qua agent (⚠):** Valkey 8.1 `SET IFEQ` (blog 8.1); các phiên bản Redis 8.6 và 8.8 (chưa mở release notes).
**Chưa kiểm:** `DELIFEQ` của Valkey chưa chạy; hành vi hash field expiration trên replica; các phiên bản Redis 8.0 đến 8.8 riêng lẻ (chỉ chạy 8.10.2); client khác `StackExchange.Redis` (redis-py, jedis, Valkey GLIDE).
