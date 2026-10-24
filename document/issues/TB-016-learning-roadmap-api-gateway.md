---
key: TB-016
title: Lộ trình học API Gateway và YARP theo Part 0–7 × L1–L8
status: review
phase: 0
branch: feature/TB-016-learning-roadmap-api-gateway
estimate: L
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-016 — Lộ trình học API Gateway và YARP theo Part 0–7 × L1–L8

## Bối cảnh
Gateway là thành phần dự án đang xây (TB-007 spec và design, TB-010 skeleton routing). Hai ghi chú `api-gateway.md` (khái niệm) và `yarp.md` (công cụ) chỉ là một file mỗi cái. Người dùng chọn lộ trình đủ Part 0–7: các cơ chế gateway mỗi cái một thẻ có use case, lab YARP chạy thật.

Phụ thuộc: TB-013, TB-014, TB-015 chưa push. Nhánh này cắt từ nhánh TB-015; PR mở với base là nhánh ngay dưới.

## Phạm vi
- Làm: lộ trình `document/learning/api-gateway/` (Part 0–7) cho khái niệm API Gateway, dùng YARP làm công cụ thực hành; lab chạy thật bằng .NET SDK trong thư mục thử ngoài repo; thay `api-gateway.md` và `yarp.md`, sửa link và mục lục.
- Không làm: sửa `src/`, `config/` hoặc tài liệu spec/design/plan của gateway; thêm gói vào `config/dependencies.yaml`; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ và mục phạm vi của YARP; mỗi ô ✓ có đúng một mục `## Lx` (script đạt).
- [x] Mỗi cơ chế gateway một thẻ có Use case; Bản đồ giải pháp có ≥3 lựa chọn kể cả không dùng gateway.
- [x] Mọi lab chạy thật có đầu ra ghi lại, hoặc gắn ⚠ chưa chạy; thư mục thử nằm ngoài repo.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; không còn link tới hai file cũ; case đã kiểm của hai file cũ được giữ lại.
- [x] Không mâu thuẫn với design TB-007; chỗ lệch thì báo, không tự sửa.

## Kế hoạch
1. Issue và nhánh (file này).
2. Đọc tài liệu nội bộ; nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab YARP thật với upstream giả ngoài repo.
4. Viết Part 0–7.
5. Hợp nhất hai file cũ, sửa link, mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue theo kế hoạch người dùng duyệt (plan mode). Quyết định: một lộ trình khái niệm với YARP là công cụ thực hành, thay cả hai file cũ.
- 2026-10-23: viết xong Part 0–7, `git rm` `api-gateway.md` và `yarp.md`, sửa link ở `microservice.md`, `topic-map.md`, mục lục. Script kiểm chứng đạt cho api-gateway, valkey, caching, docker. Không dùng Docker. Chưa chạy (⚠): TLS, gateway sau ALB, nhiều instance gateway thật, token bucket, session affinity, dự án 6.2 và 6.3. Chưa commit.
- Điểm khác giữa YARP và design TB-007 (báo, không tự sửa; chi tiết ở Part 6): method không hỗ trợ trả 405 thay vì 404; `Timeout` của route cho 400 thay vì 504; `MaxRequestBodySize` trả 413 nhưng upstream vẫn thấy yêu cầu.

- 2026-10-25: mở PR gộp [sowndv02/teamboard#10](https://github.com/sowndv02/teamboard/pull/10) (TB-013 đến TB-019), trạng thái review.

## Bài học (lessons learned)

- Chưa có.
