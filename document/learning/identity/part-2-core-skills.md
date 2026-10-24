# Part 2 — Core Skills (Identity)

> [!NOTE]
> **Mục tiêu:** làm đúng hai việc nền của một hệ định danh: **băm mật khẩu** và **dựng, kiểm JWT** bằng BCL.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** đo thời gian băm theo số vòng; ký và kiểm một JWT RS256; chỉ ra 8 token bị từ chối vì lý do gì.
> [← Part 1](part-1-fundamentals.md) · [Part 3 →](part-3-multi-component.md)

## L1 What — Các khái niệm

#### Băm mật khẩu (password hashing)
**Là gì:** biến mật khẩu thành một giá trị một chiều, chỉ lưu giá trị đó. Khác băm thường (SHA-256): băm mật khẩu cố ý **chậm** và có **salt**.
**Ví dụ đời thường:** xay thịt thành viên: có viên thì kiểm được "có đúng thịt đó không", nhưng không ghép lại thành miếng thịt.
**Use case:** lưu mật khẩu người dùng ở `identity-service` (I1).

#### Salt
**Là gì:** chuỗi ngẫu nhiên riêng cho mỗi mật khẩu, trộn vào trước khi băm; lưu công khai cùng băm. Hai người cùng mật khẩu vẫn ra băm khác nhau.
**Use case:** chặn bảng băm dựng sẵn và việc bẻ hàng loạt một lượt.

#### Số vòng (work factor)
**Là gì:** số lần lặp của hàm băm; tăng số vòng làm mỗi lần đoán chậm hơn. Là núm vặn giữa độ an toàn và thời gian đăng nhập.

#### Ký (signature)
**Là gì:** `HMAC` (khoá bí mật chung) hoặc `RSA` (khoá riêng ký, khoá công khai kiểm) trên chuỗi `header.payload`; sửa một ký tự là chữ ký sai.
**Use case:** access token của dự án (I2).

#### Kiểm claim (claim validation)
**Là gì:** sau khi chữ ký đúng, bên nhận còn phải kiểm **nội dung**: ai cấp (`iss`), dành cho ai (`aud`), còn hạn (`exp`), đã có hiệu lực (`nbf`).

## L2 Why — Vì sao làm như vậy

- **Băm chậm có salt:** nếu CSDL bị lộ, kẻ tấn công phải đoán từng mật khẩu một và mỗi lần đoán tốn thời gian (Lab 2.1: 1.000 vòng đoán được khoảng 2.288 lần mỗi giây, 600.000 vòng chỉ khoảng 4 lần mỗi giây trên một luồng). Hàm băm nhanh (SHA-256 đơn thuần) làm điều ngược lại.
- **Chữ ký không đủ:** token ký đúng nhưng của dịch vụ khác (`aud` sai) hoặc đã hết hạn vẫn là token **thật**, chỉ không dành cho bạn lúc này. Phải kiểm cả hai.
- **Hằng thời gian:** so sánh băm bằng `==` dừng ở byte khác đầu tiên, nên thời gian trả lời tiết lộ mức khớp; `FixedTimeEquals` luôn đi hết.

## L3 How — Cơ chế bên trong

### PBKDF2
`DK = PBKDF2(HMAC-SHA256, mật khẩu, salt, số vòng, độ dài)`. .NET cung cấp `Rfc2898DeriveBytes.Pbkdf2`. Lưu kèm **số vòng và salt** cùng băm (ví dụ `v1.<vòng>.<salt>.<băm>`) để sau này nâng số vòng mà vẫn kiểm được băm cũ rồi băm lại khi người dùng đăng nhập.

| Lựa chọn | Điểm mạnh | Điểm yếu |
|---|---|---|
| PBKDF2-HMAC-SHA256 | có sẵn trong BCL, chuẩn FIPS | chỉ tốn CPU, GPU bẻ rẻ hơn |
| bcrypt, scrypt, Argon2id | tốn bộ nhớ, khó song song hoá | cần thư viện ngoài (BCL không có) |

Dự án dùng PBKDF2 vì ADR 0006 chỉ cho BCL; Argon2id là hướng tốt hơn khi được phép thêm thư viện (⚠, chưa chạy).

### Dựng JWT bằng tay
1. Header `{"alg":"RS256","typ":"JWT","kid":"k1"}`, payload các claim; mỗi phần JSON → UTF-8 → Base64URL (không có `=`, `+` thành `-`, `/` thành `_`).
2. Ký `header.payload` bằng `RSA.SignData(..., SHA256, Pkcs1)`.
3. Nối `header.payload.chữ-ký-Base64URL`.

### Kiểm JWT: thứ tự bắt buộc
1. Tách đúng 3 phần.
2. **Thuật toán nằm trong danh sách cho phép đã cấu hình** (đừng tin `alg` trong token; Part 5).
3. Chọn khoá theo `kid`, kiểm chữ ký.
4. Kiểm `iss`, `aud` đúng giá trị, `exp` chưa qua, `nbf` đã tới.
5. Chỉ **sau cả bốn bước** mới tin claim (`role`, `scope`).

## L4 Hands-on — Lab Part 2

Môi trường: như Part 0 (khoá RSA 2048 sinh lúc chạy, mật khẩu thử là chuỗi giả).

#### Lab 2.1 — Băm mật khẩu PBKDF2 và đo thời gian
**Đầu ra thật:**
```text
    1,000 vòng:      3.2 ms mỗi lần băm
   10,000 vòng:      5.0 ms mỗi lần băm
  100,000 vòng:     45.9 ms mỗi lần băm
  600,000 vòng:    276.0 ms mỗi lần băm
1,000,000 vòng:    418.1 ms mỗi lần băm
cùng mật khẩu, hai salt khác nhau: băm giống nhau? False
cùng mật khẩu, cùng salt: băm giống nhau? True; so sánh hằng thời gian: True
mật khẩu sai, cùng salt: False
đoán mật khẩu một luồng, 1,000 vòng: ~2288 lần đoán mỗi giây
đoán mật khẩu một luồng, 600,000 vòng: ~4 lần đoán mỗi giây
```
**Giải thích:** thời gian tăng gần tuyến tính theo số vòng (nửa triệu vòng ≈ 0,28 giây); salt khác thì băm khác; đoán một luồng chậm hơn ~570 lần ở 600.000 vòng. Số liệu tương đối trên máy này; kẻ tấn công có GPU nhanh hơn nhiều.

#### Lab 2.2 — Tự ký và kiểm JWT RS256
**Đầu ra thật:**
```text
token hợp lệ                                 -> CHẤP NHẬN: hợp lệ
hết hạn (exp trong quá khứ)                  -> TỪ CHỐI: token đã hết hạn
chưa có hiệu lực (nbf trong tương lai)       -> TỪ CHỐI: token chưa có hiệu lực (nbf)
aud sai (dành cho dịch vụ khác)              -> TỪ CHỐI: aud sai
iss sai                                      -> TỪ CHỐI: iss sai
ký bằng khoá khác (kẻ giả mạo)               -> TỪ CHỐI: chữ ký sai
sửa claim role member -> admin, giữ chữ ký   -> TỪ CHỐI: chữ ký sai
kid không tồn tại                            -> TỪ CHỐI: không có khoá cho kid 'k9'
-- đối chiếu JsonWebTokenHandler (ValidAlgorithms=RS256, ClockSkew=0): token hợp lệ của lab tự ký -> True; token sửa claim -> False
-- ClockSkew mặc định của TokenValidationParameters: 5 phút
```
**Giải thích:** tám trường hợp, chỉ một được chấp nhận. Bản tự dựng và `JsonWebTokenHandler` của Microsoft cho cùng kết quả ở hai trường hợp đối chiếu. Lưu ý `ClockSkew` mặc định **5 phút** làm token hết hạn vẫn qua thêm 5 phút nếu bạn không đặt lại.

## L5 Troubleshooting — Triệu chứng và nguyên nhân gốc

| Triệu chứng | Nguyên nhân thường gặp | Cách kiểm |
|---|---|---|
| Token hợp lệ bị 401 "chữ ký sai" | khoá sai, hoặc `kid` trỏ khoá cũ | so `kid` trong header với khoá đang dùng |
| 401 dù chữ ký đúng | `aud` hoặc `iss` lệch giữa nơi cấp và nơi kiểm | in claim đã giải mã, so với cấu hình |
| Token hết hạn vẫn được chấp nhận vài phút | `ClockSkew` mặc định 5 phút | đặt `ClockSkew` nhỏ, ghi rõ lý do |
| Lỗi ngẫu nhiên "chưa có hiệu lực" | đồng hồ hai máy lệch | so giờ hai máy, cho độ lệch nhỏ có chủ đích |
| Đăng nhập chậm bất thường | số vòng quá cao so với CPU | đo thời gian băm một lần (Lab 2.1) |
| Băm cũ không kiểm được sau khi nâng số vòng | không lưu số vòng cùng băm | lưu phiên bản và số vòng trong chuỗi băm |

## Tự kiểm tra

1. Vì sao lưu băm nhanh (SHA-256 đơn thuần) của mật khẩu là sai?
2. Salt có cần giữ bí mật không? Nó chặn điều gì?
3. Kể 4 điều phải kiểm sau khi chữ ký đúng.
4. `ClockSkew` mặc định là bao nhiêu và có hệ quả gì?

<details>
<summary>Đáp án ngắn</summary>

1. Kẻ tấn công thử hàng tỷ lần đoán mỗi giây; băm mật khẩu phải chậm.
2. Không cần bí mật; chặn bảng dựng sẵn và bẻ hàng loạt (cùng mật khẩu ra băm khác).
3. `iss`, `aud`, `exp`, `nbf`.
4. 5 phút; token hết hạn vẫn được chấp nhận thêm 5 phút nếu không đặt lại.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 2.1 và 2.2 (.NET 10; số đo tương đối; đối chiếu `JsonWebTokenHandler` từ gói JwtBearer).
**Đã tự mở:** [ADR 0006](../../architecture/adr/0006-identity-self-built.md) (chỉ BCL).
**Chỉ qua agent (⚠):** OWASP Password Storage Cheat Sheet (PBKDF2-HMAC-SHA256 với 600.000 vòng, Argon2id là lựa chọn đầu; số vòng đổi theo thời gian, cần mở lại bản hiện hành); RFC 7515 (định dạng JWS); `ClockSkew` mặc định 5 phút (đã thấy trong đầu ra Lab 2.2).
**Chưa kiểm:** Argon2id, bcrypt, scrypt chưa chạy; nâng số vòng khi đăng nhập (rehash) mới ở mức mô tả.
