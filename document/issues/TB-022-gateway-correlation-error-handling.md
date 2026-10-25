---
key: TB-022
title: Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json
status: in-progress
phase: 1
branch: feature/TB-022-gateway-correlation-error-handling
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-022 — Gateway lát 2, correlation, trace nối xuyên service và 500 problem+json

## Bối cảnh
Lát 2 của `document/plans/api-gateway-slices.md` (feature api-gateway; spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`, issue tài liệu TB-007). Lát 1 (TB-010, PR 11) đã merge: gateway có route boards, health và `404 problem+json`, với `correlationId` tạm bằng `traceId`. Lát này đặt `X-Correlation-Id` thật và ExceptionHandler; mọi phản hồi lỗi sau đó (lát 3–7) đều mang id này.

**Phụ thuộc:** TB-010 đã merge (2026-10-24). Branch tạo từ `main` ngày 2026-10-25.

## Phạm vi
- Làm:
  - Middleware correlation dùng chung trong `src/shared/Http`: nhận `X-Correlation-Id` hợp lệ (`A-Z a-z 0-9 . _ -`, tối đa 64 ký tự) thì giữ nguyên, thiếu hoặc không hợp lệ thì tạo mới (GUID dạng chuỗi) và ghi đè header gửi xuống upstream; trả lại trong mọi response; gắn vào tag của `Activity` và log scope (FR-003).
  - `traceparent` truyền xuống upstream; span của gateway nối vào trace của client (FR-003, NFR-002).
  - `ProblemFactory` lấy `correlationId` thật từ middleware (thay giá trị tạm bằng `traceId`).
  - ExceptionHandler dùng chung: lỗi chưa lường trả `500 problem+json` mã `INTERNAL_ERROR`, không lộ chi tiết nội bộ, vẫn mang `X-Correlation-Id` (design 6.3, 6.4).
  - Nối OTLP cho `gateway` ở `docker-compose.observability.yml`; kiểm trace gateway → board-service trong Jaeger.
- Không làm: timeout và ánh xạ `502/503/504` (lát 3), rate limit và `ForwardedHeaders` (lát 4), body (lát 5), CORS (lát 6), metric độ trễ thêm vào, log retention, graceful shutdown (lát 7), correlation ở phía `board-service`, smoke với endpoint boards thật (lát 8).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Request không có `X-Correlation-Id` nhận id mới trong response, upstream nhận đúng id đó.
- [ ] Request có id hợp lệ (gồm độ dài 64) được giữ nguyên ở upstream và response.
- [ ] Id không hợp lệ (ký tự lạ, dài 65 ký tự) bị thay bằng id mới.
- [ ] Upstream nhận `traceparent` cùng trace id với client gửi.
- [ ] `404` và `500` `problem+json` mang `correlationId` bằng header response; lỗi chưa lường không lộ stack trace.
- [ ] Upstream trả `X-Correlation-Id` khác thì response tới client chỉ có đúng một giá trị (của gateway).
- [ ] Trace của một request qua gateway có span của `gateway` và `board-service` trong cùng một trace ở Jaeger.
- [ ] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.

## Kế hoạch
Plan chi tiết: `document/plans/TB-022-gateway-correlation-error-handling.md`.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue từ lát 2 của bảng lát; branch tạo từ `main` (sau PR 11).

## Workflow

Làn: lớn (service dùng chung, hợp đồng lỗi công khai). Spec/design/bảng lát dùng chung ở TB-007; bước bắt đầu từ plan-init của lát.

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | `document/plans/TB-022-gateway-correlation-error-handling.md` (draft), 3 bước B1–B3, 1 câu hỏi mở P-01 |
| plan-review | PASS | 2 | 2026-10-25 | plan `approved`; 4 quyết định (nhiều giá trị id, spike ép lỗi, không log khi thay id, cổng Docker), 12 FIX; review độc lập (sub-agent) |
| test-init | done | – | 2026-10-25 | 18 test thư viện + 16 test host gateway + @S06 Docker đỏ đúng lý do; spike A-03 đạt (thay `IHttpForwarder`); trace-by-id Jaeger kiểm tay: `GET /api/v3/traces/{traceId}` |
| test-review | pending | | | |
| coding-start | pending | – | | |
| coding-review | pending | | | |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-022:p01-multi-value | Nhiều giá trị `X-Correlation-Id` | Coi là không hợp lệ, tạo id mới | 1 | 2026-10-25 |
| 2 | plan-review:TB-022:a03-forwarder-swap | Cách ép lỗi chưa lường cho @S05 | Spike ở test-init, ghi sẵn phương án dự phòng | 1 | 2026-10-25 |
| 3 | plan-review:TB-022:invalid-id-log-policy | Log khi thay id | Không log; đếm bằng metric ở lát 7 nếu cần | 1 | 2026-10-25 |
| 4 | plan-review:TB-022:docker-ports-parallel | Test Docker tranh cổng giữa các assembly | Giữ nguyên, ghi trong `COMMANDS.md` | 1 | 2026-10-25 |
| 5 | test-review:TB-022:traceparent-edge | `traceparent` thiếu hoặc sai định dạng: thêm kịch bản? | Thêm cả thiếu và sai định dạng (@S11, @S12) | 1 | 2026-10-25 |
| 6 | test-review:TB-022:traceparent-malformed-behavior | Hành vi khi `traceparent` sai định dạng | Bỏ qua, bắt đầu trace mới, vẫn chuyển tiếp (không trả lỗi) | 1 | 2026-10-25 |
| 7 | test-review:TB-022:pyramid-grammar-duplication | Bảng ngữ pháp id lặp ở `.feature` và unit test | Giữ cả hai | 1 | 2026-10-25 |
| 8 | test-review:TB-022:gateway-log-scope-untested | Test log/scope ở gateway thật | Không cần, unit đủ | 1 | 2026-10-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 | `CorrelationMiddlewareTests` (id hợp lệ ×3, 64 ký tự, tên header chữ thường, thiếu id, tag và scope); `correlation.feature` @S01 | Đỏ ở hành vi: `The given header was not found` (chưa có `X-Correlation-Id`), cả thư viện lẫn gateway | – |
| @S02 | `CorrelationMiddlewareTests` (17 id sai gồm ký tự sát biên `[ \ ] ^ ` / : @ { ~`, 65 ký tự, id sai không lộ trong log/tag có đối chứng dương); `correlation.feature` @S02 (15 dòng) | Đỏ như @S01; riêng `"١٢٣"` đỏ vì YARP không chuyển được header không phải ASCII (upstream không nhận request), sẽ xanh khi middleware ghi đè id | – |
| @S08 | `CorrelationMiddlewareTests.Request_TwoHeaderValues_IsReplaced`; `correlation.feature` @S08 | Đỏ như @S01 | – |
| @S03 | `correlation.feature` @S03 | Xanh sẵn: YARP đã truyền `traceparent` (A-02 đúng); giữ làm test hồi quy | – |
| @S11, @S12 | `correlation.feature` @S11, @S12 | Xanh sẵn: ASP.NET và YARP đã tạo trace mới khi thiếu hoặc sai `traceparent`; giữ làm test hồi quy (bỏ qua rồi bắt đầu trace mới đúng W3C) | – |
| @S04 | `ProblemFactoryTests.Create_ContextHasCorrelationId_UsesItAndKeepsTraceId`; `correlation.feature` @S04 | Đỏ: `correlationId` chưa lấy từ context, chưa có header | – |
| @S09 | `correlation.feature` @S09 (`/health/live`) | Đỏ: chưa có header | – |
| @S10 | `correlation.feature` @S10 (HEAD, POST, DELETE) | Đỏ: chưa có header | – |
| @S05 | `ExceptionHandlerTests` (5 test: hợp đồng đầy đủ, allowlist thuộc tính, header và id, không gửi id, log scope); `error-handling.feature` @S05 | Đỏ: 500 `text/plain` ở gateway; ở thư viện exception chưa có handler lọt tới client. Spike A-03 đạt: `ConfigureTestServices` thay được `IHttpForwarder` | – |
| @S06 | `tracing.feature` @S06 (`@docker`, project `tb022-tests`, một `When`) | Đỏ: trace theo id chỉ có `board-service`, chưa có `gateway` (đã chạy lại sau vòng 1; endpoint `GET /api/v3/traces/{traceId}` kiểm tay) | – |
| @S07 | `correlation.feature` @S07 | Đỏ: phản hồi mang id của upstream (`from-upstream`) thay vì của gateway | – |

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

-
