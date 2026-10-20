# Angular rules

Frontend của dự án là Angular SPA (`src/web`, xem ADR 0005). Rule chung cho mọi code Angular/TypeScript.

## Cấu trúc & kiến trúc

- Standalone component (không NgModule), `ChangeDetectionStrategy.OnPush`, lazy-load route theo feature.
- Tổ chức theo **feature**: `src/app/features/<feature>/` (pages, components, data-access, models); `src/app/core/` (auth, http interceptor, config, layout); `src/app/shared/` (component/pipe/directive dùng lại, **không** chứa logic nghiệp vụ). Feature không import trực tiếp từ feature khác; chia sẻ qua `shared/` hoặc `core/`.
- Component chỉ lo hiển thị; gọi API và logic dữ liệu đặt trong service/store của feature.
- Frontend chỉ nói chuyện với backend qua **gateway** (`/api/v1/...`), không gọi thẳng service nào, không truy cập DB.

## TypeScript

- `strict: true` (gồm `noImplicitAny`, `strictNullChecks`); cấm `any` (dùng `unknown` + thu hẹp kiểu); không `// @ts-ignore`, `as` ép kiểu để lách lỗi.
- Model API khai báo `interface`/type riêng, khớp contract của service; không dùng lại entity từ backend.

## State & dữ liệu

Chi tiết Signals/`effect`/RxJS: `angular-reactivity.md`. i18n, mã lỗi, config, route/RBAC: `frontend-engineering.md`.

- Dùng **Signals** cho state cục bộ/dẫn xuất (`signal`, `computed`, `effect` chỉ cho side-effect thật cần). Server state qua service dùng `HttpClient` + `resource`/`rxResource` hoặc RxJS có kiểu.
- RxJS: hủy subscription đúng cách (`takeUntilDestroyed`, `async` pipe/`toSignal`), không subscribe lồng nhau; không để subscription rò rỉ.
- Form: Reactive Forms có kiểu (`FormGroup<...>`), validate ở client **và** server (server là nguồn sự thật).

## HTTP

Hình dạng request/response/lỗi/phân trang do `api-endpoints.md` quy định; client Angular phải khớp.

- Một `HttpClient` interceptor cho: gắn token, `correlationId` (header `X-Correlation-Id`), xử lý lỗi chung (map `problem+json` sang lỗi hiển thị).
- Base URL API lấy từ config runtime (`config/` hoặc `environment`), không hardcode trong component.
- Lỗi mạng/5xx hiển thị thông báo thân thiện, không lộ stack trace hay chi tiết nội bộ.

## UI, a11y, i18n

- Template dùng control flow mới (`@if`, `@for` có `track`, `@switch`), không `*ngIf/*ngFor` cho code mới.
- Truy cập được: dùng phần tử ngữ nghĩa, label cho input, điều hướng bằng bàn phím, tương phản đủ; không click handler trên `div`.
- Chuỗi hiển thị không hardcode rải rác; gom để dễ i18n sau.
- Không dùng `innerHTML`/`bypassSecurityTrust*` với dữ liệu người dùng.

## Test

- Unit test cho service, pipe, logic component; component test cho hành vi người dùng thấy (không test chi tiết cài đặt). Dùng runner mặc định của version Angular đang dùng (`ng test`).
- Mock HTTP bằng `provideHttpClientTesting`, không gọi mạng thật. Test dùng selector bền (role/label/`data-testid`), không bám class CSS.

## Chất lượng

- ESLint (angular-eslint) + Prettier; chạy trong gate (`code-change-gate.md`). Không tắt rule bằng `eslint-disable` khi chưa có lý do ghi chú.
- Build production phải sạch warning (budget kích thước bundle được cấu hình, vượt budget = lỗi).
- Dependency pin theo `dependency-pinning.md`.
