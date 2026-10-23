# API Gateway: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu API Gateway như một **vai trò** trong kiến trúc, nắm từng cơ chế của nó (định tuyến, offloading, cân bằng tải, health check, timeout, rate limit, xác thực, quan sát), thực hành bằng YARP trên .NET, và biết lúc nào **không** cần gateway.
> **Cần biết trước:** HTTP cơ bản, C# cơ bản, khái niệm microservice ([microservice.md](../microservice.md)). Chưa cần biết YARP.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Loại:** khái niệm, dùng **YARP** làm công cụ thực hành. Công cụ chỉ là **một trong các lựa chọn** (xem [Bản đồ giải pháp](part-1-fundamentals.md#bản-đồ-giải-pháp)).

## Phạm vi YARP: những gì công cụ này KHÔNG đại diện

Mọi lab dùng YARP vì dự án chọn nó (design TB-007, quyết định D0). Điều đó không có nghĩa YARP đại diện cho cả khái niệm:

- **Không phải sản phẩm quản lý API.** Không có sẵn portal cho developer, API key, quota, analytics; Azure API Management và Amazon API Gateway có.
- **Không phải load balancer chuyên dụng hay WAF.** YARP cân bằng tải ở tầng 7 nhưng không thay ALB hay WAF.
- **Không phải service mesh.** Gateway lo lưu lượng bắc-nam (client vào); mesh lo đông-tây giữa các service.
- **Khác nginx, Envoy, Kong, Traefik:** các proxy đa ngôn ngữ chạy như tiến trình riêng, nhiều tính năng và plugin; YARP là **thư viện** trong ASP.NET Core để tự code gateway. Phần cấu hình và mặc định của từng công cụ khác nhau (Part 1, L7).
- **Không có sẵn retry hay circuit breaker** (Part 4); các sản phẩm khác có cấu hình sẵn.
- **Không phải BFF.** BFF là cách tổ chức (mỗi loại client một backend); YARP có thể là nền để dựng nó.

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
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác (hoặc không dùng)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | gateway là gì, phân biệt với reverse proxy/LB/mesh/BFF, từng cơ chế, Bản đồ giải pháp | Giải thích và chọn được có dùng gateway hay không |
| [2 Core skills](part-2-core-skills.md) | route, cluster, transform, `X-Forwarded-*`, correlation, mã lỗi mặc định của YARP | Dựng được gateway định tuyến đúng |
| [3 Multi-component](part-3-multi-component.md) | nhiều upstream, cân bằng tải, health check, nhiều instance | Gateway chạy đúng khi upstream thay đổi |
| [4 Advanced](part-4-advanced.md) | rate limit, timeout, retry, JWT, session affinity | Bảo vệ hệ thống ở biên |
| [5 Production](part-5-production-security-ops.md) | quan sát, bảo mật, rollout cấu hình, điểm lỗi đơn, chi phí | Vận hành an toàn |
| [6 Projects](part-6-projects.md) | gateway mô phỏng theo design TB-007 cho `board-service` | Áp lên TeamBoard có số đo |
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
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi dựng gateway thật ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, .NET SDK 10.0.402 (runtime 10.0.13), ngày 23/10/2026. Thư viện: `Yarp.ReverseProxy` 2.3.0 (MIT; bản ổn định mới nhất trên nuget.org, mục tiêu net6 đến net8 nhưng chạy được trên .NET 10) và `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13, trong dự án thử **ngoài repo**. Mọi upstream là Kestrel giả chạy trong cùng tiến trình trên `127.0.0.1`; không dùng Docker, không tải image. Số đo thời gian chỉ có giá trị tương đối trên máy này.
**Chưa chạy:** gateway sau ALB hay trong container, TLS, mTLS, WebSocket/streaming, nhiều instance gateway thật, `Forwarded` (RFC 7239), cấu hình nạp lại nóng; các phần đó gắn ⚠ trong từng Part. YARP trong `config/dependencies.yaml` ghi `verified: false`: chỉ dùng ở lab ngoài repo.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: giải thích gateway khác reverse proxy, load balancer, mesh, BFF; nêu sáu vấn đề khi client gọi thẳng nhiều service.
- [ ] L3: kể các cơ chế (routing, aggregation, offloading, cân bằng tải, health check, timeout/retry, rate limit) và vì sao mỗi cái tồn tại.
- [ ] L4: dựng route, transform, cluster nhiều upstream, rate limit, JWT bằng YARP và đọc đúng mã lỗi.
- [ ] L5: từ triệu chứng (502, 504, 405, rate limit bị né, upstream chết mà vẫn nhận lưu lượng) tìm nguyên nhân gốc.
- [ ] L6: nêu chỉ số, rủi ro bảo mật, rollout cấu hình, điểm lỗi đơn, chi phí.
- [ ] L7: nói khi nào dùng YARP, nginx, Envoy, API Management, mesh ingress, hoặc không dùng gateway.
- [ ] L8: thiết kế gateway cho một hệ mới kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Nguồn sự thật của dự án: [spec](../../spec/api-gateway.md), [design](../../design/api-gateway.md), [plan](../../plans/api-gateway-slices.md); lộ trình **không** thay các tài liệu đó và không sửa chúng. Khi lab cho kết quả khác với design, lộ trình ghi lại ở Part 6 để người dùng quyết.
