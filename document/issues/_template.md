---
key: TB-000
title: <tiêu đề ngắn>
status: todo
phase: <số giai đoạn trong lộ trình>
branch: feature/TB-000-<slug>
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: YYYY-MM-DD
started: null           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-000 — <tiêu đề>

## Bối cảnh
Vì sao cần làm. Link ADR hoặc tài liệu liên quan.

## Phạm vi
- Làm: ...
- Không làm: ...

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] ...

## Kế hoạch
1. ...

## Ghi chú / Nhật ký

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | pending | – | | |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
