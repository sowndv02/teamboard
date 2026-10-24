---
key: TB-019
title: Lộ trình học Redis (phần riêng) và ghi chú so sánh Redis với Valkey
status: in-progress
phase: 0
branch: feature/TB-019-learning-roadmap-redis
estimate: L
created: 2026-10-24
started: 2026-10-24
closed: null
---

# TB-019 — Lộ trình học Redis (phần riêng) và ghi chú so sánh Redis với Valkey

## Bối cảnh
Lộ trình Valkey (TB-015) dạy Valkey và gọi chung là "Redis/Valkey". Người dùng muốn một lộ trình riêng cho Redis và một ghi chú so sánh Redis với Valkey. Chính sách dự án: Redis server `banned` (T3), Valkey `watch`; lộ trình là học tập, không đổi chính sách.

Phụ thuộc: TB-013 đến TB-018 chưa push. Nhánh này cắt từ nhánh TB-018; PR mở với base là nhánh ngay dưới.

## Phạm vi
- Làm: lộ trình `document/learning/redis/` (Part 0–7) chỉ về **phần riêng hoặc khác của Redis**, liên kết sang lộ trình Valkey cho cơ chế chung; ghi chú `document/learning/redis-vs-valkey.md`; lab chạy thật trong thư mục thử ngoài repo (container `lh-redis*`).
- Không làm: sửa `src/`, `config/`, `dependency-policy.md`; thêm Redis vào compose hay `dependencies.yaml`; lặp lại nội dung lộ trình Valkey; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ, mục phạm vi và cảnh báo giấy phép lẫn chính sách dự án (script đạt).
- [x] Mỗi tính năng riêng của Redis một thẻ có Use case; bảng so sánh mỗi dòng có nguồn và mức xác minh.
- [x] Khẳng định "chỉ có ở một bên" được kiểm bằng lệnh trên image thật hoặc gắn ⚠.
- [x] Không lab trùng lộ trình Valkey (chỉ liên kết); không tuyên bố hiệu năng so sánh nếu không đo cùng điều kiện.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; tài nguyên `lh-` đã dọn.

## Kế hoạch
1. Issue và nhánh (file này).
2. Nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab thật với image Redis (cần Docker bật và duyệt kéo image).
4. Viết Part 0–7 và ghi chú so sánh.
5. Liên kết hai chiều, mục lục, topic-map, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-24: tạo issue theo kế hoạch người dùng duyệt (plan mode); người dùng duyệt kéo `redis:8.10.2-alpine` và `redis:7.2.16-alpine`.
- 2026-10-24: viết xong Part 0–7 của `document/learning/redis/` và `document/learning/redis-vs-valkey.md`; thêm một dòng liên kết ở `valkey/README.md`, mục lục, topic-map. Script đạt cho 7 lộ trình. Lab thật trên Redis 8.10.2, Redis 7.2.16, Valkey 9.1.2; container `lh-` đã dọn, image giữ lại. Chưa chạy (⚠): cluster và atomic slot migration, hiệu năng, `DELIFEQ`, Redis Software/Cloud, mô-đun Valkey, `DUMP`/`RESTORE` giữa hai engine. Chưa commit.
- Phát hiện khi chạy: RDB của Redis 8.10.2 (`REDIS0015`) không nạp được vào Valkey 9.1.2 dù chỉ có kiểu cơ bản; Valkey 9.1.2 ghi tiêu đề `VALKEY080` mà Redis không đọc; Valkey 9.1.2 có hash field expiration, `HSETEX`, `HGETEX`, `SET IFEQ` nhưng không có `DELEX` và mô-đun Redis 8.

## Bài học (lessons learned)

- Chưa có.
