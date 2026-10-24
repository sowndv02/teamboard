# Part 5 — Production, Security & Operations (Identity)

> [!NOTE]
> **Mục tiêu:** biết các tấn công phổ biến vào xác thực, thấy chúng **thành công** trên bản cố ý yếu và **bị chặn** trên bản đúng, rồi nắm quản lý khoá, chống brute force, quan sát và chi phí.
> **Cần biết trước:** [Part 4](part-4-advanced.md).
> **Thời lượng:** ~7 giờ · **Đầu ra kiểm được:** tái hiện `alg=none`, key confusion, replay code và dùng lại refresh token trên lab của mình, và chỉ ra điều kiện nào chặn từng cái.
> [← Part 4](part-4-advanced.md) · [Part 6 →](part-6-projects.md)

> [!WARNING]
> Mọi tấn công dưới đây chỉ nhằm vào dịch vụ **do lab dựng** trên `127.0.0.1` với khoá sinh ngẫu nhiên. Đừng dùng cho hệ thống không phải của bạn.

## L1 What — Bảng tấn công

#### `alg=none`
**Là gì:** kẻ tấn công đặt `alg` thành `none` và bỏ chữ ký; thư viện tin header sẽ chấp nhận token không ký.
**Use case:** đổi `role` thành `admin` rồi gửi.

#### Key confusion (RS256 → HS256)
**Là gì:** đổi `alg` thành HS256 rồi ký HMAC bằng **khoá công khai** (ai cũng có); bên kiểm yếu dùng cùng khoá công khai làm khoá HMAC nên chữ ký "đúng".

#### `redirect_uri` giả và đánh cắp code
**Là gì:** lừa máy chủ ủy quyền chuyển `code` tới địa chỉ do kẻ tấn công kiểm soát, hoặc đổi code đã chặn được.

#### Replay (phát lại)
**Là gì:** dùng lại một `code` hoặc refresh token đã dùng.

#### CSRF đăng nhập
**Là gì:** ép trình duyệt nạn nhân hoàn tất luồng bằng `code` của kẻ tấn công, để nạn nhân đăng nhập vào tài khoản của hắn. `state` chặn việc này.

#### Brute force và liệt kê tài khoản
**Là gì:** thử mật khẩu hàng loạt; hoặc phân biệt "không có người dùng" với "sai mật khẩu" qua thông báo hoặc thời gian.

## L2 Why — Vì sao đều thành công trên bản yếu

Mỗi tấn công khai thác một **điều kiện kiểm tra bị thiếu**: tin `alg` từ token, tin khoá theo kiểu chung, khớp `redirect_uri` theo tiền tố, không đánh dấu code đã dùng, không có `state`, thông báo khác nhau theo lý do. Hiểu điều kiện nào thiếu thì mới biết phòng đúng chỗ.

## L3 How — Điều kiện chặn từng tấn công

| Tấn công | Điều kiện chặn | Nơi chặn |
|---|---|---|
| `alg=none` | danh sách thuật toán cho phép cố định (chỉ RS256) | bên kiểm token |
| Key confusion | `alg` phải khớp loại khoá; không dùng khoá công khai làm khoá HMAC | bên kiểm token |
| Đánh cắp code | PKCE: không có `code_verifier` thì không đổi được | máy chủ ủy quyền |
| `redirect_uri` giả | khớp **chính xác** với giá trị đã đăng ký | máy chủ ủy quyền |
| Replay code | code dùng một lần, hết hạn nhanh | máy chủ ủy quyền |
| Dùng lại refresh token | xoay vòng và phát hiện dùng lại, thu hồi cả chuỗi | máy chủ ủy quyền |
| CSRF đăng nhập | client sinh `state` ngẫu nhiên, lưu, so khi callback | client |
| Brute force | khoá tạm, giới hạn tốc độ, băm chậm | máy chủ ủy quyền |
| Liệt kê tài khoản | một thông báo chung, thời gian trả lời như nhau (băm giả) | máy chủ ủy quyền |

## L4 Hands-on — Lab Part 5

Môi trường: như Part 0. Mỗi tấn công có hai kết quả: **bản YẾU** (cố ý tắt điều kiện) và **bản ĐÚNG**.

#### Lab 5.1 — Tấn công JWT
**Đầu ra thật:**
```text
token hợp lệ (đối chứng)
   bản YẾU : CHẤP NHẬN (chấp nhận (alg=RS256))
   bản ĐÚNG: CHẤP NHẬN (hợp lệ)
alg=none, bỏ chữ ký, role=admin
   bản YẾU : CHẤP NHẬN (chấp nhận (alg=none))
   bản ĐÚNG: từ chối (thuật toán 'none' không nằm trong danh sách cho phép (chỉ RS256))
key confusion: đổi alg thành HS256, ký HMAC bằng KHOÁ CÔNG KHAI, role=admin
   bản YẾU : CHẤP NHẬN (chấp nhận (alg=HS256))
   bản ĐÚNG: từ chối (thuật toán 'HS256' không nằm trong danh sách cho phép (chỉ RS256))
alg=none kèm kid (một số thư viện coi trọng kid hơn alg)
   bản YẾU : CHẤP NHẬN (chấp nhận (alg=none))
   bản ĐÚNG: từ chối (thuật toán 'none' không nằm trong danh sách cho phép (chỉ RS256))
hết hạn
   bản YẾU : từ chối (token đã hết hạn)
   bản ĐÚNG: từ chối (token đã hết hạn)
```
**Giải thích:** bản yếu tin `alg` trong token nên hai kiểu giả mạo qua được; bản đúng chỉ cho RS256 nên từ chối. Hạn `exp` bản yếu vẫn kiểm: lỗi nằm ở **thuật toán**, không ở thời hạn.

#### Lab 5.2 — Tấn công luồng OAuth
**Đầu ra thật:**
```text
-- bản YẾU (không PKCE, redirect_uri theo tiền tố, code dùng lại được)
   1) kẻ tấn công đổi CODE ĐÃ CHẶN ĐƯỢC (không có verifier đúng): HTTP 200 -> LẤY ĐƯỢC TOKEN
   2) dùng lại cùng một code: lần 1 HTTP 200, lần 2 HTTP 200 -> REPLAY THÀNH CÔNG
   3) redirect_uri tiền tố hợp lệ nhưng có phần thêm (.../callback.evil/x): HTTP 302 -> MÁY CHỦ CHUYỂN CODE TỚI http://127.0.0.1:18701/callback.evil/x
   3) redirect_uri miền khác (http://evil.test/cb): HTTP 400 -> từ chối
-- bản ĐÚNG
   1) kẻ tấn công đổi CODE ĐÃ CHẶN ĐƯỢC (không có verifier đúng): HTTP 400 -> bị chặn: invalid_grant (code_verifier sai)
   2) dùng lại cùng một code: lần 1 HTTP 200, lần 2 HTTP 400 -> invalid_grant (code đã được dùng)
   3) redirect_uri tiền tố hợp lệ nhưng có phần thêm (.../callback.evil/x): HTTP 400 -> từ chối
   3) redirect_uri miền khác (http://evil.test/cb): HTTP 400 -> từ chối
-- 4) CSRF đăng nhập: kẻ tấn công ép nạn nhân mở callback với state của hắn; client so state nhận được ('state-của-kẻ-tấn-công') với state đã lưu ('state-của-tôi'): KHÔNG KHỚP -> client phải từ chối
```
**Giải thích:** ba tấn công thành công trên bản yếu và bị chặn trên bản đúng. Dòng "miền khác bị từ chối cả hai bản" cho thấy kiểm tiền tố **trông an toàn** nhưng vẫn để lọt `.../callback.evil/x`. CSRF được chặn ở **client**, không phải máy chủ: lab chỉ mô phỏng phép so `state`.

#### Lab 5.3 — Đăng nhập: thông báo chung, khoá tạm, thời gian trả lời
**Đầu ra thật:**
```text
người dùng không tồn tại -> 'invalid_credentials'; alice sai mật khẩu -> 'invalid_credentials'  (cùng một thông báo)
sau 5 lần sai, alice nhập ĐÚNG mật khẩu -> 'invalid_credentials' (đang khoá tạm 30 giây)
thời gian trả lời (trung bình 10 lần, 100,000 vòng): người dùng không tồn tại, KHÔNG băm giả: 0.0 ms | CÓ băm giả: 44.0 ms | tồn tại, sai mật khẩu: 43.6 ms
```
**Giải thích:** thông báo giống nhau chưa đủ: không băm giả thì người không tồn tại trả lời tức thì (0,0 ms) so với ~44 ms, nên đo thời gian vẫn liệt kê được tài khoản. Băm giả đưa hai trường hợp về cùng mức (44,0 và 43,6 ms). Khoá tạm cũng chặn cả mật khẩu đúng trong lúc khoá, **đổi lại** kẻ tấn công có thể cố ý khoá người dùng thật (DoS tài khoản).

## L5 Troubleshooting — Triệu chứng và nguyên nhân gốc

| Triệu chứng | Nguyên nhân thường gặp | Cách kiểm |
|---|---|---|
| Token giả có `role=admin` được chấp nhận | bên kiểm tin `alg` trong token | đọc cấu hình: danh sách thuật toán cho phép phải cố định |
| Một code đổi được hai lần | không đánh dấu đã dùng | thử đổi hai lần liên tiếp (Lab 5.2 bước 2) |
| Code gửi tới địa chỉ lạ | kiểm `redirect_uri` theo tiền tố | thử `.../callback.evil/x` |
| Biết được tài khoản nào tồn tại | thông báo hoặc thời gian khác nhau | so thông báo và đo thời gian (Lab 5.3) |
| Người dùng thật bị khoá liên tục | khoá tạm bị lợi dụng | xem nguồn IP, đặt giới hạn theo cặp (tài khoản, nguồn) |
| Token xuất hiện trong log | log cả header hay URL | tìm `Authorization`, `code=` trong log |

## L6 Production — Cân nhắc khi chạy thật

- **Quản lý khoá ký (⚠):** khoá riêng không nằm trong repo, không trong biến môi trường lâu dài; dùng KMS, HSM hoặc Secrets Manager, có xoay khoá định kỳ và quy trình khi nghi lộ (Part 3). Lab này sinh khoá lúc chạy chỉ để học.
- **Giới hạn tốc độ:** theo tài khoản và theo nguồn, ở `/token` và đăng nhập (Lab 5.3 chỉ có khoá tạm).
- **Băm mật khẩu:** số vòng theo OWASP hiện hành (⚠, Part 2), có đường nâng cấp băm cũ.
- **Quan sát:** đếm đăng nhập thất bại, `invalid_grant`, phát hiện dùng lại refresh token, lỗi kiểm token theo lý do; **không** log token, code, mật khẩu, `code_verifier`.
- **Phụ thuộc:** thư viện JWT và OAuth có CVE thường xuyên; quét lỗ hổng theo `COMMANDS.md`, cập nhật đều.
- **Tuân thủ:** lưu thông tin cá nhân có luật riêng (GDPR...); tự xây khó đáp ứng so với dịch vụ quản lý.

## L7 Architecture — Chi phí và đánh đổi: tự xây hay dịch vụ quản lý

| Tiêu chí | Tự xây trên BCL | Dịch vụ quản lý (ví dụ Cognito) |
|---|---|---|
| Chi phí tiền | máy chủ, CSDL, công sức | thường theo số người dùng hoạt động hằng tháng (⚠, tra giá hiện hành) |
| Chi phí công sức | cao: giao thức, xoay khoá, tấn công, vá | thấp: cấu hình và tích hợp |
| Tính năng (MFA, federation, passkeys) | phải tự làm | thường có sẵn |
| Kiểm soát và học | cao; học được từ bên trong | thấp hơn; che giấu cơ chế |
| Rủi ro sai lỗi bảo mật | thuộc về bạn | chia sẻ, đội chuyên trách |

**Cách chọn:** **học thì tự xây** (đúng ADR 0006), **giao sản phẩm thì dịch vụ quản lý** hoặc máy chủ đã kiểm chứng. Điều kiện xem lại quyết định: có người dùng thật, cần MFA hoặc federation, hoặc đội không còn thời gian vá.

## Tự kiểm tra

1. Hai tấn công nhắm vào header `alg` là gì và điều kiện nào chặn cả hai?
2. Vì sao kiểm `redirect_uri` theo tiền tố là sai?
3. Vì sao thông báo lỗi chung chưa đủ để chống liệt kê tài khoản?
4. Khoá tạm tài khoản có thể bị lợi dụng thế nào?

<details>
<summary>Đáp án ngắn</summary>

1. `alg=none` và key confusion; danh sách thuật toán cho phép cố định.
2. `.../callback.evil/x` thỏa tiền tố nhưng là địa chỉ khác (Lab 5.2).
3. Thời gian trả lời vẫn khác (0,0 ms so với ~44 ms) nếu không băm giả.
4. Kẻ tấn công cố ý nhập sai để khoá người dùng thật.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 5.1, 5.2, 5.3 (.NET 10, Kestrel `127.0.0.1`, khoá và bí mật sinh lúc chạy; số đo thời gian tương đối).
**Chỉ qua agent (⚠):** RFC 8725 (JWT BCP: `alg=none`, key confusion), RFC 9700 (Security BCP: `redirect_uri` khớp chính xác, PKCE, replay code), OWASP Authentication, Password Storage và JWT cheat sheets; giá và tính năng Cognito (tra trang giá hiện hành, chưa đưa con số).
**Chưa kiểm:** KMS, HSM, Secrets Manager, giới hạn tốc độ thật, lỗi CSRF ở client thật (chỉ mô phỏng phép so `state`), tấn công timing qua mạng thật (lab đo trong tiến trình).
