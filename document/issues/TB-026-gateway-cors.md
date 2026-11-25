---
key: TB-026
title: Gateway lát 6, CORS theo allowlist
status: in-progress
phase: 1
branch: feature/TB-026-gateway-cors
estimate: S             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-10-25
started: 2026-11-25     # ngày chuyển in-progress
closed: null            # ngày done/dropped
---

# TB-026 — Gateway lát 6, CORS theo allowlist

## Bối cảnh
Issue khung cấp key khi duyệt bảng lát: lát 6 của `document/plans/api-gateway-slices.md` (spec `document/spec/api-gateway.md`, design `document/design/api-gateway.md`). Chi tiết phạm vi, tiêu chí và plan được lập khi tới lượt lát này (`plan-init`).

**Phụ thuộc (theo bảng lát):** 1, 4. Ước lượng của bảng lát: 1 ngày.

## Phạm vi
- Làm (theo hàng lát, FR/NFR: FR-005): CORS theo allowlist: preflight không tính rate limit, header cho phép/lộ ra, `429` vẫn mang header CORS
- Không làm: các lát khác của bảng lát.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Preflight `OPTIONS` từ origin trong allowlist trả header CORS, không chuyển xuống service, không bị rate limit (FR-005, spec 8.2; kịch bản @S01, @S04)
- [x] Origin ngoài allowlist không nhận header CORS (FR-005; @S02)
- [x] Header cho phép và lộ ra đúng design 6.3; `429` vẫn mang header CORS (@S03, @S05)
- [x] Không credentials, không `*`; origin lấy từ `Cors:AllowedOrigins` (@S06, @S07)
- [x] Gate xanh theo plan mục 6 (trừ gitleaks chưa cài, ghi rõ)

## Kế hoạch
Plan: [`document/plans/TB-026-gateway-cors.md`](../plans/TB-026-gateway-cors.md) (draft, 3 bước B1–B3, 7 kịch bản @S01–@S07, 3 câu hỏi mở P-01..P-03 chờ plan-review).

## Ghi chú / Nhật ký
- 2026-10-25: tạo issue khung cùng lúc cấp key cho toàn bộ bảng lát; chưa bắt đầu.
- 2026-11-25: plan-init. Nhánh `feature/TB-026-gateway-cors` tạo từ `origin/main` (TB-025 đã merge, PR 19). Người dùng chốt: lát 1 (TB-010) coi là `done`; khoá cấu hình `Cors:AllowedOrigins`.

## Workflow

Trạng thái các bước (skill `workflow`); cập nhật khi mỗi bước đạt.

| Bước | Trạng thái | Vòng | Ngày | Ghi chú |
|---|---|---|---|---|
| plan-init | done | – | 2026-11-25 | `document/plans/TB-026-gateway-cors.md` (draft), 3 bước B1–B3, 3 câu hỏi mở P-01..P-03 |
| plan-review | PASS | 2 | 2026-11-25 | Vòng 1: 4 CONFIRM, 3 FIX đã sửa; vòng 2 (delta): 4 CONFIRM đã chốt, PASS. Plan `approved` |
| test-init | done | – | 2026-11-25 | Đỏ 11/213 đúng lý do (xem "Kịch bản và TDD log"); xanh sẵn 2 khoá hợp đồng (S02, S06) |
| test-review | PASS | 1 | 2026-11-25 | 11 đỏ đúng lý do; S02, S06, CorsOptions empty-list xanh sẵn là khóa hợp đồng (người dùng chốt) |
| coding-start | done | – | 2026-11-25 | Xanh 213/213 (gateway.Tests, không docker); build -warnaserror và format xanh; xem "Kịch bản và TDD log" |
| coding-review | PASS | 2 | 2026-11-25 | Vòng 2 delta `610e75c`: 0 fingerprint mở; gate 215/215 |
| create-pr | pending | – | | |

## Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Kịch bản và TDD log

Đối chiếu kịch bản ↔ test (xem `.agents/rules/test-review.md` mục 6):

| Kịch bản | Test thực thi | Đỏ | Xanh |
|---|---|---|---|
| @S01 Preflight từ origin trong allowlist: 204, header CORS, không tới upstream | `cors.feature` "A preflight from an allowed origin..." | Đỏ: `status code is 204`, nhận 404 (OPTIONS rơi vào fallback, đúng A-01) | Xanh B1: 204, A-01 xác nhận (UseCors trả preflight không cần route OPTIONS) |
| @S02 Origin ngoài allowlist không có Allow-Origin (preflight và simple) | `cors.feature` Outline "An origin outside the allowlist..." (2 ví dụ) | Xanh sẵn (khoá hợp đồng) | Xanh (không đổi) |
| @S03 Simple request: Allow-Origin, expose headers, Vary | `cors.feature` "A simple request from an allowed origin..." | Đỏ: không có `Access-Control-Allow-Origin`, sau đó không có `Vary` | Xanh B1 sau khi thêm `UseGatewayCors` (Vary khi có Origin) |
| @S04 Preflight không bị rate limit | `cors.feature` "A preflight is not counted against the rate limit" | Đỏ: `status code is 204` không khớp (cùng nguyên nhân với S01) | Xanh (B2): CORS đứng trước RateLimiter nên preflight trả 204 trước khi vào hạn mức |
| @S05 429 mang header CORS | `cors.feature` "A 429 answered to an allowed origin..." | Đỏ: không có `Access-Control-Allow-Origin` trên 429 | Xanh (B2): header CORS được đặt trước khi RateLimiter ghi 429 |
| @S06 Không credentials, không `*` | `cors.feature` "The gateway never allows credentials or every origin" | Xanh sẵn (khoá hợp đồng) | Xanh (không đổi; policy không có AllowCredentials) |
| @S07 Origin lấy từ cấu hình (Outline 2 ví dụ) | `cors.feature` Outline "The allowed origins come from configuration" | Đỏ (allowed): không có Allow-Origin; blocked xanh sẵn | Xanh: policy đọc `Cors:AllowedOrigins` qua Options |
| P-01 Vary, P-02 allowlist rỗng, mục 3 B1 | `CorsOptionsTests` `Validate_NotAnAbsoluteOrigin_Fails` (5 ví dụ: `*`, path, query, không phải URL, rỗng) | Đỏ 5/5: stub `CorsOptionsValidator` luôn Success | Xanh 5/5: validator kiểm `scheme://host[:port]` khớp chính xác |
| P-02 allowlist rỗng hợp lệ | `CorsOptionsTests.Validate_EmptyList_Succeeds` | Xanh sẵn (hồi quy) | Xanh |
| Mặc định `http://localhost:4200` trong `appsettings.json` | `ConfigurationTests.ShippedCors_AllowsOnlyTheLocalAngularDevOrigin` | Đỏ: "appsettings.json has no Cors section." | Xanh: thêm section `Cors` |

Lệnh xanh (coding-start, cuối B3): `dotnet test src/gateway.Tests/gateway.Tests.csproj --filter "Category!=docker"` → **Passed 213/213**. `dotnet build src/teamboard.slnx -warnaserror` → 0 warning, 0 error. `dotnet format src/teamboard.slnx --verify-no-changes` không báo lỗi. `dotnet list ... package --vulnerable` không có gói lỗi cho gateway. gitleaks chưa cài (ghi rõ, không chạy).

Ghi chú coding:
- Khi viết B1, `UseCors` đã đặt trước `RateLimiter` nên B2 (@S04, @S05) xanh ngay mà không có bước đỏ riêng. Đây là lệch so với quy trình đỏ→xanh theo bước; kết quả đỏ của S04/S05 là từ test-init.
- ASP.NET CORS **không** tự thêm `Vary: Origin` cho policy này (phát hiện khi chạy @S03). Đã thêm `UseGatewayCors` (thêm `Vary: Origin` khi request có header `Origin`), đúng quyết định P-01 đã chốt.
- Đổi tên tham chiếu: `Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions` dùng đường dẫn đầy đủ để tránh trùng với `Gateway.CorsOptions`.

Lệnh đã chạy: `dotnet test src/gateway.Tests/gateway.Tests.csproj --filter "Category!=docker"`: **Failed 11, Passed 202, Total 213**. 11 đỏ = 1 (S01) + 1 (S03) + 1 (S04) + 1 (S05) + 1 (S07 allowed) + 5 (CorsOptions) + 1 (ConfigurationTests). Build `-warnaserror` xanh; `dotnet format --verify-no-changes` không báo lỗi.

Ghi chú: S02, S06 và `Validate_EmptyList_Succeeds` xanh trước khi có code CORS. Đây là khoá hợp đồng/hồi quy (không có header CORS khi chưa có CORS), không phải đỏ→xanh. Đánh dấu cho test-review quyết định có chấp nhận.

Khung production tối thiểu (stub, không logic): `src/gateway/CorsOptions.cs` (`AllowedOrigins`), `src/gateway/CorsOptionsValidator.cs` (luôn Success). Chưa đăng ký trong `Program.cs`, chưa thêm `Cors` vào `appsettings.json`.

## Test review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | working tree sau test-init (chưa commit) | PASS | 0 (1 FIX đã sửa: format Examples S07; 1 FIX plan: Vary chỉ S01, S03; 1 CONFIRM guard đã chốt) |

## Review rounds

| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
|---|---|---|---|---|
| 1 | 2026-11-25 | `cb40107` + working tree (CORS chưa commit) | NEEDS_FIX | `workflow:TB-026:uncommitted-production` |
| 2 | 2026-11-25 | `610e75c` | PASS | 0 |

## Fix log

Mọi thay đổi agent đã sửa trong review (xem `review-loop.md` quy tắc 12):

| Vòng | Fingerprint | Nguồn (FIX / CONFIRM #n) | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (diff, commit, test) |
|---|---|---|---|---|---|---|
| 1 | workflow:TB-026:uncommitted-production | coding-review vòng 1 FIX | git, `src/gateway` | Implementation CORS chỉ ở working tree | Commit `610e75c` feat | Vòng 2 PASS; gate 215/215 |

## Bài học (lessons learned)

Liên kết lesson sinh ra từ issue này (`document/lessons/LL-NNN-*.md`), hoặc ghi "không có". Skill `lesson-learned`.

- 
