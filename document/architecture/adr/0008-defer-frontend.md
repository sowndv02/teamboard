# ADR 0008 — Tạm hoãn frontend (Angular) ở giai đoạn hiện tại

Trạng thái: Chấp nhận · Ngày: 2026-10-21
Nguồn: yêu cầu của chủ dự án
Nhãn: `[Cần]` (thu hẹp phạm vi để tập trung)
Liên quan: thay thế **một phần** ADR 0005 (phần Frontend/Angular); phần Backend ASP.NET Core vẫn hiệu lực.

## Bối cảnh
Dự án có phạm vi rộng so với một người học (6 service backend, Terraform, observability, bảo mật). Chủ dự án quyết định **không phát triển frontend ở giai đoạn này** để tập trung vào backend, hạ tầng và vận hành (mục tiêu G1–G3 trong `document/pm/roadmap.md`).

## Quyết định
- **Không phát triển frontend** ở giai đoạn hiện tại; không có thành phần `web`, không cần Node/npm/Angular CLI trong toolchain.
- Gỡ các quy tắc, cấu hình, lệnh và tham chiếu liên quan đến frontend khỏi tài liệu đang hiệu lực (rule Angular/frontend, `web` trong `config/services.yaml`, lệnh `npm`, gate Angular, mục `.gitignore` của Node).
- Tương tác với hệ thống ở giai đoạn này qua **API** (gọi bằng Postman/curl, Swagger UI của service, test tích hợp/BDD).
- Giữ nguyên các ADR đã chấp nhận (không sửa nội dung): ADR 0005, 0006, 0007 còn nhắc Angular/frontend như lịch sử; **các phần đó tạm không áp dụng** khi ADR này có hiệu lực.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. Hoãn frontend, chỉ backend + API (đã chọn) | Tập trung G1–G3; giảm phạm vi (rủi ro R1); ít công cụ hơn | Không có giao diện thấy được; luồng đăng nhập kiểu trình duyệt (Authorization Code) khó thử | Chọn |
| B. Làm frontend tối giản song song | Có giao diện để demo | Tăng phạm vi một người; chia nhỏ sự chú ý | Không chọn |
| C. Bỏ hẳn frontend vĩnh viễn | Gọn nhất | Mất cơ hội học tích hợp client–gateway–identity | Không chọn (chỉ hoãn) |

## Vì sao không chọn các phương án còn lại
- **B** sẽ đúng khi mục tiêu học gồm cả kỹ năng frontend hoặc cần demo cho người không kỹ thuật. **Tín hiệu xem lại:** các mốc M1–M3 hoàn thành và còn năng lực.
- **C** quá sớm để kết luận.

## Hệ quả và đánh đổi
- **Identity (ADR 0006):** I1–I4, I6, I8 không cần giao diện. I5 (Authorization Code + PKCE) và I7 (OpenID Connect) cần một user agent: khi tới đó chọn giữa trang đăng nhập HTML tối giản do `identity-service` phục vụ hoặc hoãn I5/I7 (`identity-learning.md`).
- **Gateway** không còn vai trò BFF cho UI; giữ vai trò API Gateway (định tuyến, kiểm JWT, rate limit).
- **Test BDD** chạy ở tầng API/service; không cần BDD runner cho UI.
- **Quyết định tồn đọng của frontend** (phiên bản Angular/Node, package manager, nơi lưu token, i18n, thư viện server-state) **bị hoãn**, không cần chốt bây giờ.
- Quyết định khôi phục: khi bắt đầu frontend, viết ADR mới thay thế ADR này, và khôi phục rule/config từ lịch sử git (commit `7e4ceb9` trở về trước chứa rule Angular/frontend).

## Tín hiệu xem lại
- Hoàn thành mốc M1–M3 và có năng lực dư.
- Nhu cầu demo giao diện hoặc muốn luyện tích hợp client.
- Yêu cầu mới của chủ dự án.

## Liên kết
ADR 0005, ADR 0006, `document/pm/roadmap.md` (R1), `document/architecture/identity-learning.md`.
