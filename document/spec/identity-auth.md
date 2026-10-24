---
title: Xác thực và ủy quyền (identity-service)
status: confirmed        # draft | confirmed | superseded; confirmed lại sau spec-review vòng 3 (2026-10-21)
owner: sondv
updated: 2026-10-21
services: [identity-service]
issues: [TB-004]
---

# Xác thực và ủy quyền (identity-service)

## 1. Yêu cầu gốc

> chức năng auth

Nguồn: sondv, phiên Claude Code ngày 2026-10-21, lệnh `requirement-discovery`.

Làm rõ qua hai lô câu hỏi cùng ngày (xem mục 18).

## 2. Bối cảnh và mục tiêu

- Vấn đề cần giải: TeamBoard chưa có cách định danh người dùng. Board/Task cần biết "ai đang gọi" và "được làm gì". Dự án còn là bài tập học bảo mật nên chủ dự án muốn tự xây thay vì dùng framework identity (ADR 0006).
- Giá trị/kết quả mong muốn: một `identity-service` cho phép người dùng tự đăng ký, đăng nhập và nhận token mà các service khác kiểm chứng được; đồng thời là nơi luyện JWT, OAuth 2.0, OIDC và kiểm thử bảo mật.
- Thước đo thành công:
  - Mỗi giai đoạn I1–I8 có kịch bản BDD (đường chính, lỗi và tấn công) xanh.
  - Các tấn công trong `identity-learning.md` mục 3 đều bị chặn bằng test.
  - Quy mô không phải thước đo thành công; spec chỉ giả định quy mô học tập (NFR-005, A-05).

## 3. Người dùng và vai trò

| Vai trò | Mô tả | Mục đích sử dụng |
|---|---|---|
| `user` | Người dùng cuối của TeamBoard, định danh bằng email. Vai trò toàn hệ thống duy nhất. | Đăng ký, đăng nhập, giữ phiên, đăng xuất |
| Client ứng dụng (public) | Ứng dụng có người dùng đăng nhập: frontend Angular (spec riêng, ADR 0009) và công cụ thử như Postman/curl | Authorization Code + PKCE (I5), OIDC (I7) |
| Client máy (confidential) | Service gọi service | Client Credentials (I6) |
| Resource server | Các service khác (board, task...) | Kiểm tra token, đọc JWKS, introspection (I2, I3, I8) |

Không có vai trò `admin` ở lần này. Quyền trên từng board (owner/member) thuộc `board-service`, không thuộc spec này.

## 4. Phạm vi

**Trong phạm vi (lần này):** toàn bộ `identity-service` theo lộ trình I1–I8 (`document/architecture/identity-learning.md` mục 2), chia lát giao theo mục 17:
- I1 người dùng và mật khẩu (gồm đổi mật khẩu); I2 phát/kiểm tra JWT; I3 khoá và JWKS; I4 refresh token.
- I5 Authorization Code + PKCE; identity-service phục vụ trang đăng nhập trên origin của nó (FR-019), **giao diện và mã trang do spec FE riêng định nghĩa** (ADR 0009, Q-13), nên I5 phụ thuộc spec FE. Frontend Angular của ứng dụng (đăng ký, đổi mật khẩu, màn hình khác) cũng thuộc spec FE.
- I6 Client Credentials; I7 OpenID Connect; I8 ủy quyền theo scope, thu hồi và introspection tối thiểu.

**Ngoài phạm vi:**
- I9: tích hợp gateway kiểm JWT và `board-service` yêu cầu đăng nhập. Làm spec/lát riêng khi `gateway` tồn tại.
- Xác minh email, quên/đặt lại mật khẩu (cần `notification-service`, chưa có).
- Xoá tài khoản, xuất/xoá dữ liệu cá nhân (compliance `none`, A-09); sự kiện miền như `UserRegistered`; đăng xuất OIDC (RP-initiated logout).
- Vai trò `admin` và endpoint quản trị tài khoản; đăng nhập qua bên thứ ba (Google, GitHub...), SAML, MFA.
- Implicit, Resource Owner Password, Hybrid: chỉ đọc lý thuyết, không xây (ADR 0006).
- Frontend ứng dụng (đăng ký, đổi mật khẩu, màn hình khác, board/task UI): spec FE riêng theo ADR 0009 (thay ADR 0008). Spec này chỉ yêu cầu `identity-service` phục vụ **trang đăng nhập** của luồng Authorization Code (FR-019) và API cho client Angular.
- Phân quyền theo board (owner/member): thuộc `board-service`.

## 5. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| Access token | JWT ngắn hạn mà client gửi kèm mỗi request API |
| Refresh token | Chuỗi bí mật dài hạn, đổi lấy access token mới; xoay vòng mỗi lần dùng |
| JWKS | Tập khoá công khai (`/.well-known/jwks.json`) để service khác xác minh chữ ký |
| `kid` | Mã định danh khoá ký, nằm trong header của JWT |
| PKCE | Cơ chế chống chặn mã uỷ quyền (RFC 7636) |
| Introspection | Hỏi `identity-service` xem một token còn hiệu lực không |
| Chuỗi refresh (token family) | Các refresh token sinh ra nối tiếp từ một lần đăng nhập |
| JWT (JSON Web Token) và claim | JWT là chuỗi `header.payload.chữ ký`; payload chứa các claim (cặp tên và giá trị) như `sub` (người hoặc client), `iss` (bên phát), `aud` (bên nhận dự kiến), `exp` (hạn), `nbf` (chưa có hiệu lực trước thời điểm này), `iat` (lúc phát), `jti` (mã duy nhất). Chữ ký cho biết token chưa bị sửa nhưng không mã hoá nội dung |
| `aud` (audience), `iss` (issuer) | Bên nhận mà token dành cho và bên đã phát token. Resource server từ chối token có `aud` hay `iss` sai |
| Xoay vòng refresh token, phát hiện dùng lại | Mỗi lần dùng refresh token thì cấp token mới và vô hiệu token cũ. Token cũ bị dùng lần nữa là dấu hiệu bị đánh cắp nên thu hồi cả chuỗi |
| OAuth 2.0, OIDC | OAuth 2.0 là chuẩn để một ứng dụng (client) xin token truy cập thay mặt người dùng hoặc máy. OIDC (OpenID Connect) thêm lớp biết ai đã đăng nhập (`id_token`, `userinfo`) |
| Authorization Code | Luồng cho ứng dụng có người dùng đăng nhập: client chuyển người dùng sang identity, nhận về một mã dùng một lần rồi đổi mã lấy token |
| PKCE: `code_verifier`, `code_challenge`, `state`, `nonce` | PKCE thêm `code_verifier` (chuỗi bí mật client tự tạo) và `code_challenge` (bản băm của nó, gửi trước) để kẻ chặn được mã không đổi được token. `state` chống CSRF trong luồng OAuth; `nonce` chống phát lại `id_token` |
| Client Credentials | Luồng giữa máy với máy: một service gửi `client_id` và bí mật của nó để nhận token, không có người dùng |
| `redirect_uri` | Địa chỉ identity chuyển người dùng về sau khi đăng nhập; phải khớp chính xác danh sách đã đăng ký, nếu không kẻ tấn công có thể nhận mã |
| Scope | Chuỗi quyền mà token được cấp; resource server kiểm scope trước khi cho thao tác |
| `userinfo`, discovery | `userinfo` trả thông tin người dùng theo scope. Discovery (`/.well-known/openid-configuration`) là trang JSON liệt kê các endpoint để client tự cấu hình |
| IdP, SPA, origin | IdP (identity provider): bên quản lý danh tính và đăng nhập. SPA (single-page application): ứng dụng web chạy trong trình duyệt như Angular. Origin: bộ ba giao thức, tên miền và cổng của một trang; trình duyệt cô lập script và cookie theo origin |
| CSRF, clickjacking, CSP | CSRF: khiến trình duyệt đã đăng nhập tự gửi request không chủ ý vì cookie gửi kèm tự động. Clickjacking: nhúng trang đăng nhập vào khung ẩn để lừa bấm. CSP (Content Security Policy): header giới hạn nguồn script và khung được phép |
| BFF (backend for frontend) | Server nhỏ giữa SPA và API, giữ token ở phía server để SPA chỉ cầm cookie |
| `alg=none`, tráo khoá, HMAC | Hai kiểu tấn công kiểm token: đặt `alg` là `none` (không cần chữ ký) hoặc là HS256 rồi dùng khoá công khai làm bí mật HMAC (mã xác thực dùng một khoá chung). Phòng bằng cách ép thuật toán đã cấu hình |
| Salt, PBKDF2, BCL | Salt: chuỗi ngẫu nhiên trộn vào mật khẩu trước khi băm. PBKDF2: hàm băm mật khẩu cố ý chậm (lặp nhiều vòng). BCL (Base Class Library): thư viện chuẩn của .NET, gồm các hàm mật mã |
| DoS, MFA | DoS: làm dịch vụ nghẽn để người khác không dùng được. MFA: xác thực nhiều yếu tố (mật khẩu cộng mã khác), ngoài phạm vi |
| Secrets Manager, KMS | Secrets Manager: dịch vụ AWS lưu bí mật có kiểm soát truy cập. KMS: dịch vụ quản lý khoá, khoá không rời dịch vụ |
| Outbox | Cách phát sự kiện tin cậy: ghi sự kiện cùng giao dịch với dữ liệu rồi gửi sau, để không mất sự kiện khi lỗi giữa chừng. Spec này không phát sự kiện nên chỉ nhắc để loại trừ |
| Blast radius (bán kính ảnh hưởng) | Mức độ một thay đổi hoặc sự cố lan sang các phần khác; ở đây là hợp đồng claim ảnh hưởng mọi service dùng token |
| p95, Fargate | p95: 95% request nhanh hơn ngưỡng đó. Fargate: chạy container trên AWS không quản lý máy chủ |

## 6. Yêu cầu chức năng

Ưu tiên: Must / Should / Could / Won't. Nguồn "Giả định" nghĩa là chưa có lời khách trực tiếp, xem mục 14.

| ID | Mô tả | Ưu tiên | Tiêu chí chấp nhận | Nguồn |
|---|---|---|---|---|
| FR-001 | Người dùng tự đăng ký bằng email và mật khẩu | Must (I1) | Email hợp lệ chưa dùng + mật khẩu đạt BR-002 → tài khoản được tạo (201), không trả mật khẩu/hash. Email đã tồn tại → 409 `problem+json` (lộ email tồn tại là đánh đổi đã chốt, xem BR-003); mật khẩu yếu → 400 `problem+json` | Lời người dùng (đăng ký mở, chưa xác minh email) |
| FR-002 | Người dùng đăng nhập bằng email và mật khẩu | Must (I1) | Đúng thông tin → nhận token theo FR-005. Sai email hoặc sai mật khẩu → cùng một lỗi chung 401, cùng cấu trúc, độ trễ xấp xỉ nhau | Lời người dùng / `identity-learning.md` |
| FR-003 | Khoá tạm đăng nhập khi sai liên tiếp, theo cặp (email, IP) | Must (I1) | Sau N lần sai liên tiếp từ một IP cho một email (A-03), cặp đó bị từ chối trong T phút kể cả khi nhập đúng mật khẩu, trả 401 chung giống sai mật khẩu kèm `Retry-After`; hết T phút đăng nhập được lại; đăng nhập thành công reset bộ đếm của cặp; đăng nhập đúng từ IP khác không bị khoá bởi lần sai của IP kia | Lời người dùng (lô review 1) |
| FR-004 | Mật khẩu được lưu dạng băm có salt, so sánh hằng thời gian | Must (I1) | Dữ liệu lưu không chứa mật khẩu rõ; hai tài khoản cùng mật khẩu có hash khác nhau; mật khẩu không xuất hiện trong log | `identity-learning.md` mục 3 |
| FR-005 | Phát access token (JWT) khi đăng nhập thành công | Must (I2) | Token có `iss`, `sub`, `aud`, `exp`, `iat`, `nbf`, `jti`; ký bằng thuật toán đã cấu hình; thời hạn theo A-03 | Lời người dùng (I1–I8) |
| FR-006 | Quy tắc kiểm tra token: mỗi resource server tự kiểm tra cục bộ bằng khoá công khai lấy từ JWKS (có cache), không gọi `identity-service` mỗi request | Must (I2) | Chấp nhận token hợp lệ. Từ chối: sửa payload, `alg=none`, `alg` khác cấu hình, hết hạn (dung sai A-03), chưa tới `nbf`, sai `iss`/`aud`, `kid` lạ. Kiểm chứng bằng test của bộ kiểm tra; nơi đặt bộ kiểm tra dùng chung (ranh giới `src/shared`) chốt ở design | Người dùng giao theo best practice (lô review 2) / `identity-learning.md` |
| FR-022 | Người dùng đã đăng nhập đổi mật khẩu | Should (I1) | Cần mật khẩu hiện tại đúng và mật khẩu mới đạt BR-002; thành công thì mọi refresh token của người dùng bị thu hồi. Sai mật khẩu hiện tại → 401 chung và tính vào bộ đếm khoá (FR-003) | Lời người dùng (lô review 1) |
| FR-007 | Công bố khoá công khai qua JWKS | Must (I3) | `GET /.well-known/jwks.json` trả các khoá hiện hành, mỗi khoá có `kid`; chỉ chứa khoá công khai | Lời người dùng |
| FR-008 | Xoay khoá ký không làm hỏng token còn hạn | Should (I3) | Sau xoay, token mới ký bằng `kid` mới; token cũ còn hạn vẫn xác minh được bằng khoá cũ còn trong JWKS; khoá cũ bị gỡ sau khi mọi token ký bằng nó hết hạn. Chu kỳ và cách kích hoạt xoay: hoãn sang design/ADR | Lời người dùng / Hoãn (Q-03) |
| FR-009 | Cấp và dùng refresh token, xoay vòng mỗi lần dùng | Must (I4) | Đổi refresh token hợp lệ → nhận cặp access/refresh mới, token cũ vô hiệu. Token hết hạn hoặc không tồn tại → từ chối. Server chỉ lưu băm refresh token. Hai request đổi cùng token đồng thời: tối đa một thành công, lần còn lại xử lý theo FR-010 | Lời người dùng |
| FR-010 | Phát hiện dùng lại refresh token đã xoay | Must (I4) | Dùng lại token cũ → từ chối, **thu hồi cả chuỗi refresh**, ghi log/metric bảo mật | `identity-learning.md` |
| FR-011 | Đăng xuất thu hồi phiên | Must (I4) | Sau đăng xuất, refresh token của phiên đó không còn dùng được **và** phiên cookie trên identity (FR-019) bị huỷ, gọi `authorize` sau đó phải nhập lại mật khẩu; access token còn hạn xử lý theo BR-006. Đăng xuất OIDC RP-initiated vẫn ngoài phạm vi | Giả định; Confirm log #15 |
| FR-012 | Endpoint `authorize` cho Authorization Code + PKCE | Should (I5) | Kiểm `redirect_uri` khớp chính xác danh sách đã đăng ký, có `state`, có `code_challenge`; sai/thiếu → lỗi, không chuyển hướng tới `redirect_uri` chưa xác thực | Lời người dùng |
| FR-013 | Endpoint `token` đổi mã lấy token (có `code_verifier`) | Should (I5) | Mã dùng một lần, ngắn hạn, gắn với client và `redirect_uri`. Thiếu/sai `code_verifier`, tái sử dụng `code`, `code` hết hạn → từ chối | Lời người dùng |
| FR-014 | Quản lý client đã đăng ký (public và confidential) | Should (I5, I6) | Mỗi client có `client_id`, loại, `redirect_uri` hợp lệ, scope được phép; secret của confidential client chỉ lưu dạng băm. Client khai báo qua cấu hình/migration khi triển khai, không có API đăng ký/quản trị; secret lấy từ kho bí mật | Lời người dùng (lô review 2) |
| FR-015 | Client Credentials cho service-to-service | Should (I6) | Đúng `client_id/secret` → token máy với scope được phép. Scope vượt quyền bị từ chối. Token máy không dùng được như token người (`sub`/loại token khác nhau) | Lời người dùng |
| FR-016 | Phát `id_token` OIDC | Could (I7) | `id_token` chứa `aud`, `nonce` (nếu client gửi), `sub`, `exp`; chỉ phát khi scope có `openid` | Lời người dùng |
| FR-017 | Endpoint `userinfo` | Could (I7) | Trả claim theo scope (`profile`, `email`); thiếu/sai token → 401 | Lời người dùng |
| FR-018 | Discovery `/.well-known/openid-configuration` | Could (I7) | Nội dung khớp endpoint thực tế (authorize, token, jwks, userinfo...) | Lời người dùng |
| FR-019 | Identity-service phục vụ trang đăng nhập cho luồng Authorization Code, trên origin của chính nó (I5/I7) | Should (I5) | Phía identity (giao diện và mã trang do spec FE định nghĩa, Q-13): client chuyển người dùng tới `authorize`; chưa có phiên thì hiển thị trang đăng nhập; người dùng nhập email/mật khẩu trên trang này (không phải trong SPA của client). Ví dụ: đúng thông tin → chuyển về `redirect_uri` đã đăng ký kèm `code` và `state`; sai mật khẩu hoặc cặp đang bị khoá → cùng lỗi chung và `Retry-After` như FR-002/FR-003, không chuyển hướng; `redirect_uri` không khớp → lỗi, không chuyển hướng (FR-012). Phiên đăng nhập dùng cookie `HttpOnly`, `Secure`, `SameSite`, hết hạn sau 30 phút không hoạt động, chỉ phục vụ luồng `authorize`. Khoá tạm FR-003 và giới hạn tốc độ NFR-008 áp dụng cho cả trang này. Chống CSRF và clickjacking (NFR-010) | Lời người dùng (lô 2; đổi sang Angular ở system-design, Confirm log #13, #14, #16) |
| FR-020 | Ủy quyền theo scope và policy | Should (I8) | Endpoint được bảo vệ chỉ chấp nhận token có scope yêu cầu; thiếu scope → 403. Không cần role | Lời người dùng (không role) |
| FR-021 | Thu hồi token và introspection tối thiểu | Should (I8) | Resource server hỏi được token còn hiệu lực không; token đã thu hồi báo không hiệu lực. Introspection là tuỳ chọn, gọi bằng token máy có scope `introspect`; resource server không gọi thì access token hiệu lực đến `exp` (BR-006) | Lời người dùng (lô review 2) |

## 7. Quy tắc nghiệp vụ

| ID | Quy tắc | Ví dụ | Nguồn |
|---|---|---|---|
| BR-001 | Email là định danh duy nhất, chuẩn hoá (bỏ khoảng trắng đầu/cuối, so sánh không phân biệt hoa thường) | `A@x.com` và `a@x.com` là cùng một tài khoản | Lời người dùng (đăng nhập bằng email) / Giả định chi tiết chuẩn hoá |
| BR-002 | Mật khẩu tối thiểu 12 ký tự, tối đa độ dài hợp lý để chống DoS băm (A-03) | `short1` bị từ chối; chuỗi 10.000 ký tự bị từ chối | Giả định |
| BR-003 | Lỗi đăng nhập (kể cả khoá tạm) không tiết lộ email có tồn tại hay không. Ngoại lệ đã chốt: đăng ký trùng email trả 409, vì chưa có xác minh email để thông báo riêng | Sai email, sai mật khẩu và đang bị khoá tạm cho cùng 401 | `identity-learning.md` / lời người dùng (lô review 1) |
| BR-004 | Access token ngắn hạn (A-03); refresh token dài hạn hơn và xoay vòng | Access 10 phút, refresh 7 ngày | Giả định |
| BR-005 | Mã uỷ quyền dùng một lần và ngắn hạn; `redirect_uri` so khớp **chính xác** | `https://app/cb?x=1` không khớp `https://app/cb` | `identity-learning.md` |
| BR-006 | Thu hồi tức thời cho refresh token; với access token, hiệu lực kết thúc theo `exp` trừ khi resource server dùng introspection (FR-021) | Đăng xuất xong, access token còn tối đa 10 phút nếu chỉ kiểm chữ ký | Giả định (đánh đổi của JWT) |
| BR-007 | Không log mật khẩu, token, refresh token, client secret | Log sự kiện đăng nhập chỉ ghi `userId`, kết quả, `correlationId` | `agent-conduct`/`identity-learning.md` |
| BR-008 | Token máy (Client Credentials) và token người phân biệt được | Token máy không có claim người dùng, có loại token khác | `identity-learning.md` |

## 8. Luồng nghiệp vụ

### 8.1 Luồng chính

Đăng ký và đăng nhập (I1, I2, I4):

1. Người dùng gửi email + mật khẩu tới đăng ký → tài khoản được tạo.
2. Người dùng gửi email + mật khẩu tới đăng nhập → nhận access token + refresh token.
3. Client gọi API với access token; resource server kiểm tra chữ ký bằng JWKS (FR-006, FR-007).
4. Access token hết hạn → client đổi refresh token lấy cặp mới (FR-009).
5. Người dùng đăng xuất → refresh token bị thu hồi (FR-011).

Authorization Code + PKCE (I5):

1. Client tạo `code_verifier`, `code_challenge`, `state`, chuyển người dùng tới `authorize`.
2. Người dùng đăng nhập trên trang đăng nhập của `identity-service` (FR-019), không nhập mật khẩu trong SPA của client.
3. `identity-service` chuyển về `redirect_uri` kèm `code` và `state`.
4. Client gửi `code` + `code_verifier` tới `token` → nhận token (kèm `id_token` nếu có scope `openid`).

Client Credentials (I6): client máy gửi `client_id/secret` + scope → nhận token máy.

### 8.2 Luồng thay thế

- Đăng nhập khi cặp (email, IP) đang bị khoá tạm → 401 chung như sai mật khẩu, kèm `Retry-After` (FR-003, BR-003).
- Xoay khoá ký đang diễn ra: JWKS chứa cả khoá mới và cũ trong thời gian chuyển tiếp (FR-008).
- Client public không có secret: dùng PKCE thay secret.

### 8.3 Luồng lỗi và trường hợp biên

- Đăng ký cùng email đồng thời (hai request cùng lúc) → đúng một tài khoản được tạo, request còn lại nhận lỗi trùng (ràng buộc duy nhất ở tầng dữ liệu).
- Hai lần đổi cùng một refresh token đồng thời → một lần thành công, lần còn lại bị coi là dùng lại và thu hồi chuỗi, không có cửa sổ dung sai (đã chốt; người dùng có thể bị đăng xuất nhầm khi mạng chậm).
- Bấm đăng nhập hai lần → hai phiên hợp lệ độc lập, không lỗi.
- Token sai định dạng, rỗng, rất dài → 401, không crash, không log nguyên token.
- Lệch đồng hồ giữa service → dung sai nhỏ khi kiểm `exp/nbf` (A-03).
- `alg=none`, `alg` trong header khác cấu hình, tráo khoá (HMAC bằng khoá công khai) → từ chối.
- `redirect_uri` giả/có wildcard, thiếu PKCE, replay `code`, thiếu `state` → từ chối.
- Mất khoá ký hoặc không đọc được kho bí mật → `ready` không đạt, không phát token bằng khoá tạm.

## 9. Dữ liệu

Chi tiết lược đồ thuộc design; dưới đây là thực thể và ràng buộc nghiệp vụ.

| Thực thể | Thuộc tính chính | Ràng buộc |
|---|---|---|
| Người dùng | id, email (chuẩn hoá), hash mật khẩu + salt + tham số băm, trạng thái khoá tạm (số lần sai, thời điểm hết khoá), thời điểm tạo | Email duy nhất; không lưu mật khẩu rõ |
| Khoá ký | `kid`, thuật toán, khoá công khai, tham chiếu khoá riêng (kho bí mật), trạng thái, thời điểm tạo/hết hạn | Khoá riêng không nằm trong repo/DB thường; mỗi `kid` gắn một thuật toán |
| Refresh token | băm token, id chuỗi, người dùng, client, hạn, trạng thái (đã dùng/thu hồi) | Chỉ lưu băm; xoay vòng; thu hồi theo chuỗi |
| Client | `client_id`, loại, băm secret (confidential), `redirect_uri` hợp lệ, scope được phép | `redirect_uri` so khớp chính xác |
| Mã uỷ quyền | băm mã, client, `redirect_uri`, `code_challenge`, người dùng, hạn, đã dùng chưa | Dùng một lần, ngắn hạn |
| Sự kiện bảo mật | loại, `userId`/client, thời điểm, `correlationId`, kết quả | Không chứa bí mật |

Refresh token hết hạn được dọn sau 30 ngày; sự kiện bảo mật giữ 90 ngày (đã chốt).

## 10. Phân quyền và bảo mật

| Hành động | Ai được phép | Ghi chú (dữ liệu nhạy cảm, audit) |
|---|---|---|
| Đăng ký, đăng nhập, đổi refresh token, đăng xuất | Công khai (đăng xuất cần refresh token hợp lệ) | Giới hạn tốc độ NFR-008 và khoá tạm FR-003; ghi sự kiện bảo mật |
| Đổi mật khẩu | Người dùng đã đăng nhập, chỉ cho chính tài khoản của mình | Cần mật khẩu hiện tại; thu hồi mọi refresh token (FR-022) |
| Đọc JWKS, discovery | Công khai | Chỉ khoá công khai |
| `authorize`, `token` (người dùng) | Client đã đăng ký | Public client dùng PKCE |
| `token` (Client Credentials) | Confidential client | Secret lưu băm; scope tối thiểu |
| `userinfo` | Người giữ access token hợp lệ có scope phù hợp | Claim theo scope |
| Introspection / thu hồi | Client máy có scope `introspect` | Tuỳ chọn (FR-021) |

- Dữ liệu nhạy cảm: mật khẩu, hash, refresh token, client secret, khoá ký riêng, email (PII). Không log; khoá ký ở Secrets Manager/KMS (`terraform.md`, ADR 0006).
- Audit: log/metric cho đăng nhập thất bại, khoá tạm, dùng lại refresh token, token bị từ chối, xoay khoá (`observability.md`).
- Lưu ý: tự xây để học, **không dùng cho người dùng/dữ liệu thật** khi chưa rà soát độc lập (ADR 0006).

## 11. Tích hợp và sự kiện

| Với service/hệ thống | Cách giao tiếp (REST/sự kiện) | Dữ liệu/sự kiện |
|---|---|---|
| Mọi resource server | REST: tải JWKS; introspection (tuỳ chọn, FR-021) | Khoá công khai; trạng thái token |
| `gateway` | Đường vào công khai duy nhất của client (kiến trúc: client → gateway → service): định tuyến route công khai của identity, rate limit, CORS, `X-Forwarded-For`, `X-Correlation-Id`. Kiểm JWT bằng JWKS thuộc I9 | Route công khai chốt ở design D0.5; cấu hình và dựng gateway ngoài phạm vi spec này (I9) |
| `board-service`, `task-service` | Dùng `sub` làm định danh người dùng | Hợp đồng claim cần chốt ở design/contract |
| Secrets Manager/KMS | SDK AWS | Khoá ký riêng |
| Sự kiện miền (ví dụ `UserRegistered`) | SNS → SQS qua outbox (ADR 0002) | Không phát ở lần này (đã chốt) |

## 12. Yêu cầu phi chức năng

| ID | Nội dung (kèm số đo) |
|---|---|
| NFR-001 | Mỗi endpoint có `/health/live`, `/health/ready` và OpenTelemetry (trace, metric, log) theo `PROJECT.md` |
| NFR-002 | Băm mật khẩu bằng PBKDF2 (BCL) với số vòng theo OWASP hiện hành; **không** tự cài thuật toán mật mã (ADR 0006) |
| NFR-003 | Lỗi theo `problem+json` (`api-endpoints.md`); không lộ chi tiết nội bộ hay stack trace |
| NFR-004 | Mọi request/event mang `correlationId` + `traceparent` |
| NFR-005 | Quy mô học tập: dưới 100 người dùng và dưới 10 request/giây; độ trễ đăng nhập p95 dưới 1 giây (A-05) |
| NFR-006 | Mỗi điểm bảo mật (`identity-learning.md` mục 3) có ít nhất một kịch bản BDD và một test tấn công |
| NFR-007 | Metric bảo mật xuất ra và không chứa bí mật |
| NFR-008 | Giới hạn 10 request/phút/IP cho đăng ký, đăng nhập và `token`; vượt giới hạn trả 429 kèm `Retry-After`; ngưỡng cấu hình được. Thực thi ở gateway theo `api-endpoints.md`; cách thực thi khi chưa có gateway: design Q-G |
| NFR-009 | Không cam kết uptime (không SLA); mất DB ở môi trường dev chấp nhận được, khôi phục bằng migration và seed; không yêu cầu RPO/RTO |
| NFR-010 | Trang đăng nhập (FR-019) chống CSRF (anti-forgery/SameSite) và clickjacking (`frame-ancestors none`/CSP), cookie phiên `HttpOnly` + `Secure`; không nhúng script bên thứ ba; mật khẩu không xuất hiện trong URL, log, hay lưu trong trình duyệt ngoài trường nhập | ADR 0009, OWASP |

## 13. Giao diện (chưa áp dụng)

Frontend Angular được làm lại theo ADR 0009 (thay ADR 0008), phạm vi và màn hình ứng dụng nằm ở **spec FE riêng**. Spec này chỉ yêu cầu trang đăng nhập của luồng Authorization Code (FR-019) trên origin của `identity-service`. Mọi giao diện khác (đăng ký, đổi mật khẩu, quản lý tài khoản, consent) thuộc spec FE.

## 14. Giả định

| # | Giả định | Người xác nhận | Trạng thái |
|---|---|---|---|
| A-01 | Đăng ký mở cho mọi người, không xác minh email; không có luồng quên mật khẩu | sondv | đã xác nhận (lô 1) |
| A-02 | Chỉ một vai trò `user`; quyền theo board thuộc `board-service` | sondv | đã xác nhận (lô 1, 2) |
| A-03 | Số đo mặc định: mật khẩu ≥ 12 ký tự; phiên cookie đăng nhập trên identity 30 phút không hoạt động (FR-019); khoá tạm 15 phút sau 5 lần sai liên tiếp theo cặp (email, IP); access token 10 phút; refresh token 7 ngày; dung sai đồng hồ 60 giây; tất cả cấu hình được | sondv | đã xác nhận (chọn mặc định ở lô 2; duyệt spec trước review) |
| A-04 | I9 (gateway) tách thành spec/lát riêng | sondv | đã xác nhận (lô 1, 2) |
| A-05 | Quy mô dưới 100 người dùng, dưới 10 request/giây; không dùng cho người dùng thật | sondv | đã xác nhận (lô review 3) |
| A-06 | Trang đăng nhập do `identity-service` phục vụ trên origin của nó, giao diện Angular (ADR 0009); SPA của client không nhận mật khẩu | sondv | đã chọn (system-design, Confirm log #14) |
| A-07 | Access token là JWT tự chứa; thu hồi tức thời chỉ áp dụng cho refresh token trừ khi dùng introspection | sondv | đã xác nhận (lô review 2) |
| A-08 | Không có dữ liệu hay người dùng cũ cần chuyển (không migration dữ liệu) | sondv | đã xác nhận (lô review 3) |
| A-09 | `quality.compliance` của spec này là `none`: dự án học, không dùng dữ liệu thật; không có yêu cầu xoá/xuất dữ liệu cá nhân | sondv | đã xác nhận (lô review 1); đề xuất điền `none` vào `config/project.yaml` (chưa sửa, chờ người dùng) |

## 15. Câu hỏi mở

Các câu Q-01 và Q-04 đến Q-10 đã được chốt (xem Confirm log). Q-02, Q-03, Q-11 và Q-12 đã được chốt ở bước sau (bảng dưới). Còn **một câu mở: Q-13**. Mỗi câu hỏi mở có lựa chọn, khuyến nghị kèm lý do và hậu quả nếu chưa chốt.

| # | Câu hỏi | Ảnh hưởng tới | Người trả lời | Hạn | Trạng thái |
|---|---|---|---|---|---|
| Q-02 | Thuật toán ký mặc định (RSA hay ECDSA)? | FR-005, FR-007 | sondv | – | `answered`: RS256 (RSA 2048), người dùng chọn, `document/design/identity-auth.md` D1 |
| Q-03 | Chu kỳ và cách kích hoạt xoay khoá, thời gian giữ khoá cũ? | FR-008 | sondv | – | `answered`: thủ công bằng ba lệnh CLI, giữ khoá cũ ít nhất 24 giờ (D7; xác nhận CLI ở Confirm log của design #6) |
| Q-11 | Nơi đặt bộ kiểm tra token dùng chung (ranh giới `src/shared`) và hợp đồng claim (`aud`, `sub`, scope)? | FR-005, FR-006 | sondv | – | `answered`: thư viện chung `Teamboard.Auth` bọc `JwtBearer`, `aud` chung `teamboard` (D2; Confirm log của design #2 và #6); hợp đồng claim ghi vào `document/contracts/` khi vào lát |
| Q-12 | Thời hạn phiên cookie, đăng xuất | FR-011, FR-019 | sondv | – | `answered`: 30 phút không hoạt động; đăng xuất huỷ phiên cookie (Confirm log #15) |
| Q-13 | Spec FE riêng: giao diện trang đăng nhập, ứng dụng Angular, nơi lưu token phía client | FR-019, I5 | sondv | Trước khi bắt đầu I5 | `open` (spec FE `document/spec/web-identity-ui.md` đang `draft`) |

Các quyết định ở Q-02, Q-03, Q-11 nằm trong design (đang `draft` chờ xác nhận lý do cấp service); spec chỉ trỏ tới nơi chốt.

### Q-13 Khi nào làm I5 so với spec FE
- **Lựa chọn:**
  - A) Làm I1–I4 và I6 trước; chỉ bắt đầu I5 khi spec FE `confirmed`. Đánh đổi: I5 đến muộn hơn.
  - B) Làm I5 ngay với trang đăng nhập tối giản, giả định nơi lưu token và CORS. Đánh đổi: nhanh hơn nhưng dễ phải làm lại khi spec FE chốt khác.
  - C) Hoãn I5, I7, I8 sang giai đoạn sau. Đánh đổi: bỏ phần học PKCE và OIDC trong giai đoạn này.
- **Khuyến nghị:** A, vì I5 phụ thuộc quyết định của spec FE (nơi lưu token, một hay hai codebase, CORS) và I1–I4, I6 độc lập nên không bị chậm; làm I5 trước dễ phải làm lại.
- **Hậu quả nếu chưa chốt:** I5 và phần phụ thuộc của I7, I8 bị chặn; I1–I4 và I6 không bị ảnh hưởng.
- **Mặc định nếu quá hạn:** không có mặc định (blocker của I5); tiếp tục I1–I4, I6.

## 16. Rủi ro và quyết định cần ADR

| Mục | Mô tả | Hướng xử lý |
|---|---|---|
| ADR 0009 (đã chấp nhận) | Làm lại FE Angular, thay ADR 0008; trang đăng nhập nằm trên origin của identity | Spec FE riêng; chỉ còn cần ADR "nơi lưu token phía client" và cách phục vụ trang đăng nhập (build tĩnh cùng service) trước I5 |
| ADR: thuật toán ký, xoay khoá | Q-02, Q-03 | ADR (hoặc cập nhật ADR 0006) trước khi làm I2/I3 |
| ADR: nơi lưu token phía client | ADR 0006 nêu cần chốt; client Angular sẽ giữ token (bộ nhớ SPA + refresh qua cookie `HttpOnly`, hay BFF qua gateway) | ADR riêng trước khi làm luồng đăng nhập của FE (spec FE) |
| Rủi ro: phình phạm vi | I1–I8 trong một spec là rất lớn (OAuth + OIDC + ủy quyền) | Chia thành lát giao theo mục 17; mỗi lát có issue, plan và test riêng; I5–I8 là Should/Could, có thể hoãn |
| Rủi ro: bảo mật tự xây | Dễ sai lỗi tinh vi; không dùng cho dữ liệu thật | Test tấn công cho mọi điểm (NFR-006), review lăng kính bảo mật, tín hiệu xem lại ở ADR 0006 |
| Rủi ro: thu hồi access token | JWT tự chứa không thu hồi tức thời (BR-006, A-07) | Token ngắn hạn; introspection tuỳ chọn (đã chấp nhận) |
| Rủi ro: lộ email khi đăng ký | Đăng ký trùng trả 409 (BR-003 ngoại lệ) | Đã chấp nhận; giảm bằng giới hạn tốc độ NFR-008 |
| Rủi ro: đăng xuất nhầm | Không dung sai khi đổi refresh token đồng thời (FR-010) | Đã chấp nhận; theo dõi bằng metric tái sử dụng refresh token |
| Rủi ro: chi phí | Fargate + PostgreSQL chạy rỗng; `quality.monthlyBudgetUsd` còn `TODO` | `design-review` yêu cầu chốt trần ngân sách; `aws-service-selection` (`document/infra/identity-service.md`) |
| Blast radius | Hợp đồng claim của JWT (`sub`, `aud`, scope) ảnh hưởng mọi service sau này | Chốt trong `document/contracts/` trước I9 (Q-11) |

## 17. Truy vết

Đề xuất chia lát (chưa tạo issue, chờ người dùng đồng ý):

| Yêu cầu | Lát đề xuất | Issue | Kịch bản (`.feature`) | ADR |
|---|---|---|---|---|
| FR-001–004, FR-022 | I1 Người dùng và mật khẩu | | | |
| FR-005–006 | I2 Phát và kiểm tra JWT | | | ADR thuật toán ký |
| FR-007–008 | I3 Khoá và JWKS | | | ADR thuật toán ký, xoay khoá |
| FR-009–011 | I4 Refresh token | | | |
| FR-012–014, FR-019 | I5 Authorization Code + PKCE (phụ thuộc spec FE cho giao diện trang đăng nhập, Q-13) | | | ADR 0009, ADR nơi lưu token |
| FR-015 | I6 Client Credentials | | | |
| FR-016–018 | I7 OpenID Connect | | | |
| FR-020–021 | I8 Ủy quyền, thu hồi | | | |

## 18. Lịch sử thay đổi

| Ngày | Thay đổi | Nguồn |
|---|---|---|
| 2026-10-21 | Tạo bản nháp từ yêu cầu "chức năng auth"; chốt qua hai lô hỏi: phạm vi I1–I8 (I9 tách), đăng ký mở không xác minh email, định danh email một vai trò, I5/I7 dùng trang đăng nhập HTML tối giản, I8 theo scope không role, dùng số đo mặc định | Phiên Claude Code, sondv |
| 2026-10-21 | spec-review vòng 1: thêm FR-022 (đổi mật khẩu), NFR-008, NFR-009, A-08, A-09, Q-11; sửa FR-001, FR-003, FR-006, FR-008, FR-009, FR-014, FR-021, BR-003 và mục 8.2, 8.3, 9, 10, 11, 14, 15, 16 theo Confirm log | spec-review, sondv |
| 2026-10-21 | Đổi phạm vi FE theo system-design (ADR 0009): FR-019 thành trang đăng nhập trên origin của identity (giao diện Angular), sửa mục 3, 4, 8.1, 13, 14 (A-06), 16, 17; thêm NFR-010, Q-12; FE ứng dụng chuyển sang spec FE riêng; status về draft | system-design, sondv (Confirm log #13, #14) |
| 2026-10-21 | spec-review vòng 3: sửa FR-011 (huỷ cookie khi đăng xuất), FR-019 (ranh giới FE, ví dụ lỗi, NFR-008), A-03 (phiên 30 phút), Q-12 đóng, thêm Q-13 (spec FE), mục 4, 17 | spec-review, sondv (Confirm log #15, #16) |
| 2026-10-22 | Mở rộng mục 5 Thuật ngữ với các thuật ngữ nâng cao (giải thích lần đầu theo `explain-terms.md`); **không đổi yêu cầu, hành vi hay tiêu chí**, nên không làm vô hiệu các bước sau | Rule explain-terms mới |
| 2026-10-22 | Cập nhật mục 15 Câu hỏi mở theo mẫu mới: Q-02, Q-03, Q-11, Q-12 chuyển `answered` kèm link tới nơi chốt; Q-13 bổ sung lựa chọn, khuyến nghị kèm lý do, hậu quả; không đổi yêu cầu | LL-005 |
| 2026-10-22 | Làm rõ vai trò gateway là đường vào công khai (mục 11) và nơi thực thi NFR-008 (rate limit ở gateway theo `api-endpoints.md`); không đổi số, hành vi hay tiêu chí | Người dùng phát hiện design bỏ qua gateway (design Confirm log #12) |

## 19. Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | `spec-review:identity-auth:register-enumeration` | BR-003 (không lộ email) mâu thuẫn Q-01 (đăng ký trùng trả lỗi trùng) | Trả lỗi trùng, ghi là đánh đổi | 1 | 2026-10-21 |
| 2 | `spec-review:identity-auth:lockout-disclosure` | Khoá tạm: lỗi riêng hay chung; chống khoá-hại-người-khác | Lỗi chung 401 + Retry-After, khoá theo cặp (email, IP) | 1 | 2026-10-21 |
| 3 | `spec-review:identity-auth:compliance-pii` | `quality.compliance` TODO, email là PII | none: dự án học, không dữ liệu thật | 1 | 2026-10-21 |
| 4 | `spec-review:identity-auth:password-change-delete` | Đổi mật khẩu, xoá tài khoản trong hay ngoài phạm vi | Thêm đổi mật khẩu (Should), xoá tài khoản ngoài phạm vi | 1 | 2026-10-21 |
| 5 | `spec-review:identity-auth:fr006-who-verifies` | Ai hiện thực việc kiểm tra token | "Cách nào là best practice hoặc các dự án thực tế sử dụng hãy follow". Áp dụng: mỗi resource server tự kiểm cục bộ qua JWKS; nơi đặt bộ kiểm tra chốt ở design (Q-11) | 1 | 2026-10-21 |
| 6 | `spec-review:identity-auth:client-registration` | Cách đăng ký client | Seed từ cấu hình/migration, không có API | 1 | 2026-10-21 |
| 7 | `spec-review:identity-auth:revocation-introspection` | A-07/Q-05 thu hồi access token, ai gọi introspection | Chấp nhận; introspection tuỳ chọn, gọi bằng token máy scope `introspect` | 1 | 2026-10-21 |
| 8 | `spec-review:identity-auth:refresh-race` | Đổi cùng refresh token đồng thời (Q-07) | Không dung sai, thu hồi ngay | 1 | 2026-10-21 |
| 9 | `spec-review:identity-auth:rate-limit` | Số giới hạn tốc độ cho endpoint công khai | 10 request/phút/IP, vượt trả 429 | 1 | 2026-10-21 |
| 10 | `spec-review:identity-auth:scale-sla` | A-05 và yêu cầu sẵn sàng/khôi phục | Xác nhận A-05; không SLA, mất DB dev chấp nhận được | 1 | 2026-10-21 |
| 11 | `spec-review:identity-auth:signing-rotation` | Q-02, Q-03 là quyết định kỹ thuật | Hoãn sang design/ADR; spec bỏ giá trị cụ thể | 1 | 2026-10-21 |
| 12 | `spec-review:identity-auth:defaults-q08-q10` | Q-08, Q-09, Q-10, A-08 | Chấp nhận cả bốn mặc định | 1 | 2026-10-21 |
| 13 | `system-design:identity-auth:frontend-scope` | Trang đăng nhập I5: Razor, HTML tĩnh hay hoãn (FR-019) | Nguyên văn: "Tạo FE angular (trước đó đã gỡ giờ bổ sung)"; "angular real-world project (học thêm angular)". **Đổi phạm vi chưa xử lý:** trái ADR 0008 và mục "Ngoài phạm vi: Frontend thật"; cần spec FE riêng + ADR supersede ADR 0008; FR-019 và mục 4, 13 sẽ cập nhật sau, rồi chạy lại `spec-review` | design | 2026-10-21 |
| 14 | `spec-update:identity-auth:login-form-location` | Form email/mật khẩu của luồng Authorization Code nằm ở đâu sau khi chọn Angular FE (FR-019) | Trên origin của `identity-service` (khuyến nghị), SPA của client không nhận mật khẩu. FR-019, A-06, mục 4, 8.1, 13, 16, 17 cập nhật; thêm NFR-010 và Q-12; `status` về `draft` chờ `spec-review` lại | cập nhật | 2026-10-21 |
| 15 | `spec-review:identity-auth:idp-session-logout` | Cookie phiên trên identity sau đăng xuất; thời hạn "vài chục phút" chưa có số (Q-12, FR-011, FR-019) | 30 phút không hoạt động; đăng xuất thu hồi refresh token **và** huỷ phiên cookie; đăng xuất OIDC RP-initiated vẫn ngoài phạm vi | 3 | 2026-10-21 |
| 16 | `spec-review:identity-auth:login-ui-boundary` | Ai làm giao diện trang đăng nhập, mã nằm ở đâu | Nguyên văn: "Lần sau spec sẽ phải khai phá cho cả FE luôn. Hiện tại cần bổ sung spec FE". Áp dụng: giao diện trang đăng nhập thuộc spec FE (khai phá riêng, `requirements-discovery`); spec này chỉ yêu cầu hành vi phía identity; I5 phụ thuộc spec FE (Q-13) | 3 | 2026-10-21 |

## 20. Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-21 | Toàn bộ checklist A–G | 12 (đã hỏi 3 lô, đã chốt hết) | 2 (đã sửa: câu thước đo mục 2, bảng Confirm log/Review rounds) | NEEDS_CONFIRM → đã sửa spec |
| 2 | 2026-10-21 | Delta: FR-001/003/006/008/009/014/021/022, BR-003, NFR-008/009, mục 8–11, 14–17; kiểm tái phát các điểm cũ | 0 | 0 (đã sửa tham chiếu Q-05 cũ ở mục 11) | PASS |
| 3 | 2026-10-21 | Delta sau đổi phạm vi FE (ADR 0009): FR-011, FR-019, NFR-010, A-03, A-06, Q-12, Q-13, mục 3, 4, 8.1, 13, 16, 17; kiểm tái phát các điểm cũ | 0 (2 CONFIRM đã chốt: Confirm log #15, #16; 2 FIX đã sửa: FR-019 bỏ giải pháp kỹ thuật, thêm NFR-008/ví dụ lỗi) | 0 | PASS (I5 phụ thuộc spec FE, Q-13) |
