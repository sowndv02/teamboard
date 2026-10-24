# Part 4 — Advanced (Identity)

> [!NOTE]
> **Mục tiêu:** hiểu và chạy luồng đăng nhập đầy đủ: OAuth 2.0 với Authorization Code + PKCE, refresh token xoay vòng, và biết thu hồi token khó ở đâu.
> **Cần biết trước:** [Part 3](part-3-multi-component.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** chạy luồng code + PKCE và chỉ ra PKCE chặn việc gì; chứng minh refresh token bị đánh cắp bị phát hiện.
> [← Part 3](part-3-multi-component.md) · [Part 5 →](part-5-production-security-ops.md)

## L1 What — Các khái niệm

#### OAuth 2.0
**Là gì:** khung **ủy quyền**: ứng dụng (client) xin quyền truy cập tài nguyên của người dùng mà **không cầm mật khẩu** của họ. Bốn vai: người dùng (resource owner), client, máy chủ ủy quyền, máy chủ tài nguyên (API).
**Use case:** web client của TeamBoard xin quyền đọc Board thay người dùng.

#### OpenID Connect (OIDC)
**Là gì:** lớp **xác thực** đặt trên OAuth 2.0: thêm `id_token` (JWT nói người dùng là ai), `nonce`, trang discovery. OAuth một mình không nói "ai đã đăng nhập".

#### Authorization Code + PKCE
**Là gì:** luồng chuẩn cho ứng dụng có người dùng. Client nhận **code** dùng một lần qua chuyển hướng, đổi lấy token ở kênh sau (back-channel); **PKCE** gắn code với một bí mật (`code_verifier`) mà chỉ client khởi tạo luồng biết.
**Use case:** đăng nhập từ web hoặc ứng dụng di động (I4).

#### Refresh token
**Là gì:** token sống dài, đổi lấy access token mới mà không bắt người dùng đăng nhập lại. **Xoay vòng (rotation):** mỗi lần dùng cấp refresh token mới, token cũ vô hiệu.

#### Thu hồi và introspection
**Thu hồi (RFC 7009):** báo máy chủ vô hiệu một token. **Introspection (RFC 7662):** API hỏi máy chủ "token này còn hiệu lực không" thay vì tự kiểm chữ ký.

## L2 Why — Vì sao có những cơ chế này

- **Code thay vì token trên URL:** chuyển hướng đi qua trình duyệt, nơi lịch sử, log và tiện ích đọc được URL. Chỉ code dùng một lần, hết hạn nhanh đi qua URL; token đổi ở kênh riêng.
- **PKCE:** nếu kẻ tấn công **chặn được code** (ứng dụng độc hại cùng máy, log) thì code đơn thuần đủ để đổi token. Với PKCE, không có `code_verifier` thì đổi thất bại (Lab 5.2).
- **Access token ngắn + refresh token dài:** token lộ chỉ có hại vài phút; refresh token cất kín và có thể phát hiện dùng lại.
- **Thu hồi:** JWT tự chứa nên bên kiểm không biết nó đã bị thu hồi; muốn tức thì phải hỏi nơi cấp (introspection) hoặc giữ danh sách thu hồi, đổi lại mất lợi ích "không cần hỏi lại".

## L3 How — Luồng và cơ chế

### Authorization Code + PKCE
```
client                       trình duyệt               máy chủ ủy quyền        API
  | tạo code_verifier (ngẫu nhiên 43 ký tự)
  | code_challenge = BASE64URL(SHA256(verifier))
  |-- mở /authorize?client_id&redirect_uri&state&code_challenge&method=S256 -->
  |                              người dùng đăng nhập, đồng ý
  |<-- chuyển hướng redirect_uri?code=...&state=... ------------------|
  | kiểm state khớp với state đã lưu
  |-- POST /token: code + code_verifier + redirect_uri ------------->|
  |<-- access_token (+ refresh_token) --------------------------------|
  |-- gọi API với Authorization: Bearer access_token ---------------------------->|
```
Máy chủ băm `code_verifier` nhận được, so với `code_challenge` đã lưu lúc `/authorize`, khớp mới đổi token.

### Ba kiểm tra bắt buộc ở máy chủ
1. **`redirect_uri` khớp CHÍNH XÁC** với giá trị đã đăng ký (không theo tiền tố).
2. **Code dùng một lần** và sống ngắn; dùng lần hai thì từ chối (và nên thu hồi token đã cấp từ code đó).
3. **PKCE bắt buộc** (chuẩn Security BCP khuyến nghị dùng cho mọi client có code flow).

### Refresh token xoay vòng
Mỗi lần dùng refresh token `r1`, máy chủ cấp `r2` và đánh dấu `r1` đã dùng. Nếu `r1` xuất hiện **lần nữa** thì hoặc kẻ trộm hoặc người dùng thật đang dùng bản cũ: máy chủ thu hồi **cả chuỗi** (mọi refresh token cùng gốc), buộc đăng nhập lại.

### `id_token` và `nonce` (OIDC, ⚠ chưa chạy)
`id_token` là JWT dành cho **client** (`aud` là `client_id`), nói người dùng là ai. `nonce` do client sinh, gửi ở `/authorize` và phải xuất hiện lại trong `id_token`: chặn phát lại `id_token` cũ. Không dùng `id_token` để gọi API.

## L4 Hands-on — Lab Part 4

Môi trường: như Part 0 (máy chủ ủy quyền do lab dựng trên `127.0.0.1`, người dùng giả `alice` luôn đã đăng nhập, `redirect_uri` đã đăng ký `http://127.0.0.1:18701/callback`).

#### Lab 4.1 — Luồng Authorization Code + PKCE
**Đầu ra thật:**
```text
code_verifier dài 43 ký tự (RFC 7636: 43 đến 128); code_challenge = BASE64URL(SHA256(verifier)) dài 43
/authorize -> HTTP 302, chuyển hướng tới http://127.0.0.1:18701/callback, có code: True, state khớp: True
/token đúng verifier -> HTTP 200, có access_token (JWT 3 phần) và refresh_token: True
gọi /api/boards bằng access token -> HTTP 200; không token -> HTTP 401
/token với code_verifier SAI -> HTTP 400: invalid_grant (code_verifier sai)
```
**Giải thích:** đủ luồng: chuyển hướng có `code` và `state` khớp, đổi token đúng `verifier` thành công, API 200 có token và 401 không có, `verifier` sai bị từ chối.

#### Lab 4.2 — Refresh token xoay vòng và phát hiện dùng lại
**Đầu ra thật:**
```text
-- bản ĐÚNG (xoay vòng + phát hiện dùng lại)
   lần 1 dùng r1: HTTP 200, r2 khác r1
   kẻ trộm dùng lại r1: HTTP 400 (invalid_grant (phát hiện dùng lại refresh token: thu hồi cả chuỗi))
   người dùng thật dùng r2 sau đó: HTTP 400 (invalid_grant (chuỗi refresh đã bị thu hồi))
-- bản YẾU (không xoay vòng)
   lần 1 dùng r1: HTTP 200, r2 GIỐNG r1 (không xoay)
   kẻ trộm dùng lại r1: HTTP 200 (THÀNH CÔNG)
   người dùng thật dùng r2 sau đó: HTTP 200 
```
**Giải thích:** bản đúng phát hiện việc dùng lại và thu hồi cả chuỗi; **cái giá** là người dùng thật cũng bị đăng xuất (bản đúng chặn cả `r2`). Bản yếu để kẻ trộm dùng song song với người thật mà không ai biết.

## L5 Troubleshooting — Triệu chứng và nguyên nhân gốc

| Triệu chứng | Nguyên nhân thường gặp | Cách kiểm |
|---|---|---|
| `invalid_grant` khi đổi code | code đã dùng hoặc hết hạn, hoặc `code_verifier` sai | đọc `error_description`; kiểm client lưu đúng verifier cho đúng luồng |
| `invalid_redirect_uri` | `redirect_uri` không khớp chính xác (dấu `/`, cổng, `http` so với `https`) | so từng ký tự với giá trị đã đăng ký |
| Người dùng bị đăng xuất bất ngờ | phát hiện dùng lại refresh token (hai tab cùng làm mới) | xem log máy chủ; làm mới tuần tự hoặc cho khoảng ân hạn ngắn |
| `state` không khớp ở callback | mở callback từ luồng khác, hoặc mất `state` đã lưu | so `state` đã lưu với `state` nhận |
| API 401 dù vừa đăng nhập | `aud` của access token không phải API này | giải mã payload, so `aud` (Part 2) |

## L6 Production — Cân nhắc khi chạy thật

- **Thời hạn:** access token vài phút đến vài chục phút; refresh token ngày đến tuần, có thời hạn tuyệt đối. Ngắn thì an toàn hơn nhưng nhiều lần làm mới.
- **Lưu refresh token:** ở máy chủ chỉ lưu **băm** (như lab lưu SHA-256 của token), không lưu bản gốc.
- **Thu hồi:** nếu cần thu hồi tức thì (đổi mật khẩu, mất thiết bị) thì cần introspection hoặc danh sách thu hồi; nếu chấp nhận trễ bằng thời hạn access token thì không cần (⚠, đánh đổi do bạn chốt).
- **Client trình duyệt:** tránh để token trong bộ nhớ lâu dài của JavaScript; mẫu BFF để máy chủ giữ token (⚠, chưa chạy).
- **Quan sát:** log mọi lần cấp, làm mới, từ chối, **không log token, `code`, `code_verifier`**, chỉ log id ngắn hoặc băm.

## L7 Architecture — Chọn luồng nào

| Tình huống | Luồng | Lý do |
|---|---|---|
| Web hoặc di động có người dùng | Authorization Code + PKCE | chuẩn hiện hành, chặn chặn-code |
| Máy gọi máy | Client Credentials (Part 3) | không có người dùng |
| Thiết bị không bàn phím (TV, CLI) | Device Authorization (RFC 8628, ⚠) | nhập mã trên thiết bị khác |
| Ứng dụng trình duyệt thuần cũ | Implicit | **không dùng**: token trên URL; Security BCP khuyến nghị bỏ |
| Client tin cậy cũ nhập mật khẩu | Resource Owner Password | **không dùng**: client cầm mật khẩu; Security BCP khuyến nghị bỏ |

**Cách chọn:** hỏi (1) có người dùng không, (2) client chạy ở đâu, giữ được bí mật không (máy chủ: có; trình duyệt và di động: không), (3) cần thu hồi tức thì không. Mặc định: **code + PKCE** cho người dùng, **Client Credentials** cho máy.

## Tự kiểm tra

1. PKCE chặn việc gì, và nó hoạt động thế nào?
2. Vì sao `redirect_uri` phải khớp chính xác?
3. Refresh token xoay vòng phát hiện đánh cắp bằng cách nào, và cái giá là gì?
4. OIDC thêm gì lên OAuth 2.0?

<details>
<summary>Đáp án ngắn</summary>

1. Chặn kẻ có code bị chặn đổi lấy token; máy chủ so băm `code_verifier` với `code_challenge`.
2. Khớp theo tiền tố có thể chuyển code sang địa chỉ do kẻ tấn công kiểm soát (Lab 5.2).
3. Refresh token cũ xuất hiện lần nữa thì thu hồi cả chuỗi; người dùng thật bị đăng xuất.
4. `id_token`, `nonce`, discovery: nói ai đã đăng nhập.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 4.1 và 4.2 (.NET 10, Kestrel `127.0.0.1`; máy chủ ủy quyền tối giản do lab dựng, **không** phải máy chủ hoàn chỉnh).
**Chỉ qua agent (⚠):** RFC 6749 (OAuth 2.0), 7636 (PKCE), 9700 (Security BCP: `redirect_uri` khớp chính xác, PKCE, bỏ Implicit và Password, xoay vòng refresh token), RFC 7009 và 7662 (thu hồi, introspection), OpenID Connect Core (`id_token`, `nonce`), RFC 8628 (Device), bản nháp BFF.
**Chưa kiểm:** `id_token`, `nonce`, discovery, introspection, thu hồi, Device, BFF chưa chạy; trang đăng nhập thật và đồng ý (consent) lab bỏ qua (người dùng giả luôn đăng nhập).
