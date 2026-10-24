---
key: TB-015
title: Lộ trình học Redis / Valkey (công cụ) theo Part 0–7 × L1–L8
status: review
phase: 0
branch: feature/TB-015-learning-roadmap-valkey
estimate: L
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-015 — Lộ trình học Redis / Valkey (công cụ) theo Part 0–7 × L1–L8

## Bối cảnh
TB-014 dạy khái niệm Caching. Redis/Valkey là công cụ cụ thể nằm trong Bản đồ giải pháp của Caching và hiện chỉ có một ghi chú `redis.md`. Người dùng chọn lộ trình đủ Part 0–7: kiểu dữ liệu kèm use case, persistence, replication, cluster, vận hành, lab chạy thật.

Phụ thuộc: TB-014 (và TB-013) chưa push. Nhánh này cắt từ nhánh TB-014; PR mở với base là nhánh TB-014 và đổi sang `main` khi các nhánh dưới đã merge.

## Phạm vi
- Làm: lộ trình `document/learning/valkey/` (Part 0–7) cho **công cụ** Redis/Valkey, không lặp khái niệm cache; lab chạy thật trong thư mục thử ngoài repo (Docker `lh-valkey*` và .NET); thay `redis.md` bằng lộ trình, sửa link và mục lục; cập nhật Lab 3.3 của Caching Part 3 nếu chạy được.
- Không làm: sửa `src/` hoặc `config/`; thêm gói vào `config/dependencies.yaml`; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ và mục phạm vi công cụ; mỗi ô ✓ có đúng một mục `## Lx` (script đạt).
- [x] Mỗi kiểu dữ liệu và mỗi cơ chế (persistence, replication, cluster...) một thẻ có Use case.
- [x] Mọi lab chạy thật có đầu ra ghi lại, hoặc gắn ⚠ chưa chạy; thư mục thử nằm ngoài repo.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; không còn link tới `redis.md` cũ; case đã kiểm của file cũ được giữ lại.

## Kế hoạch
1. Issue và nhánh (file này).
2. Nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab thật: Valkey trong Docker (cần bật Docker Desktop và duyệt kéo image) và .NET.
4. Viết Part 0–7.
5. Hợp nhất `redis.md`, sửa link, mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue theo kế hoạch người dùng duyệt (plan mode). Docker Desktop đang tắt lúc bắt đầu; người dùng bật và duyệt kéo `valkey/valkey:9.1.2-alpine`.
- 2026-10-23: viết xong Part 0–7, thay `redis.md` bằng `document/learning/valkey/`, sửa link ở `yarp.md`, `caching/*`, mục lục; cập nhật Lab 3.3 của Caching Part 3 bằng Valkey thật. Script kiểm chứng đạt cho valkey, caching, docker. Chưa chạy (gắn ⚠): Sentinel, Cluster nhiều node, TLS, ElastiCache/MemoryDB, dự án 6.3. Container, volume, mạng `lh-` đã dọn; image giữ lại. Chưa commit.
- Phát hiện khi chạy: `DEL` trên big key 3M phần tử mất 25 µs với `lazyfree-lazy-user-del yes` (mặc định 9.1.2) và ~240 ms khi `no`; replica mới chưa đồng bộ trong 2 giây đầu (diskless delay).

- 2026-10-25: mở PR gộp [sowndv02/teamboard#10](https://github.com/sowndv02/teamboard/pull/10) (TB-013 đến TB-019), trạng thái review.

## Bài học (lessons learned)

- Chưa có.
