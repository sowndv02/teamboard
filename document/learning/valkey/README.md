# Redis / Valkey: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** dùng Valkey (và Redis) như một **công cụ**: chọn đúng kiểu dữ liệu cho từng bài toán, hiểu persistence, replication, cluster, vận hành an toàn, và biết lúc nào **không** nên dùng nó.
> **Cần biết trước:** khái niệm cache ([lộ trình Caching](../caching/README.md)), C# cơ bản. Chưa cần biết Redis.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Redis riêng:** phần khác của Redis (giấy phép, mô-đun Redis 8, lệnh riêng) nằm ở [lộ trình Redis](../redis/README.md) và [ghi chú so sánh](../redis-vs-valkey.md).
> **Loại:** công cụ. Khái niệm gốc: [Caching](../caching/README.md). Redis/Valkey chỉ là **một dòng** trong [Bản đồ giải pháp](../caching/part-1-fundamentals.md#bản-đồ-giải-pháp).

## Phạm vi và những gì công cụ này KHÔNG đại diện

- **Cache không cần Redis/Valkey.** HTTP cache, CDN, cache trong tiến trình và cache của tầng dữ liệu thường là bước đầu rẻ hơn (xem Caching, Part 1 và 2).
- **Valkey không là nguồn sự thật.** Dữ liệu chỉ nằm trong nó là rủi ro: asynchronous replication và persistence đều có cửa sổ mất dữ liệu (Part 3). Case GitHub và Slack ở Part 7.
- **Memcached** là lựa chọn khác trong cùng họ: key-value đơn giản, đa luồng, không replication hay persistence sẵn (so sánh ở Part 1, L7).
- **Valkey khác Redis** ở giấy phép và một số chi tiết (Part 1). Ghi chú nào chỉ có nguồn cho một bên sẽ nói rõ bên nào.
- Module (JSON, Search, Bloom, vector) có trên một số bản nhưng ngoài phạm vi lộ trình này.

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
| [1 Fundamentals](part-1-fundamentals.md) | Valkey là gì, giấy phép, mô hình thực thi, kiểu dữ liệu và use case, TTL | Chọn được kiểu dữ liệu cho bài toán |
| [2 Core skills](part-2-core-skills.md) | khoá `SET NX`, bộ đếm, `SCAN`, pipeline, giao dịch, Lua, dùng từ .NET | Dùng đúng và an toàn từ ứng dụng |
| [3 Multi-component](part-3-multi-component.md) | persistence, replication, failover, pub/sub, Streams, nhiều instance | Hiểu khi nào dữ liệu mất và cách chia sẻ |
| [4 Advanced](part-4-advanced.md) | eviction, big key, hot key, Cluster, khoá phân tán, rate limit | Xử lý quy mô và các lỗi tinh vi |
| [5 Production](part-5-production-security-ops.md) | quan sát, bảo mật, backup, dịch vụ quản lý, chi phí | Vận hành an toàn |
| [6 Projects](part-6-projects.md) | cache Board, rate limit gateway, khoá/idempotency cho consumer (mô phỏng) | Áp lên TeamBoard có số đo |
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
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi dùng từ ứng dụng ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, Docker 29.5.3, .NET SDK 10.0.402 (runtime 10.0.13), ngày 23/10/2026. Server: image `valkey/valkey:9.1.2-alpine` (Valkey 9.1.2, bản ổn định mới nhất trên Docker Hub và valkey.io lúc đó). Client .NET: `StackExchange.Redis` 3.4.20, `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.13, `Microsoft.Extensions.Caching.Hybrid` 10.11.0. Mọi container, volume và mạng của lab có tiền tố `lh-`, chỉ bind `127.0.0.1`, đã dọn sau khi chạy. Số đo thời gian chỉ có giá trị tương đối trên máy này.
**Chưa chạy:** Sentinel (failover tự động), Cluster nhiều node, TLS, và dịch vụ ElastiCache/MemoryDB thật; các phần đó gắn ⚠ trong từng Part.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: nêu Valkey là gì, vì sao Redis đổi giấy phép và Valkey ra đời, vì sao thực thi lệnh đơn luồng quan trọng.
- [ ] L3: chọn kiểu dữ liệu cho: bộ đếm, hồ sơ, hàng đợi gần nhất, bảng xếp hạng, nhật ký sự kiện; giải thích RDB, AOF, replication.
- [ ] L4: tạo khoá `SET NX PX`, mở khoá đúng chủ, rate limit bằng Lua, dùng `IDistributedCache` và `HybridCache` với Valkey.
- [ ] L5: từ triệu chứng (server khựng, OOM, mất dữ liệu sau restart, replica không đồng bộ) tìm nguyên nhân gốc.
- [ ] L6: nêu chỉ số, ACL, backup, chi phí, rủi ro bảo mật.
- [ ] L7: nói khi nào dùng Valkey, Memcached, `HybridCache`, MemoryDB, hoặc không dùng.
- [ ] L8: thiết kế phần dùng Valkey cho một dịch vụ mới kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Khái niệm: [Caching](../caching/README.md). Chính sách dự án về Redis và Valkey: `config/dependencies.yaml` (Redis server `banned`, Valkey `watch`) và `.agents/rules/dependency-policy.md`.
