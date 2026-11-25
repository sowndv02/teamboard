---
key: TB-050
title: Auth lab dual provider (identity vs Keycloak) và ghép lab 1–4 vào dự án
status: todo
phase: 0
branch: null
estimate: L
created: 2026-11-25
started: null
closed: null
---

# TB-050 — Auth lab: settings chọn identity hoặc Keycloak

## Bối cảnh

Người dùng muốn vừa học lab `auth_std_roadmap-main` (1–4) vừa làm `identity-service` (ADR 0006), không thay IdP bằng Keycloak trong stack chính. Chiến lược: **`Auth:Provider`** chọn một AS mỗi lần chạy; Keycloak optional qua compose profile. Tài liệu: [identity-auth-lab.md](../architecture/identity-auth-lab.md), mẫu [auth-development.example.yaml](../../config/auth-development.example.yaml).

Phụ thuộc slice identity (TB-032+ cho lab 2 đầy đủ; TB-042+ cho lab 3). Có thể làm phần Keycloak + RS switch trước khi identity đủ OAuth.

## Phạm vi

- Làm: wire `Auth` settings (RS + client lab); compose profile `keycloak-lab` (host **8090**); project `identity-client-lab`; mở rộng test/BDD theo kịch bản lab 2–4; cập nhật `COMMANDS.md` / auth-standards Part 0 khi có lệnh chạy.
- Không làm: SAML lab 5; Keycloak làm IdP production; sync user giữa hai IdP; federation; sửa ADR 0006 bỏ tự xây.

## Tiêu chí hoàn thành

- [ ] `Auth:Provider=Identity` và `Keycloak` đều chạy được luồng JWT tối thiểu (login hoặc CC) tới một RS có bảng lỗi tương đương lab 2.
- [ ] Compose `keycloak-lab` không bật khi profile không gọi; không đụng gateway 8080.
- [ ] Không secret trong repo; client secret qua env/user-secrets.
- [ ] CI chính chỉ test `Provider=Identity` (hoặc chỉ unit cho Keycloak path tách job).

## Ghi chú

- TB-049 chỉ ghi **lý thuyết** auth-standards; lab hands-on chuyển sang issue này.