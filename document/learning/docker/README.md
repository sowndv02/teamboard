# Docker: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** từ chỗ chưa biết Docker, tới khi tự đóng gói, ghép, vận hành và gỡ lỗi một dịch vụ .NET trong container, và giải thích được vì sao chọn (hoặc không chọn) Docker.
> **Cần biết trước:** dùng được terminal; biết HTTP và biến môi trường ở mức cơ bản. Chưa cần biết Linux sâu.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Loại:** công cụ · **Một trong các lựa chọn của** [Container](../container.md#bản-đồ-giải-pháp) (Podman, containerd và nerdctl, CRI-O là các lựa chọn khác).

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự triển khai bằng lệnh hoặc cấu hình thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn Docker thay vì lựa chọn khác? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | Container vs VM, kiến trúc Docker, vòng đời, namespaces, cgroups | Giải thích được container là gì bên dưới |
| [2 Core skills](part-2-core-skills.md) | CLI, Dockerfile, layer, mạng, lưu trữ, môi trường, log | Tự đóng gói và chạy một dịch vụ |
| [3 Multi-container](part-3-multi-container.md) | Compose, DNS giữa service, healthcheck, cấu hình, secret | Chạy cả hệ thống bằng một lệnh |
| [4 Advanced](part-4-advanced.md) | BuildKit, multi-stage, cache, đa nền tảng, tài nguyên | Build nhanh, image gọn, giới hạn đúng |
| [5 Production](part-5-production-security-ops.md) | Least privilege, quét image, quan sát, CI/CD, rollback | Đưa image vào vận hành an toàn |
| [6 Projects](part-6-projects.md) | Dự án thật trên repo TeamBoard | Áp dụng trọn vẹn trên mã thật |
| [7 Mastery](part-7-troubleshooting-mastery.md) | Sự cố, thiết kế, phỏng vấn, kiểm tra tổng hợp | Tự đánh giá mức thành thạo |

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
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi chạy lệnh ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức này.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thử

Windows 11 (WSL2), Docker Desktop 4.77.0, Engine 29.5.3, Compose v5.1.4, ngày 23/10/2026. Các lab L4 của Part 1 đến 5 đã chạy thật trên môi trường này và ghi đầu ra thật. **Part 6: Lab 6.2 và 6.3 (chạy container `board-service` và ghép với Jaeger) chưa chạy** vì Docker Desktop tắt khi viết; build lại image từ Dockerfile cũng chưa chạy vì cần kéo SDK khoảng 1 GB. Chỗ nào chưa chạy được đều gắn ⚠. Mọi tài nguyên tạo ra có tiền tố `lh-` và được dọn bằng đúng tên đó. Lộ trình **không** dùng `docker system prune`, `docker volume rm` hay `down -v` trên môi trường thật của dự án.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6 trên repo.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: giải thích container khác VM và vì sao dự án dùng nó.
- [ ] L3: vẽ được luồng `docker run` từ client tới runc, nêu vai trò namespaces và cgroups.
- [ ] L4: viết Dockerfile multi-stage, chạy Compose có healthcheck, publish cổng, dùng volume.
- [ ] L5: từ triệu chứng (thoát 137, unhealthy, không resolve được tên, đầy đĩa) tìm ra nguyên nhân gốc.
- [ ] L6: nêu 5 biện pháp siết quyền, cách quét và ghim image, cách rollback.
- [ ] L7: nói khi nào dùng Compose, ECS Fargate, Kubernetes, Podman.
- [ ] L8: thiết kế đóng gói và triển khai cho một service mới kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`.
