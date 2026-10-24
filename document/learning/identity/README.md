# Identity: lộ trình học xác thực và ủy quyền (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu **xác thực (authentication)** và **ủy quyền (authorization)** như hai việc khác nhau, nắm từng cơ chế (mật khẩu, JWT, JWKS và xoay khoá, OAuth 2.0 và OIDC, refresh token, scope), tự chạy thử cả các **tấn công** vào dịch vụ do chính bạn dựng, và biết lúc nào **không** nên tự xây.
> **Cần biết trước:** HTTP, C# cơ bản, khái niệm băm (hash) và chữ ký số ở mức cơ bản; lộ trình [API Gateway](../api-gateway/README.md) Part 4 (JWT ở gateway) là điểm nối.
> **Thời lượng ước tính:** 25 đến 35 giờ nếu làm đủ lab.
> **Loại:** khái niệm. Ứng dụng trong dự án: `identity-service` tự xây để học ([ADR 0006](../../architecture/adr/0006-identity-self-built.md), [identity-learning.md](../../architecture/identity-learning.md), các giai đoạn I1 đến I9).

## Cảnh báo: tự xây để học không phải để chạy sản phẩm

- Mật mã và giao thức xác thực rất dễ sai lỗi tinh vi. Lộ trình này dạy bạn **hiểu** chúng bằng cách tự dựng một bản nhỏ; nó **không** biến bản tự dựng thành thứ đủ an toàn cho người dùng thật. ADR 0006 nói cùng ý: nếu `identity-service` dùng ngoài mục đích học thì thay bằng giải pháp đã kiểm chứng (ví dụ Cognito).
- **Không tự viết thuật toán mật mã.** Lab chỉ gọi nguyên thuỷ của .NET BCL (`RSA`, `HMAC`, `Rfc2898DeriveBytes.Pbkdf2`, `RandomNumberGenerator`, `CryptographicOperations.FixedTimeEquals`); phần tự viết là logic ghép chúng (định dạng JWT, kiểm claim, các luồng OAuth).
- **Mọi tấn công trong lộ trình chỉ nhằm vào dịch vụ do lab dựng** trên `127.0.0.1`, với khoá RSA và HMAC **sinh ngẫu nhiên lúc chạy**, mật khẩu là chuỗi giả; không có khoá, mật khẩu hay token thật nào, và không có gì được commit vào repo ngoài tài liệu này.
- Lộ trình không phải tư vấn bảo mật cho hệ thống thật.

## Phạm vi và những gì lộ trình này KHÔNG đại diện

- **Không thay `identity-learning.md`:** tài liệu đó là nguồn sự thật về phạm vi dự án; lộ trình này là cách **học và kiểm chứng** từng cơ chế. Chỗ lệch hoặc chưa chốt chỉ được báo ở Part 6.
- **Không phải mọi cơ chế đều được chạy:** SAML, MFA, passkeys (WebAuthn), OIDC `id_token` và discovery, introspection và thu hồi (RFC 7009 và 7662), sender-constrained token (DPoP, mTLS), quản lý khoá bằng KMS hay Secrets Manager chỉ ở mức khái niệm và gắn ⚠.
- **JWT không phải là "xác thực":** nó chỉ là một định dạng token. Chọn session cookie hay token là quyết định riêng (Part 1).

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự triển khai và đo bằng code hoặc lệnh thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác (hoặc không dùng)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Giai đoạn của dự án | Làm xong thì |
|---|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | định danh, xác thực, ủy quyền, session so với token, cấu trúc JWT, Bản đồ giải pháp | nền | Giải thích và chọn được có tự xây hay không |
| [2 Core skills](part-2-core-skills.md) | băm mật khẩu, dựng và kiểm JWT bằng BCL, kiểm claim | I1, I2 | Làm đúng hai việc cơ bản nhất |
| [3 Multi-component](part-3-multi-component.md) | `kid`, JWKS, xoay khoá, nhiều service cùng kiểm token, Client Credentials | I3, I6, I9 | Kiểm token đúng khi có nhiều service |
| [4 Advanced](part-4-advanced.md) | OAuth 2.0 và OIDC, Authorization Code + PKCE, refresh token, thu hồi | I4, I5, I7 | Hiểu và chạy được luồng đăng nhập đầy đủ |
| [5 Production](part-5-production-security-ops.md) | các tấn công và biện pháp, quản lý khoá, brute force, quan sát, chi phí | mọi giai đoạn | Biết tấn công thế nào và chặn ra sao |
| [6 Projects](part-6-projects.md) | độ phủ I1 đến I9 và 12 điểm bảo mật, các điểm cần chốt | I1 đến I9 | Có bằng chứng cho từng điểm bảo mật |
| [7 Mastery](part-7-troubleshooting-mastery.md) | sự cố thực tế, phỏng vấn, bài mở | – | Tự đánh giá mức thành thạo |

## Ma trận phủ

`✓` là Part có mục tương ứng, `–` là không áp dụng (lý do ở dưới). Mỗi ô `✓` có đúng một mục `## Lx` trong file Part.

| Part | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 |
|---|---|---|---|---|---|---|---|---|
| 1 | ✓ | ✓ | ✓ | ✓ | – | – | ✓ | – |
| 2 | ✓ | ✓ | ✓ | ✓ | ✓ | – | – | – |
| 3 | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | – |
| 4 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 5 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 6 | ✓ | – | – | ✓ | ✓ | – | – | – |
| 7 | – | – | – | – | ✓ | – | ✓ | ✓ |

**Lý do các ô `–`:**
- **Part 1, L5:** chưa có thao tác nào để hỏng; sự cố đầu tiên xuất hiện khi dựng và kiểm token ở Part 2.
- **L6 ở Part 1 đến 3:** các quyết định production được gom vào Part 4 và 5 để tránh nói hai lần.
- **L7 ở Part 2:** chưa có lựa chọn nào để so sánh ở mức kỹ năng lõi; so sánh nằm ở Part 1 và 3.
- **L8 ở Part 1 đến 6:** bài tập mở và tiêu chí thành thạo nằm ở Part 7.
- **Part 6, L2 và L3:** là dự án áp dụng; lý do và cơ chế đã học ở Part 1 đến 5.
- **Part 7, L1 đến L4 và L6:** Part này dành cho sự cố, đánh đổi và tự đánh giá, không có kiến thức nền hay lab mới.

## Môi trường dùng để chạy thật

Windows 11, .NET SDK 10.0.402 (runtime 10.0.13), ngày 25/10/2026. Gói ngoài BCL: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13 (kéo theo `Microsoft.IdentityModel.*` để **đối chiếu** với bản tự dựng). Dịch vụ giả là Kestrel trên `127.0.0.1`; không dùng Docker, không AWS. Dự án thử nằm **ngoài repo**. Số đo thời gian chỉ tương đối trên máy này.
**Chưa chạy (⚠):** OIDC (`id_token`, `nonce`, discovery), introspection và thu hồi, MFA, passkeys, SAML, Argon2id, quản lý khoá bằng KMS hay Secrets Manager, Cognito, Keycloak, Duende, mô hình session cookie đầy đủ (CSRF, `SameSite`), gateway thật kiểm token (đã có ở [API Gateway Lab 4.2](../api-gateway/part-4-advanced.md)).

## Tiêu chí hoàn thành

- Làm đủ lab L4 của Part 1 đến 5 và ghi lại đầu ra của chính bạn.
- Tự gỡ được ít nhất 5 sự cố ở L5 mà không nhìn đáp án.
- Hoàn thành ít nhất 2 dự án ở Part 6.
- Trả lời đúng phần lớn câu "Tự kiểm tra" ở mọi Part và làm một bài mở ở L8 (Part 7).

## Tự đánh giá nhanh

Đánh dấu những gì bạn làm được **không cần tra cứu**:

- [ ] L1–L2: phân biệt định danh, xác thực, ủy quyền; session so với token; vì sao JWT "được ký chứ không được mã hoá".
- [ ] L3: kể cấu trúc JWT, vai trò của `iss`, `aud`, `exp`, `kid`; ba lý do OAuth tách vai trò; PKCE chặn gì.
- [ ] L4: băm mật khẩu và so sánh hằng thời gian; dựng, ký và kiểm JWT; xoay khoá; chạy luồng code + PKCE.
- [ ] L5: từ triệu chứng (token giả được chấp nhận, đăng nhập lộ thông tin, refresh bị dùng lại, key xoay làm lỗi) tìm nguyên nhân gốc.
- [ ] L6: nêu thời hạn token, số vòng băm, chính sách khoá, quan sát, chi phí.
- [ ] L7: chọn giữa tự xây, Cognito, Keycloak, Duende, OpenIddict, session cookie, API key, mTLS, hoặc không xác thực.
- [ ] L8: thiết kế hệ định danh cho một sản phẩm mới kèm đánh đổi và điều kiện đổi quyết định.

## Nguồn và độ tin cậy

Mỗi file Part có mục "Nguồn và độ tin cậy" riêng. Mô hình bài học và khuôn Part: `.agents/skills/learning-hub/references/`. Nguồn sự thật của dự án: [identity-learning.md](../../architecture/identity-learning.md) và [ADR 0006](../../architecture/adr/0006-identity-self-built.md); lộ trình **không** sửa các tài liệu đó và không sửa `src/`.
