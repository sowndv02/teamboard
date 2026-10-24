# Redis: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** nắm **phần riêng hoặc khác của Redis** so với Valkey: giấy phép và phiên bản, các tính năng chỉ có ở Redis (mô-đun hợp nhất ở Redis 8, vector set, `DELEX`, `XDELEX`...), khác biệt tương thích dữ liệu, và vì sao dự án này chọn Valkey.
> **Cần biết trước:** lộ trình [Valkey](../valkey/README.md) (cơ chế chung: kiểu dữ liệu cơ bản, persistence, replication, eviction, ACL, cluster) và khái niệm [Caching](../caching/README.md).
> **Thời lượng ước tính:** 12 đến 18 giờ nếu làm đủ lab.
> **Loại:** công cụ. Đây **không** phải bản sao của lộ trình Valkey: cái gì chung thì liên kết sang, ở đây chỉ dạy cái khác.

## Cảnh báo giấy phép và chính sách dự án (đọc trước)

- **Chính sách dự án (không đổi bởi lộ trình này):** Redis server `banned` (T3), Valkey `watch` (T1) trong `config/dependencies.yaml`; Redis ≥ 7.4 mang giấy phép source-available (RSALv2 hoặc SSPLv1), Redis ≥ 8.0 thêm AGPLv3; AGPL bị cấm theo mặc định trong `.agents/rules/dependency-policy.md`. Muốn dùng Redis phải được người dùng duyệt và có ADR.
- **Lab chạy cục bộ để học**, trong container `lh-redis*`/`lh-r*` chỉ bind `127.0.0.1`, không phân phối lại, không đưa vào repo hay Compose của dự án.
- **Đây không phải tư vấn pháp lý:** lộ trình trình bày điều trang giấy phép chính thức của Redis nói (đã mở) và gắn ⚠ chỗ chưa kiểm.

## Phạm vi và những gì công cụ này KHÔNG đại diện

- **Redis không đại diện cho cache.** Cache là một họ kỹ thuật; Redis chỉ là một dòng trong [Bản đồ giải pháp](../caching/part-1-fundamentals.md#bản-đồ-giải-pháp).
- **Redis không đại diện cho Valkey** và ngược lại: sau khi tách (Valkey fork từ Redis 7.2.4) hai bên phát triển tính năng khác nhau (Part 2, 3 và [ghi chú so sánh](../redis-vs-valkey.md)).
- **Redis 7.2 khác Redis 8:** 7.2 là BSD-3 và gần như giống Valkey 7.2; Redis 8 có thêm mô-đun, lệnh mới và giấy phép khác. Lộ trình ghi rõ phiên bản cho từng khẳng định.
- **Số hiệu năng của hãng** (Redis nói "nhanh hơn 87%", Valkey nói "1,2 triệu QPS") là **tuyên bố của nhà cung cấp**, không được dùng để kết luận Redis hay Valkey nhanh hơn.

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự triển khai và đo bằng lệnh hoặc code thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác (hoặc không dùng)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | lịch sử, ba giấy phép, phiên bản, sản phẩm, so với Valkey | Nói đúng phiên bản nào dùng giấy phép nào |
| [2 Core skills](part-2-core-skills.md) | lệnh riêng của Redis, hash field expiration, `DELEX`, `SET IFEQ`, tương thích client | Biết lệnh nào có ở bên nào |
| [3 Multi-component](part-3-multi-component.md) | mô-đun hợp nhất ở Redis 8: JSON, time series, Bloom, Top-K, vector set, query engine | Dùng được các kiểu dữ liệu mở rộng |
| [4 Advanced](part-4-advanced.md) | khoá an toàn, atomic slot migration, I/O threading, hot key, compact hash | Hiểu tính năng quy mô lớn của Redis 8 |
| [5 Production](part-5-production-security-ops.md) | giấy phép khi chạy production, ACL và mô-đun, vá lỗi, dịch vụ quản lý, chi phí | Đánh giá rủi ro khi chọn Redis |
| [6 Projects](part-6-projects.md) | vì sao TeamBoard chọn Valkey, di chuyển giữa hai bên | Quyết định có số đo |
| [7 Mastery](part-7-troubleshooting-mastery.md) | sự cố, case thực tế, phỏng vấn, bài mở | Tự đánh giá mức thành thạo |

## Ma trận phủ

`✓` là Part có mục tương ứng, `–` là không áp dụng (lý do ở dưới). Mỗi ô `✓` có đúng một mục `## Lx` trong file Part.

| Part | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 |
|---|---|---|---|---|---|---|---|---|
| 1 | ✓ | ✓ | ✓ | ✓ | – | – | ✓ | – |
| 2 | ✓ | ✓ | ✓ | ✓ | ✓ | – | – | – |
| 3 | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | – |
| 4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 5 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 6 | ✓ | – | – | ✓ | ✓ | – | – | – |
| 7 | – | – | – | – | ✓ | – | ✓ | ✓ |

**Lý do các ô `–`:**
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi dùng lệnh riêng ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, Docker 29.5.3 (Docker Desktop, WSL2). Image **đã kéo với sự đồng ý của người dùng**: `redis:8.10.2-alpine` (Redis 8.10.2, bản mới nhất trên Docker Hub lúc đó) và `redis:7.2.16-alpine` (bản BSD-3 cuối); để so sánh dùng lại `valkey/valkey:9.1.2-alpine` (Valkey 9.1.2) đã có từ lộ trình Valkey. Client .NET: `StackExchange.Redis` 3.4.20. Container có tiền tố `lh-`, chỉ `127.0.0.1`, đã dọn sau mỗi lab; image giữ lại. Số đo thời gian chỉ tương đối.
**Chưa chạy (⚠):** Redis Software, Redis Cloud, Redis Insight; Redis Cluster và atomic slot migration; I/O threading và hiệu năng; `HEXPIRE` v.v. trên replica; các phiên bản 8.0, 8.2, 8.4, 8.6, 8.8 riêng lẻ (chỉ chạy 8.10.2 và 7.2.16).

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 hoặc liên kết sang lab tương ứng ở lộ trình Valkey, và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: nói đúng phiên bản Redis nào dùng giấy phép nào, và vì sao Valkey ra đời.
- [ ] L3: kể các tính năng chỉ có ở Redis 8 (mô-đun hợp nhất, vector set, `DELEX`...) và ngược lại các lệnh chỉ có ở Valkey.
- [ ] L4: dùng JSON, time series, Bloom, vector set; `DELEX`; so sánh một lệnh trên hai server.
- [ ] L5: từ triệu chứng (lệnh không tồn tại, RDB không nạp được, ACL quá rộng, lệnh khác phiên bản) tìm nguyên nhân gốc.
- [ ] L6: nêu nghĩa vụ giấy phép, vá lỗi, ACL cho mô-đun, chi phí.
- [ ] L7: chọn giữa Redis, Valkey, Memcached, hoặc không dùng, cho ba bối cảnh.
- [ ] L8: viết đề xuất chọn Redis hay Valkey cho một dự án kèm điều kiện đổi quyết định.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Cơ chế chung: [lộ trình Valkey](../valkey/README.md). Chính sách dự án: `config/dependencies.yaml`, `.agents/rules/dependency-policy.md`; lộ trình **không** sửa các tài liệu đó và không sửa `src/`.
