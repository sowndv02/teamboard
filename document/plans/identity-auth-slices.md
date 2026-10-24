---
feature: identity-auth
spec: document/spec/identity-auth.md
design: document/design/identity-auth.md   # draft, chưa confirmed; D6 (FE) chưa chốt
status: draft            # draft | approved | done
updated: 2026-10-21
---

# Slices: identity-auth

Bảng nháp do chạy thử `plan-init` chế độ `slices` (`.agents/skills/workflow/references/slicing.md`) trên spec `confirmed`, **trước khi có design**. Chưa duyệt; không tạo issue từ bảng này cho tới khi design `confirmed` và người dùng duyệt.

Ước lượng là khoảng cho người làm một mình, giả định đã có mẫu service từ board-service; **chưa hiệu chỉnh bằng số liệu thực** (độ tin cậy thấp).

| # | Lát (hành vi giao được) | FR/NFR | Phụ thuộc | Ước lượng | Issue | Trạng thái |
|---|---|---|---|---|---|---|
| 1 | Khung identity-service chạy được (health, OTel, DB, migration) + đăng ký bằng email/mật khẩu băm | FR-001, FR-004, BR-001, BR-002, NFR-001–004, NFR-007 | TB-001 (ServiceDefaults) | 3–5 ngày | – | todo |
| 2 | Đăng nhập, lỗi chung không lộ email | FR-002, BR-003 | 1 | 2–3 ngày | – | todo |
| 3 | Khoá tạm theo (email, IP) và giới hạn tốc độ | FR-003, NFR-008 | 2 | 2–3 ngày | – | todo |
| 4 | Phát access token JWT khi đăng nhập | FR-005, BR-004 | 2, ADR thuật toán ký | 2–3 ngày | – | todo |
| 5 | Bộ kiểm tra token dùng chung (JWKS, cache) và test tấn công | FR-006, NFR-006 | 4, JWKS (6); nơi đặt chốt ở design | 3–4 ngày | – | todo |
| 6 | Công bố JWKS | FR-007 | 4 | 1–2 ngày | – | todo |
| 7 | Xoay khoá ký không hỏng token còn hạn | FR-008 | 6, ADR xoay khoá | 2–3 ngày | – | todo |
| 8 | Refresh token xoay vòng | FR-009 | 4 | 2–3 ngày | – | todo |
| 9 | Phát hiện dùng lại refresh token, thu hồi chuỗi | FR-010 | 8 | 2 ngày | – | todo |
| 10 | Đăng xuất thu hồi phiên | FR-011 | 8 | 1 ngày | – | todo |
| 11 | Đổi mật khẩu (thu hồi refresh) | FR-022 | 3, 8 | 1–2 ngày | – | todo |
| 12 | Quản lý client đăng ký (cấu hình/migration) | FR-014 | 1 | 2 ngày | – | todo |
| 13 | Client Credentials, token máy | FR-015, BR-008 | 4, 12 | 2–3 ngày | – | todo |
| 14 | Authorization Code + PKCE: authorize và trang đăng nhập | FR-012, FR-019, BR-005 | 2, 12, ADR trang đăng nhập | 4–5 ngày | – | todo |
| 15 | Endpoint token đổi mã (code_verifier) | FR-013 | 14 | 2–3 ngày | – | todo |
| 16 | OIDC: id_token, userinfo, discovery | FR-016–018 | 15 | 3–4 ngày | – | todo (Could) |
| 17 | Ủy quyền theo scope | FR-020 | 5 | 2 ngày | – | todo |
| 18 | Thu hồi token và introspection tối thiểu | FR-021, BR-006 | 13, 17 | 2–3 ngày | – | todo |

Tổng khoảng 40–55 ngày công; mỗi lát nhỏ hơn cỡ một plan (≤ ~3 ngày) trừ các lát 1, 14 (lớn nhất, cần cân nhắc chia thêm sau design).

## Tự kiểm theo `slicing.md` mục 3.2

- Phủ: FR-001…FR-022 đều có lát (FR-022 → 11). BR-001…008 và NFR-001…009 đều có lát chịu trách nhiệm; NFR-005 và NFR-009 là ràng buộc chung, không có lát riêng.
- Cỡ: lát 1 (khung + đăng ký) và lát 14 (authorize + trang HTML) ở mức cao nhất, đáng ngờ về cỡ.
- Thứ tự theo phụ thuộc rồi giá trị; lát 1–2 là "xương sống".
- Lát nào cũng có hành vi chạy được, không có lát "chuẩn bị" thuần.

## Điểm cần người dùng chốt (CONFIRM, chưa hỏi vì bảng còn nháp)

1. Ranh giới lát 1 và 14 có chia thêm không (ví dụ tách "khung service" khỏi "đăng ký"; tách "authorize" khỏi "trang đăng nhập")?
2. Lát 16 (Could) và 7 (xoay khoá) làm hay hoãn?
3. Thứ tự lát 5 (bộ kiểm tra dùng chung) trước hay sau 6, phụ thuộc vị trí đặt ở design.
4. Lát 12 đặt sớm (trước 13/14) hay làm cùng lát đầu tiên cần client.

## Ngoài lát (hoãn/không làm)
Theo mục "Phạm vi" của spec (chưa xác minh email, không role, không API quản trị client).

## Thứ tự và lý do
Nền → đăng nhập → bảo vệ chống tấn công → token → hạ tầng khoá → vòng đời refresh → OAuth/OIDC → ủy quyền, bám lộ trình I1–I8 của spec (mục 17).

## Confirm log

| # | Điểm cần chốt | Quyết định | Ngày |
|---|---|---|---|
