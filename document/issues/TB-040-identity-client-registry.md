---
key: TB-040
title: Identity lát 12, quản lý client đăng ký
status: in-progress
phase: 1
branch: feature/TB-040-identity-client-registry
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: null           # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-040 — Identity lát 12, quản lý client đăng ký

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 12 của `document/plans/identity-auth-slices.md` (spec `document/spec/identity-auth.md`, design `document/design/identity-auth.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1b. Ước lượng của bảng lát: 2 ngày.

## Phạm vi
- Làm (theo hàng lát, **FR-014**, **BR-005**, **BR-007**): bảng `client`, seed/sync từ config; `IClientRegistry` (lookup, redirect exact, verify secret confidential); default client `teamboard` khớp refresh; không API đăng ký client.
- Không làm: client_credentials (TB-041), authorize/PKCE (TB-042), CRUD admin, đổi contract login JSON.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Bảng `client` + migration; client mặc định tồn tại sau startup (@S01).
- [ ] Secret confidential chỉ băm trong DB; verify đúng/sai (@S02); không log secret (@S05).
- [ ] `ValidateRedirectUri` khớp chính xác BR-005 (@S03).
- [ ] Hồi quy refresh/login `!~Compose` xanh.
- [ ] `config/clients.example.json` (hoặc tương đương) không chứa secret thật.

## Kế hoạch
Plan: [`document/plans/TB-040-identity-client-registry.md`](../plans/TB-040-identity-client-registry.md) (`approved`, B1–B6; plan-review PASS v1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-26: TB-039 merged PR #35; `plan-init` trên nhánh `feature/TB-040-identity-client-registry` từ `main` `a923cb2`.
- 2026-11-26: plan-review v1 **PASS** — Confirm #1–#6 (PBKDF2 secret, jsonb, exact redirect, env secret, teamboard public, ready clients).
- 2026-11-26: test-init — `client-registry.feature` @S01–@S05; stub `Security/Clients/*`; **7 failed, 75 passed** `!~Compose`.
- 2026-11-26: test-review v1 **PASS** — FIX secret_hash only, tách @S03b, `ClientRegistryTests`; **8 failed, 76 passed** `!~Compose`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-26 | Draft B1–B6; O-1..O-6 |
| plan-review | PASS | 1 | 2026-11-26 | Confirm #1–#6 |
| test-init | done | – | 2026-11-26 | 7 đỏ @S01–@S04 + unit; @S05 xanh |
| test-review | PASS | 1 | 2026-11-26 | Sẵn sàng code; 8 đỏ đúng lý do |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | `client-registry.feature` | bảng `client` / seed | |
| @S02 | `client-registry.feature` | bảng `client` / hash | |
| @S03, @S03b | `client-registry.feature` + `RedirectUriMatcherTests` | `NotImplementedException` | |
| @S04 | `client-registry.feature` + `ClientRegistryTests` | bảng `client` / stub | |
| @S05 | `client-registry.feature` | | **xanh** |
| unit hash | `ClientSecretHasherTests` (×2) | `NotImplementedException` | |

**Lệnh:** `dotnet test src/identity-service.Tests --filter "FullyQualifiedName!~Compose"` → **76 passed, 8 failed** (sau test-review v1).

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-26 | `a817a7c` + FIX test-review | Sẵn sàng code | – |

Đối chiếu: 6 scenario Reqnroll + 3 unit class; mọi @S01–@S04/@S03 đỏ vì chưa có migration/impl; @S05 xanh.

## Fix log (test-review)

| Vòng | Fingerprint | Nguồn | Vị trí | Vấn đề | Đã sửa | Evidence |
|---|---|---|---|---|---|---|
| 1 | test:identity:client-secret-echo-column | FIX P1 | `PostgresDatabase`, step @S02 | Giả định cột `secret_plaintext_echo` không có trong plan | Chỉ assert `secret_hash` ≠ plaintext | diff |
| 1 | test:identity:redirect-two-when | FIX P2 | `client-registry.feature` | @S03 hai `When` | Tách `@S03b` | diff feature |
| 1 | test:identity:client-registry-unit | FIX P2 | `ClientRegistryTests` | Thiếu unit @S04 (TryGet) | Assert `NotImplementedException` tạm | diff test |
| 1 | test:identity:ready-without-client | NOTE P3 | @S01 | Ready 200 có thể xanh trước khi có bảng `client` | coding B4 + Confirm #6 | plan B4 |

**Giới hạn đã biết:** Chưa BDD ready **503** khi thiếu default client (chỉ @S01 200 happy path); verify secret sai/đúng chỉ ở unit, không BDD — đủ cho lát.

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
