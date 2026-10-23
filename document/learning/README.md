# Thư viện kiến thức

Ghi chú gom kiến thức theo chủ đề (vấn đề, cách dùng, ưu/nhược, thực tế có nguồn, áp dụng trong dự án), tạo bằng skill `learning-hub` (nói "gom tài liệu về <chủ đề>"). Ghi chú trỏ về nguồn sự thật (ADR, rule, spec, code); lệch nhau thì nguồn gốc thắng.

**Quy ước:**
- **Khái niệm trước, công cụ sau; công cụ không đại diện khái niệm.** Ghi chú khái niệm có "Bản đồ giải pháp" (≥3 lựa chọn, kể cả "không dùng"); ghi chú công cụ có mục "Phạm vi và những gì công cụ này KHÔNG đại diện" và trỏ về khái niệm.
- Mục real-world phải có ≥3 case có nguồn đã mở. Mức: **Chắc** = tôi tự mở nguồn; **Vừa** = chỉ agent nghiên cứu đọc.
- Cột "Real-world" ghi chú chưa đạt chuẩn.

## Khái niệm

| Chủ đề | Ghi chú | Bản đồ giải pháp | Làm mới | Real-world |
|---|---|---|---|---|
| Container | [container.md](container.md) | Có (runtime, build, điều phối, không dùng container) | 2026-10-23 | Đạt (Borg, OCI, Kaniko tự mở; dockershim Vừa) |
| Caching (lộ trình Part 0–7) | [caching/README.md](caching/README.md) | Có (trình duyệt, CDN, trong tiến trình, HybridCache, Redis/Valkey, Memcached, DB, không cache) | 2026-10-23 | Đạt (Facebook, Cloudflare, AWS, Uber, Meta, Discord, Cloudbleed tự mở; Part 3 Lab Valkey và Part 6 dự án 6.2/6.3 chưa chạy) |
| API Gateway | [api-gateway.md](api-gateway.md) | Có (thư viện proxy, proxy đa năng, plugin, managed, LB L7, K8s, mesh ingress, không dùng) | 2026-10-23 | Đạt (Azure, Microsoft, Uber, Stripe tự mở; Zuul, K8s Gateway API Vừa; chưa có postmortem công khai) |
| Kiến trúc microservice | [microservice.md](microservice.md) | Có (monolith, modular monolith, microservice, DOMA, serverless, strangler) | 2026-10-23 | Đạt (Segment, Shopify, Uber tự mở; Prime Video chỉ thứ cấp) |

## Công cụ

| Chủ đề | Một trong các lựa chọn của | Ghi chú | Làm mới | Real-world |
|---|---|---|---|---|
| Docker, Docker Compose (lộ trình Part 0–7) | [Container](container.md) | [docker/README.md](docker/README.md) | 2026-10-23 | Đạt một phần (OCI, Borg, arXiv, ECS tự mở; Part 6 chưa chạy lab container; xem từng Part) |
| Redis / Valkey | [Caching](caching/README.md) | [redis.md](redis.md) | 2026-10-23 | Đạt (GitHub, Redis giấy phép, Valkey tự mở; Stack Overflow, Slack, AWS Vừa) |
| YARP | [API Gateway](api-gateway.md) | [yarp.md](yarp.md) | 2026-10-23 | Đạt một phần (YARP 1.0, eShopOnContainers tự mở; hai nguồn còn lại Vừa) |
