---
key: TB-032
title: Identity lát 4, phát access token JWT khi đăng nhập
status: done
phase: 1
branch: feature/TB-032-identity-access-token
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: 2026-11-26      # merge PR #27
---

# TB-032 — Identity lát 4, phát access token JWT khi đăng nhập

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 4 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 2, ADR thuật toán ký. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-005, BR-004): Phát access token JWT khi đăng nhập (ADR RS256 + Secrets Manager tạo trước lát này)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Login thành công trả access JWT RS256 + claim FR-005 (@S01–@S03)
- [x] TTL access ~10 phút cấu hình được (BR-004, @S02)
- [x] 401 không trả token (@S05)
- [x] Ready fail khi thiếu khoá ký active
- [x] ADR RS256 + loader khoá; gate xanh plan-review

## Kế hoạch
Plan: [`document/plans/TB-032-identity-access-token.md`](../plans/TB-032-identity-access-token.md) (`approved`, B0–B4; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-031 merged PR #26 (`cbc8124`); `plan-init` trên nhánh `feature/TB-032-identity-access-token`.
- 2026-11-26: plan-review v1 PASS (Confirm #1–#4).
- 2026-11-26: test-init — ADR `0010-identity-jwt-signing.md` (đề xuất), `access-token.feature` @S01–@S06, cập nhật `login.feature` @S01, `SigningKeyReadyTests`; 6 đỏ (JWT/metric/ready), 38 hồi quy xanh.
- 2026-11-26: test-review v1 PASS — sửa @S04 vacuous green, Background, reconcile; **7 đỏ / 37 xanh**.
- 2026-11-26: commit `9d96fae` test(TB-032); coding-start B0–B4 — JWT RS256 login, ready khoá ký, metric; **44/44 xanh**.
- 2026-11-26: coding-review v1 PASS — FIX 503 thiếu khoá, compose signing keys.
- 2026-11-26: commit `5250b45` feat + PR [#27](https://github.com/sowndv02/teamboard/pull/27).
- 2026-11-26: merged PR #27 vào `main` (`315449c`); lát 4 xong; mở TB-033.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | done | – | 2026-11-26 | 6 đỏ JWT + ready; 38 xanh |
| test-review | PASS | 1 | 2026-11-26 | 7 đỏ đúng lý do |
| coding-start | done | – | 2026-11-26 | 44/44 xanh (!~Compose) |
| coding-review | PASS | 1 | 2026-11-26 | FIX 503 + compose keys |
| create-pr | done | – | 2026-11-26 | PR #27 |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-032:login-response-shape | Login 200 body | id + access_token, token_type, expires_in | 1 | 2026-11-26 |
| 2 | plan-review:TB-032:issuer-url | iss local | Identity:Jwt:Issuer configurable | 1 | 2026-11-26 |
| 3 | plan-review:TB-032:secrets-manager-scope | SM trong lát | Dev file only | 1 | 2026-11-26 |
| 4 | plan-review:TB-032:adr-number | ADR | 0007 JWT signing | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| login @S01 (TB-032) | `login.feature` + steps JWT | missing `access_token` | **xanh** |
| access-token @S01–@S06 | `access-token.feature` | JWT/metric/ready | **xanh** |
| Ready thiếu khoá | `SigningKeyReadyTests` | ready 503 | **xanh** |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **44/44 pass** (sau coding-start).

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | working tree | Sẵn sàng code | – |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | working tree | Sẵn sàng merge | – |

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | test:identity-service:s04-vacuous-green | FIX | access-token @S04 | PEM check xanh khi chưa có token | Thêm assert Bearer trước PEM | 7 đỏ |
| 1 | test:identity-service:access-token-background | FIX | access-token.feature | Lặp Given | Background DB + signing config | feature |
| 1 | test:identity-service:s05-error-shape | FIX | access-token @S05 | Thiếu assert credential error | Thêm INVALID_CREDENTIALS | feature |
| 1 | test:identity-service:adr-number | FIX | ADR / Confirm #4 | 0007 đã là dependency-policy | ADR JWT = **0010** | adr/0010 |
| 1 | test:identity-service:identitymodel-dep | FIX | dependencies.yaml | Package test chưa ghi danh | Thêm JsonWebTokens 8.15.0 | config |
| 1 | identity-service:login:no-key-500 | FIX | LoginEndpoints | Login thành công thiếu khoá → 500 | 503 SERVICE_UNAVAILABLE | test 44/44 |
| 1 | identity-service:compose:ready-keys | FIX | docker-compose.yml | Ready 503 thiếu khoá trong compose | Mount signing-keys.dev.json | config |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
