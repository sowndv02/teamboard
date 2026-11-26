---
key: TB-035
title: Identity lát 7, xoay khoá ký không hỏng token còn hạn
status: done
phase: 1
branch: feature/TB-035-identity-signing-key-rotation
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-26
closed: 2026-11-26            # ngày done/dropped
---

# TB-035 — Identity lát 7, xoay khoá ký không hỏng token còn hạn

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 7 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 6, ADR xoay khoá. Ước lượng của bảng lát: 2–3 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-008): Xoay khoá ký không hỏng token còn hạn (CLI, D7)
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Ba lệnh CLI D7: `keys rotate start` / `activate` / `retire` trên file khoá dev với guard thời gian.
- [x] JWKS công bố `pending`, `active`, `retiring`; login chỉ ký bằng `active`.
- [x] Sau activate: token mới dùng `kid` mới; token phát trước đó còn hạn vẫn verify qua JWKS.
- [x] Sau retire hợp lệ: khoá cũ không còn trong JWKS (theo FR-008).
- [x] Gate identity + Auth + board tests xanh (`!~Compose`, `Category!=docker`).

## Kế hoạch
Plan: [`document/plans/TB-035-identity-signing-key-rotation.md`](../plans/TB-035-identity-signing-key-rotation.md) (`approved`, B0–B6; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-034 merged PR #29; `plan-init` trên nhánh `feature/TB-035-identity-signing-key-rotation` từ `main` `21ffee1`.
- 2026-11-26: plan-review v1 PASS — log audit; reload file mỗi sign/JWKS; fake time CLI; file-only keys.
- 2026-11-26: test-init — `signing-key-rotation.feature` @S01–@S05; `SigningKeyRotationCliTests`; `JwtMultiKidJwksIntegrationTests`; stub CLI; **6 đỏ** (4×rotation BDD + 1×CLI + 1×Auth), @S05 xanh; hồi quy identity 43 + jwks xanh.
- 2026-11-26: test-review v1 PASS — @S06 retire JWKS; @S01 dual kid; `JwtRetiringKidTokenValidationIntegrationTests` (probe JWKS trước validate); **7+2 đỏ**.
- 2026-11-26: **coding-start done, xanh 55/55** identity (`!~Compose`), **10/10** Auth, **25/25** board (`Category!=docker`); ADR 0013 + cập nhật 0012; BDD fix S02 fake time + S05 step wording.
- 2026-11-26: **coding-review v1 PASS** — 1 FIX (CLI `JsonException`); giới hạn kid cố định / không file-lock ghi trong ADR 0013 phạm vi lab.
- 2026-11-26: [PR #30](https://github.com/sowndv02/teamboard/pull/30) (`feat` `965bf04`).
- 2026-11-26: merged PR #30 vào `main` (`94dd3a2`); lát 7 done; mở TB-036.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Plan draft B0–B6; O-1..O-4 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#4 |
| test-init | done | – | 2026-11-26 | 6 đỏ; @S05 regression xanh |
| test-review | PASS | 1 | 2026-11-26 | @S06 + consumer probe; guard @S03/@S04 hoãn tới CLI start |
| coding-start | done, xanh 55/55 | – | 2026-11-26 | ADR 0013; gate slnx build+format |
| coding-review | PASS | 1 | 2026-11-26 | fix CLI JsonException; 0 CONFIRM mở |
| create-pr | done | – | 2026-11-26 | [PR #30](https://github.com/sowndv02/teamboard/pull/30) |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-035:audit-scope | O-1 audit | Log + `IRotationAudit`, không DB | 1 | 2026-11-26 |
| 2 | plan-review:TB-035:reload-keys | O-2 reload | Reload file mỗi sign/JWKS | 1 | 2026-11-26 |
| 3 | plan-review:TB-035:time-guards-test | O-3 guard test | Fake `TimeProvider` CLI | 1 | 2026-11-26 |
| 4 | plan-review:TB-035:key-source | O-4 nguồn khoá | File-only | 1 | 2026-11-26 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| rotation @S01–@S06 | `signing-key-rotation.feature` | | **xanh** |
| CLI rotate start/activate/retire | `SigningKeyRotationCliTests` | | **xanh** |
| FR-006 multi-kid JWKS | `JwtMultiKidJwksIntegrationTests` | | **xanh** |
| FR-008 token cũ sau activate | `JwtRetiringKidTokenValidationIntegrationTests` | | **xanh** |
| jwks TB-034 @S01–@S04 | `jwks.feature` | | xanh |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `289f0f3` + delta | Sẵn sàng code | – |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `dd71f58` + working tree coding-start | PASS | – |

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:rotation:s01-active-in-jwks | FIX | `signing-key-rotation.feature` @S01 | Plan: JWKS còn active khi pending | +Then `test-active-1` | plan §2 |
| 1 | test:rotation:s06-retire-jwks | FIX | feature @S06 | Thiếu happy-path retire JWKS | Kịch bản @S06 + step missing kid | plan §2 |
| 1 | test:auth:retiring-token-probe | FIX | `JwtRetiringKidTokenValidationIntegrationTests` | Pass sai do validator không ép `kid` | Probe JWKS 2 kids trước validate | FR-008 / TB-034 pattern |
| 1 | test:rotation:guard-cli-prereq | NOTE | @S03/@S04 | Đỏ tại `rotate start` cho tới khi CLI có | Ghi giới hạn; không đổi stub | đỏ đúng lý do |

**Giới hạn đã biết:** @S03/@S04 assert exit code chỉ chạy sau `rotate start` xong; tới coding-start `B3` start phải xanh trước khi guard scenarios đỏ đúng layer.

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | identity:SigningKeyRotationCommands:json-parse | FIX | `SigningKeyRotationCommands.cs:47` | `JsonException` không vào filter → thoát không kiểm soát | Thêm `JsonException`; test invalid JSON | `SigningKeyRotationCliTests.RotateStart_ReturnsNonZeroWhenSigningKeysFileIsInvalidJson` |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 