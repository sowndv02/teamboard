# Part 6 — Projects (Identity)

> [!NOTE]
> **Mục tiêu:** đối chiếu các giai đoạn **I1 đến I9** và **12 điểm bảo mật** của [identity-learning.md](../../architecture/identity-learning.md) với bằng chứng từ lab, nhìn ra chỗ **chưa có bằng chứng**, và có 3 dự án nhỏ để tự làm.
> **Cần biết trước:** [Part 1 đến 5](README.md).
> **Thời lượng:** ~8 giờ · **Đầu ra kiểm được:** mỗi giai đoạn I1 đến I9 có trạng thái "đã thấy trong lab" hoặc "chưa kiểm" kèm lý do; làm được ít nhất 2 dự án.
> [← Part 5](part-5-production-security-ops.md) · [Part 7 →](part-7-troubleshooting-mastery.md)
>
> **Phạm vi:** các dự án làm **ngoài repo** (thư mục thử riêng). Lộ trình **không** sửa `src/`, ADR 0006 hay `identity-learning.md`; chỗ lệch hoặc chưa chốt chỉ **báo** ở L5.

## L1 What — Ba dự án

#### Dự án A — Người dùng và JWT (I1, I2)
Một dịch vụ nhỏ có đăng ký, đăng nhập (PBKDF2, thông báo chung, khoá tạm), phát JWT RS256 và một API kiểm token. **Xong khi:** các trường hợp của Lab 2.2 và Lab 5.3 đều cho kết quả như đầu ra mẫu trên dịch vụ của bạn.

#### Dự án B — Khoá và refresh token (I3, I4)
Thêm `kid`, JWKS, xoay khoá và refresh token xoay vòng có phát hiện dùng lại. **Xong khi:** xoay khoá không làm hỏng token còn hạn (Lab 3.1) và dùng lại refresh token thu hồi cả chuỗi (Lab 4.2).

#### Dự án C — OAuth và máy gọi máy (I5, I6)
Thêm `/authorize` và `/token` với code + PKCE, đăng ký client, Client Credentials. **Xong khi:** các tấn công của Lab 5.2 bị chặn trên dịch vụ của bạn **và** bạn thấy được chúng thành công khi tự tắt từng điều kiện.

## L4 Hands-on — Độ phủ I1 đến I9 so với bằng chứng

Trạng thái chỉ có hai giá trị: **đã thấy trong lab** (có đầu ra thật ở Part nêu) hoặc **chưa kiểm** (không có đầu ra).

| GĐ | Nội dung | Trạng thái | Bằng chứng hoặc lý do |
|---|---|---|---|
| I1 | người dùng, mật khẩu | một phần | băm, salt, hằng thời gian: Lab 2.1; thông báo chung, khoá tạm, băm giả: Lab 5.3; chưa có đăng ký thật |
| I2 | phát và kiểm JWT | đã thấy | Lab 2.2, 5.1 (từ chối `none`, HS256, sai `aud`, hết hạn, sửa claim) |
| I3 | khoá, JWKS, xoay | đã thấy | Lab 3.1 (cache, xoay, khoảng tối thiểu); chưa xuất JWKS từ máy chủ thật có `/.well-known/jwks.json` đầy đủ |
| I4 | refresh token | đã thấy | Lab 4.2 (xoay vòng, dùng lại, thu hồi chuỗi); chưa thu hồi khi đăng xuất |
| I5 | code + PKCE | đã thấy | Lab 4.1, 5.2 (verifier sai, `redirect_uri`, replay, `state`); lab không có trang đăng nhập thật |
| I6 | Client Credentials | một phần | Lab 3.2 (scope, sai bí mật); chưa kiểm "token máy không dùng như token người" |
| I7 | OpenID Connect | chưa kiểm | không có `id_token`, `nonce`, userinfo, discovery trong lab |
| I8 | ủy quyền | chưa kiểm | chỉ có kiểm `scope` ở một endpoint; chưa có policy, IDOR, thu hồi hay introspection |
| I9 | tích hợp | chưa kiểm ở đây | gateway kiểm JWT đã có ở [API Gateway Lab 4.2](../api-gateway/part-4-advanced.md); luồng đầu-cuối chưa dựng |

### 12 điểm bảo mật của identity-learning.md

| Điểm | Có bằng chứng ở | Ghi chú |
|---|---|---|
| Lưu mật khẩu | Lab 2.1 | số vòng theo OWASP hiện hành cần mở lại (⚠) |
| So sánh bí mật | Lab 2.1 | `FixedTimeEquals` dùng trong lab |
| Giả mạo token | Lab 2.2, 5.1 | `alg=none`, sửa claim, sai `aud`, hết hạn |
| Tráo khoá (key confusion) | Lab 5.1 | bản yếu chấp nhận, bản đúng từ chối |
| Chặn mã uỷ quyền | Lab 4.1, 5.2 | PKCE, code dùng một lần |
| Chuyển hướng độc hại | Lab 5.2 | khớp chính xác; tiền tố để lọt `.../callback.evil/x` |
| CSRF và replay đăng nhập | Lab 5.2 | `state` chỉ mô phỏng phép so; `nonce` (OIDC) chưa kiểm |
| Lộ refresh token | Lab 4.2 | lưu băm có trong mã lab, chưa có test riêng |
| Brute force | Lab 5.3 | khoá tạm có; giới hạn tốc độ theo nguồn chưa có |
| Lộ thông tin qua lỗi và log | một phần | thông báo chung có (Lab 5.3); `problem+json` và quét log chưa làm |
| Khoá ký bị lộ | không | KMS, Secrets Manager chỉ ở mức khái niệm (Part 5) |
| Scope tối thiểu, kiểm quyền từng service | một phần | scope: Lab 3.2; quyền theo tài nguyên (IDOR) chưa kiểm |

## L5 Troubleshooting — Điểm lệch, thiếu và chưa chốt

Các điểm dưới đây **chỉ báo**, không sửa tài liệu dự án; quyết định thuộc về bạn.

| Điểm | Quan sát | Gợi ý |
|---|---|---|
| Thiếu bằng chứng I7 và I8 | lab không chạm OIDC, policy, IDOR | thêm lab riêng (issue mới) trước khi coi hai giai đoạn này là đã học |
| `invalid_client` | máy chủ dựng trả HTTP 400; RFC 6749 cho phép 401 khi client xác thực bằng header (⚠ chưa đối chiếu bản gốc) | khi viết thật, đối chiếu RFC 6749 mục 5.2 |
| `ClockSkew` mặc định 5 phút | thư viện Microsoft cho token hết hạn qua thêm 5 phút (Lab 2.2) | đặt giá trị có chủ đích và ghi lý do |
| Thu hồi tức thì | JWT không thu hồi được trước hạn (Part 4) | chốt: chấp nhận trễ bằng thời hạn access token hay làm introspection |
| Trang đăng nhập cho I5 và I7 | `identity-learning.md` mục 6 còn `TODO`: làm trang HTML tối giản hay hoãn tới khi có frontend | lab Part 4 dùng người dùng giả; quyết định thuộc dự án |
| Thuật toán ký và xoay khoá | mục 6 còn mở: RSA hay ECDSA, chính sách xoay | lab dùng RSA 2048 và xoay thủ công; chưa so ECDSA |

## Tự kiểm tra

1. Giai đoạn nào trong I1 đến I9 chưa có bằng chứng từ lab, và vì sao đừng coi là đã học?
2. Vì sao dự án C nên cho bạn tự tắt từng điều kiện bảo vệ rồi bật lại?
3. Điểm nào của `identity-learning.md` còn chờ quyết định?

<details>
<summary>Đáp án ngắn</summary>

1. I7, I8 (và I9 ở đây); chỉ coi là đã học khi tự chạy và thấy kết quả.
2. Để thấy tấn công thành công khi thiếu điều kiện và bị chặn khi đủ, thay vì chỉ tin lời mô tả.
3. Trang đăng nhập cho I5, I7; RSA hay ECDSA; chính sách xoay khoá.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** các lab ở Part 1 đến 5, dùng làm bằng chứng cho bảng độ phủ (đầu ra có ở từng Part).
**Đã tự mở:** [identity-learning.md](../../architecture/identity-learning.md), [ADR 0006](../../architecture/adr/0006-identity-self-built.md), [API Gateway Part 4](../api-gateway/part-4-advanced.md).
**Chỉ qua agent (⚠):** mã `invalid_client` theo RFC 6749 mục 5.2; số vòng PBKDF2 theo OWASP hiện hành.
**Chưa kiểm:** I7, I8, I9; đăng ký người dùng thật; xuất JWKS qua `/.well-known/jwks.json` đầy đủ; thu hồi khi đăng xuất.
