---
key: TB-031
title: Identity lát 3, khoá tạm và giới hạn tốc độ đăng nhập
status: in-progress
phase: 1
branch: feature/TB-031-identity-lockout-rate-limit
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-031 — Identity lát 3, khoá tạm và giới hạn tốc độ đăng nhập

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 3 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 2. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-003, NFR-008): Khoá tạm theo (email, IP) và giới hạn tốc độ (cờ `RateLimiter`, Q-G = A)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Sau 5 lần sai liên tiếp (email, IP): 401 chung + `Retry-After`, không đăng nhập được dù đúng mật khẩu (FR-003, A-03, @S01–@S02)
- [ ] Đăng nhập thành công reset bộ đếm cặp (@S03)
- [ ] IP khác không kế thừa khoá (@S04)
- [ ] Hết thời gian khoá → đăng nhập được (@S05)
- [ ] Cờ rate limit bật: 10 req/phút/IP login+register → 429 (NFR-008, @S07–@S08); tắt cờ không 429 (@S09)
- [ ] `lockout_total` metric (@S10)
- [ ] Gate xanh đầy đủ (plan-review)

## Kế hoạch
Plan: [`document/plans/TB-031-identity-lockout-rate-limit.md`](../plans/TB-031-identity-lockout-rate-limit.md) (`approved`, B1–B4; Confirm #1–#4).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-030 merged PR #25 (`916fcc7`); `plan-init` trên nhánh `feature/TB-031-identity-lockout-rate-limit`.
- 2026-11-26: plan-review v1 PASS (Confirm #1–#4).
- 2026-11-26: test-init — `lockout-rate-limit.feature`, steps + `TestClientIpStartupFilter`; build xanh; chạy test đỏ **bị chặn** (Docker Desktop không chạy → 33 scenario @docker skipped).
- 2026-11-26: test-review v1 PASS — Docker: Failed 9, Passed 28 (hồi quy 27 + @S09); FIX vacuous green @S03–@S05.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B1–B4, P-01..P-04 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | done | – | 2026-11-26 | 10 kịch bản @S01–@S10; đỏ chờ Docker |
| test-review | PASS | 1 | 2026-11-26 | 9 đỏ đúng lý do; @S09 xanh chủ đích |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-031:client-ip-source | IP cho khoá/rate limit | `IClientIpResolver`; ForwardedHeaders cờ default off | 1 | 2026-11-26 |
| 2 | plan-review:TB-031:rate-limit-policy | Policy rate limit | Một policy `auth-public` / IP | 1 | 2026-11-26 |
| 3 | plan-review:TB-031:lockout-response | Retry-After | Chỉ HTTP header | 1 | 2026-11-26 |
| 4 | plan-review:TB-031:rate-limit-scope | Endpoint rate limit | login + register only | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | Five consecutive failures… | ✅ 401/Retry-After (got 200) | |
| @S02 | Locked pair… Retry-After | ✅ Given lock / Retry-After | |
| @S03 | Successful login resets… | ✅ 5 fail sau reset → Retry-After | |
| @S04 | Lockout for one IP… | ✅ Given lock IP .10 / Then .11 | |
| @S05 | Login succeeds after lockout… | ✅ Given lock / retry 200 | |
| @S06 | Lockout response does not reveal… | ✅ Given lock | |
| @S07 | Enabled rate limiting… 429 | ✅ Expected 429, got 401 | |
| @S08 | Enabled rate limiting… registration | ✅ Expected 429 | |
| @S09 | Disabled rate limiting… | — | ✅ (burst không 429) |
| @S10 | Lockout records lockout metrics | ✅ lockout_total = 0 | |

Lệnh: `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"`. Kết quả Docker 2026-11-26: **Failed 9**, **Passed 28** (27 hồi quy + @S09). Lý do đỏ: thiếu lockout / rate limit / metric (không lỗi môi trường).

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `63b4627` + test-review delta | PASS | — |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | test:identity-service:vacuous-green-lockout | FIX | @S04 feature | Pass khi chưa khoá IP .11 | Thêm When/Then khoá .10 trước | 9 failed sau sửa |
| 1 | test:identity-service:vacuous-green-reset | FIX | @S03 feature | Không phát hiện thiếu reset | Thêm 5 fail → Retry-After | S03 failed đúng |
| 1 | test:identity-service:given-lock-assert | FIX | IdentityServiceSteps | Given lock không kiểm chứng | Probe login 401 + Retry-After | S05–S06 failed trong Given |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
