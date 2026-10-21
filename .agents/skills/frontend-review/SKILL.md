---
name: frontend-review
description: Review code/PR Angular (diff chạm src/web) - signals/RxJS, change detection, bảo mật client, token, a11y, i18n, bundle và hiệu năng, test hành vi, contract OpenAPI, quan sát - phân loại CONFIRM/FIX, người dùng chốt, lặp đến PASS. Dùng bên trong coding-review khi diff có frontend, hoặc khi người dùng yêu cầu review code Angular.
---

# Frontend review (Angular)

Bổ sung checklist riêng cho frontend vào `coding-review`. Vòng lặp, CONFIRM/FIX, Confirm log, PASS theo `../workflow/references/review-loop.md`; mức P0–P3, probe chung, định dạng báo cáo theo `code-review.md`. Skill này **không thay** các probe đó; nó thêm probe cho Angular.

Rule đối chiếu: `angular.md`, `angular-reactivity.md`, `frontend-engineering.md`, `frontend-testing.md`, `api-endpoints.md`, `quality-gate.md`, `dependency-policy.md`.

## Cổng đầu vào
1. Test và gate frontend **xanh** (lint `--max-warnings=0`, format, typecheck, test gồm policy test, build đạt budgets, `openapi:check`, `npm audit --omit=dev --audit-level=high`). Đỏ → dừng, trả về `angular-feature`/`coding-start`.
2. Diff xác định được so với `main`; không có secret, `node_modules`, `dist`, `.angular` trong diff.
3. Đã có bằng chứng xem trên trình duyệt (golden path + đường lỗi) trong issue; thiếu → FIX `major`.

## Checklist (vòng 1 chạy hết; làn nhỏ chỉ phần liên quan diff)

### A. Reactivity và change detection (`angular-reactivity.md`)
- `effect` dùng để đồng bộ state hoặc gọi API (phải là `computed`/`linkedSignal`/`resource`) → FIX.
- Subscribe không huỷ, subscribe lồng, `shareReplay(1)` trần, `Subject` làm kho state → FIX.
- Đột biến mảng/đối tượng trong signal; đọc signal rồi cache; tạo signal trong vòng lặp/hàm gọi lặp → FIX.
- Thiếu `OnPush`, hàm nặng trong template, `@for` không `track` ổn định, danh sách lớn không phân trang/ảo hoá → FIX; nếu cần quyết định đánh đổi → CONFIRM.
- `eslint-disable`/`@ts-ignore` cho rule bắt buộc → `blocker`.

### B. Cấu trúc và kiểu (`angular.md`)
- Vi phạm ranh giới feature (import chéo), logic nghiệp vụ trong `shared/`, component gọi API trực tiếp.
- `any`, ép kiểu `as` để lách, model tự viết trùng kiểu sinh từ OpenAPI.
- Template dùng `*ngIf/*ngFor` (code mới), `@Input()` decorator, không `inject()`.

### C. Bảo mật client (`frontend-engineering.md`; lăng kính Bảo mật, F8)
- XSS: `innerHTML`, `bypassSecurityTrust*`, dựng URL/HTML từ dữ liệu người dùng → `blocker`.
- Token/secret trong `localStorage`/bundle/config/URL/log; nơi lưu token khác ADR đã chốt → `blocker` (hoặc CONFIRM nếu ADR chưa chốt).
- Guard mặc định mở, route mới không khai báo quyền; ẩn UI được coi là bảo mật (server vẫn phải kiểm) → FIX.
- Interceptor gắn token cho origin lạ; CORS/redirect (OIDC: `redirect_uri` cố định, `state`/`nonce`/PKCE đúng) → `blocker`.
- Phụ thuộc mới: hạng T3/giấy phép lạ/lỗ hổng High+ (`dependency-policy.md`) → CONFIRM hoặc `blocker`.

### D. Lỗi, i18n, cấu hình
- Chuỗi hiển thị inline, `aria-label` không qua i18n, rẽ nhánh theo `error.message` thay vì `code`, lộ stack trace.
- URL/route/quyền/timeout inline; config bake theo môi trường thay vì runtime; cờ mock lọt build production.

### E. A11y (WCAG 2.2 AA)
- Phần tử không ngữ nghĩa (`div` click), thiếu label/`alt`, bẫy focus, không điều hướng bằng bàn phím, tương phản, thông báo lỗi không liên kết với field; axe có vi phạm serious/critical → FIX.

### F. Test (`frontend-testing.md`)
- Test bám chi tiết cài đặt, selector theo class CSS, `sleep`/chờ cố định, mock HTTP không bám schema, kịch bản BDD luồng sống còn thiếu, coverage vùng đã có giảm → FIX.
- Hành vi chưa được spec quy định mà code tự chọn → CONFIRM.

### G. Hiệu năng và quan sát
- Bundle tăng vượt ngân sách, không lazy-load route/feature nặng, thư viện lớn kéo cả bundle, ảnh không `NgOptimizedImage`.
- Thiếu `correlationId`/`traceparent` trong request, lỗi JS không được ghi nhận, log chứa PII.

### H. Lăng kính Chất lượng / Bảo mật / Chi phí (`../workflow/references/review-philosophy.md` mục 4)
Báo cáo có bảng ngắn cho ba lăng kính. Chi phí FE: kích thước bundle/băng thông, số request thừa, polling, phụ thuộc nặng.

## Phân loại và vòng lặp
- **CONFIRM** (người dùng quyết): hành vi/UX spec không nói; đánh đổi hiệu năng/bảo mật/phạm vi; thêm thư viện; đổi kiến trúc FE hoặc nơi lưu token; breaking đổi contract API.
- **FIX** (agent sửa): lỗi rõ một cách sửa (huỷ subscription, đổi `effect` → `computed`, thêm `track`, bỏ hardcode, thêm label, thêm test hành vi).
- Hỏi theo lô ≤ 4 câu, ghi Confirm log, không tự chọn khuyến nghị. Sửa theo lô ≤ 3 finding, mỗi finding có test/evidence, **chạy lại gate FE đầy đủ** trước khi review delta.
- Mức: P0–P2 = must fix (chặn merge); P3 = nit (người dùng chốt sửa hay bỏ, có lý do).

## Review độc lập
Nên chạy trong phiên/sub-agent khác với phiên đã viết code (`review-loop.md` mục 6): đưa diff, spec/plan, rule; không đưa lập luận của người viết. Nếu PR đã có comment của người thật: mỗi comment là một finding, trả lời từng cái.

## Làn (`../workflow/references/lanes.md`)
Làn nhỏ: một vòng, chạy phần A–G liên quan diff, gate FE vẫn bắt buộc. Làn lớn (đổi auth/contract): đủ checklist, nên review độc lập.

## Ngoài đời thực
Review FE thật tập trung vào hành vi trên trình duyệt, a11y, bundle và bảo mật phía client; reviewer thường mở preview deploy để thử. Ở đây hãy yêu cầu bằng chứng (ảnh/ghi chú/đầu ra axe, báo cáo budget) thay vì tin lời "chạy ổn".

## Không làm
- Không review khi gate FE chưa xanh.
- Không tự quyết UX, thư viện, đánh đổi bảo mật; không áp dụng khuyến nghị khi chưa được trả lời.
- Không sửa code mà không có test/evidence; không tắt lint/test/budget để qua.
- Không sửa spec/ADR cũ ngầm.

## PASS khi
- Verdict `PASS` theo `coding-review` (0 CONFIRM mở, không P0–P2 mở, P3 đã sửa hoặc `skipped` có lý do); lần chạy gate FE cuối **xanh**.
- Issue: Review rounds đầy đủ, Confirm log ghi mọi quyết định; báo `workflow` mở `create-pr`.

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md` (làn lớn bắt buộc): ghi trước khi bắt đầu, sau mỗi vòng/khi cần người dùng chốt/khi bị chặn, và khi hoàn thành (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue.
