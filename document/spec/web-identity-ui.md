---
title: Giao diện web cho xác thực (Angular, phần identity)
status: draft            # draft | confirmed | superseded
owner: sondv
updated: 2026-10-21
services: [web, identity-service]
issues: [TB-004]
---

# Giao diện web cho xác thực (Angular, phần identity)

## 1. Yêu cầu gốc

> Tạo FE angular (trước đó đã gỡ giờ bổ sung)
>
> angular real-world project (học thêm angular)
>
> Lần sau spec sẽ phải khai phá cho cả FE luôn. Hiện tại cần bổ sung spec FE

Nguồn: sondv, phiên Claude Code ngày 2026-10-21 (system-design và spec-review identity-auth); xem `document/spec/identity-auth.md` Confirm log #13, #14, #16 và ADR 0009.

Làm rõ qua hai lô câu hỏi cùng ngày (xem mục 19).

## 2. Bối cảnh và mục tiêu

- Vấn đề cần giải: chủ dự án muốn học Angular bằng một dự án gần thực tế, và `identity-service` (Authorization Code + PKCE, OIDC) cần một client thật để học trọn luồng đăng nhập. ADR 0009 làm lại FE theo từng giai đoạn; spec này là giai đoạn đầu: **chỉ phần identity**, chưa có màn hình board/task.
- Giá trị/kết quả mong muốn: người dùng đăng ký, đăng nhập, giữ phiên, đổi mật khẩu, xem hồ sơ tối thiểu và đăng xuất trên trình duyệt; đồng thời là khung ứng dụng (app shell, routing, guard, i18n, lỗi, test, gate) để các tính năng board/task thêm vào sau.
- Thước đo thành công:
  - **Học:** đi hết lộ trình A1–A7 của skill `angular-learning` cho phạm vi này; chủ dự án tự giải thích được luồng PKCE ở phía client.
  - Mọi kịch bản FR-001…FR-008 (đường chính, lỗi) chạy xanh trong test; các gate frontend (`quality-gate.md`) xanh.
  - Không phải thước đo thành công: có người dùng thật, hay đẹp mắt (mục tiêu là học, không phải sản phẩm).

## 3. Người dùng và vai trò

| Vai trò | Mô tả | Mục đích sử dụng |
|---|---|---|
| `user` | Người dùng cuối của TeamBoard, định danh bằng email (vai trò duy nhất, theo spec identity) | Đăng ký, đăng nhập, đổi mật khẩu, xem hồ sơ, đăng xuất |
| Chủ dự án (người học) | Người dùng dự án này để học Angular | Dùng FE như bài tập, kiểm chứng bằng test và gate |

Không có vai trò `admin`.

## 4. Phạm vi

**Trong phạm vi (lần này):**
- Khung ứng dụng Angular: app shell, điều hướng, route guard, trang lỗi (403/404/lỗi chung), xử lý lỗi thống nhất, i18n tiếng Việt + tiếng Anh, responsive.
- Màn hình: đăng ký; đăng nhập (trang phục vụ trên origin của `identity-service`, FR-019 của spec identity); đổi mật khẩu; hồ sơ tối thiểu (xem email) và đăng xuất.
- Luồng client: Authorization Code + PKCE, giữ phiên, làm mới token im lặng, hết phiên.
- Chất lượng: test (đơn vị, component, e2e smoke), a11y WCAG 2.2 AA, gate FE, kiểu API sinh từ OpenAPI.
- Chạy local (`ng serve`/docker compose).

**Ngoài phạm vi:**
- Màn hình board/task, hoạt động, thông báo (spec riêng khi backend và gateway sẵn sàng; I9 của identity).
- Quên/đặt lại mật khẩu, xác minh email, MFA, đăng nhập bên thứ ba (ngoài phạm vi identity).
- Quản trị người dùng/client, consent screen.
- SSR/SEO (mặc định SPA; cần ADR nếu đổi), PWA/offline, thiết bị ngoài trình duyệt hiện đại.
- Deploy AWS (S3 + CloudFront): issue riêng khi học Terraform, không nằm trong lần này.
- Chọn thư viện UI, nơi lưu token, phiên bản Angular/Node: quyết định ADR/design (mục 15, 16), không chốt trong spec.

## 5. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| SPA | Ứng dụng Angular chạy trên trình duyệt, là client công khai |
| Trang đăng nhập | Trang email/mật khẩu do `identity-service` phục vụ trên origin của nó; SPA không nhận mật khẩu |
| PKCE | Cơ chế chống chặn mã uỷ quyền (RFC 7636) |
| `returnUrl` | Đường dẫn nội bộ của SPA để quay lại sau khi đăng nhập |
| `AppError` | Lỗi ứng dụng chuẩn hoá phía FE từ `problem+json` (`code`, `status`, `correlationId`) |

## 6. Yêu cầu chức năng

Ưu tiên: Must / Should / Could / Won't. Phần Must (6/10) tạo thành vòng tối thiểu "đăng ký → đăng nhập → giữ phiên → đăng xuất → bảo vệ route"; thiếu một mục thì vòng không đóng, nên đều Must.

| ID | Mô tả (user story) | Ưu tiên | Tiêu chí chấp nhận (Given/When/Then) | Nguồn |
|---|---|---|---|---|
| FR-001 | Là người dùng mới, tôi muốn đăng ký bằng email và mật khẩu để có tài khoản | Must | Given email hợp lệ chưa dùng và mật khẩu ≥ 12 ký tự, When gửi form, Then thấy thông báo thành công và được đưa tới đăng nhập, mật khẩu không hiện trong URL/log. Given email đã tồn tại, Then hiện lỗi "email đã được dùng" (409 từ API, tuân BR-003 của identity); Given mật khẩu 6 ký tự, Then lỗi cạnh trường mật khẩu (kiểm cả phía client và theo 400 từ API); Given email sai định dạng, Then lỗi cạnh trường email, không gọi API | Lời người dùng (đăng ký trong phạm vi) |
| FR-002 | Là người dùng, tôi muốn bấm "Đăng nhập" và đăng nhập an toàn để dùng ứng dụng mà SPA không giữ mật khẩu | Must | Given chưa đăng nhập, When bấm Đăng nhập, Then SPA chuyển tới `authorize` của identity kèm `state`, `code_challenge` (PKCE), `redirect_uri` đã đăng ký. Given quay về với `code` và `state` khớp, Then SPA đổi mã lấy token và vào ứng dụng ở trang `returnUrl`. Given `state` không khớp hoặc thiếu, Then bỏ qua `code`, báo lỗi đăng nhập và quay về đăng nhập | Lời người dùng / `identity-learning.md` |
| FR-003 | Là người dùng, tôi muốn trang đăng nhập rõ ràng để nhập email/mật khẩu mà không lộ thông tin cho kẻ tấn công | Must | Given nhập đúng, Then chuyển về SPA kèm mã (FR-019 identity). Given sai email hoặc sai mật khẩu hoặc đang bị khoá tạm, Then **cùng một** thông báo lỗi chung, không nói email có tồn tại hay không; Given 429 hoặc khoá có `Retry-After: 900`, Then hiện "thử lại sau khoảng 15 phút" (quy đổi từ giây) và vô hiệu nút gửi trong thời gian đó. Trang dùng được bằng bàn phím và đọc được bằng trình đọc màn hình | Lời người dùng (identity FR-019, BR-003) |
| FR-004 | Là người dùng đã đăng nhập, tôi muốn phiên được giữ mà không bị hỏi lại liên tục để làm việc liền mạch | Must | Given access token sắp hết hạn (10 phút, BR-004 identity), When còn refresh token hợp lệ, Then SPA làm mới im lặng, người dùng không thấy gián đoạn. Given refresh hết hạn hoặc bị thu hồi (7 ngày hoặc dùng lại), Then đưa về đăng nhập, lưu `returnUrl` nội bộ; `returnUrl` ngoài origin SPA bị bỏ qua (chống open redirect). Given hai tab cùng làm mới đồng thời, Then không gây thu hồi chuỗi sai | Lời người dùng (phiên) |
| FR-005 | Là người dùng đã đăng nhập, tôi muốn đổi mật khẩu để giữ tài khoản an toàn | Should | Given mật khẩu hiện tại đúng và mật khẩu mới ≥ 12 ký tự, When gửi, Then thông báo thành công và các phiên khác bị đăng xuất (FR-022 identity thu hồi mọi refresh). Given mật khẩu hiện tại sai, Then lỗi chung 401 cạnh trường, không tiết lộ chi tiết; sai nhiều lần có thể bị khoá tạm (FR-003 identity) và hiện thời gian chờ | Lời người dùng |
| FR-006 | Là người dùng đã đăng nhập, tôi muốn xem hồ sơ tối thiểu để biết mình đang đăng nhập bằng tài khoản nào | Should | Given đã đăng nhập, When mở hồ sơ, Then thấy email của mình. Given token hết hạn khi tải hồ sơ, Then làm mới rồi thử lại một lần (FR-004), không hiện lỗi nếu thành công. Phụ thuộc nguồn dữ liệu (Q-02) | Lời người dùng |
| FR-007 | Là người dùng đã đăng nhập, tôi muốn đăng xuất để người khác dùng chung máy không vào được tài khoản | Must | Given đã đăng nhập, When bấm Đăng xuất, Then token bị xoá khỏi bộ nhớ ứng dụng, phiên ở identity bị huỷ (FR-011 identity), và SPA về trang chưa đăng nhập. Given đăng xuất xong rồi bấm Đăng nhập, Then phải nhập lại mật khẩu (không tự vào). Given mạng lỗi khi gọi đăng xuất, Then vẫn xoá token cục bộ và báo "chưa chắc đã huỷ phiên trên máy chủ" | Giả định (đổi theo Confirm log #15 identity) |
| FR-008 | Là người dùng, tôi muốn ứng dụng chỉ cho vào trang cần đăng nhập khi đã đăng nhập để dữ liệu không lộ | Must | Given chưa đăng nhập, When mở route cần đăng nhập, Then chuyển tới đăng nhập kèm `returnUrl`. Given route không tồn tại, Then trang 404; Given không đủ quyền (403 từ API), Then trang 403; route mới mặc định cần đăng nhập (guard mặc định đóng) | Giả định |
| FR-009 | Là người dùng, tôi muốn chọn tiếng Việt hoặc tiếng Anh để dùng ngôn ngữ mình quen | Should | Given lần đầu, Then ngôn ngữ theo trình duyệt (vi hoặc en, mặc định vi); When đổi, Then mọi chuỗi hiển thị (kể cả nhãn truy cập và thông báo lỗi) đổi ngay, lựa chọn được nhớ cho lần sau; ngày/số theo locale. Giá trị ngôn ngữ không nhạy cảm nên được lưu phía trình duyệt | Lời người dùng (vi + en) |
| FR-010 | Là người dùng, tôi muốn lỗi được báo dễ hiểu để biết phải làm gì | Should | Given API trả `problem+json` có `code`, Then thông báo tra theo `code` (không theo `message`), `code` lạ → thông báo chung. Given lỗi không lường trước (5xx), Then hiện thông báo thân thiện kèm `correlationId` để báo lại, cho thử lại. Given mất mạng, Then thông báo riêng và cho thử lại; không lộ stack trace hay chi tiết nội bộ | `frontend-engineering.md` |

## 7. Quy tắc nghiệp vụ

| ID | Quy tắc | Ví dụ | Nguồn |
|---|---|---|---|
| BR-001 | SPA không bao giờ nhận, giữ, hay gửi mật khẩu đăng nhập; mật khẩu đăng nhập chỉ nhập trên trang của identity. Đăng ký và đổi mật khẩu gửi mật khẩu qua API đúng hợp đồng | Mã SPA không có ô mật khẩu cho đăng nhập | Confirm log #14 identity |
| BR-002 | Mật khẩu tối thiểu 12 ký tự, giới hạn tối đa theo identity (BR-002 identity); FE kiểm sớm nhưng server là nguồn sự thật | `short1` bị chặn ở client; client bỏ qua thì server vẫn trả 400 | identity BR-002 |
| BR-003 | Thông báo đăng nhập thất bại luôn chung, không nói email có tồn tại hay không (ngoại lệ: đăng ký trùng email theo identity BR-003) | Sai email và sai mật khẩu cùng một câu | identity BR-003 |
| BR-004 | Token truy cập, refresh và mật khẩu không xuất hiện trong URL, log trình duyệt, hay bộ nhớ lâu dài đọc được bằng script; nơi giữ token chốt ở ADR (Q-01) | URL callback chỉ chứa `code`/`state`, xoá khỏi lịch sử sau khi đổi mã | ADR 0006, `frontend-engineering.md` |
| BR-005 | `returnUrl` chỉ nhận đường dẫn nội bộ của SPA | `https://evil.example` bị bỏ qua, về trang chủ | OWASP (open redirect) |
| BR-006 | Mọi chuỗi hiển thị (kể cả nhãn truy cập) qua khoá i18n; lỗi tra theo `code` | Không có chuỗi tiếng Việt/Anh cứng trong template | `frontend-engineering.md` |

## 8. Luồng nghiệp vụ

### 8.1 Luồng chính

Đăng nhập lần đầu:
1. Người dùng mở SPA, bấm Đăng nhập (FR-002).
2. SPA sinh `code_verifier`/`code_challenge`/`state`, chuyển tới `authorize` của identity.
3. Người dùng nhập email/mật khẩu trên trang đăng nhập của identity (FR-003).
4. Identity chuyển về SPA với `code` và `state`; SPA kiểm `state`, đổi mã lấy token, xoá `code` khỏi URL, vào trang `returnUrl`.
5. SPA làm mới token im lặng trước khi hết hạn (FR-004); người dùng đăng xuất khi muốn (FR-007).

Đăng ký: mở trang đăng ký, gửi form, thành công thì sang đăng nhập (FR-001).

### 8.2 Luồng thay thế

- Chưa đăng nhập mở trang bảo vệ: chuyển tới đăng nhập, sau đó quay lại đúng trang (FR-008, FR-004).
- Đổi ngôn ngữ giữa chừng: giữ nguyên trạng thái trang (FR-009).
- Đổi mật khẩu thành công: các phiên khác phải đăng nhập lại (FR-005).

### 8.3 Luồng lỗi và trường hợp biên

- Callback thiếu/sai `state`, hoặc có `error` từ identity: báo lỗi đăng nhập, không đổi mã, quay về đăng nhập.
- Đổi mã thất bại (mã hết hạn, dùng lại): báo lỗi, cho đăng nhập lại.
- Hai tab: tab này đăng xuất thì tab kia cũng thoát khi gọi API kế tiếp (phát hiện qua 401), không để trạng thái "đã đăng nhập" giả.
- Làm mới đồng thời nhiều request: gộp thành một lần làm mới, các request chờ rồi dùng token mới.
- Refresh token bị dùng lại (bị thu hồi chuỗi): đưa về đăng nhập, thông báo phiên không còn hợp lệ.
- Mất mạng giữa chừng, 429, 5xx: thông báo riêng, cho thử lại (FR-010).
- Chặn màn hình ở 360px: không tràn ngang, nút bấm đủ lớn.

## 9. Dữ liệu

FE không có cơ sở dữ liệu riêng; không phát sinh PII lưu bền phía client. Dữ liệu phía client:

| Dữ liệu | Nơi giữ | Ràng buộc |
|---|---|---|
| Access/refresh token | Theo ADR (Q-01): bộ nhớ SPA, cookie `HttpOnly`, hoặc BFF | Không lưu ở `localStorage`/`sessionStorage` (BR-004) |
| `code_verifier`, `state`, `returnUrl` | Tạm trong một lần đăng nhập | Xoá sau khi dùng; `returnUrl` chỉ nội bộ |
| Ngôn ngữ | Lưu phía trình duyệt | Không nhạy cảm |
| Email hiển thị | Bộ nhớ khi đang đăng nhập | Không lưu bền |

## 10. Phân quyền và bảo mật

| Hành động | Ai được phép | Ghi chú |
|---|---|---|
| Đăng ký, đăng nhập | Công khai | Giới hạn tốc độ/khoá tạm do identity (NFR-008, FR-003 identity) |
| Đổi mật khẩu, hồ sơ, đăng xuất | Người đã đăng nhập, chỉ cho chính mình | Dùng access token; không nhận `userId` từ người dùng |

- Chống XSS: không `innerHTML`/`bypassSecurityTrust*` với dữ liệu người dùng; CSP ở hosting/gateway. Chống CSRF/clickjacking cho trang đăng nhập do identity đảm nhiệm (NFR-010 identity).
- Không lưu bí mật trong bundle; cấu hình runtime chỉ chứa giá trị công khai.
- Phụ thuộc npm: không lỗ hổng High/Critical, giấy phép theo `dependency-policy.md`.
- Lưu ý: tự xây để học, **không dùng cho người dùng/dữ liệu thật** (ADR 0006).

## 11. Tích hợp và sự kiện

| Với | Cách giao tiếp | Dữ liệu |
|---|---|---|
| `identity-service` | REST + redirect (authorize, token, đăng ký, đổi mật khẩu, đăng xuất, userinfo nếu dùng) | Mã uỷ quyền, token, lỗi `problem+json` |
| `gateway` | Mọi gọi API đi qua `/api/v1/...` (khi gateway có; trước đó trỏ trực tiếp identity ở local, cấu hình runtime) | – |
| Kiểu API | Sinh từ OpenAPI của backend (`openapi:check`) | Không viết tay kiểu response |

**Phụ thuộc vào spec identity (cần theo dõi):**
- FR-019 (trang đăng nhập), NFR-010 (CSRF/clickjacking), FR-011 (đăng xuất huỷ cookie): đã có.
- FR-017 `userinfo` đang `Could` (I7) nhưng FR-006 cần email: xem Q-02.
- Hợp đồng claim/`aud`: design identity (`document/design/identity-auth.md` D2).

## 12. Yêu cầu phi chức năng

| ID | Nội dung (kèm số đo) |
|---|---|
| NFR-001 | Truy cập: WCAG 2.2 AA; axe tự động 0 vi phạm serious/critical trên các màn hình; dùng được hoàn toàn bằng bàn phím; thử trình đọc màn hình ở luồng đăng nhập |
| NFR-002 | Responsive: dùng được từ 360px; trình duyệt hiện đại (2 bản chính mới nhất của Chrome, Edge, Firefox, Safari) |
| NFR-003 | Hiệu năng (giả định, A-05): bundle khởi tạo (initial) ≤ 250 KB gzip, `budgets` bắt buộc trong build; LCP ≤ 2,5 giây (p75) trên build production chạy local với throttling mặc định của Lighthouse |
| NFR-004 | i18n: 100% chuỗi qua khoá, đủ khoá vi và en (kiểm bằng policy test `i18n-keys-complete`); định dạng ngày/số theo locale |
| NFR-005 | Lỗi theo `code`; mọi lỗi 5xx hiển thị `correlationId`; không lộ stack trace |
| NFR-006 | Quan sát: gắn `traceparent` và `X-Correlation-Id` vào mọi request; lỗi JS không bắt được gửi về điểm thu thập (chốt ở design) |
| NFR-007 | Chất lượng: toàn bộ gate frontend trong `quality-gate.md` xanh (lint 0 warning, typecheck, build có budgets, test, policy test, `npm audit` mức High, `openapi:check`); coverage vùng đã có không giảm |
| NFR-008 | Test: mỗi FR Must/Should có ít nhất một ca chính và một ca lỗi; e2e smoke cho đăng nhập → vào ứng dụng → đăng xuất |
| NFR-009 | Không SLA; chỉ chạy local giai đoạn này |

## 13. Giao diện

Có (Angular, ADR 0009). Yêu cầu chung (chi tiết thiết kế hình ảnh ngoài phạm vi spec):
- Mỗi màn hình có đủ trạng thái: tải, lỗi, rỗng/thành công; thông báo lỗi tra theo `code`.
- Thiết kế đồng nhất (khoảng cách, màu, kiểu chữ) dùng token thiết kế; đủ tương phản (≥ 4,5:1 cho chữ thường).
- Form: nhãn gắn với ô nhập, lỗi gắn `aria-describedby`, focus chuyển tới lỗi đầu tiên khi gửi lỗi, nút gửi vô hiệu khi đang gửi (chống bấm đúp).
- Không dùng thư viện UI cụ thể trong spec (Q-04).

Màn hình: đăng ký, đăng nhập (trên origin identity), đổi mật khẩu, hồ sơ + đăng xuất (menu), app shell, 403/404/lỗi chung.

## 14. Giả định

| # | Giả định | Người xác nhận | Trạng thái |
|---|---|---|---|
| A-01 | Hợp đồng API và luồng OAuth theo spec identity-auth (FR-001…FR-022, A-03: access 10 phút, refresh 7 ngày, phiên identity 30 phút) | sondv | Đã xác nhận (theo spec identity confirmed) |
| A-02 | Vai trò duy nhất `user`; không phân quyền theo màn hình ngoài đăng nhập | sondv | Đã xác nhận (lô 1) |
| A-03 | Chạy local trước, deploy AWS là issue riêng | sondv | Đã xác nhận (lô 2) |
| A-04 | Ngôn ngữ mặc định tiếng Việt; có tiếng Anh | sondv | Đã xác nhận (lô 1) |
| A-05 | Ngưỡng NFR-003 (250 KB gzip initial, LCP 2,5 giây) là giả định ban đầu của người học, hiệu chỉnh khi có số đo thật | sondv | Chưa xác nhận |
| A-06 | Không dùng cho người dùng/dữ liệu thật; compliance `none` | sondv | Đã xác nhận (theo identity A-05, A-09) |

## 15. Câu hỏi mở

Mỗi câu có owner, hạn (trước bước cần), mặc định nếu quá hạn, trạng thái.

| # | Câu hỏi | Ảnh hưởng | Owner | Hạn | Mặc định | Trạng thái |
|---|---|---|---|---|---|---|
| Q-01 | Nơi giữ token phía client: bộ nhớ SPA + refresh qua cookie `HttpOnly`, hay BFF qua gateway? | FR-002, FR-004, BR-004, I5 | sondv | Trước design FE (blocker cho FR-002/004) | Chưa có mặc định an toàn: không tự chọn; không lưu bằng `localStorage` | open (cần ADR) |
| Q-02 | Hồ sơ lấy email từ đâu: `userinfo` (identity FR-017, đang Could) hay claim `email` trong access token? Nếu dùng `userinfo` thì nâng FR-017 lên Should | FR-006, identity I7 | sondv | Trước design FE | Dùng `userinfo`, nâng FR-017 lên Should qua `refine` spec identity | open |
| Q-03 | Trang đăng nhập (trên origin identity) và app chính là một codebase Angular hay hai? | Cấu trúc `src/web`, build, CSP | sondv | Trước design FE | Cùng một workspace Angular, hai ứng dụng/entry, build đầu ra của trang đăng nhập do identity phục vụ | open (design/ADR) |
| Q-04 | Thư viện UI: Angular Material/CDK, hay tự viết với token thiết kế? | Phụ thuộc (T0/T1), a11y, công sức | sondv | Trước design FE | Angular CDK + tự viết theo token (ít phụ thuộc, học nhiều), cần xác nhận theo `dependency-policy.md` | open |
| Q-05 | Phiên bản Angular, Node, TypeScript, runner test | Toolchain | sondv | Khi scaffold | Theo `config/tool-versions.yaml` (TODO), xác minh tại angular.dev | open |
| Q-06 | Điểm thu thập lỗi JS (NFR-006): chỉ log console + endpoint nào? | Quan sát | sondv | Trước design FE | Chỉ ghi vào console và gửi `correlationId` khi có lỗi API; điểm thu thập hoãn | deferred |

## 16. Rủi ro và quyết định cần ADR

| Mục | Mô tả | Hướng xử lý |
|---|---|---|
| ADR nơi lưu token phía client | Q-01; chọn sai làm lộ token | ADR riêng trước FR-002/FR-004 |
| ADR thư viện UI | Q-04 | Chốt theo `dependency-policy.md` |
| Rủi ro: phình phạm vi | Học Angular dễ kéo thêm màn hình | Phạm vi cố định ở mục 4; board/task là spec khác |
| Rủi ro: bảo mật phía client | XSS, lộ token, open redirect | BR-004, BR-005, lint, test tấn công, review lăng kính bảo mật |
| Rủi ro: phụ thuộc identity | FE chờ I5 và FR-017; identity I5 chờ spec này | Làm khung (app shell, i18n, form) và test với mock trước; đăng nhập thật sau khi I1–I4 và I5 có |
| Rủi ro: hai thế giới kiểu API | Kiểu viết tay lệch backend | Kiểu sinh từ OpenAPI + `openapi:check` |
| Rủi ro: ngưỡng hiệu năng sai | A-05 | Hiệu chỉnh sau khi đo |

## 17. Truy vết

Đề xuất chia lát (chưa tạo issue; bảng lát chính thức do `plan-init` slices sau design):

| Yêu cầu | Lát đề xuất | Issue | Kịch bản (`.feature`) | ADR |
|---|---|---|---|---|
| NFR-001…NFR-009, FR-008, FR-010, FR-009 | W1 Khung ứng dụng (app shell, routing, guard, lỗi, i18n, gate) | | | ADR thư viện UI |
| FR-001 | W2 Đăng ký | | | |
| FR-002, FR-003, FR-004, FR-007 | W3 Đăng nhập, giữ phiên, đăng xuất (phụ thuộc identity I5, ADR token) | | | ADR nơi lưu token |
| FR-005, FR-006 | W4 Đổi mật khẩu, hồ sơ (FR-006 phụ thuộc Q-02) | | | |

## 18. Lịch sử thay đổi

| Ngày | Thay đổi | Nguồn |
|---|---|---|
| 2026-10-21 | Tạo bản nháp từ yêu cầu bổ sung spec FE; chốt qua hai lô hỏi: mục tiêu học, phạm vi chỉ identity, vi + en, đủ bộ real-world, màn hình (đăng ký, đăng nhập, đổi mật khẩu, hồ sơ + đăng xuất), responsive, làm mới im lặng, chạy local trước | Phiên Claude Code, sondv |

## 19. Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | `discovery:web-identity-ui:goal` | Thước đo thành công | Học Angular qua dự án gần thực tế | khai phá | 2026-10-21 |
| 2 | `discovery:web-identity-ui:scope` | Phạm vi màn hình lần này | Chỉ phần identity | khai phá | 2026-10-21 |
| 3 | `discovery:web-identity-ui:i18n` | Ngôn ngữ giao diện | Tiếng Việt + tiếng Anh, chọn được | khai phá | 2026-10-21 |
| 4 | `discovery:web-identity-ui:realworld-level` | Mức real-world | Đủ bộ: auth, routing/guard, form, lỗi, i18n, a11y, test, e2e, CI gate | khai phá | 2026-10-21 |
| 5 | `discovery:web-identity-ui:screens` | Màn hình trong lần này | Đăng ký; đăng nhập (trên origin identity); đổi mật khẩu; hồ sơ tối thiểu + đăng xuất | khai phá | 2026-10-21 |
| 6 | `discovery:web-identity-ui:devices` | Thiết bị, trình duyệt | Responsive (mobile + desktop), trình duyệt hiện đại | khai phá | 2026-10-21 |
| 7 | `discovery:web-identity-ui:session` | Duy trì phiên | Tự làm mới im lặng đến hạn refresh 7 ngày | khai phá | 2026-10-21 |
| 8 | `discovery:web-identity-ui:deploy` | Môi trường chạy | Chạy local trước, deploy AWS sau | khai phá | 2026-10-21 |

## 20. Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
