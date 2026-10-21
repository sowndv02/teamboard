# Frontend testing (Angular)

Lý do tồn tại: test frontend dễ trở thành chậm, giòn (flaky) và bám chi tiết cài đặt; cần một chuẩn chung để test bảo vệ hành vi người dùng thấy, chạy nhanh và ổn định, khớp pyramid của dự án. Áp dụng khi viết/sửa test trong `src/web/**` hoặc kịch bản UI.

Bổ sung cho `testing.md` (loại test chung), `test-review.md` (TDD/BDD, `.feature`), `angular.md`, `frontend-engineering.md`, `quality-gate.md` (phạm vi targeted/full). Công cụ cụ thể (runner, thư viện, BDD web) ghi ở `config/tool-versions.yaml` (`TODO` tới khi scaffold; đề xuất Vitest + Testing Library + Playwright, chốt khi dựng).

## 1. Kim tự tháp

| Tầng | Số lượng | Công cụ (đề xuất) | Kiểm gì |
|---|---|---|---|
| Unit | Nhiều | Vitest (`ng test`) | Logic thuần: service, pipe, `computed`, mapper, validator, guard, interceptor |
| Component | Vừa | Vitest + Angular Testing Library | Hành vi người dùng thấy của một component/page: render, tương tác, trạng thái tải/lỗi/rỗng |
| Contract | Vừa | Kiểu OpenAPI + mock HTTP bám schema | Request/response/lỗi khớp contract backend (`frontend-engineering.md` mục 8) |
| A11y | Kèm component/e2e | axe | 0 vi phạm serious/critical |
| E2E | Rất ít | Playwright | Luồng sống còn qua trình duyệt thật (đăng nhập, luồng chính), smoke |

Không dựng e2e cho thứ unit/component kiểm được. Mỗi bug sửa có test tái hiện ở tầng thấp nhất kiểm được nó.

## 2. Quy ước viết test

- Test **hành vi**, không chi tiết cài đặt: không kiểm tên method private, thứ tự gọi nội bộ, tên class CSS, cấu trúc DOM sâu.
- Selector bền: theo **role/label/text qua khoá i18n** (`getByRole`, `getByLabelText`); `data-testid` là phương án cuối. Không bám class CSS hay vị trí.
- Mỗi test độc lập, không phụ thuộc thứ tự; mỗi test dựng và dọn state riêng (không state toàn cục dùng chung).
- Tên test nêu hành vi: `should <kết quả> when <điều kiện>`; cấu trúc Arrange-Act-Assert.
- Test bất đồng bộ chờ theo **điều kiện** (`findBy*`, `waitFor`, `fixture.whenStable`, auto-wait của Playwright); **cấm `sleep`/`setTimeout`/`waitForTimeout` cố định**.
- Thời gian, ngẫu nhiên, ID dùng đồng hồ/seed giả (`vi.useFakeTimers`, fake clock) để test xác định.
- Zoneless: kích hoạt thay đổi qua signal/sự kiện của người dùng; không dựa vào "tự động phát hiện thay đổi" (`angular.md` mục 1). Không `fixture.detectChanges()` bừa để che lỗi cập nhật; cần thì ghi lý do.
- Mỗi lỗi/ngoại lệ test **đường lỗi** (4xx/5xx `problem+json`, mất mạng, hết phiên), không chỉ happy path.

## 3. Test double và HTTP

- Mock HTTP bằng `provideHttpClientTesting` hoặc một lớp mock mạng dùng chung (ví dụ MSW) trả dữ liệu **đúng kiểu sinh từ OpenAPI**; không gọi mạng thật. Fixture dựng bằng builder/factory dùng chung, không JSON chép tay lệch contract.
- Chỉ mock **ranh giới** (HTTP, router khi cần, storage, thời gian); không mock service nội bộ của chính feature đang test (trừ unit cô lập).
- Fixture không chứa dữ liệu thật hay secret; message trong fixture lấy từ file hằng (`frontend-engineering.md` mục 6).
- Không test thư viện bên thứ ba hay Angular; test cách ta dùng chúng.

## 4. Accessibility tự động

- Component/page quan trọng có kiểm axe trong test (component hoặc e2e): **0 vi phạm serious/critical** (BLOCKER); moderate/minor ghi finding.
- Test bàn phím cho luồng chính (Tab, Enter, Esc, quản lý focus ở dialog). axe không thay kiểm tra thủ công bằng bàn phím/trình đọc màn hình khi thay đổi lớn về UI.
- Lint template a11y (`angular.md` mục 7) là lớp trước; không thay thế axe.

## 5. E2E và BDD cho luồng người dùng thấy

- E2E bằng Playwright chạy trên build thật với backend/mạng giả hoặc môi trường local (`docker compose`); smoke mặc định: đăng nhập thành công, đăng nhập sai (lỗi chung), luồng chính của feature, hết phiên.
- Kịch bản BDD (`.feature`) chỉ cho **hành vi người dùng thấy**; viết theo `test-review.md`. Runner BDD web (`bdd_runner_web` trong `config/tool-versions.yaml`) là `TODO`: đề xuất Playwright + `playwright-bdd` hoặc Cucumber.js; chốt khi scaffold. Không bắt mọi test là `.feature`.
- E2E ổn định: dữ liệu tạo/dọn riêng mỗi test, không dùng chung tài khoản gây xung đột khi chạy song song, trace/video chỉ giữ khi fail, retry tối đa 1 lần ở CI **và** mọi test phải xanh ở lần chạy không retry trong gate cục bộ. Test flaky bị quarantine kèm issue, không để đỏ lâu.
- Test bảo mật client: không có token/PII trong `localStorage`, URL, log; mock không lọt build production (`no-mock-in-prod-bundle`).

## 6. Hồi quy trực quan và hiệu năng (khi cần)

- Snapshot DOM lớn **cấm** (giòn, ít giá trị); snapshot nhỏ có chủ đích thì được. Hồi quy trực quan (screenshot) chỉ cho thành phần UI dùng chung ổn định, cập nhật có review.
- Lighthouse CI/Web Vitals đo trên build production khi có môi trường chạy (🚧, `quality-gate.md`); budget bundle là test build, không phải test runtime.

## 7. Phạm vi chạy (targeted/full)

- Đang code: chỉ test liên quan (`npm --prefix src/web run test -- <đường-dẫn-spec>`), theo `quality-gate.md` mục "Phạm vi test". `e2e` không chạy mỗi lát; chạy ở full gate/PR hoặc khi đổi luồng sống còn.
- Full web (trước báo xong issue, mở PR, merge): `lint`, `format:check`, `typecheck`, `test`, `build`, `openapi:check`, policy tests, axe, `e2e` smoke.
- Đổi `eslint.config.*`, `tsconfig*.json`, `angular.json`, `package.json`/lockfile, harness test chung → chạy full web ngay.
- Coverage: không ngưỡng cứng toàn dự án; logic quan trọng (auth, interceptor, guard, mapper lỗi) bắt buộc có test; PR không **giảm** coverage vùng đã có.

## 8. Cấm

- Skip/xoá/nới test, `it.skip`/`fdescribe`/`only` còn sót, tắt axe hay lint để qua gate.
- `sleep`/chờ cố định; test phụ thuộc thứ tự hoặc dữ liệu từ test khác; gọi mạng ngoài.
- Test bám class CSS, cấu trúc DOM sâu hay chi tiết cài đặt; snapshot DOM lớn.
- Fixture/dữ liệu thật hoặc secret trong test.

## Checklist

- [ ] Mỗi hành vi mới có test đỏ trước, đúng tầng (unit/component/e2e); bug có test tái hiện
- [ ] Selector theo role/label; không `sleep`; không phụ thuộc thứ tự
- [ ] HTTP mock đúng kiểu từ OpenAPI; có test đường lỗi
- [ ] axe: 0 vi phạm serious/critical; luồng chính dùng được bằng bàn phím
- [ ] E2E chỉ luồng sống còn; không test flaky còn mở
- [ ] Policy tests (`no-hardcode-policy`, `error-code-policy`, `i18n-keys-complete`, `no-mock-in-prod-bundle`) xanh
- [ ] Coverage vùng đã có không giảm; không `.only`/`.skip` sót
