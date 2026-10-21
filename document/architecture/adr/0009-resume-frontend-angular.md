# ADR 0009 — Làm lại frontend Angular (phạm vi giới hạn, kiểu dự án thực tế để học)

Trạng thái: Chấp nhận · Ngày: 2026-10-21
Nguồn: yêu cầu của chủ dự án trong lúc `system-design` identity-auth ("angular real-world project, học thêm angular"); `document/design/identity-auth.md` mục D6; `document/spec/identity-auth.md` Confirm log #13
Nhãn: `[Chọn để học]`
Liên quan: **thay thế ADR 0008** (hoãn frontend) và phần Frontend của ADR 0005; phần Backend ASP.NET Core vẫn hiệu lực.

## Bối cảnh
ADR 0008 hoãn frontend để tập trung backend/hạ tầng/vận hành. Chủ dự án nay muốn học Angular bằng một dự án giống thực tế (cấu trúc theo feature, kiểm thử, gate chất lượng, truy cập, hiệu năng, bảo mật client), và identity-service (Authorization Code + PKCE, OIDC) cần một client thật để học trọn luồng. Rủi ro đã biết: phạm vi một người (R1 trong `document/pm/roadmap.md`), thêm hệ công cụ Node.

## Quyết định
- Làm frontend Angular tại `src/web`, **phạm vi giới hạn theo giai đoạn**:
  1. Giai đoạn đầu: app nhỏ cho identity (đăng ký, đăng nhập, đổi mật khẩu, client OIDC), kèm khung ứng dụng (app shell, routing, config, i18n, lỗi, interceptor).
  2. Sau đó mới thêm màn hình board/task khi backend tương ứng có (spec riêng, mỗi tính năng một spec).
- FE chỉ gọi backend qua `gateway` (`/api/v1/...`); không truy cập DB; kiểu API sinh từ OpenAPI (một nguồn sự thật).
- Rule, skill và gate cho Angular được khôi phục và nâng cấp (`.agents/rules/angular*.md`, `frontend-*.md`; skill `angular-feature`, `frontend-review`, `angular-learning`).
- **Chưa quyết định (cần ADR/chốt khi scaffold):** phiên bản Angular và Node (xác minh tại angular.dev), package manager, nơi lưu token phía client (BFF qua gateway hay bộ nhớ + cookie `HttpOnly`), SPA hay SSR, công cụ i18n, thư viện server-state, cách deploy (đề xuất S3 + CloudFront), runner BDD UI (đề xuất Playwright).
- Không scaffold `src/web` trong ADR này; làm bằng issue riêng sau khi spec FE `confirmed`.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. Angular FE giới hạn theo giai đoạn (đã chọn) | Đúng mục tiêu học; có client thật cho OIDC; kiểm soát được phạm vi | Thêm công cụ, thêm việc cho một người | Chọn |
| B. Giữ ADR 0008, trang đăng nhập Razor tối giản | Ít công sức; không thêm Node | Không học Angular; vẫn có một trang HTML ngoại lệ | Không chọn |
| C. Hoãn I5/I7 đến khi có frontend | Không thêm phạm vi ngay | Mất phần học PKCE/OIDC sớm | Không chọn |
| D. FE toàn hệ thống ngay (board, task, activity) | Dự án thực tế đầy đủ | Rất lớn, loãng mục tiêu backend | Không chọn |

## Vì sao không chọn các phương án còn lại
- **B** sẽ đúng khi mục tiêu chỉ là thử luồng đăng nhập nhanh và không muốn thêm công cụ. **Tín hiệu:** nếu FE trễ làm chặn I5.
- **C** sẽ đúng khi năng lực không đủ cho FE trong giai đoạn này.
- **D** sẽ đúng khi backend ổn định và có thời gian dư; mở rộng dần theo spec riêng thay vì một lần.

## Hệ quả và đánh đổi
- Khôi phục rule/config/gate Angular (đã gỡ ở TB-002) kèm nâng cấp; gate FE thêm thời gian chạy (lint, test, build, a11y, e2e).
- Identity: FR-019 (trang đăng nhập HTML) phải cập nhật theo quyết định FE (login do identity phục vụ hay trong SPA); chạy lại `spec-review` cho spec identity-auth.
- Gateway giữ vai trò API Gateway và có thể là BFF nếu chọn BFF cho token (ADR riêng).
- Rủi ro phình phạm vi: giới hạn theo giai đoạn ở trên; mỗi tính năng FE qua workflow (spec → design → lát nhỏ).
- Khả năng đảo ngược: dễ (FE tách riêng `src/web`, không ảnh hưởng backend).

## Tín hiệu xem lại
- Lát backend I1–I4 trễ vì FE chiếm thời gian.
- FE chạy đồng thời với quá nhiều lát (vượt WIP `pm.wipLimit`).
- Chi phí hosting FE (S3 + CloudFront) làm vượt trần ngân sách.

## Liên kết
ADR 0005, 0006, 0008; `document/design/identity-auth.md`; `document/spec/identity-auth.md`; `document/pm/roadmap.md`; rule `.agents/rules/angular.md`, `frontend-engineering.md`.
