---
key: TB-002
title: Thiết lập quy tắc agent, workflow và tài liệu nền cho dự án
status: in-progress
phase: 0
branch: feature/TB-002-agent-rules-and-workflow
estimate: L
created: 2026-10-21
started: 2026-10-21
closed: null
---

# TB-002 — Thiết lập quy tắc agent, workflow và tài liệu nền cho dự án

## Bối cảnh
Chuẩn bị nền để làm việc với AI agent theo quy trình thống nhất trước khi viết code: kế hoạch kiến trúc, ADR, quy tắc, skill workflow, mẫu tài liệu (spec, design, plan, infra), cấu hình dự án.

## Phạm vi
- Làm: `AGENTS.md`, `CLAUDE.md`, `PROJECT.md`, `COMMANDS.md`, `config/`, `.agents/rules/`, `.agents/skills/` (+ `.claude/skills/`), `document/` (architecture, issues, spec, design, plans, infra, security, workflow), `.gitignore`, gỡ file `.env` khỏi git.
- Không làm: code service, Terraform, cài công cụ gate (analyzer, gitleaks, commitlint).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Rule, skill, mẫu tài liệu và cấu hình nằm đúng vị trí, tham chiếu chéo không chết
- [x] `.env` thật không còn bị theo dõi; có file `*.example`
- [ ] Người dùng review và chấp nhận bộ quy tắc; các `TODO` trong `config/` được chốt dần

## Kế hoạch
1. Gitignore và gỡ file env.
2. Tài liệu kiến trúc, config, issue.
3. Rule agent.
4. Skill workflow và mẫu.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | n/a | – | | Việc thiết lập, không qua workflow đầy đủ |
| create-pr | pending | – | | Chờ người dùng |

## Ghi chú / Nhật ký
- Các giá trị chưa quyết định để `TODO`: `quality.monthlyBudgetUsd`, `quality.compliance`, region/account AWS, phiên bản Terraform, BDD runner.
- Mật khẩu trong `src/.env.develop` và `src/.env.stg` từng được commit; cần xoay nếu là thật.

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- [LL-001](../lessons/LL-001-secrets-committed-in-env-files.md): không commit file `.env` chứa mật khẩu
- [LL-002](../lessons/LL-002-tool-license-assumed-before-checking.md): kiểm tra giấy phép công cụ trước khi đưa vào thiết kế
- [LL-003](../lessons/LL-003-tool-default-trailer-overrode-project-preference.md): rule dự án im lặng thì hỏi hoặc ghi rule, không theo mặc định của công cụ
