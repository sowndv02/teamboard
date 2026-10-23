---
key: TB-013
title: Lộ trình học hai chiều (Part × L1–L8) cho learning-hub, mẫu trên Docker
status: in-progress
phase: 0
branch: feature/TB-013-learning-roadmap-docker
estimate: L
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-013 — Lộ trình học hai chiều (Part × L1–L8) cho learning-hub, mẫu trên Docker

## Bối cảnh
Ghi chú một file cho mỗi chủ đề (TB-011, TB-012) vẫn khó đọc: bảng dài 600–770 ký tự, nhãn nguồn chen giữa câu, nhiều chỗ liệt kê mà không có use case (ví dụ các kiểu dữ liệu Redis). Người dùng muốn mỗi chủ đề là một **lộ trình học** đủ sâu: chiều ngang là các Part kiến thức, chiều sâu là 8 tầng L1 What, L2 Why, L3 How, L4 Hands-on, L5 Troubleshooting, L6 Production, L7 Architecture, L8 Mastery.

Phụ thuộc: thay đổi skill của PR #9 (TB-012), đã merge vào `main` lúc 2026-10-23 05:04 UTC; nhánh này cắt từ `main` sau khi #9 merge.

## Phạm vi
- Làm: mô hình bài học (8 tầng, 8 Part, ma trận phủ, khuôn một Part, quy tắc trình bày) trong `references/` của skill `learning-hub`; lộ trình **Docker** (Part 0–7) trong `document/learning/docker/` làm mẫu, các lệnh L4/L5 chạy thật trên Docker cục bộ và ghi đầu ra thật; hợp nhất `docker-compose.md` cũ vào lộ trình; cập nhật mục lục.
- Không làm: áp mô hình cho các chủ đề khác (Redis, caching, YARP, microservice...) trước khi người dùng duyệt mẫu Docker; sửa code hoặc tài liệu nguồn (ADR, rule, spec, `config/`); chạy `docker system prune`, `docker volume rm` hay compose môi trường thật; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] `lesson-model.md` và `part-template.md` mô tả 8 tầng, 8 Part, ma trận phủ, khuôn Part, quy tắc trình bày; `SKILL.md` trỏ tới và có checklist đo được.
- [x] `document/learning/docker/` có Part 0 (README: lộ trình, prerequisite, ma trận phủ, tiêu chí hoàn thành, tự đánh giá) và Part 1–7; mỗi ô ✓ của ma trận có mục `## Lx` tương ứng, mỗi ô `–` có lý do (script đối chiếu đạt).
- [ ] Mọi lệnh L4/L5 đã chạy thật với đầu ra được ghi lại, hoặc gắn ⚠ chưa chạy; tài nguyên Docker tạo ra có tiền tố `lh-` và đã dọn. **Đạt một phần:** Part 1–5 đã chạy thật và dọn; Part 6 Lab 6.2, 6.3 và build từ Dockerfile còn ⚠ chưa chạy.
- [x] Không ô bảng nào quá ~140 ký tự (dài nhất 110), không bảng quá 5 cột, không câu kể quy trình trong thân bài; mọi thẻ khái niệm có **Use case**.
- [x] Link `.md` chéo và neo không gãy, không còn link tới `docker-compose.md` cũ (script đạt trên 15 file).

## Kế hoạch
1. Sửa các lỗi review PR #9 còn mở (đã làm, commit `docs(TB-013): fix review findings left over from PR 9`, vì #9 đã merge trước khi các sửa đó kịp vào) và đóng issue TB-012.
2. Issue và nhánh (file này).
3. Skill v3: `lesson-model.md`, `part-template.md`, cập nhật `SKILL.md` và `topic-map.md`.
4. Nghiên cứu Docker (3 agent chỉ đọc), tự mở lại nguồn then chốt.
5. Chạy thật lệnh Docker, ghi đầu ra.
6. Viết Part 0–7.
7. Mục lục, nhật ký, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue theo kế hoạch người dùng duyệt (plan mode). Quyết định đã chốt: kết hợp hai chiều; pilot Docker; giữ phân tầng khái niệm gốc/công cụ; issue riêng, PR #9 giữ nguyên.
- 2026-10-23: PR #9 (TB-012) đã merge lúc 05:04 UTC; dựng lại nhánh TB-013 từ `main`, cherry-pick bản sửa lỗi review thành commit `docs(TB-013): fix review findings left over from PR 9`, đóng issue TB-012.
- 2026-10-23: skill v3 (`lesson-model.md`, `part-template.md`, `SKILL.md`, `topic-map.md`); nghiên cứu Docker bằng 3 agent, tự mở lại nguồn then chốt (rootless, GitHub Actions cache, ECS circuit breaker, best practices, arXiv 2307.03958, Docker Hub, Kaniko).
- 2026-10-23: chạy thật lab Part 1–5 (Docker 29.5.3), ghi đầu ra thật; phát hiện và sửa cách trình bày ở các điểm: storage driver là `overlayfs` (không phải `overlay2`), BuildKit chỉ gửi file được `COPY` tham chiếu nên `.dockerignore` chỉ thấy tác dụng với `COPY . .`, `--init` thoát 143 chứ không phải 0, thứ tự ưu tiên biến Compose, `docker events` ghi mỗi lượt healthcheck là một `exec`.
- 2026-10-23: viết Part 0–7; hợp nhất `docker-compose.md` cũ vào lộ trình (`git rm`), sửa link và tên `teamboard-api` đã lỗi thời trong `container.md`, `microservice.md`, `topic-map.md`.
- 2026-10-23: Docker Desktop tắt giữa chừng nên Part 6 Lab 6.2, 6.3 chưa chạy; build lại image từ Dockerfile chờ người dùng duyệt pull SDK ~1 GB (câu hỏi bị gián đoạn, chưa có trả lời).
- Điểm lệch của repo, ghi nhận không sửa (Part 3, Part 6): `src/docker-compose.develop.yml:5` host `sqlserver` so với service `db-dev`; `Dockerfile:7` `EXPOSE 8080` so với 7070; `mssql/server:2022-latest`; chưa có `HEALTHCHECK`; `.dockerignore` thiếu `.env*` và `local/`; mật khẩu qua `-P` trong healthcheck.
- Còn lại: chạy Lab 6.2 và 6.3 khi Docker bật; quyết định build từ Dockerfile; áp mô hình lộ trình cho các chủ đề khác (Redis, caching, YARP, microservice) sau khi người dùng duyệt mẫu Docker.
- Việc pull image công khai nhỏ (`hello-world`, `alpine`, `nginx`) để kiểm lệnh đã được người dùng duyệt cùng kế hoạch.

## Bài học (lessons learned)

- Chưa có.
