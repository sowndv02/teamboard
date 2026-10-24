---
key: TB-018
title: Lộ trình học Container (khái niệm gốc) theo Part 0–7 × L1–L8
status: review
phase: 0
branch: feature/TB-018-learning-roadmap-container
estimate: L
created: 2026-10-24
started: 2026-10-24
closed: null
---

# TB-018 — Lộ trình học Container (khái niệm gốc) theo Part 0–7 × L1–L8

## Bối cảnh
Lộ trình Docker (TB-013) dạy công cụ. Khái niệm container (cô lập tiến trình, image và OCI, mô hình điều phối) vẫn nằm trong một ghi chú `container.md`. Cần một lộ trình khái niệm tách khỏi Docker, liên kết sang lab Docker đã có thay vì lặp lại.

Phụ thuộc: TB-013 đến TB-017 chưa push. Nhánh này cắt từ nhánh TB-017; PR mở với base là nhánh ngay dưới.

## Phạm vi
- Làm: lộ trình `document/learning/container/` (Part 0–7) cho khái niệm; lab mới khác Docker course (mô phỏng reconciler không cần Docker; đọc image theo chuẩn OCI nếu Docker bật); thay `container.md`, sửa link và mục lục.
- Không làm: sửa `src/`, `config/`, ADR; lặp lại lab đã có ở lộ trình Docker; cài thêm công cụ; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Part 0 có ma trận phủ và mục phạm vi (Docker không đại diện container); mỗi ô ✓ có đúng một mục `## Lx` (script đạt).
- [x] Mỗi cơ chế một thẻ có Use case; Bản đồ giải pháp có ≥3 lựa chọn kể cả không dùng container.
- [x] Không lab trùng với lộ trình Docker (chỉ liên kết); mô phỏng ghi rõ là mô hình; lab Docker có đầu ra thật hoặc ⚠.
- [x] Không ô bảng quá ~140 ký tự, không bảng quá 5 cột, không câu kể quy trình trong thân bài.
- [x] Link `.md` và neo không gãy; không còn link tới `container.md` cũ; case đã kiểm của file cũ được giữ lại.

## Kế hoạch
1. Issue và nhánh (file này).
2. Đọc tài liệu nội bộ; nghiên cứu (3 agent chỉ đọc), tự mở lại nguồn then chốt.
3. Lab: mô phỏng reconciler (.NET); đọc image theo OCI (cần Docker).
4. Viết Part 0–7.
5. Hợp nhất file cũ, sửa link, mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-24: tạo issue theo kế hoạch người dùng duyệt (plan mode). Docker Desktop đang tắt lúc bắt đầu; người dùng bật và duyệt dùng image có sẵn (`alpine:latest`), không kéo thêm.
- 2026-10-24: viết xong Part 0–7, `git rm` `container.md`, sửa link ở `docker/README.md`, `docker/part-1`, `docker/part-4`, `topic-map.md`, mục lục. Script kiểm chứng đạt cho 6 lộ trình. Lab mới: `/proc` trong container, đọc image OCI bằng tay (manifest, config, layer, whiteout), mô hình reconciler (không Docker), capabilities và seccomp từ `/proc`, lớp ghi và volume. Lab trùng với lộ trình Docker chỉ liên kết. Chưa chạy (⚠): Kubernetes, ECS, Nomad, runc trực tiếp, Podman và rootless, quét lỗ hổng, bài tập 6.2 và 6.3. Chưa commit.
- Phát hiện khi chạy: `docker image inspect` báo Size 125 MB còn `docker images` báo 455 MB cho cùng `board-service:latest` (hai cách đo, chưa điều tra); `seccomp=unconfined` và `--privileged` vẫn còn một bộ lọc seccomp trong Docker Desktop (chưa rõ nguồn). Điểm lệch với `.agents/rules/docker.md` (báo ở Part 6): thiếu HEALTHCHECK, `EXPOSE 8080` không khớp cổng 7070, `.dockerignore` chưa có `.env.*`.

- 2026-10-25: mở PR gộp [sowndv02/teamboard#10](https://github.com/sowndv02/teamboard/pull/10) (TB-013 đến TB-019), trạng thái review.

## Bài học (lessons learned)

- Chưa có.
