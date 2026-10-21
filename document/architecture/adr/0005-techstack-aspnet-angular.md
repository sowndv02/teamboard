# ADR 0005 — Tech stack chính: ASP.NET Core + Angular

Trạng thái: Chấp nhận (phần Backend); phần Frontend/Angular **tạm hoãn, thay thế một phần bởi ADR 0008** · Ngày: 2026-10-21

## Quyết định
- Backend: **ASP.NET Core (.NET 10)** cho mọi service chính.
- Frontend: **Angular** (SPA, standalone components, Signals) tại `src/web`, nói chuyện với backend chỉ qua `gateway`.
- `activity-service` vẫn có thể dùng ngôn ngữ khác nếu có ADR riêng (mặc định cũng là .NET).

## Hệ quả
- Thêm một thành phần `web` vào bản đồ service (xem `config/services.yaml`).
- Gateway đóng vai trò BFF cho Angular; CORS cấu hình ở gateway, không ở từng service.
- Cần Node.js + npm cho Angular CLI (phiên bản công cụ: xem `config/tool-versions.yaml`).
- Gate cho code thay đổi gồm cả .NET và Angular (`.agents/rules/code-change-gate.md`).

## Chưa quyết định (cần chốt khi bắt đầu làm `web`)
- Phiên bản Angular và Node cụ thể; package manager (mặc định đề xuất: npm).
- Cách deploy trên AWS: đề xuất S3 + CloudFront (static) thay vì chạy container; local dùng `ng serve` hoặc nginx container.
- Cách xác thực phía client (token từ `identity-service`: lưu ở đâu, refresh thế nào).

## Đánh đổi
Angular có cấu trúc chặt (DI, RxJS, CLI) hợp với mục tiêu luyện kiến trúc, nhưng nặng hơn framework nhẹ. Thêm một hệ công cụ (Node) bên cạnh .NET.
