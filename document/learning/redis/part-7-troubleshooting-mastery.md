# Part 7 — Troubleshooting & Mastery (Redis)

> [!NOTE]
> **Mục tiêu:** luyện chẩn đoán các sự cố đặc thù khi dùng hoặc chuyển giữa Redis và Valkey, học từ các sự kiện giấy phép và bảo mật, rồi tự viết một quyết định chọn engine có điều kiện đổi.
> **Cần biết trước:** [Part 1 đến 6](README.md), [Valkey Part 7](../valkey/part-7-troubleshooting-mastery.md) (các case chung: GitHub, Slack, Stack Overflow, Discord).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** chẩn đoán đúng 5 tình huống, trả lời được câu phỏng vấn, hoàn thành một bài mở và tự chấm theo tiêu chí.
> [← Part 6](part-6-projects.md) · [Về Part 0](README.md)

## L5 Troubleshooting — Diễn tập sự cố

Mỗi tình huống: đọc triệu chứng, tự chẩn đoán, rồi mở đáp án.

#### Tình huống 1: sau khi "chỉ đổi image", server không khởi động

Đội đổi `redis:8` thành `valkey/valkey:9` trong Compose và mount lại thư mục dữ liệu cũ; container thoát ngay với mã 1.
<details>
<summary>Chẩn đoán</summary>

Tệp `dump.rdb` do Redis 8 tạo (tiêu đề `REDIS0015`) mà Valkey không đọc: log có "Can't handle RDB format version 15" (Lab, Dự án 6.1). Sửa: không đổi engine bằng cách copy file; khôi phục từ bản sao lưu tương thích hoặc di chuyển ở tầng ứng dụng; kiểm tiêu đề RDB trước khi đổi.
</details>

#### Tình huống 2: ứng dụng báo `unknown command 'JSON.SET'` ở môi trường staging

Chạy ổn ở máy dev (Redis 8), lỗi ở staging (Valkey).
<details>
<summary>Chẩn đoán</summary>

Mô-đun JSON là phần của Redis 8, Valkey mặc định không có (Lab 3.1, 2.2). Sửa: dùng cùng engine ở mọi môi trường; hoặc bỏ phụ thuộc lệnh riêng; kiểm `MODULE LIST` và `COMMAND INFO` trong kiểm tra khởi động.
</details>

#### Tình huống 3: user "chỉ đọc" lại đọc được cả chỉ mục tìm kiếm

Sau khi nâng cấp lên Redis 8, một user chỉ có `+@read` truy vấn được `FT.SEARCH` và `JSON.GET`.
<details>
<summary>Chẩn đoán</summary>

Lệnh mô-đun được gộp vào `@read` (Lab 3.3). Sửa: ACL liệt kê lệnh tường minh hoặc trừ danh mục (`-@search`); kiểm lại ACL mỗi lần nâng phiên bản.
</details>

#### Tình huống 4: lệnh `DELEX` chạy ở Redis nhưng không ở Valkey

Mã mở khoá dùng `DELEX`; khi chuyển engine, mọi lần mở khoá lỗi.
<details>
<summary>Chẩn đoán</summary>

`DELEX` chỉ có ở Redis; Valkey có `DELIFEQ` (Lab 2.2). Sửa: dùng Lua so chủ rồi xoá, chạy được ở mọi nơi ([Valkey Part 2](../valkey/part-2-core-skills.md)), hoặc chọn lệnh theo engine qua một lớp trừu tượng; nhớ giới hạn khoá không đổi (Part 4).
</details>

#### Tình huống 5: câu hỏi pháp lý từ khách hàng

Khách hỏi: "sản phẩm của các bạn có cung cấp Redis cho chúng tôi dưới dạng dịch vụ không, và giấy phép nào?"
<details>
<summary>Chẩn đoán</summary>

Phân biệt dùng nội bộ với cung cấp dịch vụ (Part 5): RSALv2 cấm dịch vụ cạnh tranh, SSPLv1 đòi công bố mã nguồn lớp quản lý, AGPLv3 đòi cung cấp mã nguồn bản sửa đổi dùng qua mạng; Redis ≤ 7.2 và Valkey là BSD-3. Cần luật sư; lộ trình không phải tư vấn pháp lý. Với dự án này: dùng nội bộ, và chính sách chọn Valkey tránh câu hỏi.
</details>

## L7 Architecture — Trong thực tế

Mức: **đã mở** là tự mở nguồn gốc; **⚠ qua agent** là agent nghiên cứu đã đọc, chưa tự đối chiếu. Các case về cách dùng Redis (GitHub, Slack, Stack Overflow, Discord, Twitter) nằm ở [Valkey Part 7](../valkey/part-7-troubleshooting-mastery.md), không lặp lại.

| Sự kiện | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **Redis đổi giấy phép (20/03/2024)** | từ 7.4: RSALv2 hoặc SSPLv1, không còn BSD-3; không hồi tố; dùng nội bộ không bị ảnh hưởng | giấy phép đi theo phiên bản; có thể đổi giữa chừng | [Redis adopts dual source-available licensing](https://redis.io/blog/redis-adopts-dual-source-available-licensing/) (đã mở ở TB-015) |
| **Valkey ra đời (28/03/2024)** | Linux Foundation, fork từ Redis 7.2.4, BSD-3; AWS, Google Cloud, Oracle, Ericsson, Snap hậu thuẫn | cộng đồng và nhà cung cấp cloud có thể tách nhánh | [Linux Foundation](https://www.linuxfoundation.org/press/linux-foundation-launches-open-source-valkey-community) (đã mở) |
| **ElastiCache và MemoryDB hỗ trợ Valkey (08/10/2024)** | Valkey 7.2; giá thấp hơn theo AWS (node khoảng 20%, Serverless khoảng 33%, MemoryDB khoảng 30%) | nhà cung cấp cloud chọn engine theo giấy phép và giá | [AWS Database Blog](https://aws.amazon.com/blogs/database/amazon-elasticache-and-amazon-memorydb-announce-support-for-valkey/) (đã mở) |
| **Redis thêm AGPLv3 (01/05/2025, Redis 8)** | bài thừa nhận quyết định trước làm tổn hại quan hệ với cộng đồng; antirez quay lại 11/2024; Redis 8 hợp nhất Redis Stack | giấy phép là một quyết định có thể đảo | [Redis is open source again](https://redis.io/blog/agplv3/) (⚠ qua agent) |
| **Valkey 9.0 (21/10/2025)** | hash field expiration, atomic slot migration, nhiều database ở cluster mode; các số tăng tốc là của hãng | hai bên cùng thêm tính năng theo cách riêng | [Linux Foundation, Valkey 9.0](https://www.linuxfoundation.org/press/valkey-9.0-delivers-performance-and-resiliency-for-real-time-workloads) (⚠ qua agent) |

### Khi Redis là nơi xảy ra sự cố bảo mật

| Sự cố | Điều đã xảy ra | Bài học | Nguồn (mức) |
|---|---|---|---|
| **Lỗ hổng RCE qua Lua (CVE-2025-49844 và các CVE khác), 10/2025** | vá ở Redis 8.0.4 và 8.2.2 | script phía server là bề mặt tấn công; hạn chế `EVAL` nếu không cần | release notes Redis Open Source (⚠ qua agent) |
| **RCE liên quan `RESTORE` và Lua, 05/2026** | vá ở 8.2.6 và 8.4.3 | lệnh nhập dữ liệu nhị phân cũng là bề mặt tấn công | release notes (⚠ qua agent) |
| **Bỏ qua xác thực chứng chỉ TLS do NUL trong CN, 08/2026** | vá ở 8.10.1 | TLS bằng chứng chỉ cần cập nhật như mã khác | release notes 8.10 (⚠ qua agent) |

**Mô hình phổ biến:** cache và bộ đếm chạy trên engine nào cũng được nếu giữ ở tập lệnh chung; mô-đun và lệnh riêng làm tăng khoá chặt vào một bên.
**Chưa tìm được nguồn gốc đã mở cho:** số liệu hiệu năng so sánh Redis và Valkey tái lập được (chỉ có tuyên bố của hãng), danh sách hỗ trợ Valkey của Azure, ngày ngừng Azure Cache for Redis; không nêu.

### Đánh đổi khi thiết kế

- **Tính năng so với giấy phép:** mô-đun Redis 8 tiện nhưng đi kèm giấy phép chọn một trong ba; Valkey giấy phép mở nhưng ít mô-đun mặc định.
- **Tiện so với khoá chặt:** lệnh riêng (`JSON.*`, `DELEX`, `FT.*`) tiện, nhưng đổi engine thì phải viết lại.
- **Vá nhanh so với ổn định:** Redis 8 ra bản vá bảo mật thường xuyên; cần quy trình cập nhật.
- **Tự chạy so với quản lý:** dịch vụ quản lý đỡ việc nhưng bị giới hạn engine và phiên bản dịch vụ hỗ trợ.

## L8 Mastery — Câu hỏi và bài mở

### Câu hỏi phỏng vấn

1. Redis ≤ 7.2, 7.4 đến 7.8 và ≥ 8 mang giấy phép nào? Điều khoản "dịch vụ cạnh tranh" nghĩa là gì?
2. Valkey ra đời thế nào và khác Redis ở điểm nào bạn đã kiểm?
3. Lệnh nào có ở Redis 8 mà Valkey 9.1.2 không có, và ngược lại?
4. Vì sao copy `dump.rdb` giữa hai engine thất bại, và cách di chuyển đúng?
5. ACL `+@read` thay đổi thế nào khi lên Redis 8?
6. Vì sao không kết luận "Redis nhanh hơn" hay "Valkey nhanh hơn" từ quảng cáo?
7. Khi nào bạn chọn Redis cho một dự án mới? Khi nào Valkey? Khi nào không dùng cả hai?
8. Làm sao viết mã dùng cache sao cho đổi engine không phải sửa nhiều?

### Bài mở

#### Bài 1 — Viết ADR chọn engine cho một dịch vụ mới
Một dịch vụ cần cache và bộ đếm phân tán, có thể cần tìm kiếm văn bản sau này. Viết ADR: phương án (Valkey, Redis 8, Redis 7.2, Memcached, không dùng), giấy phép, tính năng, vận hành, chi phí, **điều kiện xem lại**. **Tiêu chí:** mỗi lập luận trỏ tới một Lab hoặc nguồn đã mở; nêu rõ phần ⚠ chưa kiểm; có phương án loại kèm lý do.

#### Bài 2 — Kế hoạch di chuyển Redis 8 sang Valkey
Hệ thống đang chạy Redis 8 với dữ liệu kiểu cơ bản và một khoá JSON. Viết kế hoạch: kiểm gì trước (lệnh, mô-đun, ACL), cách chuyển dữ liệu (không copy file), kiểm sau, quay lại thế nào. **Tiêu chí:** dựa trên Dự án 6.1 và danh sách kiểm; nêu phần dữ liệu JSON xử lý ra sao.

#### Bài 3 — Phản biện một quyết định
Đồng nghiệp đề xuất "dùng Redis 8 vì nhanh hơn 87%, bật mọi mô-đun, copy dữ liệu từ Valkey sang cho nhanh, và tag `latest`". Viết phản biện nêu ít nhất 5 rủi ro đã học. **Tiêu chí:** mỗi rủi ro trỏ tới một Lab hay nguồn.

### Tiêu chí thành thạo

- Giải thích không nhìn tài liệu: giấy phép theo phiên bản, lệnh riêng của mỗi bên, RDB không tương thích, ACL và mô-đun.
- Chẩn đoán đúng 5 tình huống L5 không xem đáp án.
- Một bài mở có bằng chứng từ lab và nêu được giới hạn của nó.
- Nói được ít nhất ba bối cảnh nên chọn Valkey, hai bối cảnh nên chọn Redis, và một bối cảnh không dùng cả hai.

## Tự kiểm tra

1. Vì sao quyết định giấy phép của Redis ảnh hưởng đến việc chọn engine ở dự án này?
2. Tại sao một lệnh nhập như `RESTORE` cũng cần được cân nhắc khi nói về bảo mật?
3. Tình huống 1 xảy ra vì điều gì mà người đổi image không thấy?

<details>
<summary>Đáp án ngắn</summary>

1. Redis ≥ 7.4 là source-available; ≥ 8 có AGPL bị cấm mặc định; dự án chọn Valkey (BSD-3) tránh rủi ro.
2. Release notes cho thấy các CVE RCE liên quan `RESTORE` và Lua; dữ liệu nhị phân do người dùng đưa vào là bề mặt tấn công.
3. Định dạng RDB (`REDIS0015`) không tương thích; chỉ thấy khi đọc log khởi động.
</details>

## Nguồn và độ tin cậy

**Đã tự mở:** Redis đổi giấy phép (20/03/2024, đã mở ở TB-015), Linux Foundation Valkey (28/03/2024), AWS Database Blog (08/10/2024).
**Chỉ qua agent (⚠):** redis.io `/blog/agplv3/` (01/05/2025), Linux Foundation Valkey 9.0 (21/10/2025), release notes Redis Open Source cho các CVE (10/2025, 05/2026, 08/2026).
**Chưa kiểm:** các đáp án chẩn đoán là suy luận từ lab đã chạy và từ cơ chế đã học, không phải sự cố đã gặp ở dự án này; mọi so sánh hiệu năng.
