# ADR 0006 — Tự xây `identity-service`, không dùng framework identity trọn gói

Trạng thái: Chấp nhận · Ngày: 2026-10-21
Nguồn: yêu cầu của chủ dự án; `document/architecture/identity-learning.md`
Nhãn: `[Chọn để học]`

## Bối cảnh
Mục tiêu dự án là học system design và hiểu sâu bảo mật. Chủ dự án muốn tự xây các chức năng xác thực/ủy quyền (JWT, OAuth 2.0, OpenID Connect) để hiểu từ bên trong, thay vì dùng IdentityServer, OpenIddict, Keycloak hay Cognito cho phần cần học.

## Quyết định
- Tự xây `identity-service` (Vertical Slice, PostgreSQL) theo các giai đoạn I1–I9 trong `identity-learning.md`.
- **Được tự viết:** logic phát/kiểm token, luồng OAuth/OIDC, quản lý người dùng/phiên, ủy quyền theo claim/policy.
- **Dùng nguyên thuỷ mật mã của .NET BCL**, **không tự cài thuật toán mật mã** (RSA, ECDSA, SHA, AES, PBKDF2...).
- Ưu tiên Authorization Code + PKCE và Client Credentials; Implicit/ROPC/Hybrid chỉ để hiểu và thử tấn công trong lab.
- Khoá ký và bí mật lưu ở Secrets Manager/KMS, không trong repo.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. Tự xây trên BCL (đã chọn) | Hiểu sâu giao thức; luyện kiểm thử bảo mật; kiểm soát hoàn toàn | Dễ sai lỗi tinh vi; công sức; không đủ tin cậy cho sản phẩm thật nếu không rà soát kỹ | Chọn, nhãn `[Chọn để học]` |
| B. Amazon Cognito | Trả theo MAU, ít vận hành, tính năng bảo mật sẵn, tuân thủ | Ít hiểu sâu; tuỳ biến hạn chế; lock-in | Không chọn cho mục tiêu học; **phương án nên dùng nếu làm sản phẩm thật** |
| C. IdentityServer/Duende hoặc OpenIddict | Chuẩn, đầy đủ, được kiểm chứng | Che giấu cơ chế (trái mục tiêu học); Duende có giấy phép thương mại; IdentityServer4 hết hỗ trợ | Không chọn; có thể dùng làm chuẩn đối chiếu hành vi |
| D. Keycloak tự lưu trữ | Đầy đủ, nguồn mở | Nặng, chi phí chạy rỗng, che giấu cơ chế | Không chọn |

## Vì sao không chọn các phương án còn lại
- **B (Cognito)** sẽ đúng khi mục tiêu là giao sản phẩm nhanh, an toàn, chi phí vận hành thấp. **Tín hiệu xem lại:** khi `identity-service` được dùng ngoài mục đích học.
- **C/D** sẽ đúng khi cần tính năng nâng cao (SAML, federation, MFA đầy đủ) mà tự xây quá tốn công.

## Hệ quả và đánh đổi
- Chấp nhận rủi ro tự xây: bù bằng test tấn công cho từng điểm bảo mật, review lăng kính bảo mật, và **không dùng làm hệ thống thật** khi chưa được rà soát độc lập.
- Cần chốt: trang đăng nhập (server-rendered hay Angular), nơi lưu token phía client (ADR 0005), thuật toán ký, chính sách xoay khoá.
- Chi phí chạy rỗng (Fargate + PostgreSQL) cần được đưa vào ước tính (`aws-service-selection`).

## Tín hiệu xem lại
- Có yêu cầu dùng cho người dùng thật hoặc dữ liệu thật.
- Cần SAML/federation/MFA ngoài phạm vi học.
- Phát hiện lỗ hổng nghiêm trọng mà đội không kịp xử lý.

## Liên kết
`document/architecture/identity-learning.md`, `document/security/infra-attack-scenarios.md`, ADR 0005, `.agents/rules/api-endpoints.md` mục 7.
