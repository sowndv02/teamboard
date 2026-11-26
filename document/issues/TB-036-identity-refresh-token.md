---
key: TB-036
title: Identity lát 8, refresh token xoay vòng
status: done
phase: 1
branch: feature/TB-036-identity-refresh-token
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: 2026-11-26            # ngày done/dropped
---

# TB-036 — Identity lát 8, refresh token xoay vòng

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 8 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 4 (access token). Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-009): Refresh token xoay vòng (ADR refresh nguyên tử, D4)
- Không làm: FR-010 reuse chuỗi (TB-037), đăng xuất (TB-038), client registry (TB-040).

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Login thành công trả refresh token opaque; chỉ lưu băm trong DB.
- [x] `POST /api/v1/auth/refresh` xoay vòng: cặp access/refresh mới; token cũ không dùng lại được.
- [x] Refresh hết hạn hoặc không hợp lệ → 401 chung (không lộ chi tiết).
- [x] Hai request đồng thời cùng refresh → tối đa một thành công (FR-009; reuse đầy đủ FR-010 ở TB-037).
- [x] Gate identity + hồi quy login/JWKS xanh (`!~Compose`, `Category!=docker`).

## Kế hoạch
Plan: [`document/plans/TB-036-identity-refresh-token.md`](../plans/TB-036-identity-refresh-token.md) (`approved`, B0–B6; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-035 merged PR #30; `plan-init` trên nhánh `feature/TB-036-identity-refresh-token` từ `main` `94dd3a2`.
- 2026-11-26: plan-review v1 PASS — Confirm #1–#5 (FR-010 → TB-037; client `teamboard`; OAuth refresh contract; auth-public limit; metrics).
- 2026-11-26: test-init — `refresh-token.feature` @S01–@S05; `RefreshTokenHashTests`; stub `/refresh` 501; **6 đỏ**, hồi quy **56/56** (`!~Compose`).
- 2026-11-26: test-review v1 PASS — FIX Background `And`, assert refresh mới ≠ cũ (@S02); **6 đỏ** đúng lý do (trừ @S03 status tạm thời, xem Giới hạn).
- 2026-11-26: coding-start — ADR 0014, migration `refresh_token`, login/refresh + CAS; metrics; **62/62** identity `!~Compose`, board `Category!=docker` xanh.
- 2026-11-26: commit `5a43719` feat refresh; coding-review v1 **PASS** (0 CONFIRM, 0 FIX mở).
- 2026-11-26: [PR #32](https://github.com/sowndv02/teamboard/pull/32) (head `a97803a` after Co-author cleanup).
- 2026-11-26: merged PR #32 → `main` `1bfb081`; lát 8 done; mở TB-037.

## Workflow

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B0–B6; O-1..O-5 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#5 |
| test-init | done | – | 2026-11-26 | 6 đỏ (5 BDD + 1 unit); hồi quy 56 xanh |
| test-review | PASS | 1 | 2026-11-26 | Sẵn sàng code; 6 đỏ |
| coding-start | done | – | 2026-11-26 | `5a43719`; 62/62 `!~Compose` |
| coding-review | PASS | 1 | 2026-11-26 | Sẵn sàng merge |
| create-pr | done | – | 2026-11-26 | [PR #32](https://github.com/sowndv02/teamboard/pull/32) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-036:fr010-boundary | O-1 | TB-036 CAS; reuse family TB-037 | 1 | 2026-11-26 |
| 2 | plan-review:TB-036:default-client | O-2 | `teamboard` config | 1 | 2026-11-26 |
| 3 | plan-review:TB-036:refresh-contract | O-3/O-4 | OAuth body/response, no `id` on refresh | 1 | 2026-11-26 |
| 4 | plan-review:TB-036:refresh-rate-limit | O-5 | `auth-public` when enabled | 1 | 2026-11-26 |
| 5 | plan-review:TB-036:refresh-metrics | Metrics | issued + rotations counters | 1 | 2026-11-26 |

## Kịch bản và TDD log

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| refresh @S01–@S05 | `refresh-token.feature` | | **xanh** |
| refresh hash unit | `RefreshTokenHashTests` | | **xanh** |
| login/access/jwks hồi quy | các feature TB-030–035 | | xanh |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `f6d8711` + delta | Sẵn sàng code | – |

Đối chiếu kịch bản ↔ test (mục 6 `test-review.md`):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| refresh @S01–@S05 | `refresh-token.feature` | | **xanh** |
| hash SHA-256 | `RefreshTokenHashTests` | | **xanh** |
| login/access/jwks hồi quy | TB-030–035 features | | xanh |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `5a43719` | Sẵn sàng merge | – |

**Lăng kính (v1):** CAS D4 + transaction; 401 chung; hash-only DB; metric; không log refresh; rate limit `auth-public`. **Giới hạn đã biết:** FR-010 family revoke → TB-037; login access+refresh chưa một transaction (fail insert → không 200); chưa BDD expired/revoked seed.

**Gate review:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **62/62**; build `-warnaserror` xanh.

## Fix log (coding-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | – | – | – | Không có FIX mở | – | review `5a43719` |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:refresh-background-and | FIX P3 | `refresh-token.feature` Background | Hai `Given` liên tiếp | `And` cho bước thứ hai | feature |
| 1 | test:identity:refresh-s02-differs | FIX P2 | @S02 + `IdentityServiceSteps` | Thiếu assert refresh mới khác token lưu | Then + step | plan @S02 |
| 1 | test:identity:refresh-s02-multi-when | SKIP P2 | @S02 | Nhiều `When` (login→refresh→replay) | Giữ theo precedent `signing-key-rotation.feature` | rotation flow |
| 1 | test:identity:refresh-s03-stub-status | NOTE | @S03 | Stub 501; expect 401 | Không đổi stub; xanh cùng B4 | TB-035 @S03 pattern |
| 1 | test:identity:refresh-s03-expired | SKIP P2 | plan @S03 | Chưa kịch bản hết hạn/revoked seed | Cùng 401 chung; bổ sung B4 nếu cần | contract D4 |
| 1 | test:identity:refresh-migration-unit | SKIP P2 | plan B1 | Chưa test migration riêng | Migration qua Given BDD docker | plan B1 |

**Giới hạn đã biết:** @S03 đỏ **status 501** (stub) thay vì 401 cho tới `coding-start` B4; assertion 401 đúng sau implement. @S01/@S02/@S04/@S05 đỏ vì chưa phát refresh / rotation / concurrent. Hash unit đỏ vì stub hasher rỗng.

## Fix log

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|

## Bài học (lessons learned)

- 