---
key: TB-029
title: Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2
status: in-progress
phase: 1
branch: feature/TB-029-identity-register
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-029 — Identity lát 1b, đăng ký bằng email và mật khẩu băm PBKDF2

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 1b của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1a. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-001, FR-004, BR-001, BR-002, NFR-002, NFR-007 (metric băm)): Đăng ký bằng email/mật khẩu băm PBKDF2, benchmark số vòng và vCPU
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `POST` đăng ký (đường dẫn chốt ở plan-review): email/mật khẩu hợp lệ → 201, không trả mật khẩu/hash (FR-001, @S01, @S06)
- [ ] Email trùng (sau chuẩn hoá) → 409 `problem+json` (BR-003, @S02, @S07)
- [ ] Mật khẩu &lt;12 hoặc &gt;128 ký tự → 400 (BR-002, @S03, @S08)
- [ ] Email không hợp lệ → 400 (@S11)
- [ ] PBKDF2 BCL, salt ngẫu nhiên; hash khác nhau cho cùng mật khẩu (FR-004, NFR-002, @S04)
- [ ] Không lộ mật khẩu trong log (FR-004, @S05)
- [ ] Metric `password_hash_duration_seconds` sau băm (NFR-007, @S09)
- [ ] Hai đăng ký đồng thời cùng email: một 201, một 409 (@S10)
- [ ] Gate xanh theo plan mục 6; TB-021/lát 1a đồng bộ `done`

## Kế hoạch
Plan: [`document/plans/TB-029-identity-register.md`](../plans/TB-029-identity-register.md) (`approved`, B1–B4, @S01–@S11; Confirm #1–#5 đã chốt).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-25: TB-021 done; `plan-init` trên nhánh `feature/TB-029-identity-register` từ `main` `0b58fdc`.
- 2026-11-25: plan-review PASS vòng 1–2 (Confirm #1–#5).
- 2026-11-25: test-init — `register.feature`, steps, `PasswordHasherTests`; `dotnet test identity-service.Tests` đỏ 10 TB-029 (chủ yếu 404), hồi quy TB-021 10/10 xanh (lọc `!~Registration`).
- 2026-11-25: test-review v1 PASS — sửa @S05 (thêm Then 201), seed register assert 201; 10 scenario + 1 unit đỏ đúng lý do.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-029-identity-register.md` (draft→approved), B1–B4 |
| plan-review | PASS | 1 | 2026-11-25 | Confirm #1–#5; plan `approved` |
| test-init | done | – | 2026-11-25 | `register.feature` @S01–@S11; 10 đỏ (404/DB/metric); unit PBKDF2 NIE |
| test-review | PASS | 1 | 2026-11-25 | FIX @S05 + Given 201; 10/10+1 đỏ đúng lý do |
| coding-start | pending | – | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-029:register-path | Đường API đăng ký | `POST /api/v1/auth/register` (tạm) | 1 | 2026-11-25 |
| 2 | plan-review:TB-029:pbkdf2-iterations | Số vòng PBKDF2 | 600_000, `Password:Iterations` | 1 | 2026-11-25 |
| 3 | plan-review:TB-029:error-codes | Mã lỗi | `EMAIL_ALREADY_EXISTS` / `VALIDATION_FAILED` | 1 | 2026-11-25 |
| 4 | plan-review:TB-029:201-response | 201 | `{ "id" }` + `Location: /api/v1/users/{id}` | 1 | 2026-11-25 |
| 5 | plan-review:TB-029:password-max | Max mật khẩu | 128 ký tự | 1 | 2026-11-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | `register.feature` + `IdentityServiceSteps` | 404 (chưa có route) | |
| @S02 | same | 409 expected, 404 | |
| @S03 | same | 400 expected, 404 | |
| @S04 | same + `GetDistinctPasswordHashesAsync` | 2 hashes / SQL | |
| @S05 | same + `CapturingLoggerProvider` | 201 expected, 404 | |
| @S07 | same | 409 expected, 404 | |
| @S08 | `WhenRegisterLongPassword` | 400 expected, 404 | |
| @S09 | `MeterListener` | 0 samples metric | |
| @S10 | concurrent POST | [404,404] | |
| @S11 | same | 400 expected, 404 | |
| unit @S04 | `PasswordHasherTests` | `NotImplementedException` | |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | (pre-push) | PASS | 0 |

### Fix log (test-review v1)

| Vòng | Fingerprint | Nguồn | Vấn đề | Đã sửa |
|---|---|---|---|---|
| 1 | test-review:TB-029:s05-vacuous-pass | FIX | @S05 xanh khi 404 | Thêm `Then status 201` trước assert log |
| 1 | test-review:TB-029:seed-register | FIX | Given register không fail sớm | `SendRegisterAsync` assert 201 khi seed |

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
