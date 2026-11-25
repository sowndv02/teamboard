---
key: TB-027
title: Gateway lát 7, quan sát và vận hành
status: in-progress
phase: 1
branch: feature/TB-027-gateway-observability-ops
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-027 — Gateway lát 7, quan sát và vận hành

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 7 của `document/plans/api-gateway-slices.md` (spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 2, 3, 4. Ước lượng của bảng lát: 1–2 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: NFR-001, NFR-006): Quan sát và vận hành: metric độ trễ thêm vào, log không lộ `Authorization`/body kèm IP, lưu log 7 ngày (local), graceful shutdown, cảnh báo "không ra internet trước JWT"
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Access log: IP + `correlationId`, không `Authorization`/body (NFR-006, @S01–@S02)
- [ ] Metric: histogram thời gian request và độ trễ thêm vào đo được (NFR-001, @S03); counter lỗi `429`/`5xx` (@S04)
- [ ] Graceful shutdown: `ShutdownTimeout` ≥ 30s, compose `stop_grace_period` 35s (@S06)
- [ ] Retention log local ~7 ngày trên docker gateway (@S05); Jaeger theo quyết định P-03
- [ ] Cảnh báo không expose internet trước JWT (@S07)
- [ ] Gate xanh theo plan mục 6; TB-026/lát 6 đồng bộ `done` (LL-006)

## Kế hoạch
Plan: [`document/plans/TB-027-gateway-observability-ops.md`](../plans/TB-027-gateway-observability-ops.md) (`approved`, B1–B4, @S01–@S07; P-01..P-04 đã chốt vòng plan-review 1).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-25: plan-init. Nhánh `feature/TB-027-gateway-observability-ops` từ `origin/main` (sau PR #20). Đồng bộ TB-026 merge → `done`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-027-gateway-observability-ops.md` (draft), B1–B4, P-01..P-04 |
| plan-review | PASS | 1 | 2026-11-25 | 4 CONFIRM chốt (Activity overhead, counter tách, docker logs, runbook JWT) |
| test-init | done | – | 2026-11-25 | observability.feature @S01–@S07; ObservabilitySteps; 8 đỏ / 215 hồi quy xanh |
| test-review | PASS | 1 | 2026-11-25 | S01 bổ sung method/status/traceId; shutdown đọc appsettings |
| coding-start | in-progress | – | 2026-11-25 | B1 xong (@S01–@S02 xanh); B2–B4 đỏ |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | observability.feature + ObservabilitySteps | | xanh (B1) |
| @S02 | observability.feature + ObservabilitySteps | | xanh (B1) |
| @S03 | observability.feature + ObservabilitySteps | đỏ (chưa histogram) | |
| @S04 | observability.feature (2 scenario) + ObservabilitySteps | đỏ (counter = 0) | |
| @S05 | observability.feature + ObservabilitySteps | đỏ (compose thiếu logging) | |
| @S06 | observability.feature + ObservabilitySteps | đỏ (ShutdownTimeout / stop_grace_period) | |
| @S07 | observability.feature + ObservabilitySteps | đỏ (thiếu runbook) | |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | `e8047c9`..`013fa44` (rewrite bỏ Co-Authored-By) | Sẵn sàng code | 0 |

### Test review vòng 1 (tóm tắt)

- **Phạm vi:** `observability.feature` @S01–@S07, `ObservabilitySteps.cs`, mở rộng `GatewayContext` meter listener.
- **Đối chiếu:** plan approved (bảng truy vết), NFR-001/NFR-006, design mục 9 (log/metric), tiêu chí issue TB-027.
- **Chạy lại:** `dotnet test src/gateway.Tests/gateway.Tests.csproj --filter "Category!=docker"` → 8 fail (TB-027), 215 pass — đỏ đúng lý do (chưa production).
- **Findings đã xử lý (FIX):**
  - `test:gateway:access-log-fields` (P1): S01 thiếu method/status/traceId so với plan B1 — thêm 2 `Then` + step.
  - `test:gateway:shutdown-source` (P2): S06 chỉ grep `Program.cs` — đọc thêm `appsettings.json` `Host:ShutdownTimeout`.
- **Giới hạn đã biết:** @S04 chỉ smoke `gateway_http_429` và `503` (plan cho phép hai scenario); `502`/`504` triển khai + test hồi quy ở B2/coding. Độ đúng histogram overhead (P-01 Activity) chứng minh khi @S03 xanh, không có spike riêng. S05 chưa assert cụ thể `max-size`×`max-file` ≈ 7 ngày (chọn giá trị ở B3). Tên instrument trong `Then` là hợp đồng quan sát đã chốt plan P-02.

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | test:gateway:access-log-fields | FIX | observability.feature @S01, ObservabilitySteps | Access log chưa assert method/status/traceId | Thêm Then + step `traceId=`/`method=`/`status=` | vòng 1 delta |
| 1 | test:gateway:shutdown-source | FIX | ObservabilitySteps.ThenHostShutdownTimeout | Chỉ khớp regex Program | Fallback appsettings `Host:ShutdownTimeout` | vòng 1 delta |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
