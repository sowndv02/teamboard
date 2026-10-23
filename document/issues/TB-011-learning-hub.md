---
key: TB-011
title: Skill learning-hub gom kiến thức theo chủ đề vào document/learning
status: review
phase: 0
branch: feature/TB-011-learning-hub
estimate: S
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-011 — Skill learning-hub gom kiến thức theo chủ đề vào document/learning

## Bối cảnh
Dự án phục vụ học tập, nhưng kiến thức (docker, compose, API gateway...) nằm rải rác ở rule, ADR, spec, design và code. Người học cần một nơi đọc theo chủ đề, có giải thích để nắm kiến thức mới.

## Phạm vi
- Làm: skill `learning-hub` (canonical ở `.agents/skills/`, bản trỏ ở `.claude/skills/`) với bản đồ chủ đề `references/topic-map.md`; thư mục `document/learning/` gồm mục lục và hai ghi chú đầu tiên (docker và compose, API gateway/YARP).
- Không làm: sửa code hay tài liệu nguồn; sửa các điểm lệch ghi chú tìm thấy (compose develop trỏ sai host DB, `.dockerignore` thiếu `.env.*`...), sẽ báo riêng.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Skill có mẫu ghi chú 10 mục (vấn đề, cách dùng, ưu/nhược, real-world, áp dụng trong dự án).
- [x] Ghi chú docker-compose và api-gateway tạo theo mẫu, đường dẫn trỏ tới file có thật.
- [x] `docker compose config -q` đã chạy qua cho các file compose được ghi chú nêu.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue, skill và hai ghi chú đầu tiên.
- 2026-10-23: mở PR [#7](https://github.com/sowndv02/teamboard/pull/7), chuyển sang review.

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
