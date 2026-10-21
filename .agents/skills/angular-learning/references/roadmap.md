# Lộ trình học Angular (gắn với identity-auth và TeamBoard)

Mỗi giai đoạn là một lát giao được (issue riêng qua `workflow`, làm bằng `angular-feature`, review bằng `frontend-review`). Phiên bản Angular/Node pin ở `config/tool-versions.yaml` (chốt khi scaffold; kiểm tại angular.dev). Mức tin cậy nội dung dưới đây: `Vừa` (khung học kinh điển; kiểm chi tiết API theo phiên bản đang dùng).

Điều kiện chung: ADR 0009 `accepted`, `src/web` đã scaffold, kiểu API sinh từ OpenAPI. Mỗi giai đoạn "xong" khi gate FE xanh (lint, format, typecheck, test + policy test, build đạt budgets, `openapi:check`, `npm audit`) và đã xem trên trình duyệt.

| GĐ | Chủ đề | Lát sản phẩm (identity/TeamBoard) | Bài tập | Lỗi hay gặp | Tiêu chí xong |
|---|---|---|---|---|---|
| **A1** | TypeScript strict, standalone component, template control flow, app shell, routing cơ bản, config runtime, i18n theo khoá | Khung app: layout, trang chủ, trang 404, nạp `config.json` lúc khởi động | Dựng app shell; thêm một trang qua route lazy | Dùng `any`; `*ngIf` cũ; hardcode chuỗi/URL; quên `track` | Lint/typecheck sạch; chữ qua khoá i18n; config runtime validate; `i18n-keys-complete` xanh |
| **A2** | Forms có kiểu, validation, HTTP client + interceptor, lỗi `problem+json`, `AppError` | Đăng ký (FR-001), đổi mật khẩu (FR-022): form + gọi API qua gateway | Form đăng ký có validate mật khẩu theo BR-002, hiển thị lỗi theo `code` | Validate chỉ ở client; rẽ nhánh theo `message`; không xử lý 409/429 | Component test hành vi; `error-code-policy` xanh; lỗi 409/400/429 hiển thị đúng khoá i18n |
| **A3** | Signals (`signal/computed/linkedSignal/effect`), `resource`/`httpResource`, state theo feature, change detection OnPush/zoneless | Danh sách phiên/thông tin tài khoản; trạng thái tải/lỗi/rỗng | Chuyển một màn hình từ RxJS thủ công sang signals + resource | `effect` đồng bộ state/gọi API; đột biến mảng trong signal; subscribe rò rỉ | Không `eslint-disable`; trạng thái tải/lỗi/rỗng có test; không rò rỉ subscription |
| **A4** | **OIDC Authorization Code + PKCE phía client**, guard/interceptor, refresh phiên, đăng xuất, lưu token theo ADR | Đăng nhập (FR-002, FR-012/013), guard mặc định đóng, interceptor gắn token, đổi refresh (FR-009) | Hoàn chỉnh luồng login qua `identity-service`: `state`/`nonce`/`code_verifier`, xử lý callback, 401 → refresh → thử lại một lần | Lưu token ở nơi script đọc được khi ADR chưa cho; `redirect_uri` lỏng; thiếu `state`; vòng lặp refresh; gắn token cho origin lạ | Test tấn công phía client (thiếu/sai `state`, replay `code`, token hết hạn); không token trong URL/log; theo đúng ADR nơi lưu token; e2e đăng nhập pass |
| **A5** | Chiến lược test FE: Vitest, component test theo hành vi, Playwright e2e + BDD, a11y (axe), test không flaky | Test cho toàn bộ luồng A1–A4; kịch bản BDD đăng nhập/đăng ký | Viết kịch bản BDD "đăng nhập sai 5 lần bị khoá" ở e2e với backend giả bám contract | Test bám class CSS; `sleep`; mock không bám schema; e2e quá nhiều | Pyramid hợp lý; axe 0 serious/critical; không test flaky (chạy lặp ổn định); coverage không giảm |
| **A6** | Hiệu năng và triển khai: bundle budgets, lazy-load, `@defer`, `NgOptimizedImage`, Web Vitals, build production, deploy S3 + CloudFront (cấu hình qua Terraform, ADR/skill `aws-service-selection`) | Đưa app lên môi trường dev; cache/headers/CSP | Đo bundle, giảm initial; thêm CSP và header bảo mật ở hosting | Bundle phình do thư viện; không cache-bust; CSP lỏng; bake config theo môi trường | Budgets đạt; Lighthouse CI đạt ngưỡng (khi có môi trường); một bản build chạy mọi môi trường |
| **A7** (tuỳ chọn) | SSR/hydration, micro-frontend, PWA, Signal Forms nâng cao | Chỉ làm khi có nhu cầu và ADR | Đánh giá SSR cho trang công khai | Làm vì "hay" mà không có yêu cầu | Có ADR đánh đổi trước khi làm; không phá gate |

## Điểm bảo mật phía client cần luyện (mỗi điểm một test)
Đối ứng `identity-learning.md` mục 3: chặn mã uỷ quyền (PKCE bắt buộc), chuyển hướng độc hại (`redirect_uri` cố định), CSRF/replay (`state`/`nonce`), lộ token (không `localStorage` khi ADR chưa cho, không URL/log), XSS (không `innerHTML`/`bypassSecurityTrust*`), phụ thuộc có lỗ hổng (`npm audit`).

## Cách theo dõi
Sau mỗi giai đoạn: `audit` đối chiếu tiêu chí xong; ghi `document/pm/pm-journal.md` (khái niệm, ví dụ, điều còn mơ hồ, bài tiếp). Lỗi hệ thống lặp lại → `lesson-learned`.

## Nguồn đọc thêm
angular.dev (hướng dẫn chính thức; kiểm theo phiên bản), RFC 6749 và 7636, OAuth 2.0 Security BCP và OAuth 2.0 for Browser-Based Apps (đọc bản hiện hành, kiểm số RFC/trạng thái), OWASP ASVS/Cheat Sheets, WCAG 2.2, web.dev (Web Vitals). Chưa xác minh bằng tìm kiếm trong phiên này; mức `Cần xác minh` trước khi trích.
