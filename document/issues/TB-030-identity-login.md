---
key: TB-030
title: Identity lát 2, đăng nhập với lỗi chung không lộ email
status: in-progress
phase: 1
branch: feature/TB-030-identity-login
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-030 — Identity lát 2, đăng nhập với lỗi chung không lộ email

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 2 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1b. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-002, BR-003): Đăng nhập, lỗi chung không lộ email
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `POST /api/v1/auth/login`: credential đúng → 200 `{ "id" }`, không JWT/hash/password (FR-002, @S01)
- [ ] Sai mật khẩu / email không tồn tại → 401 `INVALID_CREDENTIALS`, cùng `title` (BR-003, @S02, @S03, @S08)
- [ ] Email/mật khẩu không hợp lệ → 400 `VALIDATION_FAILED` (@S05, @S06)
- [ ] Đăng ký `a@x.com`, login `A@x.com` → 200 (@S04, BR-001)
- [ ] Không lộ mật khẩu trong log (@S07, BR-007)
- [ ] Metric `login_failures_total` sau 401 (@S09)
- [ ] Gate xanh đầy đủ (plan-review)

## Kế hoạch
Plan: [`document/plans/TB-030-identity-login.md`](../plans/TB-030-identity-login.md) (`approved`, B1–B3, @S01–@S09; Confirm #1–#4).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-25: TB-029 merged PR #24; `plan-init` trên nhánh `feature/TB-030-identity-login` từ `main` `068212c`.
- 2026-11-25: plan-review PASS vòng 1–2 (Confirm #1–#4).
- 2026-11-25: test-init — `login.feature` @S01–@S09, steps; `dotnet test` 9 login đỏ (404/metric), 18 hồi quy xanh.
- 2026-11-25: test-review v1 PASS — 9 đỏ đúng lý do (404); tag @S06/@S09; B3 timing không BDD.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-030-identity-login.md` draft B1–B3, P-01..P-04 |
| plan-review | PASS | 2 | 2026-11-25 | Confirm #1–#4; plan `approved` |
| test-init | done | – | 2026-11-25 | `login.feature` @S01–@S09; 9 đỏ 404; TB-029/021 18 xanh |
| test-review | PASS | 1 | 2026-11-25 | Sẵn sàng code; @S08 hai When (accepted) |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-030:login-path | Đường API đăng nhập | `POST /api/v1/auth/login` | 1 | 2026-11-25 |
| 2 | plan-review:TB-030:login-success-body | 200 trước JWT | `{ "id": "<uuid>" }` | 1 | 2026-11-25 |
| 3 | plan-review:TB-030:credential-error | 401 credential | `INVALID_CREDENTIALS` + title cố định | 1 | 2026-11-25 |
| 4 | plan-review:TB-030:client-ip | IP client | `RemoteIpAddress`, không DB | 1 | 2026-11-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | `login.feature` + `IdentityServiceSteps` | 404 | |
| @S02 | same | 404 | |
| @S03 | same | 404 | |
| @S04 | same | 404 | |
| @S05 | same | 404 | |
| @S06 | same | 404 | |
| @S07 | same + `CapturingLoggerProvider` | 404 (assert 401) | |
| @S08 | same (2 When login) | 404 | |
| @S09 | `MeterListener` login_failures_total | 0 counter | |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | working tree | Sẵn sàng code | – |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | test:identity-service:s06-tag | FIX | login.feature @S06 | Tag @FR-002 sai (validation) | Bỏ @FR-002 | feature |
| 1 | test:identity-service:s09-tag | FIX | login.feature @S09 | Metric nên @NFR-007 | Đổi tag | register @S09 pattern |
| 1 | test:identity-service:s08-double-when | P3 | login.feature @S08 | Hai `When` trong một scenario | `skipped`: so sánh BR-003 trong một flow (plan @S08) | plan mục 2 |
| 1 | plan-review:TB-030:b3-timing | P3 | plan B3 | Bảng truy vết @S07 timing vs feature log | `skipped`: B3 không BDD timing; kiểm coding-review | plan mục 5 rủi ro |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
