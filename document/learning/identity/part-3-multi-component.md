# Part 3 — Multi-component (Identity)

> [!NOTE]
> **Mục tiêu:** nhiều service cùng kiểm token mà không chia sẻ khoá bí mật: `kid`, JWKS, xoay khoá, và xác thực máy với máy bằng Client Credentials.
> **Cần biết trước:** [Part 2](part-2-core-skills.md); [API Gateway Part 4](../api-gateway/part-4-advanced.md) (JWT ở gateway).
> **Thời lượng:** ~6 giờ · **Đầu ra kiểm được:** xoay khoá mà token cũ vẫn hợp lệ trong giai đoạn chuyển; giải thích vì sao kiểm lại JWKS cần giới hạn tần suất.
> [← Part 2](part-2-core-skills.md) · [Part 4 →](part-4-advanced.md)

## L1 What — Các khái niệm

#### `kid` (key id)
**Là gì:** nhãn khoá trong header JWT, cho bên kiểm biết dùng khoá công khai nào.
**Use case:** nhiều khoá cùng tồn tại khi xoay khoá.

#### JWKS (JSON Web Key Set)
**Là gì:** tài liệu JSON công khai liệt kê các khoá công khai của nơi cấp, mỗi khoá có `kid`. Service kiểm tải về và cache.
**Ví dụ đời thường:** bảng niêm yết mẫu chữ ký của ngân hàng ở quầy: ai cũng đối chiếu được, không ai giả được chữ ký.
**Use case:** `board-service` và `task-service` kiểm token của `identity-service` mà không có khoá ký.

#### Xoay khoá (key rotation)
**Là gì:** thay khoá ký theo định kỳ hoặc khi nghi lộ, giữ khoá cũ trong JWKS đủ lâu để token cũ còn kiểm được.

#### Client Credentials
**Là gì:** luồng OAuth 2.0 cho **máy gọi máy**: dịch vụ đưa `client_id` và `client_secret`, nhận access token có `scope`, không có người dùng.
**Use case:** `task-service` gọi `board-service` bằng danh tính của chính nó.

## L2 Why — Vì sao cần

- Nếu mọi dịch vụ chia sẻ **khoá HMAC** thì dịch vụ nào bị chiếm cũng ký được token giả. Với khoá công khai, dịch vụ chỉ **kiểm** được.
- Khoá không thể dùng mãi: lộ thì phải thay; định kỳ thay giảm thời gian lộ có hại. Thay mà token cũ chết hàng loạt thì không dùng được: cần giai đoạn hai khoá cùng hiệu lực.
- Dịch vụ gọi dịch vụ không có "người dùng": cần danh tính máy riêng với quyền tối thiểu.

## L3 How — Cơ chế bên trong

### Xoay khoá với JWKS
1. Tạo khoá mới `k2`, thêm vào JWKS (lúc này JWKS có `k1`, `k2`).
2. Đợi mọi service cache JWKS mới (thời hạn cache).
3. Chuyển nơi cấp sang ký bằng `k2`.
4. Giữ `k1` trong JWKS tới khi token ký bằng `k1` hết hạn hết (thời hạn token + độ lệch).
5. Bỏ `k1` khỏi JWKS.

### Cache JWKS và gặp `kid` lạ
Bên kiểm cache JWKS. Gặp `kid` chưa có thì có hai lựa chọn: **tải lại ngay** (token ký khoá mới qua được tức thì, nhưng kẻ tấn công gửi token có `kid` ngẫu nhiên biến service thành máy khuếch đại gọi vào máy chủ JWKS) hoặc **chỉ tải lại tối đa mỗi N giây** (an toàn hơn, đổi lại token ký khoá mới bị từ chối tới khi hết N giây). Lab 3.1 đo cả hai.

### Client Credentials
`POST /token` với `grant_type=client_credentials`, `client_id`, `client_secret`, `scope`. Máy chủ kiểm bí mật, kiểm `scope` không vượt quyền đã cấp cho client, rồi phát token có `sub` là chính client. Không có refresh token (có bí mật rồi thì xin token mới).

## L4 Hands-on — Lab Part 3

Môi trường: như Part 0 (Kestrel `127.0.0.1`; khoá sinh lúc chạy; `client_secret` ngẫu nhiên lúc chạy và không in ra).

#### Lab 3.1 — JWKS, `kid` và xoay khoá
**Đầu ra thật:**
```text
1) token k1: hợp lệ; số lần tải JWKS: 1
2) thêm 1000 lần kiểm: số lần tải JWKS vẫn là 1 (có cache)
3) sau khi thêm k2 (JWKS có k1 và k2): token mới k2 -> không có khoá cho kid 'k2' (không tải lại vì chưa qua khoảng tối thiểu 5 giây giữa hai lần tải; tổng 1 lần); token cũ k1 -> hợp lệ
3b) khoảng tối thiểu giữa hai lần tải là 300 ms; xoay tiếp sang k3: kiểm token k3 ngay -> không có khoá cho kid 'k3'; đợi 350 ms rồi kiểm lại -> hợp lệ (tổng 2 lần tải)
4) sau khi bỏ k1 khỏi JWKS: token cũ k1 -> không có khoá cho kid 'k1'; token k2 -> hợp lệ
5) 200 token có kid ngẫu nhiên, refetch mỗi khi gặp kid lạ (không giới hạn): thêm 200 lần gọi tới máy chủ JWKS
5) 200 token có kid ngẫu nhiên, refetch tối đa mỗi 5 giây: thêm 0 lần gọi tới máy chủ JWKS
```
**Giải thích:** cache cho 1.000 lần kiểm chỉ 1 lần tải. Token khoá mới bị từ chối cho tới khi qua khoảng tối thiểu, rồi được chấp nhận (3b). Không giới hạn thì 200 token rác sinh 200 lời gọi (khuếch đại); giới hạn thì 0. Trong thực tế, đặt thời hạn cache JWKS và khoảng tối thiểu hợp lý, và **đưa khoá mới vào JWKS trước khi dùng nó để ký** (bước 1 đến 3 ở trên).

#### Lab 3.2 — Client Credentials và scope
**Đầu ra thật:**
```text
svc-a xin scope boards.read -> HTTP 200; gọi API -> HTTP 200
svc-a xin thêm boards.write -> HTTP 400: invalid_scope (scope vượt quyền: boards.read boards.write)
svc-a sai secret -> HTTP 400: invalid_client
```
**Giải thích:** quyền tối thiểu: client chỉ nhận scope đã được cấp; xin thêm thì bị từ chối; sai bí mật bị từ chối. (Mã lỗi `invalid_client` thật theo RFC 6749 thường là HTTP 401; lab dựng trả 400 để đơn giản.)

## L5 Troubleshooting — Triệu chứng và nguyên nhân gốc

| Triệu chứng | Nguyên nhân thường gặp | Cách kiểm |
|---|---|---|
| Sau khi xoay khoá, token mới bị 401 một lúc | JWKS cache chưa có khoá mới, hoặc ký trước khi đưa khoá vào JWKS | so `kid` token với JWKS đang phục vụ; đưa khoá vào JWKS trước |
| Token cũ chết ngay sau xoay | bỏ khoá cũ khỏi JWKS quá sớm | giữ khoá cũ tới hết hạn token cũ |
| Máy chủ JWKS bị gọi dồn dập | tải lại mỗi lần gặp `kid` lạ | đặt khoảng tối thiểu giữa hai lần tải (Lab 3.1 bước 5) |
| Một service kiểm được, service khác 401 | hai service cache JWKS ở thời điểm khác nhau, hoặc `aud` khác | so thời điểm cache và cấu hình `aud` từng service |
| Client Credentials bị từ chối | sai `scope` hoặc sai bí mật | đọc `error` và `error_description` trong phản hồi |

## L7 Architecture — Khi nào chọn gì

| Nhu cầu | Chọn | Lý do |
|---|---|---|
| Nhiều service chỉ cần kiểm | RS256 hoặc ES256 + JWKS | khoá ký chỉ ở nơi cấp |
| Hai service tin nhau trong mạng kín, ít | HS256 với khoá chung | đơn giản, nhưng mọi bên giữ được khoá ký |
| Máy gọi máy trong cùng hệ | Client Credentials | quyền theo scope, danh tính riêng |
| Máy gọi máy cần chống lộ bí mật | mTLS (⚠, chưa chạy) | không có bí mật nào gửi đi |
| Bên ngoài kiểm một lần, không có JWKS | introspection (Part 4) | hỏi nơi cấp mỗi lần, thu hồi tức thì |

**Cách chọn:** hỏi (1) ai cần ký và ai chỉ cần kiểm, (2) token sống bao lâu, (3) thu hồi gấp thế nào, (4) mất bao lâu để mọi service thấy khoá mới. Gateway có thể kiểm sơ bộ (chữ ký, `exp`), nhưng **mỗi service vẫn phải tự kiểm** `aud` và quyền của chính nó (xem [API Gateway Part 4](../api-gateway/part-4-advanced.md)).

## Tự kiểm tra

1. Vì sao RS256 hợp với nhiều service hơn HS256?
2. Thứ tự đúng khi xoay khoá là gì và vì sao?
3. Tải lại JWKS mỗi khi gặp `kid` lạ gây rủi ro gì, và giảm thế nào?
4. Client Credentials khác Authorization Code ở điểm nào?

<details>
<summary>Đáp án ngắn</summary>

1. Dịch vụ chỉ giữ khoá công khai, không ký được; HS256 mọi bên giữ khoá ký.
2. Thêm khoá mới vào JWKS, đợi cache, rồi ký bằng khoá mới, giữ khoá cũ tới khi token cũ hết hạn, rồi bỏ.
3. Khuếch đại lời gọi tới máy chủ JWKS; giới hạn tần suất tải lại.
4. Không có người dùng, dịch vụ tự xác thực bằng bí mật và nhận token theo scope.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** Lab 3.1 và 3.2 (.NET 10, Kestrel `127.0.0.1`; khoảng tối thiểu 300 ms và 5 s là số của lab).
**Đã tự mở:** [API Gateway Part 4](../api-gateway/part-4-advanced.md) (kiểm JWT ở gateway).
**Chỉ qua agent (⚠):** RFC 7517 (JWK, JWKS), RFC 6749 mục 4.4 (Client Credentials), RFC 8725 (JWT BCP); quy trình xoay khoá là thực hành phổ biến, không chuẩn cứng.
**Chưa kiểm:** mTLS và introspection chưa chạy; gateway thật kiểm token đã có ở API Gateway Lab 4.2, không lặp lại ở đây.
