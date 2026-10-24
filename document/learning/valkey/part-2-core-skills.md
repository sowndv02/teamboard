# Part 2 — Core Skills (Redis / Valkey)

> [!NOTE]
> **Mục tiêu:** dùng Valkey đúng từ lệnh và từ ứng dụng .NET: khoá, bộ đếm, duyệt key an toàn, pipeline, giao dịch, Lua, `IDistributedCache`.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** lấy và mở khoá đúng chủ, duyệt key bằng `SCAN`, giải thích vì sao `MULTI` không rollback, đăng ký `IDistributedCache` và đọc key thật trong Valkey.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Bộ công cụ hằng ngày

#### `SET ... NX EX/PX`
**Là gì:** ghi key **chỉ khi chưa tồn tại**, kèm hạn, trong một lệnh nguyên tử.
**Use case:** khoá ngắn ("chỉ một worker xử lý Board 1"), khoá chống chạy trùng (idempotency key), dựng cache một lần.

#### `SCAN`
**Là gì:** duyệt key theo từng trang bằng con trỏ, không chặn server lâu như `KEYS`.
**Use case:** liệt kê hoặc dọn key theo mẫu mà không làm khựng ứng dụng.

#### Pipeline
**Là gì:** gửi nhiều lệnh liền nhau không chờ từng phản hồi.
**Use case:** nạp hàng loạt, giảm số chuyến mạng.

#### `MULTI` / `EXEC`
**Là gì:** gom lệnh vào hàng đợi rồi chạy liền nhau không bị lệnh khác xen vào.
**Use case:** vài thao tác phải xảy ra cùng nhau, không cần đọc kết quả giữa chừng.

#### Lua (`EVAL`)
**Là gì:** chạy một đoạn Lua **nguyên tử** trong server.
**Use case:** "kiểm tra rồi làm" không chen được (mở khoá đúng chủ, đếm rồi đặt hạn).

#### `IDistributedCache` và `StackExchange.Redis`
Giao diện .NET cho cache phân tán, cài đặt bằng `AddStackExchangeRedisCache`; thư viện `StackExchange.Redis` cho truy cập trực tiếp (khoá, Lua, pub/sub).

## L2 Why — Vì sao các công cụ này tồn tại

Hai client cùng đọc rồi cùng ghi sẽ ghi đè nhau; tách "kiểm tra" khỏi "làm" qua hai lệnh là nguồn lỗi tranh chấp. Server đơn luồng cho phép gom bước kiểm tra và thực hiện vào **một lệnh** (`SET NX`, `INCR`) hoặc **một script** (Lua). `SCAN` và pipeline tồn tại vì hai nỗi đau thật: lệnh duyệt toàn bộ chặn mọi client, và mỗi chuyến mạng tốn thời gian.

## L3 How — Cơ chế đáng nhớ

#### Khoá bằng `SET NX PX`
Giá trị khoá là **định danh duy nhất của chủ** (không phải `1`), hạn (`PX`) để khoá không kẹt nếu chủ chết. Mở khoá phải **so chủ rồi mới xoá**, bằng Lua:
```lua
if redis.call('get',KEYS[1])==ARGV[1] then return redis.call('del',KEYS[1]) else return 0 end
```
`DEL` trần có thể xoá khoá của người khác nếu khoá của bạn đã hết hạn. Khoá này an toàn cho việc "tránh làm trùng" (hiệu quả); với việc "bảo vệ tính đúng" cần thêm fencing token (Part 4).

#### `MULTI` không hoàn tác
Lỗi **cú pháp hoặc lệnh không biết** khiến cả giao dịch bị huỷ; lỗi **lúc chạy** (ví dụ `INCR` trên chuỗi) chỉ làm lệnh đó lỗi, các lệnh còn lại **vẫn chạy**. Lab 2.2 cho thấy.

#### `SCAN`
Mỗi lần gọi O(1); duyệt hết một vòng O(N). Bảo đảm: phần tử có mặt suốt vòng duyệt sẽ được trả; có thể **trả trùng**; phần tử thêm hoặc xoá giữa chừng có thể có hoặc không. `COUNT` chỉ là gợi ý; lặp đến khi con trỏ về 0; `MATCH` lọc sau khi lấy.

#### Pipeline và tự động pipeline của StackExchange.Redis
Mọi lời gọi đồng thời của một `ConnectionMultiplexer` dùng chung **một** kết nối và được tự gộp (auto-pipeline). Vì vậy: tạo **một** multiplexer dùng cho cả ứng dụng (singleton); không tạo mỗi thao tác. Các lệnh chặn như `BLPOP` không được cung cấp vì chúng sẽ chặn cả kết nối dùng chung.

#### Admin mode
`StackExchange.Redis` từ chối các lệnh nguy hiểm như `FLUSHALL` trừ khi cấu hình `allowAdmin=true` (Lab 2.5). Đó là một lớp bảo vệ chứ không phải lỗi.

#### Phạm vi key trong `IDistributedCache`
`AddStackExchangeRedisCache` thêm tiền tố `InstanceName`; mỗi mục lưu thành một **Hash** có các field `absexp`, `sldexp`, `data` (Lab 2.5). Đặt `InstanceName` theo ứng dụng và môi trường để nhiều ứng dụng dùng chung server không đè key nhau.

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0 (`lh-valkey`, Valkey 9.1.2). Lab .NET chạy trong thư mục thử ngoài repo với `StackExchange.Redis` 3.4.20.

#### Lab 2.1 — Khoá `SET NX` và mở khoá đúng chủ
**Đầu ra thật:**
```text
> SET lock:board:1 worker-A NX EX 30   → OK
> SET lock:board:1 worker-B NX EX 30   → (nil)        (B bị từ chối)
> GET lock:board:1 → worker-A          > TTL lock:board:1 → 29
> EVAL <unlock-lua> 1 lock:board:1 worker-B → 0       (B không xoá được khoá của A)
> EVAL <unlock-lua> 1 lock:board:1 worker-A → 1
```
**Giải thích:** chỉ một bên lấy được khoá; mở khoá bằng Lua từ chối chủ sai.

#### Lab 2.2 — `MULTI` không rollback
```text
MULTI / SET a 1 / INCR a / EXEC      → OK, QUEUED, QUEUED, rồi: OK, 2
SET s text ; MULTI ; INCR s ; SET ok yes ; EXEC ; GET ok
   → ... ERR value is not an integer or out of range, OK ; GET ok → yes
```
**Giải thích:** `INCR s` lỗi lúc chạy nhưng `SET ok yes` **vẫn được áp dụng**.

#### Lab 2.3 — `KEYS` và `SCAN` trên 200.000 key
Tạo 200.000 key `k:N` bằng một script Lua (`DBSIZE` = 200003).
```text
> KEYS k:99999*   → 1 kết quả; SLOWLOG ghi 4654 micro giây cho lệnh này
> SCAN 0 MATCH k:99999* COUNT 1000   → trả con trỏ 31872 và danh sách rỗng (còn phải lặp)
valkey-cli --scan --pattern k:99999*  → 1 kết quả sau khi lặp hết các trang
```
**Giải thích:** `KEYS` chạy một lần và giữ luồng ~4,6 ms cho 200.000 key (tỉ lệ thuận với số key; với hàng chục triệu key sẽ tính bằng giây); `SCAN` trả từng trang nhỏ, trang đầu có thể rỗng và vẫn phải lặp theo con trỏ.

#### Lab 2.4 — Pipeline
`valkey-cli --pipe` nạp 100.000 lệnh `SET`:
```text
errors: 0, replies: 100000
pipeline: 449 ms (gồm thời gian khởi động tiến trình `docker exec`)
```
**Giải thích:** 100.000 lệnh trong khoảng nửa giây trên một kết nối. Không đối chiếu được sạch với kiểu gọi từng lệnh vì phép đo gồm cả khởi động tiến trình; chỉ rút ra rằng pipeline gom được số lệnh rất lớn.

#### Lab 2.5 — `IDistributedCache` từ .NET
```text
# server: 9.1.2, StackExchange.Redis 3.0.0.0 (phiên bản assembly; gói NuGet 3.4.20)
đọc qua IDistributedCache: Sprint
key thật trong Valkey: teamboard:board:1
kiểu dữ liệu: Hash; các field: absexp, sldexp, data
TTL: 30 giây
```
Kết nối mặc định gọi `FLUSHALL` bị từ chối: `RedisCommandException: This operation is not available unless admin mode is enabled: FLUSHALL`; thêm `allowAdmin=true` vào chuỗi kết nối thì chạy được.
**Giải thích:** tiền tố `InstanceName` + khoá; mỗi mục là một Hash có hạn.

#### Lab 2.6 — Khoá từ .NET, hết hạn khi chủ còn chạy
```text
A lấy khoá: True; B lấy khoá khi A còn giữ: False
sau 600 ms (TTL 500 ms đã hết), B lấy được: True  <- A có thể vẫn đang chạy
A mở khoá bằng Lua so chủ: xoá 0 key (không xoá khoá của B); B mở khoá: xoá 1 key
```
**Giải thích:** khoá có hạn nghĩa là **hai bên có thể cùng tin mình đang giữ khoá** nếu bên đầu chạy quá hạn. Mở khoá đúng chủ chỉ ngăn xoá nhầm, không ngăn việc này (Part 4).

## L5 Troubleshooting — Lỗi hay gặp

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| Server khựng, mọi client chậm | `KEYS`, xoá big key, Lua dài | `SLOWLOG GET` xem lệnh nào lâu | thay `KEYS` bằng `SCAN`, dùng `UNLINK`, rút ngắn script |
| Khoá bị xoá bởi người khác | mở khoá bằng `DEL` trần | xem giá trị khoá và chủ | mở khoá bằng Lua so chủ |
| Khoá kẹt mãi | không đặt hạn, hoặc chủ chết | `TTL` của khoá là `-1` | luôn đặt `PX`; dùng `SET NX PX` trong một lệnh |
| `SCAN` trả trùng hoặc trang rỗng | hành vi đúng của `SCAN` | xem con trỏ có về 0 chưa | lặp đến con trỏ 0 và khử trùng |
| `MULTI` chạy nửa chừng | lỗi lúc chạy không rollback | xem phản hồi từng lệnh trong `EXEC` | kiểm đầu vào trước; dùng Lua nếu cần nguyên tử có điều kiện |
| `FLUSHALL` bị từ chối từ ứng dụng | client chưa bật admin mode | đọc thông báo `admin mode` | chỉ bật `allowAdmin` ở công cụ quản trị, không ở ứng dụng |
| Nhiều ứng dụng đè key nhau | dùng chung server, không tiền tố | `SCAN` xem key | đặt `InstanceName` theo ứng dụng và môi trường |

## Tự kiểm tra

1. Vì sao giá trị khoá phải là định danh chủ chứ không phải `1`?
2. `MULTI` có rollback không? Chứng minh bằng một ví dụ.
3. Vì sao trang đầu của `SCAN` có thể rỗng mà chưa hết dữ liệu?
4. Vì sao multiplexer phải là singleton?

<details>
<summary>Đáp án ngắn</summary>

1. Để mở khoá so được chủ; nếu không `DEL` có thể xoá khoá của người khác (Lab 2.1, 2.6).
2. Không: lỗi lúc chạy chỉ làm lệnh đó lỗi, các lệnh khác vẫn chạy (Lab 2.2).
3. `COUNT` chỉ là gợi ý, `MATCH` lọc sau khi lấy; phải lặp tới con trỏ 0 (Lab 2.3).
4. Mọi lời gọi dùng chung một kết nối được tự gộp; tạo mỗi thao tác tốn kết nối và mất lợi ích gộp.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 đến 2.6 (đầu ra thật).
**Đã tự mở:** redis.io `/docs/latest/develop/clients/patterns/distributed-locks/` (mẫu `SET NX PX`, mở khoá bằng Lua); trang seredis.dev (multiplexer dùng chung, không có lệnh chặn, giao dịch).
**Chỉ qua agent (⚠):** valkey.io `/commands/scan/` (bảo đảm của `SCAN`); learn.microsoft.com `/performance/caching/distributed`; hành vi `MULTI` lỗi lúc chạy (xác nhận bằng Lab 2.2, nhưng phát biểu "lỗi cú pháp huỷ cả giao dịch" chưa tự thử).
**Chưa kiểm:** `StackExchange.Redis` với Valkey chưa có tuyên bố chính thức; lab chạy được với Valkey 9.1.2 nhưng đó không phải bảo đảm tương thích. Trang valkey.io/clients chỉ liệt kê Valkey GLIDE cho C#.
