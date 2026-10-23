# Caching: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu caching như một khái niệm độc lập với công cụ, rồi tự thiết kế, triển khai, đo và gỡ lỗi một lớp cache cho dịch vụ .NET, biết khi nào **không** nên cache.
> **Cần biết trước:** đọc được C# cơ bản, biết HTTP và khái niệm cơ sở dữ liệu. Chưa cần biết Redis hay CDN.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Loại:** khái niệm. Công cụ trong dự án: [Redis / Valkey](../redis.md), chỉ là **một trong các lựa chọn** (xem [Bản đồ giải pháp](part-1-fundamentals.md#bản-đồ-giải-pháp)).

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự triển khai và đo bằng code hoặc lệnh thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác (hoặc không cache)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | cache là gì, sáu cơ chế đọc/ghi, eviction, invalidation, Bản đồ giải pháp | Giải thích và chọn được cơ chế phù hợp |
| [2 Core skills](part-2-core-skills.md) | HTTP caching, `IMemoryCache`, Decorator, key, TTL, output cache | Cache đúng ở tầng gần người dùng |
| [3 Multi-component](part-3-multi-component.md) | cache phân tán, `HybridCache`, nhiều instance, đồng bộ invalidation | Cache chạy đúng khi có nhiều instance |
| [4 Advanced](part-4-advanced.md) | stampede, invalidation nâng cao, consistent hashing, đo hit ratio | Chống quá tải và đo hiệu quả |
| [5 Production](part-5-production-security-ops.md) | quan sát, cache chết, bảo mật, chi phí | Vận hành cache an toàn |
| [6 Projects](part-6-projects.md) | áp dụng lên đường đọc Board của TeamBoard (mô phỏng) | Đo được lợi ích và rủi ro trên bài toán thật |
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
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi đặt cache vào ứng dụng ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh tầng cache nằm ở Part 1 và Part 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thử

Windows 11, .NET SDK 10.0.402 (runtime 10.0.13), 12 luồng logic, ngày 23/10/2026. Gói dùng trong lab (thư mục thử ngoài repo): `Microsoft.Extensions.Caching.Memory` 10.0.13 và `Microsoft.Extensions.Caching.Hybrid` 10.11.0. Các lab L4 của Part 1 đến 6 đã chạy thật trên môi trường này và ghi đầu ra thật; số đo thời gian chỉ có giá trị tương đối trên máy này. **Lab dùng Valkey thật (Part 3) chưa chạy** vì Docker Desktop đang tắt; ghi ⚠. Các lab "mô phỏng" tự viết (eviction, jitter, XFetch, poisoning) là mô hình, không phải phép đo hệ thống thật, và ghi rõ như vậy.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: giải thích cache là gì và vì sao nó đổi **độ cũ** lấy **tốc độ**.
- [ ] L3: kể sáu cơ chế đọc/ghi, mỗi cái ai nạp và ai ghi cache; vẽ được luồng cache-aside và vì sao thứ tự ghi–xoá quan trọng.
- [ ] L4: viết Decorator cache-aside, đặt `Cache-Control` và `ETag` đúng, đo hit ratio và số lần chạm nguồn.
- [ ] L5: từ triệu chứng (dữ liệu cũ, DB quá tải khi hết hạn, 304 không xuất hiện, dữ liệu riêng lộ ra) tìm nguyên nhân gốc.
- [ ] L6: nêu chỉ số cần theo dõi, cách xử lý khi cache chết, rủi ro bảo mật, chi phí.
- [ ] L7: nói khi nào dùng `IMemoryCache`, `HybridCache`, cache phân tán, CDN, và khi nào **không** cache.
- [ ] L8: thiết kế lớp cache cho một dịch vụ mới kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Ghi chú công cụ liên quan: [redis.md](../redis.md).
