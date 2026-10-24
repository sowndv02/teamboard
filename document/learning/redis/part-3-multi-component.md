# Part 3 — Multi-component (Redis)

> [!NOTE]
> **Mục tiêu:** dùng các kiểu dữ liệu mở rộng được hợp nhất vào Redis 8 (JSON, chuỗi thời gian, Bloom và họ xác suất, vector set, query engine), biết chúng **không** có ở Valkey mặc định, và chọn khi nào cần chúng so với giải pháp khác.
> **Cần biết trước:** [Part 2](part-2-core-skills.md), [Valkey Part 1](../valkey/part-1-fundamentals.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** dùng JSON, time series, Bloom, Top-K, vector set và `FT.SEARCH`; nêu lệnh nào chỉ có ở Redis 8; chọn giữa mô-đun và kho chuyên dụng.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Các mô-đun hợp nhất ở Redis 8

Redis 8.0 (05/2025) hợp nhất vào bản Open Source các thành phần của Redis Stack: query engine, JSON, time series, năm cấu trúc xác suất và vector set (bản xem trước); `MODULE LIST` cho thấy chúng được nạp sẵn (Lab 3.1).

#### JSON (RedisJSON)
**Là gì:** lưu và sửa tài liệu JSON theo đường dẫn (`JSON.SET`, `JSON.GET`, `JSON.ARRAPPEND`).
**Use case:** lưu cấu hình Board dạng lồng nhau và sửa một cột mà không đọc cả tài liệu.

#### Time series (RedisTimeSeries)
**Là gì:** chuỗi (thời điểm, giá trị) với giữ lại theo thời gian và tổng hợp (`TS.ADD`, `TS.RANGE ... AGGREGATION`).
**Use case:** số Task hoàn thành mỗi phút của một Board.

#### Cấu trúc xác suất
**Bloom/Cuckoo** (thuộc về tập hợp, có sai số dương tính giả), **Count-min sketch** (đếm tần suất xấp xỉ), **Top-K** (phần tử xuất hiện nhiều nhất), **t-digest** (phân vị xấp xỉ).
**Use case:** Bloom: "người dùng này đã từng xem Board chưa" với bộ nhớ nhỏ; Top-K: Board được mở nhiều nhất.

#### Vector set
**Là gì:** tập vector có tìm kiếm gần đúng (`VADD`, `VSIM`).
**Use case:** gợi ý Board tương tự theo vector đặc trưng; nền cho tìm kiếm ngữ nghĩa.

#### Query engine (RediSearch)
**Là gì:** đánh chỉ mục hash hoặc JSON và truy vấn (`FT.CREATE`, `FT.SEARCH`); ở 8.4 thêm `FT.HYBRID` (tìm kiếm lai).
**Use case:** tìm Task theo tiêu đề và lọc theo mức ưu tiên.

## L2 Why — Vì sao có nhóm tính năng này (và vì sao nên thận trọng)

Trước Redis 8 các tính năng này là mô-đun riêng (Redis Stack, giấy phép khác); hợp nhất cho phép dùng chúng trong server chính. Với một số bài toán (chỉ mục phụ, đếm xấp xỉ, tài liệu lồng nhau) nó tránh được phải dựng kho thứ hai. **Cái giá:** gắn chặt vào Redis 8 (giấy phép Part 1; Valkey mặc định không có chúng); Redis là **bộ nhớ**, không thay cho một DB tìm kiếm hay DB chuỗi thời gian chuyên dụng khi dữ liệu lớn hoặc cần bền.

## L3 How — Cơ chế đáng nhớ

### Cách mô-đun gắn vào server
`MODULE LIST` ở Redis 8.10.2 liệt kê `search`, `bf`, `timeseries`, `vectorset`, `ReJSON` (và các cấu trúc xác suất khác trong `bf`); Redis 7.2.16 không có mô-đun nào; Valkey 9.1.2 mặc định chỉ có `lua` (Lab 3.1). Với Valkey, JSON, Bloom, tìm kiếm là **mô-đun riêng** (valkey-json, valkey-bloom, valkey-search, nhóm này theo kết quả tìm kiếm, ⚠ chưa mở trang chính thức, chưa chạy).

### ACL thay đổi theo
Redis 8 thêm các **danh mục ACL** mới (`@json`, `@timeseries`, `@bloom`, `@cuckoo`, `@cms`, `@topk`, `@tdigest`, `@search`, `@array`) và các lệnh mô-đun được gộp vào `@read`, `@write`...; user chỉ có `@read` đọc được `JSON.GET` nhưng không `JSON.SET` (Lab 3.3, Part 5).

### Cẩn trọng với từng mô-đun
- **JSON, time series, probabilistic:** dữ liệu nằm trong RAM; có RDB/AOF như thường nhưng định dạng riêng của mô-đun (Part 6: nạp sang Valkey thất bại).
- **Vector set:** ở 8.0 là bản xem trước (preview); đã có bổ sung ở các bản vá; cần kiểm trạng thái ổn định trước khi dùng.
- **Query engine:** chỉ mục nằm trong RAM; schema đổi cần dựng lại chỉ mục.

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0; chỉ Redis 8.10.2 (`lh-r8`), đã dọn sau lab.

#### Lab 3.1 — `MODULE LIST` trên ba server
**Đầu ra thật:**
```text
Redis 8.10.2:  search (ver 81001), bf (81001), timeseries (81001), vectorset (ver 1), ReJSON (81000), ...  (nạp sẵn)
Redis 7.2.16:  (trống)
Valkey 9.1.2:  lua (ver 1)
```
**Giải thích:** các mô-đun là phần của Redis 8; Redis 7.2 và Valkey mặc định không có (Lab 2.2: `JSON.SET`, `TS.ADD`, `BF.ADD`, `VADD`, `FT.CREATE` đều "không" ở hai server kia).

#### Lab 3.2 — Dùng từng mô-đun
**Đầu ra thật** (rút gọn):
```text
JSON.SET board:1 $ {"name":"Sprint","columns":["todo","doing","done"],"owner":{"id":"u1"}} -> OK
JSON.GET board:1 $.columns[1] -> ["doing"]   JSON.ARRAPPEND board:1 $.columns "review" -> 4   JSON.ARRLEN -> 4
TS.CREATE tasks:done RETENTION 60000 ; TS.ADD ... 1000 5 ; 2000 9 ; TS.RANGE - + AGGREGATION avg 2000 -> (0, 5), (2000, 9)
BF.RESERVE seen 0.01 1000 ; BF.ADD seen u1 -> 1 ; BF.EXISTS seen u1 -> 1 ; BF.EXISTS seen u2 -> 0
TOPK.RESERVE hot 3 ; TOPK.ADD hot a a a b b c ; TOPK.LIST hot -> a, b, c
VADD vs VALUES 3 1 0 0 v1 ; v2 = (0,1,0) ; v3 = (0.9,0.1,0) ; VSIM vs VALUES 3 1 0 0 COUNT 2 -> v1, v3
HSET doc:1 ... prio 2 ; doc:2 ... prio 5 ; FT.CREATE idx ON HASH PREFIX 1 doc: SCHEMA title TEXT prio NUMERIC SORTABLE
FT.SEARCH idx @prio:[3 +inf] RETURN 1 title -> 1 kết quả: doc:2, title "sprint review"
```
**Giải thích:** JSON sửa một mục trong mảng theo đường dẫn; time series tổng hợp theo khoảng; Bloom trả "có" cho phần tử đã thêm và "không" cho phần tử chưa thêm (Bloom không có âm tính giả, có thể có dương tính giả); `VSIM` trả hai vector gần `(1,0,0)` nhất (v1 rồi v3); `FT.SEARCH` lọc đúng theo `prio ≥ 3`.

#### Lab 3.3 — ACL với `@read` và mô-đun
`ACL CAT`: Redis 8.10.2 có thêm `array bloom cuckoo cms topk tdigest search timeseries json`; Redis 7.2.16 và Valkey 9.1.2 không có các danh mục đó. User `ro` có `+@read` trên Redis 8.10.2:
```text
JSON.GET j     -> {"a":1}
JSON.SET j ... -> NOPERM User ro has no permissions to run the 'json.set' command
@read chứa: ts.range, bf.exists, FT.SEARCH, json.get
```
**Giải thích:** các lệnh đọc của mô-đun nằm trong `@read` (user chỉ đọc đọc được JSON, time series, tìm kiếm) và lệnh ghi bị chặn đúng. Quy tắc ACL cũ có thể vô tình cho phép nhiều hơn dự kiến sau khi lên Redis 8: kiểm lại (Part 5).

## L5 Troubleshooting — Khi dùng mô-đun

| Triệu chứng | Nguyên nhân hay gặp | Kiểm tra | Cách xử lý |
|---|---|---|---|
| `unknown command 'JSON.SET'` | server không có mô-đun (Redis 7.2, Valkey mặc định) | `MODULE LIST`, `COMMAND INFO JSON.SET` | dùng Redis 8 hoặc mô-đun tương đương |
| `FT.SEARCH` không ra kết quả | chỉ mục không trùng `PREFIX`, hay schema sai kiểu | `FT.INFO idx` | tạo lại chỉ mục đúng tiền tố và kiểu |
| Tìm kiếm cũ sau khi đổi schema | chỉ mục không tự cập nhật theo schema mới | `FT.INFO` | `FT.ALTER` hoặc dựng lại |
| Bloom trả dương tính giả nhiều | tỉ lệ lỗi và dung lượng đặt thấp | `BF.INFO` | đặt `BF.RESERVE` đúng với số phần tử dự kiến |
| ACL cho user đọc nhiều hơn dự kiến sau nâng cấp | lệnh mô-đun gộp vào `@read` (Lab 3.3) | `ACL CAT read`, `ACL GETUSER` | liệt kê lệnh được phép tường minh |
| Dữ liệu mô-đun không nạp ở server khác | định dạng RDB riêng của mô-đun | thử nạp (Part 6) | di chuyển bằng xuất dữ liệu ở tầng ứng dụng |
| Bộ nhớ tăng mạnh sau khi lập chỉ mục | chỉ mục nằm trong RAM | `INFO memory`, kích thước chỉ mục | chỉ lập chỉ mục field cần; giới hạn `maxmemory` |
| Dùng vector set cho sản phẩm | tính năng ở trạng thái preview (8.0) | xem release notes phiên bản đang dùng | kiểm trạng thái ổn định; có phương án thay |

## L7 Architecture — Mô-đun Redis hay kho chuyên dụng

| Nhu cầu | Chọn | Cái giá |
|---|---|---|
| Đọc ghi nhanh trên dữ liệu nhỏ, đã có Redis | mô-đun Redis 8 (JSON, TS, Bloom, Top-K) | gắn vào Redis 8 và giấy phép; dữ liệu trong RAM |
| Tìm kiếm văn bản lớn, nhiều chỉ mục, phân tích | Elasticsearch hoặc OpenSearch | thêm hạ tầng |
| Chuỗi thời gian lớn, truy vấn phức tạp | cơ sở dữ liệu chuỗi thời gian chuyên dụng | thêm hạ tầng |
| Tập hợp "đã thấy" khổng lồ, chấp nhận dương tính giả | Bloom (Redis 8 hoặc valkey-bloom ⚠) | sai số; không xoá được phần tử (Bloom) |
| Tìm kiếm ngữ nghĩa, nhiều vector | kho vector chuyên dụng, hoặc vector set (preview ở 8.0) | tính ổn định, quy mô |
| Cần giấy phép mở (BSD-3) | Valkey, và mô-đun Valkey ⚠ | tính năng khác, chưa chạy |
| **Không dùng server riêng** | cột JSON của DB đã có, hoặc tìm kiếm trong DB | giữ tải ở DB |

**Cách chọn:** bắt đầu từ bài toán, không từ công cụ: dữ liệu bao lớn, cần bền không, giấy phép nào chấp nhận được. TeamBoard không có nhu cầu nào ở đây trong giai đoạn 0 (Part 6).

## Tự kiểm tra

1. Mô-đun nào có ở Redis 8.10.2 mà Valkey 9.1.2 mặc định không có?
2. `BF.EXISTS` trả "không" và "có" khác nhau thế nào về độ tin cậy?
3. Vì sao quy tắc ACL cũ có thể thay đổi nghĩa sau khi lên Redis 8?
4. Khi nào nên dùng kho tìm kiếm chuyên dụng thay vì `FT.SEARCH`?

<details>
<summary>Đáp án ngắn</summary>

1. JSON, time series, Bloom (và Cuckoo, CMS, Top-K, t-digest), vector set, query engine (Lab 3.1, 2.2).
2. "Không" là chắc chắn; "có" có thể là dương tính giả.
3. Lệnh mô-đun được gộp vào các danh mục như `@read` và `@write` (Lab 3.3).
4. Dữ liệu lớn hơn RAM, cần phân tích phức tạp, hay cần bền và tìm kiếm là nhiệm vụ chính.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 đến 3.3 (Redis 8.10.2 và hai server so sánh; đầu ra thật).
**Đã tự mở:** redis.io release notes Redis 8.0 (mô-đun hợp nhất, danh mục ACL mới, vector set preview) và 8.4 (`FT.HYBRID`), qua agent.
**Chỉ qua agent (⚠):** valkey-json, valkey-bloom, valkey-search, valkey-ldap (kết quả tìm kiếm; trang `valkey.io/topics/modules/` trả 404, chưa mở trang chính thức).
**Chưa kiểm:** hiệu năng và độ chính xác của vector set và query engine; hành vi mô-đun khi replication và cluster; so với valkey-json, valkey-bloom (chưa chạy); `FT.HYBRID`.
