# Frontend engineering (Angular)

Lý do tồn tại: giữ UI nhất quán, đa ngôn ngữ được, lỗi xử lý theo mã (không theo chuỗi), không có giá trị cấu hình/secret nằm rải rác trong component, và frontend khớp contract backend mà không phải đoán. Áp dụng khi sửa `src/web/**`.

Bổ sung cho: `angular.md` (cấu trúc, TypeScript, form, a11y), `angular-reactivity.md` (Signals/RxJS), `frontend-testing.md` (test), `api-endpoints.md` (hình dạng API), `quality-gate.md`. Quyết định còn mở của frontend nằm ở ADR 0009 (`TODO`: nơi lưu token, SSR hay SPA, công cụ i18n, thư viện server-state).

## 1. Chống hardcode: phân loại và nơi đặt

| Loại | Quy tắc | Nơi đặt |
|---|---|---|
| **Chữ hiển thị** (label, title, toast, placeholder, lỗi, `aria-label`, `alt`) | Qua i18n bằng **khoá**; không chuỗi trong template/TS | `src/app/core/i18n/` (`message-keys.ts` + file dịch `vi.ts`/`en.ts`) |
| **Lỗi ứng dụng** | `AppError` với mã từ registry; không literal `code`/`message` | `src/app/core/errors/` |
| **Config** (URL API, timeout, feature flag) | Từ config runtime; không URL/số inline | `src/app/core/config/` |
| **Route** | Hằng số đường dẫn; không `'/boards/' + id` rải rác | `route.constants.ts` |
| **Quyền (RBAC)** | Hằng permission/scope; không chuỗi quyền inline | `permission.constants.ts` |
| **Khởi động / invariant** | Message trong file hằng; không `throw new Error('...')` inline | `bootstrap.constants.ts`, `invariant-messages.ts` |
| **Magic number/string** | Từ lần lặp thứ 2 hoặc có nghĩa nghiệp vụ → hằng đặt tên | `shared/constants/` hoặc `constants` của feature |
| **Secret** | **Không bao giờ** có trong frontend (mọi thứ trong bundle là công khai) | – |

- Message invariant/dev bằng tiếng **Anh**; chữ cho người dùng cuối qua i18n.
- Công cụ i18n (`$localize` hay thư viện) là `TODO`/ADR khi chốt. Trước đó vẫn gom chuỗi theo khoá ở một nơi; không rải chuỗi trong template.
- Ngày/số/tiền theo locale (`Intl`/pipe); dữ liệu API là UTC ISO 8601.

## 2. Lỗi ứng dụng

- Backend trả `problem+json` có `code` ổn định (`api-endpoints.md` mục 5, `backend-engineering.md` mục 1). Frontend:
  - Interceptor chuyển lỗi sang `AppError` { `code`, `status`, `correlationId`, `traceId`, `fieldErrors` }.
  - Hiển thị bằng cách **tra khoá i18n theo `code`**; không rẽ nhánh theo `error.message`; `code` lạ → thông báo chung.
  - Lỗi không lường trước hiển thị `correlationId` để người dùng báo lại.
- `ErrorCode` phía frontend khớp registry backend (nguồn sự thật: contract trong `document/contracts/` hoặc `src/shared`/OpenAPI); đổi mã ở backend thì cập nhật frontend và test cùng thay đổi (`error-code-policy`).
- Mã lỗi **phía client** (mất mạng, hết phiên, validate cục bộ) có enum riêng `ClientErrorCode`, không trộn mã backend.
- Không `console.error`/`alert` làm xử lý lỗi; dùng service thông báo/log chung (mục 9). Có **error boundary** toàn cục (`ErrorHandler` tuỳ biến) để lỗi chưa bắt được vào log chung, không để trắng trang.
- Lỗi mạng/5xx: thông báo thân thiện, cho phép thử lại; không lộ stack trace hay chi tiết nội bộ.

## 3. Cấu hình môi trường

- **Một bản build chạy ở mọi môi trường**: cấu hình lấy từ file runtime (ví dụ `/config.json` ở gốc hosting) nạp lúc khởi động bằng `provideAppInitializer`; không bake giá trị theo môi trường vào bundle.
- Config runtime chỉ chứa giá trị **không bí mật** (URL gateway, cờ tính năng, tên môi trường). Có validate khi nạp; thiếu/sai → màn hình lỗi cấu hình rõ ràng, không chạy nửa vời.
- URL API tương đối `/api/v1` qua gateway khi cùng origin; chỉ cấu hình host khi khác origin.
- Cờ mock/dev (ví dụ đăng nhập giả) **chỉ** bật ở local; không tồn tại ở build stg/prod (`no-mock-in-prod-bundle`).
- `environment.ts` (build-time) chỉ cho `production: boolean` và thứ cố định theo build.

## 4. Routing và phân quyền (RBAC)

- Route khai báo tập trung, lazy theo feature; đường dẫn từ `route.constants.ts`.
- Guard (`CanActivateFn`) kiểm tra xác thực và quyền theo permission/scope hằng; mặc định **đóng** (route mới phải khai báo quyền rõ ràng).
- Ẩn/hiện UI theo quyền chỉ là **trải nghiệm**, không phải bảo mật; server luôn kiểm tra lại (`api-endpoints.md` mục 7).
- Điều hướng sau đăng nhập/hết phiên đi qua một service duy nhất; không `window.location` rải rác.

## 5. Xác thực phía client (OIDC) và bảo mật

- Đăng nhập dùng **Authorization Code + PKCE** với `identity-service` (spec `document/spec/identity-auth.md`); client công khai, không có secret; `state`/`nonce`/`code_verifier` do thư viện OIDC hoặc code đã được review sinh bằng `crypto.getRandomValues`. Không dùng Implicit hay Resource Owner Password.
- **Nơi lưu token là `TODO`/ADR** (ADR 0009 và ADR 0006 nêu cần chốt): lựa chọn gồm BFF/cookie `HttpOnly` hoặc access token trong bộ nhớ + refresh qua cookie `HttpOnly`. Trước khi chốt: **không** lưu token ở `localStorage`/`sessionStorage`, không vào URL/log.
- Interceptor gắn `Authorization: Bearer` chỉ cho request tới gateway cấu hình; tự refresh khi 401 có khoá tránh gọi refresh song song; refresh thất bại → về màn đăng nhập, xoá state.
- Không `innerHTML`, `bypassSecurityTrust*` với dữ liệu người dùng; để Angular sanitize. Không `eval`/`new Function`.
- Không đưa PII/token vào URL, query, log trình duyệt.
- CSP, HSTS, `X-Content-Type-Options`, `frame-ancestors`: cấu hình ở gateway/hosting (CloudFront), không phụ thuộc client; ứng dụng không cần `unsafe-inline`/`unsafe-eval` (nonce hoặc hash cho style/script cần thiết).
- CORS cấu hình ở gateway, danh sách origin cụ thể (không `*` với credentials).
- Phụ thuộc npm: pin chính xác, `npm ci`, `npm audit --omit=dev --audit-level=high`, kiểm giấy phép (`dependency-policy.md`, `dependency-pinning.md`, `quality-gate.md`).

## 6. Mock và dữ liệu dev

- Repository/service mock đặt cạnh bản thật với hậu tố rõ (`*.repository.mock.ts`); chọn bản qua DI/config, không `if (isMock)` rải rác.
- Message trong mock/fixture lấy từ file hằng, không literal lặp; fixture không chứa dữ liệu thật hay secret.
- Mock chỉ cho dev/test; build production không bundle mock (`no-mock-in-prod-bundle`).

## 7. i18n và a11y

- Mọi chữ hiển thị qua khoá, kể cả `aria-label`/`alt`/title. Mọi khoá có đủ bản dịch các locale hỗ trợ (`i18n-keys-complete`).
- Hướng đọc/độ dài chuỗi không làm vỡ layout (kiểm với bản dịch dài nhất); chi tiết a11y: `angular.md` mục 7.

## 8. Contract với backend (kiểu sinh từ OpenAPI)

- Nguồn sự thật của request/response là **OpenAPI của từng service** (`api-endpoints.md`). Kiểu TypeScript và client được **sinh** từ OpenAPI (công cụ sinh là `TODO`/ADR, ví dụ `openapi-typescript`); không viết tay DTO.
- Script `openapi:check` sinh lại kiểu từ spec đã commit (`document/contracts/` hoặc artifact CI) và `git diff --exit-code`: lệch = lỗi gate. Đổi contract thì cập nhật cả hai phía trong cùng thay đổi, kèm test.
- Thay đổi phá vỡ (đổi nghĩa/xoá trường) theo version hoá của `api-endpoints.md` mục 7; frontend chịu được trường lạ (không dùng `strict` parse từ chối trường thừa).
- Hình dạng lỗi `problem+json` và phân trang khớp `api-endpoints.md`; client dùng kiểu chung cho `ProblemDetails` và `Page<T>`.

## 9. Quan sát frontend

- Mọi request gắn `traceparent` (W3C) và `X-Correlation-Id` do interceptor, theo `observability.md`; `correlationId` sinh ở client khi chưa có.
- Lỗi JS chưa bắt được và lỗi tải chunk gửi về điểm thu log do ADR/`TODO` chọn (qua gateway), có `correlationId`, phiên bản build, route; không kèm PII/token. Có service log chung với mức (debug/info/warn/error), tắt `debug` ở stg/prod; không `console.log` trong production.
- Đo Web Vitals (LCP, CLS, INP) bằng `web-vitals` và gửi về cùng điểm thu khi có hạ tầng; Lighthouse CI đo trên build production (`quality-gate.md`, 🚧 tới khi có môi trường chạy).
- Gắn phiên bản build (commit/tag) vào config runtime để liên hệ lỗi với bản deploy.

## 10. Quy tắc chính sách tự động (policy tests, chạy trong gate)

Thêm khi dựng `web`; tên test cố định để `quality-gate.md` trỏ tới:

- `no-hardcode-policy`: quét `src/app/**` (trừ constants, i18n, spec) tìm chuỗi hiển thị inline, URL tuyệt đối, `throw new Error('...')` inline.
- `error-code-policy`: mọi `ErrorCode` có khoá i18n; không `code`/`message` literal ngoài registry.
- `i18n-keys-complete`: mọi khoá có đủ locale; không khoá thừa/thiếu.
- `no-mock-in-prod-bundle`: build production không chứa mock/fixture/cờ dev.
- Lint cấm `eslint-disable` các rule bắt buộc (`angular-reactivity.md` mục 5).

## Checklist trước khi báo xong thay đổi frontend

- [ ] Không chuỗi hiển thị inline; chữ qua khoá i18n (kể cả `aria-label`); đủ locale
- [ ] Lỗi qua `AppError` + tra khoá theo `code`; không literal `code`/`message`
- [ ] Không URL/permission/route/timeout inline; dùng hằng và config runtime
- [ ] Không secret/PII/token trong bundle, URL, log, `localStorage`; không `innerHTML`/`bypassSecurityTrust*` với dữ liệu người dùng
- [ ] Route mới có guard và quyền; server vẫn kiểm tra quyền
- [ ] Kiểu API sinh từ OpenAPI; `openapi:check` xanh
- [ ] Request mang `traceparent` + `X-Correlation-Id`; lỗi vào service log chung
- [ ] Mock không lọt vào build production
- [ ] Gate đã chạy (`code-change-gate.md`, `quality-gate.md`); UI đã xem trên trình duyệt
