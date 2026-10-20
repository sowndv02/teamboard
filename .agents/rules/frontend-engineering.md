# Frontend engineering (Angular)

Lý do tồn tại: giữ UI nhất quán, đa ngôn ngữ được, lỗi xử lý theo mã (không theo chuỗi), và không có giá trị cấu hình/secret nằm rải rác trong component. Áp dụng khi sửa `src/web/**`.

Bổ sung cho: `angular.md` (cấu trúc, TypeScript, form, a11y, test), `angular-reactivity.md` (Signals/RxJS), `api-endpoints.md` (hình dạng API), `quality-gate.md`.

## 1. Chống hardcode: phân loại và nơi đặt

| Loại | Quy tắc | Nơi đặt (đề xuất) |
|---|---|---|
| **Chữ hiển thị** (label, title, toast, placeholder, thông báo lỗi) | Qua i18n bằng **khoá**; không chuỗi trong template/TS | `src/app/core/i18n/` (`message-keys.ts` + file dịch `vi.ts`/`en.ts`) |
| **Lỗi ứng dụng** | `AppError` với mã từ registry; không literal `code`/`message` | `src/app/core/errors/` |
| **Config** (URL API, timeout, feature flag) | Từ config runtime; không URL/số inline | `src/app/core/config/` |
| **Route** | Hằng số đường dẫn; không `'/boards/' + id` rải rác | `route.constants.ts` |
| **Quyền (RBAC)** | Hằng số permission/role; không chuỗi quyền inline | `permission.constants.ts` |
| **Khởi động / invariant / feature flag** | Message trong file hằng; không `throw new Error('...')` inline tại call site | `bootstrap.constants.ts`, `invariant-messages.ts` |
| **Magic number/string** | Từ lần lặp thứ 2 hoặc có ý nghĩa nghiệp vụ → hằng đặt tên | `shared/constants/` hoặc `constants` của feature |
| **Secret** | **Không bao giờ** có trong frontend (mọi thứ trong bundle là công khai) | – |

- Message invariant/dev bằng tiếng **Anh**; chữ cho người dùng cuối qua i18n.
- Chọn công cụ i18n (Angular `$localize` hay thư viện như ngx-translate) là quyết định **chưa chốt** (`TODO`, ADR khi chốt). Trước đó vẫn **gom chuỗi theo khoá** trong một nơi để chuyển được về sau; không rải chuỗi trong template.

## 2. Lỗi ứng dụng

- Backend trả `problem+json` có `code` ổn định (xem `api-endpoints.md` mục 5, `backend-engineering.md` mục 1). Frontend:
  - Interceptor HTTP chuyển lỗi sang `AppError` { `code`, `status`, `correlationId`, `traceId`, `fieldErrors` }.
  - Hiển thị thông báo bằng cách **tra khoá i18n theo `code`**; không rẽ nhánh theo `error.message`; `code` lạ → thông báo chung.
  - Hiển thị `correlationId` cho lỗi không lường trước để người dùng báo lại.
- `ErrorCode` phía frontend khớp registry backend (nguồn sự thật: contract trong `document/contracts/` hoặc `src/shared`); đổi mã ở backend thì cập nhật frontend và test cùng thay đổi.
- Mã lỗi **phía client** (mất mạng, hết hạn phiên, validate cục bộ) có enum riêng `ClientErrorCode`, không trộn với mã backend.
- Không `console.error`/`alert` làm xử lý lỗi; dùng service thông báo/log chung (có `correlationId`).
- Lỗi mạng/5xx: thông báo thân thiện, cho phép thử lại; không lộ stack trace hay chi tiết nội bộ.

## 3. Cấu hình môi trường

- **Một bản build chạy được ở mọi môi trường**: cấu hình lấy từ file runtime (`/assets/config.json`) nạp lúc khởi động (`provideAppInitializer`), không bake giá trị theo môi trường vào bundle. Đề xuất mặc định, cần xác nhận khi dựng `web` (`TODO` trong ADR 0005).
- Config runtime chỉ chứa giá trị **không bí mật** (URL gateway, cờ tính năng, tên môi trường). Có validate khi nạp; thiếu/sai → hiện màn hình lỗi cấu hình rõ ràng, không chạy nửa vời.
- URL API luôn tương đối `/api/v1` qua gateway khi cùng origin; chỉ cấu hình host khi khác origin.
- Cờ mock/dev (ví dụ đăng nhập giả) **chỉ** bật ở local; không tồn tại ở build stg/prod (kiểm trong gate/CI).
- `environment.ts` (build-time) chỉ dùng cho `production: boolean` và thứ thật sự cố định theo build.

## 4. Routing và phân quyền (RBAC)

- Route khai báo tập trung, lazy-load theo feature; đường dẫn từ `route.constants.ts`.
- Guard (`CanActivateFn`) kiểm tra xác thực và quyền theo permission hằng số; mặc định **đóng** (route mới phải khai báo quyền rõ ràng).
- Ẩn/hiện UI theo quyền chỉ là **trải nghiệm**, không phải bảo mật; server luôn kiểm tra lại (`api-endpoints.md` mục 7).
- Điều hướng sau đăng nhập/hết phiên đi qua một service duy nhất; không `window.location` rải rác.

## 5. Bảo mật phía client

- Không lưu secret trong bundle/config/`localStorage`. Chỗ lưu token (bộ nhớ, cookie HttpOnly, hay storage) là quyết định **chưa chốt** (`TODO` trong ADR 0005); trước khi chốt, không lưu token lâu dài ở nơi script đọc được mà chưa có ADR.
- Không `innerHTML`, `bypassSecurityTrust*` với dữ liệu người dùng; để Angular sanitize.
- Không đưa PII/token vào URL, query, log trình duyệt.
- Cấu hình CSP và header bảo mật ở gateway/hosting, không phụ thuộc client.
- Phụ thuộc npm: pin, `npm audit` (xem `dependency-pinning.md`, `quality-gate.md`).

## 6. Mock và dữ liệu dev

- Repository/service mock đặt cạnh bản thật với hậu tố rõ (`*.repository.mock.ts`); chọn bản qua DI/config, không `if (isMock)` rải rác.
- Message trong mock/fixture lấy từ file hằng (`mock-fixture-messages.ts`), không literal lặp.
- Fixture không chứa dữ liệu thật hay secret.
- Mock chỉ dùng cho dev/test; build production không bundle mock (kiểm tra bằng kích thước/`grep` trong gate khi có).

## 7. i18n và a11y (nhắc lại ràng buộc)

- Mọi chữ hiển thị qua khoá; định dạng ngày/số theo locale (Intl/pipe), dữ liệu API là UTC ISO 8601.
- Nhãn truy cập (`aria-label`, `alt`) cũng là chữ hiển thị → qua i18n.
- Chi tiết a11y: `angular.md`.

## 8. Test chính sách (quality gate cho quy tắc này) 🚧

Thêm khi dựng `web`, chạy trong gate:

- `no-hardcode-policy.spec.ts`: quét `src/app/**` (trừ constants, i18n, spec) tìm chuỗi tiếng Việt/Anh hiển thị inline, URL tuyệt đối, `throw new Error('...')` inline.
- `error-message-policy.spec.ts`: mọi `ErrorCode` có khoá i18n; không `code`/`message` literal ngoài registry.
- Lint rule cấm `eslint-disable` các rule bắt buộc (xem `angular-reactivity.md` mục 5).

## 9. Checklist trước khi báo xong thay đổi frontend

- [ ] Không chuỗi hiển thị inline; chữ qua khoá i18n (kể cả `aria-label`)
- [ ] Lỗi qua `AppError` + tra khoá theo `code`; không literal `code`/`message`
- [ ] Không URL/permission/route/timeout inline; dùng hằng và config runtime
- [ ] Không secret/PII trong bundle, URL, log; không `innerHTML`/`bypassSecurityTrust*` với dữ liệu người dùng
- [ ] Route mới có guard và quyền; server vẫn kiểm tra quyền
- [ ] Mock không lọt vào build production
- [ ] Gate đã chạy (`code-change-gate.md`, `quality-gate.md`); UI đã xem trên trình duyệt
