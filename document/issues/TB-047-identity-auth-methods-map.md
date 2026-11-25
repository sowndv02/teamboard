---
key: TB-047
title: Bản đồ phương thức xác thực (bổ sung phần còn thiếu cho lộ trình identity)
status: in-progress
phase: 0
branch: feature/TB-047-identity-auth-methods-map
estimate: M             # S | M | L (tương đối; ghi lại thực tế để hiệu chỉnh)
created: 2026-11-25
started: 2026-11-25
closed: null            # ngày done/dropped
---

# TB-047 — Bản đồ phương thức xác thực

## Bối cảnh
Đối chiếu roadmap `auth_std_roadmap-main` (Authentication Standards 10–15 giờ) với `document/learning/identity/` (Part 0–7) cho thấy lộ trình đủ OAuth/OIDC/JWT nhưng thiếu hẳn HTTP auth, MFA/OTP/step-up, directory doanh nghiệp (LDAP, AD, Kerberos, NTLM, RADIUS), social login và account linking, so sánh SAML với OIDC; và chỉ nhắc tên passkey/WebAuthn, API key, Device grant, DPoP, SSO. Spec `identity-auth` giữ MFA, SAML, đăng nhập bên thứ ba ngoài phạm vi code (mục 4), nên đây chỉ là tài liệu để đọc. Dùng skill `learning-hub`, chế độ `pack`.

## Phạm vi
- Làm: một ghi chú khái niệm `document/learning/identity/auth-methods-map.md` theo mẫu 10 mục của `learning-hub` (mỗi phương thức một mục con có nguồn đã kiểm, Bản đồ giải pháp, ≥ 3 case thực tế có nguồn, mục "Trong TeamBoard"); cập nhật `document/learning/README.md` và `document/learning/identity/README.md`.
- Không làm: không đổi spec, design, code; không thêm Part 8; không sửa `.agents/**` (kể cả `references/topic-map.md`); không chạy lab mới.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] Ghi chú đủ 10 mục, tiêu đề "Bản đồ giải pháp" đúng nguyên văn
- [x] Mỗi phương thức thiếu hoặc chỉ nhắc tên của roadmap có mục riêng, kèm nguồn đã mở hoặc `Chưa kiểm`
- [x] ≥ 3 case real-world có bài gốc đã mở (do agent mở, mức Vừa); trạng thái real-world: Đạt một phần
- [x] Mục "Trong TeamBoard" nêu đúng phương thức nào được làm và phương thức nào ngoài phạm vi (theo spec mục 4)
- [x] Mọi link nội bộ trỏ tới file tồn tại; hai README đã cập nhật
- [x] Không secret, không dòng ghi công AI, diff chỉ gồm các file trong phạm vi

## Kế hoạch
1. Tra cứu nguồn song song theo 4 nhóm (HTTP/API key/session, MFA/passkey, federation, directory và OAuth nâng cao) cùng real-world.
2. Viết ghi chú, cập nhật hai README.
3. Checklist `learning-hub`, ghi nguồn đã kiểm và giới hạn vào issue.

## Ghi chú / Nhật ký
- 2026-11-25: tạo issue; nhánh từ `origin/main` trong worktree `H:\learning\board-tb-047` vì nhánh `feature/TB-024-gateway-rate-limit` đang có thay đổi chưa commit.
- 2026-11-25: tra cứu nguồn bằng 4 agent `web-fetch` (HTTP/API key/session; MFA/passkey; federation; directory và OAuth nâng cao). 37 trong 42 trang đã mở được; **5 trang không mở được** (W3C WebAuthn Level 3 và CISA PDF: 403; bài Uber 2022, bài passkey của Google, Microsoft Research pre-hijacking: 404), ghi `Chưa kiểm` trong ghi chú. Trang dài chỉ đọc nguyên văn khoảng 40.000 ký tự đầu, phần còn lại qua tóm tắt công cụ nên các ý đó cũng ghi `Chưa kiểm`.
- 2026-11-25: đã viết `document/learning/identity/auth-methods-map.md` (10 mục, 6 nhóm A–F, Bản đồ giải pháp, 3 case real-world), cập nhật `document/learning/README.md` và `document/learning/identity/README.md`.
- Điểm phát hiện để người dùng quyết (không tự sửa): spec `identity-auth` BR-002 yêu cầu mật khẩu tối thiểu 12 ký tự, NIST SP 800-63B-4 ghi 15 ký tự cho mật khẩu một yếu tố.
- Chưa làm (ngoài phạm vi): `.agents/skills/learning-hub/references/topic-map.md` chưa có dòng cho bản đồ này (thuộc `.agents/**`, chờ người dùng quyết); chưa chạy lab nào (mọi mục chưa chạy gắn ⚠ hoặc `Chưa kiểm`).

## Giới hạn đã biết
- Mức xác minh là **Vừa** cho mọi nguồn web (agent mở, tôi chưa tự mở lại); case real-world vì vậy chỉ **Đạt một phần**.
- Số liệu của Microsoft về passkey là số tự báo cáo của công ty.
- WS-Federation, risk-based, Conditional Access, workload identity, TACACS+, JAR, RAR, JARM, FAPI chỉ có tên và mô tả ngắn, chưa có nguồn riêng.
