---
key: TB-017
title: Lộ trình học Microservice (khái niệm) theo Part 0–7 × L1–L8
status: review
phase: 0
branch: feature/TB-017-learning-roadmap-microservice
estimate: L
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-017 — Lộ trình học Microservice (khái niệm) theo Part 0–7 × L1–L8

## Bối cảnh
Microservice là khái niệm nền của dự án (6 service bắt buộc, mỗi service một kiến trúc). Ghi chú `microservice.md` là một file; các cơ chế (database-per-service, outbox, idempotent consumer, saga, timeout/retry/circuit breaker) cần được dạy từng cái, kèm use case và lab chạy thật.

Phụ thuộc: TB-013 đến TB-016 chưa push. Nhánh này cắt từ nhánh TB-016; PR mở với base là nhánh ngay dưới.

## Phạm vi
- Làm: lộ trình `document/learning/microservice/` (Part 0–7) cho khái niệm; lab .NET mô phỏng trong tiến trình, ngoài repo; thay `microservice.md`, sửa link và mục lục.
- Không làm: sửa `src/`, `config/`, ADR, design hay spec; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ; mỗi ô ✓ có đúng một mục `## Lx` (script đạt).
- [x] Mỗi cơ chế một thẻ có Use case; Bản đồ giải pháp có ≥3 lựa chọn kể cả không dùng microservice; có mục khi KHÔNG nên dùng.
- [x] Mọi lab chạy thật có đầu ra ghi lại, hoặc gắn ⚠; mô phỏng ghi rõ là mô hình; thư mục thử ngoài repo.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; không còn link tới `microservice.md` cũ; case đã kiểm của file cũ được giữ lại.
- [x] Không mâu thuẫn ADR 0001 và 0002; chỗ lệch thì báo, không tự sửa.

## Kế hoạch
1. Issue và nhánh (file này).
2. Đọc tài liệu nội bộ; nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab mô phỏng .NET: gọi REST có timeout, dual write và outbox, consumer idempotent, saga, bulkhead, DLQ.
4. Viết Part 0–7.
5. Hợp nhất file cũ, sửa link, mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue theo kế hoạch người dùng duyệt (plan mode).
- 2026-10-23: viết xong Part 0–7, `git rm` `microservice.md`, sửa link ở `api-gateway/README.md`, `topic-map.md`, mục lục. Script kiểm chứng đạt cho microservice, api-gateway, valkey, caching, docker. Lab là mô phỏng trong một tiến trình (HTTP loopback thật, giao dịch SQLite thật, broker và saga là mô hình), ghi rõ ở Part 0. Chưa chạy (⚠): SNS/SQS, nhiều tiến trình, circuit breaker thật, saga có bồi hoàn tự lỗi, dự án 6.2 đa tiến trình và 6.3. Chưa commit.

- 2026-10-25: mở PR gộp [sowndv02/teamboard#10](https://github.com/sowndv02/teamboard/pull/10) (TB-013 đến TB-019), trạng thái review.

## Bài học (lessons learned)

- Chưa có.
