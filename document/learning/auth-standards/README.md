# Authentication Standards: lộ trình học khái niệm (Part 0)

> [!NOTE]
> **Mục tiêu:** nắm **chuẩn và mô hình tư duy** xác thực/ủy quyền (Identity, session, token, OAuth/OIDC, SAML, doanh nghiệp, passkey, phân quyền, kiến trúc) — đủ sâu để đọc flow, chọn phương án và trao đổi với đội triển khai; **không** thay thế lab code trong dự án.
> **Cần biết trước:** HTTP, HTTPS ở mức khái niệm, client–server, REST; đọc song song hoặc sau [Identity (hands-on)](../identity/README.md) khi cần chạy JWT/PKCE trên .NET.
> **Thời lượng ước tính:** 12–18 giờ đọc (tương đương lộ trình Core/Full của bộ `auth_std_roadmap-main`, không gồm lab).
> **Loại:** khái niệm · **Bản đồ giải pháp:** [Part 7 — L7 Architecture](part-7-troubleshooting-mastery.md#bản-đồ-giải-pháp)

## Quan hệ với lộ trình Identity (TeamBoard)

| Lộ trình | Vai trò |
|---|---|
| **[auth-standards](README.md) (file này)** | Khái niệm **rộng**: SAML, Kerberos, LDAP, MFA, passkey, mô hình phân quyền, kiến trúc SPA/BFF, case doanh nghiệp — nguồn: `auth_std_roadmap-main`. |
| **[identity](../identity/README.md)** | **Thực hành** trên `identity-service`: băm mật khẩu, JWT/JWKS, OAuth Code+PKCE, tấn công tự dựng trên `127.0.0.1` (ADR 0006). |

**Không lặp:** JWT cấu trúc, PKCE, lab tấn công, xoay khoá chi tiết → chỉ [trỏ link](../identity/README.md). OAuth/OIDC/SAML ở đây giải thích **chuẩn và vai trò**; Identity chứng minh **một triển khai tối thiểu**.

## Lab và quiz (hoãn)

- **Lab** (Keycloak, demo ASP.NET Core trong bộ nguồn): **chưa tích hợp** — issue riêng sau TB-049; không chạy Keycloak, không lưu secret thật.
- **Quiz** trong bộ nguồn: **bỏ**; mỗi Part chỉ giữ mục **Tự kiểm tra** ngắn.
- **L4 (Hands-on)** trên toàn lộ trình: ô ma trận là `–` vì lab hoãn; phần đọc vẫn nêu đầu ra lab mong đợi để biết đọc xong cần làm gì tiếp.

## Cảnh báo

- Tài liệu **không** là tư vấn bảo mật cho hệ thống production.
- Ví dụ dùng **Entra ID, Keycloak, LDAP** mang tính minh hoạ; không cấu hình hay kết nối dịch vụ thật trong phạm vi TB-049.

## Cách học

Chiều ngang: **Part 1–7** (ánh xạ 7 module nguồn). Chiều sâu: **L1–L8** như [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

| Tầng | Câu hỏi |
|---|---|
| L1 What | Khái niệm/cơ chế là gì? |
| L2 Why | Vì sao cần, giải quyết vấn đề gì? |
| L3 How | Hoạt động từng bước, sơ đồ, dễ nhầm? |
| L4 Hands-on | Lab (hoãn toàn lộ trình — xem trên) |
| L5 Troubleshooting | Triệu chứng → nguyên nhân |
| L6 Production | Chính sách, NIST, storage, vận hành |
| L7 Architecture | So sánh, chọn giải pháp, Bản đồ giải pháp (Part 7) |
| L8 Mastery | Tự thiết kế / phỏng vấn (Part 7) |

## Các Part

| Part | Module nguồn | Nội dung | Sau Part này |
|---|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | M01 | Identity/account/credential, IAAA, IdP/RP/AS/RS, SSO, federation, MFA | Gọi đúng tên; phân biệt 401/403 |
| [2 Core skills](part-2-core-skills.md) | M02 | Hash/ký/mã hoá, session/cookie, token/JWT (tóm tắt), vòng đời, HTTP auth | Chọn session vs token; scheme HTTP/API key |
| [3 Multi-component](part-3-multi-component.md) | M03 | OAuth 2.0/2.1, OIDC, grants, state/nonce, discovery, `iss`+`sub` | Đọc được luồng Code+PKCE và Client Credentials |
| [4 Advanced](part-4-advanced.md) | M04 | SAML 2.0, assertion, binding, bảo mật SAML | Đọc SP-initiated SSO; so sánh với OIDC |
| [5 Production](part-5-production-security-ops.md) | M05 | LDAP/AD, Kerberos, WebAuthn, passkey | Nhận diện auth doanh nghiệp và passwordless |
| [6 Projects](part-6-projects.md) | M06 | RBAC/ABAC, 401/403, CSRF/XSS, tấn công identity, NIST, token storage | Rà API và web theo checklist bảo mật |
| [7 Mastery](part-7-troubleshooting-mastery.md) | M07 | Thiết kế web/SPA/BFF, native/S2S, case, **Bản đồ giải pháp** | Chọn giao thức theo use case |

## Ma trận phủ

`✓` = một mục `## Lx` trong file Part; `–` = không áp dụng hoặc lab hoãn.

| Part | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 |
|---|---|---|---|---|---|---|---|---|
| 1 | ✓ | ✓ | ✓ | – | – | – | ✓ | – |
| 2 | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | – |
| 3 | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | – |
| 4 | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | – |
| 5 | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | – |
| 6 | ✓ | ✓ | ✓ | – | ✓ | ✓ | – | – |
| 7 | ✓ | ✓ | – | – | ✓ | ✓ | ✓ | ✓ |

**Lý do ô `–`:**
- **L4 mọi Part:** lab ASP.NET/Keycloak hoãn (TB-049); thực hành JWT/PKCE → [Identity](../identity/README.md).
- **Part 1, L5–L6:** sự cố và production gom ở Part 2–7.
- **Part 4–5, L5:** troubleshooting SAML/enterprise ngắn; sự cố API/web sâu hơn ở Part 6–7.
- **Part 6, L7–L8:** chọn giải pháp toàn cục ở Part 7.
- **Part 7, L3–L4:** không lab mới; L3 đã phủ ở Part 1–6.

## Ánh xạ 32 bài gốc → lộ trình này

Bộ nguồn: `H:\learning\auth_std_roadmap-main\auth_std_roadmap-main` (32 bài lý thuyết; quiz/lab không map). Quiz 8 + lab 5 **không** có hàng riêng.

| Bài gốc | Part / mục |
|---|---|
| 1.1 Identity, AuthN/AuthZ, vai trò | [Part 1 — L1/L3](part-1-fundamentals.md#l1-what--ngôn-ngữ-chung) |
| 1.2 SSO, federation, MFA | [Part 1 — L3](part-1-fundamentals.md#l3-how--sso-federation-mfa) |
| 2.1 Cryptography | [Part 2 — L1/L3](part-2-core-skills.md) |
| 2.2 Session & cookie | Part 2 — L3 |
| 2.3 Token & JWT | Part 2 — L3 (chi tiết → Identity) |
| 2.4 Vòng đời token | Part 2 — L3/L6 |
| 2.5 HTTP auth & API key | Part 2 — L3/L7 |
| 3.1 OAuth cơ bản | [Part 3 — L1](part-3-multi-component.md) |
| 3.2 Authorization Code + PKCE | Part 3 — L3 |
| 3.3 Grants & OAuth 2.1 | Part 3 — L3/L6 |
| 3.4 state, nonce, redirect | Part 3 — L3/L5 |
| 3.5 OIDC & ID token | Part 3 — L3 |
| 3.6 Discovery, JWKS, logout | Part 3 — L3/L6 |
| 3.7 User identity, social login | Part 3 — L3/L7 |
| 4.1 SAML thành phần | [Part 4](part-4-advanced.md) |
| 4.2 Assertion & Response | Part 4 — L3 |
| 4.3 Flows & bindings | Part 4 — L3 |
| 4.4 Bảo mật & so OIDC | Part 4 — L6/L7 |
| 5.1 LDAP & AD | [Part 5](part-5-production-security-ops.md) |
| 5.2 Kerberos & enterprise | Part 5 — L3 |
| 5.3 WebAuthn | Part 5 — L3 |
| 5.4 Passkey & UP/UV | Part 5 — L3/L6 |
| 6.1 Mô hình phân quyền | [Part 6 — L1](part-6-projects.md) |
| 6.2 Authorization trong API | Part 6 — L2/L3 |
| 6.3 CSRF, XSS, session | Part 6 — L3 |
| 6.4 Tấn công identity | Part 6 — L5 |
| 6.5 NIST, token storage | Part 6 — L6 |
| 7.1 Thiết kế, Web, SPA, BFF | [Part 7 — L1/L3](part-7-troubleshooting-mastery.md) |
| 7.2 Desktop, mobile, S2S | Part 7 — L3 |
| 7.3 Provisioning, WPF ví dụ | Part 7 — L6 |
| 7.4 Case & troubleshooting | Part 7 — L5 |
| 7.5 Decision matrix | Part 7 — L7 (Bản đồ giải pháp) |

## Tiêu chí hoàn thành (đọc)

- Trả lời được phần lớn **Tự kiểm tra** ở Part 1–7.
- Vẽ được một luồng **Code+PKCE** và một luồng **SAML SP-initiated** (không cần lab).
- Chọn được 1 dòng trong **Bản đồ giải pháp** cho 3 tình huống (web SPA, API partner, nhân viên AD).
- (Tuỳ chọn) Làm lab Identity Part 2–4 để củng cố OAuth/JWT trên code.

## Tự kiểm tra nhanh (Part 0)

1. Khác nhau giữa identifier và credential? Cho ví dụ API chỉ đọc email header.
2. SSO và federation khác nhau thế nào?
3. Vì sao lộ trình này không thay Identity?
4. Lab L4 ở ma trận toàn `–` — làm thực hành ở đâu?

<details>
<summary>Đáp án ngắn</summary>

1. Identifier để tra cứu, không chứng minh; credential để chứng minh. API tin email header = nhầm identifier với credential.
2. SSO là trải nghiệm đăng nhập một lần; federation là quan hệ tin giữa miền danh tính.
3. Identity = tự dựng và tấn công có kiểm soát trên dự án; auth-standards = chuẩn và bối cảnh doanh nghiệp, không sửa `src/`.
4. [Identity](../identity/README.md) Part 2–5; lab Keycloak hoãn issue sau TB-049.

</details>

## Nguồn và độ tin cậy

- Nội dung học: bộ `auth_std_roadmap-main` (HTML site + `authentication_standards_15h_roadmap.md`) — viết lại the không chép nguyên văn.
- Kiểm chứng chuẩn: RFC OAuth/OIDC/SAML, NIST SP 800-63B — mức **Vừa** nếu chưa tự mở trong phiên viết.
- Thực hành dự án: [identity-learning.md](../../architecture/identity-learning.md), [ADR 0006](../../architecture/adr/0006-identity-self-built.md).