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
| Caching | [caching.md](caching.md) | Có (trình duyệt, CDN, trong tiến trình, HybridCache, Redis/Valkey, Memcached, DB, không cache) | 2026-10-23 | Đạt (Facebook, Cloudflare, AWS tự mở; Discord là sự cố) |
| API Gateway | [api-gateway.md](api-gateway.md) | Có (thư viện proxy, proxy đa năng, plugin, managed, LB L7, K8s, mesh ingress, không dùng) | 2026-10-23 | Đạt (Azure, Microsoft, Uber, Stripe tự mở; Zuul, K8s Gateway API Vừa; chưa có postmortem công khai) |
| Kiến trúc microservice | [microservice.md](microservice.md) | Có (monolith, modular monolith, microservice, DOMA, serverless, strangler) | 2026-10-23 | Đạt (Segment, Shopify, Uber tự mở; Prime Video chỉ thứ cấp) |

## Công cụ

| Chủ đề | Một trong các lựa chọn của | Ghi chú | Làm mới | Real-world |
|---|---|---|---|---|
| Docker, Docker Compose | [Container](container.md) | [docker-compose.md](docker-compose.md) | 2026-10-23 | Đạt tối thiểu (3 tài liệu Docker tự mở; Microsoft .NET ở mức Vừa; thiếu case công ty tự mở) |
| Redis / Valkey | [Caching](caching.md) | [redis.md](redis.md) | 2026-10-23 | Đạt (GitHub, Redis giấy phép, Valkey tự mở; Stack Overflow, Slack, AWS Vừa) |
| YARP | [API Gateway](api-gateway.md) | [yarp.md](yarp.md) | 2026-10-23 | Đạt tối thiểu (YARP 1.0, eShopOnContainers tự mở; hai nguồn còn lại Vừa) |
