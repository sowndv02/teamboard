---
key: TB-033
title: Identity lát 5, bộ kiểm tra token dùng chung Teamboard.Auth
status: done
phase: 1
branch: feature/TB-033-auth-token-validation-library
estimate: L             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: 2026-11-26      # merge PR #28 (e2a046f)
---

# TB-033 — Identity lát 5, bộ kiểm tra token dùng chung Teamboard.Auth

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 5 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 4, JWKS (6). Ước lượng của bảng lát: 3–4 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-006, NFR-006): Bộ kiểm tra token dùng chung `Teamboard.Auth` (JWKS, cache) và test tấn công (ADR thư viện token)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] `Teamboard.Auth` + ADR 0011; JwtBearer ép RS256, `iss`/`aud`/`exp`
- [x] JWKS cache (TTL 10 phút); `kid` lạ refresh tối đa 1 lần/phút
- [x] Contract: token identity (TB-032) pass; attack suite NFR-006
- [x] Endpoint bảo vệ mẫu trên resource server (theo plan-review)
- [x] Gate solution build + tests xanh

## Kế hoạch
Plan: [`document/plans/TB-033-auth-token-validation-library.md`](../plans/TB-033-auth-token-validation-library.md) (`approved`, B0–B4; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-032 merged PR #27; `plan-init` trên nhánh `feature/TB-033-auth-token-validation-library` từ `main` `315449c`.
- 2026-11-26: plan-review v1 PASS — JWKS static+HTTP; board `GET /api/v1/auth/me`; issuer `http://localhost:6061`.
- 2026-11-26: test-init — ADR 0011 (đề xuất), stub `Teamboard.Shared.Auth`, `auth-me.feature` @S01–@S04, xUnit contract/attack/JWKS; **9 đỏ** (5 Auth.Tests, 4 BDD), hồi quy board 19 xanh.
- 2026-11-26: nhánh `3633eec`..`44c0033` (rewrite bỏ Co-Authored-By); gate 7+25+44; [PR #28](https://github.com/sowndv02/teamboard/pull/28).
- 2026-11-26: merged PR #28 vào `main` (`e2a046f`); lát 5 xong; mở TB-034.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#3 |
| test-init | done | – | 2026-11-26 | 5 Auth + 4 BDD đỏ |
| test-review | PASS | 1 | 2026-11-26 | 7+4 đỏ; hồi quy 21 xanh |
| coding-start | done | – | 2026-11-26 | Auth 7/7, board auth 4/4 |
| coding-review | PASS | 1 | 2026-11-26 | HttpClient timeout; deps 8.19.2 |
| create-pr | done | – | 2026-11-26 | [PR #28](https://github.com/sowndv02/teamboard/pull/28) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-033:jwks-before-034 | JWKS trước TB-034 | Static file + HTTP client (`JwksUri` tùy chọn) | 1 | 2026-11-26 |
| 2 | plan-review:TB-033:board-scope | Wire FR-006 | board-service `GET /api/v1/auth/me` | 1 | 2026-11-26 |
| 3 | plan-review:TB-033:issuer-validation | ValidIssuer | `http://localhost:6061` | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| auth-me @S01–@S04 | `auth-me.feature` | | **xanh** |
| JWT contract FR-006 | `JwtContractValidationTests` | | **xanh** |
| NFR-006 attack | `JwtAttackValidationTests` (6) | | **xanh** |
| JWKS kid refresh | `JwksKeySetCacheTests` | | **xanh** |

**Lệnh:** `dotnet test src/shared/Auth.Tests` → **7/7**; `board-service.Tests` AuthProfile **4/4**, `Category!=docker` **25/25**; `identity-service.Tests` `!~Compose` **44/44**.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `f786417` | Sẵn sàng code | – |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:auth:tampered-base64 | FIX | `TestAccessTokenFactory` | Tamper encode string thay vì UTF-8 bytes | `Encode(tamperedPayloadBytes)` | attack test |
| 1 | test:auth:attack-coverage | FIX | `JwtAttackValidationTests` | Thiếu `iss` / `alg=none` (NFR-006) | +2 tests | 7 đỏ |
| 1 | test:tb-033:plan-scenario-ids | FIX | plan §2 | ID @S02–@S04 lệch feature | Khớp BDD + xUnit | plan |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `44c0033` | Sẵn sàng merge | – |

**Giới hạn đã biết:** `JwksUri` hiện chỉ fallback khi static load lỗi (TB-034 sẽ nối primary HTTP); loader dev đọc `privateKeyPem` chỉ để suy public key — production nên JWKS public-only.

## Fix log (coding-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | teamboard-auth:http-timeout | FIX | `TeamboardAuthServiceCollectionExtensions` | JWKS HTTP không timeout | Timeout 10s | code |
| 1 | config:jsonwebtokens-version | FIX | dependencies + identity csproj | Lệch 8.15 vs JwtBearer 8.19.2 | Ghim 8.19.2 | build |

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
