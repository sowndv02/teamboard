# Container: lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu container như **một tiến trình Linux bị cô lập**, không phải máy ảo nhỏ: các cơ chế tạo nên nó (namespaces, cgroups, filesystem phân lớp, image theo chuẩn OCI, runtime nhiều tầng), mô hình điều phối (trạng thái mong muốn so với thực tế), mô hình bảo mật, và biết lúc nào **không** cần container.
> **Cần biết trước:** dòng lệnh cơ bản, khái niệm tiến trình và hệ điều hành. Chưa cần biết Docker.
> **Thời lượng ước tính:** 20 đến 28 giờ nếu làm đủ lab.
> **Loại:** khái niệm. Công cụ thực hành: [lộ trình Docker](../docker/README.md), chỉ là **một trong các lựa chọn** (xem [Bản đồ giải pháp](part-1-fundamentals.md#bản-đồ-giải-pháp)).

## Phạm vi: Docker KHÔNG đại diện cho container

Lộ trình này dạy khái niệm; lab chạy bằng Docker vì đó là công cụ có sẵn trên máy này, nhưng:
- **Container không phải Docker.** Podman, containerd, CRI-O, runc và nhiều công cụ khác chạy cùng một loại container nhờ chuẩn OCI.
- **Lab Docker đã có ở [lộ trình Docker](../docker/README.md)** (namespaces, cgroups, vòng đời, build, giới hạn tài nguyên, siết quyền, digest, SBOM). Lộ trình này **liên kết sang**, không chạy lại, và chỉ thêm lab **khác**: đọc `/proc` bên trong container, đọc một image theo chuẩn OCI bằng tay (manifest, layer, whiteout), và một **mô hình** reconciler cho khái niệm điều phối.
- **Docker Desktop trên Windows chạy container trong một máy ảo Linux (WSL2)**, nên "kernel của container" là kernel của máy ảo đó, không phải Windows. Lab ghi lại điều này.
- Kubernetes, ECS, Nomad chỉ ở mức khái niệm; mô hình reconciler không chứng minh hành vi của chúng.

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
| [1 Fundamentals](part-1-fundamentals.md) | container là gì, so với VM, namespaces, cgroups, filesystem phân lớp, Bản đồ giải pháp | Giải thích container không dùng từ "máy ảo nhỏ" |
| [2 Core skills](part-2-core-skills.md) | image so với container, tag so với digest, vòng đời, dữ liệu, cấu hình | Dùng container mà không mất dữ liệu hay tái lập sai |
| [3 Multi-component](part-3-multi-component.md) | mô hình điều phối: reconcile, restart và backoff, rolling update | Hiểu điều phối làm gì và vì sao |
| [4 Advanced](part-4-advanced.md) | image bên trong theo OCI, runtime nhiều tầng, rootless | Đọc được một image bằng tay |
| [5 Production](part-5-production-security-ops.md) | mô hình bảo mật, capabilities, seccomp, giới hạn, chi phí | Đánh giá rủi ro khi chạy container |
| [6 Projects](part-6-projects.md) | `board-service` đối chiếu checklist "sẵn sàng chạy container" | Áp lên TeamBoard |
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
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi dùng container ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, Docker 29.5.3 (Docker Desktop; engine `linux/amd64` chạy trong WSL2, kernel `6.18.33.1-microsoft-standard-WSL2`), image `alpine:latest` (Alpine 3.24.3) đã có sẵn trên máy, .NET SDK 10.0.402 (runtime 10.0.13) cho mô hình reconciler, Node 24 cho script đọc image. Container và image của lab có tiền tố `lh-`; không kéo thêm image, không `prune`. Số đo thời gian chỉ tương đối.
**Chưa chạy (⚠):** Kubernetes, ECS, Nomad (chỉ qua tài liệu); runc trực tiếp (không có sẵn ngoài Docker); Podman và rootless thật; seccomp và capabilities ở mức thử nghiệm (đã có ở lộ trình Docker); quét lỗ hổng.

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 (hoặc liên kết sang lab Docker tương ứng) và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: giải thích container khác VM ở đâu, vì sao khởi động nhanh và cái giá của việc chung kernel.
- [ ] L3: kể tám loại namespace, vai trò cgroup, cấu trúc image (manifest, config, layer) và vì sao xoá file ở layer sau không xoá nó khỏi image.
- [ ] L4: đọc `/proc` trong container, mở một image và kiểm digest, chỉ ra whiteout.
- [ ] L5: từ triệu chứng (OOMKilled, thoát ngay, mất dữ liệu, image phình, tag đổi) tìm nguyên nhân gốc.
- [ ] L6: nêu mô hình bảo mật, capabilities, rootless, giới hạn, chi phí.
- [ ] L7: chọn giữa VM, container, serverless, PaaS; Docker, Podman, containerd; Kubernetes, ECS.
- [ ] L8: thiết kế cách đóng gói và chạy một dịch vụ kèm đánh đổi.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Nguồn sự thật của dự án: ADR 0003 (ECS Fargate), `.agents/rules/docker.md`, `dependency-policy.md`; lộ trình **không** sửa các tài liệu đó và không sửa `src/`.
