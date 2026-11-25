# Bản đồ các phương thức xác thực

> Loại: Khái niệm · Đi kèm lộ trình [Identity](README.md) (bổ sung phần các Part 0–7 chưa phủ)
> Làm mới lần cuối: 2026-11-25 · Phiên bản liên quan: theo từng RFC/spec ghi ở mỗi mục (mức xác minh: **Chắc** = tôi tự mở nguồn; **Vừa** = chỉ agent nghiên cứu mở và đọc; **Chưa kiểm** = chưa mở được). Khung đối chiếu: roadmap "Authentication Standards 10–15 giờ" (11/2026), không nằm trong repo.

## 1. Vấn đề nó giải quyết

Người mới thường nghĩ "xác thực" là ô nhập email và mật khẩu, rồi gặp một loạt tên khác trong dự án thật: người dùng đăng nhập bằng Google, nhân viên vào hệ thống bằng tài khoản công ty (SSO), quản trị viên phải nhập thêm mã 6 số, máy chủ gọi API bằng một chuỗi khoá, laptop trong công ty tự đăng nhập vào máy chủ file không hỏi mật khẩu, TV hiện một mã để nhập trên điện thoại. Đó là **các phương thức xác thực khác nhau**, mỗi cái sinh ra cho một ngữ cảnh. Nếu chỉ biết mật khẩu và JWT (Part 1–4) thì khi gặp SAML, Kerberos hay passkey bạn không biết chúng cùng loại hay khác loại với thứ đã học, cũng không biết khi nào nên chọn cái nào.

Ghi chú này là **bản đồ**: mỗi phương thức một mục ngắn (là gì, khi nào dùng, rủi ro chính, nguồn), rồi cách chọn theo loại ứng dụng. Nó không dạy cài đặt từng cái.

## 2. Là gì (hiểu trong 1 phút)

**Xác thực (authentication)** là chứng minh "tôi là ai" bằng một hay nhiều **bằng chứng**. Các phương thức khác nhau ở ba điểm: bằng chứng là gì, **ai** kiểm bằng chứng, và bằng chứng **đi** qua kênh nào.

```
 bằng chứng            ai kiểm                    kênh mang kết quả
 ─────────────         ────────────────────       ───────────────────────────
 mật khẩu, OTP    →    chính ứng dụng        →    session cookie / header
 passkey          →    ứng dụng (khoá công khai)  session cookie / token
 tài khoản công ty →   IdP (nơi khác)        →    assertion SAML / ID token OIDC
 tài khoản domain →    KDC / directory       →    ticket Kerberos / LDAP bind
 chứng chỉ        →    TLS + PKI             →    kết nối mTLS
 khoá cố định     →    ứng dụng              →    header (API key)
```

Hai điều dễ nhầm: **OAuth 2.0 là uỷ quyền, không phải xác thực** (OIDC mới là lớp xác thực đặt trên nó), và **JWT chỉ là định dạng token**, không phải phương thức (Part 1).

## 3. Khái niệm cốt lõi

| Khái niệm | Nghĩa | Ví dụ ngắn |
|---|---|---|
| Yếu tố xác thực (factor) | Loại bằng chứng: **biết** (mật khẩu), **có** (điện thoại, khoá bảo mật), **là** (vân tay) | Mật khẩu cộng mã trên điện thoại là hai yếu tố |
| MFA | Dùng ít nhất hai yếu tố **khác loại**; hai mật khẩu không phải MFA | Mật khẩu cộng TOTP |
| IdP / SP (SAML), OP / RP (OIDC) | Bên **xác thực người dùng** và bên **tin kết quả đó** để cho vào | Google là IdP, ứng dụng của bạn là SP hoặc RP |
| Assertion | Văn bản (XML) do IdP ký, nói "người này đã xác thực lúc nào, bằng cách nào" | Phản hồi SAML sau khi đăng nhập |
| Bind (LDAP) | Thao tác xác thực với máy chủ thư mục bằng tên và mật khẩu hoặc cơ chế SASL | Ứng dụng đăng nhập hộ người dùng vào AD |
| Ticket (Kerberos) | Vé do KDC cấp, chứng minh danh tính mà không gửi lại mật khẩu | Laptop domain mở máy chủ file không hỏi mật khẩu |
| Authenticator | Thiết bị hoặc phần mềm giữ khoá để xác thực | Khoá bảo mật, điện thoại, ứng dụng TOTP |
| RP ID (WebAuthn) | Tên miền của website mà passkey bị **ràng buộc** | Passkey tạo cho `example.com` không dùng được ở trang giả mạo |
| Phishing-resistant | Phương thức mà kẻ lừa người dùng nhập thông tin vào trang giả vẫn không lấy được thứ dùng lại | Passkey có, mã SMS không |
| Sender-constrained token | Token chỉ dùng được bởi bên **giữ khoá** đã gắn với nó, không phải "ai cầm cũng dùng" (bearer) | DPoP, token gắn chứng chỉ mTLS |
| Provisioning | Tạo, sửa, khoá tài khoản ở hệ thống đích khi nhân viên vào, chuyển, nghỉ | SCIM |

### 3.1 Các cơ chế / biến thể chính

Mỗi mục: **Là gì · Dùng khi · Rủi ro · Nguồn**. Mục đánh `⚠` chưa được chạy thử trong lab.

#### Nhóm A. HTTP và API

##### A1. HTTP Basic
**Là gì:** client gửi `Authorization: Basic <base64(tên:mật khẩu)>` ở mọi request. Base64 chỉ là cách biểu diễn, **không phải mã hoá**. **Dùng khi:** công cụ nội bộ, tích hợp cũ, hoặc để gửi thông tin xác thực một lần tới endpoint token. **Rủi ro:** lộ mật khẩu nếu không có TLS; mật khẩu đi theo từng request; không có cách thu hồi riêng ngoài đổi mật khẩu. **Nguồn:** [RFC 7617](https://www.rfc-editor.org/rfc/rfc7617) (Standards Track, 2015) · Vừa.

##### A2. Bearer token
**Là gì:** `Authorization: Bearer <token>`; ai cầm token là dùng được. Có ba cách gửi (header, form body, query), nhưng chuẩn nói rõ nên **tránh query string** vì URL bị ghi log và lưu lịch sử. **Dùng khi:** gọi API bằng access token OAuth. **Rủi ro:** lộ token là mất quyền cho tới hết hạn; **phải có TLS**. **Nguồn:** [RFC 6750](https://www.rfc-editor.org/rfc/rfc6750) (2012, mục 2.3 và 5.3; sau đó được cập nhật bởi RFC 8996 và RFC 9700) · Vừa.

##### A3. HTTP Digest
**Là gì:** client trả lời thử thách (nonce) của server nên mật khẩu không đi dạng rõ; chuẩn đã thêm SHA-256 và SHA-512/256, MD5 còn để tương thích ngược nhưng không khuyến nghị. **Dùng khi:** hầu như không dùng cho hệ thống mới; gặp ở thiết bị cũ. **Rủi ro:** mật khẩu yếu bị dò từ điển, bị hạ cấp xuống cơ chế yếu hơn (phần này chỉ qua tóm tắt công cụ, **Chưa kiểm** nguyên văn). **Nguồn:** [RFC 7616](https://www.rfc-editor.org/rfc/rfc7616) (2015) · Vừa.

##### A4. `Authorization`, `WWW-Authenticate` và mã 401
**Là gì:** khi cần xác thực, server trả `401` kèm `WWW-Authenticate` báo scheme yêu cầu, ví dụ `Bearer error="invalid_token"`; client gửi lại với `Authorization`. Với bearer token, `invalid_request` trả `400`, `invalid_token` trả `401`, `insufficient_scope` trả `403`. **Dùng khi:** luôn, đọc header này là bước đầu khi gỡ lỗi 401. **Rủi ro:** trả lỗi chi tiết quá mức giúp kẻ tấn công thăm dò. **Nguồn:** RFC 6750 (đã mở, mã lỗi); [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110) mục 11 chỉ qua tóm tắt công cụ nên số mục con **Chưa kiểm** · Vừa.

##### A5. API key
**Là gì:** chuỗi bí mật gửi kèm request (nên ở header). Google Cloud mô tả **khoá chuẩn** gắn request với một **project** để tính chi phí và hạn mức, và khoá chuẩn **không xác định principal** nên IAM không kiểm quyền của người gọi; còn "authorization key" thì chạy như một service account. **Dùng khi:** API công khai đơn giản, đo hạn mức, tích hợp máy-máy quy mô nhỏ. **Không dùng khi:** hành động thay mặt người dùng, hoặc client công khai (SPA, mobile) vì khoá lộ ngay. **Rủi ro:** sống lâu, khó xoay; Google nói khoá không bị hạn chế là không an toàn. **Thực hành tốt:** giới hạn theo dịch vụ hoặc phương thức, chỉ lưu băm phía server, có hạn dùng và thu hồi. Không có "scope chuẩn" kiểu OAuth, nhưng nhiều nền tảng cho giới hạn theo API. **Nguồn:** [Google Cloud: API keys](https://docs.cloud.google.com/docs/authentication/api-keys) · Vừa; phần "chỉ lưu băm" là thực hành chung theo roadmap, **Chưa kiểm** nguồn riêng.

#### Nhóm B. Mật khẩu, yếu tố thứ hai và passwordless

##### B1. Mật khẩu (theo NIST SP 800-63B-4)
**Là gì:** yếu tố "biết". Theo bản 800-63B-4 (trang ghi 26/08/2025): mật khẩu **một yếu tố** tối thiểu **15 ký tự**, mật khẩu chỉ dùng trong MFA tối thiểu 8; cho phép tối thiểu 64 ký tự trở lên, nên chấp nhận Unicode và khoảng trắng; **không** ép quy tắc ký tự, **không** ép đổi định kỳ (chỉ đổi khi có bằng chứng lộ); phải so với danh sách mật khẩu đã lộ và phổ biến. OWASP đồng ý các điểm này. **Rủi ro:** mật khẩu không chống phishing, bị nhồi thông tin đã lộ ở nơi khác. **Nguồn:** [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html), [OWASP Authentication](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) · Vừa. Băm mật khẩu và khoá tạm đã có ở Part 2 và Part 5.

##### B2. TOTP và HOTP (mã dùng một lần)
**Là gì:** **HOTP** (RFC 4226) tính mã từ khoá chung và một **bộ đếm** tăng sau mỗi lần dùng; **TOTP** (RFC 6238) thay bộ đếm bằng **số bước thời gian** (mặc định 30 giây). Khoá nên ngẫu nhiên và dài; verifier chấp nhận lệch tối đa khoảng một bước, **không nhận lại mã đã dùng**. **Dùng khi:** yếu tố thứ hai không cần mạng di động. **Rủi ro:** bị lừa nhập mã vào trang giả rồi kẻ tấn công dùng lại ngay trong cửa sổ thời gian (OWASP: TOTP không chặn proxy phishing thời gian thực); lộ khoá gốc khi lưu không mã hoá; cần giới hạn số lần thử sai. **Nguồn:** [RFC 6238](https://www.rfc-editor.org/rfc/rfc6238) (Informational, 2011), [RFC 4226](https://www.rfc-editor.org/rfc/rfc4226) (Informational, 2005), [OWASP MFA](https://cheatsheetseries.owasp.org/cheatsheets/Multifactor_Authentication_Cheat_Sheet.html) · Vừa.

##### B3. OTP qua SMS hoặc email
**Là gì:** mã gửi qua kênh khác. **Rủi ro:** SMS bị đổi SIM, chuyển số, chặn tin; mã hiện trên màn hình khoá; kẻ tấn công gửi dồn để đốt tiền. NIST xếp SMS/thoại vào nhóm "hạn chế" và không cho email làm kênh ngoài băng tần (cả hai ý này chỉ qua tóm tắt công cụ, **Chưa kiểm** nguyên văn). **Dùng khi:** chỉ như phương án dự phòng. **Nguồn:** NIST SP 800-63B-4, OWASP MFA · Vừa.

##### B4. Push, number matching và MFA fatigue
**Là gì:** ứng dụng đẩy thông báo "bạn có đang đăng nhập không?". **MFA fatigue** (push bombing) là gửi thông báo liên tục tới khi người dùng bấm đồng ý cho xong. Biện pháp thường nêu: number matching (nhập số hiển thị trên màn hình đăng nhập), giới hạn tần suất push. OWASP có mục riêng cho MFA fatigue (chi tiết biện pháp chỉ qua tóm tắt công cụ). **Nguồn:** OWASP MFA · Vừa. Hướng dẫn "phishing-resistant MFA" của CISA **Chưa kiểm** (trang trả 403).

##### B5. Sinh trắc học
**Là gì:** yếu tố "là". Trong WebAuthn và passkey, sinh trắc học thường chỉ **mở khoá authenticator tại thiết bị**; server nhận chữ ký mật mã, không nhận vân tay hay ảnh mặt. **Rủi ro:** không đổi được nếu lộ, nên thường chỉ làm cách mở khoá cục bộ. **Nguồn:** FIDO Alliance (passkey mở bằng sinh trắc học, PIN hoặc hình mẫu) · Vừa.

##### B6. Xác thực lại (step-up) và "ghi nhớ đăng nhập"
**Là gì:** **step-up** là đòi xác thực lại hoặc yếu tố mạnh hơn trước thao tác nhạy cảm (đổi mật khẩu, đổi email, chuyển tiền). OWASP yêu cầu nhập lại mật khẩu hiện tại khi đổi thông tin nhạy cảm. **Remember me** nên dùng token dài hạn **riêng**, có hạn và thu hồi được, không phải kéo dài phiên vô hạn (theo roadmap, **Chưa kiểm** nguồn riêng). **Nguồn:** OWASP Authentication · Vừa.

##### B7. Passkey, FIDO2, WebAuthn, CTAP
**Là gì:** **FIDO2** gồm **WebAuthn** (API web để website dùng authenticator) và **CTAP** (giao thức giữa nền tảng và khoá bảo mật rời). Khi đăng ký, authenticator tạo cặp khoá cho từng website, **khoá riêng ở lại thiết bị**, website giữ khoá công khai; khi đăng nhập, website gửi challenge mới và kiểm chữ ký, origin và RP ID. **Passkey** là credential FIDO có thể **đồng bộ qua đám mây** (synced) hoặc **gắn một thiết bị** (device-bound, kể cả khoá phần cứng). **Dùng khi:** muốn thay hoặc giảm phụ thuộc mật khẩu, chống phishing. **Rủi ro:** khôi phục tài khoản khi mất thiết bị, mô hình tin cậy của passkey đồng bộ, người dùng khó chuyển hệ sinh thái; **user presence** (chạm) khác **user verification** (PIN hoặc sinh trắc), và OWASP yêu cầu user verification thì mới tính là yếu tố thứ hai. **Nguồn:** [FIDO Alliance: passkeys](https://fidoalliance.org/passkeys/), OWASP MFA · Vừa; chi tiết giao thức W3C WebAuthn **Chưa kiểm** (trang `webauthn-3` trả 403), phần CTAP theo roadmap.

#### Nhóm C. Session

##### C1. Session cookie và quản lý phiên
**Là gì:** server giữ trạng thái phiên, trình duyệt giữ **session ID** trong cookie. Theo OWASP: ID ngẫu nhiên từ bộ sinh số mật mã an toàn (ít nhất 64 bit entropy, tự tạo thì nên 128 bit trở lên), chỉ trao đổi qua cookie, không nhận ID từ URL (chống **session fixation**, tức kẻ tấn công ép nạn nhân dùng ID do hắn biết); cookie có `Secure`, `HttpOnly` và nên có `SameSite`; TLS cho cả phiên. Theo OWASP (qua tóm tắt công cụ, **Chưa kiểm** nguyên văn): có **idle timeout** và **absolute timeout**, đổi session ID sau đăng nhập và khi đổi quyền, đăng xuất phải huỷ phiên **phía server**. **Dùng khi:** ứng dụng web một miền. **Rủi ro:** CSRF, cố định phiên; khó dùng cho nhiều dịch vụ (Part 1). **Nguồn:** [OWASP Session Management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html) · Vừa. Lưu ý: RFC 6750 cấm để **bearer token OAuth** trong cookie có thể đi qua kênh không mã hoá, còn OWASP khuyên dùng cookie cho **session ID**: hai đối tượng khác nhau, không mâu thuẫn.

#### Nhóm D. Federation và danh tính liên kết

##### D1. SSO và federation
**Là gì:** **SSO** là đăng nhập một lần, vào nhiều ứng dụng; **federation** là các miền bảo mật tin kết quả xác thực của nhau (assertion hoặc token do bên kia cấp). SSO là kết quả, federation là cách thiết lập niềm tin; SAML và OIDC là hai giao thức phổ biến. **Nguồn:** roadmap (mục 1.2.D, **Chắc** vì tôi tự đọc roadmap), [SAML technical overview](https://docs.oasis-open.org/security/saml/Post2.0/sstc-saml-tech-overview-2.0.html) · Vừa.

##### D2. SAML 2.0
**Là gì:** giao thức XML giữa **IdP** (xác thực và cấp assertion) và **SP** (tin assertion để cho vào), gồm assertion, protocol, **binding** (cách chuyển thông điệp: HTTP Redirect, HTTP POST, Artifact, SOAP) và profile. **SP-initiated**: người dùng bắt đầu ở ứng dụng, bị chuyển sang IdP; **IdP-initiated**: bắt đầu ở IdP và gửi assertion tới SP không qua request trước. **Dùng khi:** SSO doanh nghiệp với phần mềm doanh nghiệp có sẵn, đối tác cần SAML. **Rủi ro:** XML signature phức tạp (nhiều lớp tấn công riêng), **khoá ký của IdP bị lấy thì kẻ tấn công tự ký assertion giả** (case ở mục 7). **Nguồn:** tài liệu OASIS trên ghi "Committee Draft 02, 25/03/2008" (thẻ title lại ghi OASIS Standard) · Vừa; luồng IdP-initiated chỉ qua tóm tắt công cụ.

##### D3. OpenID Connect (OIDC)
**Là gì:** "lớp danh tính" đặt trên OAuth 2.0, dùng JSON và **ID Token** (JWT ký). Claim bắt buộc: `iss`, `sub`, `aud`, `exp`, `iat`; `nonce` đi từ request vào ID Token để chống phát lại. **Discovery:** metadata ở `<issuer>/.well-known/openid-configuration`, và `issuer` trong đó phải trùng `iss` của ID Token. **Dùng khi:** đăng nhập cho web, mobile, SPA; mặc định chọn cho hệ thống mới. **Nguồn:** [OIDC Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html) (Final, errata set 2, 15/12/2023), [OIDC Discovery 1.0](https://openid.net/specs/openid-connect-discovery-1_0.html) · Vừa. Đào sâu: Part 4 và lát OIDC của dự án.

##### D4. Social login và account linking
**Là gì:** "Đăng nhập bằng Google" thực chất là OIDC với Google làm OP. **Định danh người dùng dùng `sub`, không dùng email**: tài liệu Google nói rõ `sub` duy nhất giữa mọi Google Account và không tái sử dụng, còn email có thể đổi và không duy nhất. **Account linking** là hợp nhất nhiều danh tính (mật khẩu, Google, GitHub) về một tài khoản. Auth0 cảnh báo liên kết không an toàn có thể cho kẻ xấu vào tài khoản hợp lệ, nên cần xác thực **cả hai** tài khoản trước khi liên kết. **Rủi ro:** tự động liên kết theo email có thể thành chiếm tài khoản (trang Auth0 không nói riêng về email chưa xác minh; tài liệu tấn công *pre-hijacking* của Microsoft Research **Chưa kiểm** vì trang trả 404). **Nguồn:** [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect), [Auth0 account linking](https://auth0.com/docs/manage-users/user-accounts/user-account-linking) · Vừa.

##### D5. SCIM (provisioning)
**Là gì:** chuẩn để tạo, đọc, sửa, xoá User và Group giữa các hệ thống qua HTTP và JSON (`application/scim+json`), dùng khi nhân viên vào, chuyển, nghỉ. Schema định nghĩa thuộc tính như `active` (ý nghĩa do service provider quy định). **Dùng khi:** nhiều ứng dụng SaaS cần đồng bộ tài khoản từ IdP; thiếu nó thì tài khoản người đã nghỉ vẫn còn. **Nguồn:** [RFC 7643](https://www.rfc-editor.org/rfc/rfc7643), [RFC 7644](https://www.rfc-editor.org/rfc/rfc7644) (Proposed Standard, 2015; hai RFC mới hơn là RFC 9865 và RFC 9967 đã cập nhật, **Chưa kiểm**) · Vừa.

#### Nhóm E. Directory doanh nghiệp và mạng

##### E1. LDAP
**Là gì:** giao thức truy vấn và thao tác với **thư mục** (directory) tổ chức thành cây. Thao tác **bind** là xác thực: **simple bind** gửi mật khẩu, **SASL bind** chọn cơ chế SASL. LDAP là giao thức truy cập thư mục, **không phải** giao thức SSO. **Rủi ro:** không gửi mật khẩu qua kết nối không có TLS hoặc SASL bảo vệ (ý cảnh báo này chỉ qua tóm tắt công cụ). **Dùng khi:** ứng dụng nội bộ cần kiểm người dùng hay nhóm theo thư mục công ty. **Nguồn:** [RFC 4511](https://www.rfc-editor.org/rfc/rfc4511) (Proposed Standard, 2006) · Vừa.

##### E2. Active Directory (AD)
**Là gì:** dịch vụ thư mục của Microsoft cho Windows domain: lưu đối tượng (user, máy, nhóm) theo phân cấp, có schema, global catalog và sao chép giữa các domain controller, tích hợp đăng nhập và kiểm soát truy cập. AD khác LDAP: LDAP là **một** cách truy cập AD. Trang tổng quan của Microsoft không nhắc LDAP hay Kerberos nên quan hệ này theo roadmap, **Chưa kiểm** nguồn riêng. **Nguồn:** [AD DS overview](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/get-started/virtual-dc/active-directory-domain-services-overview) · Vừa.

##### E3. Kerberos
**Là gì:** giao thức xác thực mạng dựa trên **vé** và một bên trung gian tin cậy là **KDC**. Client xin **TGT** (vé cấp vé) từ **AS** (Authentication Server), rồi dùng TGT xin **service ticket** từ **TGS** cho từng dịch vụ; vé được mã hoá bằng khoá của dịch vụ, authenticator có thời gian để chống phát lại. Chuẩn nói rõ Kerberos **tự nó không cung cấp uỷ quyền**. Lưu ý: "AS" ở đây là *Authentication Server*, khác *Authorization Server* của OAuth. Trang Microsoft về NTLM nói Kerberos là phương thức ưu tiên trong môi trường AD. **Dùng khi:** domain Windows hoặc Linux tích hợp. **Rủi ro:** lệch đồng hồ, cấu hình SPN và DNS sai (mutual authentication và ngưỡng lệch khoảng 5 phút chỉ qua tóm tắt công cụ, **Chưa kiểm** nguyên văn). **Nguồn:** [RFC 4120](https://www.rfc-editor.org/rfc/rfc4120) (Proposed Standard, 2005) · Vừa.

##### E4. NTLM
**Là gì:** họ giao thức **thử thách – phản hồi** cũ của Windows (LM, NTLMv1, NTLMv2). Microsoft nói NTLM vẫn được hỗ trợ và vẫn cần cho hệ thống thuộc workgroup, khuyến nghị **giảm sử dụng**. Trang đó **không nêu năm hay lộ trình ngừng hỗ trợ**, nên đừng viết rằng đã có mốc bỏ NTLM. **Dùng khi:** chỉ vì tương thích ngược. **Nguồn:** [NTLM overview](https://learn.microsoft.com/en-us/windows-server/security/kerberos/ntlm-overview) (ms.date 17/04/2025) · Vừa.

##### E5. RADIUS và TACACS+
**Là gì:** **RADIUS** là giao thức mà một **NAS** (máy chủ truy cập mạng) dùng để chuyển thông tin đăng nhập của người dùng tới máy chủ xác thực và làm theo phản hồi `Access-Accept`, `Access-Reject` hoặc `Access-Challenge` (cổng chính thức 1812; mật khẩu được che bằng MD5). Roadmap xếp RADIUS và TACACS+ vào truy cập mạng và thiết bị (Wi-Fi doanh nghiệp, VPN, thiết bị mạng); bản RFC phần đã đọc chỉ nói về dial-in và PPP, nên việc dùng cho VPN là theo roadmap, **Chưa kiểm**. Không thay được OIDC cho đăng nhập ứng dụng web. TACACS+ chưa có nguồn riêng, **Chưa kiểm**. **Nguồn:** [RFC 2865](https://www.rfc-editor.org/rfc/rfc2865) (Draft Standard, 2000) · Vừa.

##### E6. Chứng chỉ X.509, mTLS và smart card
**Là gì:** xác thực bằng chứng thư và khoá riêng (smart card, chứng chỉ client qua **mTLS**, tức TLS hai chiều). Trong OAuth, [RFC 8705](https://www.rfc-editor.org/rfc/rfc8705) gồm **hai phần độc lập**: (1) client xác thực bằng mTLS (`tls_client_auth` qua PKI, hoặc `self_signed_tls_client_auth`); (2) access token **gắn với chứng chỉ client** qua claim `cnf` (`x5t#S256`), resource server đối chiếu với chứng chỉ TLS. **Dùng khi:** máy-máy trong mạng kín, môi trường chính phủ và ngân hàng, thiết bị được quản lý. **Rủi ro:** cần PKI, quy trình cấp và thu hồi chứng thư (theo roadmap). **Nguồn:** RFC 8705 (Proposed Standard, 2020) · Vừa.

#### Nhóm F. OAuth nâng cao và phân quyền

##### F1. Các luồng OAuth 2.0 (nhắc lại và phần còn thiếu)
Authorization Code + PKCE, Client Credentials, Implicit, ROPC đã có ở Part 4. Còn thiếu **Device Authorization Grant**: thiết bị không bàn phím (TV, máy in) gọi device authorization endpoint, nhận `device_code`, `user_code` và `verification_uri`; người dùng mở URI trên thiết bị khác và nhập `user_code`; thiết bị **hỏi định kỳ** (poll) token endpoint với các lỗi `authorization_pending`, `slow_down`, `access_denied`, `expired_token`. **Nguồn:** [RFC 8628](https://www.rfc-editor.org/rfc/rfc8628) (2019) · Vừa. Theo [RFC 9700](https://www.rfc-editor.org/rfc/rfc9700) (BCP 240, 01/2025): redirect URI so khớp chính xác; public client **bắt buộc** PKCE; Implicit **nên không dùng**; ROPC **không được dùng**; access token nên bị ràng buộc người gửi (mTLS hoặc DPoP) và giới hạn audience · Vừa.

##### F2. OAuth 2.1
**Là gì:** bản hợp nhất OAuth 2.0 với các thực hành bảo mật đã ban hành sau. Tại thời điểm làm mới (25/11/2026) trang datatracker ghi `draft-ietf-oauth-v2-1-17`, **vẫn là Internet-Draft, chưa thành RFC** (ngày tài liệu 31/10/2026). Thay đổi chính (phần danh sách chỉ qua tóm tắt công cụ): bỏ Implicit và password grant, bắt buộc PKCE, so khớp redirect URI chặt, không gửi access token qua query, ràng buộc refresh token của public client. **Nguồn:** [draft-ietf-oauth-v2-1](https://datatracker.ietf.org/doc/draft-ietf-oauth-v2-1/) · Vừa.

##### F3. Token Exchange và On-Behalf-Of
**Là gì:** đổi một token lấy token khác (`grant_type=urn:ietf:params:oauth:grant-type:token-exchange`), với `subject_token` và tuỳ chọn `actor_token`. **Impersonation**: bên nhận hành động **như** chính subject; **delegation**: token mới giữ subject và thêm claim `act` ghi bên được uỷ quyền. **Dùng khi:** chuỗi dịch vụ gọi nhau thay mặt người dùng. **Nguồn:** [RFC 8693](https://www.rfc-editor.org/rfc/rfc8693) (2020) · Vừa. On-Behalf-Of của Entra ID theo roadmap, **Chưa kiểm** nguồn Microsoft.

##### F4. DPoP
**Là gì:** ràng buộc token với **khoá của client** ở tầng ứng dụng: mỗi request kèm header `DPoP`, một JWT ký (có `htm`, `htu`, `iat`, `jti`, và `ath` là băm của access token). Khác bearer: ai cầm token cũng không dùng được nếu không có khoá riêng. DPoP không phải phương thức xác thực client và vẫn cần HTTPS. **Nguồn:** [RFC 9449](https://www.rfc-editor.org/rfc/rfc9449) (2023) · Vừa.

##### F5. PAR (và JAR, RAR)
**Là gì:** **PAR** cho client gửi tham số authorization request thẳng tới PAR endpoint, nhận `request_uri`, rồi mới chuyển người dùng đi. Lợi ích: client được xác thực trước khi người dùng tương tác, tham số không đi qua trình duyệt. **Nguồn:** [RFC 9126](https://www.rfc-editor.org/rfc/rfc9126) (2021) · Vừa. **JAR**, **RAR**, **JARM** và FAPI: chỉ có tên trong roadmap, **Chưa kiểm**.

##### F6. Introspection và revocation
Đã có ở Part 4 (RFC 7662 và RFC 7009, mức ⚠). Mục này chỉ nhắc để bản đồ đủ.

##### F7. Mô hình phân quyền: ACL, RBAC, ABAC, ReBAC
**Là gì:** **ACL** liệt kê ai làm được gì trên từng đối tượng; **RBAC** gán quyền cho **vai trò**, rồi gán vai trò cho người dùng; **ABAC** quyết định theo **thuộc tính** của chủ thể, đối tượng, thao tác và môi trường so với chính sách; **ReBAC** quyết định theo **quan hệ** giữa các đối tượng. NIST nói RBAC được Ferraiolo và Kuhn chính thức hoá năm 1992, chuẩn hiện hành là INCITS 359-2012 (trang dự án NIST đã lưu trữ); ABAC định nghĩa ở [NIST SP 800-162](https://csrc.nist.gov/pubs/sp/800/162/upd2/final) (Final, 2014, cập nhật 2019); [Zanzibar](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/) của Google (USENIX ATC 2019) là hệ thống lưu và đánh giá ACL toàn cầu, trang không dùng từ "ReBAC", nên ReBAC là cách gọi theo roadmap, **Chưa kiểm** nguồn riêng. **Dùng khi:** RBAC cho vai trò ít và ổn định; ABAC khi quyết định phụ thuộc ngữ cảnh; quan hệ khi quyền theo cấu trúc sở hữu và chia sẻ (như board, task). **Nguồn:** NIST RBAC project, SP 800-162, Zanzibar · Vừa.

##### F8. Workload identity federation, WS-Federation, risk-based và Conditional Access, Decentralized Identity
Chỉ có tên trong mục "nâng cao" của roadmap (mục 8.1), **Chưa kiểm** nguồn riêng; ghi lại để biết chúng tồn tại. **WS-Federation** là giao thức federation họ SOAP của hệ sinh thái Microsoft, thường gặp ở hệ thống cũ cạnh SAML; **risk-based authentication** và **Conditional Access** đổi mức yêu cầu xác thực theo rủi ro của từng lần đăng nhập. Phần này là **bậc học tiếp theo**, không đào sâu ở đây.

## 4. Cách dùng

### Đọc một request HTTP để biết nó dùng phương thức nào

```http
GET /api/v1/boards HTTP/1.1
Authorization: Basic dTE6ZGVtby1wYXNz        # Basic: base64("u1:demo-pass"), không mã hoá

GET /api/v1/boards HTTP/1.1
Authorization: Bearer eyJhbGciOi...          # Bearer: ai cầm token là dùng được

HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token"   # server nói scheme cần và lý do từ chối
```

Kết quả mong đợi: `printf 'dTE6ZGVtby1wYXNz' | base64 -d` in `u1:demo-pass`, chứng minh Basic chỉ là mã hoá biểu diễn. Giá trị trên là chuỗi giả của ví dụ. (Đã chạy lệnh giải mã; các dòng HTTP là minh hoạ cú pháp, **Chưa kiểm** trên máy chủ thật.)

### Chọn phương thức theo tình huống (quy trình 5 câu hỏi)

1. **Ai xác thực?** Người dùng cuối, nhân viên, hay máy?
2. **Client chạy ở đâu và có giữ được bí mật không?** (máy chủ có; trình duyệt, mobile, desktop không)
3. **Danh tính đã có ở nơi khác chưa?** Nếu có (công ty, Google) thì **federation**, đừng tự giữ mật khẩu.
4. **Dữ liệu và rủi ro thế nào?** Nhạy cảm thì thêm yếu tố thứ hai chống phishing (passkey) và step-up cho thao tác nhạy cảm.
5. **Ai vận hành?** Tự xây chỉ để học (ADR 0006); sản phẩm thật dùng dịch vụ hoặc máy chủ đã kiểm chứng.

### Ánh xạ sang ASP.NET Core (theo roadmap, **Chưa kiểm** chạy)

| Khái niệm | ASP.NET Core / .NET |
|---|---|
| Scheme và handler | `AddAuthentication()`, `AddCookie()`, `AddJwtBearer()`, `AddOpenIdConnect()` |
| Phiên đăng nhập web | Cookie authentication, ticket bảo vệ bằng Data Protection (cần chung khoá khi nhiều instance) |
| API kiểm access token | `AddJwtBearer()` với `Authority`, `Audience` |
| Mật khẩu, khoá tạm, 2FA TOTP | ASP.NET Core Identity (roadmap ghi passkey được hỗ trợ từ .NET 10) |
| Phân quyền | `[Authorize]`, policy, `IAuthorizationService` |

## 5. Ưu và nhược điểm

| Ưu | Nhược / cái giá |
|---|---|
| Có đủ công cụ cho mọi ngữ cảnh: web, SPA, mobile, máy-máy, domain doanh nghiệp | Nhiều phương thức dễ **chọn sai ngữ cảnh** (API key cho người dùng, SMS làm yếu tố chính, ROPC) |
| Phần lớn có chuẩn mở (RFC, OASIS, W3C), có thư viện và sản phẩm sẵn | Chuẩn chồng chuẩn: OAuth 2.1 vẫn là draft, nhiều RFC bị RFC sau cập nhật; phải kiểm bản hiện hành |
| Passkey và token ràng buộc người gửi giảm hẳn phishing và lộ token | Khôi phục tài khoản, tương thích và PKI làm tăng độ phức tạp vận hành |
| Federation tập trung quản lý danh tính và thu hồi một chỗ | IdP thành điểm lỗi và mục tiêu tấn công lớn nhất; khoá ký bị lộ thì mọi SP bị giả |

**Nên dùng khi:** biết rõ ai xác thực, ở đâu, rủi ro nào, rồi mới chọn. **Không nên:** copy một phương thức vì "ai cũng dùng", hoặc tự xây phương thức mật mã khi không có người rà soát.

### Bản đồ giải pháp

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn (đã mở hoặc Chưa kiểm) |
|---|---|---|---|---|
| Mật khẩu cộng session cookie | ASP.NET Core Identity | Một ứng dụng web một miền | CSRF, cố định phiên, khó cho nhiều dịch vụ | OWASP Session, NIST 800-63B-4 (Vừa) |
| Mật khẩu cộng yếu tố thứ hai (TOTP) | Ứng dụng TOTP, ASP.NET Core Identity | Tăng bảo mật nhanh, không cần phần cứng | Vẫn bị phishing thời gian thực | RFC 6238, OWASP MFA (Vừa) |
| Passkey (WebAuthn/FIDO2) | Windows Hello, khoá bảo mật, passkey đồng bộ | Muốn chống phishing, giảm phụ thuộc mật khẩu | Khôi phục tài khoản, người dùng chuyển hệ sinh thái | FIDO Alliance (Vừa); W3C WebAuthn Chưa kiểm |
| Token qua OAuth/OIDC (JWT hay opaque) | OpenIddict, Duende, Keycloak, Cognito | Nhiều dịch vụ, SPA, mobile | Thu hồi khó (token tự chứa), phải kiểm token đúng | RFC 6750, OIDC Core (Vừa) |
| Federation SAML | Keycloak, Entra ID, IdP doanh nghiệp | SSO doanh nghiệp với phần mềm hỗ trợ SAML | XML signature phức tạp, khoá ký là mục tiêu | OASIS overview (Vừa) |
| Social login (OIDC với Google, GitHub...) | Google, GitHub qua OIDC hoặc OAuth | Ứng dụng khách hàng, giảm ma sát đăng ký | Phụ thuộc bên thứ ba, rủi ro liên kết tài khoản | Google OIDC, Auth0 (Vừa) |
| Directory doanh nghiệp (LDAP, AD, Kerberos) | Active Directory, OpenLDAP | Mạng nội bộ, đăng nhập tích hợp domain | Gắn hạ tầng Windows hoặc LDAP, lệch đồng hồ, cấu hình SPN | RFC 4511, RFC 4120, Microsoft (Vừa) |
| Chứng chỉ (mTLS, smart card) | Hạ tầng PKI | Máy-máy trong mạng kín, môi trường tuân thủ cao | Vận hành PKI: cấp, gia hạn, thu hồi | RFC 8705 (Vừa) |
| API key | Khoá do nền tảng cấp | API công khai đơn giản, đo hạn mức | Không có người dùng, dễ lộ, khó xoay | Google Cloud (Vừa) |
| HTTP Basic qua TLS | Máy chủ web tự chọn | Công cụ nội bộ, tích hợp cũ | Mật khẩu theo từng request | RFC 7617 (Vừa) |
| Kerberos/NTLM tích hợp Windows | Windows Integrated Authentication | Ứng dụng intranet cho domain | NTLM cũ, chỉ còn vì tương thích | Microsoft NTLM overview (Vừa) |
| RADIUS / TACACS+ | FreeRADIUS, thiết bị mạng | Wi-Fi doanh nghiệp, quản trị thiết bị mạng | Không dành cho đăng nhập ứng dụng web | RFC 2865 (Vừa); TACACS+ Chưa kiểm |
| **Không xác thực** | – | Dữ liệu công khai hoàn toàn, hoặc mạng kín tin cậy cho nguyên mẫu | Không phù hợp bất kỳ dữ liệu riêng nào | Part 1 của lộ trình (Chắc) |

**Chọn theo loại ứng dụng** (theo roadmap, mục 7.3, tôi tự đọc, **Chắc**):

| Loại ứng dụng | Nên cân nhắc | Tránh |
|---|---|---|
| Web có backend | OIDC với Authorization Code + PKCE, session cookie phía server | Lưu token trong JavaScript nếu không cần |
| SPA | OIDC với PKCE (nên qua mẫu BFF, Part 1) | Implicit |
| Desktop và mobile | OIDC với PKCE qua trình duyệt hệ thống | ROPC (client cầm mật khẩu) |
| Máy-máy / microservice | Client Credentials (hoặc mTLS) | API key dài hạn cho dữ liệu nhạy cảm |
| Thiết bị không bàn phím | Device Authorization Grant | Bắt nhập mật khẩu trên TV |
| Mạng Windows domain | Kerberos qua AD | NTLM nếu không bắt buộc |

## 6. Lỗi hay gặp và cách tránh

| Triệu chứng | Nguyên nhân | Cách phát hiện, sửa |
|---|---|---|
| Lộ mật khẩu hoặc token trong log proxy | Basic hay bearer qua HTTP, hoặc token trong query string | Quét log tìm `Authorization`, `access_token=`; bắt buộc TLS, chỉ gửi token qua header |
| API key bị coi là "danh tính người dùng", sau đó lộ trong mã SPA | Dùng key cho hành động thay mặt người dùng hoặc client công khai | Chuyển sang OAuth/OIDC; key chỉ cho định danh project và hạn mức |
| Kẻ lạ vào được tài khoản sau khi đăng nhập xã hội | Liên kết hoặc nhận diện tài khoản theo email | Định danh bằng `sub`; chỉ liên kết sau khi xác thực cả hai tài khoản |
| Mã TOTP bị đoán hoặc dùng lại | Không giới hạn số lần thử, nhận lại mã đã dùng | Throttle, chặn mã đã dùng, chỉ chấp nhận lệch tối đa một bước |
| Người dùng bị phishing dù có MFA | Dùng SMS hoặc TOTP thay vì phương thức chống phishing | Thêm passkey; với push dùng number matching |
| SAML bị giả mạo | Khoá ký IdP bị lấy hoặc SP không kiểm chữ ký đúng | Bảo vệ khoá ký bằng phần cứng, kiểm chữ ký và audience (case ở mục 7) |
| Còn NTLM trong mạng dù đã có AD | Dịch vụ tụt về NTLM vì tương thích | Kiểm tra nhật ký xác thực, ưu tiên Kerberos, giảm NTLM theo hướng dẫn Microsoft |

## 7. Trong thực tế (real-world)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (link đã mở, năm) | Mức |
|---|---|---|---|---|---|
| GitHub | Muốn thay mật khẩu bằng passkey cho người dùng GitHub.com | Thử nghiệm từ 07/2023 với hàng chục nghìn nhà phát triển, rồi mở cho mọi người; cho đăng ký qua điện thoại cross-device khi Linux và Firefox thiếu hỗ trợ, bỏ qua kết quả kiểm `IsUVPAA`; cho nâng khoá bảo mật lên passkey | Một số tổ hợp trình duyệt, hệ điều hành và khoá lỗi khi đăng ký lại; nhắc đăng ký dần dần vì người đã có passkey dễ nhầm | [GitHub Blog, "Passkeys are generally available"](https://github.blog/news-insights/company-news/passkeys-are-generally-available/) (2023) | Vừa (bài gốc của GitHub, agent mở) |
| Microsoft | Đưa passkey cho tài khoản tiêu dùng (từ 05/2024) | Nhắc đăng ký đúng thời điểm (tạo tài khoản, sau đăng nhập, đặt lại mật khẩu), mặc định passkey khi có sẵn, nhấn mạnh tốc độ và bảo mật | Microsoft tự báo cáo: đăng nhập bằng passkey nhanh gấp 3 lần mật khẩu và gấp 8 lần mật khẩu cộng MFA; thành công 98% so với 32%. Bài học: chờ người dùng tự vào cài đặt hiệu quả thấp (dưới 1%) | [Microsoft Security Blog, "Convincing a billion users to love passkeys"](https://www.microsoft.com/en-us/security/blog/2024/12/12/convincing-a-billion-users-to-love-passkeys-ux-design-insights-from-microsoft-to-boost-adoption-and-security/) (2024) | Vừa (bài gốc của Microsoft, số liệu tự báo cáo) |
| Microsoft (ứng phó vụ tấn công SolarWinds) | Kẻ tấn công có quyền quản trị on-premises rồi lấy chứng chỉ ký SAML token tin cậy | Dùng chứng chỉ để **tự ký token SAML giả** mạo người dùng, kể cả tài khoản đặc quyền | Khuyến nghị bảo vệ khoá ký theo thực hành tốt nhất của nhà cung cấp, cân nhắc phần cứng bảo mật cho chứng chỉ ký; trang không dùng từ "Golden SAML" và không kết luận đó là lỗi quản lý khoá | [MSRC, "Customer Guidance on Recent Nation-State Cyber Attacks"](https://www.microsoft.com/en-us/msrc/blog/2020/12/customer-guidance-on-recent-nation-state-cyber-attacks) (14/12/2020) | Vừa (bài gốc của Microsoft) |

**Trạng thái real-world: Đạt một phần.** Đủ 3 bài gốc của công ty, nhưng do agent nghiên cứu mở, tôi chưa tự mở lại (mức Vừa). Các nguồn dự định dùng nhưng không mở được, ghi **Chưa kiểm**: W3C WebAuthn Level 3 (403), CISA phishing-resistant MFA (403), bài của Uber về sự cố MFA fatigue 2022 (404), bài passkey của Google (404), Microsoft Research pre-hijacking (404).

### Mô hình phổ biến
- **Federation thay vì tự giữ mật khẩu** cho nhân viên (SAML hoặc OIDC từ IdP công ty), thêm SCIM để tài khoản theo vòng đời nhân sự.
- **Passkey bật dần, mật khẩu còn làm dự phòng**, nhắc đăng ký đúng thời điểm hơn là chờ người dùng tự tìm.
- **Định danh bằng `sub`**, không bằng email, khi dùng đăng nhập xã hội.
- Máy-máy dùng Client Credentials hoặc mTLS; API key chỉ cho định danh project và hạn mức.

### Khi nó thất bại hoặc bị bỏ
- Khoá ký SAML bị lấy thì **mọi** dịch vụ tin IdP đó bị giả danh (case MSRC trên); federation tập trung rủi ro vào khoá ký.
- Passkey triển khai gặp lỗi tương thích nền tảng và khôi phục tài khoản (case GitHub trên).
- NTLM vẫn còn vì tương thích dù Kerberos được ưu tiên; Microsoft khuyến nghị giảm nhưng không nêu mốc ngừng hỗ trợ.

## 8. Trong TeamBoard

`identity-service` tự xây **để học** ([ADR 0006](../../architecture/adr/0006-identity-self-built.md)). Phạm vi theo [spec identity-auth](../../spec/identity-auth.md) mục 4 và [identity-learning.md](../../architecture/identity-learning.md):

| Phương thức | Trạng thái trong dự án | Ở đâu |
|---|---|---|
| Mật khẩu (PBKDF2, khoá tạm, đổi mật khẩu) | Làm (I1) | Spec FR-001..004, FR-022; [Part 2](part-2-core-skills.md), [Part 5](part-5-production-security-ops.md) |
| JWT, JWKS, xoay khoá | Làm (I2, I3) | [Part 2](part-2-core-skills.md), [Part 3](part-3-multi-component.md) |
| Refresh token xoay vòng | Làm (I4) | [Part 4](part-4-advanced.md) |
| Authorization Code + PKCE, OIDC, Client Credentials, scope | Làm (I5 tới I8) | [Part 3](part-3-multi-component.md), [Part 4](part-4-advanced.md) |
| Session cookie trên origin identity | Làm một phần (trang đăng nhập, FR-019) | Spec FR-011, FR-019, NFR-010 |
| MFA/OTP, passkey | **Ngoài phạm vi** | Spec mục 4 ("MFA"); chỉ khái niệm, ⚠ ở README |
| SAML, social login (Google, GitHub) | **Ngoài phạm vi** | Spec mục 4; chỉ khái niệm |
| Xác minh email, quên mật khẩu, RP-initiated logout | **Ngoài phạm vi** | Spec mục 4; cần `notification-service` |
| LDAP/AD/Kerberos/NTLM/RADIUS | Không áp dụng (không có domain Windows) | Không có trong spec; chỉ bản đồ này |
| Implicit, ROPC, Hybrid | Chỉ đọc lý thuyết, không xây | Spec mục 4, ADR 0006 |
| Device, Token Exchange, DPoP, PAR | Không làm | Chỉ nhắc ở Part 4 hoặc bản đồ này; ⚠ |
| API key, mTLS | Không làm (chỉ so sánh) | [Part 1](part-1-fundamentals.md) mục L7 |

Điểm lệch để biết (không tự sửa): spec BR-002 yêu cầu mật khẩu **tối thiểu 12 ký tự**, trong khi NIST SP 800-63B-4 (trang agent đã mở, ghi 26/08/2025) đặt **15 ký tự** cho mật khẩu một yếu tố. Hai con số khác nhau; có thể chấp nhận vì dự án học, nhưng nếu thành sản phẩm thật thì cần chốt lại theo chuẩn hiện hành (đề xuất để người dùng quyết). Khi nào cần MFA, SAML hoặc đăng nhập xã hội thật, [Part 1](part-1-fundamentals.md) L7 đã chỉ hướng: dùng dịch vụ quản lý hoặc Keycloak thay vì tự xây.

## 9. Học tiếp

| # | Tài liệu | Loại | Đọc để biết |
|---|---|---|---|
| 1 | [Identity: Part 1 Fundamentals](part-1-fundamentals.md) | Nội bộ | Session cookie so với token, Bản đồ giải pháp tự xây hay không |
| 2 | [Identity: Part 4 Advanced](part-4-advanced.md) | Nội bộ | OAuth/OIDC, PKCE, thu hồi, Device ở mức bảng chọn |
| 3 | [Identity: Part 5 Production](part-5-production-security-ops.md) | Nội bộ | Tấn công và biện pháp, brute force, quản lý khoá |
| 4 | [Spec identity-auth](../../spec/identity-auth.md) | Nội bộ | Phạm vi thật của `identity-service` (mục 4) |
| 5 | [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/) (Session, Authentication, MFA) | Chính thức | Thực hành tốt nhất theo chủ đề (Vừa) |
| 6 | [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html) | Chính thức | Mật khẩu, authenticator, mức AAL (Vừa) |
| 7 | [RFC 9700](https://www.rfc-editor.org/rfc/rfc9700) | Chính thức | Thực hành bảo mật OAuth hiện hành (Vừa) |
| 8 | [FIDO Alliance: passkeys](https://fidoalliance.org/passkeys/) | Chính thức | Passkey đồng bộ và gắn thiết bị (Vừa) |

## 10. Tự kiểm tra / thực hành

1. Vì sao Base64 trong HTTP Basic không làm mật khẩu an toàn hơn, và điều kiện tối thiểu để dùng Basic là gì?
2. Một dịch vụ đăng nhập bằng Google và liên kết tài khoản theo email. Nêu kịch bản chiếm tài khoản và cách sửa (gợi ý: `sub`, xác thực cả hai tài khoản).
3. Phân biệt TOTP, SMS OTP và passkey về khả năng chống phishing, và nêu một lý do passkey thất bại khi triển khai thật.
4. Chọn phương thức cho: (a) nhân viên vào ứng dụng nội bộ, (b) CLI trên máy chủ không có trình duyệt, (c) hai microservice gọi nhau, (d) TV. Giải thích bằng quy trình 5 câu hỏi.
5. **Việc chạy được trên repo:** chạy `printf 'dTE6ZGVtby1wYXNz' | base64 -d` rồi mở [spec identity-auth](../../spec/identity-auth.md) mục 4, liệt kê phương thức của bảng ở mục 8 mà `identity-service` **không** làm và lý do.

<details>
<summary>Đáp án ngắn</summary>

1. Base64 chỉ là cách biểu diễn, ai cũng giải mã được; Basic chỉ chấp nhận được qua TLS.
2. Kẻ tấn công tạo tài khoản ở IdP khác với email của nạn nhân (chưa xác minh) rồi nhờ liên kết theo email để chiếm; sửa bằng `sub` và xác thực cả hai tài khoản trước khi liên kết.
3. Passkey chống phishing (ràng buộc RP ID), TOTP vẫn bị proxy phishing thời gian thực, SMS yếu nhất; passkey thất bại do lỗi tương thích nền tảng và khôi phục tài khoản (case GitHub).
4. (a) OIDC/SAML từ IdP công ty; (b) Device Authorization hoặc Client Credentials; (c) Client Credentials hoặc mTLS; (d) Device Authorization.
5. Lệnh in `u1:demo-pass`; phương thức không làm gồm MFA, SAML, social login, xác minh email, LDAP/AD/Kerberos, Device, Token Exchange, DPoP, PAR, API key, mTLS, với lý do ghi ở bảng mục 8.
</details>

## Nguồn và độ tin cậy

- Mức **Chắc**: nội dung roadmap `authentication_standards_15h_roadmap.md` (tôi tự đọc), Part 1 của lộ trình.
- Mức **Vừa**: mọi RFC, OIDC, OASIS, OWASP, NIST, Microsoft, Google, GitHub, Auth0, FIDO ở trên do agent nghiên cứu mở và đọc; với trang dài, agent chỉ đọc nguyên văn khoảng 40.000 ký tự đầu, phần còn lại qua tóm tắt công cụ nên đã ghi **Chưa kiểm** ở các ý tương ứng.
- Đã sửa theo agent: tóm tắt công cụ từng nói sai (Google khuyến nghị `sub` chứ không trung lập; RFC 7616 có ghi "Obsoletes RFC 2617"; RFC 6750 có cảnh báo query string; NIST ghi 15 ký tự), bản trên theo nguyên văn.
- **Chưa kiểm** (không mở được): W3C WebAuthn Level 3, CISA phishing-resistant MFA, bài Uber 2022, bài passkey của Google, Microsoft Research pre-hijacking. Các mục liên quan chỉ nêu theo FIDO, OWASP hoặc roadmap.
- Tài liệu này không thay security review hay tư vấn bảo mật cho hệ thống thật.
