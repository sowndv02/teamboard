# Part 1 — Fundamentals & Mental Models (Identity)

> [!NOTE]
> **Mục tiêu:** hiểu định danh, xác thực, ủy quyền là ba việc khác nhau; session cookie so với token; cấu trúc JWT và vì sao nó chỉ được ký chứ không được mã hoá; và chọn có tự xây hay không.
> **Cần biết trước:** [Part 0](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** giải mã một JWT bằng tay và nói điều gì đọc được mà không cần khoá; chọn giải pháp từ Bản đồ cho 3 tình huống.
> [← Part 0](README.md) · [Part 2 →](part-2-core-skills.md)

## L1 What — Các khái niệm nền

#### Định danh (identity)
**Là gì:** một thực thể được hệ thống nhận biết (người dùng, dịch vụ, thiết bị), thường bằng một mã ổn định (`sub`).
**Ví dụ đời thường:** số căn cước của bạn.
**Use case:** `user u1` sở hữu Board 7; `svc-a` là một dịch vụ gọi API.

#### Xác thực (authentication)
**Là gì:** chứng minh "bạn là ai" (mật khẩu, mã OTP, khoá, chứng chỉ).
**Ví dụ đời thường:** đưa căn cước ở quầy lễ tân để họ biết đúng là bạn.
**Use case:** đăng nhập bằng email và mật khẩu.

#### Ủy quyền (authorization)
**Là gì:** quyết định "bạn được làm gì" sau khi biết bạn là ai (vai trò, scope, quyền trên từng tài nguyên).
**Ví dụ đời thường:** lễ tân biết bạn là ai nhưng chỉ cho bạn lên tầng 5.
**Use case:** thành viên Board được sửa Task, khách chỉ được xem.

#### Session và token
**Session:** server nhớ trạng thái đăng nhập, trình duyệt giữ một mã mờ (cookie). **Token:** mã tự chứa thông tin và chữ ký, nơi nhận kiểm được mà không cần hỏi lại nơi cấp.
**Use case:** web truyền thống dùng session cookie; API nhiều dịch vụ dùng token.

#### Access token, refresh token, ID token
**Access token:** gửi kèm mỗi lời gọi API, sống ngắn. **Refresh token:** đổi lấy access token mới, sống dài, lưu kín. **ID token** (OIDC): nói "ai vừa đăng nhập" cho ứng dụng client, không dùng để gọi API.

#### JWT (JSON Web Token)
**Là gì:** định dạng token gồm ba phần `header.payload.signature`, mỗi phần mã hoá Base64URL. Nó chỉ là **một định dạng**: JWT không tự là xác thực.
**Use case:** access token của dự án (I2).

## L2 Why — Vì sao tách và vì sao khó

Nếu mỗi dịch vụ tự lưu mật khẩu và tự đăng nhập thì có sáu bản mật khẩu, sáu chỗ để sai và người dùng đăng nhập sáu lần. Tập trung xác thực vào **một nơi** (identity) rồi các dịch vụ kiểm **token** nó cấp giải được cả ba, nhưng đổi lại: nơi đó thành mục tiêu tấn công lớn nhất, và token đã phát đi thì **khó thu hồi** (Part 4). Phần khó nằm ở chi tiết: sai một điều kiện kiểm tra (không pin thuật toán, bỏ qua `aud`) là mở cửa (Part 5).

## L3 How — Cơ chế đáng nhớ

### Session cookie so với token

| Tiêu chí | Session cookie | Token (JWT) |
|---|---|---|
| Trạng thái | server giữ (bộ nhớ, DB, Redis) | tự chứa; server không cần nhớ |
| Thu hồi | xoá phiên là xong, tức thì | khó: token còn hạn thì còn dùng được |
| Nhiều dịch vụ | mỗi dịch vụ phải hỏi nơi giữ phiên | mỗi dịch vụ tự kiểm bằng khoá công khai |
| Trình duyệt | cookie tự gửi (cần chống CSRF) | tự gắn header (cần giữ token an toàn) |
| Rủi ro | CSRF, cố định phiên | lộ token qua XSS, thu hồi chậm |
| Hợp với | ứng dụng web một miền | API, service-to-service, di động |

Với ứng dụng web chạy trong trình duyệt, khuyến nghị của IETF (bản nháp "OAuth 2.0 for Browser-Based Applications", sau là RFC 10017, chưa tự mở số RFC) là mẫu **BFF**: backend giữ token trong phiên cookie, trình duyệt không bao giờ thấy token (⚠ qua agent).

### Cấu trúc JWT
```
BASE64URL(header) . BASE64URL(payload) . BASE64URL(signature)
header   : {"alg":"RS256","typ":"JWT","kid":"k1"}
payload  : {"iss":"https://id.lab","sub":"u1","aud":"boards-api","iat":..,"exp":..,"role":"member"}
signature: ký trên "header.payload" bằng khoá bí mật của nơi cấp
```
- **Claim đã đăng ký (RFC 7519):** `iss` (ai cấp), `sub` (về ai), `aud` (dành cho ai), `exp` (hết hạn), `nbf` (chưa có hiệu lực trước), `iat` (cấp lúc), `jti` (mã duy nhất). Tất cả **tuỳ chọn** theo RFC: bên kiểm phải **tự yêu cầu** những cái mình cần (`exp`, `aud`...), nếu không thì thiếu cũng qua.
- **JWS so với JWE:** JWT phổ biến là JWS (ký, ai cũng đọc được payload); JWE mới là mã hoá. Đừng đặt dữ liệu nhạy cảm vào payload JWS.
- **Thuật toán:** HS256 (HMAC, một khoá bí mật chung cho cả ký và kiểm), RS256 (RSA, khoá riêng ký, khoá công khai kiểm), ES256 (ECDSA). RFC 7518: HS256 bắt buộc hỗ trợ, RS256 khuyến nghị, ES256 "khuyến nghị+". Khoá HMAC phải dài ít nhất bằng đầu ra của hàm băm (256 bit cho HS256); khoá RSA ít nhất 2048 bit.
- **Chọn bất đối xứng (RS256, ES256)** khi nhiều dịch vụ cần **kiểm** token mà không được phép **ký**: họ chỉ có khoá công khai, nên một dịch vụ bị chiếm không thể làm giả token.

### Hai điều cần nhớ về token
1. **Ký không phải mã hoá:** payload đọc được bằng Base64URL, không cần khoá (Lab 1.1).
2. **Kiểm token là việc của bên nhận,** và phải kiểm **mọi** thứ: chữ ký bằng thuật toán đã cấu hình, `iss`, `aud`, `exp`, `nbf` (Part 2).

## L4 Hands-on — Lab Part 1

Môi trường: như Part 0. Khoá HS256 sinh ngẫu nhiên lúc chạy, không in ra.

#### Lab 1.1 — Giải mã một JWT bằng tay
**Đầu ra thật:**
```text
số phần: 3; độ dài từng phần (ký tự): 51, 139, 43
header : {"alg":"HS256","typ":"JWT","kid":null}
payload: iss=https://id.lab, sub=u1, aud=boards-api, iat=1792864460, exp=1792864760, role=member
chữ ký: 32 byte (HMAC-SHA256 luôn 32 byte)
-> payload đọc được bằng Base64URL mà KHÔNG cần khoá: JWT được ký chứ không được mã hoá
```
**Giải thích:** ba phần đúng cấu trúc; `exp − iat = 300` giây (token sống 5 phút); payload đọc được ngay, chữ ký là 32 byte. (`"kid":null` xuất hiện vì bộ tuần tự hoá của lab in cả trường rỗng; token thật thường bỏ trường này.)

## L7 Architecture — Chọn giải pháp, và khi KHÔNG nên tự xây

### Bản đồ giải pháp

Xác thực và định danh là một **vai trò**; có nhiều cách đảm nhiệm. Tự xây trên BCL (như dự án) chỉ là **một dòng**.

| Họ / cách làm | Ví dụ | Khi chọn | Cái giá |
|---|---|---|---|
| Tự xây trên BCL (để học) | `identity-service` của dự án | muốn hiểu giao thức từ bên trong | dễ sai lỗi tinh vi; công sức; không đủ tin cậy cho sản phẩm thật nếu không rà soát |
| Dịch vụ quản lý | Amazon Cognito, Entra ID, Auth0 | giao sản phẩm nhanh, ít vận hành, tính năng bảo mật sẵn | gắn nhà cung cấp, chi phí theo người dùng, tuỳ biến hạn chế |
| Máy chủ mã nguồn mở tự lưu trữ | Keycloak (Apache-2.0) | cần SAML, federation, MFA đầy đủ, không muốn trả theo MAU | nặng, chi phí chạy rỗng, che giấu cơ chế |
| Thư viện máy chủ cho .NET | OpenIddict (Apache-2.0), Duende IdentityServer | tự dựng nhưng không tự viết giao thức | Duende có giấy phép thương mại; IdentityServer4 đã hết hỗ trợ |
| Session cookie phía server | ASP.NET Core Identity với cookie | một ứng dụng web một miền | thu hồi dễ nhưng khó dùng cho nhiều dịch vụ; cần chống CSRF |
| API key | khoá cố định cho đối tác | máy gọi máy đơn giản, ít người gọi | khó xoay, không có danh tính người dùng, dễ lộ |
| mTLS | chứng chỉ khách giữa dịch vụ | service-to-service trong mạng kín | quản lý chứng chỉ phức tạp |
| **Không xác thực** | công khai hoàn toàn, hoặc mạng kín tin cậy | dữ liệu công khai; nguyên mẫu nội bộ | không phù hợp bất kỳ dữ liệu riêng nào |

### Khi KHÔNG nên tự xây hệ định danh
- Có **người dùng thật hay dữ liệu thật** (ADR 0006: đó là tín hiệu xem lại).
- Cần SAML, federation, MFA, passkeys: tự xây quá tốn công và rủi ro.
- Đội không có người rà soát bảo mật độc lập.
- Cần tuân thủ (SOC 2, GDPR chi tiết...): dịch vụ quản lý đã làm sẵn.

**Cách chọn:** hỏi (1) mục tiêu là học hay giao sản phẩm, (2) ai là người dùng (nhân viên, khách, máy), (3) cần federation, MFA, SAML không, (4) ngân sách theo người dùng và chi phí chạy rỗng, (5) ai rà soát. Ở dự án này: tự xây để học; **nếu thành sản phẩm thật thì dùng Cognito hoặc giải pháp đã kiểm chứng**.

## Tự kiểm tra

1. Xác thực và ủy quyền khác nhau thế nào? Cho một ví dụ mỗi loại trong TeamBoard.
2. Vì sao JWT "chỉ được ký chứ không được mã hoá", và hệ quả là gì?
3. Khi nào chọn RS256 thay HS256?
4. Nêu hai tín hiệu cho thấy nên ngừng tự xây và chuyển sang giải pháp quản lý.

<details>
<summary>Đáp án ngắn</summary>

1. Xác thực: chứng minh là u1 bằng mật khẩu; ủy quyền: u1 được sửa Task của Board 7.
2. Payload chỉ Base64URL (Lab 1.1) nên ai cũng đọc được; không đặt dữ liệu nhạy cảm vào.
3. Khi nhiều dịch vụ chỉ cần kiểm mà không được ký (khoá công khai, khoá riêng giữ ở nơi cấp).
4. Có người dùng hay dữ liệu thật; cần SAML, MFA hay federation; không có người rà soát độc lập.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 1.1 (.NET 10, khoá HS256 sinh lúc chạy; đầu ra thật).
**Đã tự mở:** [ADR 0006](../../architecture/adr/0006-identity-self-built.md), [identity-learning.md](../../architecture/identity-learning.md) của dự án.
**Chỉ qua agent (⚠):** RFC 7519, 7515, 7517, 7518 (claim đăng ký đều tuỳ chọn; HS256 bắt buộc, RS256 khuyến nghị; độ dài khoá); bản nháp IETF "OAuth 2.0 for Browser-Based Applications" (BFF; số RFC 10017 chưa tự xác nhận); Amazon Cognito, Keycloak (Apache-2.0, CNCF incubating), Duende (giấy phép thương mại), IdentityServer4 (lưu trữ 06/03/2025), OpenIddict (Apache-2.0), Auth0, Entra ID (trang sản phẩm).
**Chưa kiểm:** so sánh session cookie đầy đủ (CSRF, `SameSite`) chưa chạy; chức năng cụ thể của từng sản phẩm quản lý chưa dùng thử.
