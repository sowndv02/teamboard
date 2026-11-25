---
key: TB-049
title: Lộ trình học Authentication Standards (khái niệm) theo Part 0–7 × L1–L8
status: in-progress
phase: 0
branch: feature/TB-049-learning-roadmap-auth-standards
estimate: L
created: 2026-11-25
started: 2026-11-25
closed: null
---

# TB-049 — Lộ trình học Authentication Standards theo Part 0–7 × L1–L8

## Bối cảnh
Bộ bài `auth_std_roadmap-main` của người dùng (32 bài, 5 lab, 8 quiz, 85 sơ đồ) rất chi tiết. Hai lần đưa vào `document/` trước đó (TB-047 một file bản đồ, TB-048 viết lại hình thức) đều nông: tóm tắt đề cương, không đọc bài chi tiết và không theo style của dự án. Người dùng cần chiều sâu, tham khảo chủ đề của bộ bài và cá nhân hoá theo style `learning-hub` (lộ trình hai chiều Part 0–7 × L1–L8). Dùng skill `learning-hub`, chế độ `pack`, hình thức lộ trình.

Nhánh cắt từ `origin/main` trong worktree `H:\learning\board-tb-049`; nhánh TB-048 (hai commit local chưa push, viết lại hình thức bản đồ cũ) không còn dùng.

## Phạm vi
- Làm: lộ trình khái niệm `document/learning/auth-standards/` (Part 0–7), **ưu tiên lý thuyết**, phủ 7 module của bộ bài (identity và nền tảng, session và token, OAuth/OIDC, SAML, doanh nghiệp và passwordless, phân quyền và bảo mật, kiến trúc); "Bản đồ giải pháp" thay cho `auth-methods-map.md` (xoá file này); cập nhật `document/learning/README.md` và `document/learning/identity/README.md`.
- Hoãn **code lab** sang [TB-050](TB-050-identity-auth-lab-dual-provider.md); tài liệu chiến lược dual provider: [identity-auth-lab.md](../architecture/identity-auth-lab.md). **Quiz** bỏ; L4 ma trận `–` đến khi TB-050 xong.
- Không làm: sửa `src/`, spec, design, ADR; chép nguyên văn bộ bài; sửa `.agents/**` (kể cả `references/topic-map.md`); **wire lab** (TB-050); chạy Keycloak trong CI mặc định; thử trên hệ thống thật; lưu khoá, mật khẩu hay token thật; lặp phần đã có ở lộ trình identity (JWT, PKCE, tấn công tự dựng: chỉ trỏ link); commit/push khi chưa được yêu cầu. Cho phép: tài liệu lab + `config/auth-development.example.yaml` (mẫu, không secret).

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Part 0 có ma trận phủ khớp nội dung, mục phạm vi, quan hệ với lộ trình identity và cảnh báo an toàn lab
- [ ] Mỗi cơ chế một thẻ có Use case; "Bản đồ giải pháp" ≥ 3 lựa chọn kể cả "không xác thực"
- [ ] Mỗi bài trong 32 bài gốc ánh xạ được vào một mục của lộ trình (bảng ánh xạ ở Part 0)
- [ ] Chiều sâu lý thuyết: mỗi bài gốc có cơ chế từng bước, bảng "dễ nhầm", hiểu lầm hay gặp, ví dụ và sơ đồ ở mục tương ứng (lab và quiz hoãn, ghi ở Part 0)
- [ ] Không bảng quá 4 cột (trừ ma trận phủ), không ô quá ~120 ký tự, không câu kể quy trình trong thân bài
- [ ] Mọi link `.md` và neo không gãy; mọi khối Mermaid parse và vẽ được
- [ ] Không secret trong tài liệu và repo; `auth-methods-map.md` đã xoá và không còn link tới nó

## Kế hoạch
1. Issue và nhánh (file này).
2. Đọc nguồn chiều sâu bằng 4 agent chỉ đọc theo nhóm module.
3. Lab: chiến lược TB-050 + `identity-auth-lab.md` (code chưa làm trong TB-049).
4. Viết Part 0–7 (lý thuyết).
5. Mục lục, liên kết, xoá bản đồ cũ, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-11-25: tạo issue theo kế hoạch người dùng duyệt (plan mode).
- 2026-11-25: viết `document/learning/auth-standards/` Part 0–7 từ `auth_std_roadmap-main`; xoá `identity/auth-methods-map.md`; cập nhật mục lục learning + identity README; lab/quiz hoãn.
- 2026-11-25: ghi chiến lược lab dual provider (`identity-auth-lab.md`, `auth-development.example.yaml`, issue TB-050); settings `Auth:Provider` Identity|Keycloak.
