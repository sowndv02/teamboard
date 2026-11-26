---
key: TB-034
title: Identity lát 6, công bố JWKS
status: in-progress
phase: 1
branch: feature/TB-034-identity-jwks
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: null            # ngày done/dropped
---

# TB-034 — Identity lát 6, công bố JWKS

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 6 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 4. Ước lượng của bảng lát: 1–2 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-007): Công bố JWKS
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] `GET /.well-known/jwks.json` trả JWK Set chỉ khoá công khai RS256, mỗi khoá có `kid` (FR-007).
- [ ] `Cache-Control: public, max-age=600` (design D2).
- [ ] Không có `privateKeyPem` / material riêng trong response.
- [ ] Consumer: `Teamboard.Auth` ưu tiên `JwksUri` HTTP; board (hoặc Auth integration) xác nhận token qua JWKS identity.
- [ ] Gate identity + Auth + board tests xanh (`!~Compose`, `Category!=docker`).

## Kế hoạch
Plan: [`document/plans/TB-034-identity-jwks.md`](../plans/TB-034-identity-jwks.md) (`approved`, B0–B4; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-033 merged PR #28; `plan-init` trên nhánh `feature/TB-034-identity-jwks` từ `main` `e2a046f`.
- 2026-11-26: plan-review v1 PASS — JWKS no-key **503**; `jwks_served_total`; @S04 board + Auth.Tests.
- 2026-11-26: test-init — `jwks.feature` @S01–@S04; `JwtHttpJwksIntegrationTests`; ADR 0012 proposed; **5 đỏ** (4×404/503 JWKS, 1×HTTP JWKS); hồi quy identity 44 xanh.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B4; O-1..O-3 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#3 |
| test-init | done | – | 2026-11-26 | 4 BDD + 1 Auth đỏ |
| test-review | PASS | 1 | 2026-11-26 | 5 đỏ; board BDD hoãn coding-start |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-034:jwks-no-key | JWKS khi không có khoá active | HTTP 503 | 1 | 2026-11-26 |
| 2 | plan-review:TB-034:jwks-metric | Metric phục vụ JWKS | `jwks_served_total` | 1 | 2026-11-26 |
| 3 | plan-review:TB-034:jwks-http-consumer | @S04 consumer HTTP JWKS | Board BDD + Auth.Tests | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| jwks @S01–@S04 | `jwks.feature` | **x** | |
| FR-006 HTTP JWKS (consumer) | `JwtHttpJwksIntegrationTests` | **x** | |
| auth-me TB-033 @S01–@S04 | `auth-me.feature` | | xanh |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `83124ea` + review delta | Sẵn sàng code | – |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:jwks-contract-fields | FIX | `jwks.feature` / steps | @S01 thiếu assert `kty`/`use`/`alg` | +Then RSA/sig/RS256 | plan §2 |
| 1 | test:auth:http-jwks-assertion | FIX | `JwtHttpJwksIntegrationTests` | Đỏ do exception 404, không phải assertion | Probe `GET` JWKS 200 trước validate | consumer test |
| 1 | test:tb-034:scenario-trace | FIX | plan §2, issue TDD | FR-006 lẫn @S04 (metric) | Tách metric @S04 vs integration | bảng truy vết |

**Giới hạn đã biết:** Board BDD consumer (plan-review Confirm #3) hoãn tới `coding-start` vì xung đột `Program` khi reference chéo; `JwtHttpJwksIntegrationTests` giữ phần FR-006 HTTP.

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
