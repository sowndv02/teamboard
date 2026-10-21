# Identity: lộ trình tự xây `identity-service` (để hiểu sâu)

Mục tiêu học: tự xây các chức năng xác thực/ủy quyền thay vì dùng framework có sẵn, để hiểu **JWT, OAuth 2.0, OpenID Connect** từ bên trong. Đây là quyết định `[Chọn để học]` (ADR 0006). Kiến trúc service: Vertical Slice (`PROJECT.md`), PostgreSQL.

**Nguồn:** slide "Securing Microservices with IdentityServer4 and Ocelot" (xem `references.md`; slide chỉ là dàn ý, không có chi tiết cài đặt), cộng các đặc tả công khai (RFC, OpenID Connect Core). Các RFC/chuẩn dưới đây là điều tôi nhớ; **hãy đọc bản gốc** (`Chắc` về sự tồn tại, kiểm tra số hiệu và trạng thái hiện hành).

## 0. Hai điều cần nói trước

1. **IdentityServer4 đã hết hỗ trợ**: người duy trì chuyển sang sản phẩm thương mại kế nhiệm (Duende IdentityServer, có giấy phép). Slide dùng IS4 để dạy khái niệm; hãy học **khái niệm**, đừng sao chép công nghệ. (Kiểm tra tình trạng hiện hành.)
2. **Tự xây để học ≠ tự xây để chạy sản phẩm.** Mật mã và giao thức xác thực rất dễ sai lỗi tinh vi. Quy tắc cho bài học:
   - **Được tự viết:** logic phát/kiểm tra token, các luồng OAuth/OIDC, quản lý người dùng/phiên, ủy quyền theo claim/policy.
   - **Không tự viết thuật toán mật mã.** Dùng nguyên thuỷ của .NET BCL (`System.Security.Cryptography`: RSA/ECDSA, HMAC, `Rfc2898DeriveBytes.Pbkdf2`, `RandomNumberGenerator`, `CryptographicOperations.FixedTimeEquals`), **không** tự cài RSA/SHA/AES.
   - **Không dùng** framework identity trọn gói (IdentityServer, OpenIddict, Keycloak...) cho phần cần học. Có thể dùng chúng **làm chuẩn đối chiếu** (so hành vi) hoặc để học cách một sản phẩm thật làm.
   - Ghi rõ trong ADR: nếu thành sản phẩm thật thì thay bằng giải pháp đã kiểm chứng (hoặc Cognito: `aws-service-selection`).

## 1. Khái niệm cần nắm (theo thứ tự học)

| Chủ đề | Nội dung | Chuẩn/đọc thêm |
|---|---|---|
| **JWT** | Cấu trúc header.payload.signature (Base64URL), claim đăng ký (`iss`, `sub`, `aud`, `exp`, `nbf`, `iat`, `jti`), ký (JWS) so với mã hoá (JWE) | RFC 7519 (JWT), RFC 7515 (JWS), RFC 7517 (JWK) |
| **OAuth 2.0** | Vai trò (resource owner, client, authorization server, resource server), loại client (confidential/public), scope, các grant | RFC 6749; **OAuth 2.0 Security Best Current Practice** (đọc bản mới nhất, kiểm số RFC); OAuth 2.1 (dự thảo) |
| **Authorization Code + PKCE** | Luồng chuẩn cho ứng dụng có giao diện đăng nhập (web/mobile); PKCE chống chặn mã | RFC 7636 |
| **Client Credentials** | Service-to-service (máy với máy) | RFC 6749 mục 4.4 |
| **Refresh token** | Phiên dài hạn, xoay vòng (rotation), phát hiện tái sử dụng | RFC 6749; BCP |
| **OpenID Connect** | Lớp xác thực trên OAuth 2.0: `id_token`, `userinfo`, scope `openid`, discovery, JWKS, `nonce` | OpenID Connect Core 1.0, Discovery 1.0 |
| **Ủy quyền** | Role-based, claim-based, policy-based; scope ≠ quyền người dùng | Tài liệu ASP.NET Core authorization |
| **Thu hồi và introspection** | Làm mất hiệu lực token, kiểm tra token mờ | RFC 7009, RFC 7662; JWT access token profile (RFC 9068, kiểm tra) |

**Về các luồng trong slide:** slide nêu Implicit, Resource Owner Password, Hybrid. Chúng là phần của lịch sử giao thức: **Implicit và Resource Owner Password Credentials không còn được khuyến nghị** trong hướng dẫn bảo mật hiện hành (OAuth 2.0 Security BCP, OAuth 2.1). Hãy **học để hiểu vì sao bị bỏ** (đọc luồng, thử tấn công trong lab), nhưng **xây trước Authorization Code + PKCE và Client Credentials**. Hybrid flow chỉ xây nếu muốn hiểu đầy đủ OIDC.

## 2. Phạm vi `identity-service` (từng giai đoạn học)

Mỗi giai đoạn là một lát (issue riêng, qua `workflow`, có spec và kịch bản BDD). Mỗi lát nêu rõ **cái tự viết** và **cái dùng BCL**.

| GĐ | Mục tiêu | Tự viết | Dùng BCL | Kiểm chứng (BDD/TDD) |
|---|---|---|---|---|
| **I1** | Người dùng và mật khẩu | Đăng ký/đăng nhập, quy tắc mật khẩu, khoá tạm khi sai nhiều lần | `Rfc2898DeriveBytes.Pbkdf2` (băm), `RandomNumberGenerator` (salt), `FixedTimeEquals` | Hash không lưu mật khẩu rõ; so sánh hằng thời gian; khoá tạm; **không lộ người dùng tồn tại hay không** |
| **I2** | Phát và kiểm tra JWT | Dựng JWT (header/payload), ký, parse, kiểm tra chữ ký, `exp/nbf/aud/iss`, từ chối `alg` lạ | RSA/ECDSA ký-xác minh, Base64Url, `System.Text.Json` | Token hợp lệ chấp nhận; sửa payload/`alg=none`/hết hạn/sai `aud` bị từ chối; **kiểm tra bắt buộc thuật toán mong đợi** |
| **I3** | Khoá và JWKS | Khoá ký có `kid`, xuất JWKS (`/.well-known/jwks.json`), xoay khoá (giữ khoá cũ để xác minh token cũ) | Sinh khoá RSA/ECDSA | Gateway/service xác minh theo `kid`; xoay khoá không làm hỏng token còn hạn |
| **I4** | Refresh token | Lưu băm refresh token, xoay vòng mỗi lần dùng, phát hiện dùng lại (thu hồi cả chuỗi) | `RandomNumberGenerator`, băm SHA-256 | Dùng lại token cũ bị phát hiện; hết hạn; thu hồi khi đăng xuất |
| **I5** | OAuth 2.0 Authorization Code + PKCE | Endpoint `/connect/authorize` và `/connect/token`; đăng ký client; kiểm tra `redirect_uri` chính xác, `state`, `code_challenge`; mã dùng một lần, ngắn hạn | SHA-256 | Sai `redirect_uri`, thiếu/sai `code_verifier`, tái sử dụng `code`, thiếu `state` đều bị từ chối |
| **I6** | Client Credentials | Phát token cho service-to-service theo `client_id/secret`, scope theo service | HMAC/hash so sánh bí mật | Scope vượt quyền bị từ chối; token máy không dùng như token người |
| **I7** | OpenID Connect | `id_token`, `nonce`, `/connect/userinfo`, `/.well-known/openid-configuration`, scope `openid/profile/email`, đăng xuất | – | Discovery khớp endpoint thật; `id_token` kiểm tra `aud/nonce`; claim theo scope |
| **I8** | Ủy quyền | Role/claim/policy; thu hồi/introspection tối thiểu; kiểm quyền theo tài nguyên ở từng service | Policy của ASP.NET Core (được dùng) | IDOR không xảy ra; quyền đúng theo vai trò; token hết hiệu lực khi thu hồi |
| **I9** | Tích hợp hệ thống | Gateway kiểm JWT (YARP) và chuyển tiếp; service-to-service dùng Client Credentials; thử bằng công cụ gọi API (Postman, curl), không có frontend | – | Luồng đầu-cuối: đăng nhập → gọi API qua gateway → service tự kiểm quyền |

**Chưa có frontend ở giai đoạn này (ADR 0008).** `identity-service` chỉ cung cấp API. I1–I4, I6, I8 không cần giao diện. Authorization Code + PKCE (I5) và OIDC (I7) cần một "user agent" để người dùng đăng nhập; khi tới đó có hai lựa chọn: (a) làm một trang đăng nhập HTML tối giản do chính `identity-service` phục vụ, (b) hoãn I5 và I7 đến khi có frontend (cần quyết định riêng).

## 3. Điểm bảo mật phải làm đúng (mỗi điểm = một kịch bản BDD + một test tấn công)

| Rủi ro | Biện pháp |
|---|---|
| Lưu mật khẩu | Băm có salt bằng thuật toán chậm (PBKDF2 từ BCL, số vòng theo OWASP Password Storage Cheat Sheet hiện hành); không lưu rõ; không log mật khẩu |
| So sánh bí mật | So sánh hằng thời gian (`FixedTimeEquals`) |
| Giả mạo token | Kiểm chữ ký bằng **thuật toán đã cấu hình** (không tin `alg` trong header), từ chối `none`, kiểm `iss/aud/exp/nbf` |
| Tráo khoá (key confusion) | Gắn `kid` với khoá và thuật toán cụ thể; không dùng khoá công khai làm khoá HMAC |
| Chặn mã uỷ quyền | PKCE bắt buộc cho public client; mã dùng một lần, hết hạn nhanh, gắn với client và `redirect_uri` |
| Chuyển hướng độc hại | So khớp `redirect_uri` **chính xác** với danh sách đăng ký (không dùng wildcard/prefix) |
| CSRF/replay đăng nhập | `state` (OAuth), `nonce` (OIDC), kiểm tra lúc quay lại |
| Lộ refresh token | Xoay vòng + phát hiện tái sử dụng; lưu băm phía server; thời hạn hợp lý |
| Brute force | Giới hạn tốc độ, khoá tạm, thông báo lỗi chung (không phân biệt "sai tên" hay "sai mật khẩu") |
| Lộ thông tin qua lỗi/log | Lỗi theo `problem+json` (`api-endpoints.md`); không log token/mật khẩu |
| Khoá ký bị lộ | Lưu khoá trong Secrets Manager/KMS (không trong repo), xoay vòng, quyền tối thiểu (`terraform.md` mục 3) |
| Phiên bản và quyền | Scope tối thiểu; token ngắn hạn (access token vài phút); kiểm quyền ở **từng** service, không chỉ ở gateway |

**Học bằng tấn công trong lab của chính bạn** (an toàn, trên `localhost`/sandbox của mình): thử `alg=none`, sửa claim, tái sử dụng refresh token, `redirect_uri` giả, bỏ PKCE, replay `code`; viết test mô phỏng và thấy hệ thống chặn. Không thử trên hệ thống của người khác.

## 4. Đối chiếu với slide

| Nội dung slide (IS4 + Ocelot) | Trong lộ trình này |
|---|---|
| JWT, OAuth2, OIDC (khái niệm) | Mục 1 và GĐ I2 đến I7 |
| Authorization Code, Implicit, Hybrid, Client Credentials, ROPC | Code + PKCE và Client Credentials xây; Implicit/ROPC/Hybrid chỉ để hiểu và tấn công thử |
| Protect API bằng JWT từ IS4 | GĐ I2, I9 (tự viết bộ kiểm tra token) |
| MVC client OpenID Connect, HttpClientFactory gọi API có token | Hoãn cùng frontend (ADR 0008); trước mắt gọi API có token bằng công cụ như Postman/curl hoặc test tích hợp |
| Claim-based authorization | GĐ I8 |
| Ocelot mang token qua gateway | YARP kiểm JWT và chuyển tiếp (GĐ I9) |
| `TestUsers`, `DeveloperSigningCredential` (dữ liệu/khoá giả) | Chỉ dùng cho dev; khoá thật từ Secrets Manager, người dùng thật trong PostgreSQL |

## 5. Kiểm chứng và quan sát
- Mỗi giai đoạn: spec → kịch bản BDD (gồm đường lỗi/bảo mật) → TDD → review (`test-review.md`, lăng kính bảo mật).
- Metric/log bảo mật (đăng nhập thất bại, khoá tạm, tái sử dụng refresh token, token bị từ chối) đưa vào `observability.md` (không log bí mật).
- Chi phí: tự xây trên Fargate + PostgreSQL có chi phí cố định; Cognito trả theo MAU. Ghi so sánh trong `document/infra/identity-service.md` (`aws-service-selection`).

## 6. Việc cần chốt
- Khi tới I5/I7: làm trang đăng nhập HTML tối giản hay hoãn đến khi có frontend (`TODO`).
- Thuật toán ký mặc định (RSA hay ECDSA) và chính sách xoay khoá.
- Có làm hybrid/implicit để học hay chỉ đọc lý thuyết.
