---
key: TB-024
title: Gateway lát 4, rate limit theo IP và 429 problem+json
status: in-progress
phase: 1
branch: feature/TB-024-gateway-rate-limit
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-024 — Gateway lát 4, rate limit theo IP và 429 problem+json

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 4 của `document/plans/api-gateway-slices.md` (spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1, 2. Ước lượng của bảng lát: 2 ngày; giả định RateLimiter nền tảng đủ dùng.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-004): Rate limit theo IP: `ForwardedHeaders` an toàn, chính sách đọc/ghi sliding window, IPv6 /64, `429 problem+json` + `Retry-After`, health không bị giới hạn
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Request ghi thứ N+1 trong cửa sổ nhận `429 problem+json` `RATE_LIMIT_EXCEEDED` kèm `Retry-After` và `X-Correlation-Id`; request ngay trong ngưỡng thành công (ngưỡng mặc định 120 đọc và 30 ghi mỗi phút, cấu hình được).
- [ ] `GET` và `HEAD` dùng chung bộ đếm đọc; đọc và ghi đếm riêng.
- [ ] IP khác không bị ảnh hưởng; hai địa chỉ IPv6 cùng /64 dùng chung bộ đếm; IPv4 tính theo địa chỉ đầy đủ.
- [ ] `X-Forwarded-For` từ nguồn không tin cậy không né được giới hạn; proxy trong danh sách cấu hình thì IP lấy từ header.
- [ ] Upstream nhận `X-Forwarded-*` do gateway xác định, không mang giá trị giả của client.
- [ ] `/health/live`, `/health/ready` và fallback `404` không bị giới hạn.
- [ ] `429` không lộ IP hay khoá; ngưỡng và proxy tin cậy nằm trong cấu hình; compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
- [ ] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.

## Kế hoạch
Plan chi tiết: `document/plans/TB-024-gateway-rate-limit.md`.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-10-25: bắt đầu lát 4; nhánh tạo từ `main` sau PR 16; đồng bộ lát 3 (TB-023) sang `done` cùng lúc (LL-006).

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | `document/plans/TB-024-gateway-rate-limit.md` (draft), 4 bước B1-B4, 11 kịch bản @S01-@S11, 5 câu hỏi mở P-01..P-05, 6 giả định cần spike ở test-init |
| plan-review | pending | | | |
| test-init | pending | – | | |
| test-review | pending | | | |
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

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

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
