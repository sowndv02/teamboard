---
feature: identity-auth
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md   # confirmed cho I1–I4, I6 (PASS vòng 6, 2026-10-25); I5, I7, I8 chưa có design
status: approved            # draft | approved | done
updated: 2026-10-25
---

# Slices: identity-auth

Chia theo `.agents/skills/workflow/references/slicing.md`. Bảng nháp ban đầu soạn trước khi có design; ngày 2026-10-25 người dùng duyệt (Confirm log): giữ 18 lát nhỏ và gắn nhãn giai đoạn I1–I9 của spec (mục 17), tách lát 1 thành 1a/1b, thêm lát 19 (I9) giữ chỗ.

Ước lượng là khoảng cho người làm một mình, giả định đã có mẫu service từ board-service và gateway; **chưa hiệu chỉnh bằng số liệu thực** (độ tin cậy thấp).

| # | GĐ | Lát (hành vi giao được) | FR/NFR | Phụ thuộc | Ước lượng | Issue | Trạng thái |
|---|---|---|---|---|---|---|---|
| 1a | I1 | Khung identity-service chạy được: health live/ready (ready kiểm DB), OTel, PostgreSQL + EF Core, migration nền, Dockerfile và compose | NFR-001, NFR-003, NFR-009 (một phần NFR-004: `traceparent`) | TB-001 (ServiceDefaults, xong), `shared/Http` (xong) | 2–3 ngày | TB-021 | review |
| 1b | I1 | Đăng ký bằng email/mật khẩu băm PBKDF2, benchmark số vòng và vCPU | FR-001, FR-004, BR-001, BR-002, NFR-002, NFR-007 (metric băm) | 1a | 2–3 ngày | [TB-029](../issues/TB-029-identity-register.md) | todo |
| 2 | I1 | Đăng nhập, lỗi chung không lộ email | FR-002, BR-003 | 1b | 2–3 ngày | [TB-030](../issues/TB-030-identity-login.md) | todo |
| 3 | I1 | Khoá tạm theo (email, IP) và giới hạn tốc độ (cờ `RateLimiter`, Q-G = A) | FR-003, NFR-008 | 2 | 2–3 ngày | [TB-031](../issues/TB-031-identity-lockout-rate-limit.md) | todo |
| 4 | I2 | Phát access token JWT khi đăng nhập (ADR RS256 + Secrets Manager tạo trước lát này) | FR-005, BR-004 | 2, ADR thuật toán ký | 2–3 ngày | [TB-032](../issues/TB-032-identity-access-token.md) | todo |
| 5 | I2 | Bộ kiểm tra token dùng chung `Teamboard.Auth` (JWKS, cache) và test tấn công (ADR thư viện token) | FR-006, NFR-006 | 4, JWKS (6) | 3–4 ngày | [TB-033](../issues/TB-033-auth-token-validation-library.md) | todo |
| 6 | I3 | Công bố JWKS | FR-007 | 4 | 1–2 ngày | [TB-034](../issues/TB-034-identity-jwks.md) | todo |
| 7 | I3 | Xoay khoá ký không hỏng token còn hạn (CLI, D7) | FR-008 | 6, ADR xoay khoá | 2–3 ngày | [TB-035](../issues/TB-035-identity-signing-key-rotation.md) | todo |
| 8 | I4 | Refresh token xoay vòng (ADR refresh nguyên tử) | FR-009 | 4 | 2–3 ngày | [TB-036](../issues/TB-036-identity-refresh-token.md) | todo |
| 9 | I4 | Phát hiện dùng lại refresh token, thu hồi chuỗi | FR-010 | 8 | 2 ngày | [TB-037](../issues/TB-037-identity-refresh-reuse-detection.md) | todo |
| 10 | I4 | Đăng xuất thu hồi phiên (phần phiên cookie chờ I5) | FR-011 | 8 | 1 ngày | [TB-038](../issues/TB-038-identity-logout.md) | todo |
| 11 | I1 | Đổi mật khẩu (thu hồi refresh) | FR-022 | 3, 8 | 1–2 ngày | [TB-039](../issues/TB-039-identity-change-password.md) | todo |
| 12 | I5, I6 | Quản lý client đăng ký (cấu hình/migration) | FR-014 | 1b | 2 ngày | [TB-040](../issues/TB-040-identity-client-registry.md) | todo |
| 13 | I6 | Client Credentials, token máy | FR-015, BR-008 | 4, 12 | 2–3 ngày | [TB-041](../issues/TB-041-identity-client-credentials.md) | todo |
| 14 | I5 | Authorization Code + PKCE: authorize và trang đăng nhập (chưa có design, chờ spec FE) | FR-012, FR-019, BR-005 | 2, 12, ADR trang đăng nhập | 4–5 ngày | [TB-042](../issues/TB-042-identity-authorize-pkce.md) | todo |
| 15 | I5 | Endpoint token đổi mã (code_verifier) | FR-013 | 14 | 2–3 ngày | [TB-043](../issues/TB-043-identity-token-endpoint-code-exchange.md) | todo |
| 16 | I7 | OIDC: id_token, userinfo, discovery | FR-016–018 | 15 | 3–4 ngày | [TB-044](../issues/TB-044-identity-oidc.md) | todo (Could) |
| 17 | I8 | Ủy quyền theo scope | FR-020 | 5 | 2 ngày | [TB-045](../issues/TB-045-identity-scope-authorization.md) | todo |
| 18 | I8 | Thu hồi token và introspection tối thiểu | FR-021, BR-006 | 13, 17 | 2–3 ngày | [TB-046](../issues/TB-046-identity-revocation-introspection.md) | todo |
| 19 | I9 | **Giữ chỗ:** tích hợp gateway: route công khai của identity, gateway kiểm JWT, `board-service` yêu cầu đăng nhập | FR-006 (phía gateway), NFR-008 (nếu chuyển rate limit sang gateway) | 4–6, gateway lát 2 và 4 | Chưa ước lượng | – | todo (chưa thiết kế) |

Tổng khoảng 42–58 ngày công (chưa tính lát 19); mỗi lát ≤ ~3 ngày trừ lát 14 (cần cân nhắc chia thêm sau design I5).

**Lát 19 chưa lập plan được:** spec identity (mục "Phạm vi") và design chưa phủ I9; trước khi plan cần spec/design bổ sung cho cả gateway (route công khai của identity chưa có trong `document/spec/api-gateway.md`; kiểm JWT đang ngoài phạm vi gateway) và `board-service`. Gateway mới có lát 1 (TB-010); lát 2 (correlation) và 4 (rate limit) là điều kiện.

## Tự kiểm theo `slicing.md` mục 3.2

- Phủ: FR-001…FR-022 đều có lát (FR-022 → 11). BR-001…008 và NFR-001…009 đều có lát chịu trách nhiệm; NFR-005 và NFR-009 là ràng buộc chung (NFR-009 ghi ở 1a vì migration và seed).
- Cỡ: lát 14 vẫn đáng ngờ về cỡ; lát 1 đã tách thành 1a/1b.
- Thứ tự theo phụ thuộc rồi giá trị; 1a–2 là "xương sống".
- Lát 1a chạy được (health, ready kiểm DB, compose), không phải lát "chuẩn bị" thuần.
- ADR đề xuất ở mục 16 của design tạo ngay trước lát cần chúng (4–5: RS256/Secrets Manager và `Teamboard.Auth`/`Microsoft.IdentityModel`; 8: refresh nguyên tử; 14–15: ngoại lệ đường dẫn OAuth), không chặn 1a–3.

## Ngoài lát (hoãn/không làm)
Theo mục "Phạm vi" của spec (chưa xác minh email, không role, không API quản trị client).

## Thứ tự và lý do
Nền → đăng nhập → bảo vệ chống tấn công → token → hạ tầng khoá → vòng đời refresh → OAuth/OIDC → ủy quyền → tích hợp gateway, bám lộ trình I1–I9 của spec (mục 17). Lát đầu tiên chưa `done` là lát hiện tại: **1a**.

## Confirm log

| # | Điểm cần chốt | Quyết định | Ngày |
|---|---|---|---|
| 1 | Cách định nghĩa "lát I1" khi design dùng nhãn giai đoạn và bảng dùng số lát | Giữ 18 lát nhỏ, thêm cột GĐ (I1–I9) | 2026-10-25 |
| 2 | Lát 1 vượt cỡ (3–5 ngày) | Tách 1a (khung) và 1b (đăng ký + băm) | 2026-10-25 |
| 3 | Cách thêm I9 | Một hàng giữ chỗ (lát 19), chưa thiết kế | 2026-10-25 |
| 4 | Thời điểm tạo ADR đề xuất ở mục 16 của design | Ngay trước lát cần chúng, không chặn 1a | 2026-10-25 |
