---
key: TB-024
title: Gateway lát 4, rate limit theo IP và 429 problem+json
status: done
phase: 1
branch: feature/TB-024-gateway-rate-limit
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-10-25     # ngày chuyển in-progress
closed: 2026-11-25      # ngày done/dropped
---

# TB-024 — Gateway lát 4, rate limit theo IP và 429 problem+json

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 4 của `document/plans/api-gateway-slices.md` (spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1, 2. Ước lượng của bảng lát: 2 ngày; giả định RateLimiter nền tảng đủ dùng.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-004): Rate limit theo IP: `ForwardedHeaders` an toàn, chính sách đọc/ghi sliding window, IPv6 /64, `429 problem+json` + `Retry-After`, health không bị giới hạn
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Request ghi thứ N+1 trong cửa sổ nhận `429 problem+json` `RATE_LIMIT_EXCEEDED` kèm `Retry-After` và `X-Correlation-Id`; request ngay trong ngưỡng thành công (ngưỡng mặc định 120 đọc và 30 ghi mỗi phút, cấu hình được).
- [x] `GET` và `HEAD` dùng chung bộ đếm đọc; đọc và ghi đếm riêng.
- [x] IP khác không bị ảnh hưởng; hai địa chỉ IPv6 cùng /64 dùng chung bộ đếm; IPv4 tính theo địa chỉ đầy đủ.
- [x] `X-Forwarded-For` từ nguồn không tin cậy không né được giới hạn; proxy trong danh sách cấu hình thì IP lấy từ header.
- [x] Upstream nhận `X-Forwarded-*` do gateway xác định, không mang giá trị giả của client.
- [x] `/health/live`, `/health/ready` và fallback `404` không bị giới hạn.
- [x] `429` không lộ IP hay khoá; ngưỡng và proxy tin cậy nằm trong cấu hình; compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
- [x] `dotnet build src/teamboard.slnx -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes` xanh.

## Kế hoạch
Plan chi tiết: `document/plans/TB-024-gateway-rate-limit.md`.

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-10-25: coding-start: code production viết bởi Cursor Agent (`agent -p --force --trust`, brief `local/cursor-tb024-brief.md`), Claude kiểm lại diff và test. Quyết định ngoài brief của Cursor: `RateLimitOptionsValidator` (kiểm Read/Write lồng nhau), `Retry-After` theo policy của endpoint, `ForwardedHeadersSetup` đăng ký bằng `IConfigureOptions`.
- 2026-10-25: plan-review chốt 6 điểm; spike cho thấy `Retry-After` phải lấy từ cửa sổ, YARP mặc định đã Set `X-Forwarded-For/Proto`, `KnownNetworks` obsolete.
- 2026-10-25: bắt đầu lát 4; nhánh tạo từ `main` sau PR 16; đồng bộ lát 3 (TB-023) sang `done` cùng lúc (LL-006).
- 2026-10-25: mở PR https://github.com/sowndv02/teamboard/pull/18 (nhánh `feature/TB-024-gateway-rate-limit` vào `main`); trạng thái review.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-10-25 | `document/plans/TB-024-gateway-rate-limit.md` (draft), 4 bước B1-B4, 11 kịch bản @S01-@S11, 5 câu hỏi mở P-01..P-05, 6 giả định cần spike ở test-init |
| plan-review | PASS | 1 | 2026-10-25 | plan `approved`; 6 quyết định (cờ `ForwardedHeaders`, thứ tự Routing, giữ cỡ lát, `X-Forwarded-Host`, IP null, 6 đoạn), 8 FIX theo kết quả spike (KnownIPNetworks, Retry-After từ cửa sổ, YARP mặc định Set XFF, kịch bản cấu hình thật...); review độc lập kèm spike |
| test-init | done | – | 2026-10-25 | 26 đỏ, 129 xanh, tổng 155 (sau test-review: 31 đỏ, 135 xanh, tổng 166); đỏ đúng lý do ở hành vi |
| test-review | PASS | 2 | 2026-10-25 | 1 P1 (proxy tin cậy) và 5 P2 vòng 1 đã sửa bằng @S11-@S14 và mở rộng @S02, @S03; 3 quyết định (khoá thứ tự quan sát được, bỏ cột Host, hoãn ForwardLimit); review độc lập kèm bản tham chiếu 166/166 xanh và đột biến bị bắt |
| coding-start | done | – | 2026-10-25 | xanh 166/166 gateway (3 lần liên tiếp) và toàn solution không docker (241/241); code production do Cursor Agent viết theo brief `local/cursor-tb024-brief.md` rồi được kiểm lại (diff chỉ trong `src/gateway`, build, test, format, vulnerable sạch); chưa commit; nhóm docker không có test mới, chưa chạy lại; gitleaks chưa cài |
| coding-review | PASS | 2 | 2026-10-25 | vòng 1: 0 P0-P2, 2 P3 (cả hai CONFIRM đã chốt, đã sửa); vòng 2 delta xanh: build `-warnaserror` 0/0, gateway 166/166, docker 3/3, format sạch; review của Claude (đã kiểm diff ở coding-start), code do Cursor Agent viết; gitleaks chưa cài |
| create-pr | done | – | 2026-10-25 | PR https://github.com/sowndv02/teamboard/pull/18 |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | plan-review:TB-024:forwarded-flags | Cờ `ForwardedHeaders` được xử lý | `XForwardedFor` và `XForwardedProto` | 1 | 2026-10-25 |
| 2 | plan-review:TB-024:routing-order | `UseRouting` ngầm chạy trước `ForwardedHeaders` | Gọi tường minh `UseForwardedHeaders()` rồi `UseRouting()`, giữ design 6.4 | 1 | 2026-10-25 |
| 3 | plan-review:TB-024:slice-size | Giữ hay chia lát | Giữ một lát, gộp kịch bản | 1 | 2026-10-25 |
| 4 | plan-review:TB-024:forwarded-host | `X-Forwarded-Host` bằng `Host` client gửi | Chấp nhận, ghi giới hạn đã biết | 1 | 2026-10-25 |
| 5 | plan-review:TB-024:null-remote | `RemoteIpAddress` null | Bộ đếm chung `unknown` | 1 | 2026-10-25 |
| 6 | plan-review:TB-024:segments-default | Số đoạn của cửa sổ trượt | 6 đoạn (10 giây) | 1 | 2026-10-25 |
| 7 | test-review:TB-024:middleware-order-lock | Khoá thứ tự middleware đến đâu | Chỉ khoá phần quan sát được; sửa lời design 6.4 | 1 | 2026-10-25 |
| 8 | test-review:TB-024:forwarded-host-unused-column | Cột `X-Forwarded-Host` của upstream giả không có test đọc | Bỏ cột (YAGNI; giới hạn đã ghi) | 1 | 2026-10-25 |
| 9 | test-review:TB-024:forward-limit-untested | `ForwardLimit` mặc định chưa có test | Hoãn đến giai đoạn ALB, ghi giới hạn đã biết | 1 | 2026-10-25 |
| 10 | coding-review:TB-024:redundant-validate-data-annotations | `.ValidateDataAnnotations()` trên `RateLimitOptions` không kiểm gì (thuộc tính lồng nhau) | Bỏ dòng thừa | 1 | 2026-10-25 |
| 11 | coding-review:TB-024:design-d1-retry-after-stale | Design D1 nói `Retry-After` lấy từ metadata lease, code dùng độ dài cửa sổ | Sửa lời design D1 | 1 | 2026-10-25 |

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 (429 khi ghi quá hạn mức) | `rate-limit.feature` @S01 | Đỏ: mong 429, nhận 201 (chưa có rate limiter) | Xanh 2026-10-25 (B1-B4) |
| @S02 (đọc quá hạn mức, GET và HEAD chung, Retry-After) | `rate-limit.feature` @S02 Outline (GET, HEAD) | Đỏ: mong 429, nhận 200 | Xanh 2026-10-25 (B1-B4) |
| @S03 (IP khác, nhóm khác, health, fallback, sau khi cạn nhóm ghi hoặc nhóm đọc) | `rate-limit.feature` @S03 Outline (10 dòng) | Xanh sẵn: hành vi đúng khi chưa có limiter; step cạn hạn mức không tự assert (S01, S02, S08 chứng minh cạn); giữ làm test hồi quy, các đột biến "giới hạn health" và "gộp nhóm" làm các dòng này đỏ | Xanh sẵn |
| @S04 (IPv6 cùng /64 chung hạn mức) | `rate-limit.feature` @S04 Outline (2 dòng) | Dòng cùng /64 đỏ (mong 429, nhận 201); dòng /64 khác xanh sẵn (hồi quy) | Xanh 2026-10-25 (B1-B4) |
| @S05 (XFF từ nguồn không tin cậy, chưa cấu hình danh sách) | `rate-limit.feature` @S05 | Đỏ: mong 429, nhận 201 (chưa có limiter) | Xanh 2026-10-25 (B1-B4) |
| @S06 (proxy tin cậy dùng IP trong XFF) | `rate-limit.feature` @S06 Outline (2 dòng) | Dòng cùng client đỏ; dòng client khác xanh sẵn (hồi quy) | Xanh 2026-10-25 (B1-B4) |
| @S07 (upstream nhận XFF/Proto do gateway xác định) | `rate-limit.feature` @S07 | Xanh sẵn: YARP mặc định đã Set (đúng như spike); test đặc trưng | Xanh sẵn |
| @S08 (ngưỡng đóng gói 30/120) | `rate-limit.feature` @S08 Outline (2 dòng) | Đỏ: mong 429, nhận 201 và 200 | Xanh 2026-10-25 (B1-B4) |
| @S11 (có danh sách proxy, peer ngoài danh sách không né được) | `rate-limit.feature` @S11 | Đỏ: mong 429, nhận 201; đột biến "có danh sách thì tin mọi nguồn" làm test đỏ (thêm ở test-review, lỗ hổng P1 vòng 1) | Xanh 2026-10-25 (B1-B4) |
| @S12 (mạng proxy tin cậy `KnownIPNetworks`) | `rate-limit.feature` @S12 Outline (2 dòng) | Dòng cùng client đỏ; dòng client khác xanh sẵn (hồi quy) | Xanh 2026-10-25 (B1-B4) |
| @S13 (Retry-After theo cửa sổ cấu hình 120 giây) | `rate-limit.feature` @S13 | Đỏ: mong 429, nhận 201 | Xanh 2026-10-25 (B1-B4) |
| @S14 (không có địa chỉ client dùng chung bộ đếm) | `rate-limit.feature` @S14 | Đỏ: mong 429, nhận 201 | Xanh 2026-10-25 (B1-B4) |
| @S09 (cấu hình hợp lệ, giá trị sai làm khởi động thất bại và nêu tên khoá) | `RateLimitConfigurationTests` (2 test đóng gói, 8 ca sai, 4 ca đúng) | 10 test đỏ (thiếu mục `RateLimiting`, thiếu `RateLimiterPolicy`, giá trị sai vẫn khởi động được); 4 ca đúng xanh sẵn | Xanh 2026-10-25 (B1-B4) |
| @S10 (compose không bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED`) | `ComposePolicyTests` | Xanh sẵn: chưa ai đặt biến này; test hồi quy (quét mọi cấp thư mục trong `src`, bỏ qua comment) | Xanh sẵn |
| Hàm khoá IP | `ClientPartitionKeyTests` (9 ca: IPv4, IPv6 /64, khác /64 gồm ca sát biên, IPv4-mapped, `ScopeId`, `unknown`) | Đỏ: khung trả chuỗi rỗng (assertion); kiểm khoá không rỗng để hai ca bằng nhau không xanh giả | Xanh 2026-10-25 (B1-B4) |
| Mã lỗi `RATE_LIMIT_EXCEEDED` | `GatewayErrorCodesTests` (thêm 1 ca) | Xanh sẵn: dữ liệu hợp đồng đi cùng khung biên dịch | Xanh sẵn |

Lệnh đã chạy (2026-10-25, sau test-review vòng 1): `dotnet build src/gateway.Tests/gateway.Tests.csproj -warnaserror`; `dotnet test src/gateway.Tests/gateway.Tests.csproj --no-build --filter "Category!=docker"` (3 lần liên tiếp: 31 đỏ, 135 xanh, tổng 166); `dotnet format src/teamboard.slnx --verify-no-changes` (sạch). Test-init ban đầu: 26 đỏ, 129 xanh, tổng 155. Cài đặt tham chiếu của reviewer (thư mục tạm, không phải mã production) chứng minh 166/166 xanh được và bắt được các đột biến chính.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-25 | 93444b6 | Cần sửa feature | 1 P1 (`trusted-list-untrusted-peer`), 5 P2, 9 P3 (gồm 3 CONFIRM đã chốt: Confirm log #7-#9); review độc lập kèm bản tham chiếu và đột biến |
| 2 | 2026-10-25 | working tree sau vòng 1 | PASS | 0; các FIX vòng 2 (issue và plan) đã sửa; 3 P3 ghi nhận: `s03-exhaustion-unasserted` (xanh sẵn có lý do, ghi vào TDD log), `compose-test-minor` (đã sửa quét mọi cấp và comment cuối dòng), `program-cs-crlf-noise` (không đưa vào commit) |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-10-25 | working tree trên 33527c4 (chưa commit) | Sẵn sàng sau khi sửa | 2 P3 (`redundant-validate-data-annotations`, `design-d1-retry-after-stale`), đều có CONFIRM đã chốt |
| 2 | 2026-10-25 | working tree trên 33527c4 (delta vòng 1) | Sẵn sàng merge | 0 |

Giới hạn đã biết (không chặn merge): `Retry-After` bằng cả độ dài cửa sổ (60 giây) dù chỗ trống có thể xuất hiện sau khoảng một đoạn (10 giây); `X-Forwarded-Host` bằng `Host` client gửi (Confirm #4); `ForwardLimit` mặc định chưa có test (Confirm #9); bộ đếm trong bộ nhớ từng instance (design D1); gitleaks chưa cài.

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | coding-review:TB-024:redundant-validate-data-annotations | CONFIRM #10 | `src/gateway/RateLimiting/RateLimitingExtensions.cs` | `.ValidateDataAnnotations()` thừa, dễ gây hiểu nhầm | Xoá dòng; validator tuỳ biến vẫn kiểm Read/Write | gateway 166/166 xanh (gồm 10 ca @S09), build `-warnaserror` 0/0, format sạch; chưa commit |
| 1 | coding-review:TB-024:design-d1-retry-after-stale | CONFIRM #11 | `document/design/api-gateway.md` D1 và lịch sử thay đổi | Lời design nói lấy `Retry-After` từ metadata lease | Sửa: bằng độ dài cửa sổ của policy, không lấy từ lease; thêm dòng lịch sử | diff design; khớp `rate-limit.feature` @S02, @S13 |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
