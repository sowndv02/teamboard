---
key: TB-014
title: Lộ trình học Caching (khái niệm gốc) theo Part 0–7 × L1–L8
status: review
phase: 0
branch: feature/TB-014-learning-roadmap-caching
estimate: L
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-014 — Lộ trình học Caching (khái niệm gốc) theo Part 0–7 × L1–L8

## Bối cảnh
TB-013 dựng mô hình lộ trình hai chiều và làm mẫu trên Docker. Người dùng chọn Caching làm chủ đề tiếp theo với đủ Part 0–7. Ghi chú `caching.md` hiện là một file; các cơ chế cache cần được dạy rõ từng cái, kèm use case và lab chạy thật.

Phụ thuộc: skill v3 của TB-013 (chưa push). Nhánh này cắt từ nhánh TB-013; PR mở với base là nhánh TB-013 và đổi sang `main` khi TB-013 merge.

## Phạm vi
- Làm: lộ trình `document/learning/caching/` (Part 0–7) cho **khái niệm** Caching; lab chạy thật bằng .NET SDK 10.0.402 trong thư mục thử ngoài repo; thay `caching.md` bằng lộ trình, sửa link liên quan và mục lục.
- Không làm: sửa code `src/` hoặc `config/`; thêm gói vào `config/dependencies.yaml`; lộ trình Redis/Valkey (bước sau); commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ; mỗi ô ✓ có đúng một mục `## Lx`, mỗi ô `–` có lý do (script đối chiếu đạt).
- [x] Part 1 có sáu cơ chế đọc/ghi, mỗi cơ chế một thẻ có Use case, cùng "Bản đồ giải pháp" (≥3 lựa chọn, kể cả không cache).
- [x] Mọi lab L4/L5 chạy thật với đầu ra ghi lại, hoặc gắn ⚠ chưa chạy; thư mục thử nằm ngoài repo.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` chéo và neo không gãy; không còn link tới `caching.md` cũ; nội dung đã kiểm của file cũ (Facebook, Cloudflare, AWS, Discord, Honeycomb, GitHub) được giữ lại ở Part phù hợp.

## Kế hoạch
1. Issue và nhánh (file này).
2. Nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab .NET thật: HTTP cache, `IMemoryCache`, stampede, `HybridCache`, hit ratio; Valkey nếu Docker bật và được phép.
4. Viết Part 0–7.
5. Hợp nhất `caching.md`, sửa link, mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue theo kế hoạch người dùng duyệt (plan mode). Quyết định: chủ đề Caching (khái niệm gốc), đủ Part 0–7.
- 2026-10-23: viết xong Part 0–7, thay `caching.md`, sửa link ở `redis.md`, `microservice.md`, `topic-map.md`, mục lục. Script kiểm chứng (ma trận, khuôn Part, bảng, link/neo) đạt. Chưa chạy: Lab 3.3 (Valkey thật, Docker tắt, chưa duyệt kéo image) và Dự án 6.2/6.3 (bài tập chưa chạy trên `board-service`); cả hai gắn ⚠ trong tài liệu. Chưa commit.
- Cần duyệt riêng khi tới Part 3: kéo image Valkey và bật Docker Desktop cho lab phân tán; gói NuGet `Microsoft.Extensions.Caching.StackExchangeRedis` chỉ dùng trong lab ngoài repo.

- 2026-10-25: mở PR gộp [sowndv02/teamboard#10](https://github.com/sowndv02/teamboard/pull/10) (TB-013 đến TB-019), trạng thái review.

## Bài học (lessons learned)

- Chưa có.
