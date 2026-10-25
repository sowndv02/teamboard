---
key: TB-023
title: Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)
status: in-progress
phase: 1
branch: feature/TB-023-gateway-downstream-errors
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-023 — Gateway lát 3, lỗi phía sau (timeout, 503/502/504, ngắt giữa chừng)

## Bối cảnh
Lát 3 của `document/plans/api-gateway-slices.md` (feature api-gateway; spec `document/spec/api-gateway.md` FR-006 và NFR-003, design `document/design/api-gateway.md` D4 và mục 8; issue tài liệu TB-007). Lát 1 (TB-010) và lát 2 (TB-022, PR 15) đã merge: gateway có route boards, health, `X-Correlation-Id`, ExceptionHandler `500` và trace nối sang `board-service`. Hiện lỗi kết nối tới upstream còn rơi vào ExceptionHandler hoặc mã mặc định của YARP; lát này đặt hợp đồng lỗi công khai của phía sau.

**Phụ thuộc:** TB-010, TB-022 đã merge (2026-10-25). Branch tạo từ `main` ngày 2026-10-25.

## Phạm vi
- Làm (theo FR-006, NFR-003, design D4):
  - Timeout cấu hình được: tổng 30 giây theo `Timeout` của route và `ActivityTimeout` ≤ 30 giây của cluster (giả định A-03).
  - Ánh xạ lỗi của forwarder thành `problem+json` **trước khi gửi header**: `503 UPSTREAM_UNAVAILABLE` (không có đích khả dụng hoặc không kết nối được), `502 UPSTREAM_BAD_GATEWAY` (lỗi giao thức, upstream ngắt khi chưa trả header), `504 UPSTREAM_TIMEOUT` (quá timeout trước header); mã lỗi ở bảng mã của gateway, không lộ host nội bộ hay stack trace.
  - `Retry-After` trên `503` (mặc định 5 giây, cấu hình được; giả định DA-05).
  - Upstream đã trả header rồi mới ngắt hoặc treo: gateway ngắt kết nối với client, ghi log và metric `gateway_upstream_aborted` kèm `correlationId` (không đổi được status đã gửi).
- Không làm: retry và circuit breaker (spec mục 4), rate limit (lát 4), body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention và graceful shutdown (lát 7), smoke với endpoint boards thật (lát 8), sửa `board-service` hay `identity-service`.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Upstream không kết nối được (cổng đóng) trả `503 problem+json` `UPSTREAM_UNAVAILABLE` kèm `Retry-After`, có `traceId` và `correlationId`.
- [ ] Upstream ngắt kết nối hoặc trả lỗi giao thức trước khi trả header trả `502 problem+json` `UPSTREAM_BAD_GATEWAY`.
- [ ] Upstream không trả header trong thời gian timeout (cấu hình ngắn trong test) trả `504 problem+json` `UPSTREAM_TIMEOUT`.
- [ ] Upstream đã trả header rồi ngắt hoặc treo: client bị ngắt kết nối, có log và metric `gateway_upstream_aborted` với `correlationId`.
- [ ] Phản hồi lỗi không chứa host, cổng hay tên nội bộ của upstream, không có stack trace.
- [ ] Giá trị timeout và `Retry-After` đọc từ cấu hình, không hardcode.
- [ ] `/health/live` và `/health/ready` vẫn 200 khi upstream chết.
- [ ] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.

## Kế hoạch
Plan chi tiết: `document/plans/TB-023-gateway-downstream-errors.md` (chưa soạn; bước plan-init).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ lát 3 của bảng lát; branch tạo từ `main` (sau PR 15).

## Workflow

Làn: lớn (hợp đồng lỗi công khai của gateway, rủi ro hành vi YARP khác giả định, design mục 16). Spec/design/bảng lát dùng chung ở TB-007; bước bắt đầu từ plan-init của lát.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | pending | – | | |
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

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

-
