# Angular rules

Lý do tồn tại: một chuẩn Angular chung để code frontend nhất quán, kiểm được bằng gate và giống cách các dự án thật làm (standalone, signals, zoneless, strict). Áp dụng khi sửa `src/web/**` (ADR 0009 cho phép frontend; `src/web` chưa scaffold thì rule này là đích đến khi dựng).

Bổ sung: `angular-reactivity.md` (Signals/RxJS/effect), `frontend-engineering.md` (i18n, lỗi, config, RBAC, bảo mật client, observability), `frontend-testing.md` (test), `api-endpoints.md` (hình dạng API), `quality-gate.md` (gate).

Phiên bản Angular, Node, package manager: `config/tool-versions.yaml` (`TODO` cho tới khi scaffold; xác minh tại angular.dev, không tự điền). Mọi API dưới đây được hiểu theo bản ổn định của phiên bản đã chốt; API còn experimental thì không dùng cho code production.

## 1. Cấu trúc và ranh giới

- Standalone component/directive/pipe (không NgModule). `ChangeDetectionStrategy.OnPush` bắt buộc.
- **Zoneless** cho code mới: không phụ thuộc `zone.js` (`provideZonelessChangeDetection()` hoặc mặc định của phiên bản đã chốt). Mọi cập nhật UI đi qua signal, `async` pipe hoặc event của template; không dựa vào "tự động phát hiện" của zone (`setTimeout` đổi field rồi mong UI cập nhật là lỗi).
- Tổ chức theo **feature**: `src/app/features/<feature>/` (`pages/`, `components/`, `data-access/`, `models/`, `<feature>.routes.ts`); `src/app/core/` (auth, interceptor, config, layout, i18n, errors, observability); `src/app/shared/` (UI dùng lại, pipe, directive, hằng; **không** logic nghiệp vụ, **không** gọi API).
- **Ranh giới import kiểm bằng lint** (`eslint-plugin-boundaries` hoặc `no-restricted-imports`): `features/a` không import `features/b`; `shared` không import `features` hay `core`; `core` không import `features`. Muốn dùng chung thì đưa xuống `shared/` (UI) hoặc `core/` (hạ tầng). Vi phạm ranh giới là lỗi lint, không phải gợi ý.
- Component chỉ lo hiển thị và điều phối; truy cập dữ liệu và logic đặt trong service/store của feature (`data-access/`). Component "thông minh" (page) tách khỏi component "ngốc" (presentational: chỉ `input()`/`output()`).
- Lazy-load route theo feature; tên tệp theo kiểu của style guide Angular hiện hành (một khái niệm chính mỗi file).
- Frontend chỉ nói chuyện với backend qua **gateway** (`/api/v1/...`), không gọi thẳng service, không truy cập DB.

## 2. TypeScript

- `strict: true` cùng `noImplicitOverride`, `noFallthroughCasesInSwitch`, **`noUncheckedIndexedAccess`**, `exactOptionalPropertyTypes` (nếu thư viện dùng không vướng); Angular `strictTemplates`, `strictInjectionParameters`, `strictInputAccessModifiers` bật.
- Cấm `any` (dùng `unknown` rồi thu hẹp kiểu); không `// @ts-ignore`, `@ts-nocheck`; không `as` để lách lỗi kiểu (`as const` và thu hẹp có kiểm tra thì được).
- Model API khai báo riêng, **kiểu sinh từ OpenAPI** của service (`frontend-engineering.md` mục 8); không viết tay lại DTO backend.
- `readonly` cho field không đổi; `type` import (`import type`) khi chỉ dùng làm kiểu.

## 3. Component và template

- Dùng `inject()` thay constructor injection; `input()`, `input.required()`, `output()`, `model()`, `viewChild()`/`contentChild()` (hàm), không decorator `@Input/@Output/@ViewChild` cho code mới.
- Template dùng control flow `@if`, `@for` (luôn có `track` ổn định), `@switch`, `@let`; không `*ngIf/*ngFor/*ngSwitch` cho code mới.
- **`@defer`** cho phần nặng/dưới màn hình đầu (biểu đồ, editor, màn hình hiếm dùng) với `prefetch` hợp lý và `@placeholder`/`@loading`/`@error`; không defer nội dung LCP.
- Ảnh tĩnh dùng `NgOptimizedImage` (`ngSrc`, `width`/`height`, `priority` cho ảnh LCP).
- Không logic nặng trong template; dùng `computed` hoặc pipe thuần. Không gọi hàm có side effect trong template.
- Host binding qua object `host: {}` của decorator, không `@HostBinding/@HostListener` cho code mới.
- Style: component style gắn đúng phạm vi, theo token thiết kế dùng chung (`shared/styles`); không `!important` rải rác; không `::ng-deep`.

## 4. Form

- Reactive Forms có kiểu (`FormGroup<...>`, `NonNullableFormBuilder`). **Signal Forms** chỉ dùng khi bản ổn định trong phiên bản đã chốt (xem `config/tool-versions.yaml`); trước đó không dùng API experimental.
- Validate ở client để phản hồi nhanh **và** ở server (server là nguồn sự thật); hiển thị lỗi theo `fieldErrors` của `problem+json` (`frontend-engineering.md` mục 2).
- Nút gửi bị vô hiệu khi đang gửi (chống double submit); trạng thái tải/lỗi/thành công luôn có.

## 5. Routing

- Route lazy (`loadComponent`/`loadChildren`), cấu hình tập trung từng feature; `provideRouter(..., withComponentInputBinding())` để nhận param qua `input()`.
- Guard/resolver là **hàm** (`CanActivateFn`, `ResolveFn`); mặc định đóng (`frontend-engineering.md` mục 4).
- Cấm điều hướng bằng `window.location`; dùng `Router`/`routerLink`. Có route `**` cho trang không tìm thấy.

## 6. Hiệu năng

- Ngân sách bundle (`budgets` trong `angular.json`: `initial`, `anyComponentStyle`) bật; vượt budget = lỗi build (gate). Không nới budget để qua gate; tách `@defer`/lazy hoặc bỏ thư viện.
- Phân trang phía server cho danh sách lớn (`api-endpoints.md` mục 4) hoặc virtual scroll; không render hàng nghìn phần tử.
- Mục tiêu Web Vitals (đo bằng Lighthouse CI khi có môi trường chạy): LCP, CLS, INP/TBT trong ngưỡng "tốt" của web.dev; ngưỡng chính xác ghi ở `quality-gate.md` khi áp dụng.
- **SSR/hydration** là quyết định kiến trúc (ADR 0009 mục mở). Mặc định **SPA**; chỉ bật SSR khi có ADR, vì kéo theo chạy Node server và ràng buộc code (không chạm `window`/`document` trực tiếp).

## 7. Truy cập được (a11y) và UI

- Chuẩn **WCAG 2.2 mức AA**: phần tử ngữ nghĩa (`button`, `a`, `nav`, `main`), nhãn cho input (`label`/`aria-label`), quản lý focus khi mở/đóng dialog và sau điều hướng, điều hướng bằng bàn phím, tương phản đủ, kích thước vùng chạm tối thiểu, không truyền thông tin chỉ bằng màu; không click handler trên `div`/`span`.
- Lint template bật nhóm `@angular-eslint/template/accessibility`; test tự động bằng axe (`frontend-testing.md`).
- Không `innerHTML`, `bypassSecurityTrust*` với dữ liệu người dùng; để Angular sanitize.
- Chuỗi hiển thị qua khoá i18n, không hardcode (`frontend-engineering.md` mục 1).

## 8. Chất lượng và công cụ

- ESLint flat config (`angular-eslint`, `typescript-eslint` strict + stylistic, `eslint-plugin-sonarjs`, `eslint-plugin-rxjs-x`, `eslint-plugin-boundaries`), `--max-warnings=0`; rule chỉ `error` hoặc `off`; cognitive complexity ≤ 15. Prettier `--check`. Chi tiết và lệnh: `quality-gate.md`, `COMMANDS.md`.
- Không `eslint-disable` khi chưa có lý do ghi chú; rule phản ứng/bảo mật: cấm tắt trong production (`angular-reactivity.md` mục 5).
- Build production sạch warning; `npm ci` (lockfile) cho gate; dependency pin theo `dependency-pinning.md`, chính sách `dependency-policy.md`.
- Không `console.log` trong code production (dùng service log chung, `frontend-engineering.md` mục 9).

## Checklist

- [ ] Standalone + OnPush; không phụ thuộc zone.js; control flow mới, `@for` có `track`
- [ ] Không import chéo feature; `shared` không chứa nghiệp vụ; lint ranh giới xanh
- [ ] TypeScript strict, không `any`/`@ts-ignore`; kiểu API từ OpenAPI
- [ ] Form có kiểu, chống double submit, xử lý `fieldErrors`
- [ ] Route lazy, guard hàm mặc định đóng
- [ ] a11y WCAG 2.2 AA: ngữ nghĩa, nhãn, focus, bàn phím; axe không có vi phạm serious/critical
- [ ] Bundle trong budget; `@defer`/`NgOptimizedImage` áp dụng đúng chỗ
- [ ] Gate FE đã chạy; UI đã xem trên trình duyệt (golden path + một lỗi)
