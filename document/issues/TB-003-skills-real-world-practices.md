---
key: TB-003
title: Đưa skill, rule và gate workflow sát thực hành dự án thực tế (làn, chia lát, Angular)
status: review
phase: 0
branch: feature/TB-003-skills-real-world-practices
estimate: M
created: 2026-10-21
started: 2026-10-21
closed: null
---

# TB-003 — Đưa skill và rule workflow sát thực hành dự án thực tế

## Bối cảnh
Dự án học tập nhưng cần luyện đúng thói quen dự án thật để không bỡ ngỡ. Skill cũ đưa mọi thay đổi qua một quy trình nặng, thiếu thực hành sản xuất (rollout/rollback, contract test, SLO) và thiếu cách làm việc với khách (change request, UAT, bàn giao).

## Phạm vi
- Làm: (1) làn công việc nhỏ/vừa/lớn/hotfix (`workflow/references/lanes.md`) và bổ sung 15 skill theo nhóm quy trình nhóm, kỹ thuật, khách hàng, giảm nghi thức; (2) bước chia lát spec lớn thành lát nhỏ (`slicing.md`); (3) ADR 0009 làm lại FE Angular có giới hạn, 4 rule + 3 skill + gate Angular (chưa scaffold `src/web`); sửa tối thiểu `pipeline-lifecycle.md`, `git-workflow.md`, `quality-gate.md`, `code-change-gate.md`, `COMMANDS.md`, config; thêm `pullRequest.mergeStrategy` (giá trị `TODO`).
- Không làm: không đổi code ứng dụng; không chọn giá trị merge strategy; không đụng spec/workflow của identity-auth.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Mọi `references/` được skill nhắc đến đều tồn tại
- [x] `description` trong `.claude/skills` khớp `.agents/skills`
- [x] Rule và skill không mâu thuẫn về làn, giới hạn vòng review
- [ ] Người dùng chốt `mergeStrategy`
- [ ] Chạy thử giấy trên một bug giả định (làn nhỏ); lát đăng nhập identity-auth qua `angular-feature` → `frontend-review` (sau khi MR merge, identity-auth base lại từ main)
- [x] ADR 0009 được chủ dự án duyệt (2026-10-21)

## Kế hoạch
Xem kế hoạch đã duyệt: nhóm A (làn), B (create-pr, coding-review, PM), C (kỹ thuật), D (khách hàng), E (lesson-learned).

## Ghi chú / Nhật ký
- 2026-10-21: làm theo làn lớn rút gọn (tài liệu, không có code); chưa chạy thử giấy.

## Bài học (lessons learned)

- Không có.
